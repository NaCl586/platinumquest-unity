using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimeTravel : Powerups
{
    public float timeBonus = 5f;
    public bool IsActive => isActive;

    protected override void Start()
    {
        base.Start();

        bottomTextMsg =
            "You picked up a " + timeBonus.ToString("0.###") + " second " + powerupName + " Bonus!";
    }

    public override void PickupItem()
    {
        if (isActive)
        {
            if (autoUse)
            {
                UsePowerup();
            }
            else
            {
                GameManager.instance.activePowerup = powerupType;
                GameUIManager.instance.SetPowerupIcon(powerupType);
            }

            Deactivate();

            if (showHelpOnPickup)
                GameUIManager.instance.SetCenterText(
                    "Press the <func:bind mouseFire> to use the " + powerupName
                );

            if (timeBonus >= 0)
                GameUIManager.instance.DisplayTimeTravelMessage(timeBonus);
            else
                GameUIManager.instance.DisplayTimePenaltyMessage(timeBonus);
        }
    }

    protected override void UsePowerup()
    {
        Marble.instance.ActivateTimeTravel(timeBonus);
    }

    public void SetActiveState(bool active)
    {
        if (active)
            Activate(false);
        else
            DeactivateVV();
    }
}
