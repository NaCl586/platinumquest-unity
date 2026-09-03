using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    private readonly Dictionary<string, PathNode> nodes =
        new Dictionary<string, PathNode>();

    public IReadOnlyDictionary<string, PathNode> Nodes => nodes;

    public void RegisterNode(PathNode node)
    {
        if (node == null)
            return;

        if (string.IsNullOrEmpty(node.nodeName))
            return;

        string key = node.nodeName.Trim().ToLowerInvariant();

        nodes[key] = node;
    }

    public bool TryGetNode(string nodeName, out PathNode node)
    {
        node = null;

        if (string.IsNullOrWhiteSpace(nodeName))
            return false;

        string key = nodeName.Trim().ToLowerInvariant();

        return nodes.TryGetValue(key, out node);
    }

    public bool ContainsNode(string nodeName)
    {
        return !string.IsNullOrWhiteSpace(nodeName)
            && nodes.ContainsKey(nodeName.Trim().ToLowerInvariant());
    }

    public void Clear()
    {
        nodes.Clear();
    }

    public void LogNodes()
    {
        Debug.Log($"[PathManager] Registered {nodes.Count} PathNodes:");

        foreach (KeyValuePair<string, PathNode> pair in nodes)
        {
            PathNode node = pair.Value;

            Debug.Log(
                $"[PathManager] {pair.Key} " +
                $"next='{node.nextNode}' " +
                $"branches={node.branchNodes.Count}"
            );
        }
    }
}