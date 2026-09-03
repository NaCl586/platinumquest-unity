using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitializeGraphics : MonoBehaviour
{
    void Start()
    {
        int selectedQuality =
            PlayerPrefs.GetInt("Graphics_Quality", 5);

        QualitySettings.SetQualityLevel(
            selectedQuality,
            true);
    }
}
