using TS;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PlatinumQuestScripts
{
    /// <summary>
    /// Implements the special IceShard gotoTarget behavior used by
    /// missions such as MOshard1.
    ///
    /// Original mission fields supported:
    ///
    ///     gotoTarget
    ///     initialPathPosition
    ///     initialPathScale
    ///     noParticles
    ///     object1, object2, ...
    ///     path1, path2, ...
    ///     Parent
    ///     parentModTrans
    ///     parentOffset
    ///     parentTransform
    ///     pathedInterior1, pathedInterior2, ...
    ///     persistTime
    ///     targetTime1, targetTime2, ...
    ///     text
    ///     TriggerOnce
    ///
    /// This class intentionally does NOT derive from MonoBehaviour.
    /// </summary>
    public class UnseasonablyColdMode : ISpecialGameMode
    {
        private readonly GameObject missionRoot;

        private MissionImporter importer;

        private readonly Dictionary<IceShard, TSObject> specialShards =
            new Dictionary<IceShard, TSObject>();

        private readonly HashSet<IceShard> triggeredShards =
            new HashSet<IceShard>();

        private readonly Dictionary<string, Coroutine> messageCoroutines =
            new Dictionary<string, Coroutine>();

        public UnseasonablyColdMode(GameObject missionRoot)
        {
            this.missionRoot = missionRoot;
        }

        // ============================================================
        // Mission lifecycle
        // ============================================================

        public void OnMissionLoad()
        {
            specialShards.Clear();
            triggeredShards.Clear();

            importer = null;

            if (missionRoot == null)
            {
                Debug.LogWarning(
                    "UnseasonablyColdMode: Mission root is null."
                );

                return;
            }

            importer =
                missionRoot.GetComponent<MissionImporter>();

            if (importer == null)
            {
                Debug.LogWarning(
                    "UnseasonablyColdMode: MissionImporter not found.",
                    missionRoot
                );

                return;
            }

            if (importer.MissionObjects == null ||
                importer.MissionObjects.Count == 0)
            {
                return;
            }

            TSObject mission =
                importer.MissionObjects[0];

            foreach (TSObject obj in mission.RecursiveChildren())
            {
                if (obj == null)
                    continue;

                if (!string.Equals(
                        obj.ClassName,
                        "StaticShape",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string dataBlock =
                    obj.GetField("dataBlock");

                if (!IsIceShardDataBlock(dataBlock))
                    continue;

                if (!ParseBool(
                        obj.GetField("gotoTarget")))
                {
                    continue;
                }

                if (!importer.TryGetImportedObject(
                        obj.Name,
                        out GameObject shardObject))
                {
                    Debug.LogWarning(
                        $"UnseasonablyColdMode: " +
                        $"Could not resolve IceShard '{obj.Name}'."
                    );

                    continue;
                }

                if (shardObject == null)
                    continue;

                IceShard shard =
                    shardObject.GetComponent<IceShard>();

                if (shard == null)
                {
                    shard =
                        shardObject.GetComponentInChildren<IceShard>(
                            true
                        );
                }

                if (shard == null)
                {
                    Debug.LogWarning(
                        $"UnseasonablyColdMode: " +
                        $"'{obj.Name}' has no IceShard component.",
                        shardObject
                    );

                    continue;
                }

                specialShards[shard] = obj;

                shard.SetGoToTargetHandler(this);

                shard.SetGoToTargetTriggered(false);
            }
        }

        public void OnRestart()
        {
            triggeredShards.Clear();

            foreach (IceShard shard in specialShards.Keys)
            {
                if (shard == null)
                    continue;

                shard.ResetGoToTarget();
            }
        }

        public void OnRespawn()
        {
            /*
             * The original IceShard gotoTarget code uses TriggerOnce
             * state for the current game/reset state.
             *
             * A normal marble respawn does not constitute a mission
             * restart, so do not clear triggeredShards here.
             */
        }

        public void Update()
        {
        }

        public void OnJump()
        {
        }

        public void ProcessMaterialContact(
            Marble marble,
            CollisionInfo contact)
        {
            /*
             * IceShard calls ProcessIceShardContact() directly because
             * this behavior belongs specifically to IceShard collision.
             */
        }

        // ============================================================
        // IceShard contact
        // ============================================================

        public void ProcessIceShardContact(
            IceShard shard,
            Marble marble)
        {
            if (shard == null)
                return;

            if (marble == null)
                return;

            if (!specialShards.TryGetValue(
                    shard,
                    out TSObject element))
            {
                return;
            }

            if (!ParseBool(
                    element.GetField("gotoTarget")))
            {
                return;
            }

            bool triggerOnce =
                ParseBoolWithDefault(
                    element,
                    "TriggerOnce",
                    true
                );

            if (triggerOnce &&
                triggeredShards.Contains(shard))
            {
                return;
            }

            if (triggerOnce)
                triggeredShards.Add(shard);

            shard.SetGoToTargetTriggered(true);

            // --------------------------------------------------------
            // objectN / pathN
            // --------------------------------------------------------

            RunObjectPathChain(element);

            // --------------------------------------------------------
            // pathedInteriorN / targetTimeN
            // --------------------------------------------------------

            RunPathedInteriors(element);

            // --------------------------------------------------------
            // Message
            // --------------------------------------------------------

            DisplayMessage(element);
        }

        // ============================================================
        // Object / Path chain
        // ============================================================

        private void RunObjectPathChain(
            TSObject element)
        {
            if (importer == null)
                return;

            for (int i = 1; ; i++)
            {
                string objectName =
                    GetField(
                        element,
                        $"object{i}"
                    );

                string pathName =
                    GetField(
                        element,
                        $"path{i}"
                    );

                // The original fields are paired:
                //
                // object1 -> path1
                // object2 -> path2
                // etc.
                //
                // Stop when neither field exists.
                if (string.IsNullOrWhiteSpace(objectName) &&
                    string.IsNullOrWhiteSpace(pathName))
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(objectName))
                {
                    Debug.LogWarning(
                        $"UnseasonablyColdMode: " +
                        $"object{i} is empty."
                    );

                    continue;
                }

                if (string.IsNullOrWhiteSpace(pathName))
                {
                    Debug.LogWarning(
                        $"UnseasonablyColdMode: " +
                        $"path{i} is empty for '{objectName}'."
                    );

                    continue;
                }

                MoveObjectOnPath(
                    objectName,
                    pathName,
                    element
                );
            }
        }

        private void MoveObjectOnPath(
            string objectName,
            string pathName,
            TSObject sourceElement)
        {
            GameObject target =
                importer.ResolveImportedObjectPublic(
                    objectName
                );

            if (target == null)
            {
                Debug.LogWarning(
                    $"UnseasonablyColdMode: " +
                    $"Could not find object '{objectName}'."
                );

                return;
            }

            PathManager pathManager =
                importer.PathManager;

            if (pathManager == null)
            {
                Debug.LogWarning(
                    "UnseasonablyColdMode: PathManager not found.",
                    missionRoot
                );

                return;
            }

            string normalizedPath =
                NormalizePathName(pathName);

            if (!pathManager.TryGetNode(
                    normalizedPath,
                    out PathNode node))
            {
                Debug.LogWarning(
                    $"UnseasonablyColdMode: " +
                    $"Could not find path '{pathName}' " +
                    $"for object '{objectName}'."
                );

                return;
            }

            PathMover mover =
                target.GetComponent<PathMover>();

            if (mover == null)
            {
                mover =
                    target.AddComponent<PathMover>();
            }

            /*
             * This is the Unity equivalent of:
             *
             *   Object[i].MoveOnPath(
             *       Path[i],
             *       InitialPosition
             *   );
             *
             * The existing PathMover is used rather than creating
             * another movement implementation.
             */
            mover.InitializePath(
                node.nodeName,
                pathManager,
                false
            );

            PathMovementManager movementManager =
                missionRoot.GetComponent<PathMovementManager>();

            if (movementManager != null)
            {
                movementManager.RegisterMovingObject(
                    mover
                );
            }
        }

        // ============================================================
        // Pathed interiors
        // ============================================================

        private void RunPathedInteriors(
            TSObject element)
        {
            if (importer == null)
                return;

            for (int i = 1; ; i++)
            {
                string interiorName =
                    GetField(
                        element,
                        $"pathedInterior{i}"
                    );

                string targetTimeField =
                    GetField(
                        element,
                        $"targetTime{i}"
                    );

                if (string.IsNullOrWhiteSpace(interiorName) &&
                    string.IsNullOrWhiteSpace(targetTimeField))
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(interiorName))
                {
                    Debug.LogWarning(
                        $"UnseasonablyColdMode: " +
                        $"pathedInterior{i} is empty."
                    );

                    continue;
                }

                GameObject target =
                    importer.ResolveImportedObjectPublic(
                        interiorName
                    );

                if (target == null)
                {
                    Debug.LogWarning(
                        $"UnseasonablyColdMode: " +
                        $"Could not find PathedInterior " +
                        $"'{interiorName}'."
                    );

                    continue;
                }

                MovingPlatform platform =
                    target.GetComponent<MovingPlatform>();

                if (platform == null)
                {
                    Debug.LogWarning(
                        $"UnseasonablyColdMode: " +
                        $"'{interiorName}' has no MovingPlatform.",
                        target
                    );

                    continue;
                }

                float targetTime =
                    ParseTargetTime(
                        targetTimeField
                    );

                platform.GoToTime(targetTime);
            }
        }

        private float ParseTargetTime(
            string value)
        {
            /*
             * Mission files store targetTime in milliseconds.
             *
             * Example:
             *
             *     targetTime1 = "99999"
             *
             * becomes:
             *
             *     99.999 seconds
             *
             * MovingPlatform.GoToTime() then clamps this against
             * the actual reachable path time.
             */

            if (string.IsNullOrWhiteSpace(value))
                return 99.999f;

            if (!float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float milliseconds))
            {
                return 99.999f;
            }

            return milliseconds / 1000f;
        }

        // ============================================================
        // Message
        // ============================================================

        private void DisplayMessage(
            TSObject element)
        {
            string text =
                GetField(
                    element,
                    "text"
                );

            if (string.IsNullOrWhiteSpace(text))
                return;

            float persistTime =
                ParseMilliseconds(
                    GetField(
                        element,
                        "persistTime"
                    ),
                    2000f
                );

            GameManager.instance.PlayHelpTriggerAudio();
            GameUIManager.instance.SetCenterText(text, persistTime);
        }

        private float ParseMilliseconds(
            string value,
            float defaultMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultMilliseconds / 1000f;

            if (!float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float milliseconds))
            {
                return defaultMilliseconds / 1000f;
            }

            return milliseconds / 1000f;
        }

        // ============================================================
        // Mission fields
        // ============================================================

        private static string GetField(
            TSObject obj,
            string fieldName)
        {
            if (obj == null)
                return null;

            string value =
                obj.GetField(fieldName);

            if (!string.IsNullOrWhiteSpace(value))
                return value;

            /*
             * Torque mission fields are case-insensitive in practice,
             * while TSObject.GetField() may depend on its implementation.
             *
             * Try the common capitalization variants used by the
             * mission files.
             */
            string lower =
                fieldName.ToLowerInvariant();

            if (lower != fieldName)
            {
                value =
                    obj.GetField(lower);

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            string upperFirst =
                char.ToUpperInvariant(fieldName[0]) +
                fieldName.Substring(1);

            if (!string.Equals(
                    upperFirst,
                    fieldName,
                    StringComparison.Ordinal))
            {
                value =
                    obj.GetField(upperFirst);

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return value;
        }

        private static bool ParseBool(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return value == "1" ||
                   value.Equals(
                       "true",
                       StringComparison.OrdinalIgnoreCase
                   );
        }

        private static bool ParseBoolWithDefault(
            TSObject obj,
            string fieldName,
            bool defaultValue)
        {
            string value =
                GetField(
                    obj,
                    fieldName
                );

            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            return ParseBool(value);
        }

        private static string NormalizePathName(
            string pathName)
        {
            if (string.IsNullOrWhiteSpace(pathName))
                return null;

            return pathName.Trim();
        }

        // ============================================================
        // IceShard identification
        // ============================================================

        private static bool IsIceShardDataBlock(
            string dataBlock)
        {
            if (string.IsNullOrWhiteSpace(dataBlock))
                return false;

            string value =
                dataBlock.ToLowerInvariant();

            return value == "iceshard1" ||
                   value == "iceshard2" ||
                   value == "pointiceshard1" ||
                   value == "pointiceshard2";
        }
    }
}
