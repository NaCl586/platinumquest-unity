using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DropdownAutoResize : MonoBehaviour
{
    public float maxHeight = 200f;

    private TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    private void OnEnable()
    {
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDisable()
    {
        dropdown.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(int value)
    {
        StartCoroutine(ResizeNextFrame());
    }

    private IEnumerator ResizeNextFrame()
    {
        yield return null;

        ResizePopup();
    }

    public void ResizePopup()
    {
        if (dropdown == null || dropdown.template == null)
            return;

        RectTransform template = dropdown.template;

        RectTransform viewport =
            template.Find("Viewport") as RectTransform;

        if (viewport == null)
            return;

        RectTransform content =
            viewport.Find("Content") as RectTransform;

        if (content == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        float contentHeight =
            LayoutUtility.GetPreferredHeight(content);

        float height =
            Mathf.Min(contentHeight, maxHeight);

        template.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height
        );

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            template
        );

        Canvas.ForceUpdateCanvases();
    }
}