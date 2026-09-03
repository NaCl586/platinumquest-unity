using System;
using System.Reflection;
using UnityEngine;

public class PushButton : MonoBehaviour
{
    [Header("Button Type")]
    public bool isToggleButton = false;
    public bool initialState = false;

    [Header("PQ Button Settings")]
    public float resetTime = 5f;
    public bool triggerOnce = false;

    [Header("Toggle Puzzle")]
    // -1 = field was not specified in the mission.
    public int correctState = -1;
    public int correctState1 = -1;

    [Header("Mission Callback")]
    public string triggerObject;
    public string objectMethod;

    [Header("Animation")]
    public Animator animator;
    public string pushAnimation = "push";

    [Header("Audio")]
    public AudioClip audioClip;

    private bool activated;
    private float activationTime = -Mathf.Infinity;

    // ToggleButton-specific state.
    private float disabledUntil = -Mathf.Infinity;
    private float currentCompletion = 0f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (isToggleButton)
        {
            activated = initialState;
            currentCompletion = initialState ? 1f : 0f;
        }
        else
        {
            activated = false;
            currentCompletion = 0f;
        }

        activationTime = -Mathf.Infinity;
        disabledUntil = -Mathf.Infinity;
    }

    private void Update()
    {
        if (isToggleButton)
        {
            UpdateToggleAnimation();
            return;
        }

        UpdatePushButton();
    }

    private void UpdatePushButton()
    {
        if (!activated || triggerOnce)
            return;

        if (Time.time - activationTime >= resetTime)
            Activate(false);
    }

    private void UpdateToggleAnimation()
    {
        float target = activated ? 1f : 0f;

        /*
         * Haxe:
         *
         * var duration = this.dts.sequences[0].duration;
         * var rate = duration > 0 ? timeState.dt / duration : 1;
         *
         * Unity equivalent:
         *
         * Time.deltaTime / animationDuration
         */

        float duration = GetAnimationDuration();

        float rate =
            duration > 0f
                ? Time.deltaTime / duration
                : 1f;

        if (currentCompletion < target)
        {
            currentCompletion =
                Mathf.Min(
                    target,
                    currentCompletion + rate
                );
        }
        else if (currentCompletion > target)
        {
            currentCompletion =
                Mathf.Max(
                    target,
                    currentCompletion - rate
                );
        }

        ApplyAnimationCompletion(currentCompletion);
    }

    private float GetAnimationDuration()
    {
        if (animator == null)
            return 0f;

        AnimationClip[] clips =
            animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.animationClips
                : null;

        if (clips == null)
            return 0f;

        foreach (AnimationClip clip in clips)
        {
            if (clip == null)
                continue;

            if (clip.name.Equals(
                    pushAnimation,
                    StringComparison.OrdinalIgnoreCase))
            {
                return clip.length;
            }
        }

        return 0f;
    }

    private void ApplyAnimationCompletion(float completion)
    {
        if (animator == null)
            return;

        animator.speed = 0f;

        animator.Play(
            pushAnimation,
            0,
            Mathf.Clamp01(completion)
        );

        animator.Update(0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        TryActivate(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;
    }

    private void TryActivate(GameObject other)
    {
        if (isToggleButton)
        {
            TryToggle(other);
            return;
        }

        TryPush(other);
    }

    private void TryPush(GameObject other)
    {
        if (activated)
            return;

        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        Activate(true);

        TriggerCallback(marble, other);
    }

    private void TryToggle(GameObject other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (Time.time < disabledUntil)
            return;

        disabledUntil = Time.time + 2f;

        activated = !activated;

        PlayClickSound();

        // Check whether this toggle belongs to a puzzle.
        CheckTogglePuzzle();
    }

    private void Activate(bool state)
    {
        if (state)
        {
            if (activated)
                return;

            activated = true;
            activationTime = Time.time;

            SetButtonAnimation(true);
            PlayClickSound();
        }
        else
        {
            if (!activated)
                return;

            activated = false;

            SetButtonAnimation(false);
            PlayClickSound();
        }
    }

    private void SetButtonAnimation(bool pressed)
    {
        if (animator == null)
            return;

        animator.speed = pressed ? 1f : -1f;

        animator.Play(
            pushAnimation,
            0,
            pressed ? 0f : 1f
        );
    }

    private void PlayClickSound()
    {
        if (audioClip == null)
            return;

        GameManager.instance.PlayAudioClip(audioClip);
    }

    private void TriggerCallback(
        Marble marble,
        GameObject colliderObject)
    {
        if (string.IsNullOrWhiteSpace(triggerObject))
            return;

        if (string.IsNullOrWhiteSpace(objectMethod))
            return;

        GameObject target =
            FindMissionObject(triggerObject);

        if (target == null)
        {
            Debug.LogError(
                $"PushButton '{gameObject.name}': "
                + $"triggerObject '{triggerObject}' was not found."
            );

            return;
        }

        string methodName =
            GetMethodName(objectMethod);

        if (string.IsNullOrWhiteSpace(methodName))
            return;

        Component[] components =
            target.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            MethodInfo method =
                FindMethod(
                    component.GetType(),
                    methodName
                );

            if (method == null)
                continue;

            InvokeMethod(
                component,
                method,
                marble,
                colliderObject
            );

            return;
        }

        Debug.LogError(
            $"PushButton '{gameObject.name}': "
            + $"method '{methodName}' was not found on any component "
            + $"attached to triggerObject '{target.name}'."
        );
    }

    private MethodInfo FindMethod(
        System.Type type,
        string methodName)
    {
        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.IgnoreCase;

        MethodInfo method =
            type.GetMethod(
                methodName,
                flags,
                null,
                new System.Type[]
                {
                    typeof(Marble),
                    typeof(Collider)
                },
                null
            );

        if (method != null)
            return method;

        method =
            type.GetMethod(
                methodName,
                flags,
                null,
                new System.Type[]
                {
                    typeof(Marble)
                },
                null
            );

        if (method != null)
            return method;

        return type.GetMethod(
            methodName,
            flags,
            null,
            System.Type.EmptyTypes,
            null
        );
    }

    private void InvokeMethod(
        Component component,
        MethodInfo method,
        Marble marble,
        GameObject colliderObject)
    {
        try
        {
            ParameterInfo[] parameters =
                method.GetParameters();

            if (parameters.Length == 2)
            {
                Collider collider =
                    colliderObject != null
                        ? colliderObject.GetComponent<Collider>()
                        : null;

                method.Invoke(
                    component,
                    new object[]
                    {
                        marble,
                        collider
                    }
                );
            }
            else if (parameters.Length == 1)
            {
                method.Invoke(
                    component,
                    new object[]
                    {
                        marble
                    }
                );
            }
            else
            {
                method.Invoke(
                    component,
                    null
                );
            }
        }
        catch (TargetInvocationException exception)
        {
            Debug.LogException(
                exception.InnerException ?? exception,
                component
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception,
                component
            );
        }
    }

    private string GetMethodName(string method)
    {
        method = method.Trim();

        int parenthesisIndex =
            method.IndexOf('(');

        if (parenthesisIndex >= 0)
        {
            method =
                method.Substring(
                    0,
                    parenthesisIndex
                );
        }

        method = method.Trim();

        return method;
    }

    private GameObject FindMissionObject(
        string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject target =
            GameObject.Find(objectName);

        if (target != null)
            return target;

        GameObject[] allObjects =
            FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (GameObject obj in allObjects)
        {
            if (string.Equals(
                    obj.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return obj;
            }
        }

        return null;
    }

    /// <summary>
    /// Check whether this button matches one of the
    /// toggle puzzle's expected solutions.
    /// </summary>
    public bool MatchesSolution(int solutionIndex)
    {
        if (!isToggleButton)
            return false;

        int expected =
            solutionIndex switch
            {
                0 => correctState,
                1 => correctState1,
                _ => -1
            };

        if (expected < 0)
            return false;

        return (activated ? 1 : 0) == expected;
    }

    /// <summary>
    /// Checks the other toggle buttons in the same
    /// parent group for a matching solution.
    /// </summary>
    private void CheckTogglePuzzle()
    {
        if (!isToggleButton)
            return;

        // This button isn't part of a puzzle if neither
        // solution field was supplied by the mission.
        if (correctState < 0 && correctState1 < 0)
            return;

        Transform parent = transform.parent;

        if (parent == null)
            return;

        PushButton[] buttons =
            parent.GetComponentsInChildren<PushButton>(true);

        if (buttons.Length == 0)
            return;

        bool solution0 = true;
        bool solution1 = true;

        bool foundPuzzleButton = false;

        foreach (PushButton button in buttons)
        {
            if (!button.isToggleButton)
                continue;

            if (button.correctState < 0 &&
                button.correctState1 < 0)
            {
                continue;
            }

            foundPuzzleButton = true;

            if (!button.MatchesSolution(0))
                solution0 = false;

            if (!button.MatchesSolution(1))
                solution1 = false;
        }

        if (!foundPuzzleButton)
            return;

        if (solution0)
        {
            SolveTogglePuzzle(0);
        }
        else if (solution1)
        {
            SolveTogglePuzzle(1);
        }
    }

    /// <summary>
    /// Called when all buttons match one of the
    /// configured puzzle solutions.
    /// </summary>
    private void SolveTogglePuzzle(int solutionIndex)
    {
        Debug.Log(
            $"Toggle puzzle solved with solution {solutionIndex}."
        );

        // The actual target PathedInterior / SFX handling
        // should be supplied by the parent SimGroup importer.
    }

    /// <summary>
    /// Called when the level is restarted.
    /// </summary>
    public void OnMissionReset()
    {
        if (isToggleButton)
        {
            activated = initialState;

            currentCompletion =
                initialState ? 1f : 0f;

            disabledUntil =
                -Mathf.Infinity;

            activationTime =
                -Mathf.Infinity;

            ApplyAnimationCompletion(
                currentCompletion
            );

            return;
        }

        activated = false;

        activationTime =
            -Mathf.Infinity;

        ResetAnimation();
    }

    private void ResetAnimation()
    {
        if (animator == null)
            return;

        animator.speed = 1f;

        animator.Play(
            pushAnimation,
            0,
            0f
        );

        animator.Update(0f);
    }

    public bool IsActivated()
    {
        return activated;
    }

    public void Press()
    {
        if (isToggleButton)
        {
            activated = true;

            CheckTogglePuzzle();

            return;
        }

        if (activated)
            return;

        Activate(true);
    }

    public void Release()
    {
        if (isToggleButton)
        {
            activated = false;

            CheckTogglePuzzle();

            return;
        }

        if (!activated)
            return;

        Activate(false);
    }
}