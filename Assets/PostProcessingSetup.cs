using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessingSetup : MonoBehaviour
{
    void Start()
    {
        gameObject.SetActive(PlayerPrefs.GetInt("Graphics_PostProcessing", 1) == 1);
    }
}
