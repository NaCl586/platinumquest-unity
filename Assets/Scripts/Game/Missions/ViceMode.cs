using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlatinumQuestScripts;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Persistent state shared between Vice and Versa.
///
/// Saves:
/// - FadePlatform state
/// - TimeTravel state
/// - MovingPlatform state
/// - elapsed time
/// - held powerup
///
/// Saved to:
///     <Game Root>/Replays/State/vv<Name>.txt
/// </summary>
[Serializable]
public class ViceVersaStateData
{
    public List<NamedBoolState> fadingPlatforms =
        new List<NamedBoolState>();

    public List<NamedBoolState> timeTravels =
        new List<NamedBoolState>();

    public List<MovingPlatformState> movingPlatforms =
        new List<MovingPlatformState>();

    // GameManager.elapsedTime is milliseconds.
    public float elapsedTime;

    // Stored as the PowerupType name.
    public string heldPowerup;
}

/// <summary>
/// Shared named boolean state.
///
/// For TimeTravels:
///     hidden = !IsActive
///
/// For FadePlatforms:
///     hidden is kept for compatibility,
///     while opacity/fadingState contain the real state.
///
/// The additional fields are optional, so older save files can
/// still be deserialized.
/// </summary>
[Serializable]
public class NamedBoolState
{
    public string name;

    public bool hidden;

    // ------------------------------------------------------------
    // FadePlatform state
    // ------------------------------------------------------------

    /// <summary>
    /// Exact opacity when the state was saved.
    /// </summary>
    public float opacity = 1f;
}

[Serializable]
public class MovingPlatformState
{
    public string name;
    public float currentTime;
    public float targetTime;
}

public static class ViceVersaState
{
    public static string fileName;

    private static readonly string[] FADING_PLATFORMS =
    {
        "IslandOneRight01",
        "IslandOneRight02",
        "IslandOneRight03",
        "IslandOneRight04",
        "IslandOneRight05",
        "IslandOneRight06",
        "IslandOneRight07",
        "IslandOneRight08",
        "IslandOneRight09",
        "IslandOneRight10",
        "IslandOneRight11",
        "IslandOneRight12",
        "IslandOneRight13",
        "IslandOneRight14",
        "IslandOneRight15",
        "IslandOneRight16",
        "IslandOneRight17",
        "IslandOneRight18",
        "IslandOneRight19",
        "IslandOneRight20",
        "IslandOneRight21",
        "IslandOneRight22",
        "IslandOneRight23",
        "IslandOneRight24",
        "IslandOneRight25",
        "IslandOneRight26",
        "IslandOneRight27",

        "IslandThreeLeft01",
        "IslandThreeLeft02",
        "IslandThreeLeft03",
        "IslandThreeLeft04",
        "IslandThreeLeft05",
        "IslandThreeLeft06",
        "IslandThreeLeft07",
        "IslandThreeLeft08",
        "IslandThreeLeft09",
        "IslandThreeLeft10",
        "IslandThreeLeft11",
        "IslandThreeLeft12",
        "IslandThreeLeft13",
        "IslandThreeLeft14",
        "IslandThreeLeft15",
        "IslandThreeLeft16",
        "IslandThreeLeft17",
        "IslandThreeLeft18",
        "IslandThreeLeft19",
        "IslandThreeLeft20",
        "IslandThreeLeft21",
        "IslandThreeLeft22",
        "IslandThreeLeft23",
        "IslandThreeLeft24",

        "IslandThreeUpsideDown01",
        "IslandThreeUpsideDown02",
        "IslandThreeUpsideDown03",
        "IslandThreeUpsideDown04",
        "IslandThreeUpsideDown05",
        "IslandThreeUpsideDown06",
        "IslandThreeUpsideDown07",
        "IslandThreeUpsideDown08",
        "IslandThreeUpsideDown09",
        "IslandThreeUpsideDown10",
        "IslandThreeUpsideDown11",
        "IslandThreeUpsideDown12"
    };

    private static readonly string[] TIME_TRAVELS =
    {
        "TimeTravelRestStop",

        "TimeTravel01",
        "TimeTravel02",
        "TimeTravel03",
        "TimeTravel04",
        "TimeTravel05",
        "TimeTravel06",
        "TimeTravel07",
        "TimeTravel08",
        "TimeTravel09",
        "TimeTravel10",
        "TimeTravel11",
        "TimeTravel12",
        "TimeTravel13",
        "TimeTravel14",
        "TimeTravel15",
        "TimeTravel16",
        "TimeTravel17",
        "TimeTravel18",
        "TimeTravel19",
        "TimeTravel20",
        "TimeTravel21",
        "TimeTravel22",
        "TimeTravel23",
        "TimeTravel24",
        "TimeTravel25",
        "TimeTravel26",
        "TimeTravel27",
        "TimeTravel28",
        "TimeTravel29",
        "TimeTravel30",
        "TimeTravel31",
        "TimeTravel32"
    };

    private static readonly string[] MOVING_PLATFORMS =
    {
        "MustChange_1",
        "MustChange_5",
        "MustChange_6",
        "MustChange_7",
        "MustChange_8",
        "MustChange_9",
        "MustChange_10",
        "MustChange_11",
        "MustChange_12",
        "MustChange_13",
        "MustChange_14",
        "MustChange_15",
        "MustChange_16",
        "MustChange_17",
        "MustChange_18"
    };

    private const string ENCRYPTION_KEY =
        "PQ_ViceVersa_State_Key_2026";

    // ============================================================
    // Save path
    // ============================================================

    private static string SaveDirectory
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

    private static string SaveFilePath
    {
        get
        {
            string name = fileName;

            if (string.IsNullOrWhiteSpace(name))
                name = "vvRun.txt";

            /*
             * Make sure the save is always a Vice/Versa save.
             */
            if (!name.StartsWith("vv", StringComparison.OrdinalIgnoreCase))
                name = "vv" + name;

            if (!name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                name += ".txt";

            return Path.Combine(
                SaveDirectory,
                name
            );
        }
    }

    // ============================================================
    // Encryption
    // ============================================================

    private static byte[] Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                aes.Key =
                    sha256.ComputeHash(
                        Encoding.UTF8.GetBytes(
                            ENCRYPTION_KEY
                        )
                    );
            }

            aes.GenerateIV();

            byte[] plainBytes =
                Encoding.UTF8.GetBytes(
                    plainText
                );

            byte[] encrypted;

            using (
                ICryptoTransform encryptor =
                    aes.CreateEncryptor()
            )
            {
                encrypted =
                    encryptor.TransformFinalBlock(
                        plainBytes,
                        0,
                        plainBytes.Length
                    );
            }

            /*
             * Store IV + encrypted data.
             */
            byte[] result =
                new byte[
                    aes.IV.Length +
                    encrypted.Length
                ];

            Buffer.BlockCopy(
                aes.IV,
                0,
                result,
                0,
                aes.IV.Length
            );

            Buffer.BlockCopy(
                encrypted,
                0,
                result,
                aes.IV.Length,
                encrypted.Length
            );

            return result;
        }
    }

    private static string Decrypt(byte[] encryptedData)
    {
        using (Aes aes = Aes.Create())
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                aes.Key =
                    sha256.ComputeHash(
                        Encoding.UTF8.GetBytes(
                            ENCRYPTION_KEY
                        )
                    );
            }

            int ivLength =
                aes.BlockSize / 8;

            if (encryptedData == null ||
                encryptedData.Length <= ivLength)
            {
                throw new InvalidDataException(
                    "Invalid Vice/Versa state file."
                );
            }

            byte[] iv =
                new byte[ivLength];

            byte[] encrypted =
                new byte[
                    encryptedData.Length -
                    ivLength
                ];

            Buffer.BlockCopy(
                encryptedData,
                0,
                iv,
                0,
                ivLength
            );

            Buffer.BlockCopy(
                encryptedData,
                ivLength,
                encrypted,
                0,
                encrypted.Length
            );

            aes.IV = iv;

            using (
                ICryptoTransform decryptor =
                    aes.CreateDecryptor()
            )
            {
                byte[] plainBytes =
                    decryptor.TransformFinalBlock(
                        encrypted,
                        0,
                        encrypted.Length
                    );

                return Encoding.UTF8.GetString(
                    plainBytes
                );
            }
        }
    }

    // ============================================================
    // Save existence
    // ============================================================

    public static bool HasSavedState()
    {
        if (!Directory.Exists(SaveDirectory))
            return false;

        string[] files =
            Directory.GetFiles(
                SaveDirectory,
                "vv*.txt"
            );

        return files.Length > 0;
    }

    // ============================================================
    // Load current selected save
    // ============================================================

    public static ViceVersaStateData Load()
    {
        return LoadFromFile(
            SaveFilePath
        );
    }

    // ============================================================
    // Load arbitrary save
    // ============================================================

    public static ViceVersaStateData LoadFromFile(
        string path
    )
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (!File.Exists(path))
            return null;

        try
        {
            byte[] encrypted =
                File.ReadAllBytes(path);

            string json =
                Decrypt(encrypted);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            ViceVersaStateData data =
                JsonUtility.FromJson<ViceVersaStateData>(
                    json
                );

            Debug.Log(
                $"[Versa] Loaded state: " +
                $"Fading={data.fadingPlatforms.Count}, " +
                $"TimeTravels={data.timeTravels.Count}, " +
                $"Moving={data.movingPlatforms.Count}, " +
                $"Powerup={data.heldPowerup}, " +
                $"Time={data.elapsedTime}"
            );

            return data;
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[Vice/Versa] Failed to load state file.\n" +
                $"File: {path}\n" +
                $"{e}"
            );

            return null;
        }
    }

    // ============================================================
    // Save
    // ============================================================

    public static void Save(
        GameManager gameManager
    )
    {
        if (gameManager == null)
            return;

        ViceVersaStateData data =
            new ViceVersaStateData
            {
                elapsedTime =
                    gameManager.elapsedTime,

                heldPowerup =
                    gameManager.activePowerup.ToString()
            };

        // --------------------------------------------------------
        // Fade Platforms
        // --------------------------------------------------------

        foreach (string name in FADING_PLATFORMS)
        {
            GameObject obj =
                FindNamedObject(name);

            FadePlatform fp =
                obj != null
                    ? obj.GetComponent<FadePlatform>()
                    : null;

            if (fp != null)
            {
                data.fadingPlatforms.Add(
                    new NamedBoolState
                    {
                        name = name,

                        /*
                         * Keep hidden for compatibility and
                         * easy debugging.
                         */
                        hidden =
                            Mathf.Approximately(
                                fp.CurrentOpacity,
                                0f
                            ),

                        /*
                         * Store the actual visual state too.
                         */
                        opacity =
                            fp.CurrentOpacity
                    }
                );
            }
            else
            {
                data.fadingPlatforms.Add(
                    new NamedBoolState
                    {
                        name = name,
                        hidden = false,
                        opacity = 1f
                    }
                );
            }
        }

        // --------------------------------------------------------
        // Time Travels
        // --------------------------------------------------------

        foreach (string name in TIME_TRAVELS)
        {
            GameObject obj =
                FindNamedObject(name);

            TimeTravel tt =
                obj != null
                    ? obj.GetComponent<TimeTravel>()
                    : null;

            data.timeTravels.Add(
                new NamedBoolState
                {
                    name = name,

                    hidden =
                        tt != null &&
                        !tt.IsActive
                }
            );
        }

        // --------------------------------------------------------
        // Moving Platforms
        // --------------------------------------------------------

        foreach (string name in MOVING_PLATFORMS)
        {
            GameObject obj =
                FindNamedObject(name);

            MovingPlatform mp =
                obj != null
                    ? obj.GetComponent<MovingPlatform>()
                    : null;

            data.movingPlatforms.Add(
                new MovingPlatformState
                {
                    name = name,

                    currentTime =
                        mp != null
                            ? mp.CurrentTime
                            : 0f,

                    targetTime =
                        mp != null
                            ? mp.TargetTime
                            : 0f
                }
            );
        }

        // --------------------------------------------------------
        // Write file
        // --------------------------------------------------------

        string json =
            JsonUtility.ToJson(
                data,
                true
            );

        try
        {
            Directory.CreateDirectory(
                SaveDirectory
            );

            byte[] encrypted =
                Encrypt(json);

            File.WriteAllBytes(
                SaveFilePath,
                encrypted
            );

            Debug.Log(
                $"[Vice] Saving state: " +
                $"Fading={data.fadingPlatforms.Count}, " +
                $"TimeTravels={data.timeTravels.Count}, " +
                $"Moving={data.movingPlatforms.Count}, " +
                $"Powerup={data.heldPowerup}, " +
                $"Time={data.elapsedTime}\n" +
                $"File={SaveFilePath}"
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[Vice] Failed to save Vice run.\n" +
                $"File: {SaveFilePath}\n" +
                $"{e}"
            );
        }
    }

    // ============================================================
    // Apply
    // ============================================================

    public static void Apply(
        GameManager gameManager,
        ViceVersaStateData data
    )
    {
        if (gameManager == null ||
            data == null)
        {
            return;
        }

        // --------------------------------------------------------
        // Fade Platforms
        // --------------------------------------------------------

        if (data.fadingPlatforms != null)
        {
            foreach (
                NamedBoolState state
                in data.fadingPlatforms
            )
            {
                GameObject obj =
                    FindNamedObject(state.name);

                FadePlatform fp =
                    obj != null
                        ? obj.GetComponent<FadePlatform>()
                        : null;

                if (fp == null)
                    continue;

                fp.gameObject.SetActive(state.opacity <= 0);
            }
        }

        // --------------------------------------------------------
        // Time Travels
        // --------------------------------------------------------

        if (data.timeTravels != null)
        {
            foreach (
                NamedBoolState state
                in data.timeTravels
            )
            {
                GameObject obj =
                    FindNamedObject(state.name);

                TimeTravel tt =
                    obj != null
                        ? obj.GetComponent<TimeTravel>()
                        : null;

                if (tt == null)
                    continue;

                tt.gameObject.SetActive(!state.hidden);

                /*tt.SetActiveState(
                    !state.hidden
                );*/
            }
        }

        // --------------------------------------------------------
        // Moving Platforms
        // --------------------------------------------------------

        if (data.movingPlatforms != null)
        {
            foreach (
                MovingPlatformState state
                in data.movingPlatforms
            )
            {
                GameObject obj =
                    FindNamedObject(state.name);

                MovingPlatform mp =
                    obj != null
                        ? obj.GetComponent<MovingPlatform>()
                        : null;

                if (mp == null)
                    continue;

                mp.SetViceVersaState(
                    state.currentTime,
                    state.targetTime
                );
            }
        }

        // --------------------------------------------------------
        // Held Powerup
        // --------------------------------------------------------

        gameManager.activePowerup =
            ParseSavedPowerup(
                data.heldPowerup
            );

        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.SetPowerupIcon(
                gameManager.activePowerup
            );
        }
    }

    // ============================================================
    // Powerup parsing
    // ============================================================

    public static PowerupType ParseSavedPowerup(
        string value
    )
    {
        if (string.IsNullOrEmpty(value))
            return PowerupType.None;

        PowerupType result;

        return Enum.TryParse(
            value,
            true,
            out result
        )
            ? result
            : PowerupType.None;
    }

    // ============================================================
    // Object lookup
    // ============================================================

    private static GameObject FindNamedObject(
        string objectName
    )
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        /*
         * Fast path.
         */
        GameObject obj =
            GameObject.Find(
                objectName
            );

        if (obj != null)
            return obj;

        /*
         * Fade Platforms.
         */
        foreach (
            FadePlatform fp
            in UnityEngine.Object
                .FindObjectsOfType<FadePlatform>(true)
        )
        {
            if (
                string.Equals(
                    fp.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return fp.gameObject;
            }
        }

        /*
         * Time Travels.
         */
        foreach (
            TimeTravel tt
            in UnityEngine.Object
                .FindObjectsOfType<TimeTravel>(true)
        )
        {
            if (
                string.Equals(
                    tt.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return tt.gameObject;
            }
        }

        /*
         * Moving Platforms.
         */
        foreach (
            MovingPlatform mp
            in UnityEngine.Object
                .FindObjectsOfType<MovingPlatform>(true)
        )
        {
            if (
                string.Equals(
                    mp.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return mp.gameObject;
            }
        }

        return null;
    }
}

/// <summary>
/// Vice mode.
///
/// Plays the Vice half of the level and saves the resulting state
/// when the player finishes.
/// </summary>
public class ViceMode : ISpecialGameMode
{
    private readonly GameManager gameManager;

    private bool checkedFinish;

    public ViceMode(
        GameManager gameManager
    )
    {
        this.gameManager =
            gameManager;
    }

    public void OnMissionLoad()
    {
        GameManager.onOutOfBounds.AddListener(
            OnOutOfBounds
        );
    }

    public void OnRestart()
    {
        checkedFinish = false;
    }

    public void OnRespawn()
    {
    }

    public void Update()
    {
        if (
            checkedFinish ||
            !GameManager.gameFinish
        )
        {
            return;
        }

        checkedFinish = true;

        /*
         * Do not save replay playback runs.
         */
        if (ReplayRecorder.loadReplay)
            return;
    }

    public void OnJump()
    {
    }

    public void ProcessMaterialContact(
        Marble marble,
        CollisionInfo contact
    )
    {
    }

    private void OnOutOfBounds()
    {
        foreach (
            MegaManPlatform platform
            in UnityEngine.Object
                .FindObjectsOfType<MegaManPlatform>(true)
        )
        {
            platform.ResetPlatform();
        }
    }
}