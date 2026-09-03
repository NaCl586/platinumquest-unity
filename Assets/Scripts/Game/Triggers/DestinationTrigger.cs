using UnityEngine;

public class DestinationTrigger : MonoBehaviour
{
    [Header("Destination")]
    public Transform spawn;
    public Transform cameraPos;

    [Header("Destination Options")]
    public bool centerDestinationPoint;
    public bool keepVelocity;
    public bool inverseVelocity;
    public bool keepAngular;
    public bool keepCamera;
    public float cameraYaw;
}