using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TS;
using UnityEngine;

[Serializable]
public class MissionGemGroup
{
    public List<Gem> gems = new List<Gem>();
}

public class MissionInfo : MonoBehaviour
{
    public static MissionInfo instance;

    [HideInInspector] public string highScoreName;

    [Header("Mission Info")]
    [TextArea(1, 2)] public string MissionPath;
    public Sprite levelImage;
    public string directory;
    public int levelNumber;

    [Space]
    public int time;
    public string missionName;
    public string levelName;

    [Space]
    [TextArea(2, 10)]
    public string description;

    [Space]
    [TextArea(2, 10)]
    public string startHelpText;

    public string artist;
    public string music;
    public string skyboxName;

    public int parScore = 0;
    public int platinumTime = -1;
    public int ultimateTime = -1;
    public int awesomeTime = -1;

    public int alarmTime = 15;

    public bool hasEgg;

    public string generalHint;
    public string platinumHint;
    public string ultimateHint;
    public string awesomeHint;
    public string nestEggHint;
    public string trivia;

    [Header("Game Modes")]
    public List<Mode> gameModes = new List<Mode>();

    [Header("Quota")]
    public int gemQuota = -1;

    [Header("Laps")]
    public int lapsNumber = -1;
    public bool noLapsCheckpoint = false;

    [Header("2D")]
    public string cameraPlane;
    public bool invertCameraPlane;

    public bool hasCameraPitch;
    public float cameraPitch;

    public bool hasInitialCameraDistance;
    public float initialCameraDistance;

    public bool hasCameraFov;
    public float cameraFov;

    [Header("Consistency")]
    public float minimumSpeed;
    public float penaltyDelay;
    public float gracePeriod;

    [Header("Haste")]
    public float speedToQualify;

    [Header("Hunt")]
    public int maxGemsPerSpawn = 7;
    public float radiusFromGem = 15f;
    public float spawnBlock = 30f;

    public int minPointsPerSpawn = 5;
    public int minGemsPerSpawn = 3;

    public float redSpawnChance = 0.9f;
    public float yellowSpawnChance = 0.65f;
    public float blueSpawnChance = 0.35f;
    public float platinumSpawnChance = 0.18f;

    public int gemGroups = 0;

    [Header("Radar")]
    public string radar;
    public string customRadarRule;
    public bool forceRadar;
    public bool hideRadar;

    [Header("Physics")]
    public float gravity = 20f;
    public float angularAcceleration;
    public float brakingAcceleration;
    public float maxRollVelocity;
    public float jumpImpulse;

    [Header("Mission Mode")]
    public SpecialMissionMode specialMissionMode =
        SpecialMissionMode.None;

    [Header("Load Mission")]
    public List<Mission> missionsTutorial = new List<Mission>();
    public List<Mission> missionsBeginner = new List<Mission>();
    public List<Mission> missionsIntermediate = new List<Mission>();
    public List<Mission> missionsAdvanced = new List<Mission>();
    public List<Mission> missionsExpert = new List<Mission>();
    public List<Mission> missionsBonus = new List<Mission>();
    public List<Mission> missionsDC = new List<Mission>();

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Start()
    {
        highScoreName = PlayerPrefs.GetString("HighScoreName", "");

        LoadMissions(Type.tutorial);
        LoadMissions(Type.beginner);
        LoadMissions(Type.intermediate);
        LoadMissions(Type.advanced);
        LoadMissions(Type.expert);
        LoadMissions(Type.bonus);
        LoadMissions(Type.dc);
    }

    // ============================================================
    // LOAD MISSIONS
    // ============================================================

    public void LoadMissions(Type difficulty)
    {
        string path = "platinum/data/missions_pq/";
        string basePath = Path.Combine(Application.streamingAssetsPath, path, difficulty.ToString());

        if (!Directory.Exists(basePath))
            return;

        string[] misFiles = Directory.GetFiles(basePath, "*.mcs");
        if (misFiles == null || misFiles.Length == 0)
            return;

        List<Mission> loadedMissions = new List<Mission>();

        foreach (string misPath in misFiles)
        {
            string levelFileName = Path.GetFileNameWithoutExtension(misPath);
            Sprite sprite = TryLoadMissionSprite(basePath, levelFileName);

            Mission newMission = new Mission
            {
                levelImage = sprite,
                directory = $"{path}{difficulty}/{levelFileName}.mcs",
                levelNumber = -1,
                hasEgg = false
            };

            if (newMission.levelImage != null)
                newMission.levelImage.name = levelFileName;

            McsFile mcs = McsParser.Parse(misPath);
            if (mcs == null)
            {
                Debug.LogError($"Could not parse mission file: {misPath}");
                continue;
            }

            // 1. Process Mission Info Block
            if (mcs.MissionInfoObjects != null && mcs.MissionInfoObjects.Count > 0)
            {
                TSObject missionInfo = mcs.MissionInfoObjects[0];
                if (missionInfo != null && missionInfo.ClassName == "ScriptObject")
                {
                    ParseMissionData(missionInfo, levelFileName, newMission);
                }
            }
            else
            {
                Debug.LogWarning($"No MissionInfo found in {misPath}");
                continue;
            }

            // 2. Process World/Mission Objects (Sky, EasterEgg, etc.)
            if (mcs.MissionObjects != null)
            {
                foreach (var obj in mcs.MissionObjects)
                {
                    ParseMissionObjects(obj, misPath, newMission);
                }
            }

            loadedMissions.Add(newMission);
        }

        // Sort and assign to category list
        List<Mission> sortedList = SortMissionsByLevelNumber(loadedMissions);
        AssignMissionsToCategory(difficulty, sortedList);
    }

    private Sprite TryLoadMissionSprite(string basePath, string levelName)
    {
        string[] supportedExtensions = { ".jpg", ".jpeg", ".png" };

        if (string.IsNullOrEmpty(basePath))
        {
            return null;
        }

        if (!Directory.Exists(basePath))
        {
            return null;
        }

        // Make sure levelName is just the filename, without an extension.
        string cleanLevelName = Path.GetFileNameWithoutExtension(levelName);

        string imagePath = Directory.GetFiles(basePath)
            .FirstOrDefault(filePath =>
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                string extension = Path.GetExtension(filePath);

                bool isNameMatch = string.Equals(
                    fileNameWithoutExt,
                    cleanLevelName,
                    StringComparison.OrdinalIgnoreCase
                );

                bool isExtensionSupported = supportedExtensions.Any(
                    ext => string.Equals(
                        ext,
                        extension,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

                return isNameMatch && isExtensionSupported;
            });

        if (string.IsNullOrEmpty(imagePath))
        {
            return null;
        }

        byte[] imageData;

        try
        {
            imageData = File.ReadAllBytes(imagePath);
        }
        catch (Exception e)
        {
            return null;
        }

        Texture2D tex = new Texture2D(
            2,
            2,
            TextureFormat.RGBA32,
            false
        );

        if (!tex.LoadImage(imageData))
        {
            UnityEngine.Object.Destroy(tex);
            return null;
        }

        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    private void ParseMissionData(
    TSObject missionInfo,
    string defaultName,
    Mission mission)
    {
        // ============================================================
        // BASIC MISSION INFORMATION
        // ============================================================

        mission.missionName = Regex.Unescape(defaultName);

        mission.levelName = Regex.Unescape(
            missionInfo.GetField("name") ?? string.Empty
        );

        mission.description = Regex.Unescape(
            missionInfo.GetField("desc") ?? string.Empty
        );

        mission.startHelpText =
            Regex.Unescape(
                missionInfo.GetField("startHelpText") ?? string.Empty
            );

        mission.artist =
            missionInfo.GetField("artist");

        mission.music =
            missionInfo.GetField("music");


        // ============================================================
        // GAME MODES
        // ============================================================

        ParseGameModes(missionInfo, mission);


        // ============================================================
        // LEVEL / TIME
        // ============================================================

        mission.time = ParseIntField(
            missionInfo.GetField("time"),
            -1,
            zeroIsInvalid: true
        );

        // Some files may use "Time" rather than "time".
        if (mission.time == -1)
        {
            mission.time = ParseIntField(
                missionInfo.GetField("Time"),
                -1,
                zeroIsInvalid: true
            );
        }

        mission.levelNumber = ParseIntField(
            missionInfo.GetField("level"),
            0
        );

        // ============================================================
        // SCORE
        // ============================================================

        if(mission.gameModes.Contains(Mode.Hunt) || mission.gameModes.Contains(Mode.Madness))
        {
            mission.parScore = ParseIntField(
                missionInfo.GetField("score"),
                0
            );

            mission.platinumTime = ParseIntField(
                missionInfo.GetField("platinumScore"),
                -1
            );

            mission.ultimateTime = ParseIntField(
                missionInfo.GetField("ultimateScore"),
                -1
            );

            mission.awesomeTime = ParseIntField(
                missionInfo.GetField("awesomeScore"),
                -1
            );
        }

        else
        {
            mission.platinumTime = ParseIntField(
                missionInfo.GetField("platinumTime"),
                -1
            );

            mission.ultimateTime = ParseIntField(
                missionInfo.GetField("ultimateTime"),
                -1
            );

            mission.awesomeTime = ParseIntField(
                missionInfo.GetField("awesomeTime"),
                -1
            );
        }

        // ============================================================
        // HINTS / TEXT
        // ============================================================

        mission.generalHint =
            missionInfo.GetField("generalHint");

        mission.platinumHint =
            missionInfo.GetField("platinumHint");

        mission.ultimateHint =
            missionInfo.GetField("ultimateHint");

        mission.awesomeHint =
            missionInfo.GetField("awesomeHint");

        mission.nestEggHint =
            missionInfo.GetField("eggHint");

        mission.trivia =
            missionInfo.GetField("trivia");


        // ============================================================
        // ALARM
        // ============================================================

        mission.alarmTime = ParseIntField(
            missionInfo.GetField("alarmStartTime"),
            15
        );

        // Legacy/case variant
        if (mission.alarmTime == 15)
        {
            string upperAlarm =
                missionInfo.GetField("AlarmStartTime");

            if (!string.IsNullOrEmpty(upperAlarm))
            {
                mission.alarmTime =
                    ParseIntField(upperAlarm, 15);
            }
        }


        // ============================================================
        // QUOTA
        // ============================================================

        mission.gemQuota = ParseIntField(
            missionInfo.GetField("GemQuota"),
            -1
        );

        if (mission.gemQuota == -1)
        {
            mission.gemQuota = ParseIntField(
                missionInfo.GetField("gemQuota"),
                -1
            );
        }


        // ============================================================
        // LAPS
        // ============================================================

        mission.lapsNumber = ParseIntField(
            missionInfo.GetField("lapsNumber"),
            -1
        );

        mission.noLapsCheckpoint =
            ParseBoolField(
                missionInfo.GetField("noLapsCheckpoint"),
                false
            );


        // ============================================================
        // 2D CAMERA
        // ============================================================

        mission.cameraPlane =
            missionInfo.GetField("cameraPlane");

        mission.invertCameraPlane =
            ParseBoolField(
                missionInfo.GetField("invertCameraPlane"),
                false
            );

        string initialCameraDistance =
            missionInfo.GetField("initialCameraDistance");

        if (!string.IsNullOrEmpty(initialCameraDistance))
        {
            if (float.TryParse(
                initialCameraDistance,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float parsedDistance))
            {
                mission.hasInitialCameraDistance = true;
                mission.initialCameraDistance = parsedDistance;
            }
        }

        string cameraPitch =
            missionInfo.GetField("cameraPitch");

        if (!string.IsNullOrEmpty(cameraPitch))
        {
            if (float.TryParse(
                cameraPitch,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float parsedPitch))
            {
                mission.hasCameraPitch = true;
                mission.cameraPitch = parsedPitch;
            }
        }

        string cameraFov =
            missionInfo.GetField("cameraFov");

        if (!string.IsNullOrEmpty(cameraFov))
        {
            if (float.TryParse(
                cameraFov,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float parsedFov))
            {
                mission.hasCameraFov = true;
                mission.cameraFov = parsedFov;
            }
        }


        // ============================================================
        // CONSISTENCY
        // ============================================================

        mission.minimumSpeed =
            ParseFloatField(
                missionInfo.GetField("MinimumSpeed"),
                mission.minimumSpeed
            );

        if (mission.minimumSpeed == 0f)
        {
            mission.minimumSpeed =
                ParseFloatField(
                    missionInfo.GetField("minimumSpeed"),
                    mission.minimumSpeed
                );
        }

        mission.penaltyDelay =
            ParseFloatField(
                missionInfo.GetField("PenaltyDelay"),
                mission.penaltyDelay
            );

        mission.gracePeriod =
            ParseFloatField(
                missionInfo.GetField("gracePeriod"),
                mission.gracePeriod
            );


        // ============================================================
        // HASTE
        // ============================================================

        mission.speedToQualify =
            ParseFloatField(
                missionInfo.GetField("SpeedToQualify"),
                mission.speedToQualify
            );


        // ============================================================
        // HUNT
        // ============================================================

        mission.maxGemsPerSpawn =
            ParseIntField(
                missionInfo.GetField("maxGemsPerSpawn"),
                mission.maxGemsPerSpawn
            );

        mission.radiusFromGem =
            ParseFloatField(
                missionInfo.GetField("radiusFromGem"),
                mission.radiusFromGem
            );

        mission.spawnBlock =
            ParseFloatField(
                missionInfo.GetField("spawnBlock"),
                mission.spawnBlock
            );

        mission.minPointsPerSpawn =
            ParseIntField(
                missionInfo.GetField("minPointsPerSpawn"),
                mission.minPointsPerSpawn
            );

        mission.minGemsPerSpawn =
            ParseIntField(
                missionInfo.GetField("minGemsPerSpawn"),
                mission.minGemsPerSpawn
            );

        mission.redSpawnChance =
            ParseFloatField(
                missionInfo.GetField("RedSpawnChance"),
                mission.redSpawnChance
            );

        mission.yellowSpawnChance =
            ParseFloatField(
                missionInfo.GetField("yellowSpawnChance"),
                mission.yellowSpawnChance
            );

        mission.blueSpawnChance =
            ParseFloatField(
                missionInfo.GetField("blueSpawnChance"),
                mission.blueSpawnChance
            );

        mission.platinumSpawnChance =
            ParseFloatField(
                missionInfo.GetField("platinumSpawnChance"),
                mission.platinumSpawnChance
            );

        mission.gemGroups =
            ParseIntField(
                missionInfo.GetField("gemGroups"),
                mission.gemGroups
            );


        // ============================================================
        // RADAR
        // ============================================================

        mission.radar =
            missionInfo.GetField("Radar");

        if (string.IsNullOrEmpty(mission.radar))
        {
            mission.radar =
                missionInfo.GetField("radar");
        }

        mission.customRadarRule =
            missionInfo.GetField("CustomRadarRule");

        if (string.IsNullOrEmpty(mission.customRadarRule))
        {
            mission.customRadarRule =
                missionInfo.GetField("customRadarRule");
        }

        mission.forceRadar =
            ParseBoolField(
                missionInfo.GetField("forceRadar"),
                false
            );

        mission.hideRadar =
            ParseBoolField(
                missionInfo.GetField("hideRadar"),
                false
            );


        // ============================================================
        // PHYSICS
        // ============================================================

        mission.gravity =
            ParseFloatField(
                missionInfo.GetField("gravity"),
                mission.gravity
            );

        mission.angularAcceleration =
            ParseFloatField(
                missionInfo.GetField("angularAcceleration"),
                mission.angularAcceleration
            );

        mission.brakingAcceleration =
            ParseFloatField(
                missionInfo.GetField("brakingAcceleration"),
                mission.brakingAcceleration
            );

        mission.maxRollVelocity =
            ParseFloatField(
                missionInfo.GetField("maxRollVelocity"),
                mission.maxRollVelocity
            );

        mission.jumpImpulse =
            ParseFloatField(
                missionInfo.GetField("jumpImpulse"),
                mission.jumpImpulse
            );


        // ============================================================
        // SPECIAL FLAGS
        // ============================================================

        if (mission.missionName.ToLower() == "arkanoid") mission.specialMissionMode = SpecialMissionMode.Arkanoid;
        else if (mission.missionName.ToLower() == "bagofsecrets") mission.specialMissionMode = SpecialMissionMode.BagOfSecrets;
        else if (mission.missionName.ToLower() == "blasttothebeat") mission.specialMissionMode = SpecialMissionMode.BlastToTheBeat;
        else if (mission.missionName.ToLower() == "sacredground") mission.specialMissionMode = SpecialMissionMode.SacredGround;
        else if (mission.missionName.ToLower() == "takethegold") mission.specialMissionMode = SpecialMissionMode.TakeTheGold;
        else if (mission.missionName.ToLower() == "vice") mission.specialMissionMode = SpecialMissionMode.Vice;
        else if (mission.missionName.ToLower() == "whitenoise") mission.specialMissionMode = SpecialMissionMode.WhiteNoise;
        else if (mission.missionName.ToLower() == "minuteminute") mission.specialMissionMode = SpecialMissionMode.MinuteMinute;
        else if (mission.missionName.ToLower() == "arcticinferno") mission.specialMissionMode = SpecialMissionMode.ArcticInferno;
        else if (mission.missionName.ToLower() == "versa") mission.specialMissionMode = SpecialMissionMode.Versa;
        else if (mission.missionName.ToLower() == "unseasonablycold") mission.specialMissionMode = SpecialMissionMode.UnseasonablyCold;
        else mission.specialMissionMode = SpecialMissionMode.None;
    }

    private void ParseGameModes(TSObject missionInfo, Mission mission)
    {
        mission.gameModes.Clear();

        string gameMode = missionInfo.GetField("gameMode");

        if (string.IsNullOrWhiteSpace(gameMode))
        {
            mission.gameModes.Add(Mode.Null);
            return;
        }

        string[] modeTokens = gameMode
            .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in modeTokens)
        {
            switch (token.ToLowerInvariant())
            {
                case "quota":
                    mission.gameModes.Add(Mode.Quota);
                    break;

                case "laps":
                    mission.gameModes.Add(Mode.Laps);
                    break;

                case "consistency":
                    mission.gameModes.Add(Mode.Consistency);
                    break;

                case "haste":
                    mission.gameModes.Add(Mode.Haste);
                    break;

                case "hunt":
                    mission.gameModes.Add(Mode.Hunt);
                    break;

                case "gemmadness":
                    mission.gameModes.Add(Mode.Madness);
                    break;

                case "2d":
                    mission.gameModes.Add(Mode.TwoD);
                    break;

                default:
                    Debug.LogWarning(
                        $"Unknown game mode '{token}' in mission '{mission.levelName}'"
                    );
                    break;
            }
        }

        // If gameMode existed but contained nothing recognized,
        // fall back to Null.
        if (mission.gameModes.Count == 0)
            mission.gameModes.Add(Mode.Null);
    }

    private void ParseMissionObjects(TSObject obj, string misPath, Mission mission)
    {
        if (obj == null) return;

        if (obj.ClassName == "Sky")
        {
            string skyboxPath = ResolvePath(obj.GetField("materialList"), misPath);
            mission.skyboxName = Path.GetFileNameWithoutExtension(skyboxPath);
        }
        else if (obj.ClassName == "Item" && obj.GetField("dataBlock") == "NestEgg_PQ")
        {
            mission.hasEgg = true;
        }

        // Recurse children
        if (obj.Children != null)
        {
            foreach (var child in obj.Children)
            {
                ParseMissionObjects(child, misPath, mission);
            }
        }
    }

    private void AssignMissionsToCategory(Type difficulty, List<Mission> sortedMissions)
    {
        switch (difficulty)
        {
            case Type.tutorial: missionsTutorial = sortedMissions; break;
            case Type.beginner: missionsBeginner = sortedMissions; break;
            case Type.intermediate: missionsIntermediate = sortedMissions; break;
            case Type.advanced: missionsAdvanced = sortedMissions; break;
            case Type.expert: missionsExpert = sortedMissions; break;
            case Type.bonus: missionsBonus = sortedMissions; break;
            case Type.dc: missionsDC = sortedMissions; break;
        }
    }

    // ============================================================
    // HUNT & DEFAULTS
    // ============================================================

    private void ResetHuntMissionDefaults()
    {
        maxGemsPerSpawn = 7;
        radiusFromGem = 15f;
        spawnBlock = 30f;
        minPointsPerSpawn = 5;
        minGemsPerSpawn = 3;
        redSpawnChance = 0.9f;
        yellowSpawnChance = 0.65f;
        blueSpawnChance = 0.35f;
        platinumSpawnChance = 0.18f;
        gemGroups = 0;
    }

    private void ParseHuntMissionInfo(TSObject obj)
    {
        maxGemsPerSpawn = ParseIntField(obj.GetField("maxgemsperspawn"), maxGemsPerSpawn, mustBePositive: true);
        radiusFromGem = ParseFloatField(obj.GetField("radiusfromgem"), radiusFromGem, mustBePositive: true);

        string blockStr = obj.GetField("spawnblock");
        if (float.TryParse(blockStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedBlock) && parsedBlock > 0f)
            spawnBlock = parsedBlock;
        else
            spawnBlock = radiusFromGem * 2f;

        minPointsPerSpawn = ParseIntField(obj.GetField("minpointsperspawn"), minPointsPerSpawn, mustBePositive: true);
        minGemsPerSpawn = ParseIntField(obj.GetField("mingemsperspawn"), minGemsPerSpawn, mustBePositive: true);

        redSpawnChance = ParseFloatField(obj.GetField("RedSpawnChance"), redSpawnChance);
        yellowSpawnChance = ParseFloatField(obj.GetField("yellowSpawnChance"), yellowSpawnChance);
        blueSpawnChance = ParseFloatField(obj.GetField("blueSpawnChance"), blueSpawnChance);
        platinumSpawnChance = ParseFloatField(obj.GetField("platinumSpawnChance"), platinumSpawnChance);
        gemGroups = ParseIntField(obj.GetField("gemGroups"), gemGroups);
    }

    // ============================================================
    // SORT MISSIONS
    // ============================================================

    public static List<Mission> SortMissionsByLevelNumber(List<Mission> missions)
    {
        if (missions == null || missions.Count == 0)
            return new List<Mission>();

        int maxIndex = missions.Count - 1;

        var sortable = missions
            .Where(m => m != null)
            .Select(m => new
            {
                Mission = m,
                NormalizedLevel = Mathf.Clamp(m.levelNumber, 0, maxIndex + 1),
            })
            .ToList();

        sortable.Sort((a, b) =>
        {
            int numCompare = a.NormalizedLevel.CompareTo(b.NormalizedLevel);
            if (numCompare != 0)
                return numCompare;

            return string.Compare(b.Mission.levelName, a.Mission.levelName, StringComparison.OrdinalIgnoreCase);
        });

        List<Mission> result = new List<Mission>();
        foreach (var entry in sortable)
        {
            int targetIndex = Mathf.Clamp(entry.NormalizedLevel, 0, result.Count);
            result.Insert(targetIndex, entry.Mission);
        }

        for (int i = 0; i < result.Count; i++)
        {
            result[i].levelNumber = i + 1;
        }

        return result;
    }

    // ============================================================
    // HELPERS & PATH RESOLUTION
    // ============================================================

    private int ParseIntField(string value, int fallback, bool zeroIsInvalid = false, bool mustBePositive = false)
    {
        if (int.TryParse(value, out int parsed))
        {
            if (zeroIsInvalid && parsed == 0) return fallback;
            if (mustBePositive && parsed <= 0) return fallback;
            return parsed;
        }
        return fallback;
    }

    private float ParseFloatField(string value, float fallback, bool mustBePositive = false)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
        {
            if (mustBePositive && parsed <= 0f) return fallback;
            return parsed;
        }
        return fallback;
    }

    private string ResolvePath(string assetPath, string misPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return assetPath;

        assetPath = assetPath.Replace('\\', '/').TrimEnd('"').TrimStart('/');

        if (assetPath.StartsWith("."))
        {
            string directory = Path.GetDirectoryName(misPath)?.Replace('\\', '/') ?? string.Empty;
            assetPath = $"{directory}/{assetPath.Substring(1)}";
        }
        else
        {
            int slash = assetPath.IndexOf('/');
            assetPath = slash >= 0 ? $"platinum{assetPath.Substring(slash)}" : $"platinum/{assetPath}";
        }

        return assetPath;
    }

    private bool ParseBoolField(string value, bool defaultValue)
    {
        if (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        return defaultValue;
    }
}