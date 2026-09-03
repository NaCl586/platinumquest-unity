using System.Collections;
using UnityEngine;

public class CountdownStartTrigger : MonoBehaviour
{
    [Header("Countdown")]
    [Tooltip("Countdown duration in seconds.")]
    public float time = 10f;

    [Header("Activation")]
    [Tooltip("If enabled, this trigger can only activate once until ResetTrigger is called.")]
    public bool activateOnce = false;

    [Header("Delay")]
    [Tooltip("Delay before the countdown starts, in seconds.")]
    public float startDelay = 0f;

    [Header("Icon")]
    public string icon = "timerTimeTravel";

    // ============================================================
    // Runtime State
    // ============================================================

    private bool activated;
    private Coroutine pendingStart;


    // ============================================================
    // Trigger Enter
    // ============================================================

    private void OnTriggerEnter(Collider other)
    {
        Movement movement = other.GetComponent<Movement>();

        if (movement == null)
            return;

        if (activateOnce && activated)
            return;

        activated = true;

        // Cancel an existing delayed start.
        if (pendingStart != null)
        {
            StopCoroutine(pendingStart);
            pendingStart = null;
        }

        // Start immediately.
        if (startDelay <= 0f)
        {
            StartCountdown();
            return;
        }

        // Otherwise wait for the configured delay.
        pendingStart = StartCoroutine(DelayedStart());
    }


    // ============================================================
    // Delayed Start
    // ============================================================

    private IEnumerator DelayedStart()
    {
        float elapsed = 0f;

        while (elapsed < startDelay)
        {
            // Countdown trigger delay follows gameplay time.
            // It does not advance while the game is paused.
            if (!GameManager.isPaused)
                elapsed += Time.deltaTime;

            yield return null;
        }

        pendingStart = null;

        StartCountdown();
    }


    // ============================================================
    // Start Countdown
    // ============================================================

    private void StartCountdown()
    {
        if (GameManager.instance == null)
            return;

        string countdownIcon = string.IsNullOrEmpty(icon)
            ? "timerTimeTravel"
            : icon;

        GameManager.instance.StartCountdown(
            Mathf.Max(0f, time),
            countdownIcon
        );
    }


    // ============================================================
    // Reset
    // ============================================================

    public void ResetTrigger()
    {
        activated = false;

        if (pendingStart != null)
        {
            StopCoroutine(pendingStart);
            pendingStart = null;
        }
    }


    // ============================================================
    // Disable
    // ============================================================

    private void OnDisable()
    {
        if (pendingStart != null)
        {
            StopCoroutine(pendingStart);
            pendingStart = null;
        }
    }
}