using UnityEngine;

public class OrbitingClouds : MonoBehaviour
{
    [SerializeField] private bool reverse;

    public void SetReverse(bool value)
    {
        reverse = value;
        ApplyAnimation();
    }

    private void Start()
    {
        ApplyAnimation();
    }

    private void ApplyAnimation()
    {
        Animator animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning($"OrbitingClouds on {name} has no Animator.");
            return;
        }

        animator.Play(reverse ? "orbit-reverse" : "orbit", 0, 0f);
    }
}