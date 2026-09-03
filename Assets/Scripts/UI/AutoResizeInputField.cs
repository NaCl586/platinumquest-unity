using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class AutoResizeInputField : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputField;

    [Header("Height")]
    [SerializeField]
    private float minHeight = 100f;

    [SerializeField]
    private float maxHeight = 250f;

    [SerializeField]
    private float padding = 20f;

    private RectTransform rectTransform;
    private LayoutElement layoutElement;

    void OnEnable()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();

        rectTransform = GetComponent<RectTransform>();
        layoutElement = GetComponent<LayoutElement>();

        if (Application.isPlaying)
            inputField.onValueChanged.AddListener(OnValueChanged);

        Resize();
    }

    void OnDisable()
    {
        if (Application.isPlaying && inputField != null)
            inputField.onValueChanged.RemoveListener(OnValueChanged);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
                Resize();
        };
    }
#endif

    void Update()
    {
        // Keeps the height updated while editing text in the Inspector
        if (!Application.isPlaying)
            Resize();
    }

    private void OnValueChanged(string _)
    {
        Resize();
    }

    public void Resize()
    {
        if (inputField == null)
            return;

        inputField.ForceLabelUpdate();
        Canvas.ForceUpdateCanvases();

        float preferredHeight = inputField.textComponent.preferredHeight + padding;
        float height = Mathf.Clamp(preferredHeight, minHeight, maxHeight);

        if (layoutElement != null)
        {
            layoutElement.preferredHeight = height;
        }
        else
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
}
