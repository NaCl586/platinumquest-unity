using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Billboard : MonoBehaviour
{
    private Camera cam;

    private void Start()
    {
        StartCoroutine(WaitUntilCameraFound());
    }

    IEnumerator WaitUntilCameraFound()
    {
        yield return null;

        while (cam == null)
        {
            cam = GameObject.Find("Main Camera").GetComponent<Camera>();
            yield return null;
        }
    }

    private void LateUpdate()
    {
        if (cam == null)
            return;

        transform.forward = cam.transform.forward;
    }
}
