using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDistanceTrigger : MonoBehaviour
{
    private class CameraDistanceState
    {
        public float startDistance;
        public float targetDistance;
        public float startTime;
        public float duration;
        public bool smooth;
        public bool restoring;
    }

    [Header("Camera Distance")]
    public float distance = 2.5f;

    [Header("Transition")]
    [Tooltip("Transition time in milliseconds, matching the original MIS field.")]
    public float time = 1000f;

    public bool smooth = true;

    [Header("Leave Behavior")]
    public bool keepEffectOnLeave = true;

    [Tooltip("0 means restore the previous camera distance.")]
    public float forceExitValue = 0f;

    private const float DistanceTolerance = 0.001f;

    private readonly Dictionary<Marble, float> previousDistances =
        new Dictionary<Marble, float>();

    private readonly Dictionary<Marble, CameraDistanceState> states =
        new Dictionary<Marble, CameraDistanceState>();

    private Collider triggerCollider;

    // Only one CameraDistanceTrigger should control the camera at a time.
    private static CameraDistanceTrigger activeTrigger;

    // True while this trigger owns the camera-distance effect.
    private bool effectActive;

    private void Awake()
    {
        triggerCollider =
            GetComponent<Collider>();

        if (triggerCollider == null)
        {
            Debug.LogError(
                $"CameraDistanceTrigger on {gameObject.name} " +
                "requires a Collider."
            );

            return;
        }

        triggerCollider.isTrigger = true;
    }

    // ============================================================
    // Trigger Enter
    // ============================================================

    private void OnTriggerEnter(Collider other)
    {
        Marble marble =
            other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (marble != Marble.instance)
            return;

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

        float currentDistance =
            GetCameraDistance(camera);

        float targetDistance =
            GetTargetDistance();

        // ---------------------------------------------------------
        // Already at the requested distance.
        //
        // Do absolutely nothing:
        // - Do not cancel another trigger.
        // - Do not start an animation.
        // - Do not take ownership of the camera.
        // ---------------------------------------------------------

        if (Mathf.Abs(
                currentDistance -
                targetDistance
            ) <= DistanceTolerance)
        {
            return;
        }

        // ---------------------------------------------------------
        // Cancel any other active camera-distance trigger.
        // ---------------------------------------------------------

        if (activeTrigger != null &&
            activeTrigger != this)
        {
            activeTrigger.CancelForNewTrigger();
        }

        activeTrigger =
            this;

        effectActive =
            true;

        // The distance before entering this trigger is what should
        // be restored when leaving, unless forceExitValue is used.
        previousDistances[marble] =
            currentDistance;

        states.Clear();

        // Start from the CURRENT camera distance.
        //
        // This is important when another camera-distance trigger
        // was already animating. The new trigger takes over from
        // wherever the camera currently is.
        StartTransition(
            marble,
            currentDistance,
            targetDistance,
            false
        );
    }

    // ============================================================
    // Trigger Exit
    // ============================================================

    private void OnTriggerExit(Collider other)
    {
        Marble marble =
            other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (marble != Marble.instance)
            return;

        // If this trigger no longer owns the effect, it was
        // cancelled by another CameraDistanceTrigger.
        //
        // In that case it must not restore the old camera distance.
        if (!effectActive ||
            activeTrigger != this)
        {
            return;
        }

        if (keepEffectOnLeave)
            return;

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

        float targetDistance;

        // A non-zero forceExitValue explicitly overrides the
        // saved previous distance.
        if (Mathf.Abs(forceExitValue) > Mathf.Epsilon)
        {
            targetDistance =
                forceExitValue;
        }
        else if (previousDistances.TryGetValue(
                     marble,
                     out float previousDistance))
        {
            targetDistance =
                previousDistance;
        }
        else
        {
            targetDistance =
                GetCameraDistance(camera);
        }

        float currentDistance =
            GetCameraDistance(camera);

        // Already at the restore distance.
        if (Mathf.Abs(
                currentDistance -
                targetDistance
            ) <= DistanceTolerance)
        {
            SetCameraDistance(
                camera,
                targetDistance
            );

            effectActive =
                false;

            if (activeTrigger == this)
                activeTrigger = null;

            ClearCameraDistanceOverride();

            return;
        }

        states.Clear();

        StartTransition(
            marble,
            currentDistance,
            targetDistance,
            true
        );
    }

    // ============================================================
    // Distance Helpers
    // ============================================================

    private float GetTargetDistance()
    {
        if (float.IsNaN(distance) ||
            float.IsInfinity(distance))
        {
            return 2.5f;
        }

        return Mathf.Max(
            0.001f,
            distance
        );
    }

    private float GetDuration()
    {
        if (float.IsNaN(time) ||
            float.IsInfinity(time) ||
            time <= 0f)
        {
            return 1f;
        }

        return time / 1000f;
    }

    private float GetCameraDistance(
        CameraController camera)
    {
        if (camera == null)
            return 0f;

        return camera.GetOffset().magnitude;
    }

    // ============================================================
    // 2D Mode
    // ============================================================

    private TwoDMode GetActiveTwoDMode()
    {
        if (GameManager.instance == null)
            return null;

        foreach (IGameMode mode in
                 GameManager.instance.GameModes)
        {
            if (mode is TwoDMode twoDMode &&
                twoDMode.Active)
            {
                return twoDMode;
            }
        }

        return null;
    }

    private void SetCameraDistance(
        CameraController camera,
        float targetDistance)
    {
        if (camera == null)
            return;

        TwoDMode twoDMode =
            GetActiveTwoDMode();

        if (twoDMode != null)
        {
            twoDMode.SetCameraDistanceOverride(
                targetDistance
            );

            return;
        }

        Vector3 offset =
            camera.GetOffset();

        if (offset.sqrMagnitude < 0.000001f)
        {
            offset =
                Vector3.back;
        }

        camera.SetOffset(
            offset.normalized *
            targetDistance
        );
    }

    private void ClearCameraDistanceOverride()
    {
        TwoDMode twoDMode =
            GetActiveTwoDMode();

        if (twoDMode != null)
        {
            twoDMode.ClearCameraDistanceOverride();
        }
    }

    // ============================================================
    // Transition
    // ============================================================

    private void StartTransition(
        Marble marble,
        float startDistance,
        float targetDistance,
        bool restoring)
    {
        // No animation is necessary if the distances are already
        // effectively identical.
        if (Mathf.Abs(
                startDistance -
                targetDistance
            ) <= DistanceTolerance)
        {
            SetCameraDistance(
                CameraController.instance,
                targetDistance
            );

            return;
        }

        states[marble] =
            new CameraDistanceState
            {
                startDistance =
                    startDistance,

                targetDistance =
                    targetDistance,

                startTime =
                    Time.time,

                duration =
                    GetDuration(),

                smooth =
                    smooth,

                restoring =
                    restoring
            };
    }

    // ============================================================
    // Update
    // ============================================================

    private void Update()
    {
        if (states.Count == 0)
            return;

        // This trigger was replaced by another trigger.
        //
        // Stop updating immediately. Do NOT clear the 2D override
        // here because the new trigger may already be using it.
        if (activeTrigger != this ||
            !effectActive)
        {
            states.Clear();
            return;
        }

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

        List<Marble> completed =
            null;

        foreach (
            KeyValuePair<Marble, CameraDistanceState>
            pair in states)
        {
            Marble marble =
                pair.Key;

            CameraDistanceState state =
                pair.Value;

            if (marble == null)
            {
                if (completed == null)
                    completed =
                        new List<Marble>();

                completed.Add(marble);
                continue;
            }

            if (marble != Marble.instance)
                continue;

            float t =
                (Time.time -
                 state.startTime) /
                state.duration;

            t =
                Mathf.Clamp01(t);

            float eased;

            if (state.smooth)
            {
                eased =
                    0.5f -
                    0.5f *
                    Mathf.Cos(
                        t * Mathf.PI
                    );
            }
            else
            {
                eased =
                    t;
            }

            float newDistance =
                Mathf.Lerp(
                    state.startDistance,
                    state.targetDistance,
                    eased
                );

            SetCameraDistance(
                camera,
                newDistance
            );

            if (t >= 1f)
            {
                SetCameraDistance(
                    camera,
                    state.targetDistance
                );

                if (state.restoring)
                {
                    effectActive =
                        false;

                    if (activeTrigger == this)
                        activeTrigger = null;

                    ClearCameraDistanceOverride();
                }
                else
                {
                    // Keep the 2D override active after the
                    // transition has finished.
                    TwoDMode twoDMode =
                        GetActiveTwoDMode();

                    if (twoDMode != null)
                    {
                        twoDMode.SetCameraDistanceOverride(
                            state.targetDistance
                        );
                    }
                }

                if (completed == null)
                    completed =
                        new List<Marble>();

                completed.Add(marble);
            }
        }

        if (completed != null)
        {
            foreach (Marble marble in completed)
            {
                states.Remove(marble);
            }
        }
    }

    // ============================================================
    // Cancellation
    // ============================================================

    private void CancelForNewTrigger()
    {
        states.Clear();

        effectActive =
            false;

        // IMPORTANT:
        //
        // Do NOT clear the TwoDMode camera-distance override here.
        //
        // The new trigger immediately replaces the value, so
        // clearing it would cause a visible one-frame snap.
    }

    // ============================================================
    // Reset
    // ============================================================

    public void ResetTrigger()
    {
        previousDistances.Clear();
        states.Clear();

        Marble marble =
            Marble.instance;

        if (marble == null ||
            triggerCollider == null)
        {
            return;
        }

        if (!IsMarbleInsideTrigger(marble))
        {
            if (activeTrigger == this)
            {
                effectActive =
                    false;

                activeTrigger =
                    null;

                ClearCameraDistanceOverride();
            }

            return;
        }

        // Resetting an active trigger gives it ownership again.
        if (activeTrigger != null &&
            activeTrigger != this)
        {
            activeTrigger.CancelForNewTrigger();
        }

        activeTrigger =
            this;

        effectActive =
            true;

        StartCoroutine(
            ReapplyAfterReset(marble)
        );
    }

    private bool IsMarbleInsideTrigger(
        Marble marble)
    {
        if (marble == null ||
            triggerCollider == null)
        {
            return false;
        }

        Collider marbleCollider =
            marble.GetComponent<Collider>();

        if (marbleCollider == null)
        {
            return triggerCollider.bounds.Contains(
                marble.transform.position
            );
        }

        return Physics.ComputePenetration(
            marbleCollider,
            marble.transform.position,
            marble.transform.rotation,

            triggerCollider,
            triggerCollider.transform.position,
            triggerCollider.transform.rotation,

            out _,
            out _
        );
    }

    private IEnumerator ReapplyAfterReset(
        Marble marble)
    {
        yield return null;

        if (marble == null ||
            marble != Marble.instance)
        {
            yield break;
        }

        if (activeTrigger != this ||
            !effectActive)
        {
            yield break;
        }

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            yield break;

        if (!IsMarbleInsideTrigger(marble))
            yield break;

        float targetDistance =
            GetTargetDistance();

        SetCameraDistance(
            camera,
            targetDistance
        );

        previousDistances[marble] =
            targetDistance;
    }

    // ============================================================
    // Disable
    // ============================================================

    private void OnDisable()
    {
        previousDistances.Clear();
        states.Clear();

        // Only the trigger that currently owns the effect is
        // allowed to clear the 2D override.
        if (activeTrigger == this)
        {
            effectActive =
                false;

            activeTrigger =
                null;

            ClearCameraDistanceOverride();
        }
    }
}