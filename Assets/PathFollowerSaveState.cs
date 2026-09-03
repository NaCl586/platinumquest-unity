using System;

[Serializable]
public class PathFollowerSaveState
{
    public bool active;
    public float pathPosition;
    public string currentNode;
    public string prevNode;
    public int rngCursor;
}
