using System.Collections.Generic;
using UnityEngine;

public class PathTrigger : MonoBehaviour
{
    [System.Serializable]
    public class PathEntry
    {
        [Tooltip("Object that will be moved.")]
        public GameObject target;

        [Tooltip("PathManager containing the path nodes.")]
        public PathManager pathManager;

        [Tooltip("First node of the path.")]
        public string initialNode;
    }

    [Header("Trigger Settings")]
    [SerializeField]
    private bool triggerOnce = true;

    public bool TriggerOnce
    {
        get => triggerOnce;
        set => triggerOnce = value;
    }

    [Header("Objects")]
    [SerializeField]
    private List<PathEntry> entries = new List<PathEntry>();

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Marble"))
            return;

        OnEnterTrigger();
    }

    /// <summary>
    /// Called by the Marble trigger system or PushButton.
    /// </summary>
    public void OnEnterTrigger()
    {
        if (triggerOnce && hasTriggered)
            return;

        hasTriggered = true;

        TriggerObjects();
    }

    private void TriggerObjects()
    {
        foreach (PathEntry entry in entries)
        {
            if (entry == null)
                continue;

            if (entry.target == null)
            {
                Debug.LogWarning($"PathTrigger '{name}': " + "Entry has no target.", this);

                continue;
            }

            if (entry.pathManager == null)
            {
                Debug.LogWarning(
                    $"PathTrigger '{name}': "
                        + $"No PathManager assigned for "
                        + $"'{entry.target.name}'.",
                    this
                );

                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.initialNode))
            {
                Debug.LogWarning(
                    $"PathTrigger '{name}': "
                        + $"No initial node assigned for "
                        + $"'{entry.target.name}'.",
                    this
                );

                continue;
            }

            if (!entry.pathManager.TryGetNode(entry.initialNode, out PathNode node))
            {
                Debug.LogWarning(
                    $"PathTrigger '{name}': "
                        + $"Could not find node "
                        + $"'{entry.initialNode}' for "
                        + $"'{entry.target.name}'.",
                    this
                );

                continue;
            }

            PathMover mover = entry.target.GetComponent<PathMover>();

            if (mover == null)
            {
                Debug.LogWarning(
                    $"PathTrigger '{name}': "
                        + $"'{entry.target.name}' "
                        + "does not have a PathMover.",
                    entry.target
                );

                continue;
            }

            // IMPORTANT:
            //
            // false = this path is controlled by a trigger.
            //
            // Therefore, when the level resets,
            // PathMover.ResetMover() will call
            // DeactivatePath() instead of ResetPath().
            mover.InitializePath(node.nodeName, entry.pathManager, false);
        }
    }

    public void AddEntry(GameObject target, PathManager pathManager, string initialNode)
    {
        if (target == null)
        {
            Debug.LogWarning(
                $"PathTrigger '{name}': " + "Cannot add entry with null target.",
                this
            );

            return;
        }

        if (pathManager == null)
        {
            Debug.LogWarning(
                $"PathTrigger '{name}': " + "Cannot add entry with null PathManager.",
                this
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(initialNode))
        {
            Debug.LogWarning($"PathTrigger '{name}': " + "Cannot add entry with empty path.", this);

            return;
        }

        entries.Add(
            new PathEntry
            {
                target = target,
                pathManager = pathManager,
                initialNode = initialNode.Trim(),
            }
        );
    }

    /// <summary>
    /// Resets this trigger so it can be activated again
    /// after a level restart.
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
