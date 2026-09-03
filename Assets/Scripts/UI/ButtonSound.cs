using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class ButtonSound : MonoBehaviour
{
    private AudioSource buttonFx;

    private Button button;
    private Toggle toggle;

    public AudioClip hoverFx;
    public AudioClip clickFx;

    [Tooltip("For toggles: play sound only when toggled ON")]
    public bool playToggleOnOnly = false;

    void Awake()
    {
        buttonFx = GetComponent<AudioSource>();

        button = GetComponent<Button>();
        toggle = GetComponent<Toggle>();

        if (button != null)
            button.onClick.AddListener(PlayClickSound);

        if (toggle != null)
            toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClickSound);

        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    // Called from EventTrigger or IPointerEnter
    public void HoverSound()
    {
        if (!IsInteractable() || !IsEnabled())
            return;

        buttonFx.volume = PlayerPrefs.GetFloat("Audio_SoundVolume", 0.5f);

        if (hoverFx != null)
            buttonFx.PlayOneShot(hoverFx);
    }

    // Button click
    public void PlayClickSound()
    {
        if (!IsInteractable() || !IsEnabled())
            return;

        buttonFx.volume = PlayerPrefs.GetFloat("Audio_SoundVolume", 0.5f);

        if (clickFx != null)
            buttonFx.PlayOneShot(clickFx);
    }

    // Toggle changed
    private void OnToggleChanged(bool isOn)
    {
        if (!IsInteractable() || !IsEnabled())
            return;

        if (playToggleOnOnly && !isOn)
            return;

        buttonFx.volume = PlayerPrefs.GetFloat("Audio_SoundVolume", 0.5f);

        if (clickFx != null)
            buttonFx.PlayOneShot(clickFx);
    }

    bool IsInteractable()
    {
        if (button != null)
            return button.interactable;

        if (toggle != null)
            return toggle.interactable;

        return false;
    }

    bool IsEnabled()
    {
        if (button != null)
            return button.enabled;

        if (toggle != null)
            return toggle.enabled;

        return false;
    }
}
