using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TS
{
    public class MissionImporter : MonoBehaviour
    {
        public List<TSObject> MissionObjects;

        [Header("Scenery")]
        public SceneryDatabase sceneryObjects;

        [Header("Prefabs")]
        public GameObject interiorPrefab;
        public GameObject movingPlatformPrefab;
        public GameObject triggerGoToTarget;
        public GameObject triggerGotoDelayTargetPrefab;
        public GameObject inBoundsTrigger;
        public GameObject outOfBoundsTrigger;
        public GameObject helpTriggerInstance;
        public GameObject helpBubblePrefab;
        public GameObject teleportTrigger;
        public GameObject destinationTrigger;
        public GameObject gravityWellTrigger;
        public GameObject physModTrigger;
        public GameObject pathTrigger;

        [Header("Extended Trigger Prefabs")]
        public GameObject accelerationTriggerPrefab;
        public GameObject alignmentTriggerPrefab;
        public GameObject alterGravityTriggerPrefab;
        public GameObject cameraDistanceTriggerPrefab;
        public GameObject cameraTriggerPrefab;
        public GameObject cancelVelocityTriggerPrefab;
        public GameObject changeMarbleSizeTriggerPrefab;
        public GameObject checkpointTriggerPrefab;
        public GameObject countdownStartTimerPrefab;
        public GameObject countdownStopTimerPrefab;
        public GameObject disableShapeForceTriggerPrefab;
        public GameObject finishTriggerPrefab;
        public GameObject gemChangeTriggerPrefab;
        public GameObject gravityPointTriggerPrefab;
        public GameObject gravityTriggerPrefab;
        public GameObject lapsCheckpointPrefab;
        public GameObject lapsCounterTriggerPrefab;
        public GameObject lockPowerupTriggerPrefab;
        public GameObject megaManEmulationTriggerPrefab;
        public GameObject multipleTGTTTriggerPrefab;
        public GameObject musicTriggerPrefab;
        public GameObject mustChangeTriggerPrefab;
        public GameObject noMovementKeysTriggerPrefab;
        public GameObject relativeTPTriggerPrefab;
        public GameObject repetitiveTriggerGotoTargetPrefab;
        public GameObject setVelocityTriggerPrefab;
        public GameObject soundTriggerPrefab;
        public GameObject smbTriggerPrefab;
        public GameObject spawnTriggerPrefab;
        public GameObject TDTriggerPrefab;
        public GameObject timeStopTriggerPrefab;
        public GameObject timeTravelTriggerPrefab;
        public GameObject usePowerupTriggerPrefab;

        [Space]
        public GameObject waterPlanePrefab;
        public GameObject waterPlaneSlowPrefab;
        public GameObject waterPhysicsTriggerPrefab;

        [Space]
        public GameObject regularFinishlinesign;
        public GameObject consFinishlinesign;
        public GameObject consFinishlinesignNocrane;
        public GameObject natureFinishlinesignLight;
        public GameObject natureFinishlinesignDark;

        [Space]
        public GameObject fadePlatformPrefab;
        public GameObject fadePlatform2_1x1Prefab;
        public GameObject fadePlatform2_1x2Prefab;
        public GameObject fadePlatform2_1x3Prefab;
        public GameObject fadePlatform2_1x5Prefab;
        public GameObject fadePlatform2_2x2Prefab;
        public GameObject fadePlatform2_3x3Prefab;
        public GameObject fadePlatform2_5x5Prefab;
        public GameObject fadePlatformConcretePrefab;
        public GameObject fadePlatformGrassPrefab;
        public GameObject fadePlatformIcePrefab;

        [Space]
        public GameObject megaManPlatform2_1x1Prefab;
        public GameObject megaManPlatform2_1x2Prefab;
        public GameObject megaManPlatform2_1x3Prefab;
        public GameObject megaManPlatform2_1x5Prefab;
        public GameObject megaManPlatform2_2x2Prefab;
        public GameObject megaManPlatform2_3x3Prefab;
        public GameObject megaManPlatform2_5x5Prefab;

        [Space]
        public GameObject gemPrefab;
        public GameObject gemFancyPrefab;
        public GameObject antiGravityPrefab;
        public GameObject superJumpPrefab;
        public GameObject superSpeedPrefab;
        public GameObject superBouncePrefab;
        public GameObject shockAbsorberPrefab;
        public GameObject gyrocopterPrefab;
        public GameObject timeTravelPrefab;
        public GameObject respawningTimeTravelPrefab;
        public GameObject timePenaltyPrefab;
        public GameObject sundialPrefab;
        public GameObject easterEggPrefab;
        public GameObject bubblePrefab;
        public GameObject fireballPrefab;
        public GameObject anvilPrefab;
        public GameObject teleporterPrefab;
        public GameObject transporterPrefab;

        [Space]
        public GameObject trapdoorPrefab;
        public GameObject roundBumperPrefab;
        public GameObject triangleBumperPrefab;
        public GameObject ductFanPrefab;
        public GameObject tornadoPrefab;
        public GameObject landMinePrefab;
        public GameObject nukePrefab;
        public GameObject iceShardPrefab1;
        public GameObject iceShardPrefab2;
        public GameObject iceSlick;
        public GameObject iceSlick1;
        public GameObject iceSlick2;
        public GameObject iceSlick3;
        public GameObject iceSlick4;

        [Space]
        public GameObject pushButtonRegularPrefab;
        public GameObject pushButtonFlatPrefab;
        public GameObject pushButtonExtendedPrefab;
        public GameObject pushButtonFlatHalfPrefab;

        [Space]
        public GameObject cannonPrefab;
        public GameObject cannonTarget;
        public GameObject physModEmitterPrefab;

        [Space]
        public GameObject checkpointConPrefab;
        public GameObject checkpointPrefab;

        [Space]
        public GameObject propellerPrefab;
        public GameObject propellerLarge1Prefab;
        public GameObject propellerLarge2Prefab;
        public GameObject propellerLarge3Prefab;
        public GameObject propellerLarge4Prefab;
        public GameObject propellerLarge5Prefab;
        public GameObject propellerSmall1Prefab;
        public GameObject propellerSmall2Prefab;
        public GameObject propellerSmall3Prefab;
        public GameObject propellerSmall4Prefab;
        public GameObject propellerSmall5Prefab;
        public GameObject propellerLargeReverse1Prefab;
        public GameObject propellerLargeReverse2Prefab;
        public GameObject propellerLargeReverse3Prefab;
        public GameObject propellerLargeReverse4Prefab;
        public GameObject propellerLargeReverse5Prefab;
        public GameObject propellerSmallReverse1Prefab;
        public GameObject propellerSmallReverse2Prefab;
        public GameObject propellerSmallReverse3Prefab;
        public GameObject propellerSmallReverse4Prefab;
        public GameObject propellerSmallReverse5Prefab;

        [Space]
        public GameObject[] staticShapes;

        [Space]
        public GameObject finishPadPrefab;
        public GameObject finishPadConstructionPrefab;

        [Header("References")]
        public GameObject globalMarble;
        public GameObject startPad;
        public GameObject finishPad;
        public Light directionalLight;

        private readonly List<GameObject> checkpoints = new List<GameObject>();
        private readonly List<GameObject> destinationTriggers = new List<GameObject>();
        private readonly List<GameObject> teleportTriggers = new List<GameObject>();
        private float[] sunColor;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private PathManager pathManager;
        public PathManager PathManager => pathManager;
        private PathMovementManager movementManager;

        private readonly Dictionary<string, GameObject> importedObjects = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        [Serializable]
        private class PendingParent
        {
            public string parentName;
            public string parentModTrans;
            public string parentTransform;
            public string parentOffset;
            public bool parentSimple;
            public bool parentNoRot;
            public string dataBlock;
            public Quaternion additionalRotation;
            public bool offsetRotate = false;
        }

        private readonly Dictionary<GameObject, PendingParent> pendingParents = new Dictionary<GameObject, PendingParent>();

        [Serializable]
        private class PendingPathTriggerEntry
        {
            public PathTrigger trigger;
            public string objectName;
            public string pathName;
        }

        [Serializable]
        private class PendingMultipleTGTTEntry
        {
            public MultipleTGTT trigger;
        }

        [Serializable]
        private class PendingPathEntry
        {
            public GameObject gameObject;
            public string pathName;
        }

        [Serializable]
        private class PendingCheckpointTriggerEntry
        {
            public TSObject missionObject;
            public string respawnPoint;
        }

        private readonly List<PendingPathTriggerEntry> pendingPathTriggerEntries = new List<PendingPathTriggerEntry>();
        private readonly List<PendingMultipleTGTTEntry> pendingMultipleTGTTEntries = new List<PendingMultipleTGTTEntry>();
        private readonly List<PendingPathEntry> pendingPathEntries = new List<PendingPathEntry>();
        private readonly List<PendingCheckpointTriggerEntry> pendingCheckpointTriggerEntries = new List<PendingCheckpointTriggerEntry>();

        private GameObject finishSign;

        private readonly Dictionary<TSObject, GameObject> importedTSObjects =
    new Dictionary<TSObject, GameObject>();

        private void Start()
        {
            ImportMission();
        }

        private void LateUpdate()
        {
            if (finishSign != null)
                finishSign.transform.LookAt(finishPad.transform);
        }

        private void ImportMission()
        {
            if (string.IsNullOrEmpty(MissionInfo.instance.MissionPath))
                return;

            string path = Path.Combine(Application.streamingAssetsPath, MissionInfo.instance.MissionPath);
            McsFile mcs = McsParser.Parse(path);
            if (mcs == null)
            {
                Debug.LogError("Could not parse mission file");
                return;
            }

            GameObject pathNodeGroup = new GameObject("PathNodeGroup");
            pathNodeGroup.transform.SetParent(transform, false);

            pathManager = pathManager ?? gameObject.AddComponent<PathManager>();
            pathManager.Clear();

            importedObjects.Clear();
            importedTSObjects.Clear();

            pendingParents.Clear();
            pendingPathEntries.Clear();
            pendingPathTriggerEntries.Clear();
            pendingMultipleTGTTEntries.Clear();
            pendingCheckpointTriggerEntries.Clear();

            checkpoints.Clear();
            destinationTriggers.Clear();
            teleportTriggers.Clear();

            movementManager = movementManager ?? gameObject.AddComponent<PathMovementManager>();

            MissionObjects = mcs.MissionObjects;
            if (MissionObjects.Count == 0)
                return;

            var mission = MissionObjects[0];
            finishSign = null;

            foreach (var obj in mission.RecursiveChildren())
            {
                switch (obj.ClassName)
                {
                    case "Sun":
                        ImportSun(obj);
                        break;
                    case "Item":
                        ImportItem(obj);
                        break;
                    case "InteriorInstance":
                        ImportInteriorInstance(obj);
                        break;
                    case "StaticShape":
                        ImportStaticShape(obj, pathNodeGroup.transform);
                        break;
                    case "TSStatic":
                        ImportTSStatic(obj);
                        break;
                    case "Trigger":
                        ImportTrigger(obj);
                        break;
                    case "SimGroup":
                        ImportSimGroup(obj);
                        break;
                }
            }

            TSObject helpBubbleGroup = mission.RecursiveChildren()
                .FirstOrDefault(x => x.ClassName == "SimGroup" && x.Name == "HelpBubbleGroup");

            if (helpBubbleGroup != null)
            {
                GameObject groupObject = new GameObject("HelpBubbleGroup");
                groupObject.transform.SetParent(transform, false);

                foreach (TSObject obj in helpBubbleGroup.GetFirstChildrens()
                    .Where(o => o.ClassName == "StaticShape" && o.GetField("dataBlock").Equals("HelpBubble", StringComparison.OrdinalIgnoreCase)))
                {
                    ImportHelpBubble(obj, groupObject.transform);
                }
            }

            ResolvePaths();
            pathManager.LogNodes();
            ResolveParenting();
            ResolvePathTriggers();
            ResolveMultipleTGTTReferences();
            ResolveCheckpointTriggers();
            ResolveTeleporters();
            ResolveRelativeTeleporters();

            InitializeSpecialMissionMode();

            StartCoroutine(DelayBeforeRespawn());
        }

        #region Import Handlers

        private void ImportSun(TSObject obj)
        {
            var direction = ConvertDirection(ParseVectorString(obj.GetField("direction")));
            sunColor = ParseVectorString(obj.GetField("color"));
            var ambient = ParseVectorString(obj.GetField("ambient"));

            directionalLight.transform.localRotation = direction;
            directionalLight.color = new Color(sunColor[0], sunColor[1], sunColor[2], 1f);
            RenderSettings.ambientLight = new Color(ambient[0], ambient[1], ambient[2], 1f);
        }

        private void ImportItem(TSObject obj)
        {
            string objectName = obj.GetField("dataBlock");

            if (objectName.StartsWith("GemItem", StringComparison.OrdinalIgnoreCase) ||
    objectName.StartsWith("FancyGemItem", StringComparison.OrdinalIgnoreCase))
            {
                bool isFancy =
                    objectName.StartsWith(
                        "FancyGemItem",
                        StringComparison.OrdinalIgnoreCase
                    );

                var gobj = Instantiate(
                    isFancy ? gemFancyPrefab : gemPrefab,
                    transform,
                    false
                );

                gobj.name = isFancy ? "FancyGem" : "Gem";

                SetTransforms(
                    gobj,
                    obj,
                    Quaternion.identity
                );

                Gem gem = gobj.GetComponent<Gem>();

                if (gem != null)
                {
                    // Gem color can be specified either through:
                    //
                    //   GemItemBlue_PQ
                    //
                    // or:
                    //
                    //   FancyGemItem_PQ
                    //   skin = "blue"
                    //
                    // Prefer the explicit skin field.
                    string gemColor = obj.GetField("skin");

                    if (string.IsNullOrWhiteSpace(gemColor))
                    {
                        gemColor = ExtractGemColor(objectName);
                    }

                    gem.SetGemColor(gemColor);
                }

                RegisterImportedObject(
                    obj,
                    gobj,
                    Quaternion.identity
                );

                CheckForPath(obj, gobj);
            }
            else if (IsPowerup(objectName, out GameObject prefab, out string defaultName))
            {
                var gobj = Instantiate(prefab, transform, false);
                gobj.name = string.IsNullOrEmpty(obj.Name) ? defaultName : obj.Name;
                bool isAntiGravity = objectName == "AntiGravityItem_PQ" || objectName == "NoRespawnAntiGravityItem_PQ";

                Vector3 position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                Quaternion rotation = isAntiGravity
                    ? ConvertRotation(ParseVectorString(obj.GetField("rotation")), true)
                    : ConvertRotationPowerups(ParseVectorString(obj.GetField("rotation")));
                Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                string showInfo = obj.GetField("showHelpOnPickup");
                if (!string.IsNullOrEmpty(showInfo) && gobj.TryGetComponent<Powerups>(out var powerup))
                    powerup.showHelpOnPickup = int.Parse(showInfo) == 1;

                gobj.transform.localPosition = position;
                gobj.transform.localRotation = rotation;
                gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);

                if (objectName == "BubbleItem")
                {
                    var bubble = gobj.GetComponent<Bubble>();
                    bubble.infinite = ParseBoolField(obj, "Infinite", false);
                    bubble.duration = ParseFloatField(obj, "Time", 5000f) / 1000f;
                }
                else if (objectName.ToLower() == "fireballitem")
                {
                    var fireball = gobj.GetComponent<Fireball>();
                    fireball.activeTime = ParseFloatField(obj, "activeTime", 5000f) / 1000f;
                }
                else if (objectName == "TeleportItem")
                {
                    var teleport = gobj.GetComponent<Teleporter>();
                    teleport.teleportDelay = ParseFloatField(obj, "teletime", 2000f) / 1000f;
                }
                else if (objectName == "SuperJumpItem_PQ" || objectName == "CustomSuperJumpItem_PQ")
                {
                    var superJump = gobj.GetComponent<SuperJump>();
                    superJump.rotate = ParseBoolField(obj, "rotate", true);
                    superJump.superJumpHeight = ParseFloatField(obj, "power", 20f);
                }
                else if (objectName == "SuperSpeedItem_PQ")
                {
                    var superSpeed = gobj.GetComponent<SuperSpeed>();
                    superSpeed.rotate = ParseBoolField(obj, "rotate", true);
                }

                Quaternion additionalRot = isAntiGravity ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
                RegisterImportedObject(obj, gobj, additionalRot);
                CheckForPath(obj, gobj);
            }
            else if (objectName == "TimeTravelItem_PQ" || objectName == "SundialItem_PQ" || objectName == "TimePenaltyItem_PQ" || objectName == "RespawningTimeTravelItem_PQ")
            {
                bool isPenalty = objectName == "TimePenaltyItem_PQ";

                var prefabToUse = isPenalty ? timePenaltyPrefab : (objectName == "SundialItem_PQ" ? sundialPrefab : timeTravelPrefab);

                bool isRespawning = !string.IsNullOrEmpty(obj.GetField("respawnTime"));
                if (isRespawning)
                {
                    prefabToUse = respawningTimeTravelPrefab;
                }

                var gobj = Instantiate(prefabToUse, transform, false);
                gobj.name = string.IsNullOrEmpty(obj.Name) ? "TimeTravelItem" : obj.Name;
                SetTransformsPowerup(gobj, obj);

                string timeBonus = obj.GetField(isPenalty ? "timePenalty" : "timeBonus");
                float defaultBonus = isPenalty ? -5f : 5f;
                float multiplier = isPenalty ? -1f : 1f;

                gobj.GetComponent<TimeTravel>().timeBonus = !string.IsNullOrEmpty(timeBonus)
                    ? (multiplier * float.Parse(timeBonus)) / 1000f
                    : defaultBonus;

                string noRotStr = obj.GetField("additionalRotation");
                bool offsetRotate = noRotStr == "1" || noRotStr.Equals("true", StringComparison.OrdinalIgnoreCase);

                if (isRespawning)
                {
                    var maxRespawn = obj.GetField("maxRespawns");
                    gobj.GetComponent<TimeTravel>().maxRespawns = string.IsNullOrEmpty(maxRespawn) ? int.MaxValue : int.Parse(maxRespawn);
                    gobj.GetComponent<TimeTravel>().respawnTime = float.Parse(obj.GetField("respawnTime")) / 1000f;
                }

                RegisterImportedObject(obj, gobj, Quaternion.identity, offsetRotate);
                CheckForPath(obj, gobj);
            }
            else if (objectName == "NestEgg_PQ")
            {
                var gobj = Instantiate(easterEggPrefab, transform, false);
                gobj.name = "NestEgg";
                SetTransformsPowerup(gobj, obj);
                ApplySkins(gobj, obj.GetField("skin"), false);

                RegisterImportedObject(obj, gobj, Quaternion.identity);
                CheckForPath(obj, gobj);
            }
        }

        private void ImportInteriorInstance(TSObject obj)
        {
            var gobj = Instantiate(interiorPrefab, transform, false);
            gobj.name = string.IsNullOrEmpty(obj.Name) ? "InteriorInstance" : obj.Name;
            Vector3 scale = SetTransforms(gobj, obj, Quaternion.identity);

            var dif = gobj.GetComponent<Dif>();
            dif.filePath = ResolvePath(obj.GetField("interiorFile"), MissionInfo.instance.MissionPath);

            if (!dif.GenerateMesh(-1))
                Destroy(gobj.gameObject);

            if (scale.x == 0 || scale.y == 0 || scale.z == 0)
            {
                foreach (var mc in gobj.GetComponentsInChildren<MeshCollider>(true))
                    Destroy(mc);
            }

            RegisterImportedObject(obj, gobj, Quaternion.Euler(90f, 0f, 0f));
            CheckForPath(obj, gobj);
        }

        private void ImportStaticShape(TSObject obj, Transform pathNodeParent)
        {
            string objectName = obj.GetField("dataBlock");

            if (objectName == "PhysModEmitterBase")
            {
                GameObject emitter = Instantiate(physModEmitterPrefab, transform, false);
                emitter.name = !string.IsNullOrEmpty(obj.Name) ? obj.Name : "PhysModEmitterBase";
                emitter.transform.position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                emitter.transform.rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation"))) * Quaternion.Euler(90f, 0f, 0f);
                emitter.transform.localScale = Vector3.Scale(Vector3.one, ConvertScale(ParseVectorString(obj.GetField("scale"))));

                //jangan lupa parse noParticles (bool)

                RegisterImportedObject(obj, emitter, Quaternion.Euler(-90f, 0f, 0f));
                CheckForPath(obj, emitter);
            }
            else if (IsPushButtonDataBlock(objectName, out GameObject buttonPrefab, out bool isToggle))
            {
                ImportPushButton(obj, buttonPrefab, isToggle);
            }
            else if (objectName == "StartPad" || objectName == "StartPad_PQ" || objectName == "StartPad_PQ_Construction")
            {
                Vector3 position = ConvertPoint(ParseVectorString(obj.GetField("position")));
                Quaternion rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                var sp = startPad.GetComponent<StartPad>();
                sp.SetMeshRegular(objectName == "StartPad" || objectName == "StartPad_PQ");

                Transform spMesh = startPad.transform.Find("Mesh");
                Transform forwardPoint = spMesh.Find("Forward");

                startPad.transform.localPosition = position;
                startPad.transform.localRotation = rotation;

                spMesh.transform.parent = null;
                spMesh.transform.localRotation = rotation;
                spMesh.localScale = Vector3.Scale(scale, spMesh.localScale);

                startPad.transform.LookAt(forwardPoint);
                startPad.transform.localRotation = Quaternion.Euler(-90, startPad.transform.localRotation.eulerAngles.y, startPad.transform.localRotation.eulerAngles.z);

                ApplySkins(spMesh.gameObject, obj.GetField("skin"), false);
                RegisterImportedObject(obj, startPad, Quaternion.Euler(-90f, 0f, 0f));
            }
            else if (objectName == "EndPad_PQ")
            {
                var gobj = Instantiate(finishPadPrefab, transform, false);

                gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
                gobj.transform.localRotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
                gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);

                GameManager.instance.finishPad = gobj;
                finishPad = gobj;

                ApplySkins(gobj, obj.GetField("skin"), false);

                RegisterImportedObject(obj, gobj, Quaternion.identity);
                CheckForPath(obj, gobj);
            }
            else if (objectName == "EndPad_PQ_Construction")
            {
                var gobj = Instantiate(finishPadConstructionPrefab, transform, false);

                gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
                gobj.transform.localRotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
                gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);

                GameManager.instance.finishPad = gobj;
                finishPad = gobj;

                ApplySkins(gobj, obj.GetField("skin"), false);

                RegisterImportedObject(obj, gobj, Quaternion.identity);
                CheckForPath(obj, gobj);
            }
            else if (objectName.Equals("checkpoint_pq", StringComparison.OrdinalIgnoreCase))
            {
                ImportCheckpoint(obj);
            }
            else if (IsFinishSign(objectName, out GameObject signPrefab))
            {
                var gobj = Instantiate(signPrefab, transform, false);
                gobj.name = "SignFinish";
                gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));

                var fake = obj.GetField("fake");
                if (objectName == "RegularFinishlinesign" &&
                    (string.IsNullOrEmpty(fake) || !ParseBoolean(fake)))
                    finishSign = gobj;
                else
                    gobj.transform.localRotation = gobj.transform.localRotation * ConvertRotation(ParseVectorString(obj.GetField("rotation"))) * Quaternion.Euler(90f, 0f, 0f);

                Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
                gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);

                RegisterImportedObject(obj, gobj, Quaternion.identity);
                CheckForPath(obj, gobj);
            }
            else if (objectName == "WaterPlane")
            {
                if (waterPlanePrefab == null) return;
                GameObject gobj = Instantiate(waterPlanePrefab, transform, false);
                gobj.name = string.IsNullOrEmpty(obj.Name) ? "WaterPlane" : obj.Name;
                gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
                gobj.transform.localRotation = gobj.transform.localRotation * ConvertRotation(ParseVectorString(obj.GetField("rotation"))) * Quaternion.Euler(90f, 0f, 0f);
                Vector3 scale = ConvertScaleXZY(ParseVectorString(obj.GetField("scale")));
                gobj.transform.localScale = Vector3.Scale(Vector3.Scale(scale, gobj.transform.localScale), new Vector3(3f, 0.0001f, 3f));

                RegisterImportedObject(obj, gobj, Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(90f, 0f, 0f));
                CheckForPath(obj, gobj);
            }
            else if (objectName == "WaterCylinder_slow")
            {
                GameObject gobj = Instantiate(waterPlaneSlowPrefab, transform, false);
                gobj.name = string.IsNullOrEmpty(obj.Name) ? "WaterCylinder_slow" : obj.Name;
                gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
                gobj.transform.localRotation = gobj.transform.localRotation * ConvertRotation(ParseVectorString(obj.GetField("rotation"))) * Quaternion.Euler(90f, 0f, 0f);
                Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
                gobj.transform.localScale = new Vector3(scale.x * gobj.transform.localScale.x, scale.z * gobj.transform.localScale.y, scale.y * gobj.transform.localScale.z);

                RegisterImportedObject(obj, gobj, Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(90f, 0f, 0f));
                CheckForPath(obj, gobj);
            }
            else if (objectName == "IceShard1" || objectName == "IceShard2" || objectName == "PointsIceShard2" || objectName == "PointsIceShard1")
            {
                GameObject prefab = (objectName == "IceShard1" || objectName == "PointsIceShard1") ? iceShardPrefab1 : iceShardPrefab2;
                var gobj = Instantiate(prefab, transform, false);

                gobj.name = string.IsNullOrEmpty(obj.Name) ? objectName : obj.Name;

                gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
                gobj.transform.localRotation = ConvertRotation(ParseVectorString(obj.GetField("rotation"))) * Quaternion.Euler(90f, 0f, 0f);
                gobj.transform.localScale = ConvertScale(ParseVectorString(obj.GetField("scale")));

                var skin = obj.GetField("skin");

                if (!string.IsNullOrEmpty(skin))
                    gobj.GetComponent<IceShard>().SetSkin(skin);

                ApplySkins(gobj, obj.GetField("skin"));
                RegisterImportedObject(obj, gobj, Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(90f, 0f, 0f));
                CheckForPath(obj, gobj);
            }
            else if (IsHazard(objectName, out GameObject hazardPrefab, out string hazardName))
            {
                var gobj = Instantiate(hazardPrefab, transform, false);
                gobj.name = hazardName;
                gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
                gobj.transform.localRotation = ConvertRotation(ParseVectorString(obj.GetField("rotation"))) * Quaternion.Euler(90f, 0f, 0f);
                Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
                gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);

                var ductFan = gobj.GetComponent<DuctFan>();
                if(hazardName == "SmallDuctFan")
                {
                    ductFan.radius = 5f;
                    ductFan.strength = 10f;
                    ductFan.arc = 0.7f;
                }
                else if(hazardName == "VVDuctFan")
                {
                    ductFan.radius = 9f;
                    ductFan.strength = 40f * 0.8f;
                    ductFan.arc = 0.7f;
                }

                RegisterImportedObject(obj, gobj, Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(90f, 0f, 0f));
                CheckForPath(obj, gobj);
            }
            else if (IsCannonDataBlock(objectName))
            {
                ImportCannon(obj, objectName);
            }
            else if (objectName.Equals("target", StringComparison.OrdinalIgnoreCase))
            {
                GameObject gobj = Instantiate(cannonTarget, transform, false);
                gobj.name = string.IsNullOrEmpty(obj.Name) ? "Target" : obj.Name;
                gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
                gobj.transform.localRotation *= ConvertRotation(ParseVectorString(obj.GetField("rotation")));
                Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
                gobj.transform.localScale = new Vector3(scale.x * gobj.transform.localScale.x, scale.y * gobj.transform.localScale.z, scale.z * gobj.transform.localScale.y);

                ApplySkins(gobj, obj.GetField("skin"));
                RegisterImportedObject(obj, gobj, Quaternion.Euler(-90f, 0f, 0f));
                CheckForPath(obj, gobj);
            }
            else if (objectName.StartsWith("FadePlat", StringComparison.OrdinalIgnoreCase))
            {
                ImportFadePlatform(obj, objectName);
            }
            else if (objectName.StartsWith("MegaManPlatform2_", StringComparison.OrdinalIgnoreCase))
            {
                ImportMegaManPlatform(obj, objectName);
            }
            else if (IsPropeller(objectName, out GameObject propPrefab))
            {
                if (propPrefab == null) return;
                GameObject gobj = Instantiate(propPrefab, transform, false);
                gobj.name = objectName;
                gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
                gobj.transform.localRotation = ConvertRotation(ParseVectorString(obj.GetField("rotation"))) * Quaternion.Euler(90f, 0f, 0f);
                Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
                gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);

                RegisterImportedObject(obj, gobj, Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(90f, 0f, 0f));
                CheckForPath(obj, gobj);
            }
            else if (PathNodeParser.IsPathNode(obj))
            {
                ImportPathNode(obj, pathNodeParent, pathManager);
            }
            else
            {
                ImportSceneryObject(obj);
            }
        }

        private void ImportTSStatic(TSObject obj)
        {
            string objectName = Path.GetFileNameWithoutExtension(obj.GetField("shapeName"));

            if (objectName.Equals("checkpoint", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(obj.Name))
            {
                ImportCheckpoint(obj);
            }
            else
            {
                var shape = staticShapes.FirstOrDefault(go => go != null && go.name.Equals(objectName, StringComparison.OrdinalIgnoreCase));
                if (shape != null)
                {
                    var gobj = Instantiate(shape, transform, false);
                    gobj.name = objectName;
                    gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
                    gobj.transform.localRotation = gobj.transform.localRotation * ConvertRotation(ParseVectorString(obj.GetField("rotation"))) * Quaternion.Euler(90f, 0f, 0f);
                    Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
                    gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);

                    if (objectName.ToLower() == "endpad")
                    {
                        GameManager.instance.finishPad = gobj;
                        finishPad = gobj;
                    }

                    RegisterImportedObject(obj, gobj, Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(90f, 0f, 0f));
                    CheckForPath(obj, gobj);
                }
                else
                {
                    ImportSceneryObject(obj);
                }
            }
        }

        private void ImportTrigger(TSObject obj)
        {
            string objectName = obj.GetField("dataBlock");

            if (objectName == "InBoundsTrigger" || objectName == "OutOfBoundsTrigger" || objectName == "HelpTrigger")
            {
                GameObject prefab = objectName switch
                {
                    "InBoundsTrigger" => inBoundsTrigger,
                    "OutOfBoundsTrigger" => outOfBoundsTrigger,
                    _ => helpTriggerInstance,
                };

                var triggerObj = Instantiate(prefab, transform, false);
                triggerObj.name = obj.Name;

                if (objectName == "HelpTrigger")
                    triggerObj.GetComponent<HelpTrigger>().helpText = obj.GetField("text");

                SetupTriggerTransform(triggerObj, obj);
                RegisterImportedObject(obj, triggerObj, Quaternion.Euler(-90f, 0f, 0f));
                CheckForPath(obj, triggerObj);
            }
            else if (objectName == "TeleportTrigger" || objectName == "DestinationTrigger")
            {
                ImportTeleportOrDestinationTrigger(obj, objectName);
            }
            else if (objectName == "GravityWellTrigger" || objectName == "GravityWell")
            {
                var gwObj = Instantiate(gravityWellTrigger, transform, false);
                gwObj.name = string.IsNullOrEmpty(obj.Name) ? objectName : obj.Name;

                var gravityWell = gwObj.GetComponent<GravityWellTrigger>();
                if (gravityWell != null)
                {
                    gravityWell.axis = obj.GetField("axis");
                    gravityWell.invert = obj.GetField("invert") == "1";
                    string customPointStr = obj.GetField("custompoint");
                    if (!string.IsNullOrEmpty(customPointStr))
                    {
                        gravityWell.customPoint = ConvertPoint(ParseVectorString(customPointStr));
                        gravityWell.hasCustomPoint = true;
                    }
                    gravityWell.restoreGravity = obj.GetField("RestoreGravity");
                }

                SetupTriggerTransform(gwObj, obj);
                RegisterImportedObject(obj, gwObj, Quaternion.Euler(-90f, 0f, 0f));
                CheckForPath(obj, gwObj);
            }
            else if (objectName == "WaterPhysicsTrigger")
            {
                GameObject gobj = Instantiate(waterPhysicsTriggerPrefab, transform, false);
                gobj.name = string.IsNullOrEmpty(obj.Name) ? "WaterPhysicsTrigger" : obj.Name;

                SetupTriggerTransform(gobj, obj);
                WaterPhysicsTrigger waterTrigger = gobj.GetComponentInChildren<WaterPhysicsTrigger>(true);
                if (waterTrigger != null)
                {
                    waterTrigger.velocityMultiplier = ParseFloatField(obj, "VelocityMultiplier", 0.5f);
                }

                RegisterImportedObject(obj, gobj, Quaternion.Euler(-90f, 0f, 0f));
                CheckForPath(obj, gobj);
            }
            else if (objectName == "MarblePhysModTrigger" || objectName == "PhysModTrigger")
            {
                ImportPhysModTrigger(obj);
            }
            else if (objectName.Equals("PathTrigger", StringComparison.OrdinalIgnoreCase))
            {
                ImportPathTrigger(obj);
            }
            else if (objectName.Equals("LapsCheckpoint", StringComparison.OrdinalIgnoreCase))
            {
                ImportLapsCheckpoint(obj);
            }
            else if (objectName.Equals("LapsCounterTrigger", StringComparison.OrdinalIgnoreCase))
            {
                ImportLapsCounterTrigger(obj);
            }
            else if (IsExtendedTriggerDataBlock(objectName, out GameObject extTriggerPrefab))
            {
                ImportExtendedTrigger(obj, objectName, extTriggerPrefab);
            }
        }

        private void ImportSimGroup(TSObject obj)
        {
            // A SimGroup can contain multiple PathedInteriors that share
            // the same path and TriggerGotoTarget triggers.
            List<TSObject> pathedInteriors =
                obj.GetFirstChildrens()
                    .Where(o => string.Equals(
                        o.ClassName,
                        "PathedInterior",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (pathedInteriors.Count == 0)
                return;

            List<TSObject> markers =
                obj.RecursiveChildren()
                    .Where(o => string.Equals(
                        o.ClassName,
                        "Marker",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

            List<SmoothingType> smoothingTypes =
                new List<SmoothingType>();

            List<SequenceNumber> sharedSequence =
                new List<SequenceNumber>();

            for (int i = 0; i < markers.Count; i++)
            {
                TSObject marker = markers[i];

                Vector3 pos =
                    ConvertPoint(
                        ParseVectorString(
                            marker.GetField("position")));

                int msToNext =
                    ParseIntField(
                        marker,
                        "msToNext",
                        0);

                SmoothingType smoothingType =
                    SmoothingType.Linear;

                string smoothing =
                    marker.GetField("smoothingType");

                if (!string.IsNullOrEmpty(smoothing) &&
                    Enum.TryParse(
                        smoothing,
                        true,
                        out SmoothingType parsedSmoothing))
                {
                    smoothingType = parsedSmoothing;
                }

                smoothingTypes.Add(smoothingType);

                GameObject markerInstance =
                    new GameObject(
                        $"Marker Interior ({i})");

                markerInstance.transform.SetParent(
                    transform,
                    false);

                markerInstance.transform.position = pos;

                sharedSequence.Add(
                    new SequenceNumber
                    {
                        marker = markerInstance,
                        secondsToNext = msToNext / 1000f,
                        smoothing = smoothingType
                    });
            }

            List<MovingPlatform> movingPlatforms =
                new List<MovingPlatform>();

            foreach (TSObject pathedInterior in pathedInteriors)
            {
                GameObject gobj =
                    Instantiate(
                        movingPlatformPrefab,
                        transform,
                        false);

                gobj.name =
                    string.IsNullOrEmpty(pathedInterior.Name)
                        ? "PathedInterior"
                        : pathedInterior.Name;

                gobj.transform.localPosition =
                    ConvertPoint(
                        ParseVectorString(
                            pathedInterior.GetField(
                                "basePosition")));

                gobj.transform.localRotation =
                    ConvertRotation(
                        ParseVectorString(
                            pathedInterior.GetField(
                                "baseRotation")));

                gobj.transform.localScale =
                    ConvertScale(
                        ParseVectorString(
                            pathedInterior.GetField(
                                "baseScale")));

                Dif dif = gobj.GetComponent<Dif>();

                if (dif == null)
                {
                    Debug.LogError(
                        $"MovingPlatform prefab is missing Dif " +
                        $"for '{pathedInterior.Name}'.");
                    Destroy(gobj);
                    continue;
                }

                dif.filePath =
                    ResolvePath(
                        pathedInterior.GetField(
                            "interiorResource"),
                        MissionInfo.instance.MissionPath);

                if (!int.TryParse(
                        pathedInterior.GetField("interiorIndex"),
                        NumberStyles.Integer,
                        Invariant,
                        out int indexStr))
                {
                    Debug.LogError(
                        $"Invalid interiorIndex for " +
                        $"'{pathedInterior.Name}'.");
                    Destroy(gobj);
                    continue;
                }

                dif.GenerateMesh(indexStr);

                MovingPlatform movingPlatform =
                    gobj.GetComponent<MovingPlatform>();

                if (movingPlatform == null)
                {
                    Debug.LogError(
                        $"MovingPlatform prefab is missing " +
                        $"MovingPlatform for '{pathedInterior.Name}'.");
                    Destroy(gobj);
                    continue;
                }

                movingPlatform.initialPosition =
                    ParseIntField(
                        pathedInterior,
                        "initialPosition",
                        0) / 1000f;

                string initialTargetPosition =
                    pathedInterior.GetField(
                        "initialTargetPosition");

                if (!string.IsNullOrEmpty(initialTargetPosition) &&
                    int.TryParse(
                        initialTargetPosition,
                        NumberStyles.Integer,
                        Invariant,
                        out int itp))
                {
                    movingPlatform.initialTargetPosition =
                        itp >= 0
                            ? itp / 1000f
                            : itp;

                    movingPlatform.movementMode =
                        itp >= 0
                            ? MovementMode.Triggered
                            : MovementMode.Constant;
                }
                else
                {
                    movingPlatform.initialTargetPosition = 0f;
                    movingPlatform.movementMode =
                        MovementMode.Triggered;
                }

                movingPlatform.delayTargetTime =
                    ParseIntField(
                        pathedInterior,
                        "delayTargetTime",
                        0) / 1000f;

                movingPlatform.sequenceNumbers =
                    CloneSequenceNumbers(sharedSequence);

                movingPlatform.SetTriggerControlled(false);
                movingPlatform.InitMovingPlatform();

                if (MissionInfo.instance.specialMissionMode ==
                    SpecialMissionMode.MinuteMinute)
                {
                    if (string.Equals(
                            gobj.name,
                            "BlueDoor",
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            gobj.name,
                            "EggDoor",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        movingPlatform.GoToTime(0f);
                    }
                }

                RegisterImportedObject(
                    pathedInterior,
                    gobj,
                    Quaternion.Euler(-90f, 0f, 0f));

                CheckForPath(
                    pathedInterior,
                    gobj);

                movingPlatforms.Add(movingPlatform);
            }

            // One TriggerGotoTarget controls every PathedInterior
            // contained by this SimGroup.
            foreach (TSObject trigger in
                obj.GetFirstChildrens()
                    .Where(o => string.Equals(
                        o.ClassName,
                        "Trigger",
                        StringComparison.OrdinalIgnoreCase)))
            {
                ImportPathedInteriorTrigger(
                    trigger,
                    movingPlatforms);
            }
        }

        private int ParseIntField(
            TSObject obj,
            string fieldName,
            int defaultValue)
        {
            string value = obj.GetField(fieldName);

            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            return int.TryParse(
                value,
                NumberStyles.Integer,
                Invariant,
                out int result)
                ? result
                : defaultValue;
        }

        private SequenceNumber[] CloneSequenceNumbers(
            List<SequenceNumber> source)
        {
            SequenceNumber[] result =
                new SequenceNumber[source.Count];

            for (int i = 0; i < source.Count; i++)
            {
                result[i] = new SequenceNumber
                {
                    marker = source[i].marker,
                    markerPos = source[i].markerPos,
                    secondsToNext = source[i].secondsToNext,
                    smoothing = source[i].smoothing
                };
            }

            return result;
        }

        private void ImportPathedInteriorTrigger(
            TSObject trigger,
            List<MovingPlatform> movingPlatforms)
        {
            string dataBlock = trigger.GetField("dataBlock");

            if (string.Equals(
                    dataBlock,
                    "TriggerGotoTarget",
                    StringComparison.OrdinalIgnoreCase))
            {
                string targetTime =
                    trigger.GetField("targetTime");

                if (string.IsNullOrEmpty(targetTime))
                    return;

                GameObject tgttObj =
                    Instantiate(
                        triggerGoToTarget,
                        transform,
                        false);

                tgttObj.name =
                    string.IsNullOrEmpty(trigger.Name)
                        ? "TriggerGotoTarget"
                        : trigger.Name;

                SetupTriggerTransform(
                    tgttObj,
                    trigger);

                TriggerGoToTarget tgtt =
                    tgttObj.GetComponent<TriggerGoToTarget>();

                if (tgtt == null)
                {
                    Debug.LogError(
                        "TriggerGoToTarget prefab is missing " +
                        "TriggerGoToTarget component.");

                    Destroy(tgttObj);
                    return;
                }

                foreach (MovingPlatform movingPlatform
                    in movingPlatforms)
                {
                    if (movingPlatform != null)
                        tgtt.AddMovingPlatform(
                            movingPlatform);
                }

                tgtt.targetTime =
                    ParseIntField(
                        trigger,
                        "targetTime",
                        0) / 1000f;

                string instant =
                    trigger.GetField("instant");

                if (!string.IsNullOrEmpty(instant))
                {
                    tgtt.instantReturn =
                        ParseBoolean(instant);
                }
                else
                {
                    string instantReturn =
                        trigger.GetField("instantReturn");

                    tgtt.instantReturn =
                        !string.IsNullOrEmpty(instantReturn) &&
                        ParseBoolean(instantReturn);
                }

                RegisterImportedObject(
                    trigger,
                    tgttObj,
                    Quaternion.Euler(-90f, 0f, 0f));

                CheckForPath(
                    trigger,
                    tgttObj);
            }
            else if (string.Equals(
                         dataBlock,
                         "TriggerGotoDelayTarget",
                         StringComparison.OrdinalIgnoreCase))
            {
                // TriggerGotoDelayTarget currently exposes a single
                // MovingPlatform reference, so preserve that behaviour
                // by creating one trigger instance per platform.
                foreach (MovingPlatform movingPlatform
                    in movingPlatforms)
                {
                    GameObject tgtdObj =
                        Instantiate(
                            triggerGotoDelayTargetPrefab,
                            transform,
                            false);

                    tgtdObj.name =
                        string.IsNullOrEmpty(trigger.Name)
                            ? "TriggerGotoDelayTarget"
                            : trigger.Name;

                    SetupTriggerTransform(
                        tgtdObj,
                        trigger);

                    TriggerGotoDelayTarget tgtd =
                        tgtdObj.GetComponent<
                            TriggerGotoDelayTarget>();

                    if (tgtd == null)
                    {
                        Destroy(tgtdObj);
                        continue;
                    }

                    tgtd.movingPlatform =
                        movingPlatform;

                    RegisterImportedObject(
                        trigger,
                        tgtdObj,
                        Quaternion.Euler(-90f, 0f, 0f));

                    CheckForPath(
                        trigger,
                        tgtdObj);
                }
            }
            else if (string.Equals(
                         dataBlock,
                         "RepetitiveTriggerGotoTarget",
                         StringComparison.OrdinalIgnoreCase))
            {
                GameObject rtgttObj =
                    Instantiate(
                        repetitiveTriggerGotoTargetPrefab != null
                            ? repetitiveTriggerGotoTargetPrefab
                            : triggerGoToTarget,
                        transform,
                        false);

                rtgttObj.name =
                    string.IsNullOrEmpty(trigger.Name)
                        ? "RepetitiveTriggerGotoTarget"
                        : trigger.Name;

                SetupTriggerTransform(
                    rtgttObj,
                    trigger);

                RegisterImportedObject(
                    trigger,
                    rtgttObj,
                    Quaternion.Euler(-90f, 0f, 0f));

                CheckForPath(
                    trigger,
                    rtgttObj);
            }
        }

        private void ImportFadePlatform(TSObject obj, string objectName)
        {
            GameObject prefab = objectName.ToLowerInvariant() switch
            {
                "fadeplatform" => fadePlatformPrefab,
                "fadeplatform2_1x1" => fadePlatform2_1x1Prefab,
                "fadeplatform2_1x2" => fadePlatform2_1x2Prefab,
                "fadeplatform2_1x3" => fadePlatform2_1x3Prefab,
                "fadeplatform2_1x5" => fadePlatform2_1x5Prefab,
                "fadeplatform2_2x2" => fadePlatform2_2x2Prefab,
                "fadeplatform2_3x3" => fadePlatform2_3x3Prefab,
                "fadeplatform2_5x5" => fadePlatform2_5x5Prefab,
                "fadeplatformconcrete" => fadePlatformConcretePrefab,
                "fadeplatformgrass" => fadePlatformGrassPrefab,
                "fadeplatformice" => fadePlatformIcePrefab,
                _ => null,
            };

            if (prefab == null) return;

            GameObject gobj = Instantiate(prefab, transform, false);
            gobj.name = obj.Name;

            gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
            gobj.transform.localRotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
            Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
            gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);

            FadePlatform fadePlatform = gobj.GetComponent<FadePlatform>();
            if (fadePlatform == null)
            {
                Destroy(gobj);
                return;
            }

            ApplySkins(gobj, obj.GetField("skin"), true);
            fadePlatform.Initialize(
                obj.GetField("functionality"),
                obj.GetField("fadeStyle"),
                ParseFloatField(obj, "fadeinTime", 500f) / 1000f,
                ParseFloatField(obj, "fadeoutTime", 500f) / 1000f,
                Mathf.Clamp(ParseFloatField(obj, "visibleTime", 500f) / 1000f, 0.1f, 120f),
                Mathf.Clamp(ParseFloatField(obj, "invisibleTime", 500f) / 1000f, 0.1f, 120f),
                ParseFloatField(obj, "StartOffset", 0f) / 1000f,
                ParseBoolField(obj, "permanent", false),
                Mathf.Max(Mathf.RoundToInt(ParseFloatField(obj, "level", 1f)), 1),
                Mathf.RoundToInt(ParseFloatField(obj, "state", 0f))
            );

            RegisterImportedObject(obj, gobj, Quaternion.Euler(0f, 0f, 0f));
            CheckForPath(obj, gobj);
        }

        private void ImportMegaManPlatform(TSObject obj, string objectName)
        {
            GameObject prefab = objectName.ToLowerInvariant() switch
            {
                "megamanplatform2_1x1" => megaManPlatform2_1x1Prefab,
                "megamanplatform2_1x2" => megaManPlatform2_1x2Prefab,
                "megamanplatform2_1x3" => megaManPlatform2_1x3Prefab,
                "megamanplatform2_1x5" => megaManPlatform2_1x5Prefab,
                "megamanplatform2_2x2" => megaManPlatform2_2x2Prefab,
                "megamanplatform2_3x3" => megaManPlatform2_3x3Prefab,
                "megamanplatform2_5x5" => megaManPlatform2_5x5Prefab,
                _ => null,
            };

            if (prefab == null) return;

            GameObject gobj = Instantiate(prefab, transform, false);
            gobj.name = obj.Name;

            gobj.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
            gobj.transform.localRotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
            Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
            gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);

            MegaManPlatform megaManPlatform = gobj.GetComponent<MegaManPlatform>();

            if (megaManPlatform == null)
            {
                Destroy(gobj);
                return;
            }

            megaManPlatform.next = obj.GetField("Next");

            ApplySkins(gobj, obj.GetField("skin"), true);
            RegisterImportedObject(obj, gobj, Quaternion.Euler(0f, 0f, 0f));
            CheckForPath(obj, gobj);
        }

        private void ImportTeleportOrDestinationTrigger(TSObject obj, string objectName)
        {
            bool isTeleport = objectName == "TeleportTrigger";
            var triggerObj = Instantiate(isTeleport ? teleportTrigger : destinationTrigger, transform, false);
            triggerObj.name = string.IsNullOrEmpty(obj.Name) ? objectName : obj.Name;

            if (isTeleport)
            {
                var tele = triggerObj.GetComponent<Teleport>();
                string delay = obj.GetField("delay");
                tele.time = string.IsNullOrEmpty(delay) ? 2f : float.Parse(delay) / 1000f;
                tele.destinationGameObjectName = obj.GetField("destination");

                string gemsToActivate = obj.GetField("gemstoactivate");
                tele.gemsToActivate = string.IsNullOrEmpty(gemsToActivate) ? 0 : Mathf.RoundToInt(float.Parse(gemsToActivate));

                string gemsToDeactivate = obj.GetField("gemstodeactivate");
                tele.gemsToDeactivate = string.IsNullOrEmpty(gemsToDeactivate) ? 100000000 : Mathf.RoundToInt(float.Parse(gemsToDeactivate));

                string displayGemsMessage = obj.GetField("displayGemsMessage");
                tele.displayGemsMessage = !string.IsNullOrEmpty(displayGemsMessage) && ParseBoolean(displayGemsMessage);

                string centerDestPoint = obj.GetField("centerdestpoint");
                tele.centerDestinationPoint = !string.IsNullOrEmpty(centerDestPoint) && ParseBoolean(centerDestPoint);

                string keepVelocity = obj.GetField("keepvelocity");
                tele.keepVelocity = !string.IsNullOrEmpty(keepVelocity) && ParseBoolean(keepVelocity);

                string inverseVelocity = obj.GetField("inversevelocity");
                tele.inverseVelocity = !string.IsNullOrEmpty(inverseVelocity) && ParseBoolean(inverseVelocity);

                string keepAngular = obj.GetField("keepangular");
                tele.keepAngular = !string.IsNullOrEmpty(keepAngular) && ParseBoolean(keepAngular);

                string keepCamera = obj.GetField("keepcamera");
                tele.keepCamera = !string.IsNullOrEmpty(keepCamera) && ParseBoolean(keepCamera);

                string cameraYaw = obj.GetField("camerayaw");
                tele.cameraYaw = string.IsNullOrEmpty(cameraYaw) ? 0f : float.Parse(cameraYaw);

                teleportTriggers.Add(triggerObj);
            }
            else
            {
                var destination = triggerObj.GetComponent<DestinationTrigger>();
                string centerDestPoint = obj.GetField("centerdestpoint");
                destination.centerDestinationPoint = !string.IsNullOrEmpty(centerDestPoint) && ParseBoolean(centerDestPoint);

                string keepVelocity = obj.GetField("keepvelocity");
                destination.keepVelocity = !string.IsNullOrEmpty(keepVelocity) && ParseBoolean(keepVelocity);

                string inverseVelocity = obj.GetField("inversevelocity");
                destination.inverseVelocity = !string.IsNullOrEmpty(inverseVelocity) && ParseBoolean(inverseVelocity);

                string keepAngular = obj.GetField("keepangular");
                destination.keepAngular = !string.IsNullOrEmpty(keepAngular) && ParseBoolean(keepAngular);

                string keepCamera = obj.GetField("keepcamera");
                destination.keepCamera = !string.IsNullOrEmpty(keepCamera) && ParseBoolean(keepCamera);

                string cameraYaw = obj.GetField("camerayaw");
                destination.cameraYaw = string.IsNullOrEmpty(cameraYaw) ? 0f : float.Parse(cameraYaw);

                destinationTriggers.Add(triggerObj);
            }

            Transform cameraPos = triggerObj.transform.Find("CameraPos");
            if (cameraPos != null) cameraPos.SetParent(null, true);

            SetupTriggerTransform(triggerObj, obj);

            if (cameraPos != null) cameraPos.SetParent(triggerObj.transform, true);

            if (!string.IsNullOrEmpty(obj.Name) || !isTeleport)
                destinationTriggers.Add(triggerObj);

            RegisterImportedObject(obj, triggerObj, Quaternion.Euler(-90f, 0f, 0f));
            CheckForPath(obj, triggerObj);
        }

        private void ImportPathedInteriorTrigger(TSObject trigger, MovingPlatform movingPlatform)
        {
            string dataBlock = trigger.GetField("dataBlock");

            if (string.Equals(dataBlock, "TriggerGotoTarget", StringComparison.OrdinalIgnoreCase))
            {
                string targetTime = trigger.GetField("targetTime");
                if (string.IsNullOrEmpty(targetTime)) return;

                var tgttObj = Instantiate(triggerGoToTarget, transform, false);
                tgttObj.name = string.IsNullOrEmpty(trigger.Name) ? "TriggerGotoTarget" : trigger.Name;
                SetupTriggerTransform(tgttObj, trigger);

                TriggerGoToTarget tgtt = tgttObj.GetComponent<TriggerGoToTarget>();
                tgtt.AddMovingPlatform(movingPlatform);
                tgtt.targetTime = (float)int.Parse(targetTime) / 1000f;

                string instant = trigger.GetField("instant");
                if (!string.IsNullOrEmpty(instant))
                {
                    tgtt.instantReturn = int.TryParse(instant, out int instantValue) && instantValue == 1;
                }
                else
                {
                    string instantReturn = trigger.GetField("instantReturn");
                    tgtt.instantReturn = !string.IsNullOrEmpty(instantReturn) && int.TryParse(instantReturn, out int instantReturnValue) && instantReturnValue == 1;
                }

                RegisterImportedObject(trigger, tgttObj, Quaternion.Euler(-90f, 0f, 0f));
                CheckForPath(trigger, tgttObj);
            }
            else if (string.Equals(dataBlock, "TriggerGotoDelayTarget", StringComparison.OrdinalIgnoreCase))
            {
                var tgtdObj = Instantiate(triggerGotoDelayTargetPrefab, transform, false);
                tgtdObj.name = string.IsNullOrEmpty(trigger.Name) ? "TriggerGotoDelayTarget" : trigger.Name;
                SetupTriggerTransform(tgtdObj, trigger);

                TriggerGotoDelayTarget tgtd = tgtdObj.GetComponent<TriggerGotoDelayTarget>();
                tgtd.movingPlatform = movingPlatform;

                RegisterImportedObject(trigger, tgtdObj, Quaternion.Euler(-90f, 0f, 0f));
                CheckForPath(trigger, tgtdObj);
            }
            else if (string.Equals(dataBlock, "RepetitiveTriggerGotoTarget", StringComparison.OrdinalIgnoreCase))
            {
                var rtgttObj = Instantiate(repetitiveTriggerGotoTargetPrefab != null ? repetitiveTriggerGotoTargetPrefab : triggerGoToTarget, transform, false);
                rtgttObj.name = string.IsNullOrEmpty(trigger.Name) ? "RepetitiveTriggerGotoTarget" : trigger.Name;
                SetupTriggerTransform(rtgttObj, trigger);

                RegisterImportedObject(trigger, rtgttObj, Quaternion.Euler(-90f, 0f, 0f));
                CheckForPath(trigger, rtgttObj);
            }
        }

        private void ImportCheckpoint(TSObject obj)
        {
            var cp = Instantiate(checkpointPrefab, transform, false);
            cp.name = string.IsNullOrEmpty(obj.Name) ? "Checkpoint" : obj.Name;

            Vector3 position = ConvertPoint(ParseVectorString(obj.GetField("position")));
            Quaternion rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
            Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

            Checkpoint checkpoint = cp.GetComponentInChildren<Checkpoint>();
            if (checkpoint == null)
            {
                Destroy(cp);
                return;
            }

            cp.transform.localPosition = position;
            cp.transform.localRotation = rotation;

            var spawnPos = cp.transform.Find("SpawnPos");
            if (spawnPos != null)
            {
                spawnPos.SetParent(transform, true);
            }

            cp.transform.localScale = Vector3.Scale(scale, cp.transform.localScale);

            checkpoint.checkpointGravityDir = -checkpoint.transform.up;

            string offset = obj.GetField("add");
            string subOffset = obj.GetField("sub");

            if (!string.IsNullOrEmpty(offset))
            {
                checkpoint.hasAddOrSub = true;
                checkpoint.offset = ConvertPoint(ParseVectorString(offset));
            }
            else if (!string.IsNullOrEmpty(subOffset))
            {
                checkpoint.hasAddOrSub = true;
                checkpoint.offset = -ConvertPoint(ParseVectorString(subOffset));
            }
            else
            {
                checkpoint.hasAddOrSub = false;
                checkpoint.offset = new Vector3(0f, 3f, 0f);
            }

            checkpoint.InitCheckpoint();
            checkpoints.Add(cp);

            spawnPos.transform.parent = cp.transform;

            RegisterImportedObject(obj, cp, Quaternion.Euler(-90f, 0f, 0f));
            CheckForPath(obj, cp);
        }

        private void ImportLapsCheckpoint(TSObject obj)
        {
            if (lapsCheckpointPrefab == null)
            {
                Debug.LogWarning(
                    $"LapsCheckpoint prefab is not assigned. Skipping '{obj.Name}'."
                );
                return;
            }

            GameObject triggerObj =
                Instantiate(lapsCheckpointPrefab, transform, false);

            triggerObj.name =
                string.IsNullOrEmpty(obj.Name)
                    ? "LapsCheckpoint"
                    : obj.Name;

            LapsCheckpoint checkpoint =
                triggerObj.GetComponent<LapsCheckpoint>();

            if (checkpoint == null)
            {
                Debug.LogError(
                    $"LapsCheckpoint prefab is missing a LapsCheckpoint component: {lapsCheckpointPrefab.name}"
                );

                Destroy(triggerObj);
                return;
            }

            checkpoint.checkpointNumber =
                Mathf.RoundToInt(
                    ParseFloatField(
                        obj,
                        "checkpointNumber",
                        1f
                    )
                );

            checkpoint.customSpawnPoint =
                ParseBoolField(
                    obj,
                    "customSpawnPoint",
                    false
                );

            checkpoint.enableRespawning =
                ParseBoolField(
                    obj,
                    "enableRespawning",
                    true
                );

            checkpoint.spawnPoint =
                obj.GetField("spawnPoint");

            checkpoint.forceGravity =
                obj.GetField("forceGravity");

            // ------------------------------------------------------------
            // Main LapsCheckpoint trigger transform
            // ------------------------------------------------------------

            SetupLapsTriggerTransform(
                triggerObj,
                obj
            );

            // ------------------------------------------------------------
            // Custom spawn point
            //
            // Torque format:
            //
            // x z y axisX axisY axisZ angle
            //
            // SpawnTrigger is a child of the LapsCheckpoint prefab,
            // so we set its WORLD transform after the checkpoint itself
            // has been positioned.
            // ------------------------------------------------------------

            Transform spawnTrigger =
                triggerObj.transform.Find("SpawnTrigger");

            if (spawnTrigger != null &&
                checkpoint.customSpawnPoint &&
                !string.IsNullOrWhiteSpace(checkpoint.spawnPoint))
            {
                if (TryParseSpawnPoint(
                        checkpoint.spawnPoint,
                        out Vector3 spawnPosition,
                        out Quaternion spawnRotation))
                {
                    spawnTrigger.SetPositionAndRotation(
                        spawnPosition,
                        spawnRotation
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"Could not parse spawnPoint '{checkpoint.spawnPoint}' " +
                        $"for LapsCheckpoint '{triggerObj.name}'."
                    );
                }
            }

            RegisterImportedObject(
                obj,
                triggerObj,
                Quaternion.Euler(-90f, 0f, 0f)
            );

            CheckForPath(obj, triggerObj);
        }

        private void ImportLapsCounterTrigger(TSObject obj)
        {
            if (lapsCounterTriggerPrefab == null)
            {
                Debug.LogWarning(
                    $"LapsCounterTrigger prefab is not assigned. Skipping '{obj.Name}'."
                );
                return;
            }

            GameObject triggerObj =
                Instantiate(
                    lapsCounterTriggerPrefab,
                    transform,
                    false
                );

            triggerObj.name =
                string.IsNullOrEmpty(obj.Name)
                    ? "LapsCounterTrigger"
                    : obj.Name;

            LapsCounterTrigger trigger =
                triggerObj.GetComponent<LapsCounterTrigger>();

            if (trigger == null)
            {
                Debug.LogError(
                    $"LapsCounterTrigger prefab is missing a LapsCounterTrigger component: {lapsCounterTriggerPrefab.name}"
                );

                Destroy(triggerObj);
                return;
            }

            trigger.customSpawnPoint =
                ParseBoolField(
                    obj,
                    "customSpawnPoint",
                    false
                );

            trigger.enableRespawning =
                ParseBoolField(
                    obj,
                    "enableRespawning",
                    true
                );

            trigger.spawnPoint =
                obj.GetField("spawnPoint");

            trigger.forceGravity =
                obj.GetField("forceGravity");

            SetupTriggerTransform(triggerObj, obj);

            RegisterImportedObject(
                obj,
                triggerObj,
                Quaternion.Euler(-90f, 0f, 0f)
            );

            CheckForPath(obj, triggerObj);
        }

        private void ImportExtendedTrigger(TSObject obj, string objectName, GameObject prefab)
        {
            if (prefab == null) return;

            // Defer checkpoint trigger setup to allow out-of-order resolution
            string nameLower = objectName.ToLowerInvariant();
            if (nameLower == "checkpointtrigger" || nameLower == "checkpointtrigger_pq")
            {
                string respawnPoint = obj.GetField("respawnPoint");
                if (!string.IsNullOrEmpty(respawnPoint))
                {
                    pendingCheckpointTriggerEntries.Add(new PendingCheckpointTriggerEntry
                    {
                        missionObject = obj,
                        respawnPoint = respawnPoint
                    });
                }
                return;
            }

            GameObject triggerObj = Instantiate(prefab, transform, false);
            triggerObj.name = string.IsNullOrEmpty(obj.Name) ? objectName : obj.Name;

            switch (nameLower)
            {
                case "accelerationtrigger":
                    if (triggerObj.TryGetComponent<AccelerationTrigger>(out var accel))
                    {
                        accel.xForce = ParseFloatField(obj, "xforce", 0f);
                        accel.yForce = ParseFloatField(obj, "yforce", 0f);
                        accel.zForce = ParseFloatField(obj, "zforce", 0f);
                    }
                    break;
                case "alignmenttrigger":
                    if (triggerObj.TryGetComponent<AlignmentTrigger>(out var align))
                    {
                        align.x = obj.GetField("x");
                        align.y = obj.GetField("y");
                        align.z = obj.GetField("z");
                        align.alwaysOn = ParseBoolField(obj, "alwaysOn", false);
                    }
                    break;
                case "altergravitytrigger":
                    if (triggerObj.TryGetComponent<AlterGravityTrigger>(out var alterGrav))
                    {
                        string measureAxis = obj.GetField("measureAxis");
                        string gravityAxis = obj.GetField("gravityAxis");

                        if (!string.IsNullOrEmpty(measureAxis) && Enum.TryParse(measureAxis, true, out AlterGravityTrigger.Axis pM))
                            alterGrav.measureAxis = pM;

                        if (!string.IsNullOrEmpty(gravityAxis) && Enum.TryParse(gravityAxis, true, out AlterGravityTrigger.Axis pG))
                            alterGrav.gravityAxis = pG;

                        alterGrav.flipMeasure = ParseBoolField(obj, "flipMeasure", false);
                        alterGrav.startingGravityRot = ParseFloatField(obj, "startingGravityRot", 0f);
                        alterGrav.endingGravityRot = ParseFloatField(obj, "endingGravityRot", 720f);
                    }
                    break;
                case "cameradistancetrigger":
                    if (triggerObj.TryGetComponent<CameraDistanceTrigger>(out var camDist))
                    {
                        camDist.time = ParseFloatField(obj, "Time", 1000f);
                        camDist.smooth = ParseBoolField(obj, "Smooth", true);
                        camDist.distance = ParseFloatField(obj, "Distance", 2.5f);
                        camDist.keepEffectOnLeave = ParseBoolField(obj, "KeepEffectOnLeave", true);
                        camDist.forceExitValue = ParseFloatField(obj, "ForceExitValue", 0f);
                    }
                    break;
                case "cameratrigger":
                    if (triggerObj.TryGetComponent<CameraTrigger>(out var camTrig))
                    {
                        string pitch = obj.GetField("pitch");
                        string yaw = obj.GetField("yaw");
                        camTrig.pitch = string.IsNullOrEmpty(pitch) ? "NoChange" : pitch;
                        camTrig.yaw = string.IsNullOrEmpty(yaw) ? "NoChange" : yaw;
                        camTrig.useRadians = ParseBoolField(obj, "useRadians", true);
                    }
                    break;
                case "cancelvelocitytrigger":
                    if (triggerObj.TryGetComponent<CancelVelocityTrigger>(out var cancelVel))
                    {
                        cancelVel.cancelX = ParseBoolField(obj, "cancelX", false);
                        cancelVel.cancelY = ParseBoolField(obj, "cancelY", true);
                        cancelVel.cancelZ = ParseBoolField(obj, "cancelZ", false);
                    }
                    break;
                case "changemarblesizetrigger":
                    if (triggerObj.TryGetComponent<ChangeMarbleSizeTrigger>(out var sizeTrig))
                    {
                        sizeTrig.marbleSize = ParseFloatField(obj, "mbsize", 0.18975f);
                        sizeTrig.suppressIndicator = ParseBoolField(obj, "indicator", false);
                    }
                    break;
                case "countdownstarttrigger":
                    if (triggerObj.TryGetComponent<CountdownStartTrigger>(out var cdStart))
                    {
                        cdStart.time = ParseFloatField(obj, "time", 10000f) / 1000f;
                        cdStart.startDelay = ParseFloatField(obj, "startdelay", 0f) / 1000f;
                        cdStart.activateOnce = ParseBoolField(obj, "activateonce", false);
                        string icon = obj.GetField("icon");
                        cdStart.icon = string.IsNullOrEmpty(icon) ? "timerTimeTravel" : icon;
                    }
                    break;
                case "gemchangetrigger":
                    if (triggerObj.TryGetComponent<GemChangeTrigger>(out var gemChange))
                    {
                        gemChange.gemBonus = Mathf.RoundToInt(ParseFloatField(obj, "gemBonus", -1f));
                    }
                    break;
                case "gravitypointtrigger":
                    if (triggerObj.TryGetComponent<GravityPointTrigger>(out var gravPt))
                    {
                        string customPoint = obj.GetField("customPoint");
                        if (!string.IsNullOrEmpty(customPoint))
                        {
                            gravPt.customPoint = ConvertPoint(ParseVectorString(customPoint));
                            gravPt.useCustomPoint = true;
                        }
                        gravPt.useRadius = ParseBoolField(obj, "useRadius", false);
                        gravPt.radiusSize = ParseFloatField(obj, "RadiusSize", 20f);
                        gravPt.invert = ParseBoolField(obj, "invert", false);
                        gravPt.upDownLeave = ParseBoolField(obj, "UpDownLeave", false);
                    }
                    break;
                case "gravitytrigger":
                    if (triggerObj.TryGetComponent<GravityTrigger>(out var gravTrg))
                    {
                        gravTrg.onLeave = ParseBoolField(obj, "onLeave", false);
                    }
                    break;
                case "megamanemulationtrigger":
                    if (triggerObj.TryGetComponent<MegaManEmulationTrigger>(out var megaManTrigger))
                    {
                        megaManTrigger.StartPlatformName = obj.GetField("startPlatform");
                    }
                    break;
                case "multipletgtt":
                    if (triggerObj.TryGetComponent<MultipleTGTT>(out var multiTgtt))
                    {
                        multiTgtt.targetTime = ParseFloatField(obj, "targettime", 0f) / 1000f;
                        pendingMultipleTGTTEntries.Add(new PendingMultipleTGTTEntry { trigger = multiTgtt });
                    }
                    break;
                case "musictrigger":
                    if (triggerObj.TryGetComponent<MusicTrigger>(out var musicTrig))
                    {
                        musicTrig.musicName = obj.GetField("music");
                        if (string.IsNullOrEmpty(musicTrig.musicName))
                            musicTrig.musicName = obj.GetField("musicName");

                        musicTrig.forceRestart = ParseBoolField(obj, "forceRestart", false);
                    }
                    break;
                case "mustchangetrigger":
                    if (triggerObj.TryGetComponent<MustChangeTrigger>(out var mustChange))
                    {
                        mustChange.targetTime = ParseFloatField(obj, "targettime", 0f) / 1000f;
                        mustChange.delayTargetTime = ParseFloatField(obj, "delaytargettime", 0f) / 1000f;
                        mustChange.instant = ParseBoolField(obj, "instant", false);
                        mustChange.iContinueToTime = ParseFloatField(obj, "icontinuetottime", 0f) / 1000f;
                        mustChange.movingPlatform = ResolveMovingPlatform(obj.GetField("target"));
                    }
                    break;
                case "relativetptrigger":
                    if (triggerObj.TryGetComponent<RelativeTPTrigger>(out var relTp))
                    {
                        relTp.silent =
                            ParseBoolField(obj, "silent", false);

                        string scale =
                            obj.GetField("tpscale");

                        if (!string.IsNullOrEmpty(scale))
                            relTp.tpScale =
                                ParseVector3Field(scale);

                        string offset =
                            obj.GetField("tpoffset");

                        if (!string.IsNullOrEmpty(offset))
                        {
                            relTp.tpOffset =
                                ParseVector3Field(offset);

                            relTp.tpOffset.x =
                                -relTp.tpOffset.x;
                        }

                        // Store the destination trigger NAME.
                        // The actual GameObject is resolved later,
                        // after every mission object has been imported.
                        relTp.destinationTriggerName =
                            obj.GetField("destination");
                    }
                    break;
                case "repetitivetriggergototarget":
                    if (triggerObj.TryGetComponent<RepetitiveTriggerGotoTarget>(out var repTgtt))
                    {
                        repTgtt.numTimesToTrigger = Mathf.RoundToInt(ParseFloatField(obj, "NumTimesToTrigger", 0f));
                        repTgtt.triggerOnce = ParseBoolField(obj, "TriggerOnce", true);
                        repTgtt.numTimesToRepeat = Mathf.RoundToInt(ParseFloatField(obj, "NumTimesToRepeat", 0f));
                        repTgtt.targetTime = ParseFloatField(obj, "targetTime", 999999f);
                        repTgtt.movingPlatform = ResolveMovingPlatform(obj.GetField("target"));
                    }
                    break;
                case "setvelocitytrigger":
                    if (triggerObj.TryGetComponent<SetVelocityTrigger>(out var setVel))
                    {
                        string velocity = obj.GetField("velocity");
                        if (!string.IsNullOrEmpty(velocity)) setVel.velocity = ParseVector3Field(velocity);
                        setVel.ignoreX = ParseBoolField(obj, "ignoreX", false);
                        setVel.ignoreY = ParseBoolField(obj, "ignoreY", false);
                        setVel.ignoreZ = ParseBoolField(obj, "ignoreZ", false);
                    }
                    break;
                case "smbtrigger":
                    if (triggerObj.TryGetComponent<SMBTrigger>(out var smb))
                    {
                        smb.impulse = ParseFloatField(obj, "impulse", 0f);
                        smb.upwards = ParseFloatField(obj, "upwards", 0f);
                    }
                    break;
                case "soundtrigger":
                    if (triggerObj.TryGetComponent<SoundTrigger>(out var soundTrig))
                    {
                        string triggerOnce = obj.GetField("TriggerOnce");
                        soundTrig.triggerOnce = !string.IsNullOrEmpty(triggerOnce) && ParseBoolean(triggerOnce);
                        soundTrig.sound = ResolveAudioClip(obj.GetField("sfx"));
                    }
                    break;
                case "spawntrigger":
                    if (triggerObj.TryGetComponent<SpawnTrigger>(out var spawnTrigger))
                    {
                        string offset = obj.GetField("add");
                        string subOffset = obj.GetField("sub");

                        if (!string.IsNullOrEmpty(offset))
                        {
                            spawnTrigger.hasAddOrSub = true;
                            spawnTrigger.offset = ConvertPoint(ParseVectorString(offset));
                        }
                        else if (!string.IsNullOrEmpty(subOffset))
                        {
                            spawnTrigger.hasAddOrSub = true;
                            spawnTrigger.offset = -ConvertPoint(ParseVectorString(subOffset));
                        }
                        else
                        {
                            spawnTrigger.hasAddOrSub = false;
                            spawnTrigger.offset = new Vector3(0f, 3f, 0f);
                        }
                        spawnTrigger.InitSpawnTrigger();
                    }
                    break;
                case "tdtrigger":
                    if (triggerObj.TryGetComponent<TDTrigger>(out var tdTrigger))
                    {
                        // Plane defaults to XZ.
                        string plane = obj.GetField("Plane");
                        tdTrigger.plane = string.IsNullOrEmpty(plane)
                            ? "xz"
                            : plane;

                        // InvertDirection
                        tdTrigger.invertDirection =
                            ParseBoolField(obj, "InvertDirection", false);

                        // KeepEffectOnLeave
                        tdTrigger.keepEffectOnLeave =
                            ParseBoolField(obj, "keepeffectonleave", false);

                        // CamDistance:
                        // "NoChange" means don't modify the current camera distance.
                        string camDistance = obj.GetField("CamDistance");

                        if (!string.IsNullOrEmpty(camDistance) &&
                            !camDistance.Equals(
                                "nochange",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            if (float.TryParse(
                                camDistance,
                                NumberStyles.Float,
                                Invariant,
                                out float parsedDistance))
                            {
                                tdTrigger.camDistance = parsedDistance;
                            }
                            else
                            {
                                tdTrigger.camDistance = float.NaN;
                            }
                        }
                        else
                        {
                            tdTrigger.camDistance = float.NaN;
                        }

                        // targetPitch:
                        // "NoChange" means keep the current camera pitch.
                        string targetPitch = obj.GetField("targetPitch");

                        tdTrigger.changesPitch =
                            !string.IsNullOrEmpty(targetPitch) &&
                            !targetPitch.Equals(
                                "nochange",
                                StringComparison.OrdinalIgnoreCase);

                        if (tdTrigger.changesPitch &&
                            float.TryParse(
                                targetPitch,
                                NumberStyles.Float,
                                Invariant,
                                out float parsedPitch))
                        {
                            // Torque/Haxe value is degrees.
                            // TwoDMode stores pitch internally as radians.
                            tdTrigger.targetPitch = parsedPitch * Mathf.Deg2Rad;
                        }
                        else
                        {
                            tdTrigger.targetPitch = float.NaN;
                            tdTrigger.changesPitch = false;
                        }
                    }
                    break;
                case "timetraveltrigger":
                    if (triggerObj.TryGetComponent<TimeTravelTrigger>(out var ttTrig))
                    {
                        ttTrig.timeBonus = ParseFloatField(obj, "timeBonus", 5000f) / 1000f;
                    }
                    break;
                case "usepoweruptrigger":
                    if (triggerObj.TryGetComponent<UsePowerupTrigger>(out var usePwr))
                    {
                        usePwr.powerup = ParsePowerupType(obj.GetField("powerup"));
                    }
                    break;
            }

            SetupTriggerTransform(triggerObj, obj);
            RegisterImportedObject(obj, triggerObj, Quaternion.Euler(-90f, 0f, 0f));
            CheckForPath(obj, triggerObj);
        }

        private GameObject ImportPathTrigger(TSObject obj)
        {
            GameObject gobj =
                Instantiate(
                    pathTrigger,
                    transform,
                    false
                );

            gobj.name =
                string.IsNullOrEmpty(obj.Name)
                    ? "PathTrigger"
                    : obj.Name;

            // SetupTriggerTransform already creates/configures
            // the BoxCollider through ApplyTriggerPolyhedron().
            SetupTriggerTransform(
                gobj,
                obj
            );

            PathTrigger pt =
                gobj.GetComponent<PathTrigger>();

            pt.TriggerOnce =
                ParseBoolField(
                    obj,
                    "TriggerOnce",
                    true
                );

            int index = 1;

            while (true)
            {
                string objectName =
                    obj.GetField(
                        $"object{index}"
                    );

                string pathName =
                    obj.GetField(
                        $"Path{index}"
                    );

                if (string.IsNullOrWhiteSpace(objectName))
                    break;

                objectName =
                    NormalizeMissionObjectName(
                        objectName
                    );

                pathName =
                    NormalizeMissionObjectName(
                        pathName
                    );

                if (string.IsNullOrWhiteSpace(pathName))
                {
                    index++;
                    continue;
                }

                pendingPathTriggerEntries.Add(
                    new PendingPathTriggerEntry
                    {
                        trigger = pt,
                        objectName = objectName,
                        pathName = pathName
                    }
                );

                index++;
            }

            RegisterImportedObject(
                obj,
                gobj,
                Quaternion.Euler(-90f, 0f, 0f)
            );

            return gobj;
        }

        private void ImportPhysModTrigger(TSObject obj)
        {
            if (physModTrigger == null) return;

            GameObject gobj = Instantiate(physModTrigger, transform, false);
            gobj.name = string.IsNullOrEmpty(obj.Name) ? "PhysModTrigger" : obj.Name;
            SetupTriggerTransform(gobj, obj);

            PhysModTrigger trigger = gobj.GetComponent<PhysModTrigger>();
            if (trigger == null)
            {
                Destroy(gobj);
                return;
            }

            trigger.Overrides.Clear();
            for (int i = 0; ; i++)
            {
                string attribute = obj.GetField($"marbleAttribute{i}");
                if (string.IsNullOrWhiteSpace(attribute)) break;

                float value = ParseFloatField(obj, $"value{i}", 0f);
                trigger.Overrides.Add(new PhysicsAttributeOverride(attribute.ToLowerInvariant(), value));
            }

            trigger.Disabled = ParseBoolField(obj, "disabled", false);
            trigger.NoEmitters = ParseBoolField(obj, "noEmitters", false);

            if (!trigger.NoEmitters)
                SpawnPhysModEmitters(obj, gobj.transform);

            RegisterImportedObject(obj, gobj, Quaternion.Euler(-90f, 0f, 0f));
            CheckForPath(obj, gobj);
        }

        private void ImportCannon(TSObject obj, string objectName)
        {
            if (cannonPrefab == null) return;

            GameObject gobj = Instantiate(cannonPrefab, transform, false);
            gobj.name = string.IsNullOrEmpty(obj.Name) ? objectName : obj.Name;

            Vector3 position = ConvertPoint(ParseVectorString(obj.GetField("position")));
            Quaternion rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
            Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

            gobj.transform.localPosition = position;
            gobj.transform.localRotation = rotation;

            Cannon cannon = gobj.GetComponentInChildren<Cannon>(true);
            if (cannon == null)
            {
                Destroy(gobj);
                return;
            }

            var cpas = cannon.transform.Find("CPAS");
            cpas.parent = transform;
            gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);
            cpas.parent = cannon.transform;

            cannon.useBase = ParseBoolField(obj, "useBase", true);
            cannon.useCharge = ParseBoolField(obj, "useCharge", false);
            cannon.chargeTime = ParseFloatField(obj, "chargeTime", 2000f) / 1000f;
            cannon.force = ParseFloatField(obj, "force", 30f);
            cannon.pitch = ParseFloatField(obj, "pitch", 0f) * -1f;
            cannon.yaw = ParseFloatField(obj, "yaw", 0f) + 180f;
            cannon.pitchBoundLow = ParseFloatField(obj, "pitchBoundHigh", -30f) * -1f;
            cannon.pitchBoundHigh = ParseFloatField(obj, "pitchBoundLow", 80f) * -1f;
            cannon.yawBoundLeft = ParseFloatField(obj, "yawBoundLeft", 70f);
            cannon.yawBoundRight = ParseFloatField(obj, "yawBoundRight", 70f);
            cannon.yawLimit = ParseBoolField(obj, "yawLimit", true);
            cannon.instant = ParseBoolField(obj, "instant", false);
            cannon.instantDelayTime = ParseFloatField(obj, "instantDelayTime", 0f) / 1000f;
            cannon.lockTime = ParseFloatField(obj, "lockTime", 0f) / 1000f;
            cannon.lockCam = ParseBoolField(obj, "lockCam", false);
            cannon.showAim = ParseBoolField(obj, "showAim", true);
            cannon.aimSize = ParseFloatField(obj, "aimSize", 0.25f);

            string datablock = objectName.ToLowerInvariant();
            if (datablock == "cannon_low") { cannon.useCharge = false; cannon.force = 20f; }
            else if (datablock == "cannon_mid") { cannon.useCharge = false; cannon.force = 35f; }
            else if (datablock == "cannon_high") { cannon.useCharge = false; cannon.force = 50f; }

            string skin = cannon.instant ? "orange" : datablock switch
            {
                "cannon_low" => "green",
                "cannon_mid" => "blue",
                "cannon_high" => "red",
                _ => "white",
            };

            ApplySkins(gobj, skin);
            cannon.ResetCannon();

            RegisterImportedObject(obj, gobj, Quaternion.identity);
            CheckForPath(obj, gobj);
        }

        private void ImportHelpBubble(TSObject obj, Transform parent)
        {
            if (helpBubblePrefab == null) return;

            GameObject gobj = Instantiate(helpBubblePrefab, parent, false);
            gobj.name = string.IsNullOrEmpty(obj.Name) ? "HelpBubble" : obj.Name;

            Vector3 position = ConvertPoint(ParseVectorString(obj.GetField("position")));
            Quaternion rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
            Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

            gobj.transform.localPosition = position;
            gobj.transform.localRotation = gobj.transform.localRotation * rotation * Quaternion.Euler(90f, 0f, 0f);
            gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);

            HelpBubble helpBubble = gobj.GetComponent<HelpBubble>() ?? gobj.GetComponentInChildren<HelpBubble>(true);
            HelpTrigger helpTrigger = gobj.GetComponent<HelpTrigger>() ?? gobj.GetComponentInChildren<HelpTrigger>(true);

            if (helpBubble == null || helpTrigger == null)
            {
                Destroy(gobj);
                return;
            }

            helpBubble.helpTrigger = helpTrigger;
            helpBubble.helpTrigger.helpText = obj.GetField("text");
            helpBubble.triggerRadius = ParseFloatField(obj, "triggerRadius", 3f);
            helpBubble.displayOnce = ParseBoolField(obj, "displayonce", false);
            helpBubble.disabled = ParseBoolField(obj, "disable", false);

            SphereCollider trigger = gobj.GetComponent<SphereCollider>() ?? gobj.GetComponentInChildren<SphereCollider>(true);
            if (trigger != null)
            {
                trigger.isTrigger = true;
                trigger.radius = helpBubble.triggerRadius;
            }

            RegisterImportedObject(obj, gobj);
            CheckForPath(obj, gobj);
        }

        private void ImportPushButton(TSObject obj, GameObject prefab, bool isToggle)
        {
            if (prefab == null) return;

            GameObject instance = Instantiate(prefab, transform, false);
            instance.name = string.IsNullOrEmpty(obj.Name) ? obj.GetField("dataBlock") : obj.Name;
            instance.transform.localPosition = ConvertPoint(ParseVectorString(obj.GetField("position")));
            instance.transform.localRotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
            instance.transform.localScale = ConvertScale(ParseVectorString(obj.GetField("scale")));

            PushButton button = instance.GetComponent<PushButton>() ?? instance.GetComponentInChildren<PushButton>(true);
            if (button == null)
            {
                Destroy(instance);
                return;
            }

            button.isToggleButton = isToggle;
            button.resetTime = ParseFloatField(obj, "resettime", 5000f) / 1000f;
            button.triggerOnce = ParseBoolField(obj, "triggerOnce", false);
            button.triggerObject = obj.GetField("triggerObject");
            button.objectMethod = obj.GetField("objectMethod");
            button.initialState = isToggle && ParseBoolField(obj, "initialstate", false);

            RegisterImportedObject(obj, instance, Quaternion.Euler(-90f, 0f, 0f));
            CheckForPath(obj, instance);
        }

        private void ImportPathNode(TSObject obj, Transform pathNodeParent, PathManager pathManager)
        {
            PathNode node = PathNodeParser.Parse(obj);
            if (node == null) return;

            GameObject nodeObject = new GameObject(obj.Name);
            nodeObject.transform.SetParent(pathNodeParent, false);
            nodeObject.transform.position = node.localPosition;
            nodeObject.transform.rotation = node.localRotation;
            nodeObject.transform.localScale = node.localScale;

            PathNodeObject pathNodeObject = nodeObject.AddComponent<PathNodeObject>();
            pathNodeObject.node = node;
            pathManager.RegisterNode(node);
        }

        private void ImportSceneryObject(TSObject obj)
        {
            string datablock = obj.GetField("dataBlock");
            if (string.IsNullOrEmpty(datablock))
                datablock = Path.GetFileNameWithoutExtension(obj.GetField("shapeName"));

            GameObject prefab = GetSceneryPrefab(datablock);
            if (prefab == null) return;

            if (datablock == "OrbitingClouds")
            {
                var ob = prefab.GetComponent<OrbitingClouds>();
                var rev = obj.GetField("reverse");
                if (!string.IsNullOrEmpty(rev)) ob.SetReverse(ParseBoolean(rev));
            }

            GameObject gobj = Instantiate(prefab, transform, false);
            gobj.name = datablock;

            Vector3 position = ConvertPoint(ParseVectorString(obj.GetField("position")));
            Quaternion rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
            Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

            gobj.transform.localPosition = position;
            gobj.transform.localRotation = gobj.transform.localRotation * rotation * Quaternion.Euler(90f, 0f, 0f);
            gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);

            ApplySkins(gobj, obj.GetField("skin"), true);
            RegisterImportedObject(obj, gobj, Quaternion.Euler(90f, 0f, 0f));
            CheckForPath(obj, gobj);
        }

        private void SpawnPhysModEmitters(TSObject obj, Transform triggerTransform)
        {
            if (physModEmitterPrefab == null) return;

            BoxCollider box = triggerTransform.GetComponent<BoxCollider>();
            if (box == null) return;

            Vector3 worldCenter = triggerTransform.TransformPoint(box.center);
            Vector3 extents = Vector3.Scale(box.size * 0.5f, triggerTransform.lossyScale);

            Vector3 right = triggerTransform.right * extents.x;
            Vector3 up = triggerTransform.up * extents.y;
            Vector3 forward = triggerTransform.forward * extents.z;

            Vector3[] directions = { right, -right, up, -up, forward, -forward };
            Vector3 worldDownDir = -right;
            float maxDot = float.MinValue;

            foreach (var dir in directions)
            {
                float dot = Vector3.Dot(dir.normalized, Vector3.down);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    worldDownDir = dir;
                }
            }

            Vector3 axisA = Vector3.zero;
            Vector3 axisB = Vector3.zero;

            if (worldDownDir == up || worldDownDir == -up)
            {
                axisA = right;
                axisB = forward;
            }
            else if (worldDownDir == right || worldDownDir == -right)
            {
                axisA = up;
                axisB = forward;
            }
            else
            {
                axisA = right;
                axisB = up;
            }

            Vector3 bottomCenter = worldCenter + worldDownDir;
            Vector3[] worldCorners =
            {
                bottomCenter - axisA - axisB,
                bottomCenter + axisA - axisB,
                bottomCenter - axisA + axisB,
                bottomCenter + axisA + axisB,
            };

            for (int i = 0; i < worldCorners.Length; i++)
            {
                GameObject emitter = Instantiate(physModEmitterPrefab, triggerTransform.parent, false);
                emitter.name = string.IsNullOrEmpty(obj.Name) ? $"PhysModEmitterBase_{i}" : $"{obj.Name}_Emitter_{i}";
                emitter.transform.position = worldCorners[i];
                emitter.transform.rotation = Quaternion.identity;
                emitter.transform.localScale = Vector3.one;
            }
        }

        #endregion

        #region Post-Processing Resolvers

        private void ResolvePaths()
        {
            int initializedCount = 0;
            int missingPathCount = 0;

            foreach (PendingPathEntry entry in pendingPathEntries)
            {
                if (entry == null || entry.gameObject == null || string.IsNullOrEmpty(entry.pathName)) continue;

                if (!pathManager.TryGetNode(entry.pathName, out PathNode node))
                {
                    missingPathCount++;
                    continue;
                }

                PathMover mover = entry.gameObject.GetComponent<PathMover>() ?? entry.gameObject.AddComponent<PathMover>();
                mover.InitializePath(node.nodeName, pathManager, true);
                movementManager.RegisterMovingObject(mover);
                initializedCount++;
            }

            Debug.Log($"Path resolution complete: {initializedCount} paths initialized, {missingPathCount} path references unresolved.");
            pendingPathEntries.Clear();
        }

        private void ResolveParenting()
        {
            int parentedCount = 0;
            int missingParentCount = 0;

            HashSet<GameObject> resolving = new HashSet<GameObject>();
            HashSet<GameObject> resolved = new HashSet<GameObject>();

            foreach (var pair in pendingParents)
            {
                if (pair.Key == null) continue;
                ResolveParentRecursive(pair.Key, pendingParents, resolving, resolved, ref parentedCount, ref missingParentCount);
            }

            Debug.Log($"Parenting complete: {parentedCount} objects parented, {missingParentCount} parent references unresolved.");
        }

        private bool ResolveParentRecursive(
            GameObject child,
            Dictionary<GameObject, PendingParent> pending,
            HashSet<GameObject> resolving,
            HashSet<GameObject> resolved,
            ref int parentedCount,
            ref int missingParentCount
        )
        {
            if (child == null) return false;
            if (resolved.Contains(child)) return true;
            if (resolving.Contains(child)) return false;

            if (!pending.TryGetValue(child, out PendingParent info))
            {
                resolved.Add(child);
                return true;
            }

            if (!importedObjects.TryGetValue(info.parentName, out GameObject parent) || parent == null || parent == child)
            {
                missingParentCount++;
                resolved.Add(child);
                return false;
            }

            resolving.Add(child);
            bool parentResolved = true;

            if (pending.ContainsKey(parent))
            {
                parentResolved = ResolveParentRecursive(parent, pending, resolving, resolved, ref parentedCount, ref missingParentCount);
            }

            resolving.Remove(child);

            if (!parentResolved)
            {
                resolved.Add(child);
                return false;
            }

            GameObjectParentFollower follower = child.GetComponent<GameObjectParentFollower>() ?? child.AddComponent<GameObjectParentFollower>();
            Quaternion extraRotation = info.offsetRotate ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;

            follower.Initialize(
                parent,
                info.parentTransform,
                info.parentOffset,
                info.parentSimple,
                info.parentNoRot,
                info.additionalRotation * extraRotation
            );

            follower.ApplyNow();
            parentedCount++;
            resolved.Add(child);
            return true;
        }

        private void ResolveMultipleTGTTReferences()
        {
            if (pendingMultipleTGTTEntries.Count == 0) return;

            MovingPlatform[] polymorphismPlatforms = new MovingPlatform[9];
            for (int i = 0; i < polymorphismPlatforms.Length; i++)
            {
                polymorphismPlatforms[i] = ResolveMovingPlatform($"plat{i + 1}");
            }

            foreach (PendingMultipleTGTTEntry entry in pendingMultipleTGTTEntries)
            {
                if (entry?.trigger != null)
                    entry.trigger.SetMovingPlatforms(polymorphismPlatforms);
            }

            pendingMultipleTGTTEntries.Clear();
        }

        private void ResolvePathTriggers()
        {
            foreach (PendingPathTriggerEntry entry in pendingPathTriggerEntries)
            {
                if (entry?.trigger == null || string.IsNullOrWhiteSpace(entry.objectName) || string.IsNullOrWhiteSpace(entry.pathName))
                    continue;

                if (!importedObjects.TryGetValue(entry.objectName, out GameObject target)) continue;
                if (!pathManager.TryGetNode(entry.pathName, out PathNode node)) continue;

                MovingPlatform movingPlatform = target.GetComponent<MovingPlatform>();
                if (movingPlatform != null)
                {
                    movingPlatform.SetTriggerControlled(true);
                }

                PathMover mover = target.GetComponent<PathMover>() ?? target.AddComponent<PathMover>();
                movementManager.RegisterMovingObject(mover);
                entry.trigger.AddEntry(target, pathManager, node.nodeName);
            }

            pendingPathTriggerEntries.Clear();
        }

        private void ResolveCheckpointTriggers()
        {
            foreach (var entry in pendingCheckpointTriggerEntries)
            {
                TSObject obj = entry.missionObject;
                string respawnPoint = entry.respawnPoint;

                GameObject cp = checkpoints.FirstOrDefault(go =>
                    go != null && string.Equals(go.name, respawnPoint, StringComparison.OrdinalIgnoreCase));

                if (cp == null)
                {
                    Debug.LogWarning($"CheckpointTrigger '{obj.Name}' references checkpoint '{respawnPoint}', but that checkpoint was not found.");
                    continue;
                }

                Checkpoint baseCheckpoint = cp.GetComponentInChildren<Checkpoint>();
                if (baseCheckpoint == null)
                {
                    Debug.LogWarning($"Checkpoint '{cp.name}' does not have a Checkpoint component.");
                    continue;
                }

                GameObject cpTrigger = Instantiate(checkpointTriggerPrefab, transform, false);
                cpTrigger.name = string.IsNullOrEmpty(obj.Name) ? $"CheckpointTrigger_{respawnPoint}" : obj.Name;

                CheckpointTrigger trigger = cpTrigger.GetComponent<CheckpointTrigger>();
                if (trigger == null)
                {
                    Destroy(cpTrigger);
                    continue;
                }

                trigger.baseCheckpoint = baseCheckpoint;
                SetupTriggerTransform(cpTrigger, obj);

                BoxCollider collider = cpTrigger.GetComponent<BoxCollider>();
                if (collider != null)
                    collider.enabled = true;

                RegisterImportedObject(obj, cpTrigger, Quaternion.Euler(-90f, 0f, 0f));
                CheckForPath(obj, cpTrigger);
            }

            pendingCheckpointTriggerEntries.Clear();
        }

        private void ResolveTeleporters()
        {
            foreach (GameObject go in teleportTriggers)
            {
                Teleport tele = go.GetComponent<Teleport>();
                if (tele == null) continue;

                tele.destination = destinationTriggers.FirstOrDefault(dest =>
                    !string.IsNullOrEmpty(dest.name) &&
                    !string.IsNullOrEmpty(tele.destinationGameObjectName) &&
                    string.Equals(dest.name, tele.destinationGameObjectName, StringComparison.OrdinalIgnoreCase)
                );

                tele.InitTeleporter();
            }
        }

        private void ResolveRelativeTeleporters()
        {
            foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
            {
                RelativeTPTrigger relativeTP =
                    child.GetComponent<RelativeTPTrigger>();

                if (relativeTP == null)
                    continue;

                if (string.IsNullOrWhiteSpace(relativeTP.destinationTriggerName))
                {
                    Debug.LogWarning(
                        $"RelativeTPTrigger '{child.name}' has no destination name."
                    );

                    continue;
                }

                GameObject destination =
                    destinationTriggers.FirstOrDefault(dest =>
                        dest != null &&
                        string.Equals(
                            dest.name,
                            relativeTP.destinationTriggerName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                if (destination == null)
                {
                    Debug.LogWarning(
                        $"RelativeTPTrigger '{child.name}' could not find " +
                        $"DestinationTrigger '{relativeTP.destinationTriggerName}'."
                    );

                    continue;
                }

                relativeTP.SetDestination(destination);

                Debug.Log(
                    $"RelativeTPTrigger '{child.name}' -> " +
                    $"DestinationTrigger '{destination.name}'"
                );
            }
        }


        #endregion

        #region Setup & Transform Helpers

        private void SetupTriggerTransform(GameObject gobj, TSObject obj, bool setLocal = true)
        {
            Vector3 position = ConvertPoint(ParseVectorString(obj.GetField("position")));
            Quaternion rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")), false);
            Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
            Quaternion convertedRotation = Quaternion.Euler(-90f, 0f, 0f) * rotation;

            if (setLocal)
            {
                gobj.transform.localPosition = position;
                gobj.transform.localRotation = convertedRotation;
                gobj.transform.localScale = scale;
            }
            else
            {
                gobj.transform.position = position;
                gobj.transform.rotation = convertedRotation;
                gobj.transform.localScale = scale;
            }

            ApplyTriggerPolyhedron(gobj, obj);
        }

        private void SetupLapsTriggerTransform(
            GameObject gobj,
            TSObject obj, bool setLocal = true)
        {
            Vector3 position = ConvertPoint(ParseVectorString(obj.GetField("position")));
            Quaternion rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")), false);
            Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));
            Quaternion convertedRotation = Quaternion.Euler(-90f, 0f, 0f) * rotation;

            if (setLocal)
            {
                gobj.transform.localPosition = position;
                gobj.transform.localRotation = convertedRotation;
            }
            else
            {
                gobj.transform.position = position;
                gobj.transform.rotation = convertedRotation;
            }

            var child = gobj.transform.GetChild(0);
            child.parent = transform;

            if (setLocal)
            {
                gobj.transform.localScale = scale;
            }
            else
            {
                gobj.transform.localScale = scale;
            }

            ApplyTriggerPolyhedron(gobj, obj);

            Vector3 childPos;
            Quaternion childRot;
            TryParseSpawnPoint(obj.GetField("spawnPoint"), out childPos, out childRot);

            child.SetPositionAndRotation(childPos, childRot);
        }


        private void ApplyTriggerPolyhedron(GameObject gobj, TSObject obj)
        {
            string polyhedronString = obj.GetField("polyhedron");
            if (string.IsNullOrWhiteSpace(polyhedronString)) return;

            float[] coordinates = ParseVectorString(polyhedronString);
            if (coordinates == null || coordinates.Length != 12) return;

            Vector3 origin = new Vector3(-coordinates[0], coordinates[1], coordinates[2]);
            Vector3 d1 = new Vector3(-coordinates[3], coordinates[4], coordinates[5]);
            Vector3 d2 = new Vector3(-coordinates[6], coordinates[7], coordinates[8]);
            Vector3 d3 = new Vector3(-coordinates[9], coordinates[10], coordinates[11]);

            Bounds bounds = new Bounds(origin, Vector3.zero);
            bounds.Encapsulate(origin + d1);
            bounds.Encapsulate(origin + d2);
            bounds.Encapsulate(origin + d3);
            bounds.Encapsulate(origin + d1 + d2);
            bounds.Encapsulate(origin + d1 + d3);
            bounds.Encapsulate(origin + d2 + d3);
            bounds.Encapsulate(origin + d1 + d2 + d3);

            BoxCollider collider = gobj.GetComponent<BoxCollider>() ?? gobj.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(-bounds.center.x, -bounds.center.y, bounds.center.z);
            collider.size = bounds.size;
        }

        private Vector3 SetTransforms(GameObject gobj, TSObject obj, Quaternion additionalRotation)
        {
            Vector3 position = ConvertPoint(ParseVectorString(obj.GetField("position")));
            Quaternion rotation = ConvertRotation(ParseVectorString(obj.GetField("rotation")));
            Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

            gobj.transform.localPosition = position;
            gobj.transform.localRotation = gobj.transform.localRotation * rotation * additionalRotation;
            gobj.transform.localScale = scale;
            return scale;
        }

        private void SetTransformsPowerup(GameObject gobj, TSObject obj)
        {
            Vector3 position = ConvertPoint(ParseVectorString(obj.GetField("position")));
            Quaternion rotation = ConvertRotationPowerups(ParseVectorString(obj.GetField("rotation")));
            Vector3 scale = ConvertScale(ParseVectorString(obj.GetField("scale")));

            gobj.transform.localPosition = position;
            gobj.transform.localRotation = rotation;
            gobj.transform.localScale = Vector3.Scale(scale, gobj.transform.localScale);
        }

        private void CheckForPath(TSObject obj, GameObject go)
        {
            if (obj == null || go == null) return;
            string pathName = NormalizeMissionObjectName(obj.GetField("Path"));
            if (string.IsNullOrEmpty(pathName)) return;

            pendingPathEntries.Add(new PendingPathEntry { gameObject = go, pathName = pathName });
        }

        private void RegisterImportedObject(TSObject obj, GameObject go, Quaternion additionalRotation = default, bool offset = false)
        {
            if (obj == null || go == null) return;

            if (obj != null && go != null)
            {
                importedTSObjects[obj] = go;
            }

            string objectName = NormalizeMissionObjectName(obj.Name);
            if (!string.IsNullOrEmpty(objectName) && !importedObjects.ContainsKey(objectName))
            {
                importedObjects.Add(objectName, go);
            }

            string parentName = NormalizeMissionObjectName(obj.GetField("Parent"));
            if (string.IsNullOrEmpty(parentName)) return;

            pendingParents[go] = new PendingParent
            {
                parentName = parentName,
                parentModTrans = obj.GetField("parentModTrans"),
                parentTransform = obj.GetField("parentTransform"),
                parentOffset = obj.GetField("parentOffset"),
                parentSimple = bool.TryParse(obj.GetField("parentSimple"), out bool simple) && simple,
                parentNoRot = bool.TryParse(obj.GetField("parentNoRot"), out bool noRot) && noRot,
                dataBlock = obj.GetField("dataBlock"),
                additionalRotation = additionalRotation == default ? Quaternion.identity : additionalRotation,
                offsetRotate = offset
            };
        }

        #endregion

        #region Value Conversion & Data Field Helpers

        private GameObject ResolveImportedObject(string objectName) =>
            string.IsNullOrEmpty(objectName) ? null : importedObjects.Values.FirstOrDefault(go => go != null && string.Equals(go.name, objectName, StringComparison.OrdinalIgnoreCase));

        private MovingPlatform ResolveMovingPlatform(string objectName) => ResolveImportedObject(objectName)?.GetComponent<MovingPlatform>();

        private string NormalizeMissionObjectName(string name) => string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim().Trim('"').Trim();

        private float[] ParseVectorString(string vs) => vs.Split(' ').Select(s => float.Parse(s, Invariant)).ToArray();

        private Vector3 ParseVector3Field(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Vector3.zero;
            float[] values = ParseVectorString(value);
            return (values == null || values.Length < 3) ? Vector3.zero : new Vector3(values[0], values[1], values[2]);
        }

        private float ParseFloatField(TSObject obj, string fieldName, float defaultValue)
        {
            string value = obj.GetField(fieldName);
            return (string.IsNullOrEmpty(value) || !float.TryParse(value, NumberStyles.Float, Invariant, out float result)) ? defaultValue : result;
        }

        private bool ParseBoolField(TSObject obj, string fieldName, bool defaultValue)
        {
            string value = obj.GetField(fieldName);
            if (string.IsNullOrEmpty(value)) return defaultValue;
            if (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            return defaultValue;
        }

        private bool ParseBoolean(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            value = value.Trim().ToLowerInvariant();
            return value == "1" || value == "true" || value == "yes";
        }

        private Vector3 ConvertPoint(float[] p) => new Vector3(p[0], p[2], p[1]);
        private Vector3 ConvertPoint(Vector3 p) => new Vector3(p.x, p.z, p.y);
        private Vector3 ConvertScale(float[] s) => new Vector3(s[0], s[1], s[2]);
        private Vector3 ConvertScaleXZY(float[] s) => new Vector3(s[0], s[2], s[1]);

        private Quaternion ConvertDirection(float[] torqueDir)
        {
            Vector3 unityDir = new Vector3(torqueDir[0], torqueDir[2], torqueDir[1]).normalized;
            return Quaternion.LookRotation(unityDir, Vector3.up);
        }

        private Quaternion ConvertRotationPowerups(float[] torqueRotation)
        {
            float x = torqueRotation[0], y = torqueRotation[1], z = torqueRotation[2], angle = torqueRotation[3];

            if (Mathf.Approximately(y, 0f) && Mathf.Approximately(z, 0f) && !Mathf.Approximately(x, 0f))
                return Quaternion.AngleAxis(angle * x, Vector3.right);
            if (Mathf.Approximately(x, 0f) && Mathf.Approximately(z, 0f) && !Mathf.Approximately(y, 0f))
                return Quaternion.AngleAxis(angle * y, Vector3.forward);
            if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f) && !Mathf.Approximately(z, 0f))
                return Quaternion.AngleAxis(angle * z, Vector3.up);

            return Quaternion.AngleAxis(angle, new Vector3(x, z, -y));
        }

        private Quaternion ConvertRotation(float[] torqueRotation, bool additionalRotate = true)
        {
            Quaternion rot = Quaternion.AngleAxis(torqueRotation[3], new Vector3(torqueRotation[0], -torqueRotation[1], torqueRotation[2]));
            return additionalRotate ? Quaternion.Euler(-90.0f, 0, 0) * rot : rot;
        }

        private string ExtractGemColor(string dataBlock)
        {
            const string prefix = "GemItem";
            const string suffix = "_PQ";

            if (string.IsNullOrEmpty(dataBlock) || !dataBlock.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            string color = dataBlock.Substring(prefix.Length);
            if (color.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                color = color.Substring(0, color.Length - suffix.Length);

            return string.IsNullOrEmpty(color) ? null : color.ToLowerInvariant();
        }

        private string ResolvePath(string assetPath, string misPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return assetPath;
            assetPath = assetPath.Replace('\\', '/').TrimEnd('"').TrimStart('/');

            if (assetPath.StartsWith("."))
            {
                string directory = Path.GetDirectoryName(misPath).Replace('\\', '/');
                assetPath = directory + assetPath.Substring(1);
            }
            else
            {
                int slash = assetPath.IndexOf('/');
                assetPath = slash >= 0 ? "platinum" + assetPath.Substring(slash) : "platinum/" + assetPath;
            }

            return assetPath;
        }

        private bool IsExtendedTriggerDataBlock(string dataBlock, out GameObject prefab)
        {
            prefab = dataBlock.ToLowerInvariant() switch
            {
                "accelerationtrigger" => accelerationTriggerPrefab,
                "alignmenttrigger" => alignmentTriggerPrefab,
                "altergravitytrigger" => alterGravityTriggerPrefab,
                "cameradistancetrigger" => cameraDistanceTriggerPrefab,
                "cameratrigger" => cameraTriggerPrefab,
                "cancelvelocitytrigger" => cancelVelocityTriggerPrefab,
                "changemarblesizetrigger" => changeMarbleSizeTriggerPrefab,
                "checkpointtrigger" => checkpointTriggerPrefab,
                "countdownstarttrigger" => countdownStartTimerPrefab,
                "countdownstoptrigger" => countdownStopTimerPrefab,
                "disableshapeforcetrigger" => disableShapeForceTriggerPrefab,
                "finishtrigger" => finishTriggerPrefab,
                "gemchangetrigger" => gemChangeTriggerPrefab,
                "gravitypointtrigger" => gravityPointTriggerPrefab,
                "gravitytrigger" => gravityTriggerPrefab,
                "lockpoweruptrigger" => lockPowerupTriggerPrefab,
                "megamanemulationtrigger" => megaManEmulationTriggerPrefab,
                "multipletgtt" => multipleTGTTTriggerPrefab,
                "musictrigger" => musicTriggerPrefab,
                "mustchangetrigger" => mustChangeTriggerPrefab,
                "nomovementkeystrigger" => noMovementKeysTriggerPrefab,
                "relativetptrigger" => relativeTPTriggerPrefab,
                "repetitivetriggergototarget" => repetitiveTriggerGotoTargetPrefab,
                "setvelocitytrigger" => setVelocityTriggerPrefab,
                "smbtrigger" => smbTriggerPrefab,
                "soundtrigger" => soundTriggerPrefab,
                "spawntrigger" => spawnTriggerPrefab,
                "tdtrigger" => TDTriggerPrefab,
                "timestoptrigger" => timeStopTriggerPrefab,
                "timetraveltrigger" => timeTravelTriggerPrefab,
                "usepoweruptrigger" => usePowerupTriggerPrefab,
                _ => null
            };

            return prefab != null;
        }

        private bool IsPushButtonDataBlock(string datablock, out GameObject prefab, out bool isToggle)
        {
            prefab = null;
            isToggle = false;

            switch (datablock.ToLowerInvariant())
            {
                case "pushbutton_pq":
                    prefab = pushButtonRegularPrefab;
                    return true;
                case "pushbuttonflat_pq":
                    prefab = pushButtonFlatPrefab;
                    return true;
                case "togglebutton_pq":
                    prefab = pushButtonExtendedPrefab;
                    isToggle = true;
                    return true;
                case "togglebuttonflat_pq":
                    prefab = pushButtonFlatHalfPrefab;
                    isToggle = true;
                    return true;
                default:
                    return false;
            }
        }

        private bool IsPropeller(string name, out GameObject prefab)
        {
            prefab = name.ToLowerInvariant() switch
            {
                "propeller" => propellerPrefab,
                "proplarge1" => propellerLarge1Prefab,
                "proplarge2" => propellerLarge2Prefab,
                "proplarge3" => propellerLarge3Prefab,
                "proplarge4" => propellerLarge4Prefab,
                "proplarge5" => propellerLarge5Prefab,
                "propsmall1" => propellerSmall1Prefab,
                "propsmall2" => propellerSmall2Prefab,
                "propsmall3" => propellerSmall3Prefab,
                "propsmall4" => propellerSmall4Prefab,
                "propsmall5" => propellerSmall5Prefab,
                "proplargereverse1" => propellerLargeReverse1Prefab,
                "proplargereverse2" => propellerLargeReverse2Prefab,
                "proplargereverse3" => propellerLargeReverse3Prefab,
                "proplargereverse4" => propellerLargeReverse4Prefab,
                "proplargereverse5" => propellerLargeReverse5Prefab,
                "propsmallreverse1" => propellerSmallReverse1Prefab,
                "propsmallreverse2" => propellerSmallReverse2Prefab,
                "propsmallreverse3" => propellerSmallReverse3Prefab,
                "propsmallreverse4" => propellerSmallReverse4Prefab,
                "propsmallreverse5" => propellerSmallReverse5Prefab,
                _ => null,
            };

            return prefab != null;
        }

        private bool IsPowerup(string name, out GameObject prefab, out string defaultName)
        {
            prefab = name switch
            {
                "AntiGravityItem_PQ" => antiGravityPrefab,
                "NoRespawnAntiGravityItem_PQ" => antiGravityPrefab,
                "SuperJumpItem_PQ" => superJumpPrefab,
                "CustomSuperJumpItem_PQ" => superJumpPrefab,
                "SuperSpeedItem_PQ" => superSpeedPrefab,
                "SuperBounceItem_PQ" => superBouncePrefab,
                "ShockAbsorberItem_PQ" => shockAbsorberPrefab,
                "HelicopterItem_PQ" => gyrocopterPrefab,
                "BubbleItem" => bubblePrefab,
                "FireballItem" => fireballPrefab,
                "FireBallItem" => fireballPrefab,
                "TeleportItem" => teleporterPrefab,
                "AnvilItem" => anvilPrefab,
                _ => null,
            };

            defaultName = name switch
            {
                "AntiGravityItem_PQ" => "AntiGravityItem",
                "NoRespawnAntiGravityItem_PQ" => "AntiGravityItem",
                "SuperJumpItem_PQ" => "SuperJumpItem",
                "SuperSpeedItem_PQ" => "SuperSpeedItem",
                "SuperBounceItem_PQ" => "SuperBounceItem",
                "ShockAbsorberItem_PQ" => "ShockAbsorberItem",
                "HelicopterItem_PQ" => "HelicopterItem",
                "BubbleItem" => "BubbleItem",
                "FireballItem" => "FireballItem",
                "TeleportItem" => "TeleportItem",
                "AnvilItem" => "AnvilItem",
                _ => null,
            };

            return prefab != null;
        }

        private bool IsFinishSign(string name, out GameObject prefab)
        {
            prefab = name switch
            {
                "RegularFinishlinesign" => regularFinishlinesign,
                "ConsFinishlinesign" => consFinishlinesign,
                "ConsFinishlinesignNocrane" => consFinishlinesignNocrane,
                "NatureFinishlinesignDark" => natureFinishlinesignDark,
                "NatureFinishlinesignLight" => natureFinishlinesignLight,
                _ => null,
            };

            return prefab != null;
        }

        private bool IsHazard(string name, out GameObject prefab, out string hazardName)
        {
            (prefab, hazardName) = name.ToLowerInvariant() switch
            {
                "trapdoor_pq" => (trapdoorPrefab, "Trapdoor"),
                "ductfan_pq" => (ductFanPrefab, "DuctFan"),
                "smallductfan_pq" => (ductFanPrefab, "SmallDuctFan"),
                "vvductfan" => (ductFanPrefab, "VVDuctFan"),
                "tornado_pq" => (tornadoPrefab, "Tornado"),
                "landmine_pq" => (landMinePrefab, "LandMine"),
                "nuke_pq" => (nukePrefab, "Nuke"),
                "roundbumper_pq" => (roundBumperPrefab, "RoundBumper"),
                "trianglebumper_pq" => (triangleBumperPrefab, "TriangleBumper"),
                "iceslick" => (iceSlick, "IceSlick"),
                "iceslick1" => (iceSlick1, "IceSlick1"),
                "iceslick2" => (iceSlick2, "IceSlick2"),
                "iceslick3" => (iceSlick3, "IceSlick3"),
                "iceslick4" => (iceSlick4, "IceSlick4"),
                _ => (null, null),
            };

            return prefab != null;
        }

        private bool IsCannonDataBlock(string dataBlock)
        {
            if (string.IsNullOrEmpty(dataBlock)) return false;
            string db = dataBlock.ToLowerInvariant();
            return db == "defaultcannon" || db == "cannon_custom" || db == "cannon_low" || db == "cannon_mid" || db == "cannon_high";
        }

        private void ApplySkins(GameObject gobj, string skin, bool useBase = true)
        {
            if (string.IsNullOrEmpty(skin) && useBase) skin = "base";
            foreach (SkinSwapper skinSwapper in FindSkinSwappers(gobj))
            {
                if (!string.IsNullOrEmpty(skin)) skinSwapper.skinName = skin;
                else skinSwapper.ApplyRandomSkin();
            }
        }

        private SkinSwapper[] FindSkinSwappers(GameObject obj)
        {
            List<SkinSwapper> result = new List<SkinSwapper>(obj.GetComponentsInChildren<SkinSwapper>(true));
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                if (parent.TryGetComponent<SkinSwapper>(out var skinSwapper)) result.Add(skinSwapper);
                parent = parent.parent;
            }

            return result.Distinct().ToArray();
        }

        private GameObject GetSceneryPrefab(string datablock)
        {
            if (sceneryObjects == null) return null;
            var field = typeof(SceneryDatabase).GetField(datablock);
            return field?.GetValue(sceneryObjects) as GameObject;
        }

        private AudioClip ResolveAudioClip(string soundName) => string.IsNullOrEmpty(soundName) ? null : Resources.Load<AudioClip>(soundName.Replace(".wav", ""));

        private PowerupType ParsePowerupType(string value)
        {
            if (string.IsNullOrEmpty(value)) return PowerupType.None;
            string name = value.Replace("Item", "").Replace("_PQ", "").ToLowerInvariant();

            return name switch
            {
                "superjump" => PowerupType.SuperJump,
                "superbounce" => PowerupType.SuperBounce,
                "shockabsorber" => PowerupType.ShockAbsorber,
                "gyrocopter" => PowerupType.Gyrocopter,
                "timetravel" => PowerupType.TimeTravel,
                "timepenalty" => PowerupType.TimePenalty,
                _ => PowerupType.None
            };
        }

        private void InitializeSpecialMissionMode()
        {
            GameManager.instance.specialGameMode = null;

            switch (MissionInfo.instance.specialMissionMode)
            {
                case SpecialMissionMode.Arkanoid:
                    GameManager.instance.specialGameMode =
                        new PlatinumQuestScripts.ArkanoidMode(gameObject);
                    break;

                case SpecialMissionMode.BagOfSecrets:
                    GameManager.instance.specialGameMode =
                        new PlatinumQuestScripts.BagOfSecretsMode(gameObject);
                    break;

                case SpecialMissionMode.BlastToTheBeat:
                    GameManager.instance.specialGameMode =
                        new PlatinumQuestScripts.BlastToTheBeatMode(gameObject);
                    break;

                case SpecialMissionMode.SacredGround:
                    GameManager.instance.specialGameMode =
                        new PlatinumQuestScripts.SacredGroundMode(gameObject);
                    break;

                case SpecialMissionMode.TakeTheGold:
                    GameManager.instance.specialGameMode =
                        new PlatinumQuestScripts.TakeTheGoldMode(gameObject);
                    break;

                case SpecialMissionMode.WhiteNoise:
                    GameManager.instance.specialGameMode =
                        new PlatinumQuestScripts.WhiteNoiseMode(gameObject);
                    break;

                case SpecialMissionMode.MinuteMinute:
                    GameManager.instance.specialGameMode =
                        new PlatinumQuestScripts.MinuteMinuteMode(gameObject);
                    break;

                case SpecialMissionMode.ArcticInferno:
                    GameManager.instance.specialGameMode =
                        new PlatinumQuestScripts.ArcticInfernoMode(GameManager.instance);
                    break;

                case SpecialMissionMode.Vice:
                    GameManager.instance.specialGameMode =
                        new ViceMode(GameManager.instance);
                    break;

                case SpecialMissionMode.Versa:
                    GameManager.instance.specialGameMode =
                        new VersaMode(GameManager.instance);
                    break;

                case SpecialMissionMode.UnseasonablyCold:
                    GameManager.instance.specialGameMode =
                        new PlatinumQuestScripts.UnseasonablyColdMode(
                            gameObject
                        );
                    break;

                case SpecialMissionMode.None:
                default:
                    break;
            }

            if (GameManager.instance.specialGameMode == null)
                return;

            // Minute Minute needs access to the original MCS SimGroups.
            if (GameManager.instance.specialGameMode
                is PlatinumQuestScripts.MinuteMinuteMode minuteMinute)
            {
                foreach (TSObject obj in MissionObjects[0].RecursiveChildren())
                {
                    if (!string.Equals(
                            obj.ClassName,
                            "SimGroup",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    minuteMinute.RegisterToggleGroup(
                        obj,
                        TryGetImportedObject
                    );
                }
            }

            GameManager.instance.specialGameMode.OnMissionLoad();
        }

        private bool TryGetImportedObject(
            TSObject obj,
            out GameObject gameObject)
        {
            return importedTSObjects.TryGetValue(
                obj,
                out gameObject
            );
        }

        public bool TryGetImportedObject(
    string objectName,
    out GameObject gameObject)
        {
            gameObject = null;

            if (string.IsNullOrWhiteSpace(objectName))
                return false;

            string normalized =
                NormalizeMissionObjectName(objectName);

            if (string.IsNullOrEmpty(normalized))
                return false;

            return importedObjects.TryGetValue(
                normalized,
                out gameObject
            );
        }

        public GameObject ResolveImportedObjectPublic(
            string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            string normalized =
                NormalizeMissionObjectName(objectName);

            if (string.IsNullOrEmpty(normalized))
                return null;

            importedObjects.TryGetValue(
                normalized,
                out GameObject gameObject
            );

            return gameObject;
        }

        #endregion

        private IEnumerator DelayBeforeRespawn()
        {
            while (!GameManager.instance.startPad)
                yield return null;

            while (!GameUIManager.instance)
                yield return null;

            MarbleInfo.instance.ApplyMesh();

            GameUIManager.instance.Init();
            globalMarble.GetComponent<Movement>().GenerateMeshData();

            Time.timeScale = 1f;
            GameManager.instance.InitGemCount();

            GameManager.instance.InitializeHuntCheckpoint();

            foreach (IGameMode gm in GameManager.instance.GameModes)
                gm.OnMissionLoad();

            GameManager.instance.SetSoundVolumes();
            GameManager.instance.PlayLevelMusic();

            directionalLight.GetComponent<Light>().shadows = PlayerPrefs.GetInt("Graphics_Shadow", 1) == 1 ? LightShadows.Soft : LightShadows.None;
            directionalLight.intensity *= (sunColor != null && sunColor.Length >= 3) ? Mathf.Max(sunColor[0], sunColor[1], sunColor[2]) : 1f;

            Scene loadingScene = SceneManager.GetSceneByName("Loading");
            if (loadingScene.IsValid() && loadingScene.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(loadingScene);
                while (!unloadOp.isDone) yield return null;
            }

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(MissionInfo.instance.skyboxName));
            CameraController.instance.GetComponent<Camera>().enabled = true;
            GameUIManager.instance.GetComponent<Canvas>().enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false; 

            Marble.instance.GetComponent<SphereCollider>().enabled = true;
            Invoke(nameof(EnableSounds), 0.1f);
            Marble.onRespawn?.Invoke();
            GetComponent<ReplayRecorder>().enabled = true;
        }

        private void EnableSounds()
        {
            var sounds = Marble.instance.transform.Find("Sounds");
            var audioSources = sounds.GetComponentsInChildren<AudioSource>(true);
            audioSources.First(a => a.name == "Rolling").Play();
            audioSources.First(a => a.name == "Sliding").Play();
        }

        private bool TryParseSpawnPoint(
    string value,
    out Vector3 position,
    out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            string[] parts =
                value.Split(
                    new[] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries
                );

            // x z y axisX axisY axisZ angle
            if (parts.Length < 7)
                return false;

            float[] values = new float[7];

            for (int i = 0; i < 7; i++)
            {
                if (!float.TryParse(
                        parts[i],
                        NumberStyles.Float,
                        Invariant,
                        out values[i]))
                {
                    return false;
                }
            }

            // ------------------------------------------------------------
            // Position
            //
            // Torque:
            //     X Z Y
            //
            // Unity:
            //     X Y Z
            // ------------------------------------------------------------

            position = new Vector3(
                values[0],
                values[2],
                values[1]
            );

            // ------------------------------------------------------------
            // Rotation
            //
            // Torque spawnPoint:
            //     axisX axisY axisZ angle
            //
            // The Torque -> Unity axis conversion used by the importer is:
            //     (x, y, z) -> (x, -y, z)
            //
            // The -90 degree conversion is also applied here, matching
            // SetupTriggerTransform().
            // ------------------------------------------------------------

            Vector3 axis = new Vector3(
                values[3],
                -values[4],
                values[5]
            );

            if (axis.sqrMagnitude < 0.000001f)
            {
                rotation = Quaternion.identity;
                return true;
            }

            axis.Normalize();

            Quaternion torqueRotation =
                Quaternion.AngleAxis(
                    values[6],
                    axis
                );

            rotation =
                Quaternion.Euler(-90f, 0f, 0f) *
                torqueRotation;

            return true;
        }
    }
}