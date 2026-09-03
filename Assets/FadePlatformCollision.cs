using System.Collections.Generic;
using UnityEngine;

public class FadePlatformCollision : MonoBehaviour
{
    public FadePlatform fadePlatform;

    [Header("Detection")]
    [SerializeField]
    private float detectionInterval = 0.001f;

    [SerializeField]
    private float detectionDistsance = 0.1f;

    [SerializeField]
    private LayerMask targetLayers = Physics.DefaultRaycastLayers;

    private Marble marble;
    private SphereCollider marbleSphere;
    private HashSet<Collider> platformColliderSet;

    private Vector3 previousPosition;
    private float timer;

    // Fixed non-allocating hit buffers.
    private readonly Collider[] hitBuffer = new Collider[16];
    private readonly RaycastHit[] sweepBuffer = new RaycastHit[16];

    private bool isInside;

    private void Awake()
    {
        Collider[] platformColliders =
            GetComponentsInChildren<Collider>(true);

        platformColliderSet =
            new HashSet<Collider>(platformColliders);

        CacheMarble();
    }

    private void Update()
    {
        if (fadePlatform == null)
            return;

        /*
         * Periodic platforms disable this component during initialization.
         * Keep this check as a safety guard.
         */
        if (!fadePlatform.RequiresMarbleCollisionDetection)
        {
            enabled = false;
            return;
        }

        if (marble == null)
        {
            CacheMarble();

            if (marble == null)
                return;
        }

        if (marbleSphere == null)
        {
            marbleSphere = marble.GetComponent<SphereCollider>();

            if (marbleSphere == null)
                return;
        }

        timer += Time.deltaTime;

        /*
         * IMPORTANT:
         *
         * detectionInterval remains 0.001 seconds.
         *
         * Every elapsed 0.001-second interval gets its own collision
         * check. We intentionally do NOT collapse multiple intervals
         * into one query because FadePlatform behavior depends on the
         * high-frequency polling.
         *
         * A safety cap prevents an extremely long/stalled frame from
         * producing an unbounded number of physics queries.
         */
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

        /*
         * If the game stalls for a long time, don't carry an enormous
         * backlog into future frames. The actual detection interval
         * remains 0.001 seconds during normal operation.
         */
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
            detectionDistsance;

        Vector3 currentPosition =
            marble.transform.position;

        Vector3 movement =
            currentPosition - previousPosition;

        bool detected = false;

        /*
         * 1. Current position check.
         */
        if (CheckSphereNonAlloc(currentPosition, radius))
        {
            detected = true;
        }
        /*
         * 2. Sweep between previous and current position.
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
                if (IsPlatformCollider(sweepBuffer[i].collider))
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
                CustomCollisionStay(marble, stepDeltaTime);
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
            if (IsPlatformCollider(hitBuffer[i]))
                return true;
        }

        return false;
    }

    private bool IsPlatformCollider(Collider col)
    {
        return
            col != null &&
            col.enabled &&
            !col.isTrigger &&
            platformColliderSet.Contains(col);
    }

    private void CustomCollisionEnter(Marble target)
    {
        if (fadePlatform == null)
            return;

        fadePlatform.OnCollisionWithMarble(target);
    }

    private void CustomCollisionStay(
        Marble target,
        float stepDeltaTime
    )
    {
        if (fadePlatform == null)
            return;

        fadePlatform.OnCollisionWithMarble(target);
    }

    private void CustomCollisionExit(Marble target)
    {
        // Custom exit logic if needed.
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
