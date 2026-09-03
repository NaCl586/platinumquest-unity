using UnityEngine;

public class CountdownStopTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Movement movement = other.GetComponent<Movement>();

        if (movement == null)
            return;

        // Stop the active countdown.
        if (GameManager.instance != null)
            GameManager.instance.StopCountdown();

        // QoL:
        // Re-arm all CountdownStartTriggers so they can
        // start a new countdown if entered again.
        foreach (
            CountdownStartTrigger trigger
            in FindObjectsOfType<CountdownStartTrigger>(true)
        )
        {
            trigger.ResetTrigger();
        }
    }
}