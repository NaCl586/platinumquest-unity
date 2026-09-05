using System.Collections.Generic;
using UnityEngine;

public class GameObjectPathFollower
{
    private readonly GameObject target;
    private readonly PathManager pathManager;
    private readonly string firstNodeName;

    private string currentNodeName;
    private string prevNodeName;

    private float pathPosition;
    private int rngCursor;

    private bool ended;

    private PathFollowerState frameStartState;
    private PathFollowerState frameEndState;

    private float frameDuration;
    private float substepAccum;

    private readonly Vector3 initialPosition;
    private readonly Quaternion initialRotation;
    private readonly Vector3 initialScale;
    private readonly Vector3 originalObjectScale;

    private Vector3 linearVelocity;
    private Vector3 angularVelocity;

    // Transform history.
    private Vector3 previousPosition;
    private Quaternion previousRotation;

    public Vector3 PreviousPosition => previousPosition;

    public Quaternion PreviousRotation => previousRotation;

    public Vector3 CurrentPosition => target != null ? target.transform.position : previousPosition;

    public Quaternion CurrentRotation =>
        target != null ? target.transform.rotation : previousRotation;

    public Vector3 LinearVelocity => linearVelocity;

    public Vector3 AngularVelocity => angularVelocity;

    private class PathFollowerState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    public GameObjectPathFollower(
    GameObject target,
    string firstNodeName,
    PathManager pathManager,
    Vector3? initialPathPosition = null,
    Quaternion? initialPathRotation = null,
    Vector3? initialPathScale = null
)
    {
        this.target = target;
        this.pathManager = pathManager;

        PathOriginalScale originalScaleComponent =
            target != null
                ? target.GetComponent<PathOriginalScale>()
                : null;

        originalObjectScale =
            originalScaleComponent != null
                ? originalScaleComponent.scale
                : target.transform.localScale;

        initialPosition =
            initialPathPosition ?? target.transform.position;

        initialRotation =
            initialPathRotation ?? target.transform.rotation;

        initialScale =
            initialPathScale ?? target.transform.localScale;

        // Make the actual GameObject start at the
        // specified initial path transform.
        target.transform.SetPositionAndRotation(
            initialPosition,
            initialRotation
        );

        target.transform.localScale = initialScale;

        previousPosition = initialPosition;
        previousRotation = initialRotation;

        this.firstNodeName = firstNodeName.ToLowerInvariant();

        currentNodeName = this.firstNodeName;

        prevNodeName = currentNodeName;

        rngCursor = UnityEngine.Random.Range(0, 256);

        PathFollowerState state =
            EvaluateTransform(
                currentNodeName,
                prevNodeName,
                0f
            );

        if (state != null)
        {
            frameStartState = state;
            frameEndState = CloneState(state);

            ApplyState(
                state.position,
                state.rotation,
                state.scale
            );
        }
    }

    // =========================================================
    // NODE LOOKUP
    // =========================================================

    private PathNode GetNode(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (pathManager == null)
            return null;

        pathManager.TryGetNode(name, out PathNode node);

        return node;
    }

    // =========================================================
    // NEXT NODE
    // =========================================================

    private string PickNextNode(PathNode node)
    {
        if (node == null)
            return null;

        if (node.branchNodes != null && node.branchNodes.Count > 0)
        {
            int index = rngCursor % node.branchNodes.Count;

            return node.branchNodes[index];
        }

        return node.nextNode;
    }

    // =========================================================
    // TIMING
    // =========================================================

    private float GetPathTime(PathNode node)
    {
        if (node == null)
            return 0f;

        if (node.speed > 0f && !node.isBezier)
        {
            string nextName = PickNextNode(node);

            PathNode next = GetNode(nextName);

            if (next == null)
                return node.delay;

            float distance = Vector3.Distance(node.localPosition, next.localPosition);

            return node.delay + distance / node.speed;
        }

        return node.delay + node.timeToNext;
    }

    // =========================================================
    // PATH SIMULATION
    // =========================================================

    public void ComputeNextPathStep(float dt)
    {
        if (ended)
            return;

        dt = Mathf.Max(0f, dt);

        frameStartState = EvaluateTransform(currentNodeName, prevNodeName, pathPosition);

        pathPosition += dt;

        int safety = 0;

        while (safety++ < 100)
        {
            PathNode node = GetNode(currentNodeName);

            if (node == null)
            {
                ended = true;
                break;
            }

            float segmentTime = GetPathTime(node);

            // -------------------------------------------------
            // Zero-duration node.
            // -------------------------------------------------

            if (segmentTime <= 0f)
            {
                string nextName = PickNextNode(node);

                if (string.IsNullOrEmpty(nextName))
                {
                    ended = true;
                    break;
                }

                PathNode next = GetNode(nextName);

                if (next == null)
                {
                    ended = true;
                    break;
                }

                if (node.IsBranching())
                {
                    rngCursor = (rngCursor + 1) % 256;
                }

                prevNodeName = currentNodeName;

                currentNodeName = nextName;

                continue;
            }

            // Still inside this segment.
            if (pathPosition < segmentTime)
                break;

            // Consume the segment.
            pathPosition -= segmentTime;

            string nextNodeName = PickNextNode(node);

            if (string.IsNullOrEmpty(nextNodeName))
            {
                ended = true;
                break;
            }

            PathNode nextNode = GetNode(nextNodeName);

            if (nextNode == null)
            {
                ended = true;
                break;
            }

            if (node.IsBranching())
            {
                rngCursor = (rngCursor + 1) % 256;
            }

            prevNodeName = currentNodeName;

            currentNodeName = nextNodeName;
        }

        frameEndState = EvaluateTransform(currentNodeName, prevNodeName, pathPosition);

        // Calculate velocity once per path simulation step.
        // This uses the same dt that advances the path, so the
        // velocity is synchronized with the custom physics step.
        if (frameStartState != null && frameEndState != null && dt > 0.000001f)
        {
            linearVelocity = (frameEndState.position - frameStartState.position) / dt;

            Quaternion deltaRotation =
                frameEndState.rotation * Quaternion.Inverse(frameStartState.rotation);

            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle > 180f)
                angle -= 360f;

            if (Mathf.Abs(angle) < 0.0001f || axis.sqrMagnitude < 0.000001f)
            {
                angularVelocity = Vector3.zero;
            }
            else
            {
                axis.Normalize();

                angularVelocity = axis * (angle * Mathf.Deg2Rad) / dt;
            }
        }
        else
        {
            linearVelocity = Vector3.zero;
            angularVelocity = Vector3.zero;
        }

        frameDuration = dt;
        substepAccum = 0f;
    }

    // =========================================================
    // APPLY PATH
    // =========================================================

    public void AdvancePath(float timeStep)
    {
        if (frameStartState == null || frameEndState == null)
        {
            return;
        }

        substepAccum += Mathf.Max(0f, timeStep);

        float t = frameDuration > 0f ? Mathf.Clamp01(substepAccum / frameDuration) : 1f;

        Vector3 position = Vector3.Lerp(frameStartState.position, frameEndState.position, t);

        Vector3 scale = Vector3.Lerp(frameStartState.scale, frameEndState.scale, t);

        Quaternion rotation = Quaternion.Slerp(frameStartState.rotation, frameEndState.rotation, t);

        ApplyState(position, rotation, scale);
    }

    private Vector3 velocityPosition;
    private Quaternion velocityRotation;
    private bool velocityInitialized;

    private void ApplyState(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        // Path movement is driven directly through the Transform.
        // Linear/angular velocity is calculated separately in
        target.transform.SetPositionAndRotation(position, rotation);

        target.transform.localScale = scale;
    }

    public Vector3 GetPointVelocity(Vector3 worldPoint)
    {
        Vector3 rotationCenter = target.transform.position;

        Vector3 radius = worldPoint - rotationCenter;

        Vector3 rotationalVelocity = Vector3.Cross(angularVelocity, radius);

        Vector3 result = linearVelocity + rotationalVelocity;

        return result;
    }

    // =========================================================
    // TRANSFORM EVALUATION
    // =========================================================

    private PathFollowerState EvaluateTransform(
        string nodeName,
        string previousNodeName,
        float position
    )
    {
        PathNode node = GetNode(nodeName);

        if (node == null)
            return null;

        string nextName = PickNextNode(node);

        PathNode next = GetNode(nextName);

        if (next == null)
            next = node;

        float nodeDelay = node.delay;

        float localT;

        if (nodeDelay > 0f && position < nodeDelay)
        {
            localT = 0f;
        }
        else
        {
            float p = position - nodeDelay;

            float segmentTime = GetPathTime(node) - nodeDelay;

            localT = segmentTime > 0f ? Mathf.Clamp01(p / segmentTime) : 1f;
        }

        float adjustedT = GetAdjustedProgress(node, localT);

        Vector3 basePosition = target.transform.position;

        Quaternion baseRotation = target.transform.rotation;

        Vector3 baseScale = target.transform.localScale;

        Vector3 positionResult = node.usePosition
            ? GetPathPosition(node, next, previousNodeName, adjustedT)
            : basePosition;

        Quaternion rotationResult = node.useRotation
            ? GetPathRotation(node, next, localT)
            : baseRotation;

        Vector3 pathScale = node.useScale
            ? Vector3.Lerp(
                node.localScale,
                next.localScale,
                adjustedT
            )
            : Vector3.one;

        Vector3 scaleResult =
            Vector3.Scale(
                originalObjectScale,
                pathScale
            );

        return new PathFollowerState
        {
            position = positionResult,
            rotation = rotationResult,
            scale = scaleResult,
        };
    }

    // =========================================================
    // EASING
    // =========================================================

    private float GetAdjustedProgress(PathNode node, float t)
    {
        if (node.smooth || (t <= 0.5f && node.smoothStart) || (t > 0.5f && node.smoothEnd))
        {
            return -0.5f * Mathf.Cos(t * Mathf.PI) + 0.5f;
        }

        return t;
    }

    // =========================================================
    // POSITION
    // =========================================================

    private Vector3 GetPathPosition(PathNode node, PathNode next, string previousNodeName, float t)
    {
        List<Vector3> points = GetPointList(node, next, previousNodeName);

        return Interpolate(points, t);
    }

    private List<Vector3> GetPointList(PathNode node, PathNode next, string previousNodeName)
    {
        Vector3 start = node.localPosition;

        Vector3 end = next.localPosition;

        List<Vector3> points = new List<Vector3>();

        points.Add(start);

        // -------------------------------------------------
        // Bezier handle from current node.
        // -------------------------------------------------

        if (node.isBezier && !string.IsNullOrEmpty(node.bezierHandle2))
        {
            PathNode handle = GetNode(node.bezierHandle2);

            if (handle != null)
            {
                points.Add(handle.localPosition);
            }
        }

        // -------------------------------------------------
        // Bezier handle from next node.
        // -------------------------------------------------

        if (next.isBezier && !string.IsNullOrEmpty(next.bezierHandle1))
        {
            PathNode handle = GetNode(next.bezierHandle1);

            if (handle != null)
            {
                points.Add(handle.localPosition);
            }
        }

        points.Add(end);

        return points;
    }

    private Vector3 Interpolate(List<Vector3> points, float t)
    {
        if (points.Count == 2)
        {
            return Vector3.Lerp(points[0], points[1], t);
        }

        if (points.Count == 3)
        {
            float u = 1f - t;

            return points[0] * (u * u) + points[1] * (2f * u * t) + points[2] * (t * t);
        }

        if (points.Count == 4)
        {
            float u = 1f - t;

            return points[0] * (u * u * u)
                + points[1] * (3f * u * u * t)
                + points[2] * (3f * u * t * t)
                + points[3] * (t * t * t);
        }

        return points[0];
    }

    // =========================================================
    // ROTATION
    // =========================================================

    private Quaternion GetPathRotation(PathNode current, PathNode next, float t)
    {
        if (current == null)
            return Quaternion.identity;

        if (next == null)
            return current.localRotation;

        t = Mathf.Clamp01(t);

        Quaternion startRot = current.localRotation;
        Quaternion endRot = next.localRotation;

        // Adapt identity quaternion components (1, 0, 0, 0) based on target orientation
        if (IsIdentityRotation(startRot) && !IsIdentityRotation(endRot))
        {
            startRot = AdjustIdentityToTarget(startRot, endRot);
        }
        else if (!IsIdentityRotation(startRot) && IsIdentityRotation(endRot))
        {
            endRot = AdjustIdentityToTarget(endRot, startRot);
        }

        // Get shortest angular distance via Quaternion.Angle
        float angle = Quaternion.Angle(startRot, endRot);

        if (angle < 0.001f)
            return startRot;

        Vector3 axis = GetUnityRotationAxis(current, next);

        if (axis.sqrMagnitude < 0.000001f)
            return startRot;

        axis.Normalize();

        float direction;

        // Ensure 180-degree boundary cases use the explicit clockwise direction logic
        if (Mathf.Abs(angle - 180f) <= 5f)
        {
            direction = DetermineClockwiseDirection(current, next);
        }
        else
        {
            direction = DetermineNormalDirection(startRot, endRot, axis, ref angle);
        }

        if (current.reverseRotation)
            direction *= -1f;

        float rotationAngle = angle * direction * t * current.rotationMultiplier;

        return Quaternion.AngleAxis(rotationAngle, axis) * startRot;
    }

    // =========================================================
    // NORMAL ROTATION
    // =========================================================

    private float DetermineNormalDirection(
        Quaternion startRot,
        Quaternion endRot,
        Vector3 axis,
        ref float angle
    )
    {
        Quaternion delta = endRot * Quaternion.Inverse(startRot);

        delta.ToAngleAxis(out float deltaAngle, out Vector3 deltaAxis);

        if (deltaAxis.sqrMagnitude < 0.000001f)
            return 1f;

        deltaAxis.Normalize();

        // Ensure deltaAngle stays within standard shortest-arc range [-180, 180]
        if (deltaAngle > 180f)
        {
            deltaAngle -= 360f;
        }

        // Project the true calculated delta axis onto the requested path axis
        float dot = Vector3.Dot(deltaAxis, axis);

        if (dot < 0f)
        {
            deltaAngle = -deltaAngle;
        }

        // Overwrite angle with the minimal shortest-path angle magnitude
        angle = Mathf.Abs(deltaAngle);

        return deltaAngle >= 0f ? 1f : -1f;
    }

    // Helper method to detect pure identity quaternions (1, 0, 0, 0)
    private bool IsIdentityRotation(Quaternion rot)
    {
        return Mathf.Abs(rot.w - 1f) < 0.0001f
            && Mathf.Abs(rot.x) < 0.0001f
            && Mathf.Abs(rot.y) < 0.0001f
            && Mathf.Abs(rot.z) < 0.0001f;
    }

    // Adjusts identity quaternion components to align with non-identity rotation patterns
    private Quaternion AdjustIdentityToTarget(Quaternion identityRot, Quaternion targetRot)
    {
        // Extracts component orientation signs/axes to ensure identity transforms maintain matching component axes
        Vector3 eulerTarget = targetRot.eulerAngles;

        // Preserve 0 1 0 0 conversion mapping when non-zero component profiles (like 0 1 0 90) are targeted
        if (
            Mathf.Abs(eulerTarget.x) < 0.001f
            && Mathf.Abs(eulerTarget.z) < 0.001f
            && Mathf.Abs(eulerTarget.y) > 0.001f
        )
        {
            return new Quaternion(0f, 1f, 0f, 0f);
        }

        return identityRot;
    }

    // =========================================================
    // ROTATION AXIS
    // =========================================================

    private Vector3 DetermineRotationAxis(PathNode current, PathNode next)
    {
        bool currentIdentity = IsTorqueIdentity(current);

        bool nextIdentity = IsTorqueIdentity(next);

        if (currentIdentity && !nextIdentity)
        {
            return TorqueAxisToUnity(next.torqueRotationAxis);
        }

        if (!currentIdentity && nextIdentity)
        {
            return TorqueAxisToUnity(current.torqueRotationAxis);
        }

        if (!currentIdentity)
        {
            return TorqueAxisToUnity(current.torqueRotationAxis);
        }

        return Vector3.up;
    }

    private bool IsTorqueIdentity(PathNode node)
    {
        if (node == null)
            return true;

        return Mathf.Abs(node.torqueRotationAngle) <= 0.0001f;
    }

    // =========================================================
    // TORQUE AXIS -> RELATIVE UNITY AXIS
    // =========================================================

    private Vector3 TorqueAxisToUnity(Vector3 torqueAxis)
    {
        return new Vector3(torqueAxis.x, -torqueAxis.y, torqueAxis.z).normalized;
    }

    // =========================================================
    // NORMAL ROTATION
    // =========================================================

    private float DetermineNormalDirection(PathNode current, PathNode next, Vector3 axis)
    {
        Quaternion delta = Quaternion.Inverse(current.localRotation) * next.localRotation;

        delta.ToAngleAxis(out float angle, out Vector3 deltaAxis);

        if (Vector3.Dot(deltaAxis, axis) < 0f)
        {
            angle = -angle;
        }

        if (angle > 180f)
            angle -= 360f;

        return angle >= 0f ? 1f : -1f;
    }

    // =========================================================
    // CLOCKWISE ROTATION
    // =========================================================

    private float DetermineClockwiseDirection(PathNode current, PathNode next)
    {
        PathNode reference = IsTorqueIdentity(current) && !IsTorqueIdentity(next) ? next : current;

        if (reference == null)
            return 1f;

        Vector3 axis = reference.torqueRotationAxis;

        if (axis.sqrMagnitude < 0.000001f)
            return 1f; // Default clockwise for zero/unassigned axis

        axis.Normalize();

        // Check projections against standard X, Y, and Z axes
        float dotX = Vector3.Dot(axis, Vector3.right);   // (1, 0, 0)
        float dotY = Vector3.Dot(axis, Vector3.up);      // (0, 1, 0)
        float dotZ = Vector3.Dot(axis, Vector3.forward); // (0, 0, 1)

        // Positive Axis Alignment (+X, +Y, +Z) -> Clockwise (-1)
        if (dotX > 0.99f || dotY > 0.99f || dotZ > 0.99f)
        {
            return 1f;
        }
        // Negative Axis Alignment (-X, -Y, -Z) -> Flip sign (+1) to maintain visual direction
        else if (dotX < -0.99f || dotY < -0.99f || dotZ < -0.99f)
        {
            return -1f;
        }

        // Default fallback for custom or arbitrary diagonal axes
        return 1f;
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetPath()
    {
        pathPosition = 0f;
        ended = false;

        currentNodeName = firstNodeName;
        prevNodeName = firstNodeName;

        // ---------------------------------------------------------
        // IMPORTANT:
        //
        // Reset the Transform DIRECTLY before EvaluateTransform().
        //
        // EvaluateTransform() uses target.transform.rotation as the
        // base rotation when the path does not specify rotation.
        // here can leave EvaluateTransform() seeing the old rotation.
        // ---------------------------------------------------------

        target.transform.SetPositionAndRotation(initialPosition, initialRotation);

        target.transform.localScale = initialScale;

        // ---------------------------------------------------------
        // NOW evaluate the first path state.
        // target.transform.rotation is guaranteed to be the
        // original rotation at this point.
        // ---------------------------------------------------------

        PathFollowerState state = EvaluateTransform(currentNodeName, prevNodeName, 0f);

        if (state != null)
        {
            frameStartState = state;
            frameEndState = CloneState(state);

            ApplyState(state.position, state.rotation, state.scale);
        }
        else
        {
            ApplyState(initialPosition, initialRotation, initialScale);
        }

        frameDuration = 0f;
        substepAccum = 0f;

        linearVelocity = Vector3.zero;
        angularVelocity = Vector3.zero;
    }

    public void DeactivatePath()
    {
        ended = true;

        linearVelocity = Vector3.zero;
        angularVelocity = Vector3.zero;

        frameStartState = null;
        frameEndState = null;

        frameDuration = 0f;
        substepAccum = 0f;

        ApplyState(initialPosition, initialRotation, initialScale);
    }

    // =========================================================
    // SAVE / RESTORE
    // =========================================================

    public void FillState(PathFollowerSaveState state)
    {
        if (state == null)
            return;

        state.active = !ended;

        state.pathPosition = pathPosition;

        state.currentNode = currentNodeName;

        state.prevNode = prevNodeName;

        state.rngCursor = rngCursor;
    }

    public void SetState(PathFollowerSaveState state)
    {
        if (state == null)
            return;

        pathPosition = state.pathPosition;

        currentNodeName = state.currentNode;

        prevNodeName = state.prevNode;

        rngCursor = state.rngCursor;

        ended = !state.active;

        PathFollowerState restored = EvaluateTransform(currentNodeName, prevNodeName, pathPosition);

        if (restored == null)
            return;

        frameStartState = restored;

        frameEndState = CloneState(restored);

        frameDuration = 0f;
        substepAccum = 0f;
        linearVelocity = Vector3.zero;
        angularVelocity = Vector3.zero;

        ApplyState(restored.position, restored.rotation, restored.scale);
    }

    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public bool HasPath()
    {
        return !string.IsNullOrEmpty(currentNodeName);
    }

    public bool IsEnded()
    {
        return ended;
    }

    public string CurrentNodeName => currentNodeName;

    public string NextNodeName
    {
        get
        {
            PathNode node = GetNode(currentNodeName);

            return node != null ? PickNextNode(node) : null;
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private PathFollowerState CloneState(PathFollowerState state)
    {
        return new PathFollowerState
        {
            position = state.position,
            rotation = state.rotation,
            scale = state.scale,
        };
    }

    private Vector3 GetUnityRotationAxis(PathNode current, PathNode next)
    {
        bool currentIdentity = IsTorqueIdentity(current);

        bool nextIdentity = IsTorqueIdentity(next);

        PathNode reference;

        if (currentIdentity && !nextIdentity)
        {
            // Identity -> rotation:
            // use target's axis.
            reference = next;
        }
        else
        {
            // Rotation -> identity OR
            // rotation -> rotation:
            // use current's axis.
            reference = current;
        }

        Vector3 torqueAxis = reference.torqueRotationAxis;

        if (torqueAxis.sqrMagnitude < 0.000001f)
            return Vector3.up;

        // This is the actual axis transformation used by
        // ConvertRotation().
        Vector3 convertedAxis = new Vector3(torqueAxis.x, -torqueAxis.y, torqueAxis.z);

        convertedAxis = Quaternion.Euler(-90f, 0f, 0f) * convertedAxis;

        return convertedAxis.normalized;
    }
}
