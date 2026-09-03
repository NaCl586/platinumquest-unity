using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomBackgrounds : MonoBehaviour
{
    public Sprite[] backgrounds;
    public Image bg;

    public void Start()
    {
        bg.sprite = backgrounds[Random.Range(0, backgrounds.Length)];
    }
}
