using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class LapsMode : NullMode
{
    private readonly GameManager gameManager;

    // ============================================================
    // Mission settings
    // ============================================================

    private readonly int lapsNumber;
    private readonly bool noLapsCheckpoint;

    // ============================================================
    // Laps state
    // ============================================================

    private int lapsCounter = 1;
    private int lapsCPCheck = 1;
    private bool lapsHitLastCP = false;

    private float lapsStartTime = 0f;

    // ============================================================
    // Active respawn checkpoint
    // ============================================================

    private ILapsRespawnTrigger lapsCheckpoint;

    private Vector3 lapsPosition;

    private Quaternion lapsRotation =
        Quaternion.identity;

    private Vector3 lapsCameraPosition;

    private Vector3 lapsUp =
        Vector3.up;

    // ============================================================
    // Saved respawn checkpoint state
    // ============================================================

    private int checkpointLapsCounter = 1;
    private int checkpointLapsCPCheck = 1;
    private bool checkpointLapsHitLastCP = false;
    private float checkpointLapsStartTime = 0f;

    private Vector3 checkpointPosition;

    private Quaternion checkpointRotation =
        Quaternion.identity;

    private Vector3 checkpointCameraPosition;

    private Vector3 checkpointUp =
        Vector3.up;

    // ============================================================
    // Collected gems
    // ============================================================

    // Gems collected during the current Laps run.
    public readonly List<Gem> collectedGem =
        new List<Gem>();

    // Gems that were collected when the currently active
    // respawn checkpoint was reached.
    //
    // Gems collected AFTER reaching the checkpoint are not
    // stored here and will therefore respawn.
    private readonly List<Gem> checkpointCollectedGem =
        new List<Gem>();

    // ============================================================
    // Public state
    // ============================================================

    public int LapsCounter =>
        lapsCounter;

    public int LapsNumber =>
        lapsNumber;

    public int CurrentCheckpoint =>
        lapsCPCheck;

    public bool HitLastCheckpoint =>
        lapsHitLastCP;

    public bool NoLapsCheckpoint =>
        noLapsCheckpoint;

    // ============================================================
    // Constructor
    // ============================================================

    public LapsMode(GameManager gameManager)
        : base(gameManager)
    {
        this.gameManager =
            gameManager;

        lapsNumber =
            Mathf.Max(
                1,
                MissionInfo.instance.lapsNumber
            );

        noLapsCheckpoint =
            MissionInfo.instance.noLapsCheckpoint;
    }

    // ============================================================
    // Mission lifecycle
    // ============================================================

    public override void OnMissionLoad()
    {
        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.SetTargetGem(
                gameManager.TotalGems
            );

            GameUIManager.instance.SetCurrentGem(
                gameManager.CurrentGems
            );
        }

        collectedGem.Clear();
        checkpointCollectedGem.Clear();

        ResetLaps();
    }

    public override void OnRestart()
    {
        collectedGem.Clear();
        checkpointCollectedGem.Clear();

        gameManager.currentGems = 0;

        foreach (Gem gem in gameManager.Gems)
        {
            if (gem == null)
                continue;

            gem.gameObject.SetActive(true);
        }

        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.SetCurrentGem(0);
        }

        ResetLaps();
    }

    private void ResetLaps()
    {
        lapsCounter = 1;
        lapsCPCheck = 1;
        lapsHitLastCP = false;
        lapsStartTime = 0f;

        lapsCheckpoint = null;

        lapsPosition =
            Vector3.zero;

        lapsRotation =
            Quaternion.identity;

        lapsCameraPosition =
            Vector3.zero;

        lapsUp =
            Vector3.up;

        checkpointLapsCounter = 1;
        checkpointLapsCPCheck = 1;
        checkpointLapsHitLastCP = false;
        checkpointLapsStartTime = 0f;

        checkpointPosition =
            Vector3.zero;

        checkpointRotation =
            Quaternion.identity;

        checkpointCameraPosition =
            Vector3.zero;

        checkpointUp =
            Vector3.up;

        UpdateLapsHud();
    }

    // ============================================================
    // HUD
    // ============================================================

    private void UpdateLapsHud()
    {
        if (GameUIManager.instance == null)
            return;

        GameUIManager.instance.EnableLaps();

        GameUIManager.instance.SetLapsText(
            lapsCounter,
            lapsNumber
        );
    }

    private void DisplayHelp(
        string message,
        float duration = 5f)
    {
        if (GameUIManager.instance == null)
            return;

        GameUIManager.instance.SetCenterText(
            message, duration
        );
    }

    // ============================================================
    // GEM COLLECTION
    // ============================================================

    public override void OnGemCollected(
        Gem gem,
        int newGemCount)
    {
        if (gem == null)
            return;

        if (!collectedGem.Contains(gem))
        {
            collectedGem.Add(gem);
        }

        Debug.Log(
            $"[Laps] Collected gem: {gem.name} | " +
            $"Tracked: {collectedGem.Count} | " +
            $"GameManager count: {newGemCount} | " +
            $"LapsMode instance: {GetHashCode()}"
        );
    }

    // ============================================================
    // COUNTER TRIGGER
    // ============================================================

    public void OnCounterTrigger(
        LapsCounterTrigger trigger,
        Marble marble)
    {
        if (trigger == null)
            return;

        if (marble == null ||
            marble != Marble.instance)
            return;

        Debug.Log(
            $"[Laps] Counter entered | " +
            $"Tracked gems: {collectedGem.Count} | " +
            $"Checkpoint gems: {checkpointCollectedGem.Count} | " +
            $"LapsMode instance: {GetHashCode()}"
        );

        // The lap counter is only valid after the final
        // checkpoint has been reached.
        if (lapsHitLastCP)
        {
            if (!OnNextLap())
                return;

            // The LapsCounterTrigger is also a possible
            // respawn checkpoint.
            if (!noLapsCheckpoint &&
                trigger.EnableRespawning)
            {
                SaveCheckpointGems();

                ActivateCheckpoint(
                    trigger
                );
            }
        }
        else if (lapsCPCheck != 1)
        {
            DisplayHelp(
                "Wrong way!",
                5f
            );
        }
    }

    // ============================================================
    // SAVE GEM CHECKPOINT
    // ============================================================

    private void SaveCheckpointGems()
    {
        checkpointCollectedGem.Clear();

        checkpointCollectedGem.AddRange(
            collectedGem
        );

        Debug.Log(
            $"[Laps] Saved " +
            $"{checkpointCollectedGem.Count} gems " +
            $"at lap checkpoint | " +
            $"LapsMode instance: {GetHashCode()}"
        );
    }

    // ============================================================
    // ADVANCE LAP
    // ============================================================

    private bool OnNextLap()
    {
        // Final lap requires all gems.
        if (lapsCounter >= lapsNumber)
        {
            if (!gameManager.CheckForAllGems())
            {
                DisplayHelp(
                    "You need to collect all the gems to finish!",
                    5f
                );

                gameManager.PlayMissingGemAudio();
            }
            else
            {
                GameManager.onFinish?.Invoke();
            }
        }

        float timeDiff =
            gameManager.elapsedTime / 1000f -
            lapsStartTime;

        DisplayHelp(
            $"Lap {lapsCounter}'s Time: " +
            $"{Utils.FormatTime(timeDiff * 1000f)}",
            5f
        );

        lapsCounter++;

        lapsCPCheck = 1;
        lapsHitLastCP = false;

        lapsStartTime =
            gameManager.elapsedTime / 1000f;

        UpdateLapsHud();

        return true;
    }

    // ============================================================
    // CHECKPOINT TRIGGER
    // ============================================================

    public void OnCheckpointTrigger(
        LapsCheckpoint trigger,
        Marble marble)
    {
        if (trigger == null)
            return;

        if (marble == null ||
            marble != Marble.instance)
            return;

        int highest =
            GetHighestCheckpointNumber();

        if (trigger.checkpointNumber == lapsCPCheck)
        {
            if (lapsCPCheck == highest)
            {
                lapsCPCheck = 0;
                lapsHitLastCP = true;
            }
            else
            {
                lapsCPCheck++;
            }

            ActivateCheckpoint(
                trigger
            );
        }
        else
        {
            bool previousCheckpoint =
                trigger.checkpointNumber + 1 ==
                lapsCPCheck;

            bool finalCheckpointRepeat =
                trigger.checkpointNumber == highest &&
                lapsCPCheck == 0;

            if (!previousCheckpoint &&
                !finalCheckpointRepeat)
            {
                DisplayHelp(
                    "Wrong way!",
                    5f
                );
            }
        }
    }

    // ============================================================
    // FIND HIGHEST CHECKPOINT
    // ============================================================

    private int GetHighestCheckpointNumber()
    {
        LapsCheckpoint[] checkpoints =
            UnityEngine.Object.FindObjectsByType<LapsCheckpoint>(
                FindObjectsSortMode.None
            );

        int highest = 0;

        foreach (
            LapsCheckpoint checkpoint
            in checkpoints)
        {
            if (checkpoint == null)
                continue;

            highest =
                Mathf.Max(
                    highest,
                    checkpoint.checkpointNumber
                );
        }

        return Mathf.Max(
            1,
            highest
        );
    }

    // ============================================================
    // ACTIVATE CHECKPOINT
    // ============================================================

    private void ActivateCheckpoint(
        ILapsRespawnTrigger trigger)
    {
        if (trigger == null)
            return;

        if (noLapsCheckpoint)
            return;

        if (!trigger.EnableRespawning)
            return;

        lapsCheckpoint =
            trigger;

        // --------------------------------------------------------
        // Gravity
        // --------------------------------------------------------

        string forceGravity =
            trigger.ForceGravity;

        if (!string.IsNullOrWhiteSpace(
                forceGravity))
        {
            if (!TryParseForceGravity(
                    forceGravity,
                    out lapsUp))
            {
                lapsUp =
                    Vector3.up;
            }
        }
        else
        {
            lapsUp =
                Vector3.up;
        }

        // --------------------------------------------------------
        // Spawn
        // --------------------------------------------------------

        if (trigger.spawn != null)
        {
            lapsPosition =
                trigger.spawn.position;

            lapsRotation =
                trigger.spawn.rotation;
        }
        else
        {
            CaptureCurrentMarbleTransform();
        }

        // --------------------------------------------------------
        // Camera
        // --------------------------------------------------------

        if (trigger.cameraPos != null)
        {
            lapsCameraPosition =
                trigger.cameraPos.position;
        }
        else
        {
            CaptureCurrentCameraPosition();
        }

        // --------------------------------------------------------
        // Save Laps state
        // --------------------------------------------------------

        checkpointLapsCounter =
            lapsCounter;

        checkpointLapsCPCheck =
            lapsCPCheck;

        checkpointLapsHitLastCP =
            lapsHitLastCP;

        checkpointLapsStartTime =
            lapsStartTime;

        checkpointPosition =
            lapsPosition;

        checkpointRotation =
            lapsRotation;

        checkpointCameraPosition =
            lapsCameraPosition;

        checkpointUp =
            lapsUp;

        // IMPORTANT:
        //
        // Do NOT clear or modify collectedGem here.
        //
        // For a normal LapsCheckpoint, OnCheckpointTrigger()
        // reaches this method while collectedGem already contains
        // everything collected so far.
        //
        // For the LapsCounterTrigger, OnCounterTrigger() calls
        // SaveCheckpointGems() explicitly before this method.

        SetGameManagerRespawnPoint();
    }

    // ============================================================
    // MARBLE TRANSFORM
    // ============================================================

    private void CaptureCurrentMarbleTransform()
    {
        if (Marble.instance == null)
            return;

        lapsPosition =
            Marble.instance.transform.position;

        lapsRotation =
            Marble.instance.transform.rotation;
    }

    // ============================================================
    // CAMERA TRANSFORM
    // ============================================================

    private void CaptureCurrentCameraPosition()
    {
        if (CameraController.instance == null)
        {
            lapsCameraPosition =
                Vector3.zero;

            return;
        }

        lapsCameraPosition =
            CameraController.instance.transform.position;
    }

    // ============================================================
    // GAMEMANAGER RESPAWN POINT
    // ============================================================

    private void SetGameManagerRespawnPoint()
    {
        if (gameManager == null)
            return;

        Transform spawn =
            GetOrCreateLapsRespawnTransform();

        spawn.SetPositionAndRotation(
            lapsPosition,
            lapsRotation
        );

        gameManager.activeCheckpoint =
            spawn;

        gameManager.activeCheckpointGravityDir =
            -lapsUp.normalized;

        gameManager.useCheckpoint =
            true;
    }

    private Transform GetOrCreateLapsRespawnTransform()
    {
        const string objectName =
            "_LapsRespawnPoint";

        GameObject existing =
            GameObject.Find(objectName);

        if (existing == null)
        {
            existing =
                new GameObject(
                    objectName
                );

            existing.hideFlags =
                HideFlags.HideInHierarchy;
        }

        return existing.transform;
    }

    // ============================================================
    // RESPAWN
    // ============================================================

    public override void OnRespawn()
    {
        if (noLapsCheckpoint)
            return;

        if (lapsCheckpoint == null)
        {
            CameraController.instance?.ResetCam();
            return;
        }

        lapsCounter =
            checkpointLapsCounter;

        lapsCPCheck =
            checkpointLapsCPCheck;

        lapsHitLastCP =
            checkpointLapsHitLastCP;

        lapsStartTime =
            checkpointLapsStartTime;

        lapsPosition =
            checkpointPosition;

        lapsRotation =
            checkpointRotation;

        lapsCameraPosition =
            checkpointCameraPosition;

        lapsUp =
            checkpointUp;

        RestoreGravity();

        // --------------------------------------------------------
        // Marble
        // --------------------------------------------------------

        if (Movement.instance != null)
        {
            Movement.instance.SetPosition(
                lapsPosition
            );

            Movement.instance.StopAllMovement();
        }

        // --------------------------------------------------------
        // Camera
        // --------------------------------------------------------

        RestoreCamera();

        // --------------------------------------------------------
        // Gems
        // --------------------------------------------------------

        RestoreCheckpointGems();

        // --------------------------------------------------------
        // HUD
        // --------------------------------------------------------

        UpdateLapsHud();
    }

    // ============================================================
    // RESTORE CHECKPOINT GEMS
    // ============================================================

    private void RestoreCheckpointGems()
    {
        if (gameManager.Gems == null)
            return;

        foreach (Gem gem in gameManager.Gems)
        {
            if (gem == null)
                continue;

            bool collectedAtCheckpoint =
                checkpointCollectedGem.Contains(
                    gem
                );

            gem.gameObject.SetActive(
                !collectedAtCheckpoint
            );
        }

        collectedGem.Clear();

        collectedGem.AddRange(
            checkpointCollectedGem
        );

        gameManager.currentGems =
            checkpointCollectedGem.Count;

        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.SetCurrentGem(
                gameManager.currentGems
            );
        }

        Debug.Log(
            $"[Laps] Restored " +
            $"{checkpointCollectedGem.Count} gems " +
            $"from checkpoint | " +
            $"LapsMode instance: {GetHashCode()}"
        );
    }

    // ============================================================
    // CAMERA
    // ============================================================

    private void RestoreCamera()
    {
        if (CameraController.instance == null)
            return;

        if (Marble.instance == null)
            return;

        if (lapsCheckpoint == null)
            return;

        Transform spawn =
            lapsCheckpoint.spawn;

        Transform cameraPos =
            lapsCheckpoint.cameraPos;

        if (spawn == null ||
            cameraPos == null)
            return;

        CameraController.instance.SetCameraPosition(
            spawn.position,
            cameraPos.position
        );
    }

    // ============================================================
    // GRAVITY
    // ============================================================

    private void RestoreGravity()
    {
        if (lapsUp.sqrMagnitude < 0.0001f)
            lapsUp = Vector3.up;

        lapsUp.Normalize();

        Vector3 gravityDir =
            -lapsUp;

        gameManager.activeCheckpointGravityDir =
            gravityDir;

        GravityModifier.ResetGravityGlobal(
            gravityDir
        );
    }

    // ============================================================
    // FORCE GRAVITY
    // ============================================================

    private static bool TryParseForceGravity(
    string value,
    out Vector3 up)
    {
        up = Vector3.up;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string[] words =
            value.Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries
            );

        if (words.Length < 4)
            return false;

        if (!float.TryParse(words[0], out float x) ||
            !float.TryParse(words[1], out float y) ||
            !float.TryParse(words[2], out float z) ||
            !float.TryParse(words[3], out float angle))
        {
            return false;
        }

        Vector3 axis = new Vector3(x, -y, z);

        if (axis.sqrMagnitude < Mathf.Epsilon)
            return false;

        axis.Normalize();

        Quaternion rotation = Quaternion.AngleAxis(angle, axis);

        // Convert the quaternion into the direction it faces.
        up = rotation * Vector3.up;

        return true;
    }

}