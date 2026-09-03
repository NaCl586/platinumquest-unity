using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VersaRunManager : MonoBehaviour
{
    public Button cancelButton;
    public Button playButton;
    public Transform content;
    public Button buttonInstance;

    [Space]
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

    private readonly List<Button> runButtons =
        new List<Button>();

    private string SaveDirectory
    {
        get
        {
            string gameRoot =
                Directory.GetParent(Application.dataPath).FullName;

            return Path.Combine(
                gameRoot,
                "Replays",
                "State"
            );
        }
    }

    public void ScrollUp()
    {
        scrollRect.verticalNormalizedPosition =
            Mathf.Clamp01(
                scrollRect.verticalNormalizedPosition + step
            );
    }

    public void ScrollDown()
    {
        scrollRect.verticalNormalizedPosition =
            Mathf.Clamp01(
                scrollRect.verticalNormalizedPosition - step
            );
    }

    private void HighlightButton(Button button)
    {
        if (highlightedButton == button)
            return;

        ClearHighlight();

        highlightedButton = button;

        var colors = button.colors;

        button.targetGraphic.color =
            colors.selectedColor;
    }

    private void ExecuteHighlighted()
    {
        if (!highlightedButton)
            return;

        highlightedButton.onClick.Invoke();

        HighlightButton(highlightedButton);
    }

    private void ClearHighlight()
    {
        if (!highlightedButton)
            return;

        var colors =
            highlightedButton.colors;

        highlightedButton.targetGraphic.color =
            colors.normalColor;

        highlightedButton = null;
    }

    private void OnScrollbarValueChanged(float value)
    {
        // Disable when limits are reached.
        scrollUpButton.interactable =
            value < 1f;

        scrollDownButton.interactable =
            value > 0f;
    }

    public void Start()
    {
        cancelButton.onClick.AddListener(() =>
        {
            PlayMissionManager manager =
                GetComponent<PlayMissionManager>();

            manager.ToggleVersaRunWindow(false);
            manager.raycastBlocker.SetActive(false);
        });

        playButton.onClick.AddListener(() =>
        {
            ExecuteHighlighted();

            SceneManager.LoadScene("Loading");
        });

        // Listen for scrollbar movement.
        scrollbar.onValueChanged.AddListener(
            OnScrollbarValueChanged
        );

        // Initial scrollbar state.
        OnScrollbarValueChanged(
            scrollbar.value
        );

        scrollUpButton.onClick.AddListener(
            ScrollUp
        );

        scrollDownButton.onClick.AddListener(
            ScrollDown
        );

        playButton.interactable = false;

        LoadRuns();
    }

    private void LoadRuns()
    {
        // Remove existing buttons.
        foreach (Button button in runButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        runButtons.Clear();

        if (!Directory.Exists(SaveDirectory))
        {
            Debug.LogWarning(
                "[VersaRunManager] Save directory does not exist:\n" +
                SaveDirectory
            );

            return;
        }

        string[] files =
            Directory.GetFiles(
                SaveDirectory,
                "vv*.txt"
            );

        List<RunEntry> runs =
            new List<RunEntry>();

        // -----------------------------------------
        // LOAD AND DECRYPT EVERY RUN
        // -----------------------------------------

        foreach (string file in files)
        {
            try
            {
                ViceVersaStateData data =
                    ViceVersaState.LoadFromFile(file);

                if (data == null)
                {
                    Debug.LogWarning(
                        "[VersaRunManager] Could not load run:\n" +
                        file
                    );

                    continue;
                }

                runs.Add(
                    new RunEntry
                    {
                        filePath = file,
                        data = data
                    }
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "[VersaRunManager] Failed to load run:\n" +
                    file +
                    "\n\n" +
                    e
                );
            }
        }

        // -----------------------------------------
        // SORT FASTEST -> SLOWEST
        // -----------------------------------------

        runs = runs
            .OrderBy(run => run.data.elapsedTime)
            .ToList();

        // -----------------------------------------
        // CREATE BUTTONS
        // -----------------------------------------

        foreach (RunEntry run in runs)
        {
            CreateRunButton(run);
        }

        // Start at the top of the list.
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition =
                1f;
        }
    }

    private void CreateRunButton(RunEntry run)
    {
        Button button =
            Instantiate(
                buttonInstance,
                content
            );

        button.gameObject.SetActive(true);

        runButtons.Add(button);

        // -----------------------------------------
        // FIRST CHILD = RUN NAME
        // -----------------------------------------

        if (button.transform.childCount > 0)
        {
            Transform filenameTransform =
                button.transform.GetChild(0);

            TMP_Text filenameText =
                filenameTransform.GetComponent<TMP_Text>();

            if (filenameText != null)
            {
                string filename =
                    Path.GetFileNameWithoutExtension(
                        run.filePath
                    );

                // Remove "vv" from the beginning.
                //
                // vvRun     -> Run
                // vvRun123  -> Run123
                // vvMyRun   -> MyRun
                if (filename.StartsWith(
                    "vv",
                    StringComparison.OrdinalIgnoreCase))
                {
                    filename =
                        filename.Substring(2);
                }

                filenameText.text =
                    filename;
            }
        }

        // -----------------------------------------
        // SECOND CHILD = SAVED TIME
        // -----------------------------------------

        if (button.transform.childCount > 1)
        {
            Transform timeTransform =
                button.transform.GetChild(1);

            TMP_Text timeText =
                timeTransform.GetComponent<TMP_Text>();

            if (timeText != null)
            {
                timeText.text =
                    Utils.FormatTime(
                        run.data.elapsedTime
                    );
            }
        }

        // -----------------------------------------
        // SELECT RUN
        // -----------------------------------------

        button.onClick.AddListener(() =>
        {
            HighlightButton(button);

            string filename = Path.GetFileName(run.filePath);

            VersaMode.SelectedRunFile =
                filename;

            playButton.interactable = true;
        });
    }

    private class RunEntry
    {
        public string filePath;
        public ViceVersaStateData data;
    }

    private void OnDestroy()
    {
        if (scrollbar != null)
        {
            scrollbar.onValueChanged.RemoveListener(
                OnScrollbarValueChanged
            );
        }
    }
}