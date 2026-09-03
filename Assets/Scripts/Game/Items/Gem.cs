using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GemType
{
    Base,
    Black,
    Blue,
    Platinum,
    Purple,
    Red,
    Orange,
    Green,
    Turquoise,
    Yellow,
    Pink
}

public class Gem : MonoBehaviour
{
    [SerializeField]
    private Texture[] gemColors;

    [SerializeField]
    private MeshRenderer mrTop;

    [SerializeField]
    private MeshRenderer mrBottom;

    [SerializeField]
    private AudioClip pickupSound;

    [SerializeField]
    private AudioClip pickupLastGem;

    // Actual type of gem selected from the texture.
    public GemType gemType { get; private set; }

    // Average visual color of the selected texture.
    public Color gemColor;

    // The index of the texture selected for this gem.
    // The radar uses this to select the corresponding GemItem*.png.
    public int gemColorIndex { get; private set; }

    public GameObject beam;

    public void SetGemColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            // No color specified -> choose a random gem color.
            gemColorIndex = Random.Range(0, gemColors.Length);
        }
        else
        {
            string colorName = color.Trim().ToLowerInvariant();

            gemColorIndex = -1;

            for (int i = 0; i < gemColors.Length; i++)
            {
                if (gemColors[i] == null)
                    continue;

                string textureName =
                    gemColors[i].name.ToLowerInvariant();

                if (textureName == colorName ||
                    textureName == colorName + ".gem")
                {
                    gemColorIndex = i;
                    break;
                }
            }

            if (gemColorIndex == -1)
            {
                Debug.LogWarning(
                    $"Gem '{name}' could not find a gem texture " +
                    $"for color '{color}'."
                );

                gemColorIndex =
                    Random.Range(0, gemColors.Length);
            }
        }

        Texture selectedTexture =
            gemColors[gemColorIndex];

        // Store the actual gem type.
        SetGemType(selectedTexture);

        if (mrTop != null)
            mrTop.materials[0].mainTexture =
                selectedTexture;

        if (mrBottom != null)
            mrBottom.materials[0].mainTexture =
                selectedTexture;

        Texture2D tex2D =
            selectedTexture as Texture2D;

        if (tex2D != null)
        {
            gemColor = GetAverageColor(tex2D);
            gemColor.a = 1f;
        }
        else
        {
            gemColor = Color.white;
        }

        if(GameManager.instance.GetGameMode<HuntMode>() != null)
        {
            beam.SetActive(true);
            for(int i = 0; i < beam.transform.childCount; i++)
            {
                var skinswapper = beam.transform.GetChild(i).GetComponent<SkinSwapper>();
                skinswapper.skinName = color;
                skinswapper.ApplySkin();
            }
        }
        else
        {
            beam.SetActive(false);
        }
    }

    private void SetGemType(Texture texture)
    {
        if (texture == null)
        {
            gemType = GemType.Base;
            return;
        }

        string textureName =
            texture.name.ToLowerInvariant();

        // Remove ".gem" if present.
        if (textureName.EndsWith(".gem"))
        {
            textureName = textureName.Substring(
                0,
                textureName.Length - 4
            );
        }

        switch (textureName)
        {
            case "base":
                gemType = GemType.Base;
                break;

            case "black":
                gemType = GemType.Black;
                break;

            case "blue":
                gemType = GemType.Blue;
                break;

            case "platinum":
                gemType = GemType.Platinum;
                break;

            case "purple":
                gemType = GemType.Purple;
                break;

            case "red":
                gemType = GemType.Red;
                break;

            case "orange":
                gemType = GemType.Orange;
                break;

            case "green":
                gemType = GemType.Green;
                break;

            case "turquoise":
                gemType = GemType.Turquoise;
                break;

            case "yellow":
                gemType = GemType.Yellow;
                break;

            case "pink":
                gemType = GemType.Pink;
                break;

            default:
                Debug.LogWarning(
                    $"Gem '{name}' has unknown gem texture " +
                    $"'{texture.name}'. Defaulting to Base."
                );

                gemType = GemType.Base;
                break;
        }
    }

    public Color GetAverageColor(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();

        Vector3 total = Vector3.zero;
        int count = 0;

        foreach (Color pixel in pixels)
        {
            if (pixel.a < 0.1f)
                continue;

            total += new Vector3(
                pixel.r,
                pixel.g,
                pixel.b
            );

            count++;
        }

        if (count == 0)
            return Color.white;

        return new Color(
            total.x / count,
            total.y / count,
            total.z / count
        );
    }

    private void FixedUpdate()
    {
        Transform mesh = transform.Find("Mesh");

        if (mesh == null)
            return;

        Quaternion rot = mesh.rotation;

        mesh.rotation =
            Quaternion.AngleAxis(
                Time.fixedDeltaTime * 120f,
                rot * Vector3.up
            ) * rot;
    }

    public void PickupItem()
    {
        int newGemCount =
            GameManager.instance.CurrentGems + 1;

        GameManager.onCollectGem?.Invoke(
            newGemCount
        );

        bool shouldPlayLastGemSound = true;

        foreach (IGameMode mode in
                 GameManager.instance.GameModes)
        {
            if (!mode.ShouldPlayCollectAllGemsSound(
                    newGemCount))
            {
                shouldPlayLastGemSound = false;
                break;
            }
        }

        if (shouldPlayLastGemSound)
        {
            GameManager.instance.PlayAudioClip(
                pickupLastGem
            );
        }
        else
        {
            GameManager.instance.PlayAudioClip(
                pickupSound
            );
        }

        GameManager.instance.recentGems.Add(
            gameObject
        );

        GameManager.instance.OnGemCollected(
            this
        );

        gameObject.SetActive(false);
    }
}