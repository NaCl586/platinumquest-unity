using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera cam;

    private void Start()
    {
        cam = CameraController.instance.GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (cam == null)
            return;

        transform.forward = cam.transform.forward;
    }
}
