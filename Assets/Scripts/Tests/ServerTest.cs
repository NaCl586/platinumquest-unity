/*using System;
using System.IO;
using Server.Replay;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Server;
using Server.DTOs.Requests;
using Server.DTOs.Responses;
using Server.Exceptions;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ServerTest : MonoBehaviour
{
    #region Configuration

    [Header("Credentials")]

    [SerializeField]
    private string username = "Gerson";

    [SerializeField]
    private string password = "123456";

    [SerializeField]
    private string invalidPassword = "123456789";

    [SerializeField]
    private string invalidUsername = "UnknownUser";

    [Header("Replay Test")]

    [SerializeField]
    private string replayTestFileName = "ServerTest.urec";

    [SerializeField]
    private string replayDownloadFileName = "ServerTest_Downloaded.urec";

    [Header("Level")]

    [SerializeField]
    private string level =
        "missions_mbp/beginner/Let'sRoll!";

    [SerializeField]
    private int page = 1;

    [SerializeField]
    private int pageSize = 10;

    #endregion

    [Header("Execution")]

    [SerializeField]
    private bool runOnStart = true;

    [SerializeField]
    private bool stopOnFailure = true;

    [SerializeField]
    private int testId = TEST_ALL;

    protected const int TEST_ALL = 999;

    protected class TestCase
    {
        public int Id;
        public string Name;
        public Func<UniTask> Action;

        public TestCase(
            int id,
            string name,
            Func<UniTask> action)
        {
            Id = id;
            Name = name;
            Action = action;
        }
    }

    private readonly List<TestCase> _tests =
        new List<TestCase>();

    private readonly List<string> _passed =
        new List<string>();

    private readonly List<string> _failed =
        new List<string>();

    private Stopwatch _stopwatch;

    private async void Start()
    {
        RegisterTests();

        if (!runOnStart)
            return;

        await Execute();
    }

    private void RegisterTests()
    {
        // Authentication

        _tests.Add(new TestCase(
            0,
            "Login Success",
            TestLoginSuccess));

        _tests.Add(new TestCase(
            1,
            "Wrong Password",
            TestWrongPassword));

        _tests.Add(new TestCase(
            2,
            "Unknown User",
            TestUnknownUser));

        _tests.Add(new TestCase(
            3,
            "Logout",
            TestLogout));

        // Score

        _tests.Add(new TestCase(
            100,
            "Submit Score",
            TestSubmitScore));

        _tests.Add(new TestCase(
            101,
            "Invalid Score",
            TestInvalidScore));

        _tests.Add(new TestCase(
            102,
            "Better Score",
            TestBetterScore));

        _tests.Add(new TestCase(
            103,
            "Worse Score",
            TestWorseScore));

        _tests.Add(new TestCase(
            104,
            "Unauthorized Submit",
            TestUnauthorizedSubmit));

        // Leaderboard

        _tests.Add(new TestCase(
            200,
            "Leaderboard",
            TestLeaderboard));

        // Replay

        _tests.Add(new TestCase(
            300,
            "Replay Upload",
            TestReplayUpload));

        _tests.Add(new TestCase(
            301,
            "Replay Download",
            TestReplayDownload));

        _tests.Add(new TestCase(
            302,
            "Replay Upload - Not Found",
            TestReplayUploadNotFound));

        _tests.Add(new TestCase(
            303,
            "Replay Download - Not Found",
            TestReplayDownloadNotFound));

        _tests.Add(new TestCase(
            304,
            "Replay Upload - Unauthorized",
            TestReplayUploadUnauthorized));

        _tests.Add(new TestCase(
            305,
            "Replay Download - Unauthorized",
            TestReplayDownloadUnauthorized));

        _tests.Add(new TestCase(
            306,
            "World Record",
            TestWorldRecord));

        _tests.Add(new TestCase(
            307,
            "World Record Replaced",
            TestWorldRecordReplaced));

        _tests.Add(new TestCase(
            308,
            "Stale World Record Replay",
            TestStaleWorldRecordReplay));
    }

    private async UniTask Login()
    {
        await Login(username, password);
    }

    private async UniTask Login(
        string testUsername,
        string testPassword)
    {
        await OnlineManager.Instance.Auth.LoginAsync(
            testUsername,
            testPassword,
            false);
    }

    private void Logout()
    {
        OnlineManager.Instance.Auth.Logout();
    }

    private UniTask<SubmitScoreResponse> Submit(
    int time)
    {
        return OnlineManager.Instance.Score.SubmitScoreAsync(
            new SubmitScoreRequest
            {
                Level = level,
                TimeMs = time
            });
    }

    private UniTask<LeaderboardResponse> GetLeaderboard()
    {
        return OnlineManager.Instance
            .Leaderboard
            .GetLeaderboardAsync(
                level,
                page,
                pageSize);
    }

    protected void AssertResponse(
    SubmitScoreResponse response)
    {
        AssertNotNull(
            response,
            "Response is null.");

        AssertTrue(
            response.TimeMs > 0,
            "Invalid score.");
    }

    protected async UniTask AssertThrows<T>(
    Func<UniTask> action)
    where T : Exception
    {
        try
        {
            await action();   // <-- panggil delegate

            throw new Exception(
                $"Expected {typeof(T).Name} but no exception was thrown.");
        }
        catch (Exception ex)
        {
            if (ex is T)
                return;

            throw new Exception(
                $"Expected {typeof(T).Name}, got {ex.GetType().Name}");
        }
    }

    private async UniTask Execute()
    {
        _passed.Clear();
        _failed.Clear();

        _stopwatch = Stopwatch.StartNew();

        if (testId == TEST_ALL)
        {
            foreach (TestCase test in _tests)
            {
                bool success =
                    await RunTest(test);

                if (!success && stopOnFailure)
                    break;
            }
        }
        else
        {
            TestCase test =
                _tests.Find(x => x.Id == testId);

            if (test == null)
            {
                Debug.LogError($"Unknown Test ID {testId}");
                return;
            }

            await RunTest(test);
        }

        _stopwatch.Stop();

        PrintSummary();
    }

    private async UniTask<bool> RunTest(
    TestCase test)
    {
        Debug.Log("");

        Debug.Log(
            $"========== TEST {test.Id} ==========");

        Debug.Log(test.Name);

        Stopwatch sw =
            Stopwatch.StartNew();

        try
        {
            await test.Action();

            sw.Stop();

            Debug.Log(
                $"PASS ({sw.ElapsedMilliseconds} ms)");

            _passed.Add(test.Name);

            return true;
        }
        catch (Exception ex)
        {
            sw.Stop();

            Debug.LogError(
                $"FAIL ({sw.ElapsedMilliseconds} ms)");

            Debug.LogException(ex);

            _failed.Add(test.Name);

            return false;
        }
    }

    private void PrintSummary()
    {
        Debug.Log("");

        Debug.Log(
            "========== SERVER TEST SUMMARY ==========");

        foreach (string name in _passed)
        {
            Debug.Log(
                $"PASS  {name}");
        }

        foreach (string name in _failed)
        {
            Debug.LogError(
                $"FAIL  {name}");
        }

        Debug.Log("");

        Debug.Log(
            $"Passed : {_passed.Count}");

        Debug.Log(
            $"Failed : {_failed.Count}");

        Debug.Log(
            $"Elapsed : {_stopwatch.Elapsed}");

        Debug.Log(
            "=========================================");
    }

    protected void AssertTrue(
    bool condition,
    string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    protected void AssertFalse(
        bool condition,
        string message)
    {
        if (condition)
            throw new Exception(message);
    }

    protected void AssertEqual<T>(
        T expected,
        T actual,
        string message = "")
    {
        if (!EqualityComparer<T>.Default.Equals(
                expected,
                actual))
        {
            throw new Exception(
                $"{message}\nExpected : {expected}\nActual : {actual}");
        }
    }

    protected void AssertNotNull(
        object obj,
        string message)
    {
        if (obj == null)
            throw new Exception(message);
    }

    private async UniTask TestLoginSuccess()
    {
        await Login();

        await OnlineManager.Instance.Auth.LoginAsync(
             username,
             password,
             false);

        ReplayRecorder.leaderboardRecording = true;

        await OnlineManager.Instance.ReplayUpload
            .UploadPendingReplayAsync();

        AssertTrue(
            OnlineManager.Instance.Auth.IsLoggedIn,
            "User should be logged in.");
    }

    private async UniTask TestWrongPassword()
    {
        await AssertThrows<UnauthorizedException>(
            async () =>
            {
                await OnlineManager.Instance.Auth.LoginAsync(
                    username,
                    invalidPassword,
                    false);
            });
    }

    private async UniTask TestBetterScore()
    {
        await Login();

        SubmitScoreResponse first =
            await Submit(15000);

        Debug.Log($"First: PB={first.IsNewPersonalBest}, Time={first.TimeMs}");

        SubmitScoreResponse second =
            await Submit(10000);

        Debug.Log($"Second: PB={second.IsNewPersonalBest}, Time={second.TimeMs}");

        AssertTrue(
            second.IsNewPersonalBest,
            "Should become new PB.");
    }

    private async UniTask TestLogout()
    {
        await Login();

        Logout();

        AssertFalse(
            OnlineManager.Instance.Auth.IsLoggedIn,
            "Logout failed.");
    }

    private async UniTask TestSubmitScore()
    {
        await Login();

        SubmitScoreResponse response =
            await Submit(10000);

        AssertResponse(response);
    }

    private async UniTask TestInvalidScore()
    {
        await Login();

        await AssertThrows<ValidationException>(
            async () =>
            {
                await Submit(-1);
            });
    }

    private async UniTask TestUnknownUser()
    {
        await AssertThrows<UnauthorizedException>(
            async () =>
            {
                await OnlineManager.Instance.Auth.LoginAsync(
                    invalidUsername,
                    password,
                    false);
            });
    }

    private async UniTask TestWorseScore()
    {
        await Login();

        await Submit(10000);

        SubmitScoreResponse response =
            await Submit(15000);

        AssertFalse(
            response.IsNewPersonalBest,
            "Should not become PB.");
    }

    private async UniTask TestUnauthorizedSubmit()
    {
        await Login();

        Logout();

        await AssertThrows<UnauthorizedException>(
            async () =>
            {
                await Submit(10000);
            });
    }

    private async UniTask TestLeaderboard()
    {
        await Login();

        LeaderboardResponse response =
            await GetLeaderboard();

        AssertNotNull(
            response,
            "Leaderboard is null.");

        AssertNotNull(
            response.Scores,
            "Score list is null.");

        foreach (ScoreResponse score in response.Scores)
        {
            Debug.Log(
                $"{score.Rank}. {score.PlayerName} ({score.TimeMs})");
        }
    }

    private string GetReplayTestDirectory()
    {
        string directory = Path.Combine(
            ReplayPaths.ReplayDirectory,
            "ServerTest");

        Directory.CreateDirectory(directory);

        return directory;
    }

    private string GetReplayTestFilePath()
    {
        return Path.Combine(
            GetReplayTestDirectory(),
            replayTestFileName);
    }

    private string GetReplayDownloadPath()
    {
        return Path.Combine(
            GetReplayTestDirectory(),
            replayDownloadFileName);
    }

    private async UniTask TestReplayUpload()
    {
        await Login();

        SubmitScoreResponse score =
            await CreateReplayTestScore();

        int scoreId = score.ScoreId;

        string replayPath =
            GetReplayTestFile();

        UploadReplayResponse response =
            await OnlineManager.Instance
                .Replay
                .UploadReplayAsync(
                    scoreId,
                    score.TimeMs,
                    replayPath);

        AssertNotNull(
            response,
            "Upload response is null.");

        AssertTrue(
            response.ReplayId > 0,
            "Invalid ReplayId.");

        Debug.Log(
            $"Replay uploaded successfully. " +
            $"ScoreId={scoreId}, " +
            $"ReplayId={response.ReplayId}");
    }

    private int _replayTestScoreId;

    private async UniTask TestReplayDownload()
    {
        await Login();

        SubmitScoreResponse score =
            await CreateReplayTestScore();

        int scoreId = score.ScoreId;

        string replayPath =
            GetReplayTestFile();

        await OnlineManager.Instance
            .Replay
            .UploadReplayAsync(
                scoreId,
                10000,
                replayPath);

        ReplayPaths.EnsureDirectories();

        var scoreTime = 10000;

        string fileName =
            $"{GetLevelFileName(level)}_" +
            $"{scoreTime}_" +
            $"{username}.urec";

        string savePath =
            Path.Combine(
                ReplayPaths.LeaderboardDirectory,
                fileName);

        if (File.Exists(savePath))
            File.Delete(savePath);

        await OnlineManager.Instance
            .Replay
            .DownloadReplayAsync(
                scoreId,
                savePath);

        AssertTrue(
            File.Exists(savePath),
            "Leaderboard replay was not downloaded.");

        FileInfo file =
            new FileInfo(savePath);

        AssertTrue(
            file.Length > 0,
            "Downloaded replay is empty.");

        Debug.Log(
            $"Leaderboard replay downloaded: {savePath}");

        Debug.Log(
            $"Size: {file.Length} bytes");
    }

    private string GetReplayTestFile()
    {
        string replayDirectory =
            Path.Combine(
                Directory.GetParent(
                    Application.dataPath).FullName,
                "Replay");

        string path =
            Path.Combine(
                replayDirectory,
                replayTestFileName);

        if (!File.Exists(path))
        {
            throw new Exception(
                $"Replay test file not found: {path}");
        }

        return path;
    }

    private async UniTask<SubmitScoreResponse> CreateReplayTestScore()
    {
        SubmitScoreResponse response =
            await Submit(10000);

        AssertNotNull(
            response,
            "Score response is null.");

        AssertTrue(
            response.ScoreId > 0,
            "Invalid ScoreId.");

        return response;
    }

    private string GetLevelFileName(string level)
    {
        string fileName =
            Path.GetFileName(level);

        return fileName;
    }

    private async UniTask TestReplayUploadNotFound()
    {
        await Login();

        string replayPath =
            GetReplayTestFile();

        await AssertThrows<NotFoundException>(
            () =>
                OnlineManager.Instance
                    .Replay
                    .UploadReplayAsync(
                        999999,
                        10000,
                        replayPath));
    }

    private async UniTask TestReplayDownloadNotFound()
    {
        await Login();

        int nonExistentScoreId = 999999999;

        string savePath =
            Path.Combine(
                ReplayPaths.LeaderboardDirectory,
                "ShouldNotExist.urec");

        if (File.Exists(savePath))
            File.Delete(savePath);

        await AssertThrows<NotFoundException>(
            () =>
                OnlineManager.Instance
                    .Replay
                    .DownloadReplayAsync(
                        nonExistentScoreId,
                        savePath));

        AssertFalse(
            File.Exists(savePath),
            "Replay file should not exist.");
    }

    private async UniTask TestReplayUploadUnauthorized()
    {
        await Login();

        SubmitScoreResponse score =
            await CreateReplayTestScore();

        string replayPath =
            GetReplayTestFile();

        OnlineManager.Instance.Auth.Logout();

        await AssertThrows<UnauthorizedException>(
            () =>
                OnlineManager.Instance
                    .Replay
                    .UploadReplayAsync(
                        score.ScoreId,
                        score.TimeMs,
                        replayPath));
    }

    private async UniTask TestReplayDownloadUnauthorized()
    {
        await Login();

        SubmitScoreResponse score =
            await CreateReplayTestScore();

        string replayPath =
            GetReplayTestFile();

        // Give the score a replay first.
        await OnlineManager.Instance
            .Replay
            .UploadReplayAsync(
                score.ScoreId,
                score.TimeMs,
                replayPath);

        string savePath =
            Path.Combine(
                ReplayPaths.LeaderboardDirectory,
                "UnauthorizedTest.urec");

        if (File.Exists(savePath))
            File.Delete(savePath);

        OnlineManager.Instance.Auth.Logout();

        await AssertThrows<UnauthorizedException>(
            () =>
                OnlineManager.Instance
                    .Replay
                    .DownloadReplayAsync(
                        score.ScoreId,
                        savePath));

        AssertFalse(
            File.Exists(savePath),
            "Unauthorized download should not create a replay file.");
    }

    private async UniTask TestWorldRecord()
    {
        await Login();

        SubmitScoreResponse first =
            await Submit(10000);

        Debug.Log(
            $"First: PB={first.IsNewPersonalBest}, " +
            $"WR={first.IsWorldRecord}, " +
            $"Time={first.TimeMs}");

        AssertTrue(
            first.IsNewPersonalBest,
            "First score should be a PB.");

        AssertTrue(
            first.IsWorldRecord,
            "First score should be the World Record.");

        SubmitScoreResponse second =
            await Submit(12000);

        Debug.Log(
            $"Second: PB={second.IsNewPersonalBest}, " +
            $"WR={second.IsWorldRecord}, " +
            $"Time={second.TimeMs}");

        AssertFalse(
            second.IsNewPersonalBest,
            "Worse score should not be a PB.");

        AssertFalse(
            second.IsWorldRecord,
            "Worse score should not be the World Record.");
    }

    private async UniTask TestWorldRecordReplaced()
    {
        // Player A
        await Login();

        SubmitScoreResponse first =
            await Submit(10000);

        Debug.Log(
            $"Player A: PB={first.IsNewPersonalBest}, " +
            $"WR={first.IsWorldRecord}, " +
            $"Time={first.TimeMs}");

        AssertTrue(
            first.IsNewPersonalBest,
            "Player A should have a new PB.");

        AssertTrue(
            first.IsWorldRecord,
            "Player A should initially be the World Record.");

        // Player B
        await Login(
            "NaCl586",
            "1234");

        SubmitScoreResponse second =
            await Submit(9000);

        Debug.Log(
            $"Player B: PB={second.IsNewPersonalBest}, " +
            $"WR={second.IsWorldRecord}, " +
            $"Time={second.TimeMs}");

        AssertTrue(
            second.IsNewPersonalBest,
            "Player B should have a new PB.");

        AssertTrue(
            second.IsWorldRecord,
            "Player B should become the new World Record.");

        AssertTrue(
            second.TimeMs < first.TimeMs,
            "Player B should have a better time.");
    }

    private async UniTask TestStaleWorldRecordReplay()
    {
        // =========================
        // Player A gets World Record
        // =========================

        await Login();

        SubmitScoreResponse playerA =
            await Submit(10000);

        AssertTrue(
            playerA.IsNewPersonalBest,
            "Player A should have a new PB.");

        AssertTrue(
            playerA.IsWorldRecord,
            "Player A should initially be the World Record.");

        // =========================
        // Create fake pending replay
        // =========================

        string replayPath =
            Path.Combine(
                ReplayPaths.PendingDirectory,
                "StaleWRTest.urec");

        if (File.Exists(replayPath))
            File.Delete(replayPath);

        Directory.CreateDirectory(
            ReplayPaths.PendingDirectory);

        File.WriteAllText(
            replayPath,
            "Fake replay data");

        PendingReplay pendingReplay =
            new PendingReplay
            {
                ScoreId = playerA.ScoreId,
                FileName = replayPath,
                RetryCount = 0
            };

        // =========================
        // Player B gets new WR
        // =========================

        await Login(
            "NaCl586",
            "1234");

        SubmitScoreResponse playerB =
            await Submit(9000);

        AssertTrue(
            playerB.IsNewPersonalBest,
            "Player B should have a new PB.");

        AssertTrue(
            playerB.IsWorldRecord,
            "Player B should become the new World Record.");

        // =========================
        // Put old replay into queue
        // =========================

        ReplayQueue queue =
            new ReplayQueue();

        queue.Enqueue(pendingReplay);

        ReplayUploadManager manager =
            new ReplayUploadManager(
                OnlineManager.Instance.Replay,
                queue);

        // =========================
        // Player A must be logged in
        // =========================

        await Login();

        // =========================
        // Upload pending replay
        // =========================

        await manager.UploadPendingReplayAsync();

        // =========================
        // Replay should be removed
        // =========================

        AssertFalse(
            queue.HasPendingReplay,
            "Stale replay should be removed from queue.");

        AssertTrue(
            File.Exists(replayPath),
            "This test should not delete the local replay file yet.");

        File.Delete(replayPath);

        Debug.Log(
            "Stale World Record replay test passed.");
    }
}*/
