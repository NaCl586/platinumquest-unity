using UnityEngine;

public class TimeTravelTrigger : MonoBehaviour
{
    [Header("Time Travel")]
    public float timeBonus = 5f;

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        marble.ActivateTimeTravel(timeBonus);

        if (timeBonus >= 0f)
        {
            GameUIManager.instance.DisplayTimeTravelMessage(timeBonus);
        }
        else
        {
            GameUIManager.instance.DisplayTimePenaltyMessage(timeBonus);
        }
    }
}