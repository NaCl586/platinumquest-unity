/*using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OfflinePlayMission : PlayMissionManager
{
    [Header("Offline UI References")]
    public TextMeshProUGUI bestTimesText;
    public GameObject[] spaces;
    public GameObject statisticsWindow;
    public Button statisticsButton;

    protected override bool IsAnyWindowActive()
    {
        return base.IsAnyWindowActive() || (statisticsWindow && statisticsWindow.activeSelf);
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAnyWindowActive() && Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    protected override void Start()
    {
        if (statisticsWindow)
            statisticsWindow.SetActive(false);

        if (statisticsButton)
        {
            statisticsButton.onClick.AddListener(() =>
            {
                SetBlockerActive(true, false);
                GetComponent<StatisticsManager>()?.InitStatistics();
                ToggleWindow(statisticsWindow, true);
            });
        }

        base.Start();
    }

    protected override void UpdateMissionSpecificUI(int levelIndex)
    {
        SetSpacesActive(missions.Count != 0);

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 3; i++)
        {
            string name = PlayerPrefs.GetString(
                $"{MissionInfo.instance.levelName}_Name_{i}",
                "Matan W."
            );
            float time = PlayerPrefs.GetFloat($"{MissionInfo.instance.levelName}_Time_{i}", -1);

            if (string.IsNullOrEmpty(name))
                name = "\t\t";

            sb.Append($"{i + 1}. {name}\t");
            sb.AppendLine(Utils.FormatTime(time));
        }

        if (bestTimesText)
            bestTimesText.text = sb.ToString();
    }

    protected override void HandleEmptyMissionList()
    {
        SetSpacesActive(false);

        if (levelDescriptionText)
            levelDescriptionText.gameObject.SetActive(false);
        if (levelImage)
            levelImage.color = Color.clear;
        if (currentLevelText)
            currentLevelText.text = "Level 0";

        if (notQualifiedImage)
            notQualifiedImage.SetActive(true);
        if (notQualifiedText)
            notQualifiedText.SetActive(true);

        if (prev)
            prev.interactable = false;
        if (next)
            next.interactable = false;
        if (play)
            play.interactable = false;

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 3; i++)
        {
            sb.AppendLine($"{i + 1}. Matan W.\t{Utils.FormatTime(-1)}");
        }

        if (bestTimesText)
            bestTimesText.text = sb.ToString();
    }

    private void SetSpacesActive(bool active)
    {
        if (spaces == null)
            return;
        foreach (GameObject g in spaces)
        {
            if (g)
                g.SetActive(active);
        }
    }

    public void ToggleStatisticsWindow(bool active) => ToggleWindow(statisticsWindow, active);
}
*/