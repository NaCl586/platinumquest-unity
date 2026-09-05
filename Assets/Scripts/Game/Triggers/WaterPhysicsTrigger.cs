using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterPhysicsTrigger : MonoBehaviour
{
    [Header("Water Physics")]
    [Tooltip("Matches the Haxe implementation. 0.5 removes 50% of entry velocity.")]
    public float velocityMultiplier = 0.5f;

    private Collider waterCollider;

    private static readonly Dictionary<Marble, List<WaterPhysicsTrigger>> activeWaterTriggers =
        new Dictionary<Marble, List<WaterPhysicsTrigger>>();

    private readonly Dictionary<Marble, HashSet<Collider>> localOverlaps =
        new Dictionary<Marble, HashSet<Collider>>();

    private void Awake()
    {
        waterCollider = GetComponent<Collider>();
        waterCollider.isTrigger = true;
    }

    private void Start()
    {
        // Physics trigger events may not be generated for objects
        // that were already inside the trigger when the scene started.
        StartCoroutine(InitializeExistingOverlaps());
    }

    private IEnumerator InitializeExistingOverlaps()
    {
        // Wait one physics frame so all objects have been initialized
        // and physics transforms are up to date.
        yield return new WaitForFixedUpdate();

        if (waterCollider == null || !waterCollider.enabled)
            yield break;

        // Use the water collider's bounds to find possible overlaps.
        // This is only a broad-phase query. The actual collider overlap
        // is verified with ComputePenetration below.
        Bounds bounds = waterCollider.bounds;

        Collider[] colliders = Physics.OverlapBox(
            bounds.center,
            bounds.extents,
            Quaternion.identity,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider other in colliders)
        {
            if (other == null)
                continue;

            if (other == waterCollider)
                continue;

            Marble marble = other.GetComponent<Marble>();

            if (marble == null)
                continue;

            // Verify that the actual colliders overlap.
            // bounds.Intersects() only compares axis-aligned bounds
            // and can incorrectly report an overlap.
            if (!CollidersActuallyOverlap(waterCollider, other))
                continue;

            ProcessTriggerEnter(other);
        }
    }

    private static bool CollidersActuallyOverlap(Collider a, Collider b)
    {
        if (a == null || b == null)
            return false;

        if (!a.enabled || !b.enabled)
            return false;

        Vector3 direction;
        float distance;

        return Physics.ComputePenetration(
            a,
            a.transform.position,
            a.transform.rotation,
            b,
            b.transform.position,
            b.transform.rotation,
            out direction,
            out distance
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessTriggerEnter(other);
    }

    private void ProcessTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (!localOverlaps.TryGetValue(
                marble,
                out HashSet<Collider> overlaps))
        {
            overlaps = new HashSet<Collider>();
            localOverlaps.Add(marble, overlaps);
        }

        if (!overlaps.Add(other))
            return;

        if (overlaps.Count > 1)
            return;

        if (!activeWaterTriggers.TryGetValue(
                marble,
                out List<WaterPhysicsTrigger> triggers))
        {
            triggers = new List<WaterPhysicsTrigger>();
            activeWaterTriggers.Add(marble, triggers);
        }

        bool wasAlreadyInWater = triggers.Count > 0;

        if (!triggers.Contains(this))
            triggers.Add(this);

        // Only the first trigger entered is a real water entry.
        // RefreshMarbleWaterState() never calls this method, so teleports
        // into water do not produce a splash.
        if (!wasAlreadyInWater)
            marble.OnWaterTriggerEntered(this);

        marble.OnWaterTriggerChanged();
    }

    private void OnTriggerExit(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (!localOverlaps.TryGetValue(marble, out HashSet<Collider> overlaps))
            return;

        overlaps.Remove(other);

        if (overlaps.Count > 0)
            return;

        localOverlaps.Remove(marble);

        if (!activeWaterTriggers.TryGetValue(
                marble,
                out List<WaterPhysicsTrigger> triggers))
            return;

        triggers.Remove(this);

        // Only play an exit splash when this was the last water trigger.
        // If another water trigger is still active, the marble is still
        // in water and has not actually exited water yet.
        if (triggers.Count == 0)
        {
            // Tell the marble which exact trigger boundary was crossed
            // before removing the final active trigger.
            marble.OnWaterTriggerExited(this);

            activeWaterTriggers.Remove(marble);
        }

        marble.OnWaterTriggerChanged();
    }

    public static bool IsMarbleInWater(Marble marble)
    {
        if (marble == null)
            return false;

        return activeWaterTriggers.TryGetValue(
            marble,
            out List<WaterPhysicsTrigger> triggers)
            && triggers.Count > 0;
    }

    public static WaterPhysicsTrigger GetClosestTrigger(Marble marble)
    {
        if (marble == null)
            return null;

        if (!activeWaterTriggers.TryGetValue(
                marble,
                out List<WaterPhysicsTrigger> triggers))
            return null;

        WaterPhysicsTrigger closest = null;
        float closestDistance = float.MaxValue;

        Vector3 marblePosition = marble.transform.position;

        foreach (WaterPhysicsTrigger trigger in triggers)
        {
            if (trigger == null || trigger.waterCollider == null)
                continue;

            Vector3 center = trigger.waterCollider.bounds.center;
            float distance = (center - marblePosition).sqrMagnitude;

            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closest = trigger;
            }
        }

        return closest;
    }

    // ============================================================
    // Water Surface
    // ============================================================

    public float GetWaterSurfaceHeight()
    {
        if (waterCollider == null)
            return transform.position.y;

        return waterCollider.bounds.max.y;
    }

    public bool IsPointUnderwater(Vector3 position)
    {
        return position.y < GetWaterSurfaceHeight();
    }

    public float GetWaterDepth(Marble marble)
    {
        if (marble == null || waterCollider == null)
            return 0f;

        float waterSurface = GetWaterSurfaceHeight();

        return Mathf.Clamp(
            marble.transform.position.y - waterSurface,
            0f,
            0.2f
        );
    }

    public float GetWaterDepth01(Marble marble)
    {
        return GetWaterDepth(marble) / 0.2f;
    }

    public float GetVelocityMultiplier()
    {
        return velocityMultiplier;
    }

    // ============================================================
    // Camera Underwater Detection
    // ============================================================

    public static bool IsCameraUnderwater(Camera camera)
    {
        if (camera == null)
            return false;

        Vector3 cameraPosition = camera.transform.position;

        foreach (WaterPhysicsTrigger trigger in FindObjectsOfType<WaterPhysicsTrigger>())
        {
            if (trigger == null || trigger.waterCollider == null)
                continue;

            Bounds bounds = trigger.waterCollider.bounds;

            if (!bounds.Contains(cameraPosition))
                continue;

            if (trigger.IsPointUnderwater(cameraPosition))
                return true;
        }

        return false;
    }

    public static WaterPhysicsTrigger GetCameraWaterTrigger(Camera camera)
    {
        if (camera == null)
            return null;

        Vector3 cameraPosition = camera.transform.position;

        foreach (WaterPhysicsTrigger trigger in FindObjectsOfType<WaterPhysicsTrigger>())
        {
            if (trigger == null || trigger.waterCollider == null)
                continue;

            Bounds bounds = trigger.waterCollider.bounds;

            if (!bounds.Contains(cameraPosition))
                continue;

            if (trigger.IsPointUnderwater(cameraPosition))
                return trigger;
        }

        return null;
    }

    private void OnDisable()
    {
        List<Marble> affected = new List<Marble>();

        foreach (var pair in localOverlaps)
            affected.Add(pair.Key);

        foreach (Marble marble in affected)
        {
            if (!activeWaterTriggers.TryGetValue(
                    marble,
                    out List<WaterPhysicsTrigger> triggers))
                continue;

            triggers.Remove(this);

            if (triggers.Count == 0)
                activeWaterTriggers.Remove(marble);

            if (marble != null)
                marble.OnWaterTriggerChanged();
        }

        localOverlaps.Clear();
    }

    public static void RefreshMarbleWaterState(Marble marble)
    {
        if (marble == null)
            return;

        // Remove the marble from all currently tracked water triggers.
        foreach (WaterPhysicsTrigger trigger in FindObjectsOfType<WaterPhysicsTrigger>())
        {
            if (trigger == null)
                continue;

            if (trigger.localOverlaps.TryGetValue(
                    marble,
                    out HashSet<Collider> overlaps))
            {
                overlaps.Clear();
                trigger.localOverlaps.Remove(marble);
            }

            if (activeWaterTriggers.TryGetValue(
                    marble,
                    out List<WaterPhysicsTrigger> triggers))
            {
                triggers.Remove(trigger);
            }
        }

        activeWaterTriggers.Remove(marble);

        // Rebuild the state from the marble's current physical position.
        Collider marbleCollider = marble.GetComponent<Collider>();

        if (marbleCollider == null)
            return;

        foreach (WaterPhysicsTrigger trigger in FindObjectsOfType<WaterPhysicsTrigger>())
        {
            if (trigger == null ||
                trigger.waterCollider == null ||
                !trigger.waterCollider.enabled)
                continue;

            // Use the actual collider geometry rather than AABB bounds.
            if (!CollidersActuallyOverlap(
                    trigger.waterCollider,
                    marbleCollider))
                continue;

            if (!trigger.localOverlaps.TryGetValue(
                    marble,
                    out HashSet<Collider> overlaps))
            {
                overlaps = new HashSet<Collider>();
                trigger.localOverlaps.Add(marble, overlaps);
            }

            overlaps.Add(marbleCollider);

            if (!activeWaterTriggers.TryGetValue(
                    marble,
                    out List<WaterPhysicsTrigger> triggers))
            {
                triggers = new List<WaterPhysicsTrigger>();
                activeWaterTriggers.Add(marble, triggers);
            }

            if (!triggers.Contains(trigger))
                triggers.Add(trigger);
        }

        // Rebuild Marble's water physics state.
        marble.OnWaterTriggerChanged();
    }
}