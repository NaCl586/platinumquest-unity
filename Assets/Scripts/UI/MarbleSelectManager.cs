using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarbleSelectManager : MonoBehaviour
{
    public Button next;
    public Button prev;
    public Button select;
    public Button nextCategory;
    public Button prevCategory;
    public TextMeshProUGUI categoryNameText;
    public TextMeshProUGUI marbleNameText;

    [Header("Abstract")]
    public GameObject[] abstractMarblePreview;

    [Header("Community Selected")]
    public GameObject[] communitySelectedMarblePreview;

    [Header("Custom")]
    public GameObject[] customMarblePreview;

    [Header("CyberFox")]
    public GameObject[] cyberFoxMarblePreview;

    [Header("Fruit")]
    public GameObject[] fruitMarblePreview;

    [Header("Fubar")]
    public GameObject[] fubarMarblePreview;

    [Header("Material Effects")]
    public GameObject[] materialEffectsMarblePreview;

    [Header("Platinum")]
    public GameObject[] platinumMarblePreview;

    [Header("PQ Classic")]
    public GameObject[] pqClassicMarblePreview;

    [Header("Solid Colors")]
    public GameObject[] solidColorsMarblePreview;

    [Header("Sports")]
    public GameObject[] sportsMarblePreview;

    [Header("Ultra")]
    public GameObject[] ultraMarblePreview;

    private MarbleType marbleType;

    private int selectedIndexAbstract;
    private int selectedIndexCommunitySelected;
    private int selectedIndexCustom;
    private int selectedIndexCyberFox;
    private int selectedIndexFruit;
    private int selectedIndexFubar;
    private int selectedIndexMaterialEffects;
    private int selectedIndexPlatinum;
    private int selectedIndexPQClassic;
    private int selectedIndexSolidColors;
    private int selectedIndexSports;
    private int selectedIndexUltra;

    private void Start()
    {
        LoadSelectedCategory();
        LoadSelectedIndices();

        next.onClick.AddListener(Next);
        prev.onClick.AddListener(Prev);
        select.onClick.AddListener(CloseMarbleSelect);

        nextCategory.onClick.AddListener(NextCategory);
        prevCategory.onClick.AddListener(PrevCategory);

        SelectMarble();
    }

    private void LoadSelectedCategory()
    {
        int savedCategory = PlayerPrefs.GetInt(
            "SelectedMarbleCategory",
            (int)MarbleType.PQClassic
        );

        if (Enum.IsDefined(typeof(MarbleType), savedCategory))
        {
            marbleType = (MarbleType)savedCategory;
        }
        else
        {
            marbleType = MarbleType.PQClassic;
        }
    }

    private void LoadSelectedIndices()
    {
        selectedIndexAbstract = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_Abstract",
            0
        );

        selectedIndexCommunitySelected = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_CommunitySelected",
            0
        );

        selectedIndexCustom = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_Custom",
            0
        );

        selectedIndexCyberFox = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_CyberFox",
            0
        );

        selectedIndexFruit = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_Fruit",
            0
        );

        selectedIndexFubar = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_Fubar",
            0
        );

        selectedIndexMaterialEffects = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_MaterialEffects",
            0
        );

        selectedIndexPlatinum = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_Platinum",
            0
        );

        selectedIndexPQClassic = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_PQClassic",
            0
        );

        selectedIndexSolidColors = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_SolidColors",
            0
        );

        selectedIndexSports = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_Sports",
            0
        );

        selectedIndexUltra = PlayerPrefs.GetInt(
            "SelectedMarbleIndex_Ultra",
            0
        );

        ValidateAllIndices();
    }

    private void ValidateAllIndices()
    {
        selectedIndexAbstract = ValidateIndex(
            selectedIndexAbstract,
            abstractMarblePreview
        );

        selectedIndexCommunitySelected = ValidateIndex(
            selectedIndexCommunitySelected,
            communitySelectedMarblePreview
        );

        selectedIndexCustom = ValidateIndex(
            selectedIndexCustom,
            customMarblePreview
        );

        selectedIndexCyberFox = ValidateIndex(
            selectedIndexCyberFox,
            cyberFoxMarblePreview
        );

        selectedIndexFruit = ValidateIndex(
            selectedIndexFruit,
            fruitMarblePreview
        );

        selectedIndexFubar = ValidateIndex(
            selectedIndexFubar,
            fubarMarblePreview
        );

        selectedIndexMaterialEffects = ValidateIndex(
            selectedIndexMaterialEffects,
            materialEffectsMarblePreview
        );

        selectedIndexPlatinum = ValidateIndex(
            selectedIndexPlatinum,
            platinumMarblePreview
        );

        selectedIndexPQClassic = ValidateIndex(
            selectedIndexPQClassic,
            pqClassicMarblePreview
        );

        selectedIndexSolidColors = ValidateIndex(
            selectedIndexSolidColors,
            solidColorsMarblePreview
        );

        selectedIndexSports = ValidateIndex(
            selectedIndexSports,
            sportsMarblePreview
        );

        selectedIndexUltra = ValidateIndex(
            selectedIndexUltra,
            ultraMarblePreview
        );
    }

    private int ValidateIndex(
        int index,
        GameObject[] previewArray)
    {
        if (previewArray == null ||
            previewArray.Length == 0)
        {
            return 0;
        }

        if (index < 0 ||
            index >= previewArray.Length)
        {
            return 0;
        }

        return index;
    }

    public void CloseMarbleSelect()
    {
        SaveSelectedCategory();
        SaveSelectedIndex();

        PlayerPrefs.Save();

        PlayMissionManager playMissionManager =
            GetComponent<PlayMissionManager>();

        playMissionManager.raycastBlocker.SetActive(false);
        playMissionManager.ToggleMarbleSelectWindow(false);
    }

    private void SaveSelectedCategory()
    {
        PlayerPrefs.SetInt(
            "SelectedMarbleCategory",
            (int)marbleType
        );
    }

    private void SaveSelectedIndex()
    {
        switch (marbleType)
        {
            case MarbleType.Abstract:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_Abstract",
                    selectedIndexAbstract
                );
                break;

            case MarbleType.CommunitySelected:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_CommunitySelected",
                    selectedIndexCommunitySelected
                );
                break;

            case MarbleType.Custom:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_Custom",
                    selectedIndexCustom
                );
                break;

            case MarbleType.CyberFox:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_CyberFox",
                    selectedIndexCyberFox
                );
                break;

            case MarbleType.Fruit:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_Fruit",
                    selectedIndexFruit
                );
                break;

            case MarbleType.Fubar:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_Fubar",
                    selectedIndexFubar
                );
                break;

            case MarbleType.MaterialEffects:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_MaterialEffects",
                    selectedIndexMaterialEffects
                );
                break;

            case MarbleType.Platinum:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_Platinum",
                    selectedIndexPlatinum
                );
                break;

            case MarbleType.PQClassic:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_PQClassic",
                    selectedIndexPQClassic
                );
                break;

            case MarbleType.SolidColors:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_SolidColors",
                    selectedIndexSolidColors
                );
                break;

            case MarbleType.Sports:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_Sports",
                    selectedIndexSports
                );
                break;

            case MarbleType.Ultra:
                PlayerPrefs.SetInt(
                    "SelectedMarbleIndex_Ultra",
                    selectedIndexUltra
                );
                break;
        }
    }

    public void SelectMarble()
    {
        DisableAllPreviews();

        GameObject[] previews = GetCurrentPreviewArray();
        int index = GetCurrentIndex();

        categoryNameText.text = GetCategoryName();

        if (previews == null ||
            previews.Length == 0)
        {
            marbleNameText.text = "";
            return;
        }

        index = Mathf.Clamp(
            index,
            0,
            previews.Length - 1
        );

        SetCurrentIndex(index);

        GameObject selectedPreview = previews[index];

        if (selectedPreview == null)
        {
            marbleNameText.text = "";
            return;
        }

        marbleNameText.text = selectedPreview.name;
        selectedPreview.SetActive(true);
    }

    private void DisableAllPreviews()
    {
        DisablePreviews(abstractMarblePreview);
        DisablePreviews(communitySelectedMarblePreview);
        DisablePreviews(customMarblePreview);
        DisablePreviews(cyberFoxMarblePreview);
        DisablePreviews(fruitMarblePreview);
        DisablePreviews(fubarMarblePreview);
        DisablePreviews(materialEffectsMarblePreview);
        DisablePreviews(platinumMarblePreview);
        DisablePreviews(pqClassicMarblePreview);
        DisablePreviews(solidColorsMarblePreview);
        DisablePreviews(sportsMarblePreview);
        DisablePreviews(ultraMarblePreview);
    }

    private void DisablePreviews(GameObject[] previews)
    {
        if (previews == null)
        {
            return;
        }

        foreach (GameObject marble in previews)
        {
            if (marble != null)
            {
                marble.SetActive(false);
            }
        }
    }

    public void Next()
    {
        GameObject[] previews = GetCurrentPreviewArray();

        if (previews == null ||
            previews.Length == 0)
        {
            return;
        }

        int index = GetCurrentIndex();

        index++;

        if (index >= previews.Length)
        {
            index = 0;
        }

        SetCurrentIndex(index);
        SaveSelectedIndex();

        SelectMarble();
    }

    public void Prev()
    {
        GameObject[] previews = GetCurrentPreviewArray();

        if (previews == null ||
            previews.Length == 0)
        {
            return;
        }

        int index = GetCurrentIndex();

        index--;

        if (index < 0)
        {
            index = previews.Length - 1;
        }

        SetCurrentIndex(index);
        SaveSelectedIndex();

        SelectMarble();
    }

    public void NextCategory()
    {
        int category = (int)marbleType;

        category++;

        if (category >= Enum.GetValues(typeof(MarbleType)).Length)
        {
            category = 0;
        }

        marbleType = (MarbleType)category;

        SaveSelectedCategory();
        PlayerPrefs.Save();

        SelectMarble();
    }

    public void PrevCategory()
    {
        int category = (int)marbleType;

        category--;

        if (category < 0)
        {
            category = Enum.GetValues(typeof(MarbleType)).Length - 1;
        }

        marbleType = (MarbleType)category;

        SaveSelectedCategory();
        PlayerPrefs.Save();

        SelectMarble();
    }

    private GameObject[] GetCurrentPreviewArray()
    {
        switch (marbleType)
        {
            case MarbleType.Abstract:
                return abstractMarblePreview;

            case MarbleType.CommunitySelected:
                return communitySelectedMarblePreview;

            case MarbleType.Custom:
                return customMarblePreview;

            case MarbleType.CyberFox:
                return cyberFoxMarblePreview;

            case MarbleType.Fruit:
                return fruitMarblePreview;

            case MarbleType.Fubar:
                return fubarMarblePreview;

            case MarbleType.MaterialEffects:
                return materialEffectsMarblePreview;

            case MarbleType.Platinum:
                return platinumMarblePreview;

            case MarbleType.PQClassic:
                return pqClassicMarblePreview;

            case MarbleType.SolidColors:
                return solidColorsMarblePreview;

            case MarbleType.Sports:
                return sportsMarblePreview;

            case MarbleType.Ultra:
                return ultraMarblePreview;

            default:
                return null;
        }
    }

    private int GetCurrentIndex()
    {
        switch (marbleType)
        {
            case MarbleType.Abstract:
                return selectedIndexAbstract;

            case MarbleType.CommunitySelected:
                return selectedIndexCommunitySelected;

            case MarbleType.Custom:
                return selectedIndexCustom;

            case MarbleType.CyberFox:
                return selectedIndexCyberFox;

            case MarbleType.Fruit:
                return selectedIndexFruit;

            case MarbleType.Fubar:
                return selectedIndexFubar;

            case MarbleType.MaterialEffects:
                return selectedIndexMaterialEffects;

            case MarbleType.Platinum:
                return selectedIndexPlatinum;

            case MarbleType.PQClassic:
                return selectedIndexPQClassic;

            case MarbleType.SolidColors:
                return selectedIndexSolidColors;

            case MarbleType.Sports:
                return selectedIndexSports;

            case MarbleType.Ultra:
                return selectedIndexUltra;

            default:
                return 0;
        }
    }

    private void SetCurrentIndex(int index)
    {
        switch (marbleType)
        {
            case MarbleType.Abstract:
                selectedIndexAbstract = index;
                break;

            case MarbleType.CommunitySelected:
                selectedIndexCommunitySelected = index;
                break;

            case MarbleType.Custom:
                selectedIndexCustom = index;
                break;

            case MarbleType.CyberFox:
                selectedIndexCyberFox = index;
                break;

            case MarbleType.Fruit:
                selectedIndexFruit = index;
                break;

            case MarbleType.Fubar:
                selectedIndexFubar = index;
                break;

            case MarbleType.MaterialEffects:
                selectedIndexMaterialEffects = index;
                break;

            case MarbleType.Platinum:
                selectedIndexPlatinum = index;
                break;

            case MarbleType.PQClassic:
                selectedIndexPQClassic = index;
                break;

            case MarbleType.SolidColors:
                selectedIndexSolidColors = index;
                break;

            case MarbleType.Sports:
                selectedIndexSports = index;
                break;

            case MarbleType.Ultra:
                selectedIndexUltra = index;
                break;
        }
    }

    private string GetCategoryName()
    {
        switch (marbleType)
        {
            case MarbleType.Abstract:
                return "Abstract";

            case MarbleType.CommunitySelected:
                return "Community Selected";

            case MarbleType.Custom:
                return "Custom";

            case MarbleType.CyberFox:
                return "CyberFox";

            case MarbleType.Fruit:
                return "Fruit";

            case MarbleType.Fubar:
                return "Fubar";

            case MarbleType.MaterialEffects:
                return "Material Effects";

            case MarbleType.Platinum:
                return "Platinum";

            case MarbleType.PQClassic:
                return "PQ Classic";

            case MarbleType.SolidColors:
                return "Solid Colors";

            case MarbleType.Sports:
                return "Sports";

            case MarbleType.Ultra:
                return "Ultra";

            default:
                return "";
        }
    }

    public MarbleType GetSelectedMarbleType()
    {
        return marbleType;
    }

    public int GetSelectedMarbleIndex()
    {
        return GetCurrentIndex();
    }
}