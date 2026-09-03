using UnityEngine;

public class ControlBinding : MonoBehaviour
{
    public static ControlBinding instance;

    // Marble controls
    public KeyCode moveForward;
    public KeyCode moveBackward;
    public KeyCode moveLeft;
    public KeyCode moveRight;

    public KeyCode usePowerup;
    public KeyCode jump;

    // Camera controls
    public KeyCode rotateCameraUp;
    public KeyCode rotateCameraDown;
    public KeyCode rotateCameraLeft;
    public KeyCode rotateCameraRight;

    // Mouse controls
    public KeyCode freelookKey;
    public float mouseSensitivity;
    public bool invertMouseYAxis;
    public bool alwaysFreeLook;

    // Keyboard sensitivity
    public float keyboardSensitivity;

    // Other controls
    public KeyCode blast;
    public KeyCode respawn;
    public KeyCode toggleRadar;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadBindings();
    }

    public void LoadBindings()
    {
        // Marble
        moveForward = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Marble_Forward",
                "W"
            )
        );

        moveBackward = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Marble_Back",
                "S"
            )
        );

        moveLeft = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Marble_Left",
                "A"
            )
        );

        moveRight = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Marble_Right",
                "D"
            )
        );

        usePowerup = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Marble_Powerup",
                "Left Mouse Button"
            )
        );

        jump = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Marble_Jump",
                "Space"
            )
        );

        // Camera
        rotateCameraDown = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Cam_Down",
                "Down"
            )
        );

        rotateCameraLeft = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Cam_Left",
                "Left"
            )
        );

        rotateCameraRight = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Cam_Right",
                "Right"
            )
        );

        rotateCameraUp = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Cam_Up",
                "Up"
            )
        );

        // Mouse
        freelookKey = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Mouse_Freelook",
                "Right Mouse Button"
            )
        );

        mouseSensitivity = PlayerPrefs.GetFloat(
            "Controls_MouseSensitivity",
            1f
        );

        invertMouseYAxis = PlayerPrefs.GetInt(
            "Controls_Mouse_InvertYAxis",
            0
        ) == 1;

        alwaysFreeLook = PlayerPrefs.GetInt(
            "Controls_Mouse_Freelook",
            1
        ) == 1;

        // Keyboard sensitivity
        keyboardSensitivity = PlayerPrefs.GetFloat(
            "Controls_KeyboardSensitivity",
            1f
        );

        // Other
        blast = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Blast",
                "E"
            )
        );

        respawn = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_Respawn",
                "R"
            )
        );

        toggleRadar = Utils.ParseKeyCode(
            PlayerPrefs.GetString(
                "Control_ToggleRadar",
                "Tab"
            )
        );
    }

    public void AssignKey(
        string controlName,
        KeyCode keycode)
    {
        switch (controlName)
        {
            // Marble
            case "Move Forward":
                moveForward = keycode;

                PlayerPrefs.SetString(
                    "Control_Marble_Forward",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            case "Move Backward":
                moveBackward = keycode;

                PlayerPrefs.SetString(
                    "Control_Marble_Back",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            case "Move Left":
                moveLeft = keycode;

                PlayerPrefs.SetString(
                    "Control_Marble_Left",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            case "Move Right":
                moveRight = keycode;

                PlayerPrefs.SetString(
                    "Control_Marble_Right",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            case "Use Powerup":
                usePowerup = keycode;

                PlayerPrefs.SetString(
                    "Control_Marble_Powerup",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            case "Jump":
                jump = keycode;

                PlayerPrefs.SetString(
                    "Control_Marble_Jump",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            // Camera
            case "Rotate Camera Down":
                rotateCameraDown = keycode;

                PlayerPrefs.SetString(
                    "Control_Cam_Down",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            case "Rotate Camera Left":
                rotateCameraLeft = keycode;

                PlayerPrefs.SetString(
                    "Control_Cam_Left",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            case "Rotate Camera Right":
                rotateCameraRight = keycode;

                PlayerPrefs.SetString(
                    "Control_Cam_Right",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            case "Rotate Camera Up":
                rotateCameraUp = keycode;

                PlayerPrefs.SetString(
                    "Control_Cam_Up",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            // Mouse
            case "Free-Look Key":
                freelookKey = keycode;

                PlayerPrefs.SetString(
                    "Control_Mouse_Freelook",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            // Other
            case "Blast":
                blast = keycode;

                PlayerPrefs.SetString(
                    "Control_Blast",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            case "Respawn":
                respawn = keycode;

                PlayerPrefs.SetString(
                    "Control_Respawn",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            case "Toggle Radar":
                toggleRadar = keycode;

                PlayerPrefs.SetString(
                    "Control_ToggleRadar",
                    Utils.KeyCodeToString(keycode)
                );
                break;

            default:
                Debug.LogWarning(
                    $"ControlBinding: Unknown control name '{controlName}'"
                );
                return;
        }

        PlayerPrefs.Save();
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;

        PlayerPrefs.SetFloat(
            "Controls_MouseSensitivity",
            sensitivity
        );

        PlayerPrefs.Save();
    }

    public void SetKeyboardSensitivity(float sensitivity)
    {
        keyboardSensitivity = sensitivity;

        PlayerPrefs.SetFloat(
            "Controls_KeyboardSensitivity",
            sensitivity
        );

        PlayerPrefs.Save();
    }

    public static float SensitivityToSliderValue(float sensitivity)
    {
        return Mathf.Clamp(sensitivity * 25f, 0f, 95f);
    }

    public static float SliderValueToSensitivity(float value)
    {
        return Mathf.Clamp(value / 25f, 0f, 3.8f);
    }
}
