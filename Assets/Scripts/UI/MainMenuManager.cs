using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public Button playButton;
    public Button helpButton;
    public Button optionsButton;
    public Button quitButton;
    public Button websiteButton;
    public Button replayButton;
    public Button leaderboardButton;

    bool isQuitting;

    public void Start()
    {
        JukeboxManager.instance.PlayMusic("Pianoforte");

        playButton.onClick.AddListener(() => SceneManager.LoadScene("PlayMission"));
        helpButton.onClick.AddListener(() => SceneManager.LoadScene("HelpCredits"));
        optionsButton.onClick.AddListener(() => SceneManager.LoadScene("Options"));
        replayButton.onClick.AddListener(() => SceneManager.LoadScene("ReplayMenu"));
        leaderboardButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("LBAuth");
        });
        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
        websiteButton.onClick.AddListener(() => Application.OpenURL("https://marbleblast.com/"));
    }
}
