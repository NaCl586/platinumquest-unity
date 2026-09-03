using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Torque-style logical parent relationship.
///
/// The relationship is:
///
///     objectTransform = parentTransform * parentModTrans
///     objectPosition += parentOffset
///
/// parentTransform from the mission file is NOT used.
///
/// If Parent exists but parentModTrans does not exist, the first
/// initialization captures the object's current transform relative
/// to the parent:
///
///     parentModTrans = inverse(parentTransform) * objectTransform
///
/// No Unity Transform.SetParent() is required.
/// </summary>
public sealed class GameObjectParentFollower : MonoBehaviour
{
    private Transform parentObject;

    // Relative transform represented by parentModTrans.
    private Vector3 relativePosition;
    private Quaternion relativeRotation;

    private Vector3 parentOffset;

    private bool parentSimple;
    private bool parentNoRot;

    private Vector3 ownWorldScale;

    private bool initialized;
    private bool applying;

    private Quaternion additionalRotation;

    // ------------------------------------------------------------
    // Moving Platform registration
    // ------------------------------------------------------------

    private MovingPlatform registeredMovingPlatform;


    // ============================================================
    // INITIALIZATION
    // ============================================================

    public void Initialize(
        GameObject parent,
        string parentModTransString,
        string parentOffsetString,
        bool parentSimple,
        bool parentNoRot,
        Quaternion additionalRotation)
    {
        UnregisterFromMovingPlatform();

        parentObject =
            parent != null
                ? parent.transform
                : null;

        this.parentSimple = parentSimple;
        this.parentNoRot = parentNoRot;
        this.additionalRotation = additionalRotation;

        ownWorldScale = transform.lossyScale;

        parentOffset =
            ParseParentOffset(parentOffsetString);

        if (parentObject == null)
        {
            initialized = false;
            return;
        }

        // --------------------------------------------------------
        // parentModTrans exists:
        //
        // Use the transform explicitly supplied by the mission.
        //
        // IMPORTANT:
        // additionalRotation is intentionally NOT applied here.
        //
        // parentModTrans is already a complete relative transform.
        // --------------------------------------------------------

        if (TryParseParentModTrans(
                parentModTransString,
                out relativePosition,
                out relativeRotation))
        {
            initialized = true;
        }
        else
        {
            // ----------------------------------------------------
            // Parent exists, but parentModTrans does not.
            //
            // Capture the object's CURRENT world transform as the
            // relative transform to the parent.
            //
            // This is equivalent to:
            //
            //     parentModTrans =
            //         inverse(parentTransform) * objectTransform
            // ----------------------------------------------------

            Quaternion parentRotation =
                parentObject.rotation;

            relativePosition =
                Quaternion.Inverse(parentRotation) *
                (transform.position - parentObject.position);

            relativeRotation =
                Quaternion.Inverse(parentRotation) *
                transform.rotation;

            initialized = true;
        }

        // --------------------------------------------------------
        // MovingPlatform support.
        //
        // MovingPlatform uses Rigidbody.MovePosition(), so its
        // Transform does not immediately contain the intended
        // physics pose.
        // --------------------------------------------------------

        registeredMovingPlatform =
            parentObject.GetComponent<MovingPlatform>();

        if (registeredMovingPlatform != null)
        {
            registeredMovingPlatform.RegisterFollower(this);
        }
    }


    // ============================================================
    // CLEANUP
    // ============================================================

    private void OnDestroy()
    {
        UnregisterFromMovingPlatform();
    }


    private void UnregisterFromMovingPlatform()
    {
        if (registeredMovingPlatform != null)
        {
            registeredMovingPlatform.UnregisterFollower(this);
            registeredMovingPlatform = null;
        }
    }


    // ============================================================
    // AUTOMATIC UPDATE
    // ============================================================

    /// <summary>
    /// Automatically follows normal Transform-driven parents.
    ///
    /// MovingPlatforms are excluded because they explicitly call
    /// ApplyNowFromParentPose() after calculating their intended
    /// Rigidbody pose.
    /// </summary>
    private void Update()
    {
        if (!initialized ||
            parentObject == null ||
            registeredMovingPlatform != null)
        {
            return;
        }

        ApplyNow();
    }


    // ============================================================
    // MANUAL UPDATE
    // ============================================================

    public void ApplyNow()
    {
        if (!initialized ||
            parentObject == null ||
            applying)
        {
            return;
        }

        applying = true;

        try
        {
            // ----------------------------------------------------
            // If our parent is itself logically parented, update it
            // first so we use its current world pose.
            // ----------------------------------------------------

            GameObjectParentFollower parentFollower =
                parentObject.GetComponent<GameObjectParentFollower>();

            if (parentFollower != null &&
                parentFollower != this)
            {
                parentFollower.ApplyNow();
            }

            ApplyParentTransform(
                parentObject.position,
                parentObject.rotation
            );
        }
        finally
        {
            applying = false;
        }
    }


    // ============================================================
    // MOVING PLATFORM UPDATE
    // ============================================================

    /// <summary>
    /// Called directly by MovingPlatform after it calculates its
    /// intended new physics pose.
    ///
    /// This avoids waiting for Rigidbody.MovePosition() to update
    /// Transform.position.
    /// </summary>
    public void ApplyNowFromParentPose(
        Vector3 parentPosition,
        Quaternion parentRotation)
    {
        if (!initialized ||
            parentObject == null ||
            applying)
        {
            return;
        }

        applying = true;

        try
        {
            ApplyParentTransform(
                parentPosition,
                parentRotation
            );
        }
        finally
        {
            applying = false;
        }
    }


    // ============================================================
    // APPLY TRANSFORM
    // ============================================================

    private void ApplyParentTransform(
    Vector3 parentPosition,
    Quaternion parentRotation)
    {
        Quaternion effectiveParentRotation =
            parentNoRot
                ? Quaternion.identity
                : parentRotation;

        Vector3 finalPosition;

        if (parentSimple)
        {
            finalPosition = parentPosition;
        }
        else
        {
            finalPosition =
                parentPosition +
                effectiveParentRotation *
                relativePosition;
        }

        // parent * parentModTrans
        //
        // The IceShard importer uses a +90° X model correction,
        // so that correction must remain when the follower replaces
        // the object's transform.
        Quaternion modelCorrection = Quaternion.identity;

        if (GetComponent<IceShard>() != null || GetComponent<TimeTravel>() != null)
        {
            modelCorrection = Quaternion.Euler(-90f, 0f, 0f);
        }
        else if(GetComponentInChildren<Bumper>() != null)
        {
            modelCorrection = Quaternion.Euler(90f, 0f, 0f);
        }

        Quaternion finalRotation =
            effectiveParentRotation *
            relativeRotation *
            modelCorrection * additionalRotation;

        finalPosition += parentOffset;

        transform.SetPositionAndRotation(
            finalPosition,
            finalRotation
        );

        SetWorldScale(ownWorldScale);
    }


    // ============================================================
    // parentModTrans PARSER
    // ============================================================

    /// <summary>
    /// Parses a Torque transform:
    ///
    ///     x y z axisX axisY axisZ angle
    ///
    /// Torque's transform is converted to Unity's coordinate system.
    ///
    /// Position:
    ///
    ///     Torque X Y Z
    ///          ↓
    ///     Unity  X -Y Z
    ///
    /// Rotation:
    ///
    /// Because the coordinate conversion is a reflection
    /// (Y axis is flipped), the rotation must be transformed
    /// consistently as well.
    ///
    /// Equivalent axis-angle conversion:
    ///
    ///     axisUnity  = ( axisX, -axisY, axisZ )
    ///     angleUnity = -angle
    ///
    /// This keeps the rotation physically equivalent after the
    /// handedness-changing coordinate conversion.
    /// </summary>
    private static bool TryParseParentModTrans(
    string value,
    out Vector3 position,
    out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string[] parts = value.Split(
            new[] { ' ', '\t' },
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 7)
            return false;

        if (!TryParse(parts[0], out float x) ||
            !TryParse(parts[1], out float y) ||
            !TryParse(parts[2], out float z) ||
            !TryParse(parts[3], out float axisX) ||
            !TryParse(parts[4], out float axisY) ||
            !TryParse(parts[5], out float axisZ) ||
            !TryParse(parts[6], out float angleRadians))
            return false;

        // Position is already correct in the existing importer.
        if ((MissionInfo.instance.specialMissionMode == SpecialMissionMode.UnseasonablyCold))
        {
            position = new Vector3(
                x,
                y,
                -z
            );
        }
        else
        {
            position = new Vector3(
                x,
                -y,
                z
            );
        }

        Vector3 axis = new Vector3(
            axisX,
            axisY,
            axisZ
        );

        if (axis.sqrMagnitude < 0.000001f)
        {
            rotation = Quaternion.identity;
            return true;
        }

        // Exact Torque/Haxe behavior:
        //
        // initRotateAxis(axis, -angle)
        //
        Quaternion q = Quaternion.AngleAxis(
            -angleRadians * Mathf.Rad2Deg,
            axis.normalized
        );

        // Exact conversion from the Haxe source:
        //
        // quat.x = -quat.x;
        // quat.w = -quat.w;
        //
        rotation = new Quaternion(
            -q.x,
             q.y,
             q.z,
            -q.w
        );

        return true;
    }


    // ============================================================
    // parentOffset
    // ============================================================

    private static Vector3 ParseParentOffset(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Vector3.zero;


        string[] parts =
            value.Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries
            );


        if (parts.Length < 3)
            return Vector3.zero;


        if (!TryParse(
                parts[0],
                out float x) ||
            !TryParse(
                parts[1],
                out float y) ||
            !TryParse(
                parts[2],
                out float z))
        {
            return Vector3.zero;
        }


        // Keep the existing parentOffset conversion.
        return new Vector3(
            x,
            z,
            y
        );
    }


    // ============================================================
    // NUMBER PARSING
    // ============================================================

    private static bool TryParse(
        string value,
        out float result)
    {
        return float.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result
        );
    }


    // ============================================================
    // SCALE
    // ============================================================

    private void SetWorldScale(
        Vector3 worldScale)
    {
        Transform hierarchyParent =
            transform.parent;


        if (hierarchyParent == null)
        {
            transform.localScale =
                worldScale;

            return;
        }


        Vector3 parentScale =
            hierarchyParent.lossyScale;


        transform.localScale =
            new Vector3(
                Mathf.Abs(parentScale.x) > 0.000001f
                    ? worldScale.x / parentScale.x
                    : worldScale.x,

                Mathf.Abs(parentScale.y) > 0.000001f
                    ? worldScale.y / parentScale.y
                    : worldScale.y,

                Mathf.Abs(parentScale.z) > 0.000001f
                    ? worldScale.z / parentScale.z
                    : worldScale.z
            );
    }
}