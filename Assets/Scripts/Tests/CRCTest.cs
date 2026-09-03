using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CRCTest : MonoBehaviour
{
    void Start()
    {
        string path = Path.Combine(
            Application.streamingAssetsPath,
            "marble",
            "data",
            "missions_mbp",
            "beginner",
            "TrainingTowers.mis"
        );

        //DataIntegrityManager.VerifyMissionFile(path);
    }
}
