using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NewReplayManager : MonoBehaviour
{
    public Button cancelButton;
    public Button applyButton;
    public TMP_InputField inputField;

    [Space]
    public Button errorOkayButton;
    public GameObject errorWindow;
    public TextMeshProUGUI errorTitle;
    public TextMeshProUGUI errorDesc;

    public void Start()
    {
        inputField.text = string.Empty;
        UpdateName(string.Empty);
        inputField.ForceLabelUpdate();

        ReplayRecorder.recordReplay = false;

        cancelButton.onClick.AddListener(() =>
        {
            cancelButton.GetComponent<ButtonSound>().PlayClickSound();
            Cancel();
            GetComponent<PlayMissionManager>().ToggleReplayWindow(false);
            GetComponent<PlayMissionManager>().raycastBlocker.SetActive(false);
        });

        applyButton.onClick.AddListener(() =>
        {
            applyButton.GetComponent<ButtonSound>().PlayClickSound();
            Apply();
        });

        errorOkayButton.onClick.AddListener(() =>
        {
            errorOkayButton.GetComponent<ButtonSound>().PlayClickSound();
            CloseError();
            applyButton.enabled = true;
            cancelButton.enabled = true;
        });

        inputField.onEndEdit.AddListener(UpdateName);
    }

    public void Init()
    {
        inputField.text = ReplayRecorder.replayName;
    }

    public void UpdateName(string s)
    {
        ReplayRecorder.replayName = s;
    }

    public void Cancel()
    {
        UpdateName(string.Empty);
        ReplayRecorder.recordReplay = false;
        GetComponent<PlayMissionManager>().replayButton.SetIsOnWithoutNotify(false);
    }

    public void Apply()
    {
        if (ReplayFileExists(ReplayRecorder.replayName))
        {
            ShowError("Filename Exists", "Please use a different filename");
            return;
        }

        if (string.IsNullOrWhiteSpace(ReplayRecorder.replayName))
        {
            ShowError("Empty Filename", "Please specify a filename.");
            return;
        }

        char[] forbidden = { '/', '\\', '?', '%', '*', ':', '|', '"', '<', '>', '.' };

        foreach (char c in forbidden)
        {
            if (ReplayRecorder.replayName.Contains(c))
            {
                ShowError(
                    "Invalid Filename",
                    "You can't use the following characters for your replay filename:\n\n"
                        + "/ \\ ? % * : | \" < > .\n\n"
                        + "Those are operating system reserved characters."
                );
                return;
            }
        }

        GetComponent<PlayMissionManager>().ToggleReplayWindow(false);

        ReplayRecorder.recordReplay = true;
        GetComponent<PlayMissionManager>().raycastBlocker.SetActive(false);
        GetComponent<PlayMissionManager>().replayButton.SetIsOnWithoutNotify(true);
    }

    public void ShowError(string title, string desc)
    {
        GetComponent<PlayMissionManager>().raycastBlocker2.SetActive(true);

        applyButton.enabled = true;
        cancelButton.enabled = true;

        errorWindow.SetActive(true);
        errorTitle.text = title;
        errorDesc.text = desc;
    }

    public void CloseError()
    {
        errorWindow.SetActive(false);
        GetComponent<PlayMissionManager>().raycastBlocker2.SetActive(false);
    }

    public bool ReplayFileExists(string fileName)
    {
        string replayFolder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Replay");

        string fullPath = Path.Combine(replayFolder, fileName);

        return File.Exists(fullPath + ".urec");
    }
}
