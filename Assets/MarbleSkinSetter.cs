using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MarbleSkinSetter : MonoBehaviour
{
    public MeshRenderer marbleMesh;
    public MeshRenderer marbleTeleportMesh;
    public Texture[] marbleTextures;

    private void Start()
    {
        int selectedIndex = PlayerPrefs.GetInt("SelectedMarbleIndex", 0);
        marbleMesh.material.mainTexture = marbleTextures[selectedIndex];
        marbleTeleportMesh.material.mainTexture = marbleTextures[selectedIndex];
    }
}
