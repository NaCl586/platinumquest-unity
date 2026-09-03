using System;
using System.Globalization;
using UnityEngine;

public class AlignmentTrigger : MonoBehaviour
{
    private enum AlignmentMode
    {
        None,
        Trigger,
        Value
    }

    [Header("Alignment (Torque/MIS axes)")]
    public string x = "none";
    public string y = "none";
    public string z = "none";

    [Header("Options")]
    public bool alwaysOn;

    private AlignmentMode xMode;
    private AlignmentMode yMode;
    private AlignmentMode zMode;

    private float xValue;
    private float yValue;
    private float zValue;

    private Collider triggerCollider;

    private void Start()
    {
        triggerCollider = GetComponent<Collider>();

        ParseAxis(
            x,
            out xMode,
            out xValue,
            true
        );

        ParseAxis(
            y,
            out yMode,
            out yValue,
            false
        );

        ParseAxis(
            z,
            out zMode,
            out zValue,
            false
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsMarble(other))
            return;

        if (Movement.instance != null)
            Align(Movement.instance);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!alwaysOn)
            return;

        if (!IsMarble(other))
            return;

        if (Movement.instance != null)
            Align(Movement.instance);
    }

    private bool IsMarble(Collider other)
    {
        if (other == null)
            return false;

        if (Marble.instance == null)
            return false;

        return other.transform == Marble.instance.transform
            || other.transform.IsChildOf(
                Marble.instance.transform
            );
    }

    private void Align(Movement movement)
    {
        if (movement == null)
            return;

        if (triggerCollider == null)
        {
            triggerCollider =
                GetComponent<Collider>();
        }

        if (triggerCollider == null)
        {
            Debug.LogWarning(
                $"AlignmentTrigger on {gameObject.name}: " +
                "No Collider found."
            );

            return;
        }

        Vector3 position =
            movement.transform.position;

        Vector3 center =
            triggerCollider.bounds.center;

        // =========================================================
        // Torque X
        // =========================================================

        if (xMode != AlignmentMode.None)
        {
            position.x =
                xMode == AlignmentMode.Trigger
                    ? center.x
                    : xValue;

            movement.marbleVelocity.x = 0f;

            movement.marbleAngularVelocity.y = 0f;
        }

        // =========================================================
        // Torque Y
        // =========================================================

        if (yMode != AlignmentMode.None)
        {
            position.z =
                yMode == AlignmentMode.Trigger
                    ? center.z
                    : yValue;

            movement.marbleVelocity.z = 0f;

            movement.marbleAngularVelocity.x = 0f;
        }

        // =========================================================
        // Torque Z
        // =========================================================

        if (zMode != AlignmentMode.None)
        {
            position.y =
                zMode == AlignmentMode.Trigger
                    ? center.y
                    : zValue;

            movement.marbleVelocity.y = 0f;
        }

        movement.SetAlignedPosition(position);
    }

    private void ParseAxis(
        string value,
        out AlignmentMode mode,
        out float parsedValue,
        bool negateValue)
    {
        parsedValue = 0f;

        if (
            string.IsNullOrEmpty(value)
            || value.Equals(
                "none",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            mode = AlignmentMode.None;
            return;
        }

        if (
            value.Equals(
                "trigger",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            mode = AlignmentMode.Trigger;
            return;
        }

        if (
            float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result
            )
        )
        {
            mode = AlignmentMode.Value;

            parsedValue =
                negateValue
                    ? -result
                    : result;

            return;
        }

        Debug.LogWarning(
            $"AlignmentTrigger on {gameObject.name}: " +
            $"Could not parse '{value}'. " +
            "Treating it as none."
        );

        mode = AlignmentMode.None;
    }
}