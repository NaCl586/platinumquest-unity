using UnityEngine;

public class Bubble : Powerups
{
    [Header("Bubble")]
    public float duration = 5f;
    public bool infinite = false;

    protected override void Start()
    {
        base.Start();
        autoUse = false;
    }

    public override void PickupItem()
    {
        if (!isActive)
            return;

        Marble marble = FindFirstObjectByType<Marble>();

        if (marble == null)
            return;

        if (IsFireballActive())
            return;

        marble.SetBubbleTime(duration, infinite);
        GameUIManager.instance.SetBubbleTimer(infinite ? Mathf.Infinity : duration, duration);

        Deactivate();
    }

    protected override void FixedUpdate()
    {
        if (Time.time - timeDeactivated >= respawnTime && !this.isActive)
            Activate(true);
    }

    protected override void UsePowerup() { }

    private void LateUpdate()
    {
        if (Camera.main == null)
            return;

        Transform mesh = transform.Find("Mesh");

        if (mesh == null)
            return;

        mesh.rotation = Camera.main.transform.rotation;
    }

    private bool IsFireballActive()
    {
        return false;
    }
}
