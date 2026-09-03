using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartPad : MonoBehaviour
{
    public GameObject regularMesh, constructionMesh;

    public void SetMeshRegular(bool regular)
    {
        regularMesh.SetActive(regular);
        constructionMesh.SetActive(!regular);
    }
}
