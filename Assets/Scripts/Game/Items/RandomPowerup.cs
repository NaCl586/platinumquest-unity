using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RandomPowerup : Powerups
{
    public AudioClip pickupSuperJump;
    public AudioClip pickupSuperSpeed;
    public AudioClip pickupSuperBounce;
    public AudioClip pickupShockAbsorber;
    public AudioClip pickupGyrocopter;
    public AudioClip pickupTimeTravel;

    public SuperJump superJump;
    public SuperSpeed superSpeed;
    public SuperBounce superBounce;
    public ShockAbsorber shockAbsorber;
    public Gyrocopter gyrocopter;

    public static SuperJump superJumpInstance;
    public static SuperSpeed superSpeedInstance;
    public static SuperBounce superBounceInstance;
    public static ShockAbsorber shockAbsorberInstance;
    public static Gyrocopter gyrocopterInstance;

    protected override void Start()
    {
        //subscribing to events

        base.Start();

        if (!superJumpInstance)
            superJumpInstance = Instantiate(superJump);
        if (!superSpeedInstance)
            superSpeedInstance = Instantiate(superSpeed);
        if (!superBounceInstance)
            superBounceInstance = Instantiate(superBounce);
        if (!shockAbsorberInstance)
            shockAbsorberInstance = Instantiate(shockAbsorber);
        if (!gyrocopterInstance)
            gyrocopterInstance = Instantiate(gyrocopter);
    }

    protected override void UsePowerup()
    {
        PowerupType powerUp = RandomEnumExcept(
            PowerupType.AntiGravity,
            PowerupType.None,
            PowerupType.EasterEgg
        );

        switch (powerUp)
        {
            case PowerupType.SuperJump:
                powerupName = "Jump Boost PowerUp!";
                pickupSound = pickupSuperJump;
                break;
            case PowerupType.SuperSpeed:
                powerupName = "Speed Booster PowerUp!";
                pickupSound = pickupSuperSpeed;
                break;
            case PowerupType.SuperBounce:
                powerupName = "Marble Recoil PowerUp!";
                pickupSound = pickupSuperBounce;
                break;
            case PowerupType.ShockAbsorber:
                powerupName = "Anti Recoil PowerUp!";
                pickupSound = pickupShockAbsorber;
                break;
            case PowerupType.Gyrocopter:
                powerupName = "Helicopter PowerUp!";
                pickupSound = pickupGyrocopter;
                break;
        }

        if (powerUp != PowerupType.TimeTravel)
        {
            powerupType = powerUp;
            respawnTime = 7;

            GameManager.instance.activePowerup = powerupType;
            GameUIManager.instance.SetPowerupIcon(powerupType);
        }
        else
        {
            powerupType = powerUp;
            respawnTime = Mathf.Infinity;
            pickupSound = pickupTimeTravel;
            bottomTextMsg = "You picked up a 5 second Time Modifier Bonus!";
            Marble.instance.ActivateTimeTravel(5f);
        }
    }

    public static T RandomEnumExcept<T>(params T[] excluded)
        where T : System.Enum
    {
        var values = System.Enum.GetValues(typeof(T)).Cast<T>().Except(excluded).ToArray();

        if (values.Length == 0)
            throw new System.InvalidOperationException("No enum values left after exclusion.");

        return values[Random.Range(0, values.Length)];
    }
}
