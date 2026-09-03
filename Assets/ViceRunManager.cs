using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ViceRunManager : MonoBehaviour
{
    public TMP_InputField inputField;
    public Button save;
    public Button discard;

    private string SaveDirectory
    {
        get
        {
            DirectoryInfo parent =
                Directory.GetParent(Application.dataPath);

            if (parent == null)
            {
                return Path.Combine(
                    Application.dataPath,
                    "Replays",
                    "State"
                );
            }

            return Path.Combine(
                parent.FullName,
                "Replays",
                "State"
            );
        }
    }

    private void Start()
    {
        inputField.onValueChanged.AddListener(
            OnInputChanged
        );

        save.onClick.AddListener(
            SaveRun
        );

        discard.onClick.AddListener(
            Discard
        );

        UpdateSaveButton();
    }

    private void OnInputChanged(string value)
    {
        UpdateSaveButton();
    }

    private void UpdateSaveButton()
    {
        if (save == null || inputField == null)
            return;

        string runName =
            inputField.text.Trim();

        if (string.IsNullOrEmpty(runName))
        {
            save.interactable = false;
            return;
        }

        if (ContainsInvalidFileNameCharacters(runName))
        {
            save.interactable = false;
            return;
        }

        string fileName =
            "vv" + runName + ".txt";

        string filePath =
            Path.Combine(
                SaveDirectory,
                fileName
            );

        // Don't allow duplicate run names.
        if (File.Exists(filePath))
        {
            save.interactable = false;
            return;
        }

        save.interactable = true;
    }

    private void SaveRun()
    {
        string runName = inputField.text.Trim();

        if (string.IsNullOrEmpty(runName))
            return;

        if (ContainsInvalidFileNameCharacters(runName))
            return;

        string fileName = "vv" + runName + ".txt";

        if (File.Exists(Path.Combine(SaveDirectory, fileName)))
        {
            Debug.LogWarning(
                $"[Vice] A run named '{runName}' already exists."
            );

            return;
        }

        try
        {
            Directory.CreateDirectory(SaveDirectory);

            GameManager gameManager =
                FindObjectOfType<GameManager>();

            if (gameManager == null)
            {
                Debug.LogError(
                    "[Vice] Could not find GameManager."
                );

                return;
            }

            ViceVersaState.fileName = fileName;

            ViceVersaState.Save(gameManager);

            Debug.Log(
                $"[Vice] Saved named run: {fileName}"
            );

            HideWindow();
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[Vice] Failed to save named run.\n{e}"
            );
        }
    }

    private void Discard()
    {
        HideWindow();
    }

    private void HideWindow()
    {
        GameUIManager.instance.viceSaveWindow.SetActive(false);
    }

    private bool ContainsInvalidFileNameCharacters(
        string value
    )
    {
        char[] invalidCharacters =
            Path.GetInvalidFileNameChars();

        foreach (char character in value)
        {
            foreach (char invalid in invalidCharacters)
            {
                if (character == invalid)
                    return true;
            }
        }

        return false;
    }

    private void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(
                OnInputChanged
            );
        }

        if (save != null)
        {
            save.onClick.RemoveListener(
                SaveRun
            );
        }

        if (discard != null)
        {
            discard.onClick.RemoveListener(
                Discard
            );
        }
    }


}