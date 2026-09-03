using System.Collections.Generic;
using UnityEngine;

public class IceShardCollision : MonoBehaviour
{
    [SerializeField]
    private IceShard iceShard;

    [Header("Detection")]
    [SerializeField]
    private float detectionInterval = 0.001f;

    [SerializeField]
    private float detectionDistance = 0.1f;

    [SerializeField]
    private LayerMask targetLayers = Physics.DefaultRaycastLayers;

    private Marble marble;
    private SphereCollider marbleSphere;
    private HashSet<Collider> iceShardColliderSet;

    private Vector3 previousPosition;
    private float timer;

    private readonly Collider[] hitBuffer = new Collider[16];
    private readonly RaycastHit[] sweepBuffer = new RaycastHit[16];

    private bool isInside;

    private void Awake()
    {
        Collider[] iceShardColliders =
            GetComponentsInChildren<Collider>(true);

        iceShardColliderSet =
            new HashSet<Collider>(iceShardColliders);

        CacheMarble();
    }

    private void Update()
    {
        if (iceShard == null)
            return;

        if (marble == null)
        {
            CacheMarble();

            if (marble == null)
                return;
        }

        if (marbleSphere == null)
        {
            marbleSphere =
                marble.GetComponent<SphereCollider>();

            if (marbleSphere == null)
                return;
        }

        timer += Time.deltaTime;

        const int maxStepsPerFrame = 1;

        int stepsTaken = 0;

        while (
            timer >= detectionInterval &&
            stepsTaken < maxStepsPerFrame
        )
        {
            CheckCollision(detectionInterval);

            timer -= detectionInterval;
            stepsTaken++;
        }

        if (stepsTaken >= maxStepsPerFrame)
        {
            timer = 0f;
        }

        previousPosition = marble.transform.position;
    }

    private void CacheMarble()
    {
        if (marble != null)
            return;

        marble = FindFirstObjectByType<Marble>();

        if (marble != null)
        {
            marbleSphere =
                marble.GetComponent<SphereCollider>();

            previousPosition =
                marble.transform.position;
        }
    }

    private void CheckCollision(float stepDeltaTime)
    {
        if (marbleSphere == null)
            return;

        float scale = Mathf.Max(
            marble.transform.lossyScale.x,
            marble.transform.lossyScale.y,
            marble.transform.lossyScale.z
        );

        float radius =
            (marbleSphere.radius * scale) +
            detectionDistance;

        Vector3 currentPosition =
            marble.transform.position;

        Vector3 movement =
            currentPosition - previousPosition;

        bool detected = false;

        /*
         * 1. Check the marble's current position.
         */
        if (CheckSphereNonAlloc(
            currentPosition,
            radius
        ))
        {
            detected = true;
        }
        /*
         * 2. Sweep between the previous and current
         *    marble positions.
         */
        else if (movement.sqrMagnitude > 0.000001f)
        {
            float distance = movement.magnitude;
            Vector3 direction = movement / distance;

            int hitCount =
                Physics.SphereCastNonAlloc(
                    previousPosition,
                    radius,
                    direction,
                    sweepBuffer,
                    distance,
                    targetLayers,
                    QueryTriggerInteraction.Ignore
                );

            for (int i = 0; i < hitCount; i++)
            {
                if (IsIceShardCollider(
                    sweepBuffer[i].collider
                ))
                {
                    detected = true;
                    break;
                }
            }
        }

        /*
         * State machine.
         */
        if (detected)
        {
            if (!isInside)
            {
                isInside = true;
                CustomCollisionEnter(marble);
            }
            else
            {
                CustomCollisionStay(
                    marble,
                    stepDeltaTime
                );
            }
        }
        else if (isInside)
        {
            isInside = false;
            CustomCollisionExit(marble);
        }
    }

    private bool CheckSphereNonAlloc(
        Vector3 position,
        float radius
    )
    {
        int count =
            Physics.OverlapSphereNonAlloc(
                position,
                radius,
                hitBuffer,
                targetLayers,
                QueryTriggerInteraction.Ignore
            );

        for (int i = 0; i < count; i++)
        {
            if (IsIceShardCollider(hitBuffer[i]))
                return true;
        }

        return false;
    }

    private bool IsIceShardCollider(Collider col)
    {
        return
            col != null &&
            col.enabled &&
            !col.isTrigger &&
            iceShardColliderSet.Contains(col);
    }

    private void CustomCollisionEnter(Marble target)
    {
        if (iceShard == null)
            return;

        /*
         * IceShard's existing collision handler expects
         * a Collider rather than a Marble.
         */
        Collider marbleCollider = marbleSphere;

        if (marbleCollider != null)
            iceShard.HandleCollision(marbleCollider);
    }

    private void CustomCollisionStay(
        Marble target,
        float stepDeltaTime
    )
    {
        if (iceShard == null)
            return;

        Collider marbleCollider = marbleSphere;

        if (marbleCollider != null)
            iceShard.HandleCollision(marbleCollider);
    }

    private void CustomCollisionExit(Marble target)
    {
        // IceShard currently has no custom exit logic.
    }

    public void ResetCollision()
    {
        isInside = false;
        timer = 0f;

        if (marble != null)
            previousPosition = marble.transform.position;
    }

    private void OnDisable()
    {
        ResetCollision();
    }
}