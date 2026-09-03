using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Anvil : Powerups
{
    [SerializeField]
    float superJumpHeight = -20f;

    public class OnUseAnvil : UnityEvent { };

    public static OnUseAnvil onUseAnvil = new OnUseAnvil();
    public static bool alreadyListened = false;

    GameObject psObj;

    protected override void Start()
    {
        base.Start();

        psObj = null;
        if (!alreadyListened)
        {
            alreadyListened = true;
            onUseAnvil.AddListener(UsePowerup);
        }
    }

    public void OnDisable()
    {
        alreadyListened = false;
        onUseAnvil.RemoveAllListeners();
    }

    public void OnEnable()
    {
        if (!alreadyListened)
        {
            alreadyListened = true;
            onUseAnvil.AddListener(UsePowerup);
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (psObj != null)
            psObj.transform.position = Marble.instance.transform.position;
    }

    protected override void UsePowerup()
    {
        GameManager.instance.PlayAudioClip(useSound);

        Movement.instance.marbleVelocity += -GravitySystem.GravityDir.normalized * superJumpHeight;
    }
}
