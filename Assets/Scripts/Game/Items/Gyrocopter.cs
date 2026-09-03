using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Gyrocopter : Powerups
{
    public class OnUseGyrocopter : UnityEvent { };

    public static OnUseGyrocopter onUseGyrocopter = new OnUseGyrocopter();
    public static bool alreadyListened = false;

    protected override void Start()
    {
        base.Start();

        if (!alreadyListened)
        {
            alreadyListened = true;
            onUseGyrocopter.AddListener(UsePowerup);
        }
    }

    public void OnDisable()
    {
        alreadyListened = false;
        onUseGyrocopter.RemoveAllListeners();
    }

    public void OnEnable()
    {
        if (!alreadyListened)
        {
            alreadyListened = true;
            onUseGyrocopter.AddListener(UsePowerup);
        }
    }

    protected override void UsePowerup()
    {
        Marble.instance.UseGyrocopter();
        GameManager.instance.gyroActiveTime = Time.time;
    }
}
