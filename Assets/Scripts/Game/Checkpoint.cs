using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 3, 0);
    public bool hasAddOrSub;
    public Transform spawn;
    public Transform cameraPos;
    public Transform spawnPos;
    public Transform forward;
    public Vector3 checkpointGravityDir;

    public void InitCheckpoint()
    {
        if (!hasAddOrSub)
        {
            var magnitude = offset.magnitude;
            offset = Quaternion.FromToRotation(offset, -checkpointGravityDir) * offset;
            offset = offset.normalized * magnitude;
        }

        spawnPos.position += offset;
    }

    public void CollisionEnter()
    {
        CancelInvoke();
        GameManager.onReachCheckpoint?.Invoke(spawn, checkpointGravityDir);
    }
}
