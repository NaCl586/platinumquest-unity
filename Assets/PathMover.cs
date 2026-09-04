using UnityEngine;

public class PathMover : MonoBehaviour
{
    public GameObjectPathFollower pathFollower;

    private PathManager pathManager;
    private string firstNodeName;

    // True if this path is controlled by a PathTrigger.
    private bool triggerControlled;

    // Whether this mover is currently allowed to be updated.
    private bool pathActive;

    public Vector3 LinearVelocity =>
        pathFollower != null ? pathFollower.LinearVelocity : Vector3.zero;

    public Vector3 AngularVelocity =>
        pathFollower != null ? pathFollower.AngularVelocity : Vector3.zero;

    public Vector3 PreviousPosition =>
        pathFollower != null ? pathFollower.PreviousPosition : transform.position;

    public Quaternion PreviousRotation =>
        pathFollower != null ? pathFollower.PreviousRotation : transform.rotation;

    public Vector3 CurrentPosition =>
        pathFollower != null ? pathFollower.CurrentPosition : transform.position;

    public Quaternion CurrentRotation =>
        pathFollower != null ? pathFollower.CurrentRotation : transform.rotation;

    public bool HasPath => pathFollower != null && pathFollower.HasPath() && pathActive;

    public string CurrentNode => pathFollower != null ? pathFollower.CurrentNodeName : null;

    public string NextNode => pathFollower != null ? pathFollower.NextNodeName : null;

    public void InitializePath(
    string nodeName,
    PathManager manager,
    bool fromField = true,
    Vector3? initialPathPosition = null,
    Quaternion? initialPathRotation = null,
    Vector3? initialPathScale = null
)
    {
        if (manager == null)
        {
            Debug.LogWarning(
                $"PathMover on '{name}': PathManager is null.",
                this
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(nodeName))
        {
            Debug.LogWarning(
                $"PathMover on '{name}': Path name is empty.",
                this
            );

            return;
        }

        if (!manager.TryGetNode(nodeName, out PathNode node))
        {
            Debug.LogWarning(
                $"PathMover on '{name}': " +
                $"Could not find path node '{nodeName}'.",
                this
            );

            return;
        }

        pathManager = manager;

        firstNodeName = node.nodeName.ToLowerInvariant();

        // fromField == false means this path was
        // activated by a PathTrigger.
        triggerControlled = !fromField;

        pathFollower = new GameObjectPathFollower(
            gameObject,
            firstNodeName,
            pathManager,
            initialPathPosition,
            initialPathRotation,
            initialPathScale
        );

        pathActive = true;
    }

    public void ComputeNextPathStep(float deltaTime)
    {
        if (!pathActive)
            return;

        if (pathFollower == null)
            return;

        pathFollower.ComputeNextPathStep(deltaTime);
    }

    public void AdvancePath(float timeStep)
    {
        if (!pathActive)
            return;

        if (pathFollower == null)
            return;

        pathFollower.AdvancePath(timeStep);
    }

    public void ResetMover()
    {
        if (pathFollower == null)
            return;

        /*
         * Normal paths:
         * Keep running after restart.
         *
         * PathTrigger paths:
         * Stop completely until the trigger fires again.
         */
        if (triggerControlled)
        {
            pathActive = false;

            pathFollower.DeactivatePath();
        }
        else
        {
            pathActive = true;

            pathFollower.ResetPath();
        }
    }

    public void DeactivatePath()
    {
        pathActive = false;

        if (pathFollower == null)
            return;

        pathFollower.DeactivatePath();
    }

    public void FillPathState(PathFollowerSaveState state)
    {
        if (pathFollower == null)
            return;

        pathFollower.FillState(state);
    }

    public void SetPathState(PathFollowerSaveState state)
    {
        if (pathFollower == null)
            return;

        pathFollower.SetState(state);
    }
}
