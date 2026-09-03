using System.Collections.Generic;
using UnityEngine;

public class PathNode
{
    public string nodeName;

    public Vector3 localPosition;
    public Quaternion localRotation;
    public Vector3 localScale;

    public string parentName;

    public string nextNode;
    public List<string> branchNodes = new List<string>();

    public float delay;
    public float timeToNext;
    public float speed;

    public bool isBezier;
    public bool isSpline;

    public string bezierHandle1;
    public string bezierHandle2;

    public bool smooth;
    public bool smoothStart;
    public bool smoothEnd;

    public bool usePosition = true;
    public bool useRotation = true;
    public bool useScale = true;

    public bool reverseRotation;
    public float rotationMultiplier = 1f;

    public Matrix4x4? rotationOffset;

    public Vector3 torqueRotationAxis;
    public float torqueRotationAngle;

    public bool IsBranching()
    {
        return branchNodes != null && branchNodes.Count > 0;
    }
}
