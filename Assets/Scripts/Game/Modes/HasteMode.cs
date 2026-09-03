using UnityEngine;

public class HasteMode : NullMode
{
    private readonly float speedToQualify;

    public HasteMode(GameManager gameManager) : base(gameManager)
    {
        speedToQualify = MissionInfo.instance.speedToQualify;
    }

    public override void OnMissionLoad()
    {
        base.OnMissionLoad();

        GameUIManager.instance.SetThresholdIconHaste(speedToQualify);
        GameUIManager.instance.InitVisibilityTresholdHasteIcon(true);
        GameUIManager.instance.SetTreasholdHasteIcon(false);
    }

    public override void OnUpdate()
    {
        if (GameManager.gameFinish)
            return;

        float speed = Movement.instance.marbleVelocity.magnitude;
        GameUIManager.instance.SetSpeedometer(speed);


        bool achieved = speed >= speedToQualify;

        GameUIManager.instance.SetTreasholdHasteIcon(
            achieved
        );
    }

    public override bool CanFinish()
    {
        if (!base.CanFinish())
            return false;

        return Movement.instance.marbleVelocity.magnitude >=
               speedToQualify;
    }

    public override string GetFinishMessage()
    {
        if (Movement.instance.marbleVelocity.magnitude < speedToQualify)
            return "You may not finish without reaching the qualifying speed!";

        return base.GetFinishMessage();
    }

}