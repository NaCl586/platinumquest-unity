using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nuke : MonoBehaviour
{
    public Transform explosionParticle;
    public float respawnTime = 15f;

    [Space]
    [HideInInspector]
    public bool isActive = true;
    float timeDeactivated;

    MeshRenderer mr;
    MeshCollider mc;

    AudioSource audioSource;
    public AudioClip explodeSfx;

    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        mc = GetComponent<MeshCollider>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Time.time - timeDeactivated >= respawnTime)
        {
            ActivateLandMine();
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        Movement movement = null;
        if (collision.gameObject.TryGetComponent<Movement>(out movement))
        {
            CollisionEnter(collision.gameObject, movement);
        }
    }

    public void CollisionEnter(GameObject marble, Movement m)
    {
        // Marble + positions
        Vector3 nukePos = transform.position;

        // Add velocity to the marble
        Vector3 explosionForce = ComputeExplosionStrength(marble.transform.position - nukePos);

        m.marbleVelocity += explosionForce;

        var effect = Instantiate(explosionParticle);
        effect.transform.position = transform.position;
        effect.transform.localScale = Vector3.one * 12f;
        Destroy(effect.gameObject, 10f);

        audioSource.PlayOneShot(explodeSfx);

        DeactivateLandMine();
    }

    Vector3 ComputeExplosionStrength(Vector3 distVec)
    {
        float range = 10f;
        float power = 100f;

        float dist = distVec.magnitude;
        if (dist < range)
        {
            float scalar = (1f - dist / range) * power;
            distVec *= scalar; // same as distVec = distVec * scalar
        }

        return distVec;
    }

    public void DeactivateLandMine()
    {
        timeDeactivated = Time.time;
        isActive = false;

        mr.enabled = false;
        mc.enabled = false;
    }

    public void ActivateLandMine()
    {
        isActive = true;

        mr.enabled = true;
        mc.enabled = true;
    }
}
