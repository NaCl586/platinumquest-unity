using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using PlatinumQuestScripts;
using Server;
using Server.DTOs.Responses;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Core References")]
    public GameObject mainCam;
    public GameObject gameUIManager;
    [SerializeField] private AudioSource audioSource;
    public GameObject startPad, finishPad;
    public Transform activeCheckpoint;
    public Vector3 activeCheckpointGravityDir;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip jump, puSpawn, puReady, puSet, puGo, puFinish, puOutOfBounds, puHelp, puMissingGems;
    [SerializeField] private AudioClip checkpointSfx, overParTimeSfx, bassPunch, platformSurpriseSfx, helpTutorialSfx;

    [Header("UI Menu")]
    public GameObject pauseMenu, finishMenu;
    [SerializeField] private GameObject enterNameMenu, platinumTimeBox, ultimateTimeBox, awesomeTimeBox, ratingGameObject;
    [SerializeField] private TextMeshProUGUI finalTime, finalTimeCaption, finishCaption, numbersCaption, namesCaption, timesCaption, enterNameCaption, parTimeText, timePassedText, clockBonusesText, ratingText;
    [SerializeField] private Button replayButton, continueButton, noButton, yesButton, restartButton, okayButton;
    [SerializeField] private TMP_InputField nameInputField;

    [Header("Particles")]
    public GameObject finishParticles;
    private GameObject finishParticleInstance;

    // Game state
    public static bool gameFinish = false, gameStart = false, isPaused = false, alarmIsPlaying = false, notQualified = false;
    [HideInInspector] public bool alarmCoroutineStarted, countdownActive, useCheckpoint, timeTravelActive, spawnAudioPlayed, superBounceIsActive, shockAbsorberIsActive, gyrocopterIsActive;
    [HideInInspector] public int timeStopTriggerCount, currentGems;
    [HideInInspector] public float countdownRemaining, elapsedTime, huntTimeRemaining, sbsaActiveTime, gyroActiveTime, timeTravelStartTime, timeTravelBonus;
    [HideInInspector] public string countdownIcon = "timerTimeTravel";

    [HideInInspector] public List<GameObject> recentGems = new List<GameObject>();
    [HideInInspector] public PowerupType tempPowerup, activePowerup;

    public float HuntTimeRemaining => huntTimeRemaining;
    private float bonusTime;
    private string bestTimeName = string.Empty;
    private int pendingNamePosition = -1, totalGems;
    private Gem[] gems;
    public Gem[] Gems => gems;
    public int CurrentGems => currentGems;
    public int TotalGems => totalGems;

    private readonly List<IGameMode> gameModes = new List<IGameMode>();
    public IReadOnlyList<IGameMode> GameModes => gameModes;
    public ISpecialGameMode specialGameMode;

    private Coroutine alarmCoroutine;
    private bool isSubmitInProgress, startTimer, canPressEnter = true, huntRestartRequested;
    private int? serverRating;

    private string[] oobRandom, oobSpecial;
    private int[] specialThresholds;

    public bool WasFullReset { get; private set; }
    public bool IsOutOfBounds { get; private set; }
    public SpawnTrigger[] spawnTriggers;
    public bool HuntRestartRequested { get => huntRestartRequested; set => huntRestartRequested = value; }

    // Events
    public class OnFinish : UnityEvent { }
    public class OnOutOfBounds : UnityEvent { }
    public class OnCollectGem : UnityEvent<int> { }
    public class OnReachCheckpoint : UnityEvent<Transform, Vector3> { }

    public static OnFinish onFinish = new OnFinish();
    public static OnOutOfBounds onOutOfBounds = new OnOutOfBounds();
    public static OnCollectGem onCollectGem = new OnCollectGem();
    public static OnReachCheckpoint onReachCheckpoint = new OnReachCheckpoint();
    public Texture whiteTexture;

    int maxFrameRate;
    bool vSync;

    private void Awake()
    {
        instance = this;
        InitializeOutOfBoundsInsult();
        onFinish.AddListener(Finish);
        onOutOfBounds.AddListener(OutOfBounds);
        onCollectGem.AddListener(UpdateGem);
        Marble.onRespawn.AddListener(Respawn);
        StartCoroutine(AssignReferences());
        InitGameMode();

        maxFrameRate = PlayerPrefs.GetInt("Graphics_MaxFramerate", 7);
        vSync = PlayerPrefs.GetInt("Graphics_VerticalSync", 1) == 1;
    }

    private IEnumerator AssignReferences()
    {
        while (!Marble.instance) yield return null;
        startPad = GameObject.Find("StartPad");
        finishPad = GameObject.Find("EndPad");
        mainCam.SetActive(true);
        gameUIManager.SetActive(true);
        activeCheckpoint = startPad.transform.Find("Spawn");
        activeCheckpointGravityDir = Vector3.down;
    }

    public TwoDMode ActivateTwoDMode()
    {
        foreach (IGameMode mode in gameModes)
            if (mode is TwoDMode twoDMode) return twoDMode;

        TwoDMode newTwoDMode = new TwoDMode(this);
        gameModes.Add(newTwoDMode);
        return newTwoDMode;
    }

    public void DeactivateTwoDMode()
    {
        for (int i = gameModes.Count - 1; i >= 0; i--)
            if (gameModes[i] is TwoDMode) gameModes.RemoveAt(i);

        if (gameModes.Count == 0) gameModes.Add(new NullMode(this));
    }

    private void Start()
    {
        TotalTimeTracker.instance?.StartLevelTracking(MissionInfo.instance.levelName);
        isPaused = isSubmitInProgress = startTimer = timeTravelActive = false;
        activePowerup = PowerupType.None;
        finishMenu.SetActive(false);
        pauseMenu.SetActive(false);

        okayButton.onClick.AddListener(CloseEnterNameWindow);
        replayButton.onClick.AddListener(() => {
            if (ReplayRecorder.recordReplay)
            {
                pauseMenu.SetActive(false);
                finishMenu.SetActive(false);
                GameUIManager.instance.SaveAndRetry();
                GameUIManager.instance.saveReplayMenu.SetActive(true);
            }
            else ReplayLevel();
        });

        continueButton.onClick.AddListener(ReturnToMenu);
        noButton.onClick.AddListener(TogglePause);
        yesButton.onClick.AddListener(OnConfirmExit);
        restartButton.onClick.AddListener(RestartLevel);
        nameInputField.onEndEdit.AddListener(UpdateName);

        UpdateBestTimes();
        spawnAudioPlayed = useCheckpoint = gameStart = gameFinish = alarmCoroutineStarted = false;
        onReachCheckpoint.AddListener(ReachCheckpoint);

        if (OnlineManager.Instance?.Auth?.IsLoggedIn == true && !ReplayRecorder.loadReplay && !LeaderboardsMenu.ReplayCenterLoadedFromLeaderboards)
            OnlineManager.Instance.Chat.SetStatus("Playing").Forget();
    }

    public void InitGameMode()
    {
        gameModes.Clear();
        if (MissionInfo.instance == null || MissionInfo.instance.gameModes == null || MissionInfo.instance.gameModes.Count == 0)
        {
            if (MissionInfo.instance == null) Debug.LogError("Cannot initialize GameModes: MissionInfo.instance is null.");
            gameModes.Add(new NullMode(this));
            return;
        }

        GameUIManager guim = GameObject.FindObjectOfType<GameUIManager>(true);

        guim.ShowSpeedometer(false);
        guim.ShowMadnessHuntGemCountUI(false);

        foreach (Mode mode in MissionInfo.instance.gameModes)
        {
            switch (mode)
            {
                case Mode.Quota: gameModes.Add(new QuotaMode(this, MissionInfo.instance.gemQuota)); break;
                case Mode.Laps: gameModes.Add(new LapsMode(this)); StartCoroutine(DisableFinish()); break;
                case Mode.TwoD: gameModes.Add(new TwoDMode(this)); break;
                case Mode.Consistency: gameModes.Add(new ConsistencyMode(this)); guim.ShowSpeedometer(true); break;
                case Mode.Haste: gameModes.Add(new HasteMode(this)); guim.ShowSpeedometer(true); break;
                case Mode.Madness: gameModes.Add(new MadnessMode(this)); guim.ShowMadnessHuntGemCountUI(true); break;
                case Mode.Hunt: gameModes.Add(new HuntMode(this)); guim.ShowMadnessHuntGemCountUI(true); huntRestartRequested = true; break;
            }
        }

        if (gameModes.Count == 0) gameModes.Add(new NullMode(this));
    }

    public void AddGameMode(IGameMode mode) { if (mode != null) gameModes.Add(mode); }

    public bool RemoveGameMode(IGameMode mode)
    {
        if (mode == null) return false;
        bool removed = gameModes.Remove(mode);
        if (gameModes.Count == 0) gameModes.Add(new NullMode(this));
        return removed;
    }

    private IEnumerator DisableFinish()
    {
        while (finishPad == null)
        {
            finishPad = GameObject.Find("EndPad");
            yield return null;
        }
        finishPad.SetActive(false);
        finishPad = null;
    }

    private void OnConfirmExit()
    {
        if (ReplayRecorder.recordReplay)
        {
            ReplayRecorder.incompleteReplay = true;
            ReplayRecorder.Instance.StopRecording();
            GameUIManager.instance.SaveAndReturn();
            GameUIManager.instance.saveReplayMenu.SetActive(true);
        }
        else LoadSceneByAuth("PlayMission", "LBPlayMission");
    }

    public void InitializeHuntCheckpoint()
    {
        if (spawnTriggers == null || spawnTriggers.Length == 0) spawnTriggers = GameObject.FindObjectsOfType<SpawnTrigger>();
        if (spawnTriggers == null || spawnTriggers.Length == 0) { CameraController.instance.ResetCam(); return; }

        SpawnTrigger selected = spawnTriggers[UnityEngine.Random.Range(0, spawnTriggers.Length)];
        if (selected == null) { CameraController.instance.ResetCam(); return; }

        Transform spawnPoint = selected.transform.Find("SpawnPos/Spawn");
        Transform cameraPos = selected.transform.Find("SpawnPos/CameraPos");

        SetActiveCheckpoint(spawnPoint != null ? spawnPoint : selected.transform, -selected.transform.forward);
        CameraController.instance.SetCameraPosition(spawnPoint.position, cameraPos.position);

        var sp = GameObject.Find("StartPad");
        if (sp != null) { sp.SetActive(false); startPad = null; }
    }

    #region Audio Delegates
    public void PlayJumpAudio() => audioSource.PlayOneShot(jump);
    public void PlaySpawnAudio() => audioSource.PlayOneShot(puSpawn);
    public void PlayReadyAudio() => audioSource.PlayOneShot(puReady);
    public void PlaySetAudio() => audioSource.PlayOneShot(puSet);
    public void PlayGoAudio() => audioSource.PlayOneShot(puGo);
    public void PlayFinishAudio() => audioSource.PlayOneShot(puFinish);
    public void PlayOutOfBoundsAudio() => audioSource.PlayOneShot(puOutOfBounds);
    public void PlayHelpAudio() => audioSource.PlayOneShot(puHelp);
    public void PlayMissingGemAudio() => audioSource.PlayOneShot(puMissingGems);
    public void PlayBassPunchAudio() => audioSource.PlayOneShot(bassPunch);
    public void PlayHelpTriggerAudio() => audioSource.PlayOneShot(helpTutorialSfx);
    public void PlayPlatformSurpriseSfx() => audioSource.PlayOneShot(platformSurpriseSfx);
    public void PlayAudioClip(AudioClip clip) => audioSource.PlayOneShot(clip);

    public void PlayLevelMusic()
    {
        JukeboxManager.instance.audioSource.volume = PlayerPrefs.GetFloat("Audio_MusicVolume", 0.5f);
        JukeboxManager.instance.PlayMusic(MissionInfo.instance.music, true);
    }

    public void SetSoundVolumes()
    {
        float vol = PlayerPrefs.GetFloat("Audio_SoundVolume", 0.5f);
        foreach (var src in FindObjectsOfType<AudioSource>()) src.volume = vol;
    }
    #endregion

    public void InitGemCount()
    {
        gems = FindObjectsOfType<Gem>();
        totalGems = gems.Length;
        GameUIManager.instance.ShowGemCountUI(totalGems > 0);
    }

    public PowerupType ConsumePowerup()
    {
        PowerupType powerup = activePowerup;
        activePowerup = PowerupType.None;
        GameUIManager.instance.SetPowerupIcon(activePowerup);
        return powerup;
    }

    private void ForEachGameMode(Action<IGameMode> action) { for (int i = 0; i < gameModes.Count; i++) action(gameModes[i]); }

    private void ApplyFramerate(int selectedMaxFramerate)
    {
        switch (selectedMaxFramerate)
        {
            case 0:
                Application.targetFrameRate = 30;
                break;

            case 1:
                Application.targetFrameRate = 45;
                break;

            case 2:
                Application.targetFrameRate = 60;
                break;

            case 3:
                Application.targetFrameRate = 75;
                break;

            case 4:
                Application.targetFrameRate = 120;
                break;

            case 5:
                Application.targetFrameRate = 200;
                break;

            case 6:
                Application.targetFrameRate = -1;
                break;

            case 7:
                Application.targetFrameRate = -1;
                break;
        }
    }

    private void ApplyVSync(int selectedMaxFramerate, bool selectedVSync)
    {
        if (selectedMaxFramerate == 7)
        {
            QualitySettings.vSyncCount = 1;
            return;
        }

        QualitySettings.vSyncCount =
            selectedVSync ? 1 : 0;
    }

    private void Update()
    {
        ApplyFramerate(maxFrameRate);
        ApplyVSync(maxFrameRate, vSync);

        specialGameMode?.Update();
        if (activeCheckpoint == null) return;

        ForEachGameMode(mode => mode.OnUpdate());

        MadnessMode madnessMode = GetGameMode<MadnessMode>();
        HuntMode huntMode = GetGameMode<HuntMode>();

        if (huntMode != null)
        {
            if (startTimer && !timeTravelActive && !gameFinish && timeStopTriggerCount == 0)
            {
                huntTimeRemaining = Mathf.Max(0f, huntTimeRemaining - Time.deltaTime * 1000f);
                GameUIManager.instance.SetTimerText(huntTimeRemaining);
                if (huntTimeRemaining <= 0f) { GameUIManager.instance.SetTimerText(0f); Finish(); }
            }
            else if ((timeTravelActive && !gameFinish) || timeStopTriggerCount > 0)
                GameUIManager.instance.SetTimerText(huntTimeRemaining, true);
        }
        else if (madnessMode == null)
        {
            if (startTimer && !timeTravelActive && timeStopTriggerCount == 0)
            {
                elapsedTime = Mathf.RoundToInt(elapsedTime + Time.deltaTime * 1000f);
                GameUIManager.instance.SetTimerText(elapsedTime);
            }
            else if (timeTravelActive || timeStopTriggerCount > 0)
                GameUIManager.instance.SetTimerText(elapsedTime, true);
        }

        if (madnessMode == null && huntMode == null)
        {
            if (gameStart && MissionInfo.instance.time != -1 && elapsedTime >= (MissionInfo.instance.time - MissionInfo.instance.alarmTime * 1000))
            {
                if (elapsedTime >= MissionInfo.instance.time) { alarmIsPlaying = false; notQualified = true; }
                else
                {
                    alarmIsPlaying = true; notQualified = false;
                    if (!alarmCoroutineStarted) { alarmCoroutineStarted = true; alarmCoroutine = StartCoroutine(AlarmCoroutine()); }
                }
            }
            else notQualified = false;
        }

        UpdateCountdown();

        if ((shockAbsorberIsActive || superBounceIsActive) && Time.time - sbsaActiveTime > 5f) Marble.instance.RevertMaterial();
        if (gyrocopterIsActive && Time.time - gyroActiveTime > 5f) Marble.instance.CancelGyrocopter();

        if (timeTravelActive && (!gameFinish || !gameStart))
        {
            float elapsed = Time.time - timeTravelStartTime;
            float remainingTime = timeTravelBonus - elapsed;
            bonusTime += Time.deltaTime * 1000f;

            if (!gameFinish) GameUIManager.instance.SetTimeTravelTimer(remainingTime * 1000f);
            if (elapsed >= timeTravelBonus)
            {
                bonusTime -= (elapsed - timeTravelBonus) * 1000f;
                if (!gameFinish) GameUIManager.instance.SetTimeTravelTimer(-1);
                Marble.instance.InactivateTimeTravel();
            }
        }
        else if (!timeTravelActive && !gameFinish) GameUIManager.instance.SetTimeTravelTimer(-1);

        if (GameUIManager.instance != null && GameUIManager.instance.IsChatInputOpen) return;

        if (ReplayRecorder.loadReplay) { if (Input.GetKeyDown(KeyCode.Escape)) ReturnToMenu(); }
        else if (Input.GetKeyDown(KeyCode.Escape) && !gameFinish) TogglePause();

        if (gameFinish && !ReplayRecorder.loadReplay)
        {
            if (GameUIManager.instance.viceSaveWindow.activeSelf) return;
            else if (enterNameMenu.activeSelf && Input.GetKeyDown(KeyCode.Return)) CloseEnterNameWindow();
            else if (finishMenu.activeSelf && Input.GetKeyDown(KeyCode.Return) && canPressEnter) ReturnToMenu();
        }
    }

    public IEnumerator AlarmCoroutine()
    {
        GameUIManager.instance.SetCenterText($"You have {MissionInfo.instance.alarmTime} seconds remaining.");
        Marble.instance.alarmSound.Play();
        float time = 0f;
        while (!notQualified)
        {
            if (!timeTravelActive) time += Time.deltaTime;
            GameUIManager.instance.SetTimerColor(Mathf.FloorToInt(time) % 2 == 0);
            yield return null;
        }
        GameUIManager.instance.SetCenterText("The clock has passed the Par Time - please retry the level.");
        Marble.instance.alarmSound.Stop();
        PlayAudioClip(overParTimeSfx);
    }

    public void TogglePause()
    {
        if (GameUIManager.instance.isInitialized && (GameUIManager.instance.oobInsultMenu.activeSelf || GameUIManager.instance.saveReplayMenu.activeSelf)) return;
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        pauseMenu.SetActive(isPaused);
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }

    public bool CheckForAllGems() => totalGems == currentGems;

    private void UpdateGem(int count)
    {
        if (totalGems == 0) return;
        currentGems = Mathf.Abs(count);
        GameUIManager.instance.SetCurrentGem(currentGems);
        if (count > 0)
        {
            string message = gameModes.Count > 0 ? gameModes[gameModes.Count - 1].GetGemPickupMessage() : $"You picked up a gem! {totalGems - currentGems} gems to go!";
            GameUIManager.instance.SetBottomText(message);
        }
    }

    public void OnGemCollected(Gem gem)
    {
        if (gem == null) return;
        ForEachGameMode(mode => mode.OnGemCollected(gem, currentGems));
        if (specialGameMode is TakeTheGoldMode takeTheGold) takeTheGold.OnGemPickup();
    }

    #region Countdown
    public void StartCountdown(float time, string icon = "timerTimeTravel")
    {
        countdownRemaining = Mathf.Max(0f, time);
        countdownIcon = string.IsNullOrEmpty(icon) ? "timerTimeTravel" : icon;
        countdownActive = true;
        UpdateCountdownDisplay();
    }

    public void StopCountdown()
    {
        countdownActive = false;
        countdownRemaining = 0f;
        GameUIManager.instance?.SetCountdownTimer(-1f, countdownIcon);
    }

    private void UpdateCountdown()
    {
        if (!countdownActive || isPaused) return;
        countdownRemaining -= Time.deltaTime;
        if (countdownRemaining <= 0f) { countdownRemaining = 0f; UpdateCountdownDisplay(); countdownActive = false; return; }
        UpdateCountdownDisplay();
    }

    private void UpdateCountdownDisplay() => GameUIManager.instance?.SetCountdownTimer(countdownRemaining * 1000f, countdownIcon);
    #endregion

    private void OutOfBounds()
    {
        IsOutOfBounds = true;
        if (!ReplayRecorder.loadReplay)
        {
            IncrementOutOfBoundsCount();
            TotalTimeTracker.instance?.RecordOutOfBounds();
        }

        GameUIManager guim = GameUIManager.instance;

        if (guim == null)
            guim = GameObject.FindObjectOfType<GameUIManager>(true);

        guim.SetPowerupLocked(true);
        guim.SetCenterImage(3);
        PlayOutOfBoundsAudio();
        CameraController.instance.LockCamera(false);
        CameraController.instance.EnterTwoDOutOfBounds();
        CancelInvoke();

        if (GetGameMode<HuntMode>() != null) huntRestartRequested = false;
        if (!ReplayRecorder.loadReplay) Invoke(nameof(InvokeRespawn), 2f);
    }

    public void IncrementOutOfBoundsCount()
    {
        int oobCount = PlayerPrefs.GetInt("OutOfBoundsCount", 0) + 1;
        PlayerPrefs.SetInt("OutOfBoundsCount", oobCount);

        for (int i = 0; i < specialThresholds.Length; i++)
        {
            if (oobCount == specialThresholds[i]) { GameUIManager.instance.SetOutOfBoundsMessage(oobCount, oobSpecial[i]); return; }
        }

        if (oobCount != 0 && oobCount % 200 == 0) GameUIManager.instance.SetOutOfBoundsMessage(oobCount, oobRandom[UnityEngine.Random.Range(0, oobRandom.Length)]);
    }

    public void InvokeRespawn()
    {
        if (GetGameMode<HuntMode>() != null)
        {
            SpawnTrigger closest = null;
            float closestSqrDistance = Mathf.Infinity;
            Vector3 referencePosition = Marble.instance.transform.position;

            CheckCollision collision = Marble.instance.GetComponent<CheckCollision>();
            if (collision != null && collision.HasLastContactPosition) referencePosition = collision.LastContactPosition;

            if (spawnTriggers != null)
            {
                foreach (SpawnTrigger trigger in spawnTriggers)
                {
                    if (trigger == null) continue;
                    float sqrDistance = (trigger.transform.position - referencePosition).sqrMagnitude;
                    if (sqrDistance < closestSqrDistance) { closestSqrDistance = sqrDistance; closest = trigger; }
                }
            }

            if (closest != null)
            {
                Transform spawnPoint = closest.transform.Find("Spawn");
                SetActiveCheckpoint(spawnPoint != null ? spawnPoint : closest.transform, -closest.transform.forward);
            }
        }
        Marble.onRespawn?.Invoke();
    }

    public IEnumerator ResetSpawnAudio()
    {
        yield return new WaitForSeconds(0.1f);
        spawnAudioPlayed = false;
    }

    public void RestartLevel()
    {
        if (isPaused) TogglePause();
        if (GetGameMode<HuntMode>() != null) huntRestartRequested = true;
        ResetCheckpointState();
        Marble.onRespawn?.Invoke();
    }

    public void ReplayLevel()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GameUIManager.instance.saveReplayMenu.SetActive(false);
        if (GetGameMode<HuntMode>() != null) huntRestartRequested = true;
        finishMenu.SetActive(false);
        ResetCheckpointState();
        Marble.onRespawn?.Invoke();
    }

    private void ResetCheckpointState()
    {
        if (GetGameMode<HuntMode>() != null && activeCheckpoint != startPad)
        {
            InitializeHuntCheckpoint();
        }
        else
        {
            activeCheckpoint = startPad.transform.Find("Spawn"); 
            activeCheckpointGravityDir = Vector3.down; 
            useCheckpoint = false;
        }
    }

    public void ReachCheckpoint(Transform checkpoint, Vector3 checkpointGravityDir)
    {
        if (checkpoint == activeCheckpoint) return;
        useCheckpoint = true;
        GameUIManager.instance.SetBottomText("Checkpoint reached!");
        SetActiveCheckpoint(checkpoint, checkpointGravityDir);
        tempPowerup = activePowerup;
        Marble.instance.SavePowerupCheckpoint();
        PlayAudioClip(checkpointSfx);
        recentGems.Clear();
        ForEachGameMode(mode => mode.OnCheckpointReached());
    }

    public void SetActiveCheckpoint(Transform checkpoint, Vector3 checkpointGravityDir)
    {
        activeCheckpoint = checkpoint;
        activeCheckpointGravityDir = checkpointGravityDir;
    }

    public void Respawn()
    {
        if (!spawnAudioPlayed) 
        { 
            PlaySpawnAudio(); 
            spawnAudioPlayed = true; 
            StartCoroutine(ResetSpawnAudio()); 
        }
        CancelInvoke();

        gameFinish = IsOutOfBounds = false;
        Movement.instance.freezeMovement = false;
        CameraController.instance?.LockCamera(true);
        CameraController.instance.ExitTwoDOutOfBounds();
        StopCountdown();

        ResetAll<CountdownStartTrigger>(t => t.ResetTrigger());
        HuntMode huntMode = GetGameMode<HuntMode>();
        bool fullReset = huntMode != null ? huntRestartRequested : !useCheckpoint;

        if (huntMode != null) 
            WasFullReset = fullReset;

        TotalTimeTracker.instance?.RecordRespawn();

        if (fullReset)
        {
            Marble.instance.InactivateTimeTravel();
            ForEachGameMode(mode => mode.OnRestart());
            specialGameMode?.OnRestart();
            FullReset();
        }
        else
        {
            if (huntMode == null) 
                Marble.instance.InactivateTimeTravel();

            ForEachGameMode(mode => mode.OnRespawn());
            specialGameMode?.OnRespawn();
            CheckpointReset();
        }

        huntRestartRequested = false;
        recentGems.Clear();
        WasFullReset = false;

        Marble.instance.RevertMaterial();
        Marble.instance.ToggleGyrocopterBlades(false);
        if (gyrocopterIsActive) Marble.instance.CancelGyrocopter();
    }

    private void FullReset()
    {
        if (GetGameMode<HuntMode>() == null) GravityModifier.ResetGravityGlobal(Vector3.down);
        else GravityModifier.ResetGravityGlobal(activeCheckpointGravityDir);

        Movement.instance.StopAllMovement();
        Movement.instance.StopAllbutJumping();
        Movement.instance.ResetMovementTriggerCount();
        timeStopTriggerCount = 0;

        ResetAll<IceShard>(ice => ice.ResetShard());
        ResetAll<FadePlatform>(fp => fp.ResetPlatform());
        ResetAll<PushButton>(pb => pb.OnMissionReset());
        ResetAll<PathMover>(pm => pm.ResetMover());
        ResetAll<PathTrigger>(pt => pt.ResetTrigger());
        ResetAll<MovingPlatform>(mp => mp.ResetMP());
        ResetAll<CameraDistanceTrigger>(cdt => cdt.ResetTrigger());
        ResetAll<GravityPointTrigger>(gpt => gpt.ResetTrigger());
        ResetAll<RepetitiveTriggerGotoTarget>(rt => rt.ResetTrigger());
        ResetAll<Teleport>(tp => tp.ResetTeleporter());
        ResetAll<Teleporter>(tp => tp.ResetTeleporter());
        ResetAll<SoundTrigger>(st => st.ResetTrigger());

        Marble.instance.nextFireBlastTime = Mathf.NegativeInfinity;
        alarmIsPlaying = alarmCoroutineStarted = false;
        if (alarmCoroutine != null) StopCoroutine(alarmCoroutine);
        Marble.instance.alarmSound.Stop();

        HuntMode huntMode = GetGameMode<HuntMode>();
        huntTimeRemaining = (huntMode != null) ? (MissionInfo.instance.time > 0 ? MissionInfo.instance.time : 300000f) : 0f;
        GameUIManager.instance.SetTimerText(huntMode != null ? huntTimeRemaining : (GetGameMode<MadnessMode>() != null ? MissionInfo.instance.time : 0f), true);

        if (ReplayRecorder.loadReplay) { ReplayRecorder.Instance.StartReplay(ReplayRecorder.loadedReplayPath); Debug.Log("Replay Loaded"); }
        else if (ReplayRecorder.recordReplay || ReplayRecorder.leaderboardRecording) { ReplayRecorder.Instance.StartRecording(); Debug.Log("Replay Started"); }

        GameStateStart();
    }

    private void CheckpointReset()
    {
        if (!ReplayRecorder.loadReplay)
        {
            ReplayRecorder.Instance?.RecordRespawn();
        }

        Movement.instance.StopAllMovement();
        Movement.instance.StartMoving();

        if (GetGameMode<HuntMode>() == null)
        {
            foreach (GameObject gem in recentGems) { gem.SetActive(true); currentGems--; }
            GameUIManager.instance.SetCurrentGem(currentGems);
            activePowerup = tempPowerup;
            GameUIManager.instance.SetPowerupIcon(activePowerup);
            Marble.instance.RestorePowerupCheckpoint();
        }

        GameUIManager.instance.SetCenterImage(-1);
        Movement.instance.ResetMovementTriggerCount();
        timeStopTriggerCount = 0;

        ResetAll<CameraDistanceTrigger>(cdt => cdt.ResetTrigger());
        ResetAll<GravityPointTrigger>(gpt => gpt.ResetTrigger());
        ResetAll<Teleport>(tp => tp.ResetTeleporter());
        ResetAll<Teleporter>(tp => tp.ResetTeleporter());
    }

    private static void ResetAll<T>(Action<T> resetAction) where T : UnityEngine.Object
    {
        foreach (T item in FindObjectsOfType<T>(true)) resetAction(item);
    }

    private void GameStateStart()
    {
        gameStart = startTimer = false;
        UpdateGem(0);
        elapsedTime = bonusTime = 0;
        serverRating = null;

        HuntMode huntMode = GetGameMode<HuntMode>();
        huntTimeRemaining = (huntMode != null) ? (MissionInfo.instance.time > 0 ? MissionInfo.instance.time : 300000f) : 0f;

        if (huntMode == null) foreach (Gem gem in gems) gem.gameObject.SetActive(true);
        ConsumePowerup();

        foreach (Powerups po in FindObjectsOfType<Powerups>()) if (po.powerupType != PowerupType.EasterEgg) po.Activate(false);
        foreach (MovingPlatform mp in FindObjectsOfType<MovingPlatform>()) mp.ResetMP();

        GameUIManager.instance.SetTimerText(huntMode != null ? huntTimeRemaining : (GetGameMode<MadnessMode>() != null ? MissionInfo.instance.time : 0f), true);
        if (!string.IsNullOrEmpty(MissionInfo.instance.startHelpText)) GameUIManager.instance.SetCenterText(MissionInfo.instance.startHelpText);
        if (finishParticleInstance) Destroy(finishParticleInstance);

        GameUIManager.instance.SetCenterImage(-1);
        Invoke(nameof(GameStateReady), 0.5f);
    }

    private void GameStateReady() { PlayReadyAudio(); GameUIManager.instance.SetCenterImage(0); Invoke(nameof(GameStateSet), 1.5f); }
    private void GameStateSet() { PlaySetAudio(); GameUIManager.instance.SetCenterImage(1); Invoke(nameof(GameStateGo), 1.5f); }

    private void GameStateGo()
    {
        PlayGoAudio();
        startTimer = gameStart = true;
        if (GetGameMode<HuntMode>() != null) GameUIManager.instance.SetTimerText(huntTimeRemaining);

        GameUIManager.instance.SetCenterImage(2);
        Movement.instance.StartMoving();
        Invoke(nameof(ClearCenterImage), 2f);
    }

    private void ClearCenterImage() => GameUIManager.instance.SetCenterImage(-1);

    private void Finish()
    {
        for (int i = 0; i < gameModes.Count; i++)
        {
            if (!gameModes[i].CanFinish())
            {
                GameUIManager.instance.SetBottomText(gameModes[i].GetFinishMessage());
                PlayMissingGemAudio();
                return;
            }
        }

        if (!ReplayRecorder.loadReplay)
        {
            if (gameFinish)
                return;

            gameFinish = true;
            FinishRoutine();
            if (ReplayRecorder.recordReplay || ReplayRecorder.leaderboardRecording)
            {
                canPressEnter = false;
                Invoke(nameof(StopRecordingAfterFinish), 0.0625f + Time.fixedDeltaTime);
            }
            Invoke(nameof(ShowFinishUI), 2f);
        }
    }

    public void FinishRoutine()
    {
        CancelInvoke();
        PlayFinishAudio();
        startTimer = false;

        HuntMode huntMode = GetGameMode<HuntMode>();
        if (huntMode != null) huntTimeRemaining = 0f;

        GameUIManager.instance.SetBottomText("Congratulations! You've finished!");

        if (finishPad != null)
        {
            Transform finishParticle = finishPad.transform.Find("FinishParticle");
            if (finishParticle != null)
            {
                finishParticleInstance = Instantiate(finishParticles, finishParticle.position, Quaternion.identity);
                finishParticleInstance.transform.localScale = Vector3.one * 1.5f;
                finishParticleInstance.transform.rotation = finishPad.transform.rotation;
            }
        }

        Marble.instance.InactivateTimeTravel();
        GameUIManager.instance.SetTimerText(huntMode != null ? 0f : elapsedTime, true);
        CameraController.onCameraFinish?.Invoke();
        Invoke(nameof(StopMarbleMovement), 0.0625f);
    }

    private async void StopRecordingAfterFinish()
    {
        ReplayRecorder.incompleteReplay = false;
        ReplayRecorder.Instance.StopRecording();
        await SubmitOnlineScore();
    }

    private async UniTask SubmitOnlineScore()
    {
        if (OnlineManager.Instance?.Auth?.IsLoggedIn != true) { GameUIManager.instance.SetLBStatus(""); return; }

        canPressEnter = replayButton.interactable = continueButton.interactable = false;
        isSubmitInProgress = true;
        GameUIManager.instance.SetLBStatus("Submitting your score to Leaderboards...");
        string level = Path.ChangeExtension(MissionInfo.instance.MissionPath, null);

        try
        {
            SubmitScoreResponse? response = await OnlineManager.Instance.OnlineScore.SubmitScoreAsync(level, MissionInfo.instance.levelName);
            if (response != null)
            {
                serverRating = response.Rating;
                if (finishMenu.activeSelf) ratingText.text = response.Rating.ToString("N0");
            }
            else serverRating = null;
            GameUIManager.instance.SetLBStatus("Information sent successfully.");
        }
        catch (Exception ex)
        {
            serverRating = null;
            GameUIManager.instance.SetLBStatus("There was an error submitting the information.");
            Debug.LogException(ex);
        }
        finally
        {
            replayButton.interactable = continueButton.interactable = canPressEnter = true;
            isSubmitInProgress = false;
        }
    }

    private void StopMarbleMovement()
    {
        Movement.instance.freezeMovement = true;
        Movement.instance.FinishState();
    }

    #region UI Handlers
    public void ReturnToMenu()
    {
        if (ReplayRecorder.recordReplay)
        {
            pauseMenu.SetActive(false);
            finishMenu.SetActive(false);
            GameUIManager.instance.SaveAndReturn();
            GameUIManager.instance.saveReplayMenu.SetActive(true);
        }
        else if (ReplayRecorder.loadReplay) LoadSceneByAuth("ReplayMenu", "LBPlayMission");
        else
        {
            TotalTimeTracker.instance?.StopLevelTracking();

            if (OnlineManager.Instance?.Auth?.IsLoggedIn == true) 
                OnlineManager.Instance.Chat.SetStatus("Level Select").Forget();

            LoadSceneByAuth("PlayMission", "LBPlayMission");
        }
    }

    private static void LoadSceneByAuth(string offlineScene, string onlineScene)
    {
        if (OnlineManager.Instance == null || !OnlineManager.Instance.Auth.IsLoggedIn)
        {
            JukeboxManager.instance.PlayMusic("Pianoforte", true);
            SceneManager.LoadScene(offlineScene);
        }
        else
        {
            JukeboxManager.instance.PlayMusic("Flanked");
            SceneManager.LoadScene(onlineScene);
        }
    }

    public void ShowFinishUI()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        finishMenu.SetActive(true);
        GenerateFinishUIText();

        if (MissionInfo.instance.specialMissionMode == SpecialMissionMode.Vice)
            GameUIManager.instance.viceSaveWindow.SetActive(true);
    }

    public void UpdateName(string name)
    {
        bestTimeName = name ?? "";

        PlayerPrefs.SetString(
            "HighScoreName",
            bestTimeName
        );

        MissionInfo.instance.highScoreName = bestTimeName;
    }

    public void CloseEnterNameWindow()
    {
        enterNameMenu.SetActive(false);

        if (!isSubmitInProgress)
            replayButton.interactable = continueButton.interactable = true;

        if (pendingNamePosition >= 0 && pendingNamePosition < 5)
        {
            PlayerPrefs.SetString(
                $"{MissionInfo.instance.levelName}_Name_{pendingNamePosition}",
                bestTimeName ?? ""
            );

            PlayerPrefs.Save();

            pendingNamePosition = -1;
        }

        UpdateBestTimes();
    }

    private bool IsScoreBasedLeaderboard()
    {
        // Hunt is always score-based.
        if (ContainsMode(Mode.Hunt))
            return true;

        // Madness is score-based until all gems are collected.
        // Once all gems are collected, the completed result is ranked by time.
        if (ContainsMode(Mode.Madness))
        {
            MadnessMode madness = GetGameMode<MadnessMode>();

            return madness != null && !madness.GotAllGems;
        }

        return false;
    }

    private float GetLeaderboardValue()
    {
        // Hunt is always ranked by Points.
        if (ContainsMode(Mode.Hunt))
        {
            HuntMode hunt = GetGameMode<HuntMode>();

            return hunt != null
                ? hunt.Points
                : 0f;
        }

        // Madness:
        // Before collecting all gems -> Score.
        // After collecting all gems -> completion time.
        if (ContainsMode(Mode.Madness))
        {
            MadnessMode madness = GetGameMode<MadnessMode>();

            if (madness != null && !madness.GotAllGems)
                return madness.Score;

            return elapsedTime;
        }

        // Other modes are time-based.
        return elapsedTime;
    }

    private int DeterminePosition(float value)
    {
        bool isHunt = ContainsMode(Mode.Hunt);
        bool isMadness = ContainsMode(Mode.Madness);

        // =========================================================
        // HUNT
        // =========================================================
        //
        // Hunt is always score-based.
        // Higher score is better.
        //

        if (isHunt)
        {
            for (int i = 0; i < 10; i++)
            {
                float existingValue = PlayerPrefs.GetFloat(
                    $"{MissionInfo.instance.levelName}_Time_{i}",
                    -1
                );

                if (existingValue == -1)
                    return i;

                if (value > existingValue)
                    return i;
            }

            return -1;
        }

        // =========================================================
        // MADNESS
        // =========================================================
        //
        // Madness has two possible result types:
        //
        // < 1000  = Score
        // >= 1000 = Time
        //
        // A completed all-gems run is a TIME result.
        // An incomplete run is a SCORE result.
        //
        // Time results rank ahead of score results.
        //

        if (isMadness)
        {
            bool currentIsTime =
                value >= 1000f;

            for (int i = 0; i < 10; i++)
            {
                float existingValue = PlayerPrefs.GetFloat(
                    $"{MissionInfo.instance.levelName}_Time_{i}",
                    -1
                );

                if (existingValue == -1)
                    return i;

                bool existingIsTime =
                    existingValue >= 1000f;

                // -------------------------------------------------
                // Current result is a TIME.
                // -------------------------------------------------

                if (currentIsTime)
                {
                    // Any existing SCORE is worse than a completed
                    // all-gems TIME result.
                    if (!existingIsTime)
                        return i;

                    // Both are times.
                    // Lower time is better.
                    if (value < existingValue)
                        return i;
                }

                // -------------------------------------------------
                // Current result is a SCORE.
                // -------------------------------------------------

                else
                {
                    // Existing completed TIME results stay ahead
                    // of score results.
                    if (existingIsTime)
                        continue;

                    // Both are scores.
                    // Higher score is better.
                    if (value > existingValue)
                        return i;
                }
            }

            return -1;
        }

        // =========================================================
        // NORMAL TIME-BASED MODES
        // =========================================================
        //
        // Lower time is better.
        //

        for (int i = 0; i < 10; i++)
        {
            float existingValue = PlayerPrefs.GetFloat(
                $"{MissionInfo.instance.levelName}_Time_{i}",
                -1
            );

            if (existingValue == -1)
                return i;

            if (value < existingValue)
                return i;
        }

        return -1;
    }

    private void SaveTimeToTop10(float newValue, int position)
    {
        string levelName = MissionInfo.instance.levelName;

        for (int i = 9; i > position; i--)
        {
            string previousValueKey =
                $"{levelName}_Time_{i - 1}";

            string currentValueKey =
                $"{levelName}_Time_{i}";

            string previousNameKey =
                $"{levelName}_Name_{i - 1}";

            string currentNameKey =
                $"{levelName}_Name_{i}";

            float previousValue = PlayerPrefs.GetFloat(
                previousValueKey,
                -1
            );

            PlayerPrefs.SetFloat(
                currentValueKey,
                previousValue
            );

            // Only copy the name if the previous leaderboard
            // slot actually contains a saved value.
            if (previousValue != -1)
            {
                string previousName = PlayerPrefs.GetString(
                    previousNameKey,
                    ""
                );

                PlayerPrefs.SetString(
                    currentNameKey,
                    previousName
                );
            }
            else
            {
                // Remove the name key for an unassigned slot.
                if (PlayerPrefs.HasKey(currentNameKey))
                    PlayerPrefs.DeleteKey(currentNameKey);
            }
        }

        // Insert the new leaderboard value.
        PlayerPrefs.SetFloat(
            $"{levelName}_Time_{position}",
            newValue
        );

        // The name is filled in by the name-entry window.
        // An empty string is a valid submitted name.
        PlayerPrefs.SetString(
            $"{levelName}_Name_{position}",
            ""
        );

        PlayerPrefs.Save();
    }

    private void UpdateBestTimes()
    {
        numbersCaption.text =
            "<color=#EEC884>1.</color>\n" +
            "<color=#CDCDCD>2.</color>\n" +
            "<color=#C9AFA0>3.</color>\n" +
            "<color=#A4A4A4>4.</color>\n" +
            "<color=#949494>5.</color>";

        namesCaption.text = string.Empty;
        timesCaption.text = string.Empty;

        bool isHunt = ContainsMode(Mode.Hunt);
        bool isMadness = ContainsMode(Mode.Madness);

        bool scoreBased = IsScoreBasedLeaderboard();

        for (int i = 0; i < 5; i++)
        {
            string valueKey =
                $"{MissionInfo.instance.levelName}_Time_{i}";

            string nameKey =
                $"{MissionInfo.instance.levelName}_Name_{i}";

            float value = PlayerPrefs.GetFloat(
                valueKey,
                -1
            );

            // -----------------------------------------------------
            // Empty / unassigned leaderboard slot
            // -----------------------------------------------------

            if (value == -1)
            {
                namesCaption.text += "Matan W.\n";

                if (scoreBased)
                    timesCaption.text += "0\n";
                else
                    timesCaption.text += "99:59.999\n";

                continue;
            }

            // -----------------------------------------------------
            // Assigned leaderboard slot
            // -----------------------------------------------------

            string name;

            if (PlayerPrefs.HasKey(nameKey))
            {
                name = PlayerPrefs.GetString(nameKey);
            }
            else
            {
                name = "Matan W.";
            }

            namesCaption.text += $"{name}\n";

            // =====================================================
            // HUNT
            // =====================================================

            if (isHunt)
            {
                // Hunt is always score-based.
                // Higher score is better.
                //
                // A threshold of -1 means the medal does not exist.

                bool awesome =
                    MissionInfo.instance.awesomeTime != -1 &&
                    value >= MissionInfo.instance.awesomeTime;

                bool ultimate =
                    MissionInfo.instance.ultimateTime != -1 &&
                    value >= MissionInfo.instance.ultimateTime;

                bool platinum =
                    MissionInfo.instance.platinumTime != -1 &&
                    value >= MissionInfo.instance.platinumTime;

                if (awesome)
                {
                    timesCaption.text +=
                        $"<color=#FF3333>{Mathf.RoundToInt(value)}</color>\n";
                }
                else if (ultimate)
                {
                    timesCaption.text +=
                        $"<color=#FFCC33>{Mathf.RoundToInt(value)}</color>\n";
                }
                else if (platinum)
                {
                    timesCaption.text +=
                        $"<color=#CCCCCC>{Mathf.RoundToInt(value)}</color>\n";
                }
                else
                {
                    timesCaption.text +=
                        $"{Mathf.RoundToInt(value)}\n";
                }

                continue;
            }

            // =====================================================
            // MADNESS
            // =====================================================

            if (isMadness)
            {
                // < 1000 = score
                // >= 1000 = time

                bool storedAsScore = value < 1000f;

                // -------------------------------------------------
                // Stored score
                // -------------------------------------------------

                if (storedAsScore)
                {
                    bool awesome =
                        MissionInfo.instance.awesomeTime != -1 &&
                        MissionInfo.instance.awesomeTime < 1000f &&
                        value >= MissionInfo.instance.awesomeTime;

                    bool ultimate =
                        MissionInfo.instance.ultimateTime != -1 &&
                        MissionInfo.instance.ultimateTime < 1000f &&
                        value >= MissionInfo.instance.ultimateTime;

                    bool platinum =
                        MissionInfo.instance.platinumTime != -1 &&
                        MissionInfo.instance.platinumTime < 1000f &&
                        value >= MissionInfo.instance.platinumTime;

                    if (awesome)
                    {
                        timesCaption.text +=
                            $"<color=#FF3333>{Mathf.RoundToInt(value)}</color>\n";
                    }
                    else if (ultimate)
                    {
                        timesCaption.text +=
                            $"<color=#FFCC33>{Mathf.RoundToInt(value)}</color>\n";
                    }
                    else if (platinum)
                    {
                        timesCaption.text +=
                            $"<color=#CCCCCC>{Mathf.RoundToInt(value)}</color>\n";
                    }
                    else
                    {
                        timesCaption.text +=
                            $"{Mathf.RoundToInt(value)}\n";
                    }

                    continue;
                }

                // -------------------------------------------------
                // Stored time
                // -------------------------------------------------

                bool awesomeTime =
                    MissionInfo.instance.awesomeTime != -1 &&
                    MissionInfo.instance.awesomeTime >= 1000f &&
                    value < MissionInfo.instance.awesomeTime;

                bool ultimateTime =
                    MissionInfo.instance.ultimateTime != -1 &&
                    MissionInfo.instance.ultimateTime >= 1000f &&
                    value < MissionInfo.instance.ultimateTime;

                bool platinumTime =
                    MissionInfo.instance.platinumTime != -1 &&
                    MissionInfo.instance.platinumTime >= 1000f &&
                    value < MissionInfo.instance.platinumTime;

                if (awesomeTime)
                {
                    timesCaption.text +=
                        $"<color=#FF3333>{Utils.FormatTime(value)}</color>\n";
                }
                else if (ultimateTime)
                {
                    timesCaption.text +=
                        $"<color=#FFCC33>{Utils.FormatTime(value)}</color>\n";
                }
                else if (platinumTime)
                {
                    timesCaption.text +=
                        $"<color=#CCCCCC>{Utils.FormatTime(value)}</color>\n";
                }
                else
                {
                    timesCaption.text +=
                        $"{Utils.FormatTime(value)}\n";
                }

                continue;
            }

            // =====================================================
            // NORMAL TIME-BASED MODES
            // =====================================================

            bool normalAwesome =
                MissionInfo.instance.awesomeTime != -1 &&
                value < MissionInfo.instance.awesomeTime;

            bool normalUltimate =
                MissionInfo.instance.ultimateTime != -1 &&
                value < MissionInfo.instance.ultimateTime;

            bool normalPlatinum =
                MissionInfo.instance.platinumTime != -1 &&
                value < MissionInfo.instance.platinumTime;

            if (normalAwesome)
            {
                timesCaption.text +=
                    $"<color=#FF3333>{Utils.FormatTime(value)}</color>\n";
            }
            else if (normalUltimate)
            {
                timesCaption.text +=
                    $"<color=#FFCC33>{Utils.FormatTime(value)}</color>\n";
            }
            else if (normalPlatinum)
            {
                timesCaption.text +=
                    $"<color=#CCCCCC>{Utils.FormatTime(value)}</color>\n";
            }
            else
            {
                timesCaption.text +=
                    $"{Utils.FormatTime(value)}\n";
            }
        }
    }


    public void GenerateFinishUIText()
    {
        if (!isSubmitInProgress)
            replayButton.interactable = continueButton.interactable = true;

        bool isHunt = ContainsMode(Mode.Hunt);
        bool isMadness = ContainsMode(Mode.Madness);

        MadnessMode madnessMode = isMadness
            ? GetGameMode<MadnessMode>()
            : null;

        bool gotAllGems =
            madnessMode != null &&
            madnessMode.GotAllGems;

        float leaderboardValue = GetLeaderboardValue();

        bool parPassed = false;
        bool platinum = false;
        bool ultimate = false;
        bool awesome = false;

        // ---------------------------------------------------------
        // HUNT
        // ---------------------------------------------------------
        //
        // Hunt:
        // parScore is ALWAYS the Par Score.
        // time is ONLY the time limit.
        //
        // Higher score is better.
        //
        if (isHunt)
        {
            parPassed =
                MissionInfo.instance.parScore <= 0 ||
                leaderboardValue >= MissionInfo.instance.parScore;

            platinum =
                MissionInfo.instance.platinumTime <= 0 ||
                leaderboardValue >= MissionInfo.instance.platinumTime;

            ultimate =
                MissionInfo.instance.ultimateTime <= 0 ||
                leaderboardValue >= MissionInfo.instance.ultimateTime;

            awesome =
                MissionInfo.instance.awesomeTime <= 0 ||
                leaderboardValue >= MissionInfo.instance.awesomeTime;
        }

        // ---------------------------------------------------------
        // MADNESS
        // ---------------------------------------------------------
        //
        // parScore:
        //
        // < 1000  = Par Score
        // >= 1000 = Par Time
        //
        // A time-based Par requires GotAllGems.
        //
        // Platinum / Ultimate / Awesome use the same
        // <1000 score / >=1000 time convention.
        //
        else if (isMadness)
        {
            bool parIsScore =
                MissionInfo.instance.parScore >= 0 &&
                MissionInfo.instance.parScore < 1000f;

            bool parIsTime =
                MissionInfo.instance.parScore >= 1000f;

            bool platinumIsScore =
                MissionInfo.instance.platinumTime >= 0 &&
                MissionInfo.instance.platinumTime < 1000f;

            bool ultimateIsScore =
                MissionInfo.instance.ultimateTime >= 0 &&
                MissionInfo.instance.ultimateTime < 1000f;

            bool awesomeIsScore =
                MissionInfo.instance.awesomeTime >= 0 &&
                MissionInfo.instance.awesomeTime < 1000f;

            // -------------------------
            // Par
            // -------------------------

            if (parIsScore)
            {
                // Par Score.
                parPassed =
                    leaderboardValue >= MissionInfo.instance.parScore;
            }
            else if (parIsTime)
            {
                // Par Time.
                // Must have collected all gems.
                parPassed =
                    gotAllGems &&
                    elapsedTime < MissionInfo.instance.parScore;
            }
            else
            {
                parPassed = false;
            }

            // -------------------------
            // Platinum
            // -------------------------

            if (MissionInfo.instance.platinumTime > 0)
            {
                if (platinumIsScore)
                {
                    platinum =
                        leaderboardValue >= MissionInfo.instance.platinumTime;
                }
                else
                {
                    platinum =
                        gotAllGems &&
                        elapsedTime < MissionInfo.instance.platinumTime;
                }
            }

            // -------------------------
            // Ultimate
            // -------------------------

            if (MissionInfo.instance.ultimateTime > 0)
            {
                if (ultimateIsScore)
                {
                    ultimate =
                        leaderboardValue >= MissionInfo.instance.ultimateTime;
                }
                else
                {
                    ultimate =
                        gotAllGems &&
                        elapsedTime < MissionInfo.instance.ultimateTime;
                }
            }

            // -------------------------
            // Awesome
            // -------------------------

            if (MissionInfo.instance.awesomeTime > 0)
            {
                if (awesomeIsScore)
                {
                    awesome =
                        leaderboardValue >= MissionInfo.instance.awesomeTime;
                }
                else
                {
                    awesome =
                        gotAllGems &&
                        elapsedTime < MissionInfo.instance.awesomeTime;
                }
            }
        }

        // ---------------------------------------------------------
        // NORMAL TIME MODES
        // ---------------------------------------------------------

        else
        {
            parPassed =
                MissionInfo.instance.time == -1 ||
                elapsedTime < MissionInfo.instance.time;

            platinum =
                MissionInfo.instance.platinumTime <= 0 ||
                elapsedTime < MissionInfo.instance.platinumTime;

            ultimate =
                MissionInfo.instance.ultimateTime <= 0 ||
                elapsedTime < MissionInfo.instance.ultimateTime;

            awesome =
                MissionInfo.instance.awesomeTime <= 0 ||
                elapsedTime < MissionInfo.instance.awesomeTime;
        }

        if (isHunt || (isMadness && !gotAllGems))
        {
            finalTime.text = leaderboardValue.ToString();
            finalTimeCaption.text = "Your Score:"; 
        }
        else
        {
            finalTime.text = Utils.FormatTime(elapsedTime);
            finalTimeCaption.text = "Your Time:";
        }

        bool isOnline =
            OnlineManager.Instance?.Auth?.IsLoggedIn == true &&
            !ReplayRecorder.loadReplay;

        ratingGameObject.SetActive(isOnline);

        if (isOnline)
        {
            ratingText.text = serverRating.HasValue
                ? serverRating.Value.ToString("N0")
                : "-";
        }

        int pos = DeterminePosition(leaderboardValue);

        pendingNamePosition = -1;

        // Only a run that beats Par can enter the leaderboard.
        if (pos != -1 && parPassed)
        {
            SaveTimeToTop10(leaderboardValue, pos);

            if (pos < 5)
            {
                pendingNamePosition = pos;

                replayButton.interactable = continueButton.interactable = false;
                enterNameMenu.SetActive(true);

                string leaderboardType =
                    IsScoreBasedLeaderboard()
                        ? "score"
                        : "time";

                enterNameCaption.text = pos switch
                {
                    0 => $"You got the top {leaderboardType}!",
                    1 => $"You got the second top {leaderboardType}!",
                    2 => $"You got the third top {leaderboardType}!",
                    3 => $"You got the fourth top {leaderboardType}!",
                    4 => $"You got the fifth top {leaderboardType}!",
                    _ => $"You got a top {leaderboardType}!"
                };

                nameInputField.SetTextWithoutNotify(
                    MissionInfo.instance.highScoreName
                );

                UpdateName(
                    MissionInfo.instance.highScoreName
                );
            }
        }

        // ---------------------------------------------------------
        // PAR DISPLAY
        // ---------------------------------------------------------

        if (isHunt)
        {
            // Hunt Par is always a score.
            if (MissionInfo.instance.parScore <= 0)
            {
                parTimeText.text = "-";
            }
            else if (!parPassed)
            {
                parTimeText.text =
                    $"<color=#F55555>{Mathf.RoundToInt(MissionInfo.instance.parScore)}</color>";
            }
            else
            {
                parTimeText.text =
                    Mathf.RoundToInt(MissionInfo.instance.parScore).ToString();
            }
        }
        else if (isMadness)
        {
            if (MissionInfo.instance.parScore <= 0)
            {
                parTimeText.text = "-";
            }
            else if (MissionInfo.instance.parScore < 1000f)
            {
                // Par Score.
                parTimeText.text =
                    !parPassed
                        ? $"<color=#F55555>{Mathf.RoundToInt(MissionInfo.instance.parScore)}</color>"
                        : Mathf.RoundToInt(MissionInfo.instance.parScore).ToString();
            }
            else
            {
                // Par Time.
                parTimeText.text =
                    !parPassed
                        ? $"<color=#F55555>{Utils.FormatTime(MissionInfo.instance.parScore)}</color>"
                        : Utils.FormatTime(MissionInfo.instance.parScore);
            }
        }
        else
        {
            // Other modes use MissionInfo.time as Par Time.
            parTimeText.text =
                !parPassed
                    ? $"<color=#F55555>{Utils.FormatTime(MissionInfo.instance.time)}</color>"
                    : Utils.FormatTime(MissionInfo.instance.time);
        }

        // ---------------------------------------------------------
        // THRESHOLD BOX TEXT
        // ---------------------------------------------------------

        if (isHunt)
        {
            platinumTimeBox.transform.Find("Text")
                .GetComponent<TextMeshProUGUI>()
                .text =
                $"<color=#CCCCCC>{Mathf.RoundToInt(MissionInfo.instance.platinumTime)}</color>";

            ultimateTimeBox.transform.Find("Text")
                .GetComponent<TextMeshProUGUI>()
                .text =
                $"<color=#FFCC33>{Mathf.RoundToInt(MissionInfo.instance.ultimateTime)}</color>";

            awesomeTimeBox.transform.Find("Text")
                .GetComponent<TextMeshProUGUI>()
                .text =
                $"<color=#FF3333>{Mathf.RoundToInt(MissionInfo.instance.awesomeTime)}</color>";
        }
        else if (isMadness)
        {
            bool platinumIsScore =
                MissionInfo.instance.platinumTime >= 0 &&
                MissionInfo.instance.platinumTime < 1000f;

            bool ultimateIsScore =
                MissionInfo.instance.ultimateTime >= 0 &&
                MissionInfo.instance.ultimateTime < 1000f;

            bool awesomeIsScore =
                MissionInfo.instance.awesomeTime >= 0 &&
                MissionInfo.instance.awesomeTime < 1000f;

            platinumTimeBox.transform.Find("Text")
                .GetComponent<TextMeshProUGUI>()
                .text = platinumIsScore
                    ? $"<color=#CCCCCC>{Mathf.RoundToInt(MissionInfo.instance.platinumTime)}</color>"
                    : $"<color=#CCCCCC>{Utils.FormatTime(MissionInfo.instance.platinumTime)}</color>";

            ultimateTimeBox.transform.Find("Text")
                .GetComponent<TextMeshProUGUI>()
                .text = ultimateIsScore
                    ? $"<color=#FFCC33>{Mathf.RoundToInt(MissionInfo.instance.ultimateTime)}</color>"
                    : $"<color=#FFCC33>{Utils.FormatTime(MissionInfo.instance.ultimateTime)}</color>";

            awesomeTimeBox.transform.Find("Text")
                .GetComponent<TextMeshProUGUI>()
                .text = awesomeIsScore
                    ? $"<color=#FF3333>{Mathf.RoundToInt(MissionInfo.instance.awesomeTime)}</color>"
                    : $"<color=#FF3333>{Utils.FormatTime(MissionInfo.instance.awesomeTime)}</color>";
        }
        else
        {
            platinumTimeBox.transform.Find("Text")
                .GetComponent<TextMeshProUGUI>()
                .text =
                $"<color=#CCCCCC>{Utils.FormatTime(MissionInfo.instance.platinumTime)}</color>";

            ultimateTimeBox.transform.Find("Text")
                .GetComponent<TextMeshProUGUI>()
                .text =
                $"<color=#FFCC33>{Utils.FormatTime(MissionInfo.instance.ultimateTime)}</color>";

            awesomeTimeBox.transform.Find("Text")
                .GetComponent<TextMeshProUGUI>()
                .text =
                $"<color=#FF3333>{Utils.FormatTime(MissionInfo.instance.awesomeTime)}</color>";
        }

        timePassedText.text =
            Utils.FormatTime(elapsedTime + bonusTime);

        clockBonusesText.text =
            Utils.FormatTime(bonusTime);

        // Keep the existing visibility behavior:
        // Platinum and Ultimate stay visible.
        // Awesome is only shown when actually beaten.
        platinumTimeBox.SetActive(true);
        ultimateTimeBox.SetActive(true);

        bool showAwesomeBox = awesome || HasAwesomeHighScore();
        awesomeTimeBox.SetActive(showAwesomeBox);

        // ---------------------------------------------------------
        // FINISH CAPTION
        // ---------------------------------------------------------

        if (isHunt)
        {
            if (!parPassed)
            {
                finishCaption.text =
                    "<color=#F55555>You didn't beat the Par Score!</color>";
            }
            else if (awesome)
            {
                finishCaption.text =
                    "Who's Awesome <color=#FF3333>You're</color> Awesome!";
            }
            else if (ultimate)
            {
                finishCaption.text =
                    "You beat the <color=#FFCC33>Ultimate</color> Score!";
            }
            else if (platinum)
            {
                finishCaption.text =
                    "You beat the <color=#CCCCCC>Platinum</color> Score!";
            }
            else
            {
                finishCaption.text =
                    "You beat the Par Score!";
            }
        }
        else if (isMadness)
        {
            if (!parPassed)
            {
                finishCaption.text =
                    "<color=#F55555>You didn't beat the Par!</color>";
            }
            else if (awesome)
            {
                bool awesomeIsScore =
                    MissionInfo.instance.awesomeTime < 1000f;

                finishCaption.text =
                    "Who's Awesome <color=#FF3333>You're</color> Awesome!";
            }
            else if (ultimate)
            {
                bool ultimateIsScore =
                    MissionInfo.instance.ultimateTime < 1000f;

                finishCaption.text =
                    ultimateIsScore
                        ? "You beat an <color=#FFCC33>Ultimate</color> Score!"
                        : "You beat an <color=#FFCC33>Ultimate</color> Time!";
            }
            else if (platinum)
            {
                bool platinumIsScore =
                    MissionInfo.instance.platinumTime < 1000f;

                finishCaption.text =
                    platinumIsScore
                        ? "You beat a <color=#CCCCCC>Platinum</color> Score!"
                        : "You beat a <color=#CCCCCC>Platinum</color> Time!";
            }
            else
            {
                bool parIsScore =
                    MissionInfo.instance.parScore >= 0 &&
                    MissionInfo.instance.parScore < 1000f;

                finishCaption.text =
                    parIsScore
                        ? "You beat the Par Score!"
                        : "You beat the Par Time";
            }
        }
        else
        {
            if (awesome)
            {
                finishCaption.text =
                    "Who's Awesome <color=#FF3333>You're</color> Awesome!";
            }
            else if (ultimate)
            {
                finishCaption.text =
                    "You beat the <color=#FFCC33>Ultimate</color> Time!";
            }
            else if (platinum)
            {
                finishCaption.text =
                    "You beat the <color=#CCCCCC>Platinum</color> Time!";
            }
            else if (parPassed)
            {
                finishCaption.text =
                    "You beat the Par Time";
            }
            else
            {
                finishCaption.text =
                    "<color=#F55555>You didn't pass the Par Time!</color>";
            }
        }

        UpdateBestTimes();

        string keySuffix =
            $"{CapitalizeFirst(PlayMissionManager.currentlySelectedType.ToString())}";

        int qualifiedLevel =
            PlayerPrefs.GetInt(
                $"QualifiedLevel{keySuffix}",
                0
            );

        if (parPassed &&
            qualifiedLevel + 1 == MissionInfo.instance.levelNumber)
        {
            PlayerPrefs.SetInt(
                $"QualifiedLevel{keySuffix}",
                qualifiedLevel + 1
            );
        }

        PlayerPrefs.SetInt(
            $"SelectedLevel{keySuffix}",
            MissionInfo.instance.levelNumber
        );
    }

    private bool HasAwesomeHighScore()
    {
        string levelName = MissionInfo.instance.levelName;

        bool isHunt = ContainsMode(Mode.Hunt);
        bool isMadness = ContainsMode(Mode.Madness);

        for (int i = 0; i < 5; i++)
        {
            float value = PlayerPrefs.GetFloat(
                $"{levelName}_Time_{i}",
                -1
            );

            // No leaderboard entry.
            if (value == -1)
                continue;

            // =====================================================
            // HUNT
            // =====================================================

            if (isHunt)
            {
                // Hunt is always score-based.
                if (MissionInfo.instance.awesomeTime > 0 &&
                    value >= MissionInfo.instance.awesomeTime)
                {
                    return true;
                }

                continue;
            }

            // =====================================================
            // MADNESS
            // =====================================================

            if (isMadness)
            {
                // < 1000 = score
                // >= 1000 = time

                if (value < 1000f)
                {
                    // Stored score can only beat an Awesome Score.
                    if (MissionInfo.instance.awesomeTime > 0 &&
                        MissionInfo.instance.awesomeTime < 1000f &&
                        value >= MissionInfo.instance.awesomeTime)
                    {
                        return true;
                    }
                }
                else
                {
                    // Stored time can only beat an Awesome Time.
                    if (MissionInfo.instance.awesomeTime >= 1000f &&
                        value < MissionInfo.instance.awesomeTime)
                    {
                        return true;
                    }
                }

                continue;
            }

            // =====================================================
            // NORMAL TIME-BASED MODES
            // =====================================================

            if (MissionInfo.instance.awesomeTime > 0 &&
                value < MissionInfo.instance.awesomeTime)
            {
                return true;
            }
        }

        return false;
    }

    public string CapitalizeFirst(string input) => string.IsNullOrEmpty(input) ? input : char.ToUpper(input[0]) + input.Substring(1);
    #endregion

    private void InitializeOutOfBoundsInsult()
    {
        specialThresholds = new[] { 1250, 2500, 3750, 5000, 6250, 7500, 8750, 10000, 50000, 300000, 1000000, 30000000 };
        oobRandom = new[] {
            "Let's be clear of the blatant truth: You suck!", "Honestly, do you have any control over the marble? It seems to have a life on its own...",
            "Are you sure you know how to play Marble Blast?", "You are contributing to the increasing water levels in the sea below you way too much!",
            "Look at the bright side, it's part of the learning experience, but it doesn't change the fact that you still suck.",
            "If we ever had a 'You suck' achievement, you'd be having the honour to wear it today.",
            "200 more times to go Out of Bounds before you see this message again. For your sake, try and do better.",
            "\"I didn't play on the computer! It...it was.. my auntie!\" Yeah, right. Admit it, you suck.",
            "Are you having fun going Out of Bounds all the time? It seriously looks like it.",
            "Don't you just hate all these messages that make a mockery of your suckiness? It's a joke of course, but it's a nice easter egg.\nIf you don't want to see them anymore, then stop going Out of Bounds so many times!",
            "My grandmother is better than you!", "We'll see what happens first: You finishing the level, or the clock hitting the 100 minute mark.",
            "Can we put this on the video show? I mean, that was absolutely stupid of you to go Out of Bounds like that!",
            "While we're on the subject of you going Out of Bounds, you should try and find out all the possible ways to go Out of Bounds, including the stupid ways which you seem to excel in.",
            "This level isn't made out completely out of tiny thin tightropes! You have no excuse whatsoever on failing this badly. If you see this message on Tightropes, Catwalks or Slopwropes, ignore it. Instead, change it to: hahahahahahahahahaha fail!",
            "Excuse of the Day: \"I was pushed Out of Bounds by an invisible Mega Marble!\"",
            "Congratulations, you win--- wait, no, no you don't. You went Out of Bounds. Sorry, you lose. Again.",
            "I found a way for you not to go Out of Bounds. We'll change the shape of the marble to a cube. Wait, never mind, you'll still find a way, because you can.",
            "You sure you played the beginner levels? You did? Doesn't look like it.",
            "You know what would be hilarious? This message popping up on 'Let's Roll'. I hope you aren't playing that level right now... are you?",
            "Mind if we'll change your name to 'Mr. McFail?'",
            "Excuse of the Day: \"But I was distracted by ________ and he/she/it wouldn't stop and forced me to go Out of Bounds.\"",
            "Which one are you: a bad player or a bad player? We willl go with option C: a really bad player.",
            "Excuse of the Day: WHO PUT THAT GRAVITY MODIFIER IN THERE??!?!",
            "Excuse of the Day: That In Bounds Trigger WAS NOT in the level last time I played it! Somebody hacked the level and put one in there!",
            "Excuse of the Day: My awesome marble was abducted by aliens and was replaced by a really crap one!",
            "Excuse of the Day: That Out of Bounds trigger was NOT there before! I swear!", "Excuse of the Day: I'm not Pascal :(",
            "Excuse of the Day: I don't suck, I fell off because I wanted to get to the next 200 Out of Bounds multiplier so I can see the awesome messages that are written down.",
            "You know, you won't beat the level if you keep falling off. You will, however, see more of these messages. Try and stay on the level next time. Our guess is that you can't, because you're bad.",
            "Look at the statistics page! I bet you fell more times than the amount of levels you beat!", "Excuse of the Day: I'm learning to play... the hard way.",
            "Apparently your marble isn't supermarble. It is suckmarble.", "Foo-Foo Marble laughs at how bad you are.", "A Rock Can Do Better!",
            "Please, Quit Embarassing Yourself.", "Keep this up and you'll win the 'Award of LOL', courtesy of Marble Blast Fubar creators!",
            "Marble Blast Fubar creators would like to give you the title of 'Official NOOB of the Year'. Congratulations!",
            "Did you hear that 'Practice Makes Perfect'? Apparently not.",
            "You should create a new level and title it 'Learn the In Bounds and Out of Bounds Triggers' because you're so experienced with them.",
            "We've seen the ways you fell while playing this game and we gotta admit, some of their are epic fails. We still can't stop laughing!",
            "SING WITH ME:\n\nOne hundred and ninety nine times Out of Bounds, one hundred and ninety nine times Out of Bounds, throw the marble off the level, two hundred times Out of Bounds!",
            "*sigh*, you just can't stop yourself from going Out of Bounds, can you?",
            "Excuse of the Day: I'm playing one of those special levels from Technostick where you must fall off in order to beat them.",
            "Excuse of the Day: I'm having a bad karma today.", "Excuse of the Day: So THAT'S what my astronomer referred to when he said I'll keep falling off today.",
            "What do you have against the marble that you keep making it fall off the level?!",
            "I bet you wish you had a Blast or an Ultra Blast powerup to save you. Perhaps even the World's Greatest Blast. Well, reality to player, reality to player: we don't have such a thing existing in this game, so stop playing so badly!",
            "And how is it OUR fault that you're playing so badly?", "Do you ever think about the marble's safety when you're playing? Apparently not because you're really careless with it."
        };

        oobSpecial = new[] {
            "You went Out of Bounds for 1,250 times. This program will now sit in the corner and cry about how bad you are and hope that when you open it again you won't repeat it. False hopes are still hopes.",
            "You went Out of Bounds for 2,500 times. If you aren't tired of going Out of Bounds all the time, we sure did. Stop it already!",
            "Another 1,250 marbles had fallen to the great sea below, and you've reached the 3,750 Out of Bounds mark. You definitely suck. Ah yes, greenpeace would like to see you in court for your \"contribution\" to rising sea levels.",
            "If I had a nickel for every marble that fell Out of Bounds I'd be rich right now and all thanks to you. However, I'm not going to give you any money. Instead, I'll stick my tongue out at you and then laugh at you. Ah yes, congratulations on hitting the 5,000 Out of Bounds mark.",
            "6,750 times Out of Bounds. Let's assume, hypothetically, that you won't go Out of Bounds ever again. Actually, never mind that, you will still suck even if you don't go Out of Bounds again.",
            "I have an awesome gut feeling that you are going 7,500 times Out of Bounds on purpose if only to see these messages and to hear about how bad you are.\nWell then, I won't keep it away from you.\nYou suck!",
            "8,750 times Out of Bounds. For reaching this landmark, I'm giving you a nice Australian Slang sentence to answer the question: Will you ever stop sucking in this game and go Out of Bounds? Answer:\nTill it rains in Marble Bar\n\n\nIn your language it means:\nNever.",
            "Wow, you truly are bad, probably one of the worst Marble Blast players to ever live on this planet. Or you just keep failing to good runs. Are you sure you aren't playing an easy level while this message pops up? Whatever, those messages will now repeat themselves (with a few exceptions), but for now, please remember this:\n\n\nYOU suck!",
            "SING WITH ME:\n\nForty nine thousand nine hundred and ninety nine times Out of Bounds, forty nine thousand nine hundred and ninety nine times Out of Bounds, knock a marble off the level, fifty thousand times Out of Bounds!",
            "What's that in the sky? Is it a plane? Is it a bird? No! It's the marble! And it's way off the level!!! Congratulations on hitting 300,000 Out of Bounds mark. You may now suck more.",
            "1,000,000 times Out of Bounds?!?! You seriously love this game, don't you? Well then, thanks for playing Marble Blast Platinum! Please keep this bad playing up and continue to go Out of Bounds. We'll just laugh at how bad you are. Also, this is the final message as from now on they're all repeats. Thank you for sucking at Marble Blast Platinum!",
            "You have no life. This is official."
        };
    }

    public void NotifySpecialGameModeJump() => specialGameMode?.OnJump();

    public T GetGameMode<T>() where T : class, IGameMode
    {
        foreach (IGameMode mode in gameModes)
            if (mode is T typedMode) return typedMode;
        return null;
    }

    public bool ContainsMode(Mode mode)
    {
        foreach (Mode m in MissionInfo.instance.gameModes)
            if (m == mode) return true;
        return false;
    }
}