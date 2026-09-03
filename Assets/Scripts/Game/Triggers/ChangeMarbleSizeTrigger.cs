using UnityEngine;

public class ChangeMarbleSizeTrigger : MonoBehaviour
{
    [Header("Marble Size")]
    public float marbleSize = 0.2f;

    [Header("Options")]
    public bool suppressIndicator;

    private void OnTriggerEnter(Collider other)
    {
        Movement movement = other.GetComponent<Movement>();

        if (movement == null)
            return;

        ChangeMarbleSize(movement);
    }

    private void ChangeMarbleSize(Movement movement)
    {
        float newRadius = marbleSize;

        if (float.IsNaN(newRadius))
            newRadius = 0.2f;

        newRadius = Mathf.Max(0.001f, newRadius);

        float oldRadius = movement.marbleRadius;

        if (Mathf.Approximately(newRadius, oldRadius))
            return;

        float scaleFactor = newRadius / oldRadius;

        // ---------------------------------------------------------
        // Visual meshes
        // ---------------------------------------------------------

        Transform marbleRoot = movement.transform;

        Transform regularMesh = marbleRoot.Find("U3DMesh");
        Transform teleportMesh = marbleRoot.Find("TeleportMesh");

        if (regularMesh != null)
            regularMesh.localScale *= scaleFactor;

        if (teleportMesh != null)
            teleportMesh.localScale *= scaleFactor;

        // ---------------------------------------------------------
        // Physics collider
        // ---------------------------------------------------------

        SphereCollider sphereCollider =
            movement.GetComponent<SphereCollider>();

        if (sphereCollider != null)
            sphereCollider.radius = newRadius;

        // Movement uses its own cached radius for collision calculations.
        movement.marbleRadius = newRadius;

        // ---------------------------------------------------------
        // Indicator
        // ---------------------------------------------------------

        if (suppressIndicator)
            return;

        if (movement != Movement.instance)
            return;

        if (Mathf.Approximately(newRadius, 0.2f))
        {
            DisplayAlert("Your marble has returned to normal.");
        }
        else if (newRadius < oldRadius)
        {
            DisplayAlert("Oh dear, your marble has shrunk...");
        }
        else
        {
            DisplayAlert("Oh my, your marble has grown!");
        }
    }

    private void DisplayAlert(string message)
    {
        Debug.Log(message);

        // Hook this into your actual level alert system.
        // For example:
        //
        // GameManager.instance.DisplayAlert(message);
        //
        // once we know which UI method your project uses.
    }
}