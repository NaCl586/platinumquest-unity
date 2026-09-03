using System;
using System.Globalization;
using UnityEngine;

public class AlterGravityTrigger : MonoBehaviour
{
    public enum Axis
    {
        X,
        Y,
        Z
    }

    [Header("MIS Fields")]
    public Axis measureAxis = Axis.X;
    public Axis gravityAxis = Axis.Y;
    public bool flipMeasure = false;

    [Header("Gravity Rotation")]
    public float startingGravityRot = 0f;
    public float endingGravityRot = 720f;

    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
        {
            Debug.LogError(
                $"AlterGravityTrigger on {gameObject.name} requires a Collider."
            );

            return;
        }

        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Movement movement = other.GetComponent<Movement>();

        if (movement == null)
            return;

        ApplyGravity(movement.transform.position);
    }

    private void OnTriggerStay(Collider other)
    {
        Movement movement = other.GetComponent<Movement>();

        if (movement == null)
            return;

        ApplyGravity(movement.transform.position);
    }

    private void ApplyGravity(Vector3 marblePosition)
    {
        Vector3 direction = GetDownVector(marblePosition);

        direction.Normalize();

        Vector3 oldGravity = GravitySystem.GravityDir.normalized;

        GravitySystem.GravityDir = direction;

        if (Marble.instance != null &&
            Marble.instance.gyrocopterBlades != null)
        {
            Marble.instance.gyrocopterBlades.transform.up = -direction;
        }

        GravityModifier.onGravityChanged?.Invoke(
            oldGravity,
            direction
        );
    }

    private Vector3 GetDownVector(Vector3 marblePosition)
    {
        Bounds box = triggerCollider.bounds;

        float lo;
        float hi;
        float marbleCoordinate;

        switch (measureAxis)
        {
            case Axis.X:
                lo = box.min.x;
                hi = box.max.x;
                marbleCoordinate = marblePosition.x;
                break;

            case Axis.Y:
                lo = box.min.y;
                hi = box.max.y;
                marbleCoordinate = marblePosition.y;
                break;

            case Axis.Z:
                lo = box.min.z;
                hi = box.max.z;
                marbleCoordinate = marblePosition.z;
                break;

            default:
                lo = box.min.x;
                hi = box.max.x;
                marbleCoordinate = marblePosition.x;
                break;
        }

        float t = hi > lo
            ? (marbleCoordinate - lo) / (hi - lo)
            : 0f;

        if (flipMeasure)
            t = 1f - t;

        t = Mathf.Clamp01(t);

        float rotation =
            startingGravityRot +
            (endingGravityRot - startingGravityRot) * t;

        Quaternion quat = CreateGravityRotation(rotation);

        // Original:
        //
        // var direction = new Vector(0, 0, -1);
        // direction.transform(quat.toMatrix());
        // return direction.multiply(-1);
        //
        // Vector(0, 0, -1) = Vector3.back.
        // The final multiply(-1) makes it the opposite direction.

        Vector3 direction = -(quat * Vector3.back);

        return direction.normalized;
    }

    private Quaternion CreateGravityRotation(float rotation)
    {
        Vector3 axis;

        switch (gravityAxis)
        {
            case Axis.X:
                axis = Vector3.right;
                break;

            case Axis.Y:
                axis = Vector3.up;
                break;

            case Axis.Z:
                axis = Vector3.forward;
                break;

            default:
                axis = Vector3.up;
                break;
        }

        /*
         * Equivalent to:
         *
         * var rotationStr =
         *     '${gravityAxis == 0 ? 1 : 0} ' +
         *     '${gravityAxis == 1 ? 1 : 0} ' +
         *     '${gravityAxis == 2 ? 1 : 0} $rot';
         *
         * var quat = MisParser.parseRotation(rotationStr);
         *
         * quat.x = -quat.x;
         * quat.w = -quat.w;
         */

        Quaternion quat = Quaternion.AngleAxis(rotation, axis);

        quat.x = -quat.x;
        quat.w = -quat.w;

        return quat;
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();

        if (col == null)
            return;

        Bounds box = col.bounds;

        Vector3 startPosition;
        Vector3 endPosition;

        switch (measureAxis)
        {
            case Axis.X:
                startPosition = new Vector3(
                    box.min.x,
                    box.center.y,
                    box.center.z
                );

                endPosition = new Vector3(
                    box.max.x,
                    box.center.y,
                    box.center.z
                );
                break;

            case Axis.Y:
                startPosition = new Vector3(
                    box.center.x,
                    box.min.y,
                    box.center.z
                );

                endPosition = new Vector3(
                    box.center.x,
                    box.max.y,
                    box.center.z
                );
                break;

            case Axis.Z:
                startPosition = new Vector3(
                    box.center.x,
                    box.center.y,
                    box.min.z
                );

                endPosition = new Vector3(
                    box.center.x,
                    box.center.y,
                    box.max.z
                );
                break;

            default:
                startPosition = box.min;
                endPosition = box.max;
                break;
        }

        Gizmos.DrawLine(startPosition, endPosition);
        Gizmos.DrawWireSphere(startPosition, 0.15f);
        Gizmos.DrawWireSphere(endPosition, 0.15f);
    }

#endif
}