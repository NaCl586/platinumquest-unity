using UnityEngine;

public class ConsistencyMode : NullMode
{
    private readonly float minimumSpeed;
    private readonly float gracePeriod;
    private readonly float penaltyDelay;

    private float belowSpeedSince = -1f;
    private bool failing;
    private bool failed;
    private float lastSpawnTime = Mathf.NegativeInfinity;

    public ConsistencyMode(GameManager gameManager) : base(gameManager)
    {
        minimumSpeed = MissionInfo.instance.minimumSpeed;
        gracePeriod = MissionInfo.instance.gracePeriod;
        penaltyDelay = MissionInfo.instance.penaltyDelay > 0f
            ? MissionInfo.instance.penaltyDelay
            : 2f;
    }

    public override void OnMissionLoad()
    {
        base.OnMissionLoad();

        GameUIManager.instance.SetThresholdIconConsistency(minimumSpeed);

        // Initially not failing.
        GameUIManager.instance.InitVisibilityTresholdConsistencyIcon(true);
        GameUIManager.instance.SetTreasholdConsistencyIcon(false);
    }

    public override void OnRespawn()
    {
        base.OnRespawn();

        failing = false;
        failed = false;
        belowSpeedSince = -1f;

        lastSpawnTime = gameManager.elapsedTime / 1000f;

        // Reset visual state after respawn.
        GameUIManager.instance.SetTreasholdConsistencyIcon(false);
    }

    public override void OnRestart()
    {
        failing = false;
        failed = false;
        belowSpeedSince = -1f;
        lastSpawnTime = Mathf.NegativeInfinity;
    }

    public override void OnUpdate()
    {
        if (GameManager.gameFinish)
            return;

        float speed = Movement.instance.marbleVelocity.magnitude;

        // Speedometer is updated continuously.
        GameUIManager.instance.SetSpeedometer(speed);

        float currentTime = gameManager.elapsedTime / 1000f;

        if(speed < minimumSpeed)
        {
            GameUIManager.instance.SetTreasholdConsistencyIcon(false);
        }
        else
        {
            GameUIManager.instance.SetTreasholdConsistencyIcon(true);
        }

        // Initial grace period.
        if (currentTime < gracePeriod)
            return;

        // Grace period after respawning.
        if (currentTime - lastSpawnTime < gracePeriod)
            return;

        if (speed < minimumSpeed)
        {
            if (!failing)
            {
                failing = true;
                belowSpeedSince = currentTime;
                GameUIManager.instance.SetCenterText("Too slow!");
            }
            else if (currentTime - belowSpeedSince >= penaltyDelay)
            {
                OnConsistencyFail();
            }
        }
        else
        {
            failing = false;
            belowSpeedSince = -1f;
        }
    }

    private void OnConsistencyFail()
    {
        failed = true;

        GameUIManager.instance.SetCenterText(
            "Consistency failed!"
        );

        GameManager.onOutOfBounds?.Invoke();
    }
}