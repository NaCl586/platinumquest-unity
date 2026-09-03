using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Server;
using Server.DTOs.Responses;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RatingViewManager : MonoBehaviour
{
    public enum RatingGame
    {
        Platinum,
        Gold,
        Custom,
    }

    private const int PageSize = 10;

    [SerializeField]
    private GameObject raycastBlocker;

    [Header("General Window")]
    [SerializeField]
    private GameObject generalWindow;

    [SerializeField]
    private GameObject loadingTextGeneral;

    [SerializeField]
    private Button leftButtonGeneral;

    [SerializeField]
    private Button rightButtonGeneral;

    [SerializeField]
    private Button okayButtonGeneral;

    [SerializeField]
    private TextMeshProUGUI ratingListGeneral;

    [SerializeField]
    private TextMeshProUGUI playerListGeneral;

    [SerializeField]
    private TextMeshProUGUI pageTitleGeneral;

    [Header("Total Window")]
    [SerializeField]
    private GameObject totalWindow;

    [SerializeField]
    private GameObject loadingTextTotal;

    [SerializeField]
    private Button leftButtonTotal;

    [SerializeField]
    private Button rightButtonTotal;

    [SerializeField]
    private Button okayButtonTotal;

    [SerializeField]
    private Button nextGameButtonTotal;

    [SerializeField]
    private Button prevGameButtonTotal;

    [SerializeField]
    private TextMeshProUGUI ratingListTotal;

    [SerializeField]
    private TextMeshProUGUI playerListTotal;

    [SerializeField]
    private TextMeshProUGUI pageTitleTotal;

    // General State
    private int _generalPage = 1;
    private int _generalTotalPages = 1;
    private bool _isGeneralLoading;

    // Total State
    private int _totalPage = 1;
    private int _totalTotalPages = 1;
    private RatingGame _totalGame = RatingGame.Platinum;
    private bool _isTotalLoading;

    // Reusable StringBuilders to reduce allocations
    private readonly StringBuilder _playerSb = new StringBuilder();
    private readonly StringBuilder _ratingSb = new StringBuilder();

    #region Unity Lifecycle

    private void Awake()
    {
        // General Listeners
        if (leftButtonGeneral != null)
            leftButtonGeneral.onClick.AddListener(PreviousGeneralPage);
        if (rightButtonGeneral != null)
            rightButtonGeneral.onClick.AddListener(NextGeneralPage);
        if (okayButtonGeneral != null)
            okayButtonGeneral.onClick.AddListener(CloseGeneral);

        // Total Listeners
        if (leftButtonTotal != null)
            leftButtonTotal.onClick.AddListener(PreviousTotalPage);
        if (rightButtonTotal != null)
            rightButtonTotal.onClick.AddListener(NextTotalPage);
        if (okayButtonTotal != null)
            okayButtonTotal.onClick.AddListener(CloseTotal);
        if (prevGameButtonTotal != null)
            prevGameButtonTotal.onClick.AddListener(PreviousTotalGame);
        if (nextGameButtonTotal != null)
            nextGameButtonTotal.onClick.AddListener(NextTotalGame);

        UpdateGeneralButtons();
        UpdateTotalButtons();
    }

    #endregion

    #region General / Global

    public void ShowGeneral()
    {
        ShowGeneralAsync().Forget();
    }

    private async UniTask ShowGeneralAsync()
    {
        _generalPage = 1;
        _generalTotalPages = 1;

        generalWindow.SetActive(true);

        // Clear old data immediately.
        ClearList(playerListGeneral, ratingListGeneral);

        await LoadGeneralPageAsync();
    }

    private async UniTask LoadGeneralPageAsync()
    {
        SetGeneralLoading(true);

        try
        {
            var response = await OnlineManager.Instance.Rating.GetGlobalLeaderboardAsync(
                _generalPage,
                PageSize
            );

            if (response == null)
            {
                Debug.LogWarning("Global rating response was null.");
                ClearList(playerListGeneral, ratingListGeneral);
                return;
            }

            _generalPage = Mathf.Max(1, response.Page);
            _generalTotalPages = Mathf.Max(1, response.TotalPages);

            DisplayGeneralPage(response);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load global rating leaderboard.");
            Debug.LogException(ex);
            ClearList(playerListGeneral, ratingListGeneral);
        }
        finally
        {
            SetGeneralLoading(false);
        }
    }

    private void DisplayGeneralPage(GlobalRatingLeaderboardResponse response)
    {
        _playerSb.Clear().AppendLine("  \tPLAYER");
        _ratingSb.Clear().AppendLine("RATING");

        if (response.Players != null && response.Players.Count > 0)
        {
            int firstRank = ((response.Page - 1) * response.PageSize) + 1;

            for (int i = 0; i < response.Players.Count; i++)
            {
                var player = response.Players[i];

                int rank = firstRank + i;

                _playerSb.AppendLine($"{rank}. {player.PlayerName}");

                _ratingSb.AppendLine(FormatRating(player.GlobalRating));
            }
        }

        if (playerListGeneral != null)
        {
            playerListGeneral.text = _playerSb.ToString();
        }

        if (ratingListGeneral != null)
        {
            ratingListGeneral.text = _ratingSb.ToString();
        }

        UpdateGeneralTitle(response);
    }

    private void UpdateGeneralTitle(GlobalRatingLeaderboardResponse response)
    {
        if (pageTitleGeneral == null)
            return;

        int firstRank =
            response.TotalPlayers == 0 ? 0 : ((response.Page - 1) * response.PageSize) + 1;
        int lastRank =
            firstRank == 0
                ? 0
                : Mathf.Min(firstRank + response.Players.Count - 1, response.TotalPlayers);

        pageTitleGeneral.text =
            firstRank == 0 ? "Global Rating" : $"Global Rating ({firstRank} - {lastRank})";
    }

    private void PreviousGeneralPage()
    {
        if (_generalPage <= 1 || _isGeneralLoading)
            return;
        _generalPage--;
        LoadGeneralPageAsync().Forget();
    }

    private void NextGeneralPage()
    {
        if (_generalPage >= _generalTotalPages || _isGeneralLoading)
            return;
        _generalPage++;
        LoadGeneralPageAsync().Forget();
    }

    private void SetGeneralLoading(bool isLoading)
    {
        _isGeneralLoading = isLoading;
        if (loadingTextGeneral != null)
            loadingTextGeneral.SetActive(isLoading);
        UpdateGeneralButtons();
    }

    private void UpdateGeneralButtons()
    {
        if (leftButtonGeneral != null)
            leftButtonGeneral.gameObject.SetActive(!_isGeneralLoading && _generalPage > 1);
        if (rightButtonGeneral != null)
            rightButtonGeneral.gameObject.SetActive(
                !_isGeneralLoading && _generalPage < _generalTotalPages
            );
    }

    #endregion

    #region Total / Game-Specific

    public void ShowTotal()
    {
        ShowTotalAsync().Forget();
    }

    private async UniTask ShowTotalAsync()
    {
        _totalPage = 1;
        _totalTotalPages = 1;
        _totalGame = RatingGame.Platinum;

        totalWindow.SetActive(true);

        // Clear old data immediately.
        ClearList(playerListTotal, ratingListTotal);

        UpdateTotalTitle();

        await LoadTotalPageAsync();
    }

    private async UniTask LoadTotalPageAsync()
    {
        SetTotalLoading(true);

        try
        {
            string gameParam = GetGameParameter(_totalGame);
            var response = await OnlineManager.Instance.Rating.GetGameRatingLeaderboardAsync(
                gameParam,
                _totalPage,
                PageSize
            );

            if (response == null)
            {
                Debug.LogWarning("Game rating response was null.");
                ClearList(playerListTotal, ratingListTotal);
                return;
            }

            _totalPage = Mathf.Max(1, response.Page);
            _totalTotalPages = Mathf.Max(1, response.TotalPages);

            DisplayTotalPage(response);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load game rating leaderboard.");
            Debug.LogException(ex);
            ClearList(playerListTotal, ratingListTotal);
        }
        finally
        {
            SetTotalLoading(false);
        }
    }

    private void DisplayTotalPage(GameRatingLeaderboardResponse response)
    {
        _playerSb.Clear().AppendLine("  \tPLAYER");
        _ratingSb.Clear().AppendLine("RATING");

        if (response.Players != null && response.Players.Count > 0)
        {
            int firstRank = ((response.Page - 1) * response.PageSize) + 1;

            for (int i = 0; i < response.Players.Count; i++)
            {
                var player = response.Players[i];

                int rank = firstRank + i;

                _playerSb.AppendLine($"{rank}. {player.PlayerName}");

                _ratingSb.AppendLine(FormatRating(player.Rating));
            }
        }

        if (playerListTotal != null)
        {
            playerListTotal.text = _playerSb.ToString();
        }

        if (ratingListTotal != null)
        {
            ratingListTotal.text = _ratingSb.ToString();
        }

        UpdateTotalTitle(response.Players?.Count ?? 0);
    }

    private void PreviousTotalPage()
    {
        if (_totalPage <= 1 || _isTotalLoading)
            return;
        _totalPage--;
        LoadTotalPageAsync().Forget();
    }

    private void NextTotalPage()
    {
        if (_totalPage >= _totalTotalPages || _isTotalLoading)
            return;
        _totalPage++;
        LoadTotalPageAsync().Forget();
    }

    private void PreviousTotalGame()
    {
        if (_isTotalLoading)
            return;

        _totalGame = _totalGame switch
        {
            RatingGame.Platinum => RatingGame.Custom,
            RatingGame.Gold => RatingGame.Platinum,
            RatingGame.Custom => RatingGame.Gold,
            _ => RatingGame.Platinum,
        };

        _totalPage = 1;
        UpdateTotalTitle();
        LoadTotalPageAsync().Forget();
    }

    private void NextTotalGame()
    {
        if (_isTotalLoading)
            return;

        _totalGame = _totalGame switch
        {
            RatingGame.Platinum => RatingGame.Gold,
            RatingGame.Gold => RatingGame.Custom,
            RatingGame.Custom => RatingGame.Platinum,
            _ => RatingGame.Platinum,
        };

        _totalPage = 1;
        UpdateTotalTitle();
        LoadTotalPageAsync().Forget();
    }

    private void UpdateTotalTitle(int currentPlayersCount = 0)
    {
        if (pageTitleTotal == null)
            return;

        string gameName = GetGameDisplayName(_totalGame);

        if (_totalTotalPages == 0 || currentPlayersCount == 0)
        {
            pageTitleTotal.text = gameName;
            return;
        }

        int firstRank = ((_totalPage - 1) * PageSize) + 1;
        int lastRank = firstRank + currentPlayersCount - 1;

        pageTitleTotal.text = $"{gameName} ({firstRank} - {lastRank})";
    }

    private void SetTotalLoading(bool isLoading)
    {
        _isTotalLoading = isLoading;
        if (loadingTextTotal != null)
            loadingTextTotal.SetActive(isLoading);
        UpdateTotalButtons();
    }

    private void UpdateTotalButtons()
    {
        if (leftButtonTotal != null)
            leftButtonTotal.gameObject.SetActive(!_isTotalLoading && _totalPage > 1);
        if (rightButtonTotal != null)
            rightButtonTotal.gameObject.SetActive(
                !_isTotalLoading && _totalPage < _totalTotalPages
            );
        if (prevGameButtonTotal != null)
            prevGameButtonTotal.interactable = !_isTotalLoading;
        if (nextGameButtonTotal != null)
            nextGameButtonTotal.interactable = !_isTotalLoading;
    }

    #endregion

    #region Helpers & Actions

    private static string GetGameParameter(RatingGame game) =>
        game switch
        {
            RatingGame.Gold => "gold",
            RatingGame.Platinum => "platinum",
            RatingGame.Custom => "custom",
            _ => "platinum",
        };

    private static string GetGameDisplayName(RatingGame game) =>
        game switch
        {
            RatingGame.Gold => "Marble Blast Gold",
            RatingGame.Platinum => "Marble Blast Platinum",
            RatingGame.Custom => "Custom Missions",
            _ => "Rating",
        };

    private static string FormatRating(int rating) => rating.ToString("N0");

    private static void ClearList(TextMeshProUGUI playerList, TextMeshProUGUI ratingList)
    {
        if (playerList != null)
            playerList.text = string.Empty;
        if (ratingList != null)
            ratingList.text = string.Empty;
    }

    private void CloseGeneral()
    {
        raycastBlocker.SetActive(false);
        generalWindow.SetActive(false);
    }

    private void CloseTotal()
    {
        raycastBlocker.SetActive(false);
        totalWindow.SetActive(false);
    }

    #endregion
}
