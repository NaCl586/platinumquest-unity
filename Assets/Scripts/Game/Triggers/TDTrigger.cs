using System;
using System.Globalization;
using UnityEngine;
using TS;

public class TDTrigger : MonoBehaviour
{
    [Header("2D Mode")]
    public string plane = "xz";
    public bool invertDirection;
    public bool keepEffectOnLeave;

    [Header("Camera")]
    public float camDistance = float.NaN;
    public float targetPitch = float.NaN;
    public bool changesPitch;

    private TwoDMode twoDMode;

    // True when this trigger changed the active game mode
    // from NullMode to TwoDMode.
    private bool createdTwoDMode;

    // ============================================================
    // TSObject initialization
    // ============================================================

    public void Initialize(TSObject obj)
    {
        if (obj == null)
            return;

        // --------------------------------------------------------
        // Plane
        // --------------------------------------------------------

        string planeField = obj.GetField("Plane");

        if (!string.IsNullOrEmpty(planeField))
            plane = planeField;
        else
            plane = "xz";

        // --------------------------------------------------------
        // InvertDirection
        // --------------------------------------------------------

        string invertField =
            obj.GetField("InvertDirection");

        invertDirection =
            !string.IsNullOrEmpty(invertField) &&
            ParseBoolean(invertField);

        // --------------------------------------------------------
        // KeepEffectOnLeave
        // --------------------------------------------------------

        string keepField =
            obj.GetField("keepeffectonleave");

        keepEffectOnLeave =
            !string.IsNullOrEmpty(keepField) &&
            ParseBoolean(keepField);

        // --------------------------------------------------------
        // CamDistance
        // --------------------------------------------------------

        string distanceField =
            obj.GetField("CamDistance");

        if (
            !string.IsNullOrEmpty(distanceField) &&
            !distanceField.Equals(
                "nochange",
                StringComparison.OrdinalIgnoreCase
            ) &&
            float.TryParse(
                distanceField,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float distance
            )
        )
        {
            camDistance = distance;
        }
        else
        {
            camDistance = float.NaN;
        }

        // --------------------------------------------------------
        // TargetPitch
        // --------------------------------------------------------

        string pitchField =
            obj.GetField("targetPitch");

        changesPitch =
            !string.IsNullOrEmpty(pitchField) &&
            !pitchField.Equals(
                "nochange",
                StringComparison.OrdinalIgnoreCase
            );

        if (
            changesPitch &&
            float.TryParse(
                pitchField,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float pitch
            )
        )
        {
            // Haxe stores this as degrees and converts to radians.
            targetPitch =
                pitch * Mathf.Deg2Rad;
        }
        else
        {
            targetPitch = float.NaN;
            changesPitch = false;
        }
    }

    // ============================================================
    // Trigger enter
    // ============================================================

    private void OnTriggerEnter(Collider other)
    {
        if (!IsMarble(other))
            return;

        if (GameManager.instance == null)
            return;

        bool wasAlreadyTwoD = false;

        foreach (IGameMode mode in GameManager.instance.GameModes)
        {
            if (mode is TwoDMode)
            {
                wasAlreadyTwoD = true;
                break;
            }
        }

        twoDMode =
            GameManager.instance.ActivateTwoDMode();

        if (twoDMode == null)
            return;

        createdTwoDMode = !wasAlreadyTwoD;

        twoDMode.Activate(
            TwoDMode.PlaneToYaw(
                plane,
                invertDirection
            ),
            camDistance,
            changesPitch,
            targetPitch
        );
    }

    // ============================================================
    // Trigger exit
    // ============================================================

    private void OnTriggerExit(Collider other)
    {
        if (!IsMarble(other))
            return;

        if (keepEffectOnLeave)
            return;

        if (twoDMode == null)
            return;

        twoDMode.Deactivate();

        /*
         * Only return to NullMode if THIS trigger created
         * the temporary TwoDMode.
         *
         * A mission whose actual game mode is TwoDMode
         * stays in TwoDMode.
         */
        if (createdTwoDMode)
        {
            if (GameManager.instance != null)
                GameManager.instance.DeactivateTwoDMode();

            twoDMode = null;
            createdTwoDMode = false;
        }
    }

    // ============================================================
    // Boolean parsing
    // ============================================================

    private bool ParseBoolean(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        value = value.Trim();

        if (value == "1")
            return true;

        if (value == "0")
            return false;

        if (
            bool.TryParse(
                value,
                out bool result
            )
        )
        {
            return result;
        }

        return false;
    }

    // ============================================================
    // Marble detection
    // ============================================================

    private bool IsMarble(Collider other)
    {
        if (other == null)
            return false;

        if (Marble.instance == null)
            return false;

        return other.GetComponent<Marble>() ==
               Marble.instance;
    }
}