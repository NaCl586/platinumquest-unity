using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TS;

namespace PlatinumQuestScripts
{
    public class MinuteMinuteMode : ISpecialGameMode
    {
        private readonly GameObject missionRoot;

        private readonly List<ToggleButtonGroup> toggleGroups =
            new List<ToggleButtonGroup>();

        // =====================================================================
        // Toggle Button Group
        // =====================================================================

        private class ToggleButtonGroup
        {
            public PushButton[] buttons;

            public int[] correctState;
            public int[] correctState1;

            public string targetPathedInterior;
            public string targetPathedInterior1;

            public string correctSfx;
            public string correctSfx1;

            public float correctTargetTime;
            public float correctTargetTime1;

            public bool solved;
            public bool solved1;
        }

        // =====================================================================
        // Object Resolver
        // =====================================================================

        public delegate bool ObjectResolver(
            TSObject obj,
            out GameObject gameObject);

        // =====================================================================
        // Constructor
        // =====================================================================

        public MinuteMinuteMode(GameObject missionRoot)
        {
            this.missionRoot = missionRoot;
        }

        // =====================================================================
        // ISpecialGameMode
        // =====================================================================

        public void OnMissionLoad()
        {
            InitializeDoors();

            Debug.Log(
                $"[MinuteMinute] Loaded. " +
                $"Registered {toggleGroups.Count} toggle-button groups."
            );
        }

        private void InitializeDoors()
        {
            SetDoorToStart("BlueDoor");
            SetDoorToStart("EggDoor");
        }

        private void SetDoorToStart(string objectName)
        {
            GameObject target =
                FindMissionObject(objectName);

            if (target == null)
            {
                Debug.LogWarning(
                    $"[MinuteMinute] Could not find '{objectName}' " +
                    "during initialization."
                );

                return;
            }

            MovingPlatform movingPlatform =
                target.GetComponent<MovingPlatform>();

            if (movingPlatform == null)
            {
                movingPlatform =
                    target.GetComponentInChildren<MovingPlatform>(true);
            }

            if (movingPlatform == null)
            {
                Debug.LogWarning(
                    $"[MinuteMinute] '{objectName}' " +
                    "does not contain MovingPlatform."
                );

                return;
            }

            movingPlatform.GoToTime(0f);
        }

        public void OnRestart()
        {
            foreach (ToggleButtonGroup group in toggleGroups)
            {
                group.solved = false;
                group.solved1 = false;
            }

            InitializeDoors();
        }

        public void OnRespawn()
        {
            // No special respawn behavior.
        }

        public void Update()
        {
            for (int i = 0; i < toggleGroups.Count; i++)
            {
                CheckToggleButtonGroup(toggleGroups[i]);
            }
        }

        public void OnJump()
        {
            // No special jump behavior.
        }

        public void ProcessMaterialContact(
            Marble marble,
            CollisionInfo contact)
        {
            // No special material behavior.
        }

        // =====================================================================
        // MCS -> Toggle Button Group
        // =====================================================================

        public void RegisterToggleGroup(
            TSObject group,
            ObjectResolver resolver)
        {
            if (group == null)
                return;

            if (resolver == null)
                return;

            List<TSObject> buttonObjects =
                new List<TSObject>();

            /*
             * A Minute Minute puzzle is represented in the MCS as:
             *
             * SimGroup
             *     correctState
             *     correctState1
             *     targetPathedInterior
             *     targetPathedInterior1
             *     correctSfx
             *     correctSfx1
             *     correctTargetTime
             *     correctTargetTime1
             *
             *     StaticShape
             *         dataBlock = ToggleButtonFlat_PQ
             *
             *     StaticShape
             *         dataBlock = ToggleButtonFlat_PQ
             *
             * Only direct StaticShape children are considered part
             * of this puzzle.
             */

            foreach (TSObject child in group.GetFirstChildrens())
            {
                if (child == null)
                    continue;

                if (!string.Equals(
                        child.ClassName,
                        "StaticShape",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string dataBlock =
                    child.GetField("dataBlock");

                if (!string.Equals(
                        dataBlock,
                        "ToggleButtonFlat_PQ",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                buttonObjects.Add(child);
            }

            if (buttonObjects.Count == 0)
                return;

            List<PushButton> buttons =
                new List<PushButton>();

            foreach (TSObject buttonObject in buttonObjects)
            {
                GameObject unityObject;

                if (!resolver(
                        buttonObject,
                        out unityObject))
                {
                    Debug.LogWarning(
                        "[MinuteMinute] Could not resolve " +
                        "ToggleButtonFlat_PQ object."
                    );

                    continue;
                }

                if (unityObject == null)
                {
                    Debug.LogWarning(
                        "[MinuteMinute] Resolver returned null " +
                        "for ToggleButtonFlat_PQ object."
                    );

                    continue;
                }

                PushButton button =
                    unityObject.GetComponent<PushButton>();

                if (button == null)
                {
                    button =
                        unityObject.GetComponentInChildren<PushButton>(
                            true
                        );
                }

                if (button == null)
                {
                    Debug.LogWarning(
                        "[MinuteMinute] Imported toggle object " +
                        "does not contain a PushButton component."
                    );

                    continue;
                }

                buttons.Add(button);
            }

            if (buttons.Count != buttonObjects.Count)
            {
                Debug.LogWarning(
                    $"[MinuteMinute] Failed to resolve all " +
                    $"ToggleButtonFlat_PQ objects in " +
                    $"SimGroup '{group.Name}'."
                );

                return;
            }

            ToggleButtonGroup puzzle =
                new ToggleButtonGroup();

            puzzle.buttons =
                buttons.ToArray();

            puzzle.correctState =
                ParseStateArray(
                    buttonObjects,
                    "correctState"
                );

            puzzle.correctState1 =
                ParseStateArray(
                    buttonObjects,
                    "correctState1"
                );

            puzzle.targetPathedInterior =
                CleanField(
                    group.GetField(
                        "targetPathedInterior"
                    )
                );

            puzzle.targetPathedInterior1 =
                CleanField(
                    group.GetField(
                        "targetPathedInterior1"
                    )
                );

            puzzle.correctSfx =
                CleanField(
                    group.GetField(
                        "correctSfx"
                    )
                );

            puzzle.correctSfx1 =
                CleanField(
                    group.GetField(
                        "correctSfx1"
                    )
                );

            puzzle.correctTargetTime =
                ParseTargetTime(
                    group.GetField(
                        "correctTargetTime"
                    )
                );

            puzzle.correctTargetTime1 =
                ParseTargetTime(
                    group.GetField(
                        "correctTargetTime1"
                    )
                );

            toggleGroups.Add(puzzle);

            Debug.Log(
                $"[MinuteMinute] Registered toggle group " +
                $"'{group.Name}' | " +
                $"buttons={buttons.Count} | " +
                $"target='{puzzle.targetPathedInterior}' | " +
                $"target1='{puzzle.targetPathedInterior1}'"
            );
        }

        // =====================================================================
        // Parse State
        // =====================================================================

        private int[] ParseStateArray(
            List<TSObject> buttonObjects,
            string fieldName)
        {
            if (buttonObjects == null ||
                buttonObjects.Count == 0)
            {
                return null;
            }

            int[] states =
                new int[buttonObjects.Count];

            bool foundAny = false;

            for (int i = 0; i < buttonObjects.Count; i++)
            {
                string value =
                    buttonObjects[i].GetField(fieldName);

                if (string.IsNullOrWhiteSpace(value))
                {
                    states[i] = -1;
                    continue;
                }

                value = CleanField(value);

                if (!int.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int state))
                {
                    Debug.LogWarning(
                        $"[MinuteMinute] Invalid {fieldName} " +
                        $"value '{value}'."
                    );

                    states[i] = -1;
                    continue;
                }

                states[i] = state;
                foundAny = true;
            }

            if (!foundAny)
                return null;

            return states;
        }

        // =====================================================================
        // Parse Target Time
        // =====================================================================

        private float ParseTargetTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0f;

            value = CleanField(value);

            if (!float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float milliseconds))
            {
                Debug.LogWarning(
                    $"[MinuteMinute] Invalid target time '{value}'."
                );

                return 0f;
            }

            /*
             * MCS timing fields are milliseconds.
             *
             * Example:
             *
             *     9999 -> 9.999 seconds
             *
             * The resulting value is passed to MovingPlatform,
             * which operates in seconds.
             */

            return milliseconds / 1000f;
        }

        // =====================================================================
        // Puzzle Checking
        // =====================================================================

        private void CheckToggleButtonGroup(
            ToggleButtonGroup group)
        {
            if (group == null)
                return;

            if (group.buttons == null ||
                group.buttons.Length == 0)
            {
                return;
            }

            // -------------------------------------------------------------
            // First solution
            // -------------------------------------------------------------

            if (!group.solved &&
                group.correctState != null &&
                Matches(
                    group.buttons,
                    group.correctState))
            {
                group.solved = true;

                ActivateSolution(
                    group.targetPathedInterior,
                    group.correctSfx,
                    group.correctTargetTime
                );
            }

            // -------------------------------------------------------------
            // Second solution
            // -------------------------------------------------------------

            if (!group.solved1 &&
                group.correctState1 != null &&
                Matches(
                    group.buttons,
                    group.correctState1))
            {
                group.solved1 = true;

                ActivateSolution(
                    group.targetPathedInterior1,
                    group.correctSfx1,
                    group.correctTargetTime1
                );
            }
        }

        private bool Matches(
            PushButton[] buttons,
            int[] expected)
        {
            if (buttons == null ||
                expected == null)
            {
                return false;
            }

            if (buttons.Length != expected.Length)
                return false;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    return false;

                if (expected[i] < 0)
                    return false;

                int actual =
                    buttons[i].IsActivated()
                        ? 1
                        : 0;

                if (actual != expected[i])
                    return false;
            }

            return true;
        }

        // =====================================================================
        // Target Activation
        // =====================================================================

        private void ActivateSolution(
            string targetName,
            string sfxName,
            float targetTime)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                return;

            Debug.Log(
                $"[MinuteMinute] Puzzle solved -> " +
                $"{targetName} | targetTime={targetTime:F3}s"
            );

            GameObject target =
                FindMissionObject(targetName);

            if (target == null)
            {
                Debug.LogWarning(
                    $"[MinuteMinute] Could not find target " +
                    $"'{targetName}'."
                );

                return;
            }

            MovingPlatform movingPlatform =
                target.GetComponent<MovingPlatform>();

            if (movingPlatform == null)
            {
                movingPlatform =
                    target.GetComponentInChildren<MovingPlatform>(
                        true
                    );
            }

            if (movingPlatform == null)
            {
                Debug.LogWarning(
                    $"[MinuteMinute] Target '{targetName}' " +
                    $"does not contain a MovingPlatform."
                );

                return;
            }

            // -------------------------------------------------------------
            // Move target
            // -------------------------------------------------------------

            if (targetTime < 0f)
            {
                /*
                 * MCS uses -1 as a special target-time value.
                 *
                 * Preserve the behavior through the existing
                 * MovingPlatform helper.
                 */

                movingPlatform.SetCurrentTimeToTarget();
            }
            else
            {
                movingPlatform.GoToTime(targetTime);
            }

            // -------------------------------------------------------------
            // Correct-answer SFX
            // -------------------------------------------------------------

            PlayCorrectSound(sfxName);
        }

        // =====================================================================
        // Sound
        // =====================================================================

        private void PlayCorrectSound(
            string soundName)
        {
            if (string.IsNullOrWhiteSpace(soundName))
                return;

            GameManager.instance.PlayPlatformSurpriseSfx();
        }

        // =====================================================================
        // Mission Object Lookup
        // =====================================================================

        private GameObject FindMissionObject(
            string objectName)
        {
            if (missionRoot == null)
                return null;

            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Transform[] transforms =
                missionRoot.GetComponentsInChildren<Transform>(
                    true
                );

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform current =
                    transforms[i];

                if (current == null)
                    continue;

                if (string.Equals(
                        current.gameObject.name,
                        objectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return current.gameObject;
                }
            }

            return null;
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private string CleanField(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim().Trim('"');
        }
    }
}