using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class JukeboxManager : MonoBehaviour
{
    public static JukeboxManager instance;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
        OnScrollbarValueChanged(scrollbar.value);

        scrollUpButton.onClick.AddListener(ScrollUp);
        scrollDownButton.onClick.AddListener(ScrollDown);
        nextButton.onClick.AddListener(NextSong);
        prevButton.onClick.AddListener(PrevSong);
        stopButton.onClick.AddListener(Stop);
        playButton.onClick.AddListener(Play);

        audioSource.volume = PlayerPrefs.GetFloat("Audio_MusicVolume", 0.5f);

        jukeboxWindowOpen = false;
        isPlaying = true;

        StartCoroutine(LoadSongs());
    }

    public List<AudioClip> musics = new List<AudioClip>();

    public AudioSource audioSource;
    public GameObject jukeboxWindow;
    public Button prevButton;
    public Button nextButton;
    public Button playButton;
    public Button stopButton;
    public TextMeshProUGUI musicInfo;

    [Space]
    public Transform content;
    public Button buttonInstance;

    [Space]
    public GameObject raycastBlocker;

    [SerializeField]
    private Scrollbar scrollbar;
    public ScrollRect scrollRect;

    [SerializeField]
    private Button scrollUpButton;

    [SerializeField]
    private Button scrollDownButton;

    [SerializeField]
    private float step = 0.1f;

    private Button highlightedButton;
    private bool isPlaying;
    private bool jukeboxWindowOpen;
    private string currentlyPlayingMusic;
    private int selectedIndex;
    private AudioClip selectedAudioClip;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            jukeboxWindowOpen = !jukeboxWindowOpen;
            jukeboxWindow.SetActive(jukeboxWindowOpen);
            raycastBlocker.SetActive(jukeboxWindowOpen);

            Cursor.lockState = jukeboxWindowOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = jukeboxWindowOpen;
        }

        if (Input.GetKeyDown(KeyCode.F6))
            PrevSong();

        if (Input.GetKeyDown(KeyCode.F7))
            TogglePlayStop();

        if (Input.GetKeyDown(KeyCode.F8))
            NextSong();
    }

    IEnumerator LoadSongs()
    {
        foreach (AudioClip clip in musics)
            InstantiateButton(clip);

        string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "CustomMusics");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string[] files = Directory.GetFiles(folder, "*.ogg", SearchOption.TopDirectoryOnly);

        System.Array.Sort(files);

        foreach (string file in files)
        {
            string url = "file://" + file.Replace("\\", "/");

            using (
                UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
                    url,
                    AudioType.OGGVORBIS
                )
            )
            {
                yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isHttpError || request.isNetworkError)
#endif
                {
                    //Debug.LogError($"Failed to load {file}\n{request.error}");
                    continue;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                clip.name = Path.GetFileNameWithoutExtension(file);

                // Override official music if it already exists
                int index = musics.FindIndex(m => m != null && m.name == clip.name);

                if (index >= 0)
                {
                    musics[index] = clip;
                    //Debug.Log($"Replaced official music: {clip.name}");
                }
                else
                {
                    musics.Add(clip);
                    //Debug.Log($"Added custom music: {clip.name}");
                }

                InstantiateButton(clip);
            }
        }

        var children = content
            .Cast<Transform>()
            .OrderBy(t => t.Find("Text").GetComponent<TextMeshProUGUI>().text)
            .ToList();

        for (int i = 0; i < children.Count; i++)
        {
            children[i].SetSiblingIndex(i);
        }

        musics = musics.Where(m => m != null).OrderBy(m => m.name).ToList();

        //Debug.Log($"Loaded {musics.Count} songs.");
    }

    public void ScrollUp()
    {
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition + step
        );
    }

    public void ScrollDown()
    {
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition - step
        );
    }

    private void OnScrollbarValueChanged(float value)
    {
        scrollUpButton.interactable = value < 1f;
        scrollDownButton.interactable = value > 0f;
    }

    void InstantiateButton(AudioClip _ac)
    {
        var mission = Instantiate(buttonInstance, content);
        mission.gameObject.SetActive(true);

        var text = mission.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        text.text = _ac.name;

        var btn = mission.GetComponent<Button>();

        // Click = run logic + clear highlight
        btn.onClick.AddListener(() =>
        {
            ClearHighlight();
            selectedAudioClip = _ac;
        });

        // Hover = move highlight
        var trigger = mission.gameObject.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener(_ => HighlightButton(btn));
        trigger.triggers.Add(entry);
    }

    void HighlightButton(Button button)
    {
        if (highlightedButton == button)
            return;

        ClearHighlight();

        highlightedButton = button;

        ColorBlock colors = button.colors;
        button.targetGraphic.color = colors.selectedColor;

        button.transform.Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(
            0.9804f,
            0.7843f,
            0.5137f,
            1f
        );
    }

    void ExecuteHighlighted()
    {
        if (!highlightedButton)
            return;

        highlightedButton.onClick.Invoke();
        HighlightButton(highlightedButton);
    }

    void ClearHighlight()
    {
        if (!highlightedButton)
            return;

        ColorBlock colors = highlightedButton.colors;
        highlightedButton.targetGraphic.color = colors.normalColor;

        highlightedButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().color =
            Color.black;

        highlightedButton = null;
    }

    public void TogglePlayStop()
    {
        if (isPlaying && audioSource.isPlaying)
            Stop();
        else
            Play();
    }

    public void Stop()
    {
        if (selectedAudioClip == null)
            return;

        isPlaying = false;
        audioSource.Stop();

        musicInfo.text = $"Title: {selectedAudioClip.name}\nStopped";
    }

    public void Play()
    {
        if (selectedAudioClip == null)
            return;

        isPlaying = true;

        // Play the selected song from the beginning.
        PlayMusic(selectedAudioClip.name, true);
    }

    public void NextSong()
    {
        if (musics.Count == 0)
            return;

        selectedIndex++;

        if (selectedIndex >= musics.Count)
            selectedIndex = 0;

        PlayMusic(musics[selectedIndex].name, true);
    }

    public void PrevSong()
    {
        if (musics.Count == 0)
            return;

        selectedIndex--;

        if (selectedIndex < 0)
            selectedIndex = musics.Count - 1;

        PlayMusic(musics[selectedIndex].name, true);
    }

    public void ForceStop()
    {
        Stop();

        audioSource.clip = null;
        currentlyPlayingMusic = null;
        selectedAudioClip = null;
        selectedIndex = 0;

        ClearHighlight();

        musicInfo.text = "No Music";
    }

    public void PlayMusic(string name, bool forceRestart = false)
    {
        name = Path.GetFileNameWithoutExtension(name);

        AudioClip selectedMusic = musics.FirstOrDefault(c =>
            c != null && c.name.Equals(name, System.StringComparison.Ordinal)
        );

        if (selectedMusic == null)
        {
            selectedMusic = musics.FirstOrDefault(c =>
                c != null && c.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)
            );

            if (selectedMusic == null)
                return;
        }

        // Same song is already playing.
        // Don't restart unless explicitly requested.
        if (currentlyPlayingMusic == selectedMusic.name && audioSource.isPlaying && !forceRestart)
        {
            return;
        }

        currentlyPlayingMusic = selectedMusic.name;
        selectedAudioClip = selectedMusic;
        selectedIndex = GetClipIndexByName(selectedMusic.name);

        audioSource.Stop();
        audioSource.clip = selectedMusic;
        audioSource.Play();

        isPlaying = true;

        for (int i = 0; i < content.childCount; i++)
        {
            var text = content.GetChild(i).Find("Text").GetComponent<TextMeshProUGUI>();

            if (text.text.Equals(selectedMusic.name))
            {
                HighlightButton(content.GetChild(i).GetComponent<Button>());
                break;
            }
        }

        musicInfo.text = $"Title: {selectedMusic.name}\nPlaying";
    }

    public void PlayRandomMusic()
    {
        if (musics.Count == 0)
            return;

        PlayMusic(musics[Random.Range(0, musics.Count)].name);
    }

    int GetClipIndexByName(string clipName)
    {
        return musics.FindIndex(c => c != null && c.name.Equals(clipName));
    }
}
