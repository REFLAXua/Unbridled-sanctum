using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages the Character Creation Menu.
/// Allows customization of Body Size, Hair (front/back), Hair Color, Emotion, Eye Color, and Blush.
/// Includes automatic enhanced UI layout & sizing (larger fonts, wider panel, larger buttons).
/// </summary>
public class CharacterCreatorUI : MonoBehaviour
{
    [Header("Panel Reference")]
    public GameObject creatorPanel;

    [Header("Dependencies")]
    public CharacterState characterState;
    public CharacterAppearanceDatabase database;
    public PaperDollController paperDoll;

    [Header("Category Selectors (Labels + Arrows)")]
    public TextMeshProUGUI bodySizeLabel;
    public Button bodySizePrevBtn;
    public Button bodySizeNextBtn;

    public TextMeshProUGUI hairFrontLabel;
    public Button hairFrontPrevBtn;
    public Button hairFrontNextBtn;

    public TextMeshProUGUI hairBackLabel;
    public Button hairBackPrevBtn;
    public Button hairBackNextBtn;

    public TextMeshProUGUI emotionLabel;
    public Button emotionPrevBtn;
    public Button emotionNextBtn;

    public TextMeshProUGUI blushLabel;
    public Button blushPrevBtn;
    public Button blushNextBtn;

    [Header("Palette Containers")]
    public Transform hairColorPaletteContainer;
    public Transform eyeColorPaletteContainer;

    [Header("Action Buttons")]
    public Button randomButton;
    public Button acceptButton;

    public event Action OnCharacterAccepted;

    private int currentHairFrontIdx = 0;
    private int currentHairBackIdx = 0;
    private int currentEmotionIdx = 0;
    private int currentBlushIdx = 0; // 0 = None, 1 = 1.png, 2 = 2.png

    private void Start()
    {
        ApplyEnhancedLayout();
        BindButtons();
        BuildColorPalettes();
        SyncFromCharacterState();
        UpdateAllLabels();
    }

    /// <summary>
    /// Dynamically enlarges the character creation menu, fonts, arrow buttons, and action buttons.
    /// Works immediately at runtime without requiring any scene rebuild.
    /// </summary>
    public void ApplyEnhancedLayout()
    {
        if (creatorPanel == null) creatorPanel = gameObject;

        // 1. Enlarge the main creator panel
        RectTransform panelRect = creatorPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(740f, -40f);
        }

        // 2. Title ("CreatorTitle" or "Title")
        Transform titleTrans = creatorPanel.transform.Find("CreatorTitle");
        if (titleTrans == null) titleTrans = creatorPanel.transform.Find("Title");
        if (titleTrans != null)
        {
            RectTransform titleRect = titleTrans.GetComponent<RectTransform>();
            if (titleRect != null) titleRect.sizeDelta = new Vector2(0f, 60f);

            TextMeshProUGUI titleText = titleTrans.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.fontSize = 36;
                titleText.fontStyle = FontStyles.Bold;
            }
        }

        // 3. Find content/controls container
        Transform content = creatorPanel.transform.Find("ControlsContainer");
        if (content == null) content = creatorPanel.transform.Find("ScrollArea/Content");
        if (content == null) content = creatorPanel.transform.Find("Content");

        if (content != null)
        {
            RectTransform contRect = content.GetComponent<RectTransform>();
            if (contRect != null) contRect.sizeDelta = new Vector2(-40f, -140f);

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 16;
            }

            // Iterate over all rows in content to enlarge headers and rows
            foreach (Transform row in content)
            {
                RectTransform rowRect = row.GetComponent<RectTransform>();
                if (rowRect != null)
                {
                    rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, 58f);
                }

                // Row header
                Transform headerTrans = row.Find("Header");
                if (headerTrans != null)
                {
                    RectTransform hr = headerTrans.GetComponent<RectTransform>();
                    if (hr != null) hr.sizeDelta = new Vector2(165f, hr.sizeDelta.y);

                    TextMeshProUGUI ht = headerTrans.GetComponent<TextMeshProUGUI>();
                    if (ht != null) ht.fontSize = 26;
                }

                // Palette container
                Transform palTrans = row.Find("PaletteContainer");
                if (palTrans != null)
                {
                    RectTransform pr = palTrans.GetComponent<RectTransform>();
                    if (pr != null) pr.sizeDelta = new Vector2(380f, 50f);
                }
            }
        }

        // 4. Directly enlarge Value Labels (excluding hidden blush)
        TextMeshProUGUI[] valueLabels = { bodySizeLabel, hairFrontLabel, hairBackLabel, emotionLabel };
        foreach (var lbl in valueLabels)
        {
            if (lbl != null)
            {
                lbl.fontSize = 26;
                lbl.fontStyle = FontStyles.Bold;
                RectTransform vr = lbl.GetComponent<RectTransform>();
                if (vr != null) vr.sizeDelta = new Vector2(280f, vr.sizeDelta.y);
            }
        }

        // Hide Blush row (now dynamically driven by combat seduction phases)
        if (blushLabel != null)
        {
            Transform pRow = blushLabel.transform.parent;
            if (pRow != null && pRow != creatorPanel.transform) pRow.gameObject.SetActive(false);
            else blushLabel.gameObject.SetActive(false);
        }
        if (blushPrevBtn != null) blushPrevBtn.gameObject.SetActive(false);
        if (blushNextBtn != null) blushNextBtn.gameObject.SetActive(false);

        // 5. Directly enlarge Arrow Buttons
        Button[] allArrowBtns = { bodySizePrevBtn, bodySizeNextBtn, hairFrontPrevBtn, hairFrontNextBtn, hairBackPrevBtn, hairBackNextBtn, emotionPrevBtn, emotionNextBtn };
        foreach (var btn in allArrowBtns)
        {
            if (btn != null)
            {
                RectTransform ar = btn.GetComponent<RectTransform>();
                if (ar != null) ar.sizeDelta = new Vector2(48f, 48f);

                TextMeshProUGUI at = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (at != null) at.fontSize = 28;
            }
        }

        // 6. Action Buttons Row (Random + Accept)
        Transform btnRow = creatorPanel.transform.Find("ActionButtonsRow");
        if (btnRow != null)
        {
            RectTransform btnRowRect = btnRow.GetComponent<RectTransform>();
            if (btnRowRect != null)
            {
                btnRowRect.sizeDelta = new Vector2(-40f, 66f);
                btnRowRect.anchoredPosition = new Vector2(0f, 22f);
            }

            if (randomButton != null)
            {
                RectTransform rr = randomButton.GetComponent<RectTransform>();
                if (rr != null) rr.sizeDelta = new Vector2(200f, 58f);
                TextMeshProUGUI rt = randomButton.GetComponentInChildren<TextMeshProUGUI>();
                if (rt != null) rt.fontSize = 26;
            }

            if (acceptButton != null)
            {
                RectTransform ar = acceptButton.GetComponent<RectTransform>();
                if (ar != null) ar.sizeDelta = new Vector2(280f, 58f);
                TextMeshProUGUI at = acceptButton.GetComponentInChildren<TextMeshProUGUI>();
                if (at != null) at.fontSize = 26;
            }
        }
    }

    private void BindButtons()
    {
        // Body size
        if (bodySizePrevBtn != null) bodySizePrevBtn.onClick.AddListener(() => CycleBodySize(-1));
        if (bodySizeNextBtn != null) bodySizeNextBtn.onClick.AddListener(() => CycleBodySize(1));

        // Hair Front
        if (hairFrontPrevBtn != null) hairFrontPrevBtn.onClick.AddListener(() => CycleHairFront(-1));
        if (hairFrontNextBtn != null) hairFrontNextBtn.onClick.AddListener(() => CycleHairFront(1));

        // Hair Back
        if (hairBackPrevBtn != null) hairBackPrevBtn.onClick.AddListener(() => CycleHairBack(-1));
        if (hairBackNextBtn != null) hairBackNextBtn.onClick.AddListener(() => CycleHairBack(1));

        // Emotion
        if (emotionPrevBtn != null) emotionPrevBtn.onClick.AddListener(() => CycleEmotion(-1));
        if (emotionNextBtn != null) emotionNextBtn.onClick.AddListener(() => CycleEmotion(1));

        // Blush: Removed from Character Creator (dynamically handled during combat)

        // Randomize
        if (randomButton != null) randomButton.onClick.AddListener(OnRandomizeClicked);

        // Accept
        if (acceptButton != null) acceptButton.onClick.AddListener(OnAcceptClicked);
    }

    public void Show()
    {
        if (creatorPanel != null) creatorPanel.SetActive(true);
        ApplyEnhancedLayout();
        SyncFromCharacterState();
        UpdateAllLabels();
    }

    public void Hide()
    {
        if (creatorPanel != null) creatorPanel.SetActive(false);
    }

    private void SyncFromCharacterState()
    {
        if (characterState == null || database == null) return;

        // Ensure defaults are assigned
        characterState.EnsureDefaultAppearance();

        if (characterState.selectedHairFront != null)
        {
            int idx = database.hairFrontStyles.IndexOf(characterState.selectedHairFront);
            currentHairFrontIdx = Mathf.Max(0, idx);
        }

        if (characterState.selectedHairBack != null)
        {
            int idx = database.hairBackStyles.IndexOf(characterState.selectedHairBack);
            if (idx < 0 && database.hairBackStyles.Count > 0)
            {
                currentHairBackIdx = 0;
                characterState.selectedHairBack = database.hairBackStyles[0];
                characterState.SetHair(characterState.selectedHairFront, characterState.selectedHairBack);
            }
            else
            {
                currentHairBackIdx = Mathf.Max(0, idx);
            }
        }

        if (characterState.selectedExpression != null)
            currentEmotionIdx = Mathf.Max(0, database.emotionStyles.IndexOf(characterState.selectedExpression));

        characterState.SetBlush(null);
        currentBlushIdx = 0;
    }

    // --- Cycling methods ---

    private void CycleBodySize(int delta)
    {
        int count = 4;
        int current = (int)characterState.CurrentBodySize;
        current = (current + delta + count) % count;
        characterState.CurrentBodySize = (BodySizeType)current;
        UpdateBodyLabel();
    }

    private void CycleHairFront(int delta)
    {
        if (database.hairFrontStyles.Count == 0) return;
        currentHairFrontIdx = (currentHairFrontIdx + delta + database.hairFrontStyles.Count) % database.hairFrontStyles.Count;
        characterState.selectedHairFront = database.hairFrontStyles[currentHairFrontIdx];
        characterState.SetHair(characterState.selectedHairFront, characterState.selectedHairBack);
        UpdateHairFrontLabel();
    }

    private void CycleHairBack(int delta)
    {
        if (database.hairBackStyles.Count == 0) return;
        currentHairBackIdx = (currentHairBackIdx + delta + database.hairBackStyles.Count) % database.hairBackStyles.Count;
        characterState.selectedHairBack = database.hairBackStyles[currentHairBackIdx];
        characterState.SetHair(characterState.selectedHairFront, characterState.selectedHairBack);
        UpdateHairBackLabel();
    }

    private void CycleEmotion(int delta)
    {
        if (database.emotionStyles.Count == 0) return;
        currentEmotionIdx = (currentEmotionIdx + delta + database.emotionStyles.Count) % database.emotionStyles.Count;
        characterState.SetExpression(database.emotionStyles[currentEmotionIdx]);
        UpdateEmotionLabel();
    }

    private void CycleBlush(int delta)
    {
        int totalOptions = database.blushStyles.Count + 1; // 0 = None, 1..N = styles
        currentBlushIdx = (currentBlushIdx + delta + totalOptions) % totalOptions;

        if (currentBlushIdx == 0)
            characterState.SetBlush(null);
        else
            characterState.SetBlush(database.blushStyles[currentBlushIdx - 1]);

        UpdateBlushLabel();
    }

    // --- Color Palettes ---

    private void BuildColorPalettes()
    {
        if (database == null) return;

        // Hair Color Palette
        if (hairColorPaletteContainer != null)
        {
            foreach (Transform child in hairColorPaletteContainer)
                Destroy(child.gameObject);

            foreach (Color color in database.hairColorPresets)
            {
                CreatePaletteButton(hairColorPaletteContainer, color, () =>
                {
                    characterState.SetHairColor(color);
                });
            }
        }

        // Eye Color Palette
        if (eyeColorPaletteContainer != null)
        {
            foreach (Transform child in eyeColorPaletteContainer)
                Destroy(child.gameObject);

            foreach (Color color in database.eyeColorPresets)
            {
                CreatePaletteButton(eyeColorPaletteContainer, color, () =>
                {
                    characterState.SetEyeColor(color);
                });
            }
        }
    }

    private void CreatePaletteButton(Transform container, Color color, Action onClick)
    {
        GameObject btnObj = new GameObject("ColorBtn", typeof(RectTransform));
        btnObj.transform.SetParent(container, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = color;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.3f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.3f);
        btn.colors = colors;

        btn.onClick.AddListener(() => onClick?.Invoke());

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(44, 44);
    }

    // --- Label updates ---

    private void UpdateAllLabels()
    {
        UpdateBodyLabel();
        UpdateHairFrontLabel();
        UpdateHairBackLabel();
        UpdateEmotionLabel();
        UpdateBlushLabel();
    }

    private void UpdateBodyLabel()
    {
        if (bodySizeLabel != null && characterState != null)
            bodySizeLabel.text = characterState.CurrentBodySize.ToString();
    }

    private void UpdateHairFrontLabel()
    {
        if (hairFrontLabel != null)
        {
            if (database != null && database.hairFrontStyles.Count > 0)
            {
                Sprite s = database.hairFrontStyles[currentHairFrontIdx];
                hairFrontLabel.text = s != null ? s.name : "None";
            }
            else
            {
                hairFrontLabel.text = "Fixed";
                if (hairFrontPrevBtn != null) hairFrontPrevBtn.interactable = false;
                if (hairFrontNextBtn != null) hairFrontNextBtn.interactable = false;
            }
        }
    }

    private void UpdateHairBackLabel()
    {
        if (hairBackLabel != null)
        {
            if (database != null && database.hairBackStyles.Count > 0)
            {
                Sprite s = database.hairBackStyles[currentHairBackIdx];
                hairBackLabel.text = s != null ? s.name : "None";
            }
            else
            {
                hairBackLabel.text = "Fixed";
                if (hairBackPrevBtn != null) hairBackPrevBtn.interactable = false;
                if (hairBackNextBtn != null) hairBackNextBtn.interactable = false;
            }
        }
    }

    private void UpdateEmotionLabel()
    {
        if (emotionLabel != null && database != null && database.emotionStyles.Count > 0)
        {
            Sprite s = database.emotionStyles[currentEmotionIdx];
            emotionLabel.text = s != null ? s.name : "None";
        }
    }

    private void UpdateBlushLabel()
    {
        if (blushLabel != null)
        {
            if (currentBlushIdx == 0)
                blushLabel.text = "None";
            else
                blushLabel.text = $"Blush {currentBlushIdx}";
        }
    }

    private void OnRandomizeClicked()
    {
        if (characterState != null)
        {
            characterState.RandomizeAppearance();
            SyncFromCharacterState();
            UpdateAllLabels();
        }
    }

    private void OnAcceptClicked()
    {
        OnCharacterAccepted?.Invoke();
    }
}