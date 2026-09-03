using UnityEngine;

public class DisableShapeForceTrigger : MonoBehaviour
{
    [Header("Targets")]
    public GameObject[] targets;

    [Header("Options")]
    public bool invert;

    private void OnTriggerEnter(Collider other)
    {
        Movement movement = other.GetComponentInParent<Movement>();

        if (movement == null)
            return;

        SetTargetsPowered(invert);
    }

    private void OnTriggerExit(Collider other)
    {
        Movement movement = other.GetComponentInParent<Movement>();

        if (movement == null)
            return;

        SetTargetsPowered(!invert);
    }

    private void SetTargetsPowered(bool powered)
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            GameObject target = targets[i];

            if (target == null)
                continue;

            DuctFan ductFan = target.GetComponent<DuctFan>();
            if (ductFan != null)
            {
                ductFan.SetPowered(powered);
                continue;
            }

            Tornado tornado = target.GetComponent<Tornado>();
            if (tornado != null)
            {
                tornado.SetPowered(powered);
                continue;
            }

            Magnet magnet = target.GetComponent<Magnet>();
            if (magnet != null)
            {
                magnet.SetPowered(powered);
            }
        }
    }
}