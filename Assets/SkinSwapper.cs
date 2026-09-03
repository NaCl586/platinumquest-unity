using System.Collections.Generic;
using UnityEngine;

public class SkinSwapper : MonoBehaviour
{
    [Header("Skin")]
    public string skinName = "base";

    [Header("Skin Textures")]
    public Texture2D[] skins;
    public Texture2D[] normals;

    private Dictionary<string, Texture2D> skinDictionary;
    private Dictionary<string, Texture2D> normalDictionary;

    private Renderer targetRenderer;
    private Material material;

    [SerializeField]
    private int materialIndex = 0;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();

        if (materialIndex > 0)
            material = targetRenderer.materials[materialIndex];
        else
            material = targetRenderer.material;

        skinDictionary = new Dictionary<string, Texture2D>();
        normalDictionary = new Dictionary<string, Texture2D>();

        // Build skin dictionary
        if (skins != null)
        {
            foreach (Texture2D texture in skins)
            {
                if (texture == null)
                    continue;

                string key = GetTextureKey(texture);
                skinDictionary[key] = texture;
            }
        }

        // Build normal dictionary
        if (normals != null)
        {
            foreach (Texture2D texture in normals)
            {
                if (texture == null)
                    continue;

                string key = GetTextureKey(texture);
                normalDictionary[key] = texture;
            }
        }
    }

    private void Start()
    {
        ApplySkin();
    }

    private string GetTextureKey(Texture2D texture)
    {
        string textureName = texture.name;

        int dotIndex = textureName.IndexOf('.');

        if (dotIndex >= 0)
            textureName = textureName.Substring(0, dotIndex);

        return textureName.ToLower();
    }

    public void ApplySkin()
    {
        if (string.IsNullOrEmpty(skinName))
            skinName = "base";

        string key = skinName.ToLower();

        if (!skinDictionary.TryGetValue(key, out Texture2D texture))
        {
            Debug.LogWarning(
                $"Skin '{skinName}' not found on {gameObject.name}.",
                gameObject
            );

            return;
        }

        // Apply diffuse/albedo texture
        material.mainTexture = texture;

        // Apply matching normal map if one exists
        if (normalDictionary.TryGetValue(key, out Texture2D normalTexture))
        {
            material.SetTexture("_BumpMap", normalTexture);
            material.EnableKeyword("_NORMALMAP");
        }

        // Apply friction settings for skins 21-25
        ApplyFrictionForSkin();
    }

    public void ApplyRandomSkin()
    {
        if (skins == null || skins.Length == 0)
        {
            Debug.LogWarning($"No skins available on {gameObject.name}.");
            return;
        }

        List<Texture2D> validSkins = new List<Texture2D>();

        foreach (Texture2D texture in skins)
        {
            if (texture != null)
                validSkins.Add(texture);
        }

        if (validSkins.Count == 0)
        {
            Debug.LogWarning($"No valid skins available on {gameObject.name}.");
            return;
        }

        Texture2D randomTexture =
            validSkins[Random.Range(0, validSkins.Count)];

        skinName = GetTextureKey(randomTexture);

        // Apply diffuse/albedo texture
        material.mainTexture = randomTexture;

        // Apply matching normal map if one exists
        if (normalDictionary.TryGetValue(skinName, out Texture2D normalTexture))
        {
            material.SetTexture("_BumpMap", normalTexture);
            material.EnableKeyword("_NORMALMAP");
        }

        // Apply friction settings for skins 21-25
        ApplyFrictionForSkin();
    }

    private void ApplyFrictionForSkin()
    {
        // Convert skin name to a number if possible
        if (!int.TryParse(skinName, out int skinNumber))
            return;

        // Only skins 21-25 use FrictionComponent
        if (skinNumber < 21 || skinNumber > 25)
            return;

        // Get or add the FrictionComponent
        FrictionComponent frictionComponent =
            GetComponent<FrictionComponent>();

        if (frictionComponent == null)
            frictionComponent = gameObject.AddComponent<FrictionComponent>();

        switch (skinNumber)
        {
            case 21: // space
                frictionComponent.friction = 0.01f;
                frictionComponent.restitution = 0.35f;
                frictionComponent.bounce = 0f;
                break;

            case 22: // ice
                frictionComponent.friction = 0.07331f;
                frictionComponent.restitution = 0.75f;
                frictionComponent.bounce = 0f;
                break;

            case 23: // mud
                frictionComponent.friction = 0.30f;
                frictionComponent.restitution = 0.5f;
                frictionComponent.bounce = 0f;
                break;

            case 24: // grass
                frictionComponent.friction = 2.0f;
                frictionComponent.restitution = 0.5f;
                frictionComponent.bounce = 0f;
                break;

            case 25: // sand
                frictionComponent.friction = 4.0f;
                frictionComponent.restitution = 0.15f;
                frictionComponent.bounce = 0f;
                break;
        }
    }
}
