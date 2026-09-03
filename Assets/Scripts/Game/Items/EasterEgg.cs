using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//an Easter Egg!
public class EasterEgg : Powerups
{
    public AudioClip gotEasterEggSfx;
    public AudioClip alreadyGotEggSfx;

    protected override void UsePowerup()
    {
        if (PlayerPrefs.GetInt(MissionInfo.instance.levelName + "_EasterEgg", 0) == 1)
        {
            bottomTextMsg = "You already found this Nest Egg.";
            pickupSound = alreadyGotEggSfx;
        }
        else
        {
            bottomTextMsg = "You found a Nest Egg!";
            pickupSound = gotEasterEggSfx;
            PlayerPrefs.SetInt(MissionInfo.instance.levelName + "_EasterEgg", 1);

            int easterEggCount = PlayerPrefs.GetInt("EasterEggCollected", 0);
            easterEggCount++;
            PlayerPrefs.SetInt("EasterEggCollected", easterEggCount);
        }
    }
}
