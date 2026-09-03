using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public Checkpoint baseCheckpoint;

    private void OnTriggerEnter(Collider other)
    {
        Movement movement = null;
        if (other.gameObject.TryGetComponent<Movement>(out movement))
        {
            TriggerEnter();
        }
    }

    void TriggerEnter()
    {
        CancelInvoke();
        GameManager.onReachCheckpoint?.Invoke(
            baseCheckpoint.spawn,
            baseCheckpoint.checkpointGravityDir
        );
    }
}
