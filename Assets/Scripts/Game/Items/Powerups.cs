using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public enum PowerupType
{
    None,
    SuperJump,
    SuperSpeed,
    SuperBounce,
    ShockAbsorber,
    TimeTravel,
    TimePenalty,
    AntiGravity,
    Gyrocopter,
    EasterEgg,

    //pq
    Bubble,
    Fireball,
    Anvil,
    Teleporter,
    Transporter,
}

public class Powerups : MonoBehaviour
{
    [SerializeField]
    public PowerupType powerupType;

    [SerializeField]
    protected string powerupName;

    [SerializeField]
    protected bool autoUse;

    [SerializeField]
    public float respawnTime = 7f;

    [SerializeField]
    public int maxRespawns = -1;

    [SerializeField]
    protected AudioClip pickupSound;

    [SerializeField]
    protected AudioClip useSound;

    [Space]
    [SerializeField]
    MeshRenderer meshRenderer;

    [SerializeField]
    SkinnedMeshRenderer skinnedMeshRenderer;

    [HideInInspector]
    public bool isActive = true;

    protected float timeDeactivated;
    protected string bottomTextMsg = string.Empty;

    protected int respawnCount = 0;

    [HideInInspector]
    public bool rotateMesh = true;

    public bool rotate = true;

    public bool showHelpOnPickup = false;

    protected Transform mesh;

    protected virtual void Start()
    {
        isActive = true;
        mesh = transform.Find("Mesh");
        rotateMesh = mesh;
    }

    public virtual void PickupItem()
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
            {
                GameUIManager.instance.SetCenterText(
                    "Press the <func:bind mouseFire> to use the " + powerupName
                );
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        if (rotate)
        {
            if (rotateMesh)
            {
                var rot = mesh.rotation;
                mesh.rotation =
                    Quaternion.AngleAxis(
                        Time.fixedDeltaTime * 120f,
                        rot * Vector3.up
                    ) * rot;
            }
            else
            {
                transform.rotation =
                    Quaternion.AngleAxis(
                        Time.fixedDeltaTime * 120f,
                        transform.rotation * Vector3.up
                    ) * transform.rotation;
            }
        }

        if (!isActive &&
            Time.time - timeDeactivated >= respawnTime &&
            (maxRespawns < 0 || respawnCount < maxRespawns))
        {
            respawnCount++;
            Activate(true);
        }
    }

    public void Activate(bool fade)
    {
        isActive = true;

        foreach (Transform child in transform)
            child.gameObject.SetActive(true);

        if (fade)
        {
            if (meshRenderer)
            {
                foreach (Material mat in meshRenderer.materials)
                    mat.color = Color.clear;

                foreach (Material mat in meshRenderer.materials)
                    mat.DOColor(Color.white, 3f);
            }
            else if (skinnedMeshRenderer)
            {
                foreach (Material mat in skinnedMeshRenderer.materials)
                    mat.color = Color.clear;

                foreach (Material mat in skinnedMeshRenderer.materials)
                    mat.DOColor(Color.white, 3f);
            }
        }
    }

    protected virtual void Deactivate()
    {
        timeDeactivated = Time.time;
        isActive = false;

        GameManager.instance.PlayAudioClip(pickupSound);

        if (powerupType != PowerupType.TimeTravel &&
            powerupType != PowerupType.EasterEgg)
        {
            bottomTextMsg = "You recieved a " + powerupName;
        }

        GameUIManager.instance.SetBottomText(bottomTextMsg);

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);
    }

    protected virtual void DeactivateVV()
    {
        timeDeactivated = Time.time;
        isActive = false;

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);
    }

    protected virtual void UsePowerup()
    {
        // To be overridden
    }
}
