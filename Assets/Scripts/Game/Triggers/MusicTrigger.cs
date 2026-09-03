using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    [Header("Music")]
    [Tooltip("Name of the AudioClip to play.")]
    public string musicName;

    [Tooltip("Restart the song if it is already playing.")]
    public bool forceRestart;

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (JukeboxManager.instance == null)
            return;

        if (string.IsNullOrWhiteSpace(musicName))
            return;

        JukeboxManager.instance.PlayMusic(musicName, forceRestart);
    }
}