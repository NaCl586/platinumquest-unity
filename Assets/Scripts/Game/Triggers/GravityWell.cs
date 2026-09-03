using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class GravityWellTrigger : MonoBehaviour
{
    public enum GravityAxis
    {
        X,
        Y,
        Z,
    }

    [Header("MIS Fields")]
    public string axis = "x";

    public bool invert = false;

    public bool useRadius = false;

    public float radiusSize = 0f;

    /*
     * This is the MIS `custompoint`.
     *
     * IMPORTANT:
     * It is an absolute/world-space position.
     */
    public bool hasCustomPoint = false;

    public Vector3 customPoint;

    /*
     * Original field:
     *
     * RestoreGravity = "1 0 0 90";
     *
     * This is stored as the original string because it can either be:
     *
     *     "1"
     *
     * or
     *
     *     "axisX axisY axisZ angle"
     */
    public string restoreGravity = "";

    private Collider triggerCollider;

    /*
     * Marbles currently inside the Unity trigger.
     */
    private readonly HashSet<Marble> marblesInside = new HashSet<Marble>();

    /*
     * Equivalent to:
     *
     * var wasWithinRadius:Map<Marble, Bool>
     */
    private readonly Dictionary<Marble, bool> wasWithinRadius = new Dictionary<Marble, bool>();

    /*
     * Equivalent to:
     *
     * var restoreUp:Map<Marble, Vector>
     */
    private readonly Dictionary<Marble, Vector3> restoreUp = new Dictionary<Marble, Vector3>();

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
        {
            Debug.LogError($"GravityWellTrigger on {gameObject.name} " + $"requires a Collider.");

            return;
        }

        triggerCollider.isTrigger = true;
    }

    // ================================================================
    // CENTER
    // ================================================================

    private Vector3 GetCenter()
    {
        /*
         * Original:
         *
         * var pointField = this.element.fields.get("custompoint");
         *
         * if (pointField != null &&
         *     pointField[0] != null &&
         *     StringTools.trim(pointField[0]) != "")
         * {
         *     return MisParser.parseVector3(pointField[0]);
         * }
         *
         * return this.collider.boundingBox.getCenter().toVector();
         */

        if (hasCustomPoint)
            return customPoint;

        if (triggerCollider != null)
            return triggerCollider.bounds.center;

        return transform.position;
    }

    // ================================================================
    // RADIUS
    // ================================================================

    private bool WithinRadius(Vector3 marblePos, Vector3 center)
    {
        if (!useRadius)
            return true;

        float radius = Mathf.Max(0f, radiusSize);

        return (marblePos - center).sqrMagnitude <= radius * radius;
    }

    // ================================================================
    // GRAVITY WELL DIRECTION
    // ================================================================

    private Vector3 GetDownVector(Vector3 marblePos)
    {
        Vector3 center = GetCenter();

        Vector3 offset = marblePos - center;

        /*
         * Original:
         *
         * switch (axis)
         * {
         *     case "x":
         *         off.x = 0;
         *     case "y":
         *         off.y = 0;
         *     case "z":
         *         off.z = 0;
         * }
         */

        switch (axis.ToLowerInvariant())
        {
            case "x":
                offset.x = 0f;
                break;

            case "z":
                offset.y = 0f;
                break;

            case "y":
                offset.z = 0f;
                break;

            default:
                Debug.LogWarning(
                    $"GravityWellTrigger on {gameObject.name} "
                        + $"has invalid axis '{axis}'. "
                        + $"Expected x, y, or z."
                );
                break;
        }

        /*
         * Original:
         *
         * var direction =
         *     invert ? off.clone() : off.multiply(-1);
         */

        Vector3 direction = invert ? offset : -offset;

        /*
         * At the exact center there is no radial direction.
         *
         * Keep the current gravity to avoid NaN.
         */
        if (direction.sqrMagnitude < 0.000001f)
        {
            if (GravitySystem.GravityDir.sqrMagnitude > 0.000001f)
                return GravitySystem.GravityDir.normalized;

            return Vector3.down;
        }

        direction.Normalize();

        return direction;
    }

    // ================================================================
    // APPLY GRAVITY
    // ================================================================

    private void ApplyGravity(Vector3 gravityDirection)
    {
        if (gravityDirection.sqrMagnitude < 0.000001f)
            return;

        gravityDirection.Normalize();

        Vector3 oldGravity =
            GravitySystem.GravityDir.sqrMagnitude > 0.000001f
                ? GravitySystem.GravityDir.normalized
                : Vector3.down;

        GravitySystem.GravityDir = gravityDirection;

        if (Marble.instance != null && Marble.instance.gyrocopterBlades != null)
        {
            Marble.instance.gyrocopterBlades.transform.up = -gravityDirection;
        }

        GravityModifier.onGravityChanged?.Invoke(oldGravity, gravityDirection);
    }

    // ================================================================
    // SAVE ORIGINAL GRAVITY
    // ================================================================

    private void SaveGravity(Marble marble)
    {
        /*
         * Original:
         *
         * if (restoreField != null &&
         *     restoreField[0] == "1")
         *
         *     this.restoreUp.set(
         *         marble,
         *         marble.currentUp.clone()
         *     );
         *
         * We only save when RestoreGravity == "1".
         */

        if (!string.Equals(restoreGravity.Trim(), "1", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        /*
         * Original Marble.currentUp is UP.
         *
         * GravitySystem.GravityDir is DOWN.
         *
         * Therefore:
         *
         *     UP = -Gravity
         */

        Vector3 currentUp;

        if (GravitySystem.GravityDir.sqrMagnitude > 0.000001f)
            currentUp = -GravitySystem.GravityDir.normalized;
        else
            currentUp = Vector3.up;

        restoreUp[marble] = currentUp;
    }

    // ================================================================
    // PARSE TORQUE ROTATION
    // ================================================================

    private Vector3 ParseRestoreGravity()
    {
        float[] rotation = ParseVectorString(restoreGravity.Trim());

        if (rotation.Length != 4)
        {
            Debug.LogWarning(
                $"GravityWellTrigger on {gameObject.name}: "
                    + $"Invalid RestoreGravity '{restoreGravity}'."
            );

            return Vector3.down;
        }

        float angle = rotation[3];

        Vector3 axis = new Vector3(rotation[0], -rotation[1], rotation[2]);

        Quaternion rot = Quaternion.AngleAxis(angle, axis);

        // Torque defines:
        // 1 0 0 180 = DOWN
        //
        // Therefore the zero-degree reference is
        // the opposite direction after accounting for
        // the 180-degree X reference.

        Quaternion reference = Quaternion.AngleAxis(180f, Vector3.right);

        Vector3 gravity = rot * Quaternion.Inverse(reference) * Vector3.down;

        return gravity.normalized;
    }

    private Quaternion ConvertRotation(float[] torqueRotation, bool additionalRotate = true)
    {
        // Torque point is an angle axis in torquespace
        float angle = torqueRotation[3];
        Vector3 axis = new Vector3(torqueRotation[0], -torqueRotation[1], torqueRotation[2]);

        Quaternion rot = Quaternion.AngleAxis(angle, axis);

        if (additionalRotate)
            rot = Quaternion.Euler(-90.0f, 0, 0) * rot;

        return rot;
    }

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private float[] ParseVectorString(string vs)
    {
        return vs.Split(' ').Select(s => float.Parse(s, Invariant)).ToArray();
    }

    // ================================================================
    // RESTORE GRAVITY
    // ================================================================

    private void RestoreGravity(Marble marble)
    {
        string value = restoreGravity.Trim();

        // Blank = do nothing
        if (string.IsNullOrEmpty(value))
            return;

        // "1" = restore gravity saved on entry
        if (value == "1")
        {
            if (restoreUp.TryGetValue(marble, out Vector3 savedUp))
            {
                ApplyGravity(-savedUp);
                restoreUp.Remove(marble);
            }
            else
            {
                // Original fallback:
                // new Vector(0, 0, 1) = UP
                // GravitySystem wants DOWN
                ApplyGravity(Vector3.back);
            }

            return;
        }

        // Anything else = axis-angle rotation
        Vector3 gravity = ParseRestoreGravity();
        ApplyGravity(gravity);
    }

    // ================================================================
    // PROCESS MARBLE
    // ================================================================

    private void ProcessMarble(Marble marble)
    {
        if (marble == null)
            return;

        Vector3 marblePos = marble.transform.position;

        Vector3 center = GetCenter();

        bool within = WithinRadius(marblePos, center);

        // ------------------------------------------------------------
        // Outside radius
        // ------------------------------------------------------------

        if (!within)
        {
            if (wasWithinRadius.TryGetValue(marble, out bool wasWithin) && wasWithin)
            {
                RestoreGravity(marble);
            }

            wasWithinRadius[marble] = false;

            return;
        }

        // ------------------------------------------------------------
        // First time entering radius
        // ------------------------------------------------------------

        if (!wasWithinRadius.TryGetValue(marble, out bool previouslyWithin) || !previouslyWithin)
        {
            SaveGravity(marble);
        }

        wasWithinRadius[marble] = true;

        // ------------------------------------------------------------
        // Apply gravity well
        // ------------------------------------------------------------

        Vector3 gravityDirection = GetDownVector(marblePos);

        /*
         * The original code does:
         *
         *     apply(
         *         marble,
         *         getDownVector(marblePos).multiply(-1)
         *     );
         *
         * `apply()` receives UP.
         *
         * Our GravitySystem stores DOWN.
         *
         * Therefore GetDownVector() itself is exactly the direction
         * we need to assign to GravitySystem.GravityDir.
         */

        ApplyGravity(gravityDirection);
    }

    // ================================================================
    // UNITY TRIGGER
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        marblesInside.Add(marble);

        ProcessMarble(marble);
    }

    private void OnTriggerExit(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        marblesInside.Remove(marble);

        wasWithinRadius.Remove(marble);

        RestoreGravity(marble);
    }

    // ================================================================
    // CUSTOM PHYSICS UPDATE
    // ================================================================

    private void FixedUpdate()
    {
        if (marblesInside.Count == 0)
            return;

        foreach (Marble marble in marblesInside)
        {
            if (marble == null)
                continue;

            ProcessMarble(marble);
        }
    }

    // ================================================================
    // CLEANUP
    // ================================================================

    private void OnDisable()
    {
        marblesInside.Clear();
        wasWithinRadius.Clear();
        restoreUp.Clear();
    }

    // ================================================================
    // DEBUG GIZMOS
    // ================================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Vector3 center;

        if (hasCustomPoint)
        {
            center = customPoint;
        }
        else
        {
            Collider col = GetComponent<Collider>();

            center = col != null ? col.bounds.center : transform.position;
        }

        if (useRadius)
            Gizmos.DrawWireSphere(center, radiusSize);

        /*
         * Draw the current radial gravity direction
         * for easier debugging in the editor.
         */

        Vector3 testPosition = center;

        Vector3 direction = invert ? Vector3.right : Vector3.left;

        switch (axis.ToLowerInvariant())
        {
            case "x":
                direction = invert ? Vector3.forward : Vector3.back;
                break;

            case "y":
                direction = invert ? Vector3.forward : Vector3.back;
                break;

            case "z":
                direction = invert ? Vector3.right : Vector3.left;
                break;
        }

        Gizmos.DrawLine(testPosition, testPosition + direction * 2f);
    }

#endif
}
