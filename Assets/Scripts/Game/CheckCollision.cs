using System.Collections.Generic;
using UnityEngine;

public class CheckCollision : MonoBehaviour
{
    [Header("Collision Info")]
    public bool isColliding = false;
    public Vector3 normal = Vector3.up;
    public Vector3 point = Vector3.zero;
    public Collider other;

    // Hunt/PQ respawn support.
    // Stores the most recent valid solid-contact position so Hunt can
    // choose the SpawnTrigger closest to where the marble last touched
    // the level, rather than where it is after going out of bounds.
    public Vector3 LastContactPosition { get; private set; }
    public bool HasLastContactPosition { get; private set; }

    [System.Serializable]
    private class CollisionRecord
    {
        public Collider collider;
        public float time;
        public RaycastHit hit;
    }

    private readonly List<CollisionRecord> collisions = new List<CollisionRecord>();

    private Vector3 previousPosition;
    private SphereCollider sphereCollider;
    private Movement movement;

    private static readonly Vector3[] probeDirections =
    {
        Vector3.down,
        Vector3.up,
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right,
        (Vector3.down + Vector3.forward).normalized,
        (Vector3.down + Vector3.back).normalized,
        (Vector3.down + Vector3.left).normalized,
        (Vector3.down + Vector3.right).normalized,
    };

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        movement = GetComponent<Movement>();
        previousPosition = transform.position;
        LastContactPosition = transform.position;
        HasLastContactPosition = false;
    }

    private void FixedUpdate()
    {
        Vector3 movementVector = transform.position - previousPosition;

        // Manual raycasting only targets solid surfaces (ignores triggers completely)
        ManualCollisionCheck(previousPosition, movementVector);

        previousPosition = transform.position;

        // Expire old solid collision states
        for (int i = collisions.Count - 1; i >= 0; i--)
        {
            if (Time.time - collisions[i].time > 0.05f)
            {
                OnManualCollisionExit(collisions[i].collider);
                collisions.RemoveAt(i);
            }
        }

        isColliding = collisions.Count > 0;
        if (!isColliding) other = null;
    }

    // ============================================================
    // NATIVE UNITY TRIGGER EVENTS
    // ============================================================

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.TryGetComponent<Powerups>(out var p)) p.PickupItem();
        if (collider.TryGetComponent<Gem>(out var g)) g.PickupItem();
        if (collider.TryGetComponent<HelpTrigger>(out var ht) && !ht.transform.GetComponentInParent<HelpBubble>()) ht.TriggerEnter();
        if (collider.TryGetComponent<CannonBase>(out var cannonBase)) cannonBase.EnterTrigger(gameObject.GetComponent<Collider>());

        if (collider.CompareTag("OutOfBounds"))
            GameManager.onOutOfBounds?.Invoke();

        if (collider.CompareTag("Finish"))
        {
            foreach (IGameMode mode in GameManager.instance.GameModes)
            {
                if (mode is LapsMode)
                    return;
            }

            GameManager.onFinish?.Invoke();
        }
    }

    private void OnTriggerStay(Collider collider)
    {
        if (collider.TryGetComponent<Powerups>(out var p)) p.PickupItem();
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.CompareTag("InBounds"))
            GameManager.onOutOfBounds?.Invoke();
    }

    // ============================================================
    // SOLID SURFACE COLLISION PROBING
    // ============================================================

    private void ManualCollisionCheck(Vector3 startPos, Vector3 movementVector)
    {
        float radius = (sphereCollider.radius * Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.y,
            transform.lossyScale.z
        )) + 0.1f;

        // Ground Check
        Vector3 origin = transform.position - Vector3.up * radius;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && groundHit.collider != sphereCollider)
        {
            RegisterCollision(groundHit.collider, groundHit);
        }

        // Side Probes
        DetectMaterialsMultiRay();

        // Movement Sweep
        if (movementVector.sqrMagnitude > Mathf.Epsilon
            && Physics.SphereCast(startPos, radius, movementVector.normalized, out RaycastHit moveHit, movementVector.magnitude + 0.01f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && moveHit.collider != sphereCollider)
        {
            RegisterCollision(moveHit.collider, moveHit);
        }
    }

    private void DetectMaterialsMultiRay()
    {
        float probeDistance = sphereCollider.radius + 0.1f;

        foreach (var dir in probeDirections)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, probeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.collider != sphereCollider)
            {
                RegisterCollision(hit.collider, hit);
                return;
            }
        }

        ClearCollisionState();
    }

    private void RegisterCollision(Collider col, RaycastHit hit)
    {
        CollisionRecord record = collisions.Find(c => c.collider == col);

        if (record != null)
        {
            record.time = Time.time;
            record.hit = hit;
            OnManualCollisionStay(col, hit);
        }
        else
        {
            collisions.Add(new CollisionRecord
            {
                collider = col,
                time = Time.time,
                hit = hit
            });

            OnManualCollisionEnter(col, hit);
        }
    }

    private void OnManualCollisionEnter(Collider collider, RaycastHit hit)
    {
        ApplyCollision(hit);

        if (collider.TryGetComponent<Checkpoint>(out var checkpoint) && movement != null) checkpoint.CollisionEnter();
    }

    private void OnManualCollisionStay(Collider collider, RaycastHit hit)
    {
        ApplyCollision(hit);

        if (hit.collider.TryGetComponent<Trapdoor>(out var t)) t.OnCollisionWithMarble();
    }

    private void OnManualCollisionExit(Collider collider)
    {
        if (collisions.Count == 0) ClearCollisionState();
    }

    // ============================================================
    // UTILITY METHODS
    // ============================================================

    private void ClearCollisionState()
    {
        isColliding = false;
        other = null;
    }

    public Vector3 Rounding(Vector3 vector)
    {
        float x = Mathf.Abs(vector.x) < 0.1f ? 0 : vector.x;
        float y = Mathf.Abs(vector.y) < 0.1f ? 0 : vector.y;
        float z = Mathf.Abs(vector.z) < 0.1f ? 0 : vector.z;

        return new Vector3(x, y, z).normalized;
    }

    private void ApplyCollision(RaycastHit hit)
    {
        if (hit.collider == null) return;

        normal = Rounding(hit.normal);
        point = hit.point;
        other = hit.collider;
        isColliding = true;

        // Record the marble's position at the last valid solid contact.
        // This is the Unity equivalent used by Hunt respawn selection.
        LastContactPosition = transform.position;
        HasLastContactPosition = true;
    }
}
