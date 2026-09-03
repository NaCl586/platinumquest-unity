using UnityEngine;

public class UnderwaterEffect : MonoBehaviour
{
    public static UnderwaterEffect instance { get; private set; }

    [Header("Camera")]
    [SerializeField]
    private Camera targetCamera;

    [Header("Material")]
    [SerializeField]
    private Material underwaterMaterial;

    [Header("Transition")]
    [SerializeField]
    private float transitionSpeed = 5f;

    private float intensity;
    private float targetIntensity;

    private static readonly int IntensityID = Shader.PropertyToID("_Intensity");

    private void Awake()
    {
        instance = this;

        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        bool underwater = WaterPhysicsTrigger.IsCameraUnderwater(targetCamera);

        targetIntensity = underwater ? 1f : 0f;

        intensity = Mathf.MoveTowards(intensity, targetIntensity, transitionSpeed * Time.deltaTime);

        if (underwaterMaterial != null)
        {
            underwaterMaterial.SetFloat(IntensityID, intensity);
        }
    }
}
