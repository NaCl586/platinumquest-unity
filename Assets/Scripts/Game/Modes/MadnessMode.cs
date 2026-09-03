using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MadnessMode : NullMode
{
    private bool gotAllGems;
    private int score;

    // Gems collected during the current run.
    private readonly List<Gem> collectedGems =
        new List<Gem>();

    // State saved at the latest checkpoint.
    private readonly List<Gem> checkpointCollectedGems =
        new List<Gem>();

    private int checkpointScore;
    private bool checkpointGotAllGems;

    // ------------------------------------------------------------
    // TIMER
    // ------------------------------------------------------------

    // Mission Time converted to seconds.
    private float startTime;

    // Countdown timer, in seconds.
    public float remainingTime;

    // Final elapsed time, in seconds.
    private float elapsedTime;

    // Mission alarmTime, already in seconds.
    private float alarmStartTime;

    private bool timerRunning;
    public bool alarmActive;
    private bool finished;

    private Coroutine alarmCoroutine;

    public MadnessMode(GameManager gameManager)
        : base(gameManager)
    {
    }

    // ------------------------------------------------------------
    // MISSION
    // ------------------------------------------------------------

    public override void OnMissionLoad()
    {
        base.OnMissionLoad();

        gotAllGems = false;
        score = 0;

        collectedGems.Clear();
        checkpointCollectedGems.Clear();

        checkpointScore = 0;
        checkpointGotAllGems = false;

        finished = false;
        timerRunning = false;
        alarmActive = false;

        StopMadnessAlarm();

        // --------------------------------------------------------
        // Time
        // --------------------------------------------------------
        //
        // MissionInfo.time is stored in milliseconds.
        //

        if (MissionInfo.instance != null &&
            MissionInfo.instance.time > 0)
        {
            startTime =
                MissionInfo.instance.time / 1000f;
        }
        else
        {
            startTime = 300f;
        }

        remainingTime = startTime;
        elapsedTime = 0f;

        // --------------------------------------------------------
        // Alarm
        // --------------------------------------------------------
        //
        // alarmTime is already in seconds.
        //

        alarmStartTime =
            MissionInfo.instance != null
                ? MissionInfo.instance.alarmTime
                : 0f;

        GameUIManager.instance.SetCurrentMadnessHuntGem(
            score
        );

        GameUIManager.instance.SetTimerText(
            remainingTime * 1000f
        );
    }

    // ------------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------------

    public override void OnUpdate()
    {
        if (!GameManager.gameStart)
            return;

        if (finished)
            return;

        if (!timerRunning)
            timerRunning = true;

        // --------------------------------------------------------
        // Time Travel
        // --------------------------------------------------------
        //
        // Time Travel pauses the Madness countdown.
        //

        if (gameManager.timeTravelActive || gameManager.timeStopTriggerCount > 0)
        {
            GameUIManager.instance.SetTimerText(
                remainingTime * 1000f, true
            );

            return;
        }

        // --------------------------------------------------------
        // Countdown
        // --------------------------------------------------------

        if (!gotAllGems)
        {
            remainingTime -= Time.deltaTime;

            // ----------------------------------------------------
            // Timer expired
            // ----------------------------------------------------

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;

                StopMadnessAlarm();

                timerRunning = false;
                finished = true;

                // No all-gems completion.
                // The finish score is the gem score.
                gameManager.elapsedTime = 0f;

                GameUIManager.instance.SetTimerText(0f);

                GameManager.onFinish?.Invoke();

                return;
            }

            // ----------------------------------------------------
            // Alarm
            // ----------------------------------------------------

            UpdateAlarmState();

            GameUIManager.instance.SetTimerText(
                remainingTime * 1000f
            );
        }
    }

    // ------------------------------------------------------------
    // ALARM STATE
    // ------------------------------------------------------------

    private void UpdateAlarmState()
    {
        if (!timerRunning ||
            gotAllGems ||
            finished ||
            alarmStartTime <= 0f)
        {
            return;
        }

        if (remainingTime <= alarmStartTime &&
            !alarmActive)
        {
            alarmActive = true;

            if (alarmCoroutine == null)
            {
                alarmCoroutine =
                    GameManager.instance.StartCoroutine(
                        AlarmCoroutine()
                    );
            }
        }
    }

    // ------------------------------------------------------------
    // ALARM COROUTINE
    // ------------------------------------------------------------

    private IEnumerator AlarmCoroutine()
    {
        GameUIManager.instance.SetCenterText(
            $"You have {Mathf.CeilToInt(alarmStartTime)} seconds remaining."
        );

        if (Marble.instance != null &&
            Marble.instance.alarmSound != null)
        {
            Marble.instance.alarmSound.Play();
        }

        float time = MissionInfo.instance.alarmTime;

        while (alarmActive)
        {
            // Time Travel pauses the alarm animation.
            if (!gameManager.timeTravelActive && gameManager.timeStopTriggerCount == 0)
            {
                time -= Time.deltaTime;
            }

            GameUIManager.instance.SetTimerColor(
                Mathf.FloorToInt(time) % 2 == 0
            );

            yield return null;
        }

        GameUIManager.instance.SetTimerColor(false);

        if (Marble.instance != null &&
            Marble.instance.alarmSound != null)
        {
            Marble.instance.alarmSound.Stop();
        }

        alarmCoroutine = null;
    }

    // ------------------------------------------------------------
    // STOP ALARM
    // ------------------------------------------------------------

    private void StopMadnessAlarm()
    {
        alarmActive = false;

        if (alarmCoroutine != null)
        {
            GameManager.instance.StopCoroutine(
                alarmCoroutine
            );

            alarmCoroutine = null;
        }

        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.SetTimerColor(false);
        }

        if (Marble.instance != null &&
            Marble.instance.alarmSound != null)
        {
            Marble.instance.alarmSound.Stop();
        }
    }

    // ------------------------------------------------------------
    // RESTART
    // ------------------------------------------------------------

    public override void OnRestart()
    {
        // A manual restart destroys the entire run.

        StopMadnessAlarm();

        score = 0;
        gotAllGems = false;

        collectedGems.Clear();
        checkpointCollectedGems.Clear();

        checkpointScore = 0;
        checkpointGotAllGems = false;

        remainingTime = startTime;
        elapsedTime = 0f;

        timerRunning = false;
        alarmActive = false;
        finished = false;

        gameManager.elapsedTime = 0f;

        GameUIManager.instance.SetCurrentMadnessHuntGem(
            score
        );

        GameUIManager.instance.SetTimerText(
            remainingTime * 1000f
        );
    }

    // ------------------------------------------------------------
    // CHECKPOINT
    // ------------------------------------------------------------

    public override void OnCheckpointReached()
    {
        checkpointScore = score;
        checkpointGotAllGems = gotAllGems;

        checkpointCollectedGems.Clear();

        checkpointCollectedGems.AddRange(
            collectedGems
        );
    }

    // ------------------------------------------------------------
    // RESPAWN
    // ------------------------------------------------------------

    public override void OnRespawn()
    {
        base.OnRespawn();

        if (gameManager.useCheckpoint)
        {
            // Restore Madness state from checkpoint.

            score = checkpointScore;
            gotAllGems = checkpointGotAllGems;

            collectedGems.Clear();

            collectedGems.AddRange(
                checkpointCollectedGems
            );

            GameUIManager.instance.SetCurrentMadnessHuntGem(
                score
            );

            if (gotAllGems)
            {
                GameUIManager.instance.SetTimerText(
                    elapsedTime * 1000f
                );
            }
            else
            {
                GameUIManager.instance.SetTimerText(
                    remainingTime * 1000f
                );
            }
        }
        else
        {
            // Full restart.

            StopMadnessAlarm();

            score = 0;
            gotAllGems = false;

            collectedGems.Clear();
            checkpointCollectedGems.Clear();

            checkpointScore = 0;
            checkpointGotAllGems = false;

            remainingTime = startTime;
            elapsedTime = 0f;

            timerRunning = false;
            alarmActive = false;
            finished = false;

            gameManager.elapsedTime = 0f;

            GameUIManager.instance.SetCurrentMadnessHuntGem(
                score
            );

            GameUIManager.instance.SetTimerText(
                remainingTime * 1000f
            );
        }
    }

    // ------------------------------------------------------------
    // FINISH
    // ------------------------------------------------------------

    public override bool CanFinish()
    {
        return true;
    }

    public override string GetFinishMessage()
    {
        return "Congratulations! You've finished!";
    }

    // ------------------------------------------------------------
    // GEM COLLECTION
    // ------------------------------------------------------------

    public override void OnGemCollected(
        Gem gem,
        int newGemCount)
    {
        if (gem == null || finished)
            return;

        // Track the gem for checkpoint restoration.
        if (!collectedGems.Contains(gem))
        {
            collectedGems.Add(gem);
        }

        int increment = 0;
        string message = null;
        Color messageColor = Color.white;

        switch (gem.gemType)
        {
            case GemType.Red:
                increment = 1;
                message = "+1";
                messageColor =
                    new Color32(
                        255,
                        102,
                        102,
                        255
                    );
                break;

            case GemType.Yellow:
                increment = 2;
                message = "+2";
                messageColor =
                    new Color32(
                        255,
                        255,
                        102,
                        255
                    );
                break;

            case GemType.Blue:
                increment = 5;
                message = "+5";
                messageColor =
                    new Color32(
                        102,
                        102,
                        255,
                        255
                    );
                break;

            case GemType.Platinum:
                increment = 10;
                message = "+10";
                messageColor =
                    new Color32(
                        221,
                        221,
                        221,
                        255
                    );
                break;
        }

        if (increment > 0)
        {
            score += increment;

            GameUIManager.instance.DisplayGemMessage(
                message,
                messageColor
            );

            GameUIManager.instance.SetCurrentMadnessHuntGem(
                score
            );
        }

        // --------------------------------------------------------
        // All gems collected
        // --------------------------------------------------------

        if (newGemCount >= gameManager.TotalGems)
        {
            gotAllGems = true;

            // Stop countdown and alarm.
            StopMadnessAlarm();

            // Convert remaining countdown time into
            // elapsed completion time.
            elapsedTime =
                startTime - remainingTime;

            elapsedTime =
                Mathf.Max(
                    0f,
                    elapsedTime
                );

            timerRunning = false;
            finished = true;

            // GameManager's finish system uses milliseconds.
            gameManager.elapsedTime =
                elapsedTime * 1000f;

            // Switch timer display from remaining time
            // to elapsed time.
            GameUIManager.instance.SetTimerText(
                elapsedTime * 1000f
            );

            GameManager.onFinish?.Invoke();
        }
    }

    // ------------------------------------------------------------
    // NORMAL GEM PICKUP MESSAGE
    // ------------------------------------------------------------

    public override string GetGemPickupMessage()
    {
        return string.Empty;
    }

    // ------------------------------------------------------------
    // PUBLIC STATE
    // ------------------------------------------------------------

    public bool GotAllGems =>
        gotAllGems;

    public int Score =>
        score;

    public float StartTime =>
        startTime;

    public float RemainingTime =>
        remainingTime;

    public float ElapsedTime =>
        elapsedTime;

    public float AlarmStartTime =>
        alarmStartTime;

    public bool AlarmActive =>
        alarmActive;

    public bool IsTimerRunning =>
        timerRunning;

    public bool IsFinished =>
        finished;

    public IReadOnlyList<Gem> CollectedGems =>
        collectedGems;

}