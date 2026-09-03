using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnTrigger : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 0, 0);
    public bool hasAddOrSub;
    public Transform spawnPos;

    public void InitSpawnTrigger()
    {
        if (!hasAddOrSub)
        {
            var magnitude = offset.magnitude;
            offset = Quaternion.FromToRotation(offset, -transform.forward) * offset;
            offset = offset.normalized * magnitude;
        }

        spawnPos.position += offset;
    }
}
