using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ErrorSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip lbError;

    public void PlayErrorSound()
    {
        audioSource.volume = PlayerPrefs.GetFloat("Audio_SoundVolume", 0.5f);
        audioSource.PlayOneShot(lbError);
    }
}
