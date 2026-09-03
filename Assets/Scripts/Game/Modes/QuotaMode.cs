public class QuotaMode : NullMode
{
    private readonly int gemQuota;

    public QuotaMode(GameManager gameManager, int gemQuota)
        : base(gameManager)
    {
        this.gemQuota = gemQuota;
    }

    public int GemQuota => gemQuota;

    public override void OnMissionLoad()
    {
        GameUIManager.instance.SetTargetGem(gemQuota);
        GameUIManager.instance.SetCurrentGem(gameManager.CurrentGems);
        GameUIManager.instance.SetQuotaGemDigit(gameManager.TotalGems);
    }

    public override void OnRespawn()
    {
        base.OnRespawn();
    }

    public override bool CanFinish()
    {
        return gameManager.TotalGems == 0 ||
               gameManager.CurrentGems >= gemQuota;
    }

    public override string GetFinishMessage()
    {
        if (gameManager.TotalGems > 0 &&
            gameManager.CurrentGems < gemQuota)
        {
            return "You may not finish without reaching the gem quota!";
        }

        if (gameManager.TotalGems > 0 &&
            gameManager.CurrentGems == gameManager.TotalGems)
        {
            return "Wha-? How?! You ACED the level! You Rock!";
        }

        return "Congratulations! You've finished!";
    }

    public override string GetGemPickupMessage()
    {
        int gemCount = gameManager.CurrentGems;

        int remainingToQuota =
            gemQuota - gemCount;

        int remainingToAll =
            gameManager.TotalGems - gemCount;

        // Reached quota exactly.
        if (remainingToQuota == 0)
        {
            return "You've reached the gem quota, head for the finish!";
        }

        // One gem remaining to quota.
        if (remainingToQuota == 1)
        {
            return "You picked up a gem! Only one gem to go!";
        }

        // More than one gem remaining to quota.
        if (remainingToQuota > 1)
        {
            return $"You picked up a gem! {remainingToQuota} gems to go!";
        }

        // Quota has already been exceeded.
        // Now report progress toward 100%.

        if (gemCount == gameManager.TotalGems)
        {
            if (MissionInfo.instance.time == -1)
            {
                return "Wow, you got all the gems! Head for the finish!";
            }

            if (gameManager.elapsedTime < MissionInfo.instance.time)
            {
                return "Wow, you got all the gems! Head for the finish before time runs out!";
            }

            return "You got all the gems, but the time already ran out!";
        }

        if (remainingToAll == 1)
        {
            return "You picked up a gem! Only one more gem to reach 100%!";
        }

        return $"You picked up a gem! {remainingToAll} gems more to reach 100%!";
    }

    public override bool ShouldPlayCollectAllGemsSound(int newGemCount)
    {
        return newGemCount == gemQuota ||
               newGemCount == gameManager.TotalGems;
    }

    public override void OnGemCollected(int newGemCount)
    {
    }

}