using UnityEngine;

public class UsePowerupTrigger : MonoBehaviour
{
    [Header("Powerup")]
    public PowerupType powerup = PowerupType.None;

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        switch (powerup)
        {
            case PowerupType.SuperJump:
                SuperJump.onUseSuperJump?.Invoke();
                break;

            case PowerupType.SuperBounce:
                SuperBounce.onUseSuperBounce?.Invoke();
                break;

            case PowerupType.ShockAbsorber:
                ShockAbsorber.onUseShockAbsorber?.Invoke();
                break;

            case PowerupType.Gyrocopter:
                Gyrocopter.onUseGyrocopter?.Invoke();
                break;

            case PowerupType.TimeTravel:
                marble.ActivateTimeTravel(5f);
                break;

            case PowerupType.TimePenalty:
                marble.ActivateTimeTravel(-5f);
                break;

            default:
                Debug.LogWarning(
                    $"UsePowerupTrigger: unsupported powerup \"{powerup}\""
                );
                break;
        }
    }
}