using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using Server.Replay;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class ReplayRecorder : MonoBehaviour
{
    public static ReplayRecorder Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        returnToMenuTween?.Kill();
        returnToMenuTween = null;

        recordingBytes?.Clear();
        playbackBytes = null;

        if (Instance == this)
            Instance = null;
    }

    public const byte ReplayVersion = 1;

    //references
    GameManager gm;
    Movement mov;
    Marble mar;
    CameraController cc;
    GameUIManager gui;
    MissionInfo mi;
    MarbleInfo skin;

    //recording info
    public bool isRecording;
    List<byte> recordingBytes = new List<byte>();
    byte[] playbackBytes;

    ReplayFrame currentFrame;
    ReplayFrame nextFrame;

    int playhead;

    public bool isPlayingReplay;
    bool gameFinished = false;

    int teleportFinished = 0;

    public void RecordTeleportFinished()
    {
        teleportFinished = 1;
    }

    public static bool recordReplay = false;
    public static bool loadReplay = false;
    public static string replayName = string.Empty;
    public static string actualReplayName = string.Empty;
    public static string replayAuthor = string.Empty;
    public static string replayDesc = string.Empty;
    public static bool incompleteReplay = false;
    public static string loadedReplayPath = string.Empty;
    public static bool leaderboardRecording = false;

    private Tween returnToMenuTween;

    public bool HasReplay => currentFrame != null;

    //Bounce
    int bounce;
    float bounceStrength;
    Vector3 bouncePoint;
    Vector3 bounceNormal;

    //respawn
    int respawn = 0;

    public void RecordRespawn()
    {
        respawn = 1;
    }

    private void Start()
    {
        //cache marble info
        gm = GameManager.instance;
        mov = Movement.instance;
        mar = Marble.instance;
        cc = CameraController.instance;
        gui = GameUIManager.instance;
        mi = MissionInfo.instance;
        skin = MarbleInfo.instance;

        if (recordReplay || leaderboardRecording)
            incompleteReplay = false;
    }

    void FixedUpdate()
    {
        if (isRecording)
            RecordCurrentFrame();

        if (isPlayingReplay)
            UpdateReplay();
    }

    public void StartRecording()
    {
        ClearRecording();
        isRecording = true;
    }

    public void StopRecording()
    {
        isRecording = false;
    }

    public void ClearRecording()
    {
        recordingBytes.Clear();
    }

    public void RecordBounce(float strength, Vector3 point, Vector3 normal)
    {
        bounce = 1;
        bounceStrength = strength;
        bouncePoint = point;
        bounceNormal = normal;
    }

    public void RecordCurrentFrame()
    {
        ReplayFrame frame = new ReplayFrame(
            mar.transform.position,
            mar.transform.rotation,
            mar.transform.localScale,
            cc.GetOffset(),
            mov.marbleVelocity,
            mov.marbleAngularVelocity,
            bounce,
            bounceStrength,
            bouncePoint,
            bounceNormal,
            ConvertPowerupString(gm.activePowerup),
            gm.elapsedTime,
            mov.contactPct,
            mov.slipAmount,
            mov.justJumped,
            gm.currentGems,
            respawn,
            GravitySystem.GravityDir,
            GravitySystem.GravityStrength,
            GameManager.gameFinish ? 1 : 0,
            teleportFinished
        );

        frame.AppendToByteList(recordingBytes);

        respawn = 0;
        bounce = 0;
        bounceStrength = 0f;
        bouncePoint = Vector3.zero;
        bounceNormal = Vector3.zero;

        teleportFinished = 0;
    }

    public string GetReplayPath()
    {
        ReplayPaths.EnsureDirectories();

        return Path.Combine(ReplayPaths.ReplayDirectory, GetReplayFileName());
    }

    private string GetReplayFileName()
    {
        return $"{replayName}.urec";
    }

    private string GetPendingReplayFileName(int timeMs, string playerName)
    {
        string levelName = MissionInfo.instance.levelName;

        return $"{levelName}_{timeMs}_{playerName}.urec";
    }

    private string GetPendingReplayPath(int timeMs, string playerName)
    {
        ReplayPaths.EnsureDirectories();

        return Path.Combine(
            ReplayPaths.PendingDirectory,
            GetPendingReplayFileName(timeMs, playerName)
        );
    }

    public string SavePendingReplay(string playerName, out int timeMs)
    {
        timeMs = GetFinalReplayTimeMs();

        string path = GetPendingReplayPath(timeMs, playerName);

        WriteReplayFile(path);

        return path;
    }

    public int GetFinalReplayTimeMs()
    {
        if (recordingBytes == null || recordingBytes.Count == 0)
        {
            throw new System.InvalidOperationException(
                "Cannot get final replay time: no replay frames recorded."
            );
        }

        byte[] bytes = recordingBytes.ToArray();

        ReplayFrame frame = new ReplayFrame();

        int nextOffset = 0;

        while (nextOffset < bytes.Length)
        {
            ReplayFrame nextFrame = new ReplayFrame();

            nextOffset = nextFrame.GetFromByteArray(bytes, nextOffset);

            frame = nextFrame;
        }

        return Mathf.RoundToInt(frame.time);
    }

    public static int GetReplayFileTimeMs(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Replay file not found.", path);
        }

        using (FileStream stream = File.OpenRead(path))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            // Skip UREC header.
            if (
                reader.ReadByte() != 'U'
                || reader.ReadByte() != 'R'
                || reader.ReadByte() != 'E'
                || reader.ReadByte() != 'C'
            )
            {
                throw new InvalidDataException("Invalid replay header.");
            }

            reader.ReadByte(); // Replay version

            // Metadata size is the final 4 bytes.
            stream.Seek(-4, SeekOrigin.End);

            int metadataSize = reader.ReadInt32();

            long metadataStart = stream.Length - 4 - metadataSize;

            int replaySize = (int)(metadataStart - 5);

            stream.Position = 5;

            byte[] replayBytes = reader.ReadBytes(replaySize);

            if (replayBytes.Length == 0)
            {
                throw new InvalidDataException("Replay contains no frames.");
            }

            ReplayFrame frame = new ReplayFrame();

            int playhead = 0;

            while (playhead < replayBytes.Length)
            {
                playhead = frame.GetFromByteArray(replayBytes, playhead);
            }

            return Mathf.RoundToInt(frame.time);
        }
    }

    public string SaveReplay()
    {
        string path = GetReplayPath();

        WriteReplayFile(path);

        return path;
    }

    private void WriteReplayFile(string path)
    {
        using (Stream stream = File.Open(path, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            // Header
            writer.Write(new byte[] { (byte)'U', (byte)'R', (byte)'E', (byte)'C', ReplayVersion });

            // Replay frames
            writer.Write(recordingBytes.ToArray());

            // Metadata
            long metadataStart = stream.Position;

            writer.Write(mi.MissionPath);
            writer.Write(mi.levelName);
            writer.Write(GetCurrentMarbleID());
            writer.Write(actualReplayName);
            writer.Write(replayAuthor);
            writer.Write(replayDesc);

            writer.Write(incompleteReplay ? "Incomplete" : "Complete");

            // Metadata size
            int metadataSize = (int)(stream.Position - metadataStart);

            writer.Write(metadataSize);
        }
    }

    public void StartReplay(string path)
    {
        if (LoadReplay(path))
        {
            isPlayingReplay = true;
            gameFinished = false;
        }
    }

    public void StopReplay()
    {
        isPlayingReplay = false;

        playbackBytes = null;
        currentFrame = null;
        nextFrame = null;
        playhead = 0;
    }

    void UpdateReplay()
    {
        ApplyFrame(currentFrame);

        if (!AdvanceFrame())
        {
            StopReplay();

            Debug.Log("Incomplete Replay ? " + incompleteReplay);
            if (incompleteReplay)
            {
                Debug.Log("Incomplete replay, returning to menu");
                gm.ReturnToMenu();
            }
            else
            {
                returnToMenuTween?.Kill(); // Prevent multiple timers

                returnToMenuTween = DOVirtual
                    .DelayedCall(
                        5f,
                        () =>
                        {
                            gm.ReturnToMenu();
                        }
                    )
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }
        }
    }

    public bool LoadReplay(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Replay file not found: {path}");

            return false;
        }

        using (FileStream stream = File.OpenRead(path))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            // Header
            if (
                reader.ReadByte() != 'U'
                || reader.ReadByte() != 'R'
                || reader.ReadByte() != 'E'
                || reader.ReadByte() != 'C'
            )
            {
                Debug.LogWarning("Unknown replay type.");
                return false;
            }

            if (reader.ReadByte() != ReplayVersion)
            {
                Debug.LogWarning("Unsupported replay version.");
                return false;
            }

            // Read metadata size (last 4 bytes)
            stream.Seek(-4, SeekOrigin.End);
            int metadataSize = reader.ReadInt32();

            // Calculate metadata start
            long metadataStart = stream.Length - 4 - metadataSize;

            // Read metadata
            stream.Position = metadataStart;

            string missionPath = reader.ReadString();
            string levelName = reader.ReadString();
            string marbleID = reader.ReadString();
            string replayName = reader.ReadString();
            string author = reader.ReadString();
            string description = reader.ReadString();
            string complete = reader.ReadString();

            Debug.Log($"Mission Path : {missionPath}");
            Debug.Log($"Level Name   : {levelName}");
            Debug.Log($"Marble ID    : {marbleID}");
            Debug.Log($"Replay Name  : {replayName}");
            Debug.Log($"Author       : {author}");
            Debug.Log($"Description  : {description}");
            Debug.Log($"Finished?    : {complete}");

            incompleteReplay = (complete != "Complete");

            MarbleInfo.instance.ApplyReplayMarble(marbleID);

            // Read replay bytes
            int replaySize = (int)(metadataStart - 5); // 5-byte header

            stream.Position = 5;
            playbackBytes = reader.ReadBytes(replaySize);

            // Load first two frames
            currentFrame = new ReplayFrame();
            playhead = currentFrame.GetFromByteArray(playbackBytes, 0);

            if (playhead < playbackBytes.Length)
            {
                nextFrame = new ReplayFrame();
                playhead = nextFrame.GetFromByteArray(playbackBytes, playhead);
            }
            else
            {
                nextFrame = null;
            }
        }

        return true;
    }

    string GetCurrentMarbleID()
    {
        int index = PlayerPrefs.GetInt("SelectedMarbleIndex", 0);

        bool isCustom = PlayerPrefs.GetInt("DefaultMarbleIsSelected", 0) == 1;

        return (isCustom ? "C" : "D") + (index + 1).ToString("00");
    }

    void ApplyFrame(ReplayFrame frame)
    {
        mov.SetPosition(frame.GetPosition());
        mar.transform.rotation = frame.GetRotation();
        mar.transform.localScale = frame.GetLocalScale();

        mov.marbleVelocity = frame.GetVelocity();
        mov.marbleAngularVelocity = frame.GetAngularVelocity();

        mov.contactPct = frame.contactPct;
        mov.slipAmount = frame.slipAmount;

        if (frame.jump == 1)
            gm.PlayJumpAudio();

        PowerupType replayPowerup = ConvertPowerupEnum(frame.activePowerup);

        if (gm.activePowerup != PowerupType.None && replayPowerup == PowerupType.None)
            mar.UsePowerup();

        gm.activePowerup = replayPowerup;

        gm.currentGems = frame.gemCount;
        gui.SetCurrentGem(frame.gemCount);

        cc.SetOffset(frame.GetCameraOffset());

        GravitySystem.GravityDir = frame.GetGravityDirection();
        GravitySystem.GravityStrength = frame.GetGravityStrength();

        gm.elapsedTime = frame.time;

        if (frame.gameFinished == 1 && !gameFinished)
        {
            gameFinished = true;
            GameManager.gameFinish = true;
            GameUIManager.instance.SetCenterImage(-1);
            gm.FinishRoutine();
        }

        if (frame.bounce == 1)
        {
            mar.BounceEmitter(
                frame.bounceStrength,
                frame.GetBouncePoint(),
                frame.GetBounceNormal()
            );
        }

        if (frame.respawn == 1)
        {
            if (!gm.spawnAudioPlayed)
            {
                gm.PlaySpawnAudio();
                gm.spawnAudioPlayed = true;

                StartCoroutine(gm.ResetSpawnAudio());
            }

            CameraController.instance.LockCamera(true);
            GameUIManager.instance.SetCenterImage(-1);
        }

        if (frame.teleportFinished == 1)
        {
            gm.PlaySpawnAudio();
        }
    }

    bool AdvanceFrame()
    {
        if (playhead >= playbackBytes.Length)
            return false;

        currentFrame = nextFrame;

        if (playhead >= playbackBytes.Length)
            return false;

        nextFrame = new ReplayFrame();
        playhead = nextFrame.GetFromByteArray(playbackBytes, playhead);

        return true;
    }

    static PowerupType ConvertPowerupEnum(string powerup)
    {
        if (powerup == string.Empty)
            return PowerupType.None;
        switch (powerup)
        {
            case "None":
                return PowerupType.None;
            case "SuperSpeed":
                return PowerupType.SuperSpeed;
            case "SuperJump":
                return PowerupType.SuperJump;
            case "SuperBounce":
                return PowerupType.SuperBounce;
            case "ShockAbsorber":
                return PowerupType.ShockAbsorber;
            case "TimeTravel":
                return PowerupType.TimeTravel;
            case "AntiGravity":
                return PowerupType.AntiGravity;
            case "Gyrocopter":
                return PowerupType.Gyrocopter;
            case "EasterEgg":
                return PowerupType.EasterEgg;
        }
        return PowerupType.None;
    }

    static string ConvertPowerupString(PowerupType powerup)
    {
        switch (powerup)
        {
            case PowerupType.None:
                return "None";
            case PowerupType.SuperJump:
                return "SuperJump";
            case PowerupType.SuperSpeed:
                return "SuperSpeed";
            case PowerupType.SuperBounce:
                return "SuperBounce";
            case PowerupType.ShockAbsorber:
                return "ShockAbsorber";
            case PowerupType.TimeTravel:
                return "TimeTravel";
            case PowerupType.AntiGravity:
                return "AntiGravity";
            case PowerupType.Gyrocopter:
                return "Gyrocopter";
            case PowerupType.EasterEgg:
                return "EasterEgg";
        }
        return string.Empty;
    }
}
