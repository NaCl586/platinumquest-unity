using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PhysModTrigger : MonoBehaviour
{
    private static readonly List<PhysModTrigger> instances = new List<PhysModTrigger>();

    [Header("PhysMod")]
    [SerializeField]
    private List<PhysicsAttributeOverride> overrides = new List<PhysicsAttributeOverride>();

    [SerializeField]
    private bool disabled;

    [Header("Visuals")]
    [SerializeField]
    private bool noEmitters;

    private BoxCollider triggerCollider;

    // Marble -> physics layer that this trigger pushed onto it.
    private readonly Dictionary<Marble, List<PhysicsAttributeOverride>> activeLayers =
        new Dictionary<Marble, List<PhysicsAttributeOverride>>();

    public List<PhysicsAttributeOverride> Overrides => overrides;

    public bool Disabled
    {
        get => disabled;
        set
        {
            if (disabled == value)
                return;

            disabled = value;

            if (disabled)
                ClearActiveLayers();
        }
    }

    public bool NoEmitters
    {
        get => noEmitters;
        set => noEmitters = value;
    }

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        if (!instances.Contains(this))
            instances.Add(this);
    }

    private void OnDisable()
    {
        instances.Remove(this);
        ClearActiveLayers();
    }

    private void OnDestroy()
    {
        instances.Remove(this);
        ClearActiveLayers();
    }

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        OnMarbleEnter(marble);
    }

    private void OnTriggerExit(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        OnMarbleLeave(marble);
    }

    private void OnMarbleEnter(Marble marble)
    {
        if (marble == null)
            return;

        if (disabled)
            return;

        // Prevent the same trigger from pushing its layer twice.
        if (activeLayers.ContainsKey(marble))
            return;

        if (overrides == null || overrides.Count == 0)
            return;

        List<PhysicsAttributeOverride> layer = marble.PushPhysicsLayer(overrides);

        if (layer != null)
            activeLayers.Add(marble, layer);
    }

    private void OnMarbleLeave(Marble marble)
    {
        if (marble == null)
            return;

        if (!activeLayers.TryGetValue(marble, out var layer))
            return;

        marble.PopPhysicsLayer(layer);
        activeLayers.Remove(marble);
    }

    // ---------------------------------------------------------------------
    // Respawn / occupancy handling
    // ---------------------------------------------------------------------

    /// <summary>
    /// Re-checks every PhysMod trigger against the marble.
    ///
    /// This is important after respawning because Unity may not generate
    /// another OnTriggerEnter if the marble is already inside the trigger
    /// when it is moved to its checkpoint position.
    /// </summary>
    public static void RefreshAllTriggers(Marble marble)
    {
        if (marble == null)
            return;

        // Make a copy so the list cannot be modified while iterating.
        PhysModTrigger[] triggers = instances.ToArray();

        foreach (PhysModTrigger trigger in triggers)
        {
            if (trigger == null || !trigger.isActiveAndEnabled)
                continue;

            trigger.RefreshMarble(marble);
        }
    }

    private void RefreshMarble(Marble marble)
    {
        if (marble == null)
            return;

        bool inside = IsMarbleInside(marble);
        bool alreadyActive = activeLayers.ContainsKey(marble);

        if (inside && !alreadyActive)
        {
            OnMarbleEnter(marble);
        }
        else if (!inside && alreadyActive)
        {
            OnMarbleLeave(marble);
        }
    }

    /// <summary>
    /// Checks whether the marble's center is inside this trigger.
    /// </summary>
    private bool IsMarbleInside(Marble marble)
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider>();

        if (triggerCollider == null)
            return false;

        float radius = 0f;

        if (marble.movement != null)
            radius = marble.movement.marbleRadius;

        Vector3 closestPoint = triggerCollider.ClosestPoint(marble.transform.position);

        float distance = Vector3.Distance(closestPoint, marble.transform.position);

        return distance <= radius;
    }

    /// <summary>
    /// Removes this trigger's physics layer from every marble currently
    /// tracked by it.
    /// </summary>
    private void ClearActiveLayers()
    {
        if (activeLayers.Count == 0)
            return;

        foreach (var pair in activeLayers)
        {
            Marble marble = pair.Key;
            List<PhysicsAttributeOverride> layer = pair.Value;

            if (marble != null && layer != null)
                marble.PopPhysicsLayer(layer);
        }

        activeLayers.Clear();
    }

    public static void ClearAllMarbleLayers(Marble marble)
    {
        if (marble == null)
            return;

        PhysModTrigger[] triggers = instances.ToArray();

        foreach (PhysModTrigger trigger in triggers)
        {
            if (trigger == null)
                continue;

            trigger.RemoveMarble(marble);
        }
    }

    public static void ForgetAllMarbleLayers(Marble marble)
    {
        if (marble == null)
            return;

        PhysModTrigger[] triggers = instances.ToArray();

        foreach (PhysModTrigger trigger in triggers)
        {
            if (trigger == null)
                continue;

            trigger.activeLayers.Remove(marble);
        }
    }

    private void RemoveMarble(Marble marble)
    {
        if (marble == null)
            return;

        if (!activeLayers.TryGetValue(marble, out var layer))
            return;

        marble.PopPhysicsLayer(layer);
        activeLayers.Remove(marble);
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();

        if (box == null)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(0.25f, 0.5f, 1f, 0.2f);

        Gizmos.DrawCube(box.center, box.size);

        Gizmos.color = new Color(0.25f, 0.5f, 1f, 0.8f);

        Gizmos.DrawWireCube(box.center, box.size);
    }

#endif
}
