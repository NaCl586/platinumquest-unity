using UnityEngine;

public class GemChangeTrigger : MonoBehaviour
{
    [Header("Gem Change")]
    [Tooltip("How many gems to add or subtract.")]
    public int gemBonus = 0;

    [Header("Message")]
    public Color positiveMessageColor = new Color(0.6f, 1f, 0.6f);
    public Color neutralMessageColor = new Color(0.8f, 0.8f, 0.8f);
    public Color negativeMessageColor = new Color(1f, 0.6f, 0.6f);

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (marble != Marble.instance)
            return;

        ApplyGemChange();
    }

    private void ApplyGemChange()
    {
        if (GameManager.instance == null)
            return;

        int oldGems = GameManager.instance.currentGems;

        // Match TorqueScript:
        //
        // If subtracting would make the gem count negative,
        // clamp it to zero.
        int newGems = oldGems + gemBonus;

        if (newGems < 0)
            newGems = 0;

        GameManager.instance.currentGems = newGems;

        GameUIManager.instance.SetCurrentGem(newGems);

        DisplayGemMessage();
    }

    private void DisplayGemMessage()
    {
        string sign = gemBonus > 0 ? "+" : "";
        string message = sign + gemBonus;

        Color color;

        if (gemBonus > 0)
            color = positiveMessageColor;
        else if (gemBonus < 0)
            color = negativeMessageColor;
        else
            color = neutralMessageColor;

        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.DisplayGemMessage(
                message,
                color
            );
        }
    }
}