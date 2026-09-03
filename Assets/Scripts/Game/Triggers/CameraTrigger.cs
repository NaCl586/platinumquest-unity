using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Camera pitch. Use \"NoChange\" to leave unchanged.")]
    public string pitch = "NoChange";

    [Tooltip("Camera yaw. Use \"NoChange\" to leave unchanged.")]
    public string yaw = "NoChange";

    [Header("Settings")]
    [Tooltip("If false, pitch/yaw values in the .mis file are interpreted as degrees.")]
    public bool useRadians = true;

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        // CameraTrigger only affects the local/player marble.
        if (marble != Marble.instance)
            return;

        CameraController cameraController =
            CameraController.instance;

        if (cameraController == null)
            return;

        bool changePitch =
            !string.IsNullOrEmpty(pitch) &&
            !string.Equals(
                pitch,
                "NoChange",
                System.StringComparison.OrdinalIgnoreCase
            );

        bool changeYaw =
            !string.IsNullOrEmpty(yaw) &&
            !string.Equals(
                yaw,
                "NoChange",
                System.StringComparison.OrdinalIgnoreCase
            );

        // Nothing to change.
        if (!changePitch && !changeYaw)
            return;

        // Start with the CURRENT camera values.
        // This is important because "NoChange" must actually
        // preserve the existing value.
        float pitchValue =
            cameraController.CameraPitch;

        float yawValue =
            cameraController.CameraYaw;

        // --------------------------------------------------
        // Pitch
        // --------------------------------------------------

        if (changePitch)
        {
            if (float.TryParse(pitch, out float parsedPitch))
            {
                if (!useRadians)
                    parsedPitch *= Mathf.Deg2Rad;

                pitchValue = parsedPitch;
            }
            else
            {
                Debug.LogWarning(
                    $"CameraTrigger '{name}': invalid pitch value '{pitch}'."
                );

                changePitch = false;
            }
        }

        // --------------------------------------------------
        // Yaw
        // --------------------------------------------------

        if (changeYaw)
        {
            if (float.TryParse(yaw, out float parsedYaw))
            {
                if (!useRadians)
                    parsedYaw *= Mathf.Deg2Rad;

                // Faithful to the Haxe implementation:
                //
                // yaw = -yaw;
                //
                yawValue = -parsedYaw;
            }
            else
            {
                Debug.LogWarning(
                    $"CameraTrigger '{name}': invalid yaw value '{yaw}'."
                );

                changeYaw = false;
            }
        }

        // --------------------------------------------------
        // Apply
        // --------------------------------------------------

        cameraController.SetCameraAngles(
            yawValue,
            pitchValue,
            changeYaw,
            changePitch
        );
    }
}