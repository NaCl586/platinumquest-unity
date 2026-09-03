using System.Collections.Generic;
using UnityEngine;

public class GravityPointTrigger : MonoBehaviour
{
    [Header("Custom Point")]
    public bool useCustomPoint = false;

    public Vector3 customPoint;

    [Header("Radius")]
    public bool useRadius = false;

    public float radiusSize = 20f;

    [Header("Gravity")]
    public bool invert = false;

    [Header("Leave Behavior")]
    public bool upDownLeave = false;

    private readonly Dictionary<Marble, bool>
        wasWithinRadius =
            new Dictionary<Marble, bool>();

    // ============================================================
    // Trigger
    // ============================================================

    private void OnTriggerStay(Collider other)
    {
        Marble marble =
            other.GetComponent<Marble>();

        if (marble == null)
            return;

        Vector3 marblePosition =
            marble.transform.position;

        Vector3 center =
            GetCenter();

        bool within =
            WithinRadius(
                marblePosition,
                center
            );

        // --------------------------------------------------------
        // Outside the gravity radius but still inside trigger
        // --------------------------------------------------------

        if (!within)
        {
            bool wasWithin = false;

            wasWithinRadius.TryGetValue(
                marble,
                out wasWithin
            );

            if (wasWithin)
                LeaveUpDown(marble);

            wasWithinRadius[marble] = false;

            return;
        }

        // --------------------------------------------------------
        // Inside gravity radius
        // --------------------------------------------------------

        wasWithinRadius[marble] = true;

        Vector3 direction =
            GetDownVector(
                marblePosition,
                center
            );

        // Haxe:
        //
        // getDownVector(marblePos).multiply(-1)
        //
        // The resulting vector is the marble's UP direction.
        Vector3 upDirection =
            -direction;

        ApplyGravity(
            marble,
            upDirection
        );
    }

    private void OnTriggerExit(Collider other)
    {
        Marble marble =
            other.GetComponent<Marble>();

        if (marble == null)
            return;

        bool wasWithin = false;

        if (
            wasWithinRadius.TryGetValue(
                marble,
                out wasWithin
            )
        )
        {
            if (wasWithin)
                LeaveUpDown(marble);

            wasWithinRadius.Remove(marble);
        }
    }

    // ============================================================
    // Center
    // ============================================================

    private Vector3 GetCenter()
    {
        // Haxe custompoint is already a world/mission-space
        // position. The importer should perform coordinate
        // conversion when assigning this value.
        if (useCustomPoint)
            return customPoint;

        Collider collider =
            GetComponent<Collider>();

        if (collider != null)
            return collider.bounds.center;

        return transform.position;
    }

    // ============================================================
    // Radius
    // ============================================================

    private bool WithinRadius(
        Vector3 marblePosition,
        Vector3 center
    )
    {
        if (!useRadius)
            return true;

        float radius =
            radiusSize;

        if (radius < 0f)
            radius = 0f;

        return (
            marblePosition - center
        ).sqrMagnitude <=
        radius * radius;
    }

    // ============================================================
    // Gravity Direction
    // ============================================================

    private Vector3 GetDownVector(
        Vector3 marblePosition,
        Vector3 center
    )
    {
        Vector3 direction;

        if (invert)
        {
            // Haxe:
            // marblePos.sub(center)
            direction =
                marblePosition - center;
        }
        else
        {
            // Haxe:
            // center.sub(marblePos)
            direction =
                center - marblePosition;
        }

        if (direction.sqrMagnitude < 0.000001f)
            return Vector3.down;

        direction.Normalize();

        return direction;
    }

    // ============================================================
    // Apply Gravity
    // ============================================================

    private void ApplyGravity(
    Marble marble,
    Vector3 upDirection
)
    {
        if (upDirection.sqrMagnitude < 0.000001f)
            return;

        upDirection.Normalize();

        Vector3 gravityDirection = -upDirection;

        Vector3 oldGravity =
            GravitySystem.GravityDir;

        GravitySystem.GravityDir =
            gravityDirection;

        if (marble == Marble.instance)
        {
            if (Marble.instance.gyrocopterBlades != null)
            {
                Marble.instance.gyrocopterBlades.transform.up =
                    upDirection;
            }

            GravityModifier.onGravityChanged?.Invoke(
                oldGravity,
                gravityDirection
            );
        }
    }

    // ============================================================
    // Leave Behavior
    // ============================================================

    private void LeaveUpDown(
        Marble marble
    )
    {
        if (!upDownLeave)
            return;

        Vector3 marblePosition =
            marble.transform.position;

        Vector3 center =
            GetCenter();

        // Haxe:
        //
        // (marblePos.z - center.z) > 0
        //     ? (0,0,1)
        //     : (0,0,-1)
        //
        Vector3 upDirection =
            (marblePosition.y - center.y) > 0f
                ? Vector3.up
                : Vector3.down;

        ApplyGravity(
            marble,
            upDirection
        );
    }

    // ============================================================
    // Reset
    // ============================================================

    public void ResetTrigger()
    {
        wasWithinRadius.Clear();
    }
}