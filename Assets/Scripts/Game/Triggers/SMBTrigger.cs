using PlatinumQuestScripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SMBTrigger : MonoBehaviour
{
    [Header("White Noise")]
    public float impulse;
    public float upwards;

    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (GameManager.instance == null)
            return;

        if (GameManager.instance.specialGameMode
            is WhiteNoiseMode whiteNoise)
        {
            whiteNoise.SmbTriggerEnter(
                impulse,
                upwards,
                GetCurrentAttemptTime()
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (GameManager.instance == null)
            return;

        if (GameManager.instance.specialGameMode
            is WhiteNoiseMode whiteNoise)
        {
            whiteNoise.SmbTriggerLeave(
                GetCurrentAttemptTime()
            );
        }
    }

    private float GetCurrentAttemptTime()
    {
        if (GameManager.instance == null)
            return 0f;

        return GameManager.instance.elapsedTime;
    }
}