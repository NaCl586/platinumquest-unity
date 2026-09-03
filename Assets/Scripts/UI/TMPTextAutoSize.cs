using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPTextAutoSize : MonoBehaviour
{
    [SerializeField]
    private float padding = 4f;

    private TextMeshProUGUI text;
    private RectTransform rect;

    private float lastHeight = -1f;

    private void OnEnable()
    {
        text = GetComponent<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();

        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (text == null)
            text = GetComponent<TextMeshProUGUI>();

        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (text == null || rect == null)
            return;

        // Make sure TMP knows the latest text.
        text.ForceMeshUpdate();

        // Use the actual current width of the text object.
        float width = rect.rect.width;

        if (width <= 0f)
            return;

        // Explicitly calculate the preferred height
        // from the CURRENT text and CURRENT width.
        Vector2 preferred = text.GetPreferredValues(text.text, width, 0f);

        float preferredHeight = preferred.y + padding;

        if (Mathf.Approximately(preferredHeight, lastHeight))
        {
            return;
        }

        lastHeight = preferredHeight;

        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);

        Canvas.ForceUpdateCanvases();
    }
}
