using System.Collections.Generic;
using UnityEngine;

public enum SmoothingType
{
    Linear,
    Accelerate,
    Spline,
}

public enum MovementMode
{
    Constant,
    Triggered,
}

[System.Serializable]
public class SequenceNumber
{
    public GameObject marker;
    public Vector3 markerPos;
    public float secondsToNext;

    // Smoothing used for the segment starting at this marker.
    public SmoothingType smoothing = SmoothingType.Linear;
}

public class MovingPlatform : MonoBehaviour
{
    // =========================================================
    // INSPECTOR
    // =========================================================

    public SmoothingType smoothing = SmoothingType.Linear;
    public MovementMode movementMode = MovementMode.Constant;

    public SequenceNumber[] sequenceNumbers;

    // >= 0 = target position time
    // -1   = move forward continuously
    // -2   = move backward continuously
    public float initialTargetPosition;

    public float initialPosition = 0f;

    // Spline density.
    public float resolution = 0.1f;


    // =========================================================
    // TRIGGER CONTROL
    // =========================================================

    [SerializeField]
    private bool triggerControlled = false;

    private bool pathActive = true;


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private Vector3 basePos;

    private Vector3[] positions;

    private float[] segmentStartTimes;
    private float[] segmentInvDurations;
    private SmoothingType[] segmentSmoothing;

    private float time;
    private float targetTime;

    private float totalTime;
    private float maxReachableTime;

    private int index;

    private SequenceNumber[] splineSequence;

    private Rigidbody[] rigidbodies;

    public float CurrentTime => time;
    public float TargetTime => targetTime;

    // =========================================================
    // LOGICAL PARENT FOLLOWERS
    // =========================================================

    private readonly List<GameObjectParentFollower> parentFollowers =
        new List<GameObjectParentFollower>();

    public void RegisterFollower(GameObjectParentFollower follower)
    {
        if (follower == null)
            return;

        if (!parentFollowers.Contains(follower))
            parentFollowers.Add(follower);
    }

    public void UnregisterFollower(GameObjectParentFollower follower)
    {
        if (follower == null)
            return;

        parentFollowers.Remove(follower);
    }

    private void NotifyParentFollowers(Vector3 newPosition)
    {
        Quaternion newRotation = transform.rotation;

        for (int i = parentFollowers.Count - 1; i >= 0; i--)
        {
            GameObjectParentFollower follower =
                parentFollowers[i];

            if (follower == null)
            {
                parentFollowers.RemoveAt(i);
                continue;
            }

            follower.ApplyNowFromParentPose(
                newPosition,
                newRotation
            );
        }
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    public void InitMovingPlatform()
    {
        basePos = transform.position;

        rigidbodies =
            GetComponentsInChildren<Rigidbody>();

        CacheMarkerPositions();
        GeneratePositions();

        SequenceNumber[] seq =
            smoothing == SmoothingType.Spline
                ? splineSequence
                : sequenceNumbers;

        segmentSmoothing =
            new SmoothingType[seq.Length];

        for (int i = 0; i < seq.Length; i++)
        {
            segmentSmoothing[i] =
                seq[i].smoothing;
        }

        if (seq == null || seq.Length < 2)
        {
            Debug.LogWarning(
                $"MovingPlatform '{name}' requires at least 2 markers.",
                this
            );

            return;
        }

        seq[seq.Length - 1].secondsToNext = 0f;

        segmentStartTimes =
            new float[seq.Length];

        segmentInvDurations =
            new float[seq.Length];

        totalTime = 0f;
        maxReachableTime = 0f;

        for (int i = 0; i < seq.Length; i++)
        {
            segmentStartTimes[i] =
                totalTime;

            float dt =
                seq[i].secondsToNext;

            segmentInvDurations[i] =
                dt > 0f
                    ? 1f / dt
                    : 0f;

            totalTime += dt;

            if (i < seq.Length - 1)
                maxReachableTime += dt;
        }

        time =
            Mathf.Clamp(
                initialPosition,
                0f,
                maxReachableTime
            );

        targetTime =
            initialTargetPosition;

        index =
            FindSegmentIndex(time);

        Vector3 startPosition =
            basePos + positions[index];

        SetPosition(startPosition);

        if (triggerControlled)
        {
            pathActive = false;

            time =
                Mathf.Clamp(
                    initialPosition,
                    0f,
                    maxReachableTime
                );

            SetPosition(
                basePos + positions[index]
            );
        }
        else
        {
            pathActive = true;
        }
    }


    // =========================================================
    // TRIGGER CONTROL SETUP
    // =========================================================

    public void SetTriggerControlled(bool value)
    {
        triggerControlled = value;

        if (value)
            pathActive = false;
        else
            pathActive = true;
    }

    public bool IsTriggerControlled()
    {
        return triggerControlled;
    }

    public bool IsPathActive()
    {
        return pathActive;
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetMP()
    {
        if (
            segmentStartTimes == null ||
            segmentStartTimes.Length < 2
        )
            return;

        if (triggerControlled)
        {
            pathActive = false;

            time =
                Mathf.Clamp(
                    initialPosition,
                    0f,
                    maxReachableTime
                );

            targetTime =
                initialTargetPosition;

            index =
                FindSegmentIndex(time);

            SetPosition(
                basePos + positions[index]
            );

            return;
        }

        pathActive = true;

        time =
            Mathf.Clamp(
                initialPosition,
                0f,
                maxReachableTime
            );

        targetTime =
            initialTargetPosition;

        index =
            FindSegmentIndex(time);

        SetPosition(
            basePos + positions[index]
        );
    }


    // =========================================================
    // ACTIVATION / DEACTIVATION
    // =========================================================

    public void ActivatePath()
    {
        pathActive = true;
    }

    public void DeactivatePath()
    {
        pathActive = false;

        time =
            Mathf.Clamp(
                initialPosition,
                0f,
                maxReachableTime
            );

        targetTime =
            initialTargetPosition;

        if (
            segmentStartTimes != null &&
            segmentStartTimes.Length >= 2
        )
        {
            index =
                FindSegmentIndex(time);

            SetPosition(
                basePos + positions[index]
            );
        }
    }


    // =========================================================
    // POSITION
    // =========================================================

    public void SetPosition(Vector3 pos)
    {
        if (rigidbodies == null)
            return;

        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb == null)
                continue;

            rb.MovePosition(pos);
        }

        // IMPORTANT:
        // MovePosition() does not immediately update the
        // Rigidbody's Transform. Therefore we explicitly pass
        // the NEW intended platform position to our logical
        // parent followers.
        NotifyParentFollowers(pos);
    }


    // =========================================================
    // TRIGGER API
    // =========================================================

    public void GoToTime(float t)
    {
        if (movementMode != MovementMode.Triggered)
            return;

        if (triggerControlled)
            pathActive = true;

        targetTime =
            Mathf.Clamp(
                t,
                0f,
                maxReachableTime
            );
    }

    public void ResetToInitialPosition()
    {
        time =
            Mathf.Clamp(
                initialPosition,
                0f,
                maxReachableTime
            );

        targetTime =
            initialTargetPosition;

        if (
            segmentStartTimes == null ||
            segmentStartTimes.Length < 2
        )
            return;

        index =
            FindSegmentIndex(time);

        SetPosition(
            basePos + positions[index]
        );
    }


    // =========================================================
    // DELAY TARGET TIME
    // =========================================================

    public float delayTargetTime = 0f;

    public void GoToDelayTargetTime()
    {
        GoToTime(delayTargetTime);
    }


    // =========================================================
    // MUST CHANGE TRIGGER SUPPORT
    // =========================================================

    /// <summary>
    /// Immediately sets the current platform time to the
    /// currently selected target time.
    ///
    /// Equivalent to the Haxe:
    ///
    /// interior.currentTime = interior.targetTime;
    /// </summary>
    public void SetCurrentTimeToTarget()
    {
        time = targetTime;

        if (
            segmentStartTimes == null ||
            segmentStartTimes.Length < 2
        )
            return;

        index =
            FindSegmentIndex(time);

        SetPosition(
            basePos + positions[index]
        );
    }

    /// <summary>
    /// Restores the current and target path times saved by Vice.
    /// Equivalent to restoring PathedInterior currentTime/targetTime
    /// in the original MBHaxe implementation.
    /// </summary>
    public void SetViceVersaState(float currentTime, float targetTime)
    {
        time = Mathf.Clamp(
            currentTime,
            0f,
            maxReachableTime
        );

        this.targetTime = Mathf.Clamp(
            targetTime,
            0f,
            maxReachableTime
        );

        if (
            segmentStartTimes == null ||
            segmentStartTimes.Length < 2
        )
        {
            return;
        }

        index = FindSegmentIndex(time);

        SetPosition(
            basePos + positions[index]
        );
    }

    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (!pathActive)
            return;

        if (
            segmentStartTimes == null ||
            segmentStartTimes.Length < 2
        )
            return;

        SequenceNumber[] seq =
            smoothing == SmoothingType.Spline
                ? splineSequence
                : sequenceNumbers;

        if (seq == null || seq.Length < 2)
            return;


        // -----------------------------------------------------
        // TRIGGERED
        // -----------------------------------------------------

        if (movementMode == MovementMode.Triggered)
        {
            if (
                targetTime >= 0f &&
                Mathf.Approximately(
                    time,
                    targetTime
                )
            )
            {
                return;
            }

            if (targetTime >= 0f)
            {
                time =
                    Mathf.MoveTowards(
                        time,
                        targetTime,
                        Time.fixedDeltaTime
                    );
            }
            else
            {
                return;
            }
        }


        // -----------------------------------------------------
        // CONSTANT
        // -----------------------------------------------------

        else
        {
            if (initialTargetPosition == -1)
            {
                time +=
                    Time.fixedDeltaTime;
            }
            else if (initialTargetPosition == -2)
            {
                time -=
                    Time.fixedDeltaTime;
            }
            else
            {
                return;
            }
        }


        // -----------------------------------------------------
        // LOOP
        // -----------------------------------------------------

        if (movementMode == MovementMode.Constant)
        {
            if (time > totalTime)
                time = 0f;
            else if (time < 0f)
                time = totalTime;
        }


        // -----------------------------------------------------
        // UPDATE SEGMENT
        // -----------------------------------------------------

        while (
            index < seq.Length - 2 &&
            time >
                segmentStartTimes[index] +
                seq[index].secondsToNext
        )
        {
            index++;
        }

        while (
            index > 0 &&
            time < segmentStartTimes[index]
        )
        {
            index--;
        }

        if (index >= seq.Length - 1)
            return;


        float segmentDuration =
            seq[index].secondsToNext;

        if (segmentDuration <= 0f)
            return;


        float t =
            (time - segmentStartTimes[index]) *
            segmentInvDurations[index];

        t =
            Mathf.Clamp01(t);


        // -----------------------------------------------------
        // SMOOTHING
        // -----------------------------------------------------

        SmoothingType currentSmoothing =
            SmoothingType.Linear;

        if (segmentSmoothing != null &&
            index >= 0 &&
            index < segmentSmoothing.Length)
        {
            currentSmoothing =
                segmentSmoothing[index];
        }

        if (currentSmoothing == SmoothingType.Accelerate)
        {
            t =
                0.5f -
                0.5f *
                Mathf.Cos(
                    t * Mathf.PI
                );
        }


        // -----------------------------------------------------
        // POSITION
        // -----------------------------------------------------

        Vector3 newPosition =
            basePos +
            Vector3.LerpUnclamped(
                positions[index],
                positions[index + 1],
                t
            );

        SetPosition(newPosition);
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private int FindSegmentIndex(float t)
    {
        if (
            segmentStartTimes == null ||
            segmentStartTimes.Length < 2
        )
        {
            return 0;
        }

        for (
            int i = 0;
            i < segmentStartTimes.Length - 1;
            i++
        )
        {
            if (
                t <
                segmentStartTimes[i + 1]
            )
            {
                return i;
            }
        }

        return segmentStartTimes.Length - 2;
    }


    private void CacheMarkerPositions()
    {
        if (sequenceNumbers == null)
            return;

        foreach (SequenceNumber sn in sequenceNumbers)
        {
            if (
                sn == null ||
                sn.marker == null
            )
                continue;

            sn.markerPos =
                sn.marker.transform.position;
        }
    }


    private void GeneratePositions()
    {
        if (
            sequenceNumbers == null ||
            sequenceNumbers.Length < 2
        )
        {
            positions = null;
            return;
        }

        if (smoothing != SmoothingType.Spline)
        {
            positions =
                new Vector3[
                    sequenceNumbers.Length
                ];

            Vector3 first =
                sequenceNumbers[0].markerPos;

            for (
                int i = 0;
                i < positions.Length;
                i++
            )
            {
                positions[i] =
                    sequenceNumbers[i].markerPos -
                    first;
            }

            return;
        }


        List<SequenceNumber> seq =
            new List<SequenceNumber>();

        Vector3 firstMarker =
            sequenceNumbers[0].markerPos;

        for (
            int i = 0;
            i < sequenceNumbers.Length - 1;
            i++
        )
        {
            GetCatmullRomSplineVectors(
                i,
                out List<Vector3> segment
            );

            float step =
                sequenceNumbers[i]
                    .secondsToNext *
                resolution;

            foreach (Vector3 p in segment)
            {
                seq.Add(
                    new SequenceNumber
                    {
                        markerPos =
                            p + firstMarker,

                        secondsToNext =
                            step
                    }
                );
            }
        }


        seq.Add(
            new SequenceNumber
            {
                markerPos =
                    sequenceNumbers[
                        sequenceNumbers.Length - 1
                    ].markerPos,

                secondsToNext = 0f
            }
        );

        splineSequence =
            seq.ToArray();

        positions =
            new Vector3[
                splineSequence.Length
            ];

        for (
            int i = 0;
            i < positions.Length;
            i++
        )
        {
            positions[i] =
                splineSequence[i].markerPos -
                firstMarker;
        }
    }


    private void GetCatmullRomSplineVectors(
        int pos,
        out List<Vector3> segment
    )
    {
        segment =
            new List<Vector3>();

        Vector3 first =
            sequenceNumbers[0].markerPos;

        Vector3 p0 =
            sequenceNumbers[
                ClampListPos(pos - 1)
            ].markerPos - first;

        Vector3 p1 =
            sequenceNumbers[pos].markerPos -
            first;

        Vector3 p2 =
            sequenceNumbers[
                ClampListPos(pos + 1)
            ].markerPos - first;

        Vector3 p3 =
            sequenceNumbers[
                ClampListPos(pos + 2)
            ].markerPos - first;

        Vector3 last = p1;

        int loops =
            Mathf.Max(
                1,
                Mathf.FloorToInt(
                    1f /
                    Mathf.Max(
                        resolution,
                        0.0001f
                    )
                )
            );

        for (
            int i = 1;
            i <= loops;
            i++
        )
        {
            float t =
                Mathf.Min(
                    i * resolution,
                    1f
                );

            Vector3 next =
                GetCatmullRomPosition(
                    t,
                    p0,
                    p1,
                    p2,
                    p3
                );

            segment.Add(last);

            last = next;
        }
    }


    private int ClampListPos(int pos)
    {
        if (pos < 0)
            return sequenceNumbers.Length - 1;

        if (pos >= sequenceNumbers.Length)
            return 0;

        return pos;
    }


    private Vector3 GetCatmullRomPosition(
        float t,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3
    )
    {
        Vector3 a =
            2f * p1;

        Vector3 b =
            p2 - p0;

        Vector3 c =
            2f * p0 -
            5f * p1 +
            4f * p2 -
            p3;

        Vector3 d =
            -p0 +
            3f * p1 -
            3f * p2 +
            p3;

        return
            0.5f *
            (
                a +
                b * t +
                c * t * t +
                d * t * t * t
            );
    }
}