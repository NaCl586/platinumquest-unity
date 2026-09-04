using UnityEngine;

public class Fireball : Powerups
{
    [Header("Fireball")]
    public float activeTime = 7f;

    protected override void Start()
    {
        base.Start();
        autoUse = false;
    }

    public override void PickupItem()
    {
        if (!isActive)
            return;

        Marble marble = Marble.instance;

        if (marble == null)
            return;

        if (marble.fireball && marble.GetFireballTime() >= activeTime)
        {
            return;
        }

        marble.ActivateFireball(activeTime);
        GameUIManager.instance.SetFireballTimer(activeTime, activeTime);

        Deactivate();
    }
}
