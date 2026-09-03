using UnityEngine;

public class SoundTrigger : MonoBehaviour
{
    [Header("Sound")]
    public AudioClip sound;

    [Header("Options")]
    public bool triggerOnce = false;

    private bool hasBeenInOnce;

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (triggerOnce && hasBeenInOnce)
            return;

        if (sound == null)
            return;

        GameManager.instance.PlayAudioClip(sound);

        hasBeenInOnce = true;
    }

    public void ResetTrigger()
    {
        hasBeenInOnce = false;
    }
}