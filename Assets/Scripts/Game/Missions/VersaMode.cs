using System;
using System.Collections;
using System.IO;
using UnityEngine;
using PlatinumQuestScripts;

/// <summary>
/// Versa loads the state produced by Vice and starts the timer at the
/// saved Vice completion time.
///
/// Saved state:
///     <Game Root>/Replays/State/vv<Name>.txt
///
/// The selected run is provided through SelectedRunFile.
///
/// The state is loaded during mission initialization, but the actual
/// state restoration is deferred until after GameManager has completed
/// its normal restart/reset sequence.
/// </summary>
public class VersaMode : ISpecialGameMode
{
    private const float RSG_RAMP_DURATION = 3.5f;

    public static string SelectedRunFile;

    private readonly GameManager gameManager;

    private ViceVersaStateData loadedState;

    private float targetElapsedTime;

    private bool loaded;

    private Coroutine versaTimerCoroutine;

    private Coroutine restoreStateCoroutine;

    private string SaveDirectory
    {
        get
        {
            DirectoryInfo parent =
                Directory.GetParent(Application.dataPath);

            if (parent == null)
            {
                return Path.Combine(
                    Application.dataPath,
                    "Replays",
                    "State"
                );
            }

            return Path.Combine(
                parent.FullName,
                "Replays",
                "State"
            );
        }
    }

    private string SaveFilePath
    {
        get
        {
            return Path.Combine(
                SaveDirectory,
                SelectedRunFile
            );
        }
    }

    public VersaMode(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void OnMissionLoad()
    {
        GameManager.onOutOfBounds.AddListener(
            OnOutOfBounds
        );

        loadedState = null;
        loaded = false;
        targetElapsedTime = 0f;

        // Make sure there is always a selected run.
        if (string.IsNullOrEmpty(SelectedRunFile))
        {
            SelectedRunFile = "vvRun.txt";

            Debug.Log(
                "[Versa] No run was selected. " +
                "Defaulting to vvRun.txt."
            );
        }

        string path;

        try
        {
            path = SaveFilePath;
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[Versa] Failed to construct save path.\n{e}"
            );

            return;
        }

        Debug.Log(
            $"[Versa] Looking for run:\n{path}"
        );

        if (!File.Exists(path))
        {
            Debug.LogError(
                $"[Versa] No saved Vice run found.\n" +
                $"Expected file:\n{path}"
            );

            return;
        }

        try
        {
            // The file is AES encrypted.
            // Do NOT use File.ReadAllText().
            loadedState =
                ViceVersaState.LoadFromFile(path);

            if (loadedState == null)
            {
                Debug.LogError(
                    $"[Versa] Failed to decrypt or parse run.\n" +
                    $"File:\n{path}"
                );

                return;
            }

            Debug.Log(
                $"[Versa] Loaded state: " +
                $"Fading={loadedState.fadingPlatforms.Count}, " +
                $"TimeTravels={loadedState.timeTravels.Count}, " +
                $"Moving={loadedState.movingPlatforms.Count}, " +
                $"Powerup={loadedState.heldPowerup}, " +
                $"Time={loadedState.elapsedTime}"
            );

            Debug.Log(
                $"[Versa] Loaded Vice run successfully.\n" +
                $"File: {path}\n" +
                $"Saved time: {loadedState.elapsedTime} ms"
            );

            /*
             * IMPORTANT:
             *
             * Do not call ApplyLoadedState() here.
             *
             * The mission is still being initialized. GameManager and
             * other level objects may reset their state after this point.
             *
             * We only remember the loaded data here.
             */
            targetElapsedTime =
                loadedState.elapsedTime;

            loaded = true;
        }
        catch (Exception e)
        {
            loadedState = null;
            loaded = false;

            Debug.LogError(
                $"[Versa] Failed to load Vice run.\n" +
                $"File:\n{path}\n" +
                $"Exception:\n{e}"
            );
        }
    }

    private void ApplyLoadedState()
    {
        if (!loaded || loadedState == null)
            return;

        Debug.Log(
            $"[Versa] Applying saved Vice state: " +
            $"Fading={loadedState.fadingPlatforms.Count}, " +
            $"TimeTravels={loadedState.timeTravels.Count}, " +
            $"Moving={loadedState.movingPlatforms.Count}"
        );

        ViceVersaState.Apply(
            gameManager,
            loadedState
        );

        targetElapsedTime =
            loadedState.elapsedTime;
    }

    public void OnRestart()
    {
        /*
         * GameManager calls this when a new attempt is beginning.
         *
         * OnRestart happens before GameManager performs its normal
         * restart/reset operations, so we must NOT immediately apply
         * the Vice state here.
         */

        StopVersaTimerCoroutine();

        if (restoreStateCoroutine != null)
        {
            gameManager.StopCoroutine(
                restoreStateCoroutine
            );

            restoreStateCoroutine = null;
        }

        if (!loaded || loadedState == null)
            return;

        /*
         * Wait one frame so GameManager can finish its normal reset.
         * Then restore the Vice state.
         */
        restoreStateCoroutine =
            gameManager.StartCoroutine(
                RestoreStateAfterRestart()
            );
    }

    private IEnumerator RestoreStateAfterRestart()
    {
        /*
         * Allow GameManager's normal restart/reset sequence to finish.
         */
        yield return null;

        restoreStateCoroutine = null;

        if (!loaded || loadedState == null)
            yield break;

        if (GameManager.gameFinish)
            yield break;

        /*
         * NOW restore all state from Vice.
         *
         * This includes:
         * - fading platforms
         * - TimeTravels
         * - moving platforms
         * - held powerup
         */
        ApplyLoadedState();

        /*
         * Versa timer starts at zero and progressively approaches
         * the saved Vice completion time.
         */
        gameManager.elapsedTime = 0f;

        RestoreHeldPowerup();

        versaTimerCoroutine =
            gameManager.StartCoroutine(
                VersaTimerRoutine()
            );
    }

    public void OnRespawn()
    {
        if (!loaded || loadedState == null)
            return;

        StopVersaTimerCoroutine();

        if (restoreStateCoroutine != null)
        {
            gameManager.StopCoroutine(
                restoreStateCoroutine
            );

            restoreStateCoroutine = null;
        }

        /*
         * GameManager may perform its own respawn/reset immediately
         * around this callback. Defer restoration by one frame for
         * the same reason as OnRestart().
         */
        restoreStateCoroutine =
            gameManager.StartCoroutine(
                RestoreStateAfterRespawn()
            );
    }

    private IEnumerator RestoreStateAfterRespawn()
    {
        /*
         * Allow the normal respawn/reset process to finish.
         */
        yield return null;

        restoreStateCoroutine = null;

        if (!loaded || loadedState == null)
            yield break;

        if (GameManager.gameFinish)
            yield break;

        /*
         * Restore the complete Vice state again.
         */
        ApplyLoadedState();

        /*
         * Restart the Versa timer restoration.
         */
        gameManager.elapsedTime = 0f;

        RestoreHeldPowerup();

        versaTimerCoroutine =
            gameManager.StartCoroutine(
                VersaTimerRoutine()
            );
    }

    private IEnumerator VersaTimerRoutine()
    {
        float elapsed = 0f;

        /*
         * The original Versa behavior restores the saved Vice
         * completion time progressively over 3.5 seconds.
         *
         * elapsedTime is milliseconds.
         */
        while (elapsed < RSG_RAMP_DURATION)
        {
            /*
             * Stop if the player has finished the mission.
             */
            if (GameManager.gameFinish)
            {
                versaTimerCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / RSG_RAMP_DURATION
                );

            /*
             * Progressively restore the Vice completion time.
             */
            gameManager.elapsedTime =
                targetElapsedTime * progress;

            if (GameUIManager.instance != null)
            {
                GameUIManager.instance.SetTimerText(
                    gameManager.elapsedTime,
                    true
                );
            }

            yield return null;
        }

        /*
         * Guarantee that the final value is exactly the saved
         * Vice completion time.
         */
        gameManager.elapsedTime =
            targetElapsedTime;

        RestoreHeldPowerup();

        versaTimerCoroutine = null;
    }

    private void StopVersaTimerCoroutine()
    {
        if (versaTimerCoroutine == null)
            return;

        gameManager.StopCoroutine(
            versaTimerCoroutine
        );

        versaTimerCoroutine = null;
    }

    public void Update()
    {
        /*
         * Timer restoration is handled entirely by the coroutine.
         *
         * Nothing needs to happen here.
         */
    }

    public void OnJump()
    {
    }

    public void ProcessMaterialContact(
        Marble marble,
        CollisionInfo contact)
    {
    }

    private void RestoreHeldPowerup()
    {
        if (loadedState == null)
            return;

        gameManager.activePowerup =
            ViceVersaState.ParseSavedPowerup(
                loadedState.heldPowerup
            );

        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.SetPowerupIcon(
                gameManager.activePowerup
            );
        }
    }

    private void OnOutOfBounds()
    {
        foreach (
            MegaManPlatform platform
            in UnityEngine.Object
                .FindObjectsOfType<MegaManPlatform>(true))
        {
            platform.ResetPlatform();
        }
    }
}