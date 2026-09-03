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

    private readonly Dictionary<Marble, float> previousDistances =
        new Dictionary<Marble, float>();

    private readonly Dictionary<Marble, CameraDistanceState> states =
        new Dictionary<Marble, CameraDistanceState>();

    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
        {
            Debug.LogError(
                $"CameraDistanceTrigger on {gameObject.name} requires a Collider."
            );

            return;
        }

        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        // Original trigger only affects the level's main marble.
        if (marble != Marble.instance)
            return;

        CameraController camera = CameraController.instance;

        if (camera == null)
            return;

        float currentDistance = GetCameraDistance(camera);

        previousDistances[marble] = currentDistance;

        float targetDistance = distance;

        if (float.IsNaN(targetDistance))
            targetDistance = 2.5f;

        StartTransition(
            marble,
            currentDistance,
            targetDistance
        );
    }

    private void OnTriggerExit(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (marble != Marble.instance)
            return;

        if (keepEffectOnLeave)
            return;

        CameraController camera = CameraController.instance;

        if (camera == null)
            return;

        float targetDistance;

        if (Mathf.Abs(forceExitValue) > Mathf.Epsilon)
        {
            targetDistance = forceExitValue;
        }
        else if (previousDistances.TryGetValue(
                     marble,
                     out float previousDistance))
        {
            targetDistance = previousDistance;
        }
        else
        {
            targetDistance = GetCameraDistance(camera);
        }

        StartTransition(
            marble,
            GetCameraDistance(camera),
            targetDistance
        );
    }

    private void StartTransition(
        Marble marble,
        float startDistance,
        float targetDistance)
    {
        states[marble] = new CameraDistanceState
        {
            startDistance = startDistance,
            targetDistance = targetDistance,
            startTime = Time.time,
            duration = GetDuration(),
            smooth = smooth
        };
    }

    private float GetDuration()
    {
        // Original:
        // time defaults to 1000 ms
        // time / 1000 = seconds
        // invalid or <= 0 becomes 1 second.

        if (float.IsNaN(time) || time <= 0f)
            return 1f;

        return time / 1000f;
    }

    private float GetCameraDistance(CameraController camera)
    {
        Vector3 offset = camera.GetOffset();

        return offset.magnitude;
    }

    private void SetCameraDistance(
        CameraController camera,
        float targetDistance)
    {
        Vector3 offset = camera.GetOffset();

        if (offset.sqrMagnitude < 0.000001f)
        {
            // Avoid losing the camera direction if the offset somehow
            // becomes zero.
            offset = Vector3.back;
        }

        offset = offset.normalized * targetDistance;

        camera.SetOffset(offset);
    }

    private void Update()
    {
        if (states.Count == 0)
            return;

        CameraController camera = CameraController.instance;

        if (camera == null)
            return;

        List<Marble> completed = null;

        foreach (KeyValuePair<Marble, CameraDistanceState> pair in states)
        {
            Marble marble = pair.Key;
            CameraDistanceState state = pair.Value;

            if (marble == null)
            {
                if (completed == null)
                    completed = new List<Marble>();

                completed.Add(marble);
                continue;
            }

            if (marble != Marble.instance)
                continue;

            float t =
                (Time.time - state.startTime) /
                state.duration;

            if (t >= 1f)
            {
                SetCameraDistance(
                    camera,
                    state.targetDistance
                );

                if (completed == null)
                    completed = new List<Marble>();

                completed.Add(marble);

                continue;
            }

            t = Mathf.Clamp01(t);

            float eased = state.smooth
                ? 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI)
                : t;

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
        }

        if (completed != null)
        {
            foreach (Marble marble in completed)
                states.Remove(marble);
        }
    }

    public void ResetTrigger()
    {
        previousDistances.Clear();
        states.Clear();
    }

    private void OnDisable()
    {
        previousDistances.Clear();
        states.Clear();
    }
}