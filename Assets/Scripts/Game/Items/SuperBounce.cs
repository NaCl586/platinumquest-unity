using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SuperBounce : Powerups
{
    public class OnUseSuperBounce : UnityEvent { };

    public static OnUseSuperBounce onUseSuperBounce = new OnUseSuperBounce();
    public static bool alreadyListened = false;

    protected override void Start()
    {
        base.Start();

        if (!alreadyListened)
        {
            alreadyListened = true;
            onUseSuperBounce.AddListener(UsePowerup);
        }
    }

    public void OnDisable()
    {
        alreadyListened = false;
        onUseSuperBounce.RemoveAllListeners();
    }

    public void OnEnable()
    {
        if (!alreadyListened)
        {
            alreadyListened = true;
            onUseSuperBounce.AddListener(UsePowerup);
        }
    }

    protected override void UsePowerup()
    {
        Marble.instance.StopSound(PowerupType.ShockAbsorber);
        Marble.instance.PlaySound(PowerupType.SuperBounce);
        Marble.instance.UseSuperBounce();
        GameManager.instance.sbsaActiveTime = Time.time;
    }
}
