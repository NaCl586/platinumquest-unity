using System;
using UnityEngine;

public enum MarbleType
{
    Abstract,
    CommunitySelected,
    Custom,
    CyberFox,
    Fruit,
    Fubar,
    MaterialEffects,
    Platinum,
    PQClassic,
    SolidColors,
    Sports,
    Ultra
}

public class MarbleInfo : MonoBehaviour
{
    public static MarbleInfo instance;

    [Header("Abstract")]
    public GameObject[] abstractMarble;

    [Header("Community Selected")]
    public GameObject[] communitySelectedMarble;

    [Header("Custom")]
    public GameObject[] customMarble;

    [Header("CyberFox")]
    public GameObject[] cyberFoxMarble;

    [Header("Fruit")]
    public GameObject[] fruitMarble;

    [Header("Fubar")]
    public GameObject[] fubarMarble;

    [Header("Material Effects")]
    public GameObject[] materialEffectsMarble;

    [Header("Platinum")]
    public GameObject[] platinumMarble;

    [Header("PQ Classic")]
    public GameObject[] pqClassicMarble;

    [Header("Solid Colors")]
    public GameObject[] solidColorsMarble;

    [Header("Sports")]
    public GameObject[] sportsMarble;

    [Header("Ultra")]
    public GameObject[] ultraMarble;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ApplyMesh();
    }

    public void ApplyMesh()
    {
        MarbleType marbleType = GetSavedMarbleType();
        int selectedMarbleIndex = GetSavedMarbleIndex(marbleType);

        ApplyMesh(marbleType, selectedMarbleIndex);
    }

    public void ApplyMesh(
        MarbleType marbleType,
        int selectedMarbleIndex)
    {
        if (Marble.instance == null)
        {
            Debug.LogError("MarbleInfo: Marble.instance is null.");
            return;
        }

        GameObject[] marbleArray =
            GetMarbleArray(marbleType);

        if (marbleArray == null ||
            marbleArray.Length == 0)
        {
            Debug.LogError(
                $"MarbleInfo: No marble prefabs configured for {marbleType}."
            );

            return;
        }

        if (selectedMarbleIndex < 0 ||
            selectedMarbleIndex >= marbleArray.Length)
        {
            Debug.LogError(
                $"MarbleInfo: Index {selectedMarbleIndex} is out of range " +
                $"for {marbleType}. Available marbles: {marbleArray.Length}"
            );

            return;
        }

        GameObject marblePrefab =
            marbleArray[selectedMarbleIndex];

        if (marblePrefab == null)
        {
            Debug.LogError(
                $"MarbleInfo: Marble prefab at index " +
                $"{selectedMarbleIndex} for {marbleType} is null."
            );

            return;
        }

        Mesh sourceMesh = null;
        Material sourceMaterial = null;

        SkinnedMeshRenderer skinnedRenderer =
            marblePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);

        if (skinnedRenderer != null)
        {
            sourceMesh = skinnedRenderer.sharedMesh;
            sourceMaterial = skinnedRenderer.sharedMaterial;
        }
        else
        {
            Transform ballSuperballMesh = marblePrefab.transform.Find("Sphere/Sphere#2");
            MeshRenderer meshRenderer = null;

            if (ballSuperballMesh)
            {
                meshRenderer = ballSuperballMesh.GetComponent<MeshRenderer>();
            }

            if (meshRenderer == null)
            {
                meshRenderer =
                   marblePrefab.GetComponentInChildren<MeshRenderer>(true);
            }

            if (meshRenderer != null)
            {
                MeshFilter meshFilter =
                    meshRenderer.GetComponent<MeshFilter>();

                if (meshFilter != null)
                {
                    sourceMesh = meshFilter.sharedMesh;
                }

                sourceMaterial = meshRenderer.sharedMaterial;
            }
        }

        if (sourceMesh == null)
        {
            Debug.LogError(
                $"MarbleInfo: Marble prefab '{marblePrefab.name}' " +
                $"does not contain a usable mesh."
            );

            return;
        }

        if (Marble.instance.normalMesh == null)
        {
            Debug.LogError(
                "MarbleInfo: Marble.instance.normalMesh is null."
            );

            return;
        }

        MeshFilter normalFilter =
            Marble.instance.normalMesh.GetComponent<MeshFilter>();

        MeshRenderer normalRenderer =
            Marble.instance.normalMesh.GetComponent<MeshRenderer>();

        if (normalFilter == null)
        {
            Debug.LogError(
                "MarbleInfo: normalMesh does not have a MeshFilter."
            );

            return;
        }

        if (normalRenderer == null)
        {
            Debug.LogError(
                "MarbleInfo: normalMesh does not have a MeshRenderer."
            );

            return;
        }

        normalFilter.sharedMesh = sourceMesh;

        if (sourceMaterial != null)
        {
            normalRenderer.sharedMaterial = sourceMaterial;
        }
        else
        {
            Debug.LogWarning(
                $"MarbleInfo: Marble prefab '{marblePrefab.name}' " +
                $"does not have a material assigned."
            );
        }

        SphereCollider sphereCollider =
            Marble.instance.GetComponent<SphereCollider>();

        if (sphereCollider != null)
        {
            sphereCollider.radius = 0.5f;
        }

        Movement movement =
            Marble.instance.GetComponent<Movement>();

        if (movement != null)
        {
            movement.marbleRadius = 0.2f;
        }
    }

    private MarbleType GetSavedMarbleType()
    {
        int savedCategory =
            PlayerPrefs.GetInt(
                "SelectedMarbleCategory",
                (int)MarbleType.PQClassic
            );

        if (!Enum.IsDefined(
                typeof(MarbleType),
                savedCategory))
        {
            return MarbleType.PQClassic;
        }

        return (MarbleType)savedCategory;
    }

    private int GetSavedMarbleIndex(
        MarbleType marbleType)
    {
        string key =
            $"SelectedMarbleIndex_{marbleType}";

        return PlayerPrefs.GetInt(key, 0);
    }

    private GameObject[] GetMarbleArray(
        MarbleType marbleType)
    {
        switch (marbleType)
        {
            case MarbleType.Abstract:
                return abstractMarble;

            case MarbleType.CommunitySelected:
                return communitySelectedMarble;

            case MarbleType.Custom:
                return customMarble;

            case MarbleType.CyberFox:
                return cyberFoxMarble;

            case MarbleType.Fruit:
                return fruitMarble;

            case MarbleType.Fubar:
                return fubarMarble;

            case MarbleType.MaterialEffects:
                return materialEffectsMarble;

            case MarbleType.Platinum:
                return platinumMarble;

            case MarbleType.PQClassic:
                return pqClassicMarble;

            case MarbleType.SolidColors:
                return solidColorsMarble;

            case MarbleType.Sports:
                return sportsMarble;

            case MarbleType.Ultra:
                return ultraMarble;

            default:
                return null;
        }
    }

    public void ApplyReplayMarble(
        string marbleID)
    {
        if (string.IsNullOrWhiteSpace(marbleID))
        {
            Debug.LogError(
                "MarbleInfo: Replay marble ID is empty."
            );

            return;
        }

        marbleID = marbleID.Trim();

        if (marbleID.Length < 3)
        {
            Debug.LogError(
                $"MarbleInfo: Invalid marble ID '{marbleID}'."
            );

            return;
        }

        string prefix =
            marbleID.Substring(0, 2)
                .ToUpperInvariant();

        string indexString =
            marbleID.Substring(2);

        if (!int.TryParse(
                indexString,
                out int number))
        {
            Debug.LogError(
                $"MarbleInfo: Invalid marble index in '{marbleID}'."
            );

            return;
        }

        if (number <= 0)
        {
            Debug.LogError(
                $"MarbleInfo: Marble index must be 1 or greater in '{marbleID}'."
            );

            return;
        }

        MarbleType marbleType;

        switch (prefix)
        {
            case "AB":
                marbleType = MarbleType.Abstract;
                break;

            case "CS":
                marbleType = MarbleType.CommunitySelected;
                break;

            case "CU":
                marbleType = MarbleType.Custom;
                break;

            case "CF":
                marbleType = MarbleType.CyberFox;
                break;

            case "FR":
                marbleType = MarbleType.Fruit;
                break;

            case "FB":
                marbleType = MarbleType.Fubar;
                break;

            case "ME":
                marbleType = MarbleType.MaterialEffects;
                break;

            case "PL":
                marbleType = MarbleType.Platinum;
                break;

            case "PQ":
                marbleType = MarbleType.PQClassic;
                break;

            case "SC":
                marbleType = MarbleType.SolidColors;
                break;

            case "SP":
                marbleType = MarbleType.Sports;
                break;

            case "UL":
                marbleType = MarbleType.Ultra;
                break;

            default:
                Debug.LogError(
                    $"MarbleInfo: Unknown marble prefix '{prefix}' " +
                    $"in ID '{marbleID}'."
                );

                return;
        }

        ApplyMesh(
            marbleType,
            number - 1
        );
    }

    public string CreateMarbleID(
        MarbleType marbleType,
        int selectedMarbleIndex)
    {
        string prefix;

        switch (marbleType)
        {
            case MarbleType.Abstract:
                prefix = "AB";
                break;

            case MarbleType.CommunitySelected:
                prefix = "CS";
                break;

            case MarbleType.Custom:
                prefix = "CU";
                break;

            case MarbleType.CyberFox:
                prefix = "CF";
                break;

            case MarbleType.Fruit:
                prefix = "FR";
                break;

            case MarbleType.Fubar:
                prefix = "FB";
                break;

            case MarbleType.MaterialEffects:
                prefix = "ME";
                break;

            case MarbleType.Platinum:
                prefix = "PL";
                break;

            case MarbleType.PQClassic:
                prefix = "PQ";
                break;

            case MarbleType.SolidColors:
                prefix = "SC";
                break;

            case MarbleType.Sports:
                prefix = "SP";
                break;

            case MarbleType.Ultra:
                prefix = "UL";
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(marbleType),
                    marbleType,
                    null
                );
        }

        return $"{prefix}{selectedMarbleIndex + 1}";
    }
}