using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using UnbridledSanctum.Combat;
using UnbridledSanctum.UI;
using UnbridledSanctum.Data;

/// <summary>
/// Manages the Sanctuary Cave Hub (Lobby):
/// A fullscreen Point-and-Click first-person cave view with 5 interactive elements
/// rendered as 1920x1080 pixel-accurate layers with alpha silhouette hit testing (threshold >= 0.1):
///   1. [ EXIT ]: Archway -> Triggers Expedition start.
///   2. [ DUMMY ]: Training dummy -> Opens Deckbuilder & collection overlay.
///   3. [ CRYSTAL ]: Floating levitating amethyst crystal -> Opens Desire Tree overlay.
///   4. [ CHEST ]: Storage chest -> Opens Wardrobe overlay and activates PaperDoll character model.
///   5. [ SETTINGS ]: Runic root in upper right -> Opens Settings modal window.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Main Hub Canvas & Background")]
    public GameObject lobbyPanel;
    public Image caveBgImage;

    [Header("Meta Currency Display")]
    public TextMeshProUGUI lustidBadgeText;

    [Header("Interactive Cave Objects")]
    public LobbyInteractiveObject exitInteractive;
    public LobbyInteractiveObject dummyInteractive;
    public LobbyInteractiveObject crystalInteractive;
    public LobbyInteractiveObject chestInteractive;
    public LobbyInteractiveObject settingsInteractive;
    public SteppedFloatingCrystal crystalLevitation;

    [Header("Cave Sprites (1920x1080)")]
    public Sprite lobbyBgSprite;
    public Sprite exitSprite;
    public Sprite dummySprite;
    public Sprite crystalSprite;
    public Sprite chestSprite;
    public Sprite settingsSprite;

    [Header("UI Skin Sprites")]
    public Sprite woodPanelSprite;
    public Sprite woodSlotSprite;
    public Sprite woodButtonSprite;
    public Sprite cardFrameSprite;
    public Sprite woodProgressFrameSprite;

    [Header("Overlays")]
    public GameObject wardrobeOverlay;
    public GameObject deckOverlay;
    public GameObject pathsOverlay;
    public GameObject settingsOverlay;

    [Header("Wardrobe Overlay Elements")]
    public Transform wardrobeItemsContainer;
    public TextMeshProUGUI equippedClothesText;
    public Button unequipClothesBtn;
    public Button repairClothesBtn;
    public Button wardrobeCloseBtn;

    [Header("Flesh Stabilization & Archive Elements")]
    public GameObject fleshStabilizationContainer;
    public Button[] bodySizeButtons;
    public Image[] bodySizeButtonBgs;
    public TextMeshProUGUI[] bodySizeButtonTexts;
    public TextMeshProUGUI fleshArchiveText;
    public TextMeshProUGUI fleshPranaGaugeText;
    public Button fleshDebugFillBtn;

    [Header("Emotion Test Elements")]
    public GameObject emotionContainer;
    public Button[] emotionButtons;
    public Image[] emotionButtonBgs;
    public TextMeshProUGUI[] emotionButtonTexts;
    private static readonly string[] emotionNames = { "Normal", "smile", "Angry", "Annoyed", "shocked", "Pain", "O" };

    [Header("Deck Overlay Elements")]
    public TextMeshProUGUI deckCountText;
    public Transform activeDeckContainer;
    public Transform availableCardsContainer;
    public CardData selectedCollectionCard;
    public TextMeshProUGUI detailCardNameText;
    public Image detailCardArtImg;
    public Transform detailStatsRow1;
    public Transform detailStatsRow2;
    public TextMeshProUGUI detailCardDescText;
    public Button detailEquipBtn;
    public Image detailEquipBtnBg;
    public TextMeshProUGUI detailEquipBtnText;
    public Button deckCloseBtn;

    public enum DeckCategoryFilter
    {
        All,
        WarriorTactics,
        LustInstincts,
        StruggleActions
    }
    public DeckCategoryFilter currentDeckCategoryFilter = DeckCategoryFilter.All;
    public Button filterAllBtn;
    public Button filterTacticsBtn;
    public Button filterLustBtn;
    public Button filterStruggleBtn;
    public Image filterAllBg;
    public Image filterTacticsBg;
    public Image filterLustBg;
    public Image filterStruggleBg;
    public TextMeshProUGUI filterAllText;
    public TextMeshProUGUI filterTacticsText;
    public TextMeshProUGUI filterLustText;
    public TextMeshProUGUI filterStruggleText;

    [Header("Desire Tree Overlay Elements")]
    public LustPathType selectedTreePath = LustPathType.PhallusDevotee;
    public Button treePathPhallusBtn;
    public Button treePathBoundBtn;
    public Button treePathMonsterBtn;
    public Image treePathPhallusBg;
    public Image treePathBoundBg;
    public Image treePathMonsterBg;
    public TextMeshProUGUI treePathPhallusText;
    public TextMeshProUGUI treePathBoundText;
    public TextMeshProUGUI treePathMonsterText;
    public TextMeshProUGUI treeHeaderLustidText;
    public Transform treeNodesScrollContent;
    public ScrollRect treeScrollRect;
    public DesireTreeNode selectedTreeNode;
    public Button pathsCloseBtn;

    [Header("Tree Detail / Purchase Inspector")]
    public TextMeshProUGUI treeDetailTitleText;
    public TextMeshProUGUI treeDetailCategoryText;
    public TextMeshProUGUI treeDetailDescText;
    public TextMeshProUGUI treeDetailBonusText;
    public Button treePurchaseBtn;
    public Image treePurchaseBtnBg;
    public TextMeshProUGUI treePurchaseBtnText;

    [Header("Tree Inactive / Path Selection Overlay")]
    public GameObject treeOverlayPanel;
    public TextMeshProUGUI treeOverlayTitleText;
    public TextMeshProUGUI treeOverlayDescText;
    public Button treeOverlaySelectBtn;
    public Image treeOverlaySelectBtnBg;
    public TextMeshProUGUI treeOverlaySelectBtnText;

    [Header("Settings Overlay Elements")]
    public Slider settingsMasterSlider;
    public TextMeshProUGUI settingsMasterVolumeText;
    public Button settingsFullscreenToggleBtn;
    public TextMeshProUGUI settingsFullscreenToggleText;
    public Button settingsMainMenuBtn;
    public Button settingsCloseBtn;

    [Header("Dependencies & State")]
    public CharacterState characterState;
    public InventoryManager inventoryManager;
    public PaperDollController paperDoll;

    public GameObject currentActiveOverlay;
    private int currentMaxFloor = 1;

    public event Action OnStartExpedition;
    public event Action OnReturnToMainMenu;

    private void Awake()
    {
        if (lobbyPanel == null) lobbyPanel = gameObject;
        DesireTreeManager.EnsureExists();
        int savedPath = PlayerPrefs.GetInt("Sanctum_ActiveLustPath", (int)LustPathType.PhallusDevotee);
        selectedTreePath = (LustPathType)savedPath;

        AutoWireReferences();
    }

    /// <summary>
    /// Automatically connects all scene references and constructs missing overlay panels.
    /// Can be invoked directly from the Inspector context menu at design-time or at runtime.
    /// </summary>
    [ContextMenu("Auto-Wire Sanctuary Hub")]
    public void AutoWireReferences()
    {
        if (lobbyPanel == null) lobbyPanel = gameObject;

        if (characterState == null) characterState = FindFirstObjectByType<CharacterState>();
        if (inventoryManager == null) inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (paperDoll == null) paperDoll = FindFirstObjectByType<PaperDollController>(FindObjectsInactive.Include);

        // Auto-find interactive objects if not manually assigned in Inspector
        LobbyInteractiveObject[] lios = GetComponentsInChildren<LobbyInteractiveObject>(true);
        foreach (var lio in lios)
        {
            string n = lio.gameObject.name.ToLowerInvariant();
            if (n.Contains("exit") && exitInteractive == null) exitInteractive = lio;
            else if (n.Contains("crystal") && crystalInteractive == null) crystalInteractive = lio;
            else if (n.Contains("dummy") && dummyInteractive == null) dummyInteractive = lio;
            else if (n.Contains("chest") && chestInteractive == null) chestInteractive = lio;
            else if (n.Contains("setting") && settingsInteractive == null) settingsInteractive = lio;
        }

        if (crystalLevitation == null)
        {
            crystalLevitation = GetComponentInChildren<SteppedFloatingCrystal>(true);
        }

        if (caveBgImage == null)
        {
            Image[] imgs = GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                string n = img.gameObject.name.ToLowerInvariant();
                if (n.Contains("cave") || n.Contains("lobby") || n.Contains("background") || n.Contains("bg"))
                {
                    caveBgImage = img;
                    break;
                }
            }
        }

        if (lustidBadgeText == null)
        {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                string n = t.gameObject.name.ToLowerInvariant();
                string pn = t.transform.parent != null ? t.transform.parent.name.ToLowerInvariant() : "";
                if (n.Contains("lustid") || pn.Contains("lustid"))
                {
                    lustidBadgeText = t;
                    break;
                }
            }
        }

        if (filterAllBtn == null && deckOverlay != null)
        {
            Button[] deckBtns = deckOverlay.GetComponentsInChildren<Button>(true);
            foreach (var b in deckBtns)
            {
                string bn = b.gameObject.name.ToLowerInvariant();
                Image bg = b.GetComponent<Image>();
                TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>(true);
                if (bn.Contains("filterbtn_all")) { filterAllBtn = b; filterAllBg = bg; filterAllText = t; }
                else if (bn.Contains("filterbtn_warriortactics") || bn.Contains("filterbtn_tactics")) { filterTacticsBtn = b; filterTacticsBg = bg; filterTacticsText = t; }
                else if (bn.Contains("filterbtn_lustinstincts") || bn.Contains("filterbtn_lust")) { filterLustBtn = b; filterLustBg = bg; filterLustText = t; }
                else if (bn.Contains("filterbtn_struggleactions") || bn.Contains("filterbtn_struggle")) { filterStruggleBtn = b; filterStruggleBg = bg; filterStruggleText = t; }
            }
        }

        ConfigureInteractiveCaveObjects();
        EnsureOverlaysContainerAndPanels();
        ApplyWoodPanelSkin();
        SetupListeners();
    }

    public static Sprite LoadUISprite(string fileName)
    {
#if UNITY_EDITOR
        string path = $"Assets/sprites/UI/{fileName}";
        Sprite spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (spr != null) return spr;

        UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
        spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (spr != null) return spr;

        UnityEngine.Object[] subs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var sub in subs)
        {
            if (sub is Sprite s) return s;
        }
#endif
        return null;
    }

    public void ApplyWoodPanelSkin()
    {
        if (woodPanelSprite == null)
        {
            woodPanelSprite = LoadUISprite("ui_panel_wood.png");
        }

        if (woodSlotSprite == null)
        {
            woodSlotSprite = LoadUISprite("ui_slot_wood.png");
        }

        if (woodButtonSprite == null)
        {
            woodButtonSprite = LoadUISprite("ui_button.png");
        }

        if (woodProgressFrameSprite == null)
        {
            woodProgressFrameSprite = LoadUISprite("ui_progressbar_frame.png");
        }

        if (woodPanelSprite != null)
        {
            // 1. Settings modal card
            if (settingsOverlay != null)
            {
                Transform card = settingsOverlay.transform.Find("SettingsModalCard");
                if (card != null) ApplySlicedWoodBg(card.gameObject);
            }

            // 2. Wardrobe panel
            if (wardrobeOverlay != null)
            {
                Transform wp = wardrobeOverlay.transform.Find("WardrobePanel");
                if (wp != null) ApplySlicedWoodBg(wp.gameObject);
            }

            // 3. Deckbuilder panel & detail panel
            if (deckOverlay != null)
            {
                Transform dp = deckOverlay.transform.Find("CardDetailPanel") 
                    ?? deckOverlay.transform.Find("DeckPanel/TopCardSelectionSection/DetailPanel")
                    ?? deckOverlay.transform.Find("DeckbuilderPanel/TopCardSelectionSection/CardDetailPanel")
                    ?? deckOverlay.transform.Find("DeckbuilderPanel/TopCardSelectionSection/DetailPanel");
                if (dp != null) ApplySlicedWoodBg(dp.gameObject);

                Transform dPanel = deckOverlay.transform.Find("DeckPanel") ?? deckOverlay.transform.Find("DeckbuilderPanel");
                if (dPanel != null) ApplySlicedWoodBg(dPanel.gameObject);
            }

            // 4. Desire Tree panel & detail panel
            if (pathsOverlay != null)
            {
                Transform tp = pathsOverlay.transform.Find("TreeDetailPanel") ?? pathsOverlay.transform.Find("PathsPanel/TreeDetailPanel");
                if (tp != null) ApplySlicedWoodBg(tp.gameObject);

                Transform pPanel = pathsOverlay.transform.Find("PathsPanel");
                if (pPanel != null) ApplySlicedWoodBg(pPanel.gameObject);

                Transform treeLeft = pathsOverlay.transform.Find("PathsPanel/TreeNodesArea") ?? pathsOverlay.transform.Find("TreeNodesArea");
                if (treeLeft != null)
                {
                    Outline tlaOutline = treeLeft.GetComponent<Outline>();
                    if (tlaOutline != null) tlaOutline.enabled = false;
                }
            }
        }

        if (woodSlotSprite != null)
        {
            // Wardrobe inset boxes
            if (wardrobeOverlay != null)
            {
                Transform eqBox = wardrobeOverlay.transform.Find("WardrobePanel/EquippedBox") ?? wardrobeOverlay.transform.Find("EquippedBox");
                if (eqBox != null) ApplySlicedWoodSlot(eqBox.gameObject);

                Transform stabBox = wardrobeOverlay.transform.Find("WardrobePanel/FleshStabilizationBox") ?? wardrobeOverlay.transform.Find("FleshStabilizationBox");
                if (stabBox != null) ApplySlicedWoodSlot(stabBox.gameObject);

                Transform emoBox = wardrobeOverlay.transform.Find("WardrobePanel/EmotionSelectionBox") 
                    ?? wardrobeOverlay.transform.Find("WardrobePanel/EmotionBox") 
                    ?? wardrobeOverlay.transform.Find("EmotionSelectionBox") 
                    ?? wardrobeOverlay.transform.Find("EmotionBox");
                if (emoBox != null) ApplySlicedWoodSlot(emoBox.gameObject);
            }

            // Deckbuilder inset boxes
            if (deckOverlay != null)
            {
                Transform colPanel = deckOverlay.transform.Find("CollectionPanel") ?? deckOverlay.transform.Find("DeckbuilderPanel/CollectionPanel");
                if (colPanel != null) ApplySlicedWoodSlot(colPanel.gameObject);

                Transform actPanel = deckOverlay.transform.Find("ActiveDeckBar") ?? deckOverlay.transform.Find("DeckbuilderPanel/ActiveDeckBar");
                if (actPanel != null) ApplySlicedWoodSlot(actPanel.gameObject);
            }

            // Tree scroll box
            if (pathsOverlay != null)
            {
                Transform treeScroll = pathsOverlay.transform.Find("TreeScrollRect") ?? pathsOverlay.transform.Find("PathsPanel/TreeScrollRect");
                if (treeScroll != null) ApplySlicedWoodSlot(treeScroll.gameObject);
            }
        }

        if (woodButtonSprite != null)
        {
            // 1. Back / Close buttons across all overlays
            var allBackBtns = new[] { wardrobeCloseBtn, deckCloseBtn, pathsCloseBtn, settingsCloseBtn };
            foreach (var b in allBackBtns)
            {
                if (b != null)
                {
                    ApplySlicedWoodButton(b.gameObject, Color.white);
                    var txt = b.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null && !string.IsNullOrEmpty(txt.text))
                    {
                        txt.color = new Color(0.92f, 0.88f, 0.78f, 1f);
                        txt.text = txt.text.Replace("✕", "X").Replace("\u2715", "X").Replace("▢", "").Trim();
                        if (string.IsNullOrEmpty(txt.text)) txt.text = "X";
                    }
                }
            }

            // Also check CloseXBtn in settingsOverlay
            if (settingsOverlay != null)
            {
                Transform xBtn = settingsOverlay.transform.Find("SettingsModalCard/CloseXBtn");
                if (xBtn != null)
                {
                    ApplySlicedWoodButton(xBtn.gameObject, Color.white);
                    var txt = xBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null && !string.IsNullOrEmpty(txt.text))
                    {
                        txt.color = new Color(0.92f, 0.88f, 0.78f, 1f);
                        txt.text = txt.text.Replace("✕", "X").Replace("\u2715", "X").Replace("▢", "").Trim();
                        if (string.IsNullOrEmpty(txt.text)) txt.text = "X";
                    }
                }
            }

            // 2. Wardrobe action buttons
            if (unequipClothesBtn != null) ApplySlicedWoodButton(unequipClothesBtn.gameObject, new Color(0.92f, 0.65f, 0.65f, 1f));
            if (repairClothesBtn != null) ApplySlicedWoodButton(repairClothesBtn.gameObject, new Color(0.75f, 0.95f, 0.80f, 1f));
            if (fleshDebugFillBtn != null) ApplySlicedWoodButton(fleshDebugFillBtn.gameObject, new Color(0.85f, 0.75f, 0.90f, 1f));

            if (bodySizeButtons != null)
            {
                foreach (var b in bodySizeButtons)
                {
                    if (b != null) ApplySlicedWoodButton(b.gameObject);
                }
            }

            if (emotionButtons != null)
            {
                foreach (var b in emotionButtons)
                {
                    if (b != null) ApplySlicedWoodButton(b.gameObject);
                }
            }

            // 3. Deckbuilder buttons
            if (detailEquipBtn != null) ApplySlicedWoodButton(detailEquipBtn.gameObject);
            if (filterAllBtn != null) ApplySlicedWoodButton(filterAllBtn.gameObject);
            if (filterTacticsBtn != null) ApplySlicedWoodButton(filterTacticsBtn.gameObject);
            if (filterLustBtn != null) ApplySlicedWoodButton(filterLustBtn.gameObject);
            if (filterStruggleBtn != null) ApplySlicedWoodButton(filterStruggleBtn.gameObject);

            // 4. Settings controls
            if (settingsFullscreenToggleBtn != null) ApplySlicedWoodButton(settingsFullscreenToggleBtn.gameObject, new Color(0.85f, 0.82f, 0.78f, 1f));
            if (settingsMainMenuBtn != null) ApplySlicedWoodButton(settingsMainMenuBtn.gameObject, new Color(0.90f, 0.65f, 0.65f, 1f));

            if (settingsMasterSlider != null)
            {
                if (settingsMasterSlider.handleRect != null)
                {
                    Image handleImg = settingsMasterSlider.handleRect.GetComponent<Image>();
                    if (handleImg != null)
                    {
                        handleImg.sprite = woodButtonSprite;
                        handleImg.type = Image.Type.Sliced;
                        handleImg.color = new Color(1.15f, 1.10f, 0.95f, 1f);
                    }
                }
                Transform slBg = settingsMasterSlider.transform.Find("Background");
                if (slBg != null)
                {
                    Image bgImg = slBg.GetComponent<Image>();
                    if (bgImg != null)
                    {
                        bgImg.sprite = woodProgressFrameSprite != null ? woodProgressFrameSprite : woodSlotSprite;
                        bgImg.type = Image.Type.Sliced;
                        bgImg.color = Color.white;
                    }
                    Outline ol = slBg.GetComponent<Outline>();
                    if (ol != null) ol.enabled = false;
                }

                Transform slFill = settingsMasterSlider.transform.Find("Fill Area/Fill");
                if (slFill != null)
                {
                    Image fillImg = slFill.GetComponent<Image>();
                    if (fillImg != null)
                    {
                        fillImg.color = new Color(0.85f, 0.65f, 0.25f, 0.95f);
                    }
                }
            }

            // 5. Desire Tree tabs & action buttons
            if (treePathPhallusBtn != null) ApplySlicedWoodButton(treePathPhallusBtn.gameObject);
            if (treePathBoundBtn != null) ApplySlicedWoodButton(treePathBoundBtn.gameObject);
            if (treePathMonsterBtn != null) ApplySlicedWoodButton(treePathMonsterBtn.gameObject);
            if (treeOverlaySelectBtn != null) ApplySlicedWoodButton(treeOverlaySelectBtn.gameObject);
            if (treePurchaseBtn != null) ApplySlicedWoodButton(treePurchaseBtn.gameObject);
        }

        ApplyWardrobeLayoutPaddings();
        ApplySettingsLayoutPaddings();
        ApplyPathsLayoutPaddings();
        ApplyDeckLayoutPaddings();
    }

    public void ApplyWardrobeLayoutPaddings()
    {
        if (wardrobeOverlay == null) return;

        Transform wp = wardrobeOverlay.transform.Find("WardrobePanel") ?? wardrobeOverlay.transform;

        // 1. Header Title
        Transform title = wp.Find("Title");
        if (title != null)
        {
            RectTransform tr = title.GetComponent<RectTransform>();
            if (tr != null)
            {
                tr.anchoredPosition = new Vector2(32f, -22f);
                tr.sizeDelta = new Vector2(0f, 36f);
            }
        }

        // 2. Back button
        Transform back = wp.Find("BackBtn");
        if (back != null)
        {
            RectTransform br = back.GetComponent<RectTransform>();
            if (br != null)
            {
                br.anchoredPosition = new Vector2(-28f, -20f);
                br.sizeDelta = new Vector2(170f, 38f);
            }
        }

        // 3. EquippedBox
        Transform eqBox = wp.Find("EquippedBox");
        if (eqBox != null)
        {
            RectTransform eqr = eqBox.GetComponent<RectTransform>();
            if (eqr != null)
            {
                eqr.anchoredPosition = new Vector2(0f, -70f);
                eqr.sizeDelta = new Vector2(-56f, 68f);
            }

            Transform eqTxt = eqBox.Find("Text");
            if (eqTxt != null)
            {
                RectTransform tr = eqTxt.GetComponent<RectTransform>();
                if (tr != null)
                {
                    tr.anchoredPosition = new Vector2(24f, 0f);
                    tr.sizeDelta = new Vector2(-280f, 0f);
                }
            }

            Transform stripBtn = eqBox.Find("UnequipClothesBtn");
            if (stripBtn != null)
            {
                RectTransform sr = stripBtn.GetComponent<RectTransform>();
                if (sr != null)
                {
                    sr.anchoredPosition = new Vector2(-24f, 0f);
                    sr.sizeDelta = new Vector2(110f, 36f);
                }
            }

            Transform repBtn = eqBox.Find("RepairClothesBtn");
            if (repBtn != null)
            {
                RectTransform rr = repBtn.GetComponent<RectTransform>();
                if (rr != null)
                {
                    rr.anchoredPosition = new Vector2(-144f, 0f);
                    rr.sizeDelta = new Vector2(125f, 36f);
                }
            }
        }

        // 4. FleshStabilizationBox
        Transform stabBox = wp.Find("FleshStabilizationBox");
        if (stabBox != null)
        {
            RectTransform stbr = stabBox.GetComponent<RectTransform>();
            if (stbr != null)
            {
                stbr.anchoredPosition = new Vector2(0f, -148f);
                stbr.sizeDelta = new Vector2(-56f, 132f);
            }

            Transform stTitle = stabBox.Find("StabTitle");
            if (stTitle != null)
            {
                RectTransform str = stTitle.GetComponent<RectTransform>();
                if (str != null)
                {
                    str.anchoredPosition = new Vector2(24f, -14f);
                    str.sizeDelta = new Vector2(-48f, 22f);
                }
            }

            Transform sizeRow = stabBox.Find("SizeButtonsRow");
            if (sizeRow != null)
            {
                RectTransform srr = sizeRow.GetComponent<RectTransform>();
                if (srr != null)
                {
                    srr.anchoredPosition = new Vector2(0f, -42f);
                    srr.sizeDelta = new Vector2(-48f, 36f);
                }
            }

            Transform infoRow = stabBox.Find("InfoRow");
            if (infoRow != null)
            {
                RectTransform ir = infoRow.GetComponent<RectTransform>();
                if (ir != null)
                {
                    ir.anchoredPosition = new Vector2(0f, 14f);
                    ir.sizeDelta = new Vector2(-48f, 32f);
                }
            }
        }

        // 5. EmotionSelectionBox
        Transform emoBox = wp.Find("EmotionSelectionBox") ?? wp.Find("EmotionBox");
        if (emoBox != null)
        {
            RectTransform emr = emoBox.GetComponent<RectTransform>();
            if (emr != null)
            {
                emr.anchoredPosition = new Vector2(0f, -290f);
                emr.sizeDelta = new Vector2(-56f, 84f);
            }

            Transform emoTitle = emoBox.Find("EmoTitle");
            if (emoTitle != null)
            {
                RectTransform etr = emoTitle.GetComponent<RectTransform>();
                if (etr != null)
                {
                    etr.anchoredPosition = new Vector2(24f, -12f);
                    etr.sizeDelta = new Vector2(-48f, 20f);
                }
            }

            Transform emoRow = emoBox.Find("EmotionButtonsRow");
            if (emoRow != null)
            {
                RectTransform err = emoRow.GetComponent<RectTransform>();
                if (err != null)
                {
                    err.offsetMin = new Vector2(24f, 10f);
                    err.offsetMax = new Vector2(-24f, -32f);
                }
            }
        }

        // 6. ItemsScroll
        Transform scroll = wp.Find("ItemsScroll");
        if (scroll != null)
        {
            RectTransform scr = scroll.GetComponent<RectTransform>();
            if (scr != null)
            {
                scr.offsetMax = new Vector2(-28f, -384f);
                scr.offsetMin = new Vector2(28f, 25f);
            }

            VerticalLayoutGroup vlg = scroll.GetComponentInChildren<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 10f;
            }
        }
    }

    public void ApplySettingsLayoutPaddings()
    {
        if (settingsOverlay == null) return;
        Transform card = settingsOverlay.transform.Find("SettingsModalCard");
        if (card == null) return;

        RectTransform cardRt = card.GetComponent<RectTransform>();
        if (cardRt != null)
        {
            cardRt.sizeDelta = new Vector2(600f, 360f);
        }

        Transform title = card.Find("Title");
        if (title != null)
        {
            RectTransform tr = title.GetComponent<RectTransform>();
            if (tr != null)
            {
                tr.anchoredPosition = new Vector2(32f, -22f);
                tr.sizeDelta = new Vector2(-120f, 32f);
            }
        }

        Transform xBtn = card.Find("CloseXBtn");
        if (xBtn != null)
        {
            RectTransform xr = xBtn.GetComponent<RectTransform>();
            if (xr != null)
            {
                xr.anchoredPosition = new Vector2(-26f, -20f);
                xr.sizeDelta = new Vector2(38f, 38f);
            }
        }

        Transform content = card.Find("ContentArea");
        if (content != null)
        {
            RectTransform cr = content.GetComponent<RectTransform>();
            if (cr != null)
            {
                cr.offsetMin = new Vector2(36f, 30f);
                cr.offsetMax = new Vector2(-36f, -70f);
            }

            Transform volLabel = content.Find("VolumeLabel");
            if (volLabel != null)
            {
                RectTransform vr = volLabel.GetComponent<RectTransform>();
                if (vr != null)
                {
                    vr.anchoredPosition = new Vector2(0f, -6f);
                    vr.sizeDelta = new Vector2(0f, 26f);
                }
            }

            Transform slider = content.Find("MasterSlider");
            if (slider != null)
            {
                RectTransform sr = slider.GetComponent<RectTransform>();
                if (sr != null)
                {
                    sr.anchoredPosition = new Vector2(0f, -38f);
                    sr.sizeDelta = new Vector2(0f, 28f);
                }

                Transform handleArea = slider.Find("Handle Slide Area");
                if (handleArea != null)
                {
                    RectTransform har = handleArea.GetComponent<RectTransform>();
                    if (har != null)
                    {
                        har.offsetMin = new Vector2(14f, 0f);
                        har.offsetMax = new Vector2(-14f, 0f);
                    }
                }
            }

            Transform fsBtn = content.Find("FullscreenToggleBtn");
            if (fsBtn != null)
            {
                RectTransform fr = fsBtn.GetComponent<RectTransform>();
                if (fr != null)
                {
                    fr.anchoredPosition = new Vector2(0f, -96f);
                    fr.sizeDelta = new Vector2(0f, 44f);
                }
            }

            Transform mmBtn = content.Find("MainMenuBtn");
            if (mmBtn != null)
            {
                RectTransform mr = mmBtn.GetComponent<RectTransform>();
                if (mr != null)
                {
                    mr.anchoredPosition = new Vector2(0f, 8f);
                    mr.sizeDelta = new Vector2(0f, 44f);
                }
            }
        }
    }

    public void ApplyPathsLayoutPaddings()
    {
        if (pathsOverlay == null) return;
        Transform pp = pathsOverlay.transform.Find("PathsPanel") ?? pathsOverlay.transform;

        // 1. Top Bar
        Transform topBar = pp.Find("TreeTopBar");
        if (topBar != null)
        {
            RectTransform tbr = topBar.GetComponent<RectTransform>();
            if (tbr != null)
            {
                tbr.anchoredPosition = new Vector2(0f, -24f);
                tbr.sizeDelta = new Vector2(-68f, 44f);
            }

            Transform pBtns = topBar.Find("PathButtonsRow");
            if (pBtns != null)
            {
                RectTransform pbr = pBtns.GetComponent<RectTransform>();
                if (pbr != null)
                {
                    pbr.anchorMin = new Vector2(0f, 0f);
                    pbr.anchorMax = new Vector2(0.62f, 1f);
                    pbr.offsetMin = Vector2.zero;
                    pbr.offsetMax = Vector2.zero;
                }
            }

            Transform lustid = topBar.Find("TreeLustidDisplay");
            if (lustid != null)
            {
                RectTransform lr = lustid.GetComponent<RectTransform>();
                if (lr != null)
                {
                    lr.anchorMin = new Vector2(0.62f, 0f);
                    lr.anchorMax = new Vector2(0.85f, 1f);
                    lr.offsetMin = new Vector2(10f, 0f);
                    lr.offsetMax = new Vector2(-15f, 0f);
                }
            }
        }

        // 2. Back button
        Transform backBtn = pp.Find("BackBtn");
        if (backBtn != null)
        {
            RectTransform br = backBtn.GetComponent<RectTransform>();
            if (br != null)
            {
                br.anchoredPosition = new Vector2(-34f, -24f);
                br.sizeDelta = new Vector2(170f, 38f);
            }
        }

        // 3. TreeNodesArea (Left column)
        Transform treeLeft = pp.Find("TreeNodesArea");
        if (treeLeft != null)
        {
            RectTransform tlr = treeLeft.GetComponent<RectTransform>();
            if (tlr != null)
            {
                tlr.anchorMin = new Vector2(0f, 0f);
                tlr.anchorMax = new Vector2(0.63f, 1f);
                tlr.offsetMin = new Vector2(34f, 28f);
                tlr.offsetMax = new Vector2(-12f, -82f);
            }
        }

        // 4. TreeDetailPanel (Right column)
        Transform treeRight = pp.Find("TreeDetailPanel");
        if (treeRight != null)
        {
            RectTransform trr = treeRight.GetComponent<RectTransform>();
            if (trr != null)
            {
                trr.anchorMin = new Vector2(0.63f, 0f);
                trr.anchorMax = new Vector2(1f, 1f);
                trr.offsetMin = new Vector2(12f, 28f);
                trr.offsetMax = new Vector2(-34f, -82f);
            }

            Transform dHead = treeRight.Find("InspectorHeader");
            if (dHead != null)
            {
                RectTransform dhr = dHead.GetComponent<RectTransform>();
                if (dhr != null) dhr.anchoredPosition = new Vector2(24f, -20f);
            }

            Transform dTitle = treeRight.Find("NodeTitle");
            if (dTitle != null)
            {
                RectTransform dtr = dTitle.GetComponent<RectTransform>();
                if (dtr != null) dtr.anchoredPosition = new Vector2(24f, -44f);
            }

            Transform dCat = treeRight.Find("NodeCategory");
            if (dCat != null)
            {
                RectTransform dcr = dCat.GetComponent<RectTransform>();
                if (dcr != null) dcr.anchoredPosition = new Vector2(24f, -78f);
            }

            Transform dDesc = treeRight.Find("Description");
            if (dDesc != null)
            {
                RectTransform ddr = dDesc.GetComponent<RectTransform>();
                if (ddr != null)
                {
                    ddr.anchoredPosition = new Vector2(24f, -112f);
                    ddr.sizeDelta = new Vector2(-48f, 100f);
                }
            }

            Transform dBonus = treeRight.Find("NodeBonuses");
            if (dBonus != null)
            {
                RectTransform dbr = dBonus.GetComponent<RectTransform>();
                if (dbr != null)
                {
                    dbr.offsetMin = new Vector2(24f, 85f);
                    dbr.offsetMax = new Vector2(-24f, -220f);
                }
            }

            Transform awakenBtn = treeRight.Find("Btn_AwakenNode");
            if (awakenBtn != null)
            {
                RectTransform abr = awakenBtn.GetComponent<RectTransform>();
                if (abr != null)
                {
                    abr.anchoredPosition = new Vector2(0f, 26f);
                    abr.sizeDelta = new Vector2(-52f, 46f);
                }
            }
        }
    }

    public void ApplyDeckLayoutPaddings()
    {
        if (deckOverlay == null) return;
        Transform dp = deckOverlay.transform.Find("DeckPanel") ?? deckOverlay.transform.Find("DeckbuilderPanel") ?? deckOverlay.transform;

        // 1. Title
        Transform title = dp.Find("Title");
        if (title != null)
        {
            RectTransform tr = title.GetComponent<RectTransform>();
            if (tr != null)
            {
                tr.anchoredPosition = new Vector2(34f, -22f);
                tr.sizeDelta = new Vector2(0f, 36f);
            }
        }

        // 2. DeckCount
        Transform count = dp.Find("DeckCount");
        if (count != null)
        {
            RectTransform cr = count.GetComponent<RectTransform>();
            if (cr != null)
            {
                cr.anchoredPosition = new Vector2(-220f, -22f);
                cr.sizeDelta = new Vector2(0f, 32f);
            }
        }

        // 3. BackBtn
        Transform backBtn = dp.Find("BackBtn");
        if (backBtn != null)
        {
            RectTransform br = backBtn.GetComponent<RectTransform>();
            if (br != null)
            {
                br.anchoredPosition = new Vector2(-34f, -22f);
                br.sizeDelta = new Vector2(170f, 38f);
            }
        }

        // 4. TopCardSelectionSection
        Transform topSection = dp.Find("TopCardSelectionSection");
        if (topSection != null)
        {
            RectTransform tsr = topSection.GetComponent<RectTransform>();
            if (tsr != null)
            {
                tsr.offsetMin = new Vector2(34f, 12f);
                tsr.offsetMax = new Vector2(-34f, -72f);
            }

            Transform detailPanel = topSection.Find("DetailPanel");
            if (detailPanel != null)
            {
                Transform equipBtn = detailPanel.Find("EquipBtn");
                if (equipBtn != null)
                {
                    RectTransform er = equipBtn.GetComponent<RectTransform>();
                    if (er != null)
                    {
                        er.anchoredPosition = new Vector2(0f, 24f);
                        er.sizeDelta = new Vector2(260f, 46f);
                    }
                }
            }
        }

        // 5. BottomActiveDeckSection
        Transform bottomSection = dp.Find("BottomActiveDeckSection");
        if (bottomSection != null)
        {
            RectTransform bsr = bottomSection.GetComponent<RectTransform>();
            if (bsr != null)
            {
                bsr.offsetMin = new Vector2(34f, 26f);
                bsr.offsetMax = new Vector2(-34f, -8f);
            }
        }
    }

    private void ApplySlicedWoodBg(GameObject panelObj)
    {
        if (panelObj == null || woodPanelSprite == null) return;
        Image img = panelObj.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = woodPanelSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        Outline ol = panelObj.GetComponent<Outline>();
        if (ol != null) ol.enabled = false;
    }

    private void ApplySlicedWoodSlot(GameObject slotObj)
    {
        if (slotObj == null || woodSlotSprite == null) return;
        Image img = slotObj.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = woodSlotSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        Outline ol = slotObj.GetComponent<Outline>();
        if (ol != null) ol.enabled = false;
    }

    private void ApplySlicedWoodButton(GameObject btnObj, Color? tint = null)
    {
        if (btnObj == null || woodButtonSprite == null) return;
        Image img = btnObj.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = woodButtonSprite;
            img.type = Image.Type.Sliced;
            img.color = tint ?? Color.white;
        }
        Outline ol = btnObj.GetComponent<Outline>();
        if (ol != null) ol.enabled = false;

        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = tint ?? Color.white;
            cb.highlightedColor = new Color(1.22f, 1.18f, 1.08f, 1f);
            cb.pressedColor = new Color(0.70f, 0.65f, 0.60f, 1f);
            cb.selectedColor = cb.normalColor;
            cb.disabledColor = new Color(0.40f, 0.40f, 0.40f, 0.50f);
            cb.colorMultiplier = 1f;
            btn.colors = cb;
        }
    }

    public void ConfigureInteractiveCaveObjects()
    {
        // Clean up any old tooltip object if present in hierarchy
        Transform parent = lobbyPanel != null ? lobbyPanel.transform : transform;
        Transform oldTooltip = parent.Find("LobbyTooltip");
        if (oldTooltip != null)
        {
            Destroy(oldTooltip.gameObject);
        }

        if (exitInteractive != null)
        {
            exitInteractive.highlightType = LobbyInteractiveObject.HighlightType.SunlightGlow;
            exitInteractive.hoverBrightness = 1.10f;
            exitInteractive.glowColor = new Color(1.0f, 0.96f, 0.85f, 0.10f);
            exitInteractive.outlineWidth = 0f;
        }

        if (dummyInteractive != null)
        {
            dummyInteractive.highlightType = LobbyInteractiveObject.HighlightType.OutlineShader;
            dummyInteractive.outlineWidth = 2.5f;
            dummyInteractive.outlineColor = new Color(0.85f, 0.88f, 0.92f, 1f);
        }

        if (crystalInteractive != null)
        {
            crystalInteractive.highlightType = LobbyInteractiveObject.HighlightType.OutlineShader;
            crystalInteractive.outlineWidth = 2.5f;
            crystalInteractive.outlineColor = new Color(0.85f, 0.88f, 0.92f, 1f);
        }

        if (chestInteractive != null)
        {
            chestInteractive.highlightType = LobbyInteractiveObject.HighlightType.OutlineShader;
            chestInteractive.outlineWidth = 2.5f;
            chestInteractive.outlineColor = new Color(0.85f, 0.88f, 0.92f, 1f);
        }

        if (settingsInteractive != null)
        {
            settingsInteractive.highlightType = LobbyInteractiveObject.HighlightType.OutlineShader;
            settingsInteractive.outlineWidth = 2.5f;
            settingsInteractive.outlineColor = new Color(0.85f, 0.88f, 0.92f, 1f);
        }
    }

    private void Start()
    {
        ConfigureInteractiveCaveObjects();
        ApplyWoodPanelSkin();
        SetupListeners();
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnLustidChanged += HandleLustidChanged;
        }
        RefreshLustidDisplay();
    }

    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnLustidChanged -= HandleLustidChanged;
        }
    }

    private void Update()
    {
        // Handle ESC key for closing active overlays or opening settings
        bool escPressed = false;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            escPressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            escPressed = true;
        }

        if (escPressed && lobbyPanel != null && lobbyPanel.activeInHierarchy)
        {
            if (currentActiveOverlay != null && currentActiveOverlay.activeSelf)
            {
                CloseActiveOverlay();
            }
            else
            {
                OpenSettingsOverlay();
            }
        }
    }

    private void HandleLustidChanged(int _)
    {
        RefreshLustidDisplay();
    }

    public void RefreshLustidDisplay()
    {
        int lustid = CurrencyManager.Instance != null ? CurrencyManager.Instance.TotalLustid : 0;
        if (lustidBadgeText != null)
        {
            lustidBadgeText.text = $"{lustid} Lustid";
        }
    }

    public void SetupListeners()
    {
        // 1. Cave Interactive Objects clicks
        if (exitInteractive != null)
        {
            exitInteractive.OnClick -= HandleExitClicked;
            exitInteractive.OnClick += HandleExitClicked;
        }

        if (dummyInteractive != null)
        {
            dummyInteractive.OnClick -= OpenDeckOverlay;
            dummyInteractive.OnClick += OpenDeckOverlay;
        }

        if (crystalInteractive != null)
        {
            crystalInteractive.OnClick -= OpenPathsOverlay;
            crystalInteractive.OnClick += OpenPathsOverlay;
        }

        if (chestInteractive != null)
        {
            chestInteractive.OnClick -= OpenWardrobeOverlay;
            chestInteractive.OnClick += OpenWardrobeOverlay;
        }

        if (settingsInteractive != null)
        {
            settingsInteractive.OnClick -= OpenSettingsOverlay;
            settingsInteractive.OnClick += OpenSettingsOverlay;
        }

        // 2. Overlay Close / Back buttons
        if (wardrobeCloseBtn != null)
        {
            wardrobeCloseBtn.onClick.RemoveAllListeners();
            wardrobeCloseBtn.onClick.AddListener(() => CloseActiveOverlay());
        }

        if (deckCloseBtn != null)
        {
            deckCloseBtn.onClick.RemoveAllListeners();
            deckCloseBtn.onClick.AddListener(() => CloseActiveOverlay());
        }

        if (pathsCloseBtn != null)
        {
            pathsCloseBtn.onClick.RemoveAllListeners();
            pathsCloseBtn.onClick.AddListener(() => CloseActiveOverlay());
        }

        if (settingsCloseBtn != null)
        {
            settingsCloseBtn.onClick.RemoveAllListeners();
            settingsCloseBtn.onClick.AddListener(() => CloseActiveOverlay());
        }

        // 3. Wardrobe page buttons
        if (unequipClothesBtn != null)
        {
            unequipClothesBtn.onClick.RemoveAllListeners();
            unequipClothesBtn.onClick.AddListener(() =>
            {
                if (characterState != null)
                {
                    characterState.UnequipItem(EquipSlot.CombinedClothes);
                    characterState.UnequipItem(EquipSlot.UpperClothes);
                    characterState.UnequipItem(EquipSlot.LowerClothes);
                    RefreshWardrobe();
                }
            });
        }

        if (repairClothesBtn != null)
        {
            repairClothesBtn.onClick.RemoveAllListeners();
            repairClothesBtn.onClick.AddListener(() =>
            {
                CombatManager.Instance?.RepairPlayerClothes();
                RefreshWardrobe();
            });
        }

        if (bodySizeButtons != null)
        {
            for (int i = 0; i < bodySizeButtons.Length; i++)
            {
                int sizeIdx = i;
                if (bodySizeButtons[i] == null) continue;
                bodySizeButtons[i].onClick.RemoveAllListeners();
                bodySizeButtons[i].onClick.AddListener(() =>
                {
                    if (FleshTransformationManager.Instance != null)
                    {
                        if (FleshTransformationManager.Instance.characterState == null && characterState != null)
                        {
                            FleshTransformationManager.Instance.Initialize(characterState);
                        }
                        FleshTransformationManager.Instance.StabilizeBodySize((BodySizeType)sizeIdx);
                    }
                    if (characterState != null)
                    {
                        characterState.CurrentBodySize = (BodySizeType)sizeIdx;
                    }
                    RefreshWardrobe();
                });
            }
        }

        if (fleshDebugFillBtn != null)
        {
            fleshDebugFillBtn.onClick.RemoveAllListeners();
            fleshDebugFillBtn.onClick.AddListener(() =>
            {
                if (FleshTransformationManager.Instance != null)
                {
                    FleshTransformationManager.Instance.DebugFillGauge();
                    RefreshWardrobe();
                }
            });
        }

        if (emotionButtons != null)
        {
            for (int i = 0; i < emotionButtons.Length; i++)
            {
                int emoIdx = i;
                if (emotionButtons[i] == null) continue;
                emotionButtons[i].onClick.RemoveAllListeners();
                emotionButtons[i].onClick.AddListener(() =>
                {
                    if (emoIdx < emotionNames.Length)
                    {
                        SelectEmotion(emotionNames[emoIdx]);
                    }
                });
            }
        }

        // 3.5 Deck category filter buttons
        if (filterAllBtn != null)
        {
            filterAllBtn.onClick.RemoveAllListeners();
            filterAllBtn.onClick.AddListener(() => SetDeckCategoryFilter(DeckCategoryFilter.All));
        }

        if (filterTacticsBtn != null)
        {
            filterTacticsBtn.onClick.RemoveAllListeners();
            filterTacticsBtn.onClick.AddListener(() => SetDeckCategoryFilter(DeckCategoryFilter.WarriorTactics));
        }

        if (filterLustBtn != null)
        {
            filterLustBtn.onClick.RemoveAllListeners();
            filterLustBtn.onClick.AddListener(() => SetDeckCategoryFilter(DeckCategoryFilter.LustInstincts));
        }

        if (filterStruggleBtn != null)
        {
            filterStruggleBtn.onClick.RemoveAllListeners();
            filterStruggleBtn.onClick.AddListener(() => SetDeckCategoryFilter(DeckCategoryFilter.StruggleActions));
        }

        // 4. Desire Tree page buttons
        if (treePathPhallusBtn != null)
        {
            treePathPhallusBtn.onClick.RemoveAllListeners();
            treePathPhallusBtn.onClick.AddListener(() => BrowseTreePath(LustPathType.PhallusDevotee));
        }

        if (treePathBoundBtn != null)
        {
            treePathBoundBtn.onClick.RemoveAllListeners();
            treePathBoundBtn.onClick.AddListener(() => BrowseTreePath(LustPathType.BoundSubmissive));
        }

        if (treePathMonsterBtn != null)
        {
            treePathMonsterBtn.onClick.RemoveAllListeners();
            treePathMonsterBtn.onClick.AddListener(() => BrowseTreePath(LustPathType.MonsterTransformation));
        }

        if (treeOverlaySelectBtn != null)
        {
            treeOverlaySelectBtn.onClick.RemoveAllListeners();
            treeOverlaySelectBtn.onClick.AddListener(SelectCurrentBrowsedPathAsActive);
        }

        // 5. Settings overlay controls
        if (settingsMasterSlider != null)
        {
            settingsMasterSlider.onValueChanged.RemoveAllListeners();
            settingsMasterSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
        }

        if (settingsFullscreenToggleBtn != null)
        {
            settingsFullscreenToggleBtn.onClick.RemoveAllListeners();
            settingsFullscreenToggleBtn.onClick.AddListener(ToggleFullscreen);
        }

        if (settingsMainMenuBtn != null)
        {
            settingsMainMenuBtn.onClick.RemoveAllListeners();
            settingsMainMenuBtn.onClick.AddListener(() =>
            {
                CloseActiveOverlay();
                OnReturnToMainMenu?.Invoke();
            });
        }
    }

    private void HandleExitClicked()
    {
        Debug.Log("[LobbyUI] Exit Arch clicked -> Starting Expedition!");
        OnStartExpedition?.Invoke();
    }

    // =========================================================================
    // OVERLAY MANAGEMENT & HEROINE DISPLAY STATE
    // =========================================================================

    public void OpenWardrobeOverlay()
    {
        CloseActiveOverlay(suppressPaperDollHide: true);
        if (wardrobeOverlay != null)
        {
            wardrobeOverlay.SetActive(true);
            currentActiveOverlay = wardrobeOverlay;
        }
        ApplyWoodPanelSkin();

        // Activate PaperDoll on the left side and bring to front of Canvas
        if (paperDoll == null)
        {
            paperDoll = FindFirstObjectByType<PaperDollController>(FindObjectsInactive.Include);
        }

        if (paperDoll != null)
        {
            paperDoll.gameObject.SetActive(true);
            paperDoll.transform.SetAsLastSibling();
            paperDoll.SetExpeditionMode(false);
            paperDoll.SetZoomControlsVisible(true);

            if (characterState == null)
            {
                characterState = FindFirstObjectByType<CharacterState>();
            }

            if (characterState != null)
            {
                paperDoll.Initialize(characterState);
            }
            else
            {
                paperDoll.RefreshBody();
                paperDoll.RefreshEquipment();
                paperDoll.RefreshAppearance();
            }
        }

        RefreshWardrobe();
    }

    public void OpenDeckOverlay()
    {
        CloseActiveOverlay();
        if (deckOverlay != null)
        {
            deckOverlay.SetActive(true);
            currentActiveOverlay = deckOverlay;
        }
        ApplyWoodPanelSkin();
        RefreshDeck();
    }

    public void OpenPathsOverlay()
    {
        CloseActiveOverlay();
        if (pathsOverlay != null)
        {
            pathsOverlay.SetActive(true);
            currentActiveOverlay = pathsOverlay;
        }
        ApplyWoodPanelSkin();
        RefreshDesireTreeUI();
        if (treeScrollRect != null) treeScrollRect.verticalNormalizedPosition = 0f;
    }

    public void OpenSettingsOverlay()
    {
        CloseActiveOverlay();
        if (settingsOverlay != null)
        {
            settingsOverlay.SetActive(true);
            currentActiveOverlay = settingsOverlay;
        }
        ApplyWoodPanelSkin();
        RefreshSettingsUI();
    }

    public void CloseActiveOverlay(bool suppressPaperDollHide = false)
    {
        if (wardrobeOverlay != null) wardrobeOverlay.SetActive(false);
        if (deckOverlay != null) deckOverlay.SetActive(false);
        if (pathsOverlay != null) pathsOverlay.SetActive(false);
        if (settingsOverlay != null) settingsOverlay.SetActive(false);
        currentActiveOverlay = null;

        // In the resting cave view, the Heroine PaperDoll is hidden
        if (!suppressPaperDollHide && paperDoll != null)
        {
            paperDoll.gameObject.SetActive(false);
        }
    }

    public void RefreshSettingsUI()
    {
        float currentVol = PlayerPrefs.GetFloat("Sanctum_MasterVolume", AudioListener.volume);
        if (settingsMasterSlider != null)
        {
            settingsMasterSlider.SetValueWithoutNotify(currentVol);
        }
        if (settingsMasterVolumeText != null)
        {
            settingsMasterVolumeText.text = $"Master Volume: {Mathf.RoundToInt(currentVol * 100f)}%";
        }

        if (settingsFullscreenToggleText != null)
        {
            settingsFullscreenToggleText.text = Screen.fullScreen ? "Fullscreen: [ON]" : "Fullscreen: [OFF]";
        }

        if (settingsOverlay != null)
        {
            Transform oldBack = settingsOverlay.transform.Find("SettingsModalCard/ContentArea/SettingsBackBtn");
            if (oldBack != null)
            {
                Destroy(oldBack.gameObject);
            }
            Transform card = settingsOverlay.transform.Find("SettingsModalCard");
            if (card != null)
            {
                RectTransform crt = card.GetComponent<RectTransform>();
                if (crt != null && crt.sizeDelta.y > 350f)
                {
                    crt.sizeDelta = new Vector2(580f, 340f);
                }
            }
            if (settingsMainMenuBtn != null)
            {
                RectTransform mmrt = settingsMainMenuBtn.GetComponent<RectTransform>();
                if (mmrt != null)
                {
                    mmrt.anchoredPosition = new Vector2(0f, 15f);
                }
            }
        }
    }

    private void HandleMasterVolumeChanged(float val)
    {
        AudioListener.volume = val;
        PlayerPrefs.SetFloat("Sanctum_MasterVolume", val);
        PlayerPrefs.Save();
        if (settingsMasterVolumeText != null)
        {
            settingsMasterVolumeText.text = $"Master Volume: {Mathf.RoundToInt(val * 100f)}%";
        }
    }

    private void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        RefreshSettingsUI();
    }

    // =========================================================================
    // INITIALIZATION & FLOW
    // =========================================================================

    public void Initialize(CharacterState state, InventoryManager inv)
    {
        Initialize(state, inv, null);
    }

    public void Initialize(CharacterState state, InventoryManager inv, PaperDollController doll = null)
    {
        characterState = state;
        inventoryManager = inv;
        if (doll != null) paperDoll = doll;

        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= RefreshWardrobe;
            inventoryManager.OnInventoryChanged += RefreshWardrobe;
        }

        if (characterState != null)
        {
            characterState.OnEquipmentChanged -= RefreshWardrobe;
            characterState.OnEquipmentChanged += RefreshWardrobe;
        }

        SetupListeners();
    }

    public void Show(int maxFloorReached = 1)
    {
        currentMaxFloor = maxFloorReached;
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
            lobbyPanel.transform.SetAsLastSibling();
        }

        // Pure Sanctuary cave state: hero hidden, all overlays closed
        CloseActiveOverlay();
        RefreshLustidDisplay();
        SetupListeners();
    }

    public void Hide()
    {
        CloseActiveOverlay();
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
    }

    public void ShowTab(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0: // Cave hub view
                CloseActiveOverlay();
                break;
            case 1: // Wardrobe
                OpenWardrobeOverlay();
                break;
            case 2: // Deck
                OpenDeckOverlay();
                break;
            case 3: // Desire tree
                OpenPathsOverlay();
                break;
            default:
                CloseActiveOverlay();
                break;
        }
    }

    // =========================================================================
    // WARDROBE & EMOTIONS
    // =========================================================================

    public void SelectEmotion(string emotionName)
    {
        Sprite spr = null;
        if (characterState != null && characterState.appearanceDatabase != null && characterState.appearanceDatabase.emotionStyles != null)
        {
            foreach (var s in characterState.appearanceDatabase.emotionStyles)
            {
                if (s != null && s.name.Equals(emotionName, StringComparison.OrdinalIgnoreCase))
                {
                    spr = s;
                    break;
                }
            }
        }

#if UNITY_EDITOR
        if (spr == null)
        {
            spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/sprites/Expression/Emotions/{emotionName}.png");
        }
#endif

        if (spr != null)
        {
            if (characterState != null)
            {
                characterState.SetExpression(spr);
            }
            PaperDollController pd = paperDoll != null ? paperDoll : FindFirstObjectByType<PaperDollController>();
            if (pd != null && pd.expressionImage != null)
            {
                pd.expressionImage.sprite = spr;
                pd.expressionImage.enabled = true;
            }
        }

        RefreshWardrobe();
    }

    public void RefreshWardrobe()
    {
        ApplyWoodPanelSkin();
        if (characterState == null) return;

        ItemData equippedUpper = characterState.GetEquippedItem(EquipSlot.UpperClothes);
        ItemData equippedLower = characterState.GetEquippedItem(EquipSlot.LowerClothes);
        bool isTorn = (CombatManager.Instance != null && CombatManager.Instance.tornClothesItem != null);

        if (equippedClothesText != null)
        {
            if (equippedUpper != null && equippedUpper == equippedLower && equippedUpper.IsCombinedArmor)
            {
                int uHp = CombatManager.Instance != null ? CombatManager.Instance.persistentUpperClothesHp : equippedUpper.durability;
                int lHp = CombatManager.Instance != null ? CombatManager.Instance.persistentLowerClothesHp : equippedUpper.durability;
                int hp = Mathf.Min(uHp, lHp);
                int dr = equippedUpper.damageReduction;
                equippedClothesText.text = $"{equippedUpper.itemName} (Combined) [{hp}/{equippedUpper.durability} HP | Armor +{dr}]";
            }
            else if (equippedUpper != null || equippedLower != null)
            {
                string uStr = equippedUpper != null ? $"{equippedUpper.itemName} [U: {CombatManager.Instance?.persistentUpperClothesHp ?? equippedUpper.durability}]" : "Top: None";
                string lStr = equippedLower != null ? $"{equippedLower.itemName} [L: {CombatManager.Instance?.persistentLowerClothesHp ?? equippedLower.durability}]" : "Bottom: None";
                equippedClothesText.text = $"{uStr} | {lStr}";
            }
            else if (isTorn)
            {
                equippedClothesText.text = $"{CombatManager.Instance.tornClothesItem.itemName} [Torn! Durability 0]";
            }
            else
            {
                equippedClothesText.text = "None (Nude)";
            }
        }

        if (unequipClothesBtn != null)
        {
            unequipClothesBtn.gameObject.SetActive(equippedUpper != null || equippedLower != null);
        }

        if (repairClothesBtn != null)
        {
            repairClothesBtn.gameObject.SetActive(isTorn);
        }

        // Flesh Stabilization
        FleshTransformationManager ftm = FleshTransformationManager.Instance;
        if (bodySizeButtons != null)
        {
            string[] sizeLabels = { "Small (A-B)", "Medium (C-D)", "Large (D-E)", "XLarge (E-F)" };
            for (int i = 0; i < bodySizeButtons.Length; i++)
            {
                if (i >= sizeLabels.Length || bodySizeButtons[i] == null) continue;
                BodySizeType size = (BodySizeType)i;
                bool unlocked = ftm != null ? ftm.IsSizeUnlocked(size) : true;
                bool active = characterState != null && characterState.CurrentBodySize == size;

                bodySizeButtons[i].interactable = unlocked;

                if (bodySizeButtonBgs != null && i < bodySizeButtonBgs.Length && bodySizeButtonBgs[i] != null)
                {
                    if (woodButtonSprite != null)
                    {
                        bodySizeButtonBgs[i].sprite = woodButtonSprite;
                        bodySizeButtonBgs[i].type = Image.Type.Sliced;
                        if (active)
                            bodySizeButtonBgs[i].color = new Color(1.25f, 1.15f, 0.95f, 1f);
                        else if (unlocked)
                            bodySizeButtonBgs[i].color = new Color(0.70f, 0.68f, 0.65f, 1f);
                        else
                            bodySizeButtonBgs[i].color = new Color(0.35f, 0.33f, 0.32f, 0.50f);
                    }
                    else
                    {
                        if (active)
                            bodySizeButtonBgs[i].color = new Color(0.75f, 0.25f, 0.65f, 1f);
                        else if (unlocked)
                            bodySizeButtonBgs[i].color = new Color(0.18f, 0.22f, 0.30f, 0.90f);
                        else
                            bodySizeButtonBgs[i].color = new Color(0.10f, 0.12f, 0.16f, 0.50f);
                    }
                }

                if (bodySizeButtonTexts != null && i < bodySizeButtonTexts.Length && bodySizeButtonTexts[i] != null)
                {
                    if (active)
                    {
                        bodySizeButtonTexts[i].text = $"[ACTIVE] {sizeLabels[i]}";
                        bodySizeButtonTexts[i].color = new Color(1.0f, 0.92f, 0.60f, 1f);
                    }
                    else if (unlocked)
                    {
                        bodySizeButtonTexts[i].text = sizeLabels[i];
                        bodySizeButtonTexts[i].color = new Color(0.90f, 0.88f, 0.82f, 1f);
                    }
                    else
                    {
                        bodySizeButtonTexts[i].text = $"[LOCKED] {sizeLabels[i]}";
                        bodySizeButtonTexts[i].color = new Color(0.55f, 0.52f, 0.48f, 1f);
                    }
                }
            }
        }

        if (ftm != null)
        {
            if (fleshPranaGaugeText != null)
            {
                string status = ftm.IsTransformationPending ? "<color=#FF77DD>[TRANSFORMATION PRIMED]</color>" : "";
                fleshPranaGaugeText.text = $"Essence Gauge: <color=#FFAAEE>{ftm.currentPranaMl} / {ftm.thresholdMl} ml</color> {status}";
            }

            if (fleshArchiveText != null)
            {
                int slimeLvl = ftm.GetSpeciesContactLevel("slime");
                int succLvl = ftm.GetSpeciesContactLevel("succubus");
                fleshArchiveText.text = $"FLESH ARCHIVE: Slime [Tier {slimeLvl}/3] | Succubus [Tier {succLvl}/3]";
            }
        }

        // Emotions
        if (emotionButtons != null && emotionButtons.Length > 0)
        {
            string currentExpName = characterState != null && characterState.selectedExpression != null
                ? characterState.selectedExpression.name
                : "Normal";

            for (int i = 0; i < emotionButtons.Length; i++)
            {
                if (i >= emotionNames.Length || emotionButtons[i] == null) break;
                string eName = emotionNames[i];
                bool active = currentExpName.Equals(eName, StringComparison.OrdinalIgnoreCase);

                if (emotionButtonBgs != null && i < emotionButtonBgs.Length && emotionButtonBgs[i] != null)
                {
                    if (woodButtonSprite != null)
                    {
                        emotionButtonBgs[i].sprite = woodButtonSprite;
                        emotionButtonBgs[i].type = Image.Type.Sliced;
                        if (active)
                            emotionButtonBgs[i].color = new Color(1.25f, 1.15f, 0.95f, 1f);
                        else
                            emotionButtonBgs[i].color = new Color(0.70f, 0.68f, 0.65f, 1f);
                    }
                    else
                    {
                        if (active)
                            emotionButtonBgs[i].color = new Color(0.75f, 0.25f, 0.65f, 1f);
                        else
                            emotionButtonBgs[i].color = new Color(0.18f, 0.22f, 0.30f, 0.90f);
                    }
                }

                if (emotionButtonTexts != null && i < emotionButtonTexts.Length && emotionButtonTexts[i] != null)
                {
                    string label = emotionNames[i];
                    if (label.Equals("smile", StringComparison.OrdinalIgnoreCase)) label = "Smile";
                    else if (label.Equals("shocked", StringComparison.OrdinalIgnoreCase)) label = "Shocked";

                    emotionButtonTexts[i].text = active ? $"[{label}]" : label;
                    emotionButtonTexts[i].color = active ? new Color(1.0f, 0.92f, 0.60f, 1f) : new Color(0.85f, 0.82f, 0.75f, 1f);
                }
            }
        }

        // Wardrobe Items List
        if (wardrobeItemsContainer != null)
        {
            foreach (Transform child in wardrobeItemsContainer) Destroy(child.gameObject);

            if (inventoryManager != null)
            {
                foreach (var item in inventoryManager.Items)
                {
                    if (item == null || !item.IsArmor) continue;
                    CreateInventoryItemRow(item, characterState.IsEquipped(item));
                }
            }
        }
    }

    private void CreateInventoryItemRow(ItemData item, bool isEquipped)
    {
        GameObject row = new GameObject($"Item_{item.itemName}", typeof(RectTransform));
        row.transform.SetParent(wardrobeItemsContainer, false);

        RectTransform rr = row.GetComponent<RectTransform>();
        rr.sizeDelta = new Vector2(0f, 116f);

        if (woodSlotSprite == null)
        {
            woodSlotSprite = LoadUISprite("ui_slot_wood.png");
        }

        Image bg = row.AddComponent<Image>();
        if (woodSlotSprite != null)
        {
            bg.sprite = woodSlotSprite;
            bg.type = Image.Type.Sliced;
            bg.color = isEquipped ? new Color(1.1f, 1.05f, 0.95f, 1f) : new Color(0.92f, 0.92f, 0.92f, 1f);
        }
        else
        {
            bg.color = isEquipped ? new Color(0.18f, 0.22f, 0.30f, 0.90f) : new Color(0.12f, 0.14f, 0.18f, 0.85f);
        }

        if (isEquipped)
        {
            Outline ol = row.AddComponent<Outline>();
            ol.effectColor = new Color(0.85f, 0.70f, 0.35f, 0.70f);
            ol.effectDistance = new Vector2(1.5f, -1.5f);
        }

        string slotLabel = item.IsCombinedArmor ? "Combined Armor" : (item.IsUpperArmor ? "Upper Armor" : (item.IsLowerArmor ? "Lower Armor" : item.equipSlot.ToString()));
        var statsList = new List<string>();
        statsList.Add($"Dur: {item.durability}");
        if (item.damageReduction != 0) statsList.Add($"Armor {(item.damageReduction > 0 ? "+" : "")}{item.damageReduction}");
        if (item.bonusMaxHp != 0) statsList.Add($"HP {(item.bonusMaxHp > 0 ? "+" : "")}{item.bonusMaxHp}");
        if (item.bonusMaxMana != 0) statsList.Add($"Mana {(item.bonusMaxMana > 0 ? "+" : "")}{item.bonusMaxMana}");
        if (item.bonusSeductionPower != 0) statsList.Add($"Sed {(item.bonusSeductionPower > 0 ? "+" : "")}{item.bonusSeductionPower}%");
        if (item.seductionResistance != 0) statsList.Add($"Res {(item.seductionResistance > 0 ? "+" : "")}{item.seductionResistance}%");
        string statsSummary = string.Join(" | ", statsList);

        GameObject nameObj = new GameObject("Name", typeof(RectTransform));
        nameObj.transform.SetParent(row.transform, false);
        RectTransform namert = nameObj.GetComponent<RectTransform>();
        namert.anchorMin = Vector2.zero;
        namert.anchorMax = Vector2.one;
        namert.offsetMin = new Vector2(32f, 0f);
        namert.offsetMax = new Vector2(-210f, 0f);

        TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
        nameTxt.text = $"{item.itemName} ({slotLabel}) [{statsSummary}]";
        nameTxt.fontSize = 17;
        nameTxt.alignment = TextAlignmentOptions.MidlineLeft;
        nameTxt.color = isEquipped ? new Color(1.0f, 0.92f, 0.70f, 1f) : Color.white;
        nameTxt.raycastTarget = false;

        GameObject btnObj = new GameObject("EquipBtn", typeof(RectTransform));
        btnObj.transform.SetParent(row.transform, false);
        RectTransform brt = btnObj.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(1f, 0.5f);
        brt.anchorMax = new Vector2(1f, 0.5f);
        brt.pivot = new Vector2(1f, 0.5f);
        brt.anchoredPosition = new Vector2(-28f, 0f);
        brt.sizeDelta = new Vector2(160f, 44f);

        Image btnBg = btnObj.AddComponent<Image>();

        if (woodButtonSprite == null)
        {
            woodButtonSprite = LoadUISprite("ui_button.png");
        }

        if (woodProgressFrameSprite == null)
        {
            woodProgressFrameSprite = LoadUISprite("ui_progressbar_frame.png");
        }

        if (woodButtonSprite != null)
        {
            btnBg.sprite = woodButtonSprite;
            btnBg.type = Image.Type.Sliced;
            btnBg.color = isEquipped ? new Color(0.92f, 0.65f, 0.65f, 1f) : new Color(0.75f, 0.95f, 0.80f, 1f);
        }
        else
        {
            btnBg.color = isEquipped ? new Color(0.55f, 0.25f, 0.25f, 1f) : new Color(0.25f, 0.55f, 0.35f, 1f);
        }

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        ColorBlock btnCb = btn.colors;
        btnCb.highlightedColor = new Color(1.22f, 1.18f, 1.08f, 1f);
        btnCb.pressedColor = new Color(0.70f, 0.65f, 0.60f, 1f);
        btn.colors = btnCb;

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = isEquipped ? "Unequip" : "Equip";
        btnTxt.fontSize = 16;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color = isEquipped ? new Color(1f, 0.88f, 0.88f, 1f) : new Color(0.90f, 1f, 0.90f, 1f);
        btnTxt.raycastTarget = false;

        RectTransform btr = btnTxtObj.GetComponent<RectTransform>();
        btr.anchorMin = Vector2.zero;
        btr.anchorMax = Vector2.one;
        btr.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(() =>
        {
            if (isEquipped)
                characterState.UnequipItem(item.equipSlot);
            else
                characterState.EquipItem(item);

            RefreshWardrobe();
        });
    }

    // =========================================================================
    // DECKBUILDER
    // =========================================================================

    public void SetDeckCategoryFilter(DeckCategoryFilter filter)
    {
        currentDeckCategoryFilter = filter;
        UpdateFilterButtonsUI();
        RefreshDeck();
    }

    public void UpdateFilterButtonsUI()
    {
        if (woodButtonSprite == null) woodButtonSprite = LoadUISprite("ui_button.png");

        Color activeBg = (woodButtonSprite != null) ? new Color(1.25f, 1.15f, 0.95f, 1f) : new Color(0.68f, 0.54f, 0.28f, 0.95f);
        Color inactiveBg = (woodButtonSprite != null) ? new Color(0.65f, 0.65f, 0.65f, 1f) : new Color(0.12f, 0.14f, 0.18f, 0.85f);
        Color activeTxt = new Color(1.0f, 0.92f, 0.65f, 1f);
        Color inactiveTxt = new Color(0.85f, 0.82f, 0.75f, 1f);

        Image[] bgs = { filterAllBg, filterTacticsBg, filterLustBg, filterStruggleBg };
        foreach (var bg in bgs)
        {
            if (bg != null && woodButtonSprite != null && bg.sprite != woodButtonSprite)
            {
                bg.sprite = woodButtonSprite;
                bg.type = Image.Type.Sliced;
            }
        }

        if (filterAllBg != null) filterAllBg.color = (currentDeckCategoryFilter == DeckCategoryFilter.All) ? activeBg : inactiveBg;
        if (filterAllText != null) filterAllText.color = (currentDeckCategoryFilter == DeckCategoryFilter.All) ? activeTxt : inactiveTxt;

        if (filterTacticsBg != null) filterTacticsBg.color = (currentDeckCategoryFilter == DeckCategoryFilter.WarriorTactics) ? activeBg : inactiveBg;
        if (filterTacticsText != null) filterTacticsText.color = (currentDeckCategoryFilter == DeckCategoryFilter.WarriorTactics) ? activeTxt : inactiveTxt;

        if (filterLustBg != null) filterLustBg.color = (currentDeckCategoryFilter == DeckCategoryFilter.LustInstincts) ? activeBg : inactiveBg;
        if (filterLustText != null) filterLustText.color = (currentDeckCategoryFilter == DeckCategoryFilter.LustInstincts) ? activeTxt : inactiveTxt;

        if (filterStruggleBg != null) filterStruggleBg.color = (currentDeckCategoryFilter == DeckCategoryFilter.StruggleActions) ? activeBg : inactiveBg;
        if (filterStruggleText != null) filterStruggleText.color = (currentDeckCategoryFilter == DeckCategoryFilter.StruggleActions) ? activeTxt : inactiveTxt;
    }

    public void RefreshDeck()
    {
        if (CombatManager.Instance == null) return;
        var cm = CombatManager.Instance;
        UpdateFilterButtonsUI();

        HashSet<string> equippedCardIds = new HashSet<string>();
        if (cm.baseWarriorDeck != null)
        {
            for (int i = cm.baseWarriorDeck.Count - 1; i >= 0; i--)
            {
                if (cm.baseWarriorDeck[i] == null || !equippedCardIds.Add(cm.baseWarriorDeck[i].id))
                {
                    cm.baseWarriorDeck.RemoveAt(i);
                }
            }
        }

        if (deckCountText != null)
        {
            deckCountText.text = $"Active Deck: {cm.baseWarriorDeck.Count} / {CombatManager.MAX_DECK_SIZE} Cards";
        }

        List<CardData> availableCards = new List<CardData>();
        HashSet<string> seenAvailableIds = new HashSet<string>();
        if (cm.unlockedCards != null)
        {
            for (int i = 0; i < cm.unlockedCards.Count; i++)
            {
                CardData cd = cm.unlockedCards[i];
                if (cd != null && !equippedCardIds.Contains(cd.id) && seenAvailableIds.Add(cd.id))
                {
                    if (currentDeckCategoryFilter == DeckCategoryFilter.WarriorTactics && cd.category != CardCategory.WarriorTactics)
                        continue;
                    if (currentDeckCategoryFilter == DeckCategoryFilter.LustInstincts && cd.category != CardCategory.LustInstinct)
                        continue;
                    if (currentDeckCategoryFilter == DeckCategoryFilter.StruggleActions && cd.category != CardCategory.StruggleAction)
                        continue;

                    if (cd.category == CardCategory.LustInstinct)
                    {
                        if (cd.requiredPath != LustPathType.None && cd.requiredPath != cm.activeLustPath)
                            continue;
                    }
                    availableCards.Add(cd);
                }
            }
        }

        if (selectedCollectionCard == null || (!availableCards.Contains(selectedCollectionCard) && !equippedCardIds.Contains(selectedCollectionCard.id)))
        {
            if (availableCards.Count > 0)
                selectedCollectionCard = availableCards[0];
            else if (cm.baseWarriorDeck.Count > 0 && cm.baseWarriorDeck[0] != null)
                selectedCollectionCard = cm.baseWarriorDeck[0];
            else
                selectedCollectionCard = null;
        }

        if (selectedCollectionCard != null)
        {
            UpdateCardDetailPanel(selectedCollectionCard);
        }

        if (availableCardsContainer != null)
        {
            foreach (Transform child in availableCardsContainer) Destroy(child.gameObject);

            for (int i = 0; i < availableCards.Count; i++)
            {
                CardData cd = availableCards[i];
                bool isSelected = (cd == selectedCollectionCard);
                CreateVisualCard(
                    availableCardsContainer,
                    cd,
                    new Vector2(118f, 165f),
                    isEquippedDeckCard: false,
                    deckIndex: -1,
                    isSelected: isSelected,
                    onClick: () =>
                    {
                        selectedCollectionCard = cd;
                        RefreshDeck();
                    }
                );
            }
        }

        if (activeDeckContainer != null)
        {
            foreach (Transform child in activeDeckContainer) Destroy(child.gameObject);

            for (int i = 0; i < cm.baseWarriorDeck.Count; i++)
            {
                int index = i;
                CardData cardData = cm.baseWarriorDeck[i];
                bool isSelected = (cardData == selectedCollectionCard);
                CreateVisualCard(
                    activeDeckContainer,
                    cardData,
                    new Vector2(102f, 148f),
                    isEquippedDeckCard: true,
                    deckIndex: index,
                    isSelected: isSelected,
                    onClick: () =>
                    {
                        selectedCollectionCard = cardData;
                        RefreshDeck();
                    }
                );
            }

            for (int s = cm.baseWarriorDeck.Count; s < CombatManager.MAX_DECK_SIZE; s++)
            {
                CreateEmptySlotCard(activeDeckContainer, new Vector2(102f, 148f));
            }
        }
    }

    public void UpdateCardDetailPanel(CardData card)
    {
        if (card == null) return;
        selectedCollectionCard = card;

        if (detailCardNameText != null)
        {
            detailCardNameText.text = card.cardName;
        }

        if (detailCardArtImg != null)
        {
            Sprite art = card.cardArt != null ? card.cardArt : SkillArtDatabase.GetSkillSprite(card.id);
            if (art != null)
            {
                detailCardArtImg.sprite = art;
                detailCardArtImg.color = Color.white;
            }
            else
            {
                detailCardArtImg.color = Color.black;
            }
        }

        if (detailStatsRow1 != null)
        {
            foreach (Transform child in detailStatsRow1) Destroy(child.gameObject);
            CreateStatBadge(detailStatsRow1, StatIconDatabase.ApIcon, $"{card.apCost} AP", new Color(0.96f, 0.82f, 0.27f, 1f));
            CreateStatBadge(detailStatsRow1, StatIconDatabase.ManaIcon, $"{card.cooldownTurns}T CD", new Color(0.44f, 0.78f, 0.96f, 1f));
            string targetStr = card.target == CardTarget.SingleEnemy ? "Single" : (card.target == CardTarget.Self ? "Self" : "All");
            CreateStatBadge(detailStatsRow1, null, targetStr, new Color(0.77f, 0.63f, 0.96f, 1f));
        }

        if (detailStatsRow2 != null)
        {
            foreach (Transform child in detailStatsRow2) Destroy(child.gameObject);

            if (card.hpDamage > 0)
                CreateStatBadge(detailStatsRow2, StatIconDatabase.HpIcon, $"{card.hpDamage} DMG", new Color(1f, 0.37f, 0.37f, 1f));
            if (card.staminaDamage > 0)
                CreateStatBadge(detailStatsRow2, null, $"{card.staminaDamage} STM", new Color(1f, 0.65f, 0.25f, 1f));
            if (card.shieldGain > 0)
                CreateStatBadge(detailStatsRow2, StatIconDatabase.BlockIcon, $"+{card.shieldGain} SHD", new Color(0.29f, 0.70f, 1f, 1f));
            if (card.lustShieldGain > 0)
                CreateStatBadge(detailStatsRow2, StatIconDatabase.LustIcon, $"+{card.lustShieldGain} CUM SHD", new Color(0.95f, 0.45f, 0.90f, 1f));
            if (card.semenAbsorbedMl > 0)
                CreateStatBadge(detailStatsRow2, null, $"+{card.semenAbsorbedMl}ml CUM", new Color(0.98f, 0.95f, 0.85f, 1f));
            if (card.seductionAmount > 0 || card.arousalTargetDelta > 0)
            {
                int sed = card.arousalTargetDelta > 0 ? card.arousalTargetDelta : card.seductionAmount;
                CreateStatBadge(detailStatsRow2, StatIconDatabase.LustIcon, $"+{sed}% LUST", new Color(1f, 0.43f, 0.71f, 1f));
            }
            if (card.arousalSelfDelta != 0)
            {
                string sign = card.arousalSelfDelta > 0 ? "+" : "";
                CreateStatBadge(detailStatsRow2, null, $"{sign}{card.arousalSelfDelta}% SELF", new Color(0.90f, 0.55f, 0.80f, 1f));
            }
            if (card.clothesRepair > 0)
                CreateStatBadge(detailStatsRow2, StatIconDatabase.ArmorIcon, $"+{card.clothesRepair} CLO", new Color(0.36f, 0.88f, 0.52f, 1f));
            if (card.requiredTargetSlot != PenetrationSlot.None)
                CreateStatBadge(detailStatsRow2, null, $"[{card.requiredTargetSlot.ToString().ToUpper()}]", new Color(0.40f, 0.85f, 1f, 1f));
        }

        if (detailCardDescText != null)
        {
            if (card.category == CardCategory.LustInstinct)
            {
                detailCardDescText.text = $"{card.description}\n\n<color=#FF77DD><b>• DESIRE INSTINCT RULE:</b></color>\n<size=18><color=#DDC0E8>Automatically drawn into your hand based on Lust Stages:\n• <b>Stage 0:</b> 0 Lust cards drawn (Calm composure)\n• <b>Stage 1:</b> +1 Lust card drawn in combat hand\n• <b>Stage 2:</b> +2 Lust cards drawn (Lust cards cost -1 AP)\n• <b>Stage 3:</b> Climax Defeat (Ecstatic overload)</color></size>";
            }
            else if (card.category == CardCategory.StruggleAction)
            {
                detailCardDescText.text = $"{card.description}\n\n<color=#F5A623><b>• STRUGGLE ACTION RULE:</b></color>\n<size=18><color=#E8D5C0>Automatically replaces weapon strikes in your hand while trapped in Grapple or Penetration holds.</color></size>";
            }
            else
            {
                detailCardDescText.text = card.description;
            }
        }

        if (detailEquipBtn != null)
        {
            var dtm = DesireTreeManager.Instance ?? DesireTreeManager.EnsureExists();
            if (card.category == CardCategory.LustInstinct)
            {
                detailEquipBtn.interactable = false;
                bool isUnlockedInTree = dtm.IsCardUnlocked(card.id);
                if (isUnlockedInTree)
                {
                    if (detailEquipBtnText != null) detailEquipBtnText.text = "DESIRE INSTINCT (Drawn at Stage 1 & 2)";
                    if (detailEquipBtnBg != null) detailEquipBtnBg.color = new Color(0.55f, 0.22f, 0.50f, 0.85f);
                }
                else
                {
                    if (detailEquipBtnText != null) detailEquipBtnText.text = "LOCKED IN SACRED TREE (Awaken Node)";
                    if (detailEquipBtnBg != null) detailEquipBtnBg.color = new Color(0.30f, 0.25f, 0.35f, 0.70f);
                }
            }
            else if (card.category == CardCategory.StruggleAction)
            {
                detailEquipBtn.interactable = false;
                if (detailEquipBtnText != null) detailEquipBtnText.text = "STRUGGLE ACTION (Drawn in Hold)";
                if (detailEquipBtnBg != null) detailEquipBtnBg.color = new Color(0.50f, 0.32f, 0.18f, 0.85f);
            }
            else
            {
                bool isAlreadyInDeck = false;
                if (CombatManager.Instance != null && CombatManager.Instance.baseWarriorDeck != null)
                {
                    for (int i = 0; i < CombatManager.Instance.baseWarriorDeck.Count; i++)
                    {
                        if (CombatManager.Instance.baseWarriorDeck[i]?.id == card.id)
                        {
                            isAlreadyInDeck = true;
                            break;
                        }
                    }
                }

                bool isDeckFull = (CombatManager.Instance != null && CombatManager.Instance.baseWarriorDeck.Count >= CombatManager.MAX_DECK_SIZE);

                if (woodButtonSprite == null) woodButtonSprite = LoadUISprite("ui_button.png");
                if (detailEquipBtnBg != null && woodButtonSprite != null && detailEquipBtnBg.sprite != woodButtonSprite)
                {
                    detailEquipBtnBg.sprite = woodButtonSprite;
                    detailEquipBtnBg.type = Image.Type.Sliced;
                }

                if (isAlreadyInDeck)
                {
                    detailEquipBtn.interactable = false;
                    if (detailEquipBtnText != null)
                    {
                        detailEquipBtnText.text = "EQUIPPED IN DECK";
                        detailEquipBtnText.color = new Color(0.65f, 0.65f, 0.65f, 1f);
                    }
                    if (detailEquipBtnBg != null) detailEquipBtnBg.color = (woodButtonSprite != null) ? new Color(0.35f, 0.33f, 0.32f, 0.5f) : new Color(0.35f, 0.35f, 0.35f, 0.6f);
                }
                else if (isDeckFull)
                {
                    detailEquipBtn.interactable = false;
                    if (detailEquipBtnText != null)
                    {
                        detailEquipBtnText.text = "DECK FULL (9/9)";
                        detailEquipBtnText.color = new Color(0.65f, 0.65f, 0.65f, 1f);
                    }
                    if (detailEquipBtnBg != null) detailEquipBtnBg.color = (woodButtonSprite != null) ? new Color(0.35f, 0.33f, 0.32f, 0.5f) : new Color(0.35f, 0.35f, 0.35f, 0.6f);
                }
                else
                {
                    detailEquipBtn.interactable = true;
                    if (detailEquipBtnText != null)
                    {
                        detailEquipBtnText.text = "ADD TO DECK";
                        detailEquipBtnText.color = new Color(0.92f, 1.0f, 0.92f, 1f);
                    }
                    if (detailEquipBtnBg != null) detailEquipBtnBg.color = (woodButtonSprite != null) ? new Color(0.78f, 0.95f, 0.80f, 1f) : new Color(0.22f, 0.55f, 0.35f, 1f);
                }
            }
        }
    }

    private void CreateStatBadge(Transform parent, Sprite icon, string text, Color textColor)
    {
        GameObject badgeObj = new GameObject("Badge", typeof(RectTransform));
        badgeObj.transform.SetParent(parent, false);

        Image bg = badgeObj.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.12f, 0.16f, 0.90f);
        bg.raycastTarget = false;

        HorizontalLayoutGroup hlg = badgeObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 6f;
        hlg.padding = new RectOffset(8, 8, 3, 3);
        hlg.childAlignment = TextAnchor.MiddleCenter;

        ContentSizeFitter csf = badgeObj.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (icon != null)
        {
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(badgeObj.transform, false);
            Image img = iconObj.AddComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
            RectTransform irt = iconObj.GetComponent<RectTransform>();
            irt.sizeDelta = new Vector2(18f, 18f);
        }

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(badgeObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = 12;
        txt.fontStyle = FontStyles.Bold;
        txt.color = textColor;
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;

        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.sizeDelta = new Vector2(txt.preferredWidth + 4f, 20f);
    }

    private GameObject CreateVisualCard(
        Transform parent,
        CardData card,
        Vector2 size,
        bool isEquippedDeckCard,
        int deckIndex,
        bool isSelected,
        Action onClick)
    {
        var dtm = DesireTreeManager.Instance ?? DesireTreeManager.EnsureExists();
        bool isTreeLocked = (card.category == CardCategory.LustInstinct && !dtm.IsCardUnlocked(card.id));

        GameObject cardObj = new GameObject($"Card_{card.id}", typeof(RectTransform));
        cardObj.transform.SetParent(parent, false);

        RectTransform crt = cardObj.GetComponent<RectTransform>();
        crt.sizeDelta = size;

        Image hitImg = cardObj.AddComponent<Image>();
        hitImg.color = Color.clear;
        hitImg.raycastTarget = true;

        Button btn = cardObj.AddComponent<Button>();
        btn.targetGraphic = hitImg;
        btn.transition = Selectable.Transition.None;
        if (onClick != null)
        {
            btn.onClick.AddListener(() => onClick.Invoke());
        }

        GameObject restingBorderObj = new GameObject("RestingBorder", typeof(RectTransform));
        restingBorderObj.transform.SetParent(cardObj.transform, false);
        Image restingBorder = restingBorderObj.AddComponent<Image>();
        Color borderTint;
        if (card.category == CardCategory.LustInstinct)
            borderTint = new Color(0.88f, 0.28f, 0.88f, 0.95f);
        else if (card.category == CardCategory.StruggleAction)
            borderTint = new Color(0.95f, 0.55f, 0.15f, 0.95f);
        else
            borderTint = new Color(0.68f, 0.54f, 0.28f, 0.95f);
        restingBorder.color = borderTint;
        restingBorder.raycastTarget = false;
        RectTransform rbrt = restingBorderObj.GetComponent<RectTransform>();
        rbrt.anchorMin = Vector2.zero;
        rbrt.anchorMax = Vector2.one;
        rbrt.offsetMin = new Vector2(-2f, -2f);
        rbrt.offsetMax = new Vector2(2f, 2f);

        GameObject selectBorderObj = new GameObject("SelectBorder", typeof(RectTransform));
        selectBorderObj.transform.SetParent(cardObj.transform, false);
        Image selectBorder = selectBorderObj.AddComponent<Image>();
        selectBorder.color = new Color(1f, 0.88f, 0.30f, 1f);
        selectBorder.raycastTarget = false;
        RectTransform sbrt = selectBorderObj.GetComponent<RectTransform>();
        sbrt.anchorMin = Vector2.zero;
        sbrt.anchorMax = Vector2.one;
        sbrt.offsetMin = new Vector2(-3.5f, -3.5f);
        sbrt.offsetMax = new Vector2(3.5f, 3.5f);
        selectBorderObj.SetActive(isSelected);

        GameObject bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.transform.SetParent(cardObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        RectTransform bgrt = bgObj.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;

        if (cardFrameSprite == null)
        {
            cardFrameSprite = LoadUISprite("ui_card_frame.png");
        }

        if (cardFrameSprite != null)
        {
            bg.sprite = cardFrameSprite;
            bg.color = isTreeLocked ? new Color(0.55f, 0.55f, 0.60f, 1f) : Color.white;
            restingBorderObj.SetActive(false);
        }
        else
        {
            bg.color = Color.black;
            restingBorderObj.SetActive(true);
        }
        bg.raycastTarget = false;

        GameObject artWindow = new GameObject("ArtWindow", typeof(RectTransform));
        artWindow.transform.SetParent(cardObj.transform, false);
        artWindow.AddComponent<RectMask2D>();
        RectTransform winRt = artWindow.GetComponent<RectTransform>();

        if (cardFrameSprite != null)
        {
            winRt.anchorMin = new Vector2(0.06f, 0.33f);
            winRt.anchorMax = new Vector2(0.94f, 0.94f);
            winRt.offsetMin = Vector2.zero;
            winRt.offsetMax = Vector2.zero;

            Image winBg = artWindow.AddComponent<Image>();
            winBg.color = new Color(0.05f, 0.05f, 0.07f, 1f);
            winBg.raycastTarget = false;
        }
        else
        {
            winRt.anchorMin = Vector2.zero;
            winRt.anchorMax = Vector2.one;
            winRt.offsetMin = new Vector2(3f, 27f);
            winRt.offsetMax = new Vector2(-3f, -3f);
        }

        GameObject artObj = new GameObject("Art", typeof(RectTransform));
        artObj.transform.SetParent(artWindow.transform, false);
        Image artImg = artObj.AddComponent<Image>();
        artImg.preserveAspect = true;
        artImg.raycastTarget = false;
        RectTransform artRt = artObj.GetComponent<RectTransform>();
        artRt.anchorMin = Vector2.zero;
        artRt.anchorMax = Vector2.one;
        artRt.offsetMin = Vector2.zero;
        artRt.offsetMax = Vector2.zero;

        Sprite art = card.cardArt != null ? card.cardArt : SkillArtDatabase.GetSkillSprite(card.id);
        if (art != null)
        {
            artImg.sprite = art;
            artImg.color = isTreeLocked ? new Color(0.40f, 0.42f, 0.50f, 0.65f) : Color.white;
        }
        else
        {
            artImg.color = Color.black;
        }

        GameObject sepObj = new GameObject("NameSeparator", typeof(RectTransform));
        sepObj.transform.SetParent(cardObj.transform, false);
        Image sepImg = sepObj.AddComponent<Image>();
        sepImg.color = isTreeLocked ? new Color(0.35f, 0.35f, 0.45f, 0.8f) : borderTint;
        sepImg.raycastTarget = false;
        RectTransform sepRt = sepObj.GetComponent<RectTransform>();
        sepRt.anchorMin = new Vector2(0f, 0f);
        sepRt.anchorMax = new Vector2(1f, 0f);
        sepRt.pivot = new Vector2(0.5f, 0f);
        sepRt.anchoredPosition = new Vector2(0f, 25f);
        sepRt.sizeDelta = new Vector2(0f, 1.5f);
        sepObj.SetActive(cardFrameSprite == null);

        GameObject namePlate = new GameObject("NamePlate", typeof(RectTransform));
        namePlate.transform.SetParent(cardObj.transform, false);
        Image nameBg = namePlate.AddComponent<Image>();
        nameBg.raycastTarget = false;
        RectTransform nprt = namePlate.GetComponent<RectTransform>();

        if (cardFrameSprite != null)
        {
            nameBg.color = Color.clear;
            nprt.anchorMin = new Vector2(0.14f, 0.08f);
            nprt.anchorMax = new Vector2(0.86f, 0.28f);
            nprt.offsetMin = Vector2.zero;
            nprt.offsetMax = Vector2.zero;
        }
        else
        {
            nameBg.color = new Color(0.04f, 0.04f, 0.06f, 1f);
            nprt.anchorMin = new Vector2(0f, 0f);
            nprt.anchorMax = new Vector2(1f, 0f);
            nprt.pivot = new Vector2(0.5f, 0f);
            nprt.anchoredPosition = Vector2.zero;
            nprt.sizeDelta = new Vector2(0f, 26f);
        }

        GameObject nameTxtObj = new GameObject("Text", typeof(RectTransform));
        nameTxtObj.transform.SetParent(namePlate.transform, false);
        TextMeshProUGUI nameTxt = nameTxtObj.AddComponent<TextMeshProUGUI>();
        nameTxt.text = isTreeLocked ? $"{card.cardName}\n<size=7><color=#8895B0>[LOCKED]</color></size>" : card.cardName;
        nameTxt.enableAutoSizing = true;
        nameTxt.fontSizeMin = 7f;
        nameTxt.fontSizeMax = isTreeLocked ? 9.5f : 11f;
        nameTxt.lineSpacing = -8;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.color = isTreeLocked ? new Color(0.70f, 0.72f, 0.80f, 0.90f) : new Color(0.96f, 0.94f, 0.90f, 1f);
        nameTxt.raycastTarget = false;
        RectTransform ntr = nameTxtObj.GetComponent<RectTransform>();
        ntr.anchorMin = Vector2.zero;
        ntr.anchorMax = Vector2.one;
        ntr.sizeDelta = Vector2.zero;

        GameObject apBadge = new GameObject("ApBadge", typeof(RectTransform));
        apBadge.transform.SetParent(cardObj.transform, false);
        Image apRim = apBadge.AddComponent<Image>();
        apRim.color = borderTint;
        apRim.raycastTarget = false;
        RectTransform aprt = apBadge.GetComponent<RectTransform>();
        aprt.anchorMin = new Vector2(0f, 1f);
        aprt.anchorMax = new Vector2(0f, 1f);
        aprt.pivot = new Vector2(0f, 1f);
        aprt.anchoredPosition = new Vector2(3f, -3f);
        aprt.sizeDelta = new Vector2(22f, 22f);

        GameObject apInner = new GameObject("Inner", typeof(RectTransform));
        apInner.transform.SetParent(apBadge.transform, false);
        Image apBg = apInner.AddComponent<Image>();
        apBg.color = new Color(0.95f, 0.74f, 0.18f, 1f);
        apBg.raycastTarget = false;
        RectTransform apirt = apInner.GetComponent<RectTransform>();
        apirt.anchorMin = Vector2.zero;
        apirt.anchorMax = Vector2.one;
        apirt.offsetMin = new Vector2(1.5f, 1.5f);
        apirt.offsetMax = new Vector2(-1.5f, -1.5f);

        GameObject apTxtObj = new GameObject("Text", typeof(RectTransform));
        apTxtObj.transform.SetParent(apInner.transform, false);
        TextMeshProUGUI apTxt = apTxtObj.AddComponent<TextMeshProUGUI>();
        apTxt.text = $"{card.apCost}";
        apTxt.fontSize = 14;
        apTxt.fontStyle = FontStyles.Bold;
        apTxt.alignment = TextAlignmentOptions.Center;
        apTxt.color = Color.black;
        apTxt.raycastTarget = false;
        RectTransform aptr = apTxtObj.GetComponent<RectTransform>();
        aptr.anchorMin = Vector2.zero;
        aptr.anchorMax = Vector2.one;
        aptr.sizeDelta = Vector2.zero;

        if (isEquippedDeckCard)
        {
            GameObject unequipObj = new GameObject("UnequipCross", typeof(RectTransform));
            unequipObj.transform.SetParent(cardObj.transform, false);
            Image unequipBg = unequipObj.AddComponent<Image>();
            unequipBg.color = new Color(0.85f, 0.20f, 0.22f, 1f);

            Button unequipBtn = unequipObj.AddComponent<Button>();
            unequipBtn.targetGraphic = unequipBg;
            unequipBtn.onClick.AddListener(() =>
            {
                if (CombatManager.Instance != null)
                {
                    CombatManager.Instance.RemoveCardFromDeck(deckIndex);
                    RefreshDeck();
                }
            });

            RectTransform urt = unequipObj.GetComponent<RectTransform>();
            urt.anchorMin = new Vector2(1f, 1f);
            urt.anchorMax = new Vector2(1f, 1f);
            urt.pivot = new Vector2(0.5f, 0.5f);
            urt.anchoredPosition = new Vector2(-2f, -2f);
            urt.sizeDelta = new Vector2(24f, 24f);

            GameObject crossTxtObj = new GameObject("Text", typeof(RectTransform));
            crossTxtObj.transform.SetParent(unequipObj.transform, false);
            TextMeshProUGUI crossTxt = crossTxtObj.AddComponent<TextMeshProUGUI>();
            crossTxt.text = "X";
            crossTxt.fontSize = 15;
            crossTxt.fontStyle = FontStyles.Bold;
            crossTxt.alignment = TextAlignmentOptions.Center;
            crossTxt.color = Color.white;
            crossTxt.raycastTarget = false;
            RectTransform ctr = crossTxtObj.GetComponent<RectTransform>();
            ctr.anchorMin = Vector2.zero;
            ctr.anchorMax = Vector2.one;
            ctr.sizeDelta = Vector2.zero;

            unequipObj.SetActive(false);

            DeckCardHoverUI hover = cardObj.AddComponent<DeckCardHoverUI>();
            hover.removeButton = unequipObj;
            hover.focusBorder = selectBorderObj;
        }

        return cardObj;
    }

    private GameObject CreateEmptySlotCard(Transform parent, Vector2 size)
    {
        GameObject slotObj = new GameObject("EmptySlot", typeof(RectTransform));
        slotObj.transform.SetParent(parent, false);

        RectTransform srt = slotObj.GetComponent<RectTransform>();
        srt.sizeDelta = size;

        if (woodSlotSprite == null)
        {
            woodSlotSprite = LoadUISprite("ui_slot_wood.png");
        }

        GameObject borderObj = new GameObject("Border", typeof(RectTransform));
        borderObj.transform.SetParent(slotObj.transform, false);
        Image bImg = borderObj.AddComponent<Image>();
        bImg.color = new Color(0.40f, 0.35f, 0.22f, 0.40f);
        bImg.raycastTarget = false;
        RectTransform brt = borderObj.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(-1.5f, -1.5f);
        brt.offsetMax = new Vector2(1.5f, 1.5f);

        GameObject bgObj = new GameObject("Bg", typeof(RectTransform));
        bgObj.transform.SetParent(slotObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        if (woodSlotSprite != null)
        {
            bg.sprite = woodSlotSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.75f, 0.75f, 0.75f, 0.85f);
            bImg.enabled = false;
        }
        else
        {
            bg.color = new Color(0.06f, 0.08f, 0.11f, 0.45f);
        }
        bg.raycastTarget = false;
        RectTransform bgrt = bgObj.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(slotObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = "+ Empty\nSlot";
        txt.fontSize = 13;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.60f, 0.55f, 0.45f, 0.55f);
        txt.raycastTarget = false;
        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;

        return slotObj;
    }

    // =========================================================================
    // DESIRE TREE (PATHS OF DESIRE)
    // =========================================================================

    public void BrowseTreePath(LustPathType path)
    {
        selectedTreePath = path;
        selectedTreeNode = null;
        RefreshDesireTreeUI();
        if (treeScrollRect != null) treeScrollRect.verticalNormalizedPosition = 0f;
    }

    public void SelectCurrentBrowsedPathAsActive()
    {
        var dtm = DesireTreeManager.Instance ?? DesireTreeManager.EnsureExists();
        if (!dtm.IsPathUnlockedByChapter(selectedTreePath))
        {
            Debug.LogWarning($"[LobbyUI] Path {selectedTreePath} is locked by chapter progression!");
            return;
        }

        PlayerPrefs.SetInt("Sanctum_ActiveLustPath", (int)selectedTreePath);
        PlayerPrefs.Save();

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.SetActiveLustPath(selectedTreePath);
        }

        RefreshDesireTreeUI();
        RefreshDeck();
    }

    public void SelectTreePath(LustPathType path)
    {
        BrowseTreePath(path);
    }

    public string GetPathDisplayName(LustPathType path)
    {
        switch (path)
        {
            case LustPathType.PhallusDevotee:
                return "Phallus Devotee";
            case LustPathType.BoundSubmissive:
                return "Bound & Submissive";
            case LustPathType.MonsterTransformation:
                return "Monster Metamorphosis";
            default:
                return path.ToString();
        }
    }

    private static Sprite _circleSprite;
    public static Sprite GetCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;
        int res = 128;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float r = res * 0.5f;
        Vector2 c = new Vector2(r, r);
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(r - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        return _circleSprite;
    }

    public void RefreshDesireTreeUI()
    {
        var dtm = DesireTreeManager.Instance ?? DesireTreeManager.EnsureExists();
        int lustid = CurrencyManager.Instance != null ? CurrencyManager.Instance.TotalLustid : 0;

        if (treeHeaderLustidText != null)
        {
            treeHeaderLustidText.text = $"Altar Essence: <color=#FF77DD>{lustid} Lustid</color>";
        }

        LustPathType activePath = CombatManager.Instance != null 
            ? CombatManager.Instance.activeLustPath 
            : (LustPathType)PlayerPrefs.GetInt("Sanctum_ActiveLustPath", (int)LustPathType.PhallusDevotee);

        if (woodButtonSprite == null) woodButtonSprite = LoadUISprite("ui_button.png");

        Color activeGold = new Color(1.0f, 0.92f, 0.60f, 1f);
        Color selectedTabBg = (woodButtonSprite != null) ? new Color(1.25f, 1.15f, 0.95f, 1f) : new Color(0.35f, 0.18f, 0.42f, 0.95f);
        Color normalBg = (woodButtonSprite != null) ? new Color(0.65f, 0.65f, 0.65f, 1f) : new Color(0.12f, 0.14f, 0.18f, 0.85f);
        Color normalText = new Color(0.85f, 0.82f, 0.75f, 1f);
        Color lockedText = new Color(0.55f, 0.52f, 0.48f, 1f);

        Image[] tabBgs = { treePathPhallusBg, treePathBoundBg, treePathMonsterBg };
        foreach (var bg in tabBgs)
        {
            if (bg != null && woodButtonSprite != null && bg.sprite != woodButtonSprite)
            {
                bg.sprite = woodButtonSprite;
                bg.type = Image.Type.Sliced;
            }
        }

        bool isPhallusViewed = selectedTreePath == LustPathType.PhallusDevotee;
        bool isBoundViewed = selectedTreePath == LustPathType.BoundSubmissive;
        bool isMonsterViewed = selectedTreePath == LustPathType.MonsterTransformation;

        if (treePathPhallusBg != null) treePathPhallusBg.color = isPhallusViewed ? selectedTabBg : normalBg;
        if (treePathBoundBg != null) treePathBoundBg.color = isBoundViewed ? selectedTabBg : normalBg;
        if (treePathMonsterBg != null) treePathMonsterBg.color = isMonsterViewed ? selectedTabBg : normalBg;

        if (treePathPhallusText != null)
        {
            bool isAct = activePath == LustPathType.PhallusDevotee;
            treePathPhallusText.color = isAct ? activeGold : normalText;
            treePathPhallusText.text = isAct ? "[ACTIVE] PHALLUS DEVOTEE" : "PHALLUS DEVOTEE";
        }

        if (treePathBoundText != null)
        {
            bool isAct = activePath == LustPathType.BoundSubmissive;
            bool isUnl = dtm.IsPathUnlockedByChapter(LustPathType.BoundSubmissive);
            treePathBoundText.color = isAct ? activeGold : (isUnl ? normalText : lockedText);
            treePathBoundText.text = isAct ? "[ACTIVE] BOUND & SUBMISSIVE" : (isUnl ? "BOUND & SUBMISSIVE" : "LOCKED [CHAPTER 2]");
        }

        if (treePathMonsterText != null)
        {
            bool isAct = activePath == LustPathType.MonsterTransformation;
            bool isUnl = dtm.IsPathUnlockedByChapter(LustPathType.MonsterTransformation);
            treePathMonsterText.color = isAct ? activeGold : (isUnl ? normalText : lockedText);
            treePathMonsterText.text = isAct ? "[ACTIVE] MONSTER METAMORPHOSIS" : (isUnl ? "MONSTER METAMORPHOSIS" : "LOCKED [CHAPTER 3]");
        }

        if (treeOverlayPanel != null)
        {
            bool isCurrentPathActive = (selectedTreePath == activePath);
            if (isCurrentPathActive)
            {
                treeOverlayPanel.SetActive(false);
            }
            else
            {
                treeOverlayPanel.SetActive(true);
                treeOverlayPanel.transform.SetAsLastSibling();

                bool isUnlocked = dtm.IsPathUnlockedByChapter(selectedTreePath);
                if (isUnlocked)
                {
                    if (treeOverlayTitleText != null)
                        treeOverlayTitleText.text = $"PATH: {GetPathDisplayName(selectedTreePath).ToUpper()}";
                    if (treeOverlayDescText != null)
                        treeOverlayDescText.text = "This Lust Path is unlocked, but not currently selected as active.\nClick the button below to equip this Sacred Tree for expeditions.";
                    if (treeOverlaySelectBtn != null)
                        treeOverlaySelectBtn.interactable = true;
                    if (treeOverlaySelectBtnBg != null)
                        treeOverlaySelectBtnBg.color = new Color(0.85f, 0.35f, 0.75f, 1f);
                    if (treeOverlaySelectBtnText != null)
                        treeOverlaySelectBtnText.text = "SELECT PATH";
                }
                else
                {
                    int reqChapter = dtm.GetRequiredChapterForPath(selectedTreePath);
                    if (treeOverlayTitleText != null)
                        treeOverlayTitleText.text = "<color=#FF6666>PATH LOCKED</color>";
                    if (treeOverlayDescText != null)
                        treeOverlayDescText.text = $"To unlock this Sacred Tree, you must complete Chapter {reqChapter - 1}.";
                    if (treeOverlaySelectBtn != null)
                        treeOverlaySelectBtn.interactable = false;
                    if (treeOverlaySelectBtnBg != null)
                        treeOverlaySelectBtnBg.color = new Color(0.25f, 0.25f, 0.30f, 0.65f);
                    if (treeOverlaySelectBtnText != null)
                        treeOverlaySelectBtnText.text = "LOCKED";
                }
            }
        }

        if (treeNodesScrollContent != null)
        {
            foreach (Transform child in treeNodesScrollContent) Destroy(child.gameObject);

            TMP_FontAsset baseFont = (lustidBadgeText != null && lustidBadgeText.font != null)
                ? lustidBadgeText.font
                : (treeDetailTitleText != null ? treeDetailTitleText.font : null);

            var nodes = dtm.GetNodesForPath(selectedTreePath);
            if (nodes.Count > 0 && selectedTreeNode == null)
            {
                selectedTreeNode = nodes[0];
            }

            CreateTierDivider(treeNodesScrollContent, 68f, "TIER I: AWAKENING INSTINCTS", new Color(0.95f, 0.80f, 0.40f, 0.25f), baseFont);
            CreateTierDivider(treeNodesScrollContent, 360f, "TIER II: DEEP PLEASURE & UNION", new Color(0.85f, 0.45f, 0.90f, 0.25f), baseFont);
            CreateTierDivider(treeNodesScrollContent, 660f, "TIER III: APEX CORRUPTION & DEVOTION", new Color(1f, 0.85f, 0.40f, 0.35f), baseFont);

            CreateSubBranchHeaders(treeNodesScrollContent, selectedTreePath, baseFont);

            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.prerequisiteNodeId)) continue;
                var parent = nodes.Find(n => n.id == node.prerequisiteNodeId);
                if (parent == null) continue;

                CreateTreeConnectingLine(treeNodesScrollContent, parent, node, dtm);
            }

            foreach (var node in nodes)
            {
                CreateTreeNodeCircle(treeNodesScrollContent, node, node == selectedTreeNode, baseFont);
            }
        }

        if (selectedTreeNode != null)
        {
            UpdateTreeDetailPanel(selectedTreeNode);
        }
    }

    private void CreateTierDivider(Transform parent, float yPos, string label, Color color, TMP_FontAsset baseFont = null)
    {
        GameObject divObj = new GameObject($"TierDivider_{yPos}", typeof(RectTransform));
        divObj.transform.SetParent(parent, false);
        RectTransform rt = divObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(500f, 22f);

        GameObject lineObj = new GameObject("Line", typeof(RectTransform));
        lineObj.transform.SetParent(divObj.transform, false);
        RectTransform lrt = lineObj.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0.5f);
        lrt.anchorMax = new Vector2(1f, 0.5f);
        lrt.sizeDelta = new Vector2(0f, 1.5f);
        Image lImg = lineObj.AddComponent<Image>();
        lImg.color = color;
        lImg.raycastTarget = false;

        GameObject badgeObj = new GameObject("LabelBadge", typeof(RectTransform));
        badgeObj.transform.SetParent(divObj.transform, false);
        RectTransform brt = badgeObj.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(280f, 20f);

        Image txtBg = badgeObj.AddComponent<Image>();
        txtBg.color = new Color(0.06f, 0.07f, 0.10f, 0.92f);
        txtBg.raycastTarget = false;

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(badgeObj.transform, false);
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tt = textObj.AddComponent<TextMeshProUGUI>();
        if (baseFont != null) tt.font = baseFont;
        tt.text = $"— {label} —";
        tt.fontSize = 10f;
        tt.fontStyle = FontStyles.Bold;
        tt.alignment = TextAlignmentOptions.Center;
        tt.color = new Color(color.r, color.g, color.b, 0.90f);
        tt.raycastTarget = false;
    }

    private void CreateSubBranchHeaders(Transform parent, LustPathType path, TMP_FontAsset baseFont = null)
    {
        string subA = "SUB-A";
        string subB = "SUB-B";
        string subC = "SUB-C";

        switch (path)
        {
            case LustPathType.PhallusDevotee:
                subA = "SUB-A: ORAL\nMASTERY";
                subB = "SUB-B: VAGINAL\nCOITUS";
                subC = "SUB-C: SEED\nTRANSMUTATION";
                break;
            case LustPathType.BoundSubmissive:
                subA = "SUB-A: ROPES\n& BONDAGE";
                subB = "SUB-B: MASOCHISM\n& SPANKS";
                subC = "SUB-C: CONTROL\n& REVERSE";
                break;
            case LustPathType.MonsterTransformation:
                subA = "SUB-A: BUST &\nPHEROMONES";
                subB = "SUB-B: WOMB\nSIGIL";
                subC = "SUB-C: DEMON\nASSIMILATION";
                break;
        }

        CreateSubHeaderBadge(parent, -130f, 30f, subA, baseFont);
        CreateSubHeaderBadge(parent, 0f, 30f, subB, baseFont);
        CreateSubHeaderBadge(parent, 130f, 30f, subC, baseFont);
    }

    private void CreateSubHeaderBadge(Transform parent, float xPos, float yPos, string text, TMP_FontAsset baseFont = null)
    {
        GameObject badge = new GameObject($"ColHeader_{xPos}", typeof(RectTransform));
        badge.transform.SetParent(parent, false);
        RectTransform rt = badge.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(xPos, yPos);
        rt.sizeDelta = new Vector2(120f, 26f);

        Image bImg = badge.AddComponent<Image>();
        bImg.color = new Color(0.10f, 0.08f, 0.14f, 0.85f);
        bImg.raycastTarget = false;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(badge.transform, false);
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        if (baseFont != null) tmp.font = baseFont;
        tmp.text = text;
        tmp.fontSize = 8.5f;
        tmp.lineSpacing = -10f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.88f, 0.68f, 0.95f, 0.85f);
        tmp.raycastTarget = false;
    }

    private void CreateTreeConnectingLine(Transform parent, DesireTreeNode fromNode, DesireTreeNode toNode, DesireTreeManager dtm)
    {
        GameObject lineObj = new GameObject($"Line_{fromNode.id}_to_{toNode.id}", typeof(RectTransform));
        lineObj.transform.SetParent(parent, false);
        RectTransform lrt = lineObj.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.5f, 0f);
        lrt.anchorMax = new Vector2(0.5f, 0f);
        lrt.pivot = new Vector2(0.5f, 0.5f);

        Vector2 p1 = fromNode.treePos;
        Vector2 p2 = toNode.treePos;
        Vector2 mid = (p1 + p2) * 0.5f;
        Vector2 diff = p2 - p1;
        float length = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        lrt.anchoredPosition = mid;
        lrt.sizeDelta = new Vector2(length, 4f);
        lrt.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image lineImg = lineObj.AddComponent<Image>();
        lineImg.raycastTarget = false;

        bool fromUnlocked = dtm.IsNodeUnlocked(fromNode.id);
        bool toUnlocked = dtm.IsNodeUnlocked(toNode.id);

        if (fromUnlocked && toUnlocked)
        {
            lineImg.color = new Color(0.28f, 0.92f, 0.48f, 0.85f);
        }
        else if (fromUnlocked)
        {
            lineImg.color = new Color(0.85f, 0.35f, 0.90f, 0.70f);
        }
        else
        {
            lineImg.color = new Color(0.20f, 0.22f, 0.28f, 0.50f);
        }
    }

    private void CreateTreeNodeCircle(Transform parent, DesireTreeNode node, bool isSelected, TMP_FontAsset baseFont = null)
    {
        var dtm = DesireTreeManager.Instance ?? DesireTreeManager.EnsureExists();
        bool isUnlocked = dtm.IsNodeUnlocked(node.id);
        bool canUnlock = dtm.CanUnlockNode(node.id);
        bool isApex = node.tier == 3 && node.nodeType == TreeNodeType.KeystoneCard;
        bool isKeystone = node.nodeType == TreeNodeType.KeystoneCard;

        float outerDiameter = isApex ? 78f : (isKeystone ? 64f : 50f);
        float innerDiameter = isApex ? 66f : (isKeystone ? 52f : 40f);

        GameObject nodeObj = new GameObject($"NodeCircle_{node.id}", typeof(RectTransform));
        nodeObj.transform.SetParent(parent, false);
        RectTransform nrt = nodeObj.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0.5f, 0f);
        nrt.anchorMax = new Vector2(0.5f, 0f);
        nrt.pivot = new Vector2(0.5f, 0.5f);
        nrt.anchoredPosition = node.treePos;
        nrt.sizeDelta = new Vector2(outerDiameter, outerDiameter);

        Button btn = nodeObj.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            selectedTreeNode = node;
            RefreshDesireTreeUI();
        });

        if (isSelected)
        {
            GameObject selAuraObj = new GameObject("SelectionAura", typeof(RectTransform));
            selAuraObj.transform.SetParent(nodeObj.transform, false);
            RectTransform sart = selAuraObj.GetComponent<RectTransform>();
            sart.anchorMin = new Vector2(0.5f, 0.5f);
            sart.anchorMax = new Vector2(0.5f, 0.5f);
            sart.pivot = new Vector2(0.5f, 0.5f);
            sart.anchoredPosition = Vector2.zero;
            sart.sizeDelta = new Vector2(outerDiameter + 16f, outerDiameter + 16f);
            Image selImg = selAuraObj.AddComponent<Image>();
            selImg.sprite = GetCircleSprite();
            selImg.color = new Color(1f, 0.88f, 0.35f, 0.35f);
            selImg.raycastTarget = false;
        }

        GameObject ringObj = new GameObject("Ring", typeof(RectTransform));
        ringObj.transform.SetParent(nodeObj.transform, false);
        RectTransform rrt = ringObj.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero;
        rrt.anchorMax = Vector2.one;
        rrt.sizeDelta = Vector2.zero;
        Image ringImg = ringObj.AddComponent<Image>();
        ringImg.sprite = GetCircleSprite();
        ringImg.raycastTarget = true;
        btn.targetGraphic = ringImg;

        Color ringColor;
        if (isSelected)
            ringColor = new Color(1f, 0.90f, 0.35f, 1f);
        else if (isUnlocked)
            ringColor = new Color(0.28f, 0.92f, 0.48f, 1f);
        else if (canUnlock)
            ringColor = new Color(0.90f, 0.35f, 0.92f, 1f);
        else
            ringColor = new Color(0.25f, 0.28f, 0.36f, 0.85f);
        ringImg.color = ringColor;

        GameObject coreObj = new GameObject("Core", typeof(RectTransform));
        coreObj.transform.SetParent(nodeObj.transform, false);
        RectTransform crt = coreObj.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(innerDiameter, innerDiameter);
        Image coreImg = coreObj.AddComponent<Image>();
        coreImg.sprite = GetCircleSprite();
        coreImg.raycastTarget = false;

        Color coreColor;
        if (isUnlocked)
            coreColor = new Color(0.06f, 0.18f, 0.10f, 0.98f);
        else if (canUnlock)
            coreColor = new Color(0.18f, 0.08f, 0.22f, 0.98f);
        else
            coreColor = new Color(0.08f, 0.09f, 0.13f, 0.98f);
        coreImg.color = coreColor;

        GameObject iconObj = new GameObject("CenterIcon", typeof(RectTransform));
        iconObj.transform.SetParent(nodeObj.transform, false);
        RectTransform irt = iconObj.GetComponent<RectTransform>();
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.sizeDelta = Vector2.zero;
        TextMeshProUGUI itxt = iconObj.AddComponent<TextMeshProUGUI>();
        if (baseFont != null) itxt.font = baseFont;
        itxt.alignment = TextAlignmentOptions.Center;
        itxt.raycastTarget = false;

        if (isApex)
        {
            itxt.text = "*";
            itxt.fontSize = 28;
            itxt.fontStyle = FontStyles.Bold;
            itxt.color = isUnlocked ? new Color(1f, 0.90f, 0.35f, 1f) : (canUnlock ? new Color(1f, 0.60f, 0.90f, 1f) : new Color(0.35f, 0.38f, 0.48f, 1f));
        }
        else if (isKeystone)
        {
            itxt.text = "*";
            itxt.fontSize = 22;
            itxt.fontStyle = FontStyles.Bold;
            itxt.color = isUnlocked ? new Color(0.35f, 0.95f, 0.50f, 1f) : (canUnlock ? new Color(1f, 0.70f, 0.95f, 1f) : new Color(0.35f, 0.38f, 0.48f, 1f));
        }
        else
        {
            itxt.text = "+";
            itxt.fontSize = 18;
            itxt.fontStyle = FontStyles.Bold;
            itxt.color = isUnlocked ? new Color(0.35f, 0.95f, 0.50f, 1f) : (canUnlock ? new Color(0.60f, 0.85f, 1f, 1f) : new Color(0.35f, 0.38f, 0.48f, 1f));
        }

        GameObject labelObj = new GameObject("NodeLabel", typeof(RectTransform));
        labelObj.transform.SetParent(nodeObj.transform, false);
        RectTransform lbrt = labelObj.GetComponent<RectTransform>();
        lbrt.anchorMin = new Vector2(0.5f, 0f);
        lbrt.anchorMax = new Vector2(0.5f, 0f);
        lbrt.pivot = new Vector2(0.5f, 1f);
        lbrt.anchoredPosition = new Vector2(0f, -3f);
        lbrt.sizeDelta = new Vector2(140f, 32f);

        TextMeshProUGUI lbtxt = labelObj.AddComponent<TextMeshProUGUI>();
        if (baseFont != null) lbtxt.font = baseFont;
        lbtxt.alignment = TextAlignmentOptions.Top;
        lbtxt.fontSize = 10f;
        lbtxt.lineSpacing = -15f;
        lbtxt.fontStyle = FontStyles.Bold;
        lbtxt.textWrappingMode = TextWrappingModes.Normal;
        lbtxt.raycastTarget = false;

        string titleColor = isSelected ? "#FFE455" : (isUnlocked ? "#D8F8E0" : (canUnlock ? "#FFFFFF" : "#7A8094"));
        string costBadge;
        if (isUnlocked)
            costBadge = "<color=#40EE80>[MASTERED]</color>";
        else if (node.lustidCost == 0)
            costBadge = "<color=#40D0FF>FREE</color>";
        else
            costBadge = canUnlock ? $"<color=#FF77DD>* {node.lustidCost}</color>" : $"<color=#777D90>[LOCKED] {node.lustidCost}</color>";

        lbtxt.text = $"<color={titleColor}>{node.title}</color>\n{costBadge}";
    }

    public void UpdateTreeDetailPanel(DesireTreeNode node)
    {
        if (node == null) return;
        selectedTreeNode = node;

        var dtm = DesireTreeManager.Instance ?? DesireTreeManager.EnsureExists();
        bool isUnlocked = dtm.IsNodeUnlocked(node.id);
        bool canUnlock = dtm.CanUnlockNode(node.id);

        if (treeDetailTitleText != null) treeDetailTitleText.text = node.title;

        if (treeDetailCategoryText != null)
        {
            if (node.nodeType == TreeNodeType.KeystoneCard)
            {
                treeDetailCategoryText.text = "<color=#FFCC44>[KEYSTONE: UNLOCKS FETISH CARD]</color>";
            }
            else
            {
                treeDetailCategoryText.text = "<color=#70D6FF>[PASSIVE TALENT]</color>";
            }
        }

        if (treeDetailDescText != null)
        {
            treeDetailDescText.text = node.description;
        }

        if (treeDetailBonusText != null)
        {
            var bonuses = new List<string>();
            if (node.bonusHp > 0) bonuses.Add($"<color=#FF6666>+ {node.bonusHp} Max HP</color>");
            if (node.bonusShield > 0) bonuses.Add($"<color=#55AAFF>+ {node.bonusShield} Shield</color>");
            if (node.bonusCumShield > 0) bonuses.Add($"<color=#FF77DD>+ {node.bonusCumShield} Lust Shield</color>");
            if (node.bonusSeductionPower > 0) bonuses.Add($"<color=#FF44AA>+ {node.bonusSeductionPower}% Seduction Power</color>");
            if (node.bonusWillpower > 0) bonuses.Add($"<color=#E588F8>+ {node.bonusWillpower} Willpower</color>");

            if (node.unlockedCardIds != null && node.unlockedCardIds.Count > 0)
            {
                foreach (var cid in node.unlockedCardIds)
                {
                    bonuses.Add($"<color=#FFD700>+ [{cid.Replace('_', ' ').ToUpper()}] to Desire Hand</color>");
                }
            }

            treeDetailBonusText.text = string.Join("\n", bonuses);
        }

        LustPathType activePath = CombatManager.Instance != null 
            ? CombatManager.Instance.activeLustPath 
            : (LustPathType)PlayerPrefs.GetInt("Sanctum_ActiveLustPath", (int)LustPathType.PhallusDevotee);

        if (treePurchaseBtn != null)
        {
            treePurchaseBtn.onClick.RemoveAllListeners();

            if (woodButtonSprite == null) woodButtonSprite = LoadUISprite("ui_button.png");
            if (treePurchaseBtnBg != null && woodButtonSprite != null && treePurchaseBtnBg.sprite != woodButtonSprite)
            {
                treePurchaseBtnBg.sprite = woodButtonSprite;
                treePurchaseBtnBg.type = Image.Type.Sliced;
            }

            if (selectedTreePath != activePath)
            {
                treePurchaseBtn.interactable = false;
                if (treePurchaseBtnText != null) treePurchaseBtnText.text = "[SELECT PATH FIRST]";
                if (treePurchaseBtnBg != null) treePurchaseBtnBg.color = (woodButtonSprite != null) ? new Color(0.35f, 0.33f, 0.32f, 0.6f) : new Color(0.25f, 0.25f, 0.30f, 0.70f);
            }
            else if (isUnlocked)
            {
                treePurchaseBtn.interactable = false;
                if (treePurchaseBtnText != null) treePurchaseBtnText.text = "[MASTERED]";
                if (treePurchaseBtnBg != null) treePurchaseBtnBg.color = (woodButtonSprite != null) ? new Color(0.60f, 0.75f, 0.60f, 0.85f) : new Color(0.20f, 0.45f, 0.25f, 0.85f);
            }
            else if (canUnlock)
            {
                treePurchaseBtn.interactable = true;
                string costLabel = node.lustidCost > 0 ? $"UNLOCK ({node.lustidCost} LUSTID)" : "AWAKEN (FREE)";
                if (treePurchaseBtnText != null) treePurchaseBtnText.text = costLabel;
                if (treePurchaseBtnBg != null) treePurchaseBtnBg.color = (woodButtonSprite != null) ? new Color(0.78f, 0.95f, 0.80f, 1f) : new Color(0.65f, 0.22f, 0.60f, 1f);

                treePurchaseBtn.onClick.AddListener(() =>
                {
                    if (dtm.TryUnlockNode(node.id))
                    {
                        RefreshLustidDisplay();
                        RefreshDesireTreeUI();
                        RefreshDeck();
                    }
                });
            }
            else
            {
                treePurchaseBtn.interactable = false;
                int currentLustid = CurrencyManager.Instance != null ? CurrencyManager.Instance.TotalLustid : 0;
                bool needPrereq = !string.IsNullOrEmpty(node.prerequisiteNodeId) && !dtm.IsNodeUnlocked(node.prerequisiteNodeId);

                if (needPrereq)
                {
                    if (treePurchaseBtnText != null) treePurchaseBtnText.text = "LOCKED (Requires Previous Node)";
                }
                else
                {
                    if (treePurchaseBtnText != null) treePurchaseBtnText.text = $"NEED {node.lustidCost} LUSTID (Have {currentLustid})";
                }
                if (treePurchaseBtnBg != null) treePurchaseBtnBg.color = (woodButtonSprite != null) ? new Color(0.35f, 0.33f, 0.32f, 0.6f) : new Color(0.25f, 0.25f, 0.30f, 0.7f);
            }
        }
    }

    // =========================================================================
    // DYNAMIC FACTORY (SANCTUARY HUB CAVE FULLSCREEN VIEW)
    // =========================================================================

    private static Sprite LoadLobbySprite(string fileName)
    {
#if UNITY_EDITOR
        string path = $"Assets/sprites/Location/Lobby/{fileName}";
        Sprite spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (spr != null) return spr;

        UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
        spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (spr != null) return spr;

        UnityEngine.Object[] subs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var sub in subs)
        {
            if (sub is Sprite s) return s;
        }

        Debug.LogError($"[LobbyUI] Could not load Sprite at '{path}'! Check TextureImporter settings.");
#endif
        return null;
    }

    public static LobbyUI CreateDynamic(Transform parent, CharacterState state, InventoryManager inv)
    {
        return CreateDynamic(parent, state, inv, null);
    }

    public static LobbyUI CreateDynamic(Transform parent, CharacterState state = null, InventoryManager inv = null, PaperDollController doll = null)
    {
        // 1. Root Fullscreen Panel (1920x1080 canvas match)
        GameObject panelObj = new GameObject("LobbyPanel", typeof(RectTransform));
        panelObj.transform.SetParent(parent, false);

        RectTransform pr = panelObj.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;

        LobbyUI lobby = panelObj.AddComponent<LobbyUI>();
        lobby.lobbyPanel = panelObj;
        lobby.characterState = state;
        lobby.inventoryManager = inv;
        lobby.paperDoll = doll;

        // Load 6 PSD-coordinated 1920x1080 sprites
        lobby.lobbyBgSprite = LoadLobbySprite("lobby.png");
        lobby.exitSprite = LoadLobbySprite("exit.png");
        lobby.crystalSprite = LoadLobbySprite("crystal.png");
        lobby.dummySprite = LoadLobbySprite("dummy.png");
        lobby.settingsSprite = LoadLobbySprite("settings.png");
        lobby.chestSprite = LoadLobbySprite("chest.png");

        // 2. Cave View Container (Fullscreen layers)
        GameObject caveView = new GameObject("CaveView", typeof(RectTransform));
        caveView.transform.SetParent(panelObj.transform, false);
        RectTransform cvrt = caveView.GetComponent<RectTransform>();
        cvrt.anchorMin = Vector2.zero;
        cvrt.anchorMax = Vector2.one;
        cvrt.offsetMin = Vector2.zero;
        cvrt.offsetMax = Vector2.zero;

        // Base Cave Background (lobby.png)
        GameObject bgObj = new GameObject("CaveBg", typeof(RectTransform));
        bgObj.transform.SetParent(caveView.transform, false);
        RectTransform bgrt = bgObj.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = lobby.lobbyBgSprite;
        bgImg.color = Color.white;
        bgImg.preserveAspect = false;
        bgImg.raycastTarget = false;
        lobby.caveBgImage = bgImg;

        // Helper to construct interactive cave layers
        Func<string, Sprite, int, LobbyInteractiveObject> createInteractiveLayer = (name, spr, siblingIndex) =>
        {
            GameObject layerObj = new GameObject(name, typeof(RectTransform));
            layerObj.transform.SetParent(caveView.transform, false);
            RectTransform lrt = layerObj.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            Image img = layerObj.AddComponent<Image>();
            img.sprite = spr;
            img.preserveAspect = false;
            img.color = Color.white;
            img.raycastTarget = true;
            img.alphaHitTestMinimumThreshold = 0.1f;

            LobbyInteractiveObject lio = layerObj.AddComponent<LobbyInteractiveObject>();
            lio.targetImage = img;
            lio.outlineWidth = 2.5f;
            lio.outlineColor = new Color(0.85f, 0.88f, 0.92f, 1f);
#if UNITY_EDITOR
            Material outlineMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/UISpriteOutlineMat.mat");
            if (outlineMat != null) lio.outlineMaterial = outlineMat;
#endif

            return lio;
        };

        // Render Order (Back to Front):
        // 1. Exit Archway (in depth)
        lobby.exitInteractive = createInteractiveLayer("Layer_Exit", lobby.exitSprite, 1);

        // 2. Crystal (Floating above pedestal)
        lobby.crystalInteractive = createInteractiveLayer("Layer_Crystal", lobby.crystalSprite, 2);
        SteppedFloatingCrystal sfc = lobby.crystalInteractive.gameObject.AddComponent<SteppedFloatingCrystal>();
        sfc.targetFps = 8f;
        sfc.amplitude = 12f;
        sfc.frequency = 0.5f;
        lobby.crystalLevitation = sfc;

        // 3. Training Dummy
        lobby.dummyInteractive = createInteractiveLayer("Layer_Dummy", lobby.dummySprite, 3);

        // 4. Settings Root (Top right corner)
        lobby.settingsInteractive = createInteractiveLayer("Layer_Settings", lobby.settingsSprite, 4);

        // 5. Wardrobe Chest (Foreground)
        lobby.chestInteractive = createInteractiveLayer("Layer_Chest", lobby.chestSprite, 5);

        lobby.ConfigureInteractiveCaveObjects();

        // 3. Lustid Currency Badge (Top Right HUD)
        GameObject lustidBadgeObj = new GameObject("LustidBadge", typeof(RectTransform));
        lustidBadgeObj.transform.SetParent(caveView.transform, false);
        RectTransform lbr = lustidBadgeObj.GetComponent<RectTransform>();
        lbr.anchorMin = new Vector2(1f, 1f);
        lbr.anchorMax = new Vector2(1f, 1f);
        lbr.pivot = new Vector2(1f, 1f);
        lbr.anchoredPosition = new Vector2(-30f, -20f);
        lbr.sizeDelta = new Vector2(230f, 44f);

        Image lbBg = lustidBadgeObj.AddComponent<Image>();
        lbBg.color = new Color(0.12f, 0.08f, 0.16f, 0.92f);
        Outline lbOutline = lustidBadgeObj.AddComponent<Outline>();
        lbOutline.effectColor = new Color(0.75f, 0.25f, 0.65f, 0.75f);
        lbOutline.effectDistance = new Vector2(1.5f, -1.5f);

        HorizontalLayoutGroup lbHlg = lustidBadgeObj.AddComponent<HorizontalLayoutGroup>();
        lbHlg.padding = new RectOffset(12, 12, 4, 4);
        lbHlg.spacing = 10;
        lbHlg.childAlignment = TextAnchor.MiddleCenter;
        lbHlg.childControlWidth = false;
        lbHlg.childControlHeight = false;

        GameObject lbIconObj = new GameObject("Icon", typeof(RectTransform));
        lbIconObj.transform.SetParent(lustidBadgeObj.transform, false);
        RectTransform lbIconRt = lbIconObj.GetComponent<RectTransform>();
        lbIconRt.sizeDelta = new Vector2(30f, 30f);
        Image lbIconImg = lbIconObj.AddComponent<Image>();
        lbIconImg.sprite = StatIconDatabase.LustidIcon;
        lbIconImg.preserveAspect = true;

        GameObject lbTxtObj = new GameObject("Text", typeof(RectTransform));
        lbTxtObj.transform.SetParent(lustidBadgeObj.transform, false);
        TextMeshProUGUI lbTxt = lbTxtObj.AddComponent<TextMeshProUGUI>();
        lbTxt.text = "0 Lustid";
        lbTxt.fontSize = 18;
        lbTxt.fontStyle = FontStyles.Bold;
        lbTxt.alignment = TextAlignmentOptions.MidlineLeft;
        lbTxt.color = new Color(1f, 0.55f, 0.90f, 1f);
        RectTransform lbTxtRt = lbTxtObj.GetComponent<RectTransform>();
        lbTxtRt.sizeDelta = new Vector2(150f, 32f);
        lobby.lustidBadgeText = lbTxt;

        // 4. Overlays Container
        lobby.EnsureOverlaysContainerAndPanels(panelObj.transform);
        panelObj.SetActive(false);

        lobby.SetupListeners();
        return lobby;
    }

    /// <summary>
    /// Ensures that all 4 overlays (Wardrobe, Deck, Paths, Settings) are created and initialized.
    /// Can be called on an existing manual hierarchy without re-creating existing overlays.
    /// </summary>
    public void EnsureOverlaysContainerAndPanels(Transform parentOverride = null)
    {
        if (wardrobeOverlay != null && deckOverlay != null && pathsOverlay != null && settingsOverlay != null)
        {
            return;
        }

        Transform containerParent = parentOverride != null 
            ? parentOverride 
            : (lobbyPanel != null ? lobbyPanel.transform : transform);

        Transform existingContainer = containerParent.Find("OverlaysContainer");
        GameObject overlaysContainer = existingContainer != null ? existingContainer.gameObject : null;
        if (overlaysContainer == null)
        {
            overlaysContainer = new GameObject("OverlaysContainer", typeof(RectTransform));
            overlaysContainer.transform.SetParent(containerParent, false);
            RectTransform ocrt = overlaysContainer.GetComponent<RectTransform>();
            ocrt.anchorMin = Vector2.zero;
            ocrt.anchorMax = Vector2.one;
            ocrt.offsetMin = Vector2.zero;
            ocrt.offsetMax = Vector2.zero;
        }

        LobbyUI lobby = this;

        // =====================================================================
        // OVERLAY 1: WARDROBE & FLESH STABILIZATION (Right panel, Heroine on left)
        // =====================================================================
        GameObject wardrobeOverlayObj = new GameObject("WardrobeOverlay", typeof(RectTransform));
        wardrobeOverlayObj.transform.SetParent(overlaysContainer.transform, false);
        RectTransform wort = wardrobeOverlayObj.GetComponent<RectTransform>();
        wort.anchorMin = Vector2.zero;
        wort.anchorMax = Vector2.one;
        wort.offsetMin = Vector2.zero;
        wort.offsetMax = Vector2.zero;
        lobby.wardrobeOverlay = wardrobeOverlayObj;

        // Dimmer background that captures clicks
        GameObject wDimmer = new GameObject("Dimmer", typeof(RectTransform));
        wDimmer.transform.SetParent(wardrobeOverlayObj.transform, false);
        RectTransform wdrt = wDimmer.GetComponent<RectTransform>();
        wdrt.anchorMin = Vector2.zero;
        wdrt.anchorMax = Vector2.one;
        wdrt.offsetMin = Vector2.zero;
        wdrt.offsetMax = Vector2.zero;
        Image wdImg = wDimmer.AddComponent<Image>();
        wdImg.color = new Color(0.02f, 0.03f, 0.06f, 0.65f);
        wdImg.raycastTarget = true;

        // Wardrobe Panel (Docked to right, 1120px wide)
        GameObject wPanel = new GameObject("WardrobePanel", typeof(RectTransform));
        wPanel.transform.SetParent(wardrobeOverlayObj.transform, false);
        RectTransform wprt = wPanel.GetComponent<RectTransform>();
        wprt.anchorMin = new Vector2(1f, 0f);
        wprt.anchorMax = new Vector2(1f, 1f);
        wprt.pivot = new Vector2(1f, 0.5f);
        wprt.anchoredPosition = new Vector2(-30f, 0f);
        wprt.sizeDelta = new Vector2(1120f, -40f);

        Image wPanelBg = wPanel.AddComponent<Image>();
        wPanelBg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);
        Outline wOutline = wPanel.AddComponent<Outline>();
        wOutline.effectColor = new Color(0.75f, 0.35f, 0.65f, 0.85f);
        wOutline.effectDistance = new Vector2(2f, -2f);

        // Header Title
        GameObject wTitleObj = new GameObject("Title", typeof(RectTransform));
        wTitleObj.transform.SetParent(wPanel.transform, false);
        TextMeshProUGUI wTitle = wTitleObj.AddComponent<TextMeshProUGUI>();
        wTitle.text = "WARDROBE & ANATOMY";
        wTitle.fontSize = 28;
        wTitle.fontStyle = FontStyles.Bold;
        wTitle.alignment = TextAlignmentOptions.MidlineLeft;
        wTitle.color = new Color(0.95f, 0.85f, 0.60f, 1f);
        RectTransform wtr2 = wTitleObj.GetComponent<RectTransform>();
        wtr2.anchorMin = new Vector2(0f, 1f);
        wtr2.anchorMax = new Vector2(0.7f, 1f);
        wtr2.pivot = new Vector2(0f, 1f);
        wtr2.anchoredPosition = new Vector2(32f, -22f);
        wtr2.sizeDelta = new Vector2(0f, 36f);

        // Close / Back to Cave Button
        lobby.wardrobeCloseBtn = CreateOverlayBackButton(wPanel.transform, "BACK TO CAVE", () => lobby.CloseActiveOverlay());

        // Equipped Clothes Box
        GameObject eqBox = new GameObject("EquippedBox", typeof(RectTransform));
        eqBox.transform.SetParent(wPanel.transform, false);
        Image eqBg = eqBox.AddComponent<Image>();
        eqBg.color = new Color(0.14f, 0.17f, 0.23f, 0.95f);
        RectTransform eqr = eqBox.GetComponent<RectTransform>();
        eqr.anchorMin = new Vector2(0f, 1f);
        eqr.anchorMax = new Vector2(1f, 1f);
        eqr.pivot = new Vector2(0.5f, 1f);
        eqr.anchoredPosition = new Vector2(0f, -68f);
        eqr.sizeDelta = new Vector2(-56f, 62f);

        GameObject eqTxtObj = new GameObject("Text", typeof(RectTransform));
        eqTxtObj.transform.SetParent(eqBox.transform, false);
        TextMeshProUGUI eqTxt = eqTxtObj.AddComponent<TextMeshProUGUI>();
        eqTxt.text = "Equipped Clothes: None";
        eqTxt.fontSize = 16;
        eqTxt.alignment = TextAlignmentOptions.MidlineLeft;
        eqTxt.color = new Color(0.92f, 0.88f, 0.75f, 1f);
        RectTransform eqtr = eqTxtObj.GetComponent<RectTransform>();
        eqtr.anchorMin = new Vector2(0f, 0f);
        eqtr.anchorMax = new Vector2(0.68f, 1f);
        eqtr.anchoredPosition = new Vector2(24f, 0f);
        eqtr.sizeDelta = new Vector2(-24f, 0f);
        lobby.equippedClothesText = eqTxt;

        // Unequip (Strip) Button
        GameObject uBtnObj = new GameObject("UnequipClothesBtn", typeof(RectTransform));
        uBtnObj.transform.SetParent(eqBox.transform, false);
        Image uBg = uBtnObj.AddComponent<Image>();
        uBg.color = new Color(0.55f, 0.25f, 0.25f, 1f);
        Button uBtn = uBtnObj.AddComponent<Button>();
        uBtn.targetGraphic = uBg;
        RectTransform ubr = uBtnObj.GetComponent<RectTransform>();
        ubr.anchorMin = new Vector2(1f, 0.5f);
        ubr.anchorMax = new Vector2(1f, 0.5f);
        ubr.pivot = new Vector2(1f, 0.5f);
        ubr.anchoredPosition = new Vector2(-20f, 0f);
        ubr.sizeDelta = new Vector2(100f, 34f);
        GameObject uTxt = new GameObject("Text", typeof(RectTransform));
        uTxt.transform.SetParent(uBtnObj.transform, false);
        TextMeshProUGUI utxr = uTxt.AddComponent<TextMeshProUGUI>();
        utxr.text = "Strip";
        utxr.fontSize = 15;
        utxr.fontStyle = FontStyles.Bold;
        utxr.alignment = TextAlignmentOptions.Center;
        utxr.color = Color.white;
        RectTransform utxr2 = uTxt.GetComponent<RectTransform>();
        utxr2.anchorMin = Vector2.zero;
        utxr2.anchorMax = Vector2.one;
        utxr2.sizeDelta = Vector2.zero;
        lobby.unequipClothesBtn = uBtn;

        // Repair Clothes Button
        GameObject rBtnObj = new GameObject("RepairClothesBtn", typeof(RectTransform));
        rBtnObj.transform.SetParent(eqBox.transform, false);
        Image rBg = rBtnObj.AddComponent<Image>();
        rBg.color = new Color(0.20f, 0.55f, 0.35f, 1f);
        Button rBtn = rBtnObj.AddComponent<Button>();
        rBtn.targetGraphic = rBg;
        RectTransform rbr = rBtnObj.GetComponent<RectTransform>();
        rbr.anchorMin = new Vector2(1f, 0.5f);
        rbr.anchorMax = new Vector2(1f, 0.5f);
        rbr.pivot = new Vector2(1f, 0.5f);
        rbr.anchoredPosition = new Vector2(-128f, 0f);
        rbr.sizeDelta = new Vector2(120f, 34f);
        GameObject rTxt = new GameObject("Text", typeof(RectTransform));
        rTxt.transform.SetParent(rBtnObj.transform, false);
        TextMeshProUGUI rtxr = rTxt.AddComponent<TextMeshProUGUI>();
        rtxr.text = "Repair (Free)";
        rtxr.fontSize = 15;
        rtxr.fontStyle = FontStyles.Bold;
        rtxr.alignment = TextAlignmentOptions.Center;
        rtxr.color = Color.white;
        RectTransform rtxr2 = rTxt.GetComponent<RectTransform>();
        rtxr2.anchorMin = Vector2.zero;
        rtxr2.anchorMax = Vector2.one;
        rtxr2.sizeDelta = Vector2.zero;
        lobby.repairClothesBtn = rBtn;

        // Flesh Stabilization Box
        GameObject stabBox = new GameObject("FleshStabilizationBox", typeof(RectTransform));
        stabBox.transform.SetParent(wPanel.transform, false);
        Image stabBg = stabBox.AddComponent<Image>();
        stabBg.color = new Color(0.11f, 0.12f, 0.18f, 0.95f);
        RectTransform stbr = stabBox.GetComponent<RectTransform>();
        stbr.anchorMin = new Vector2(0f, 1f);
        stbr.anchorMax = new Vector2(1f, 1f);
        stbr.pivot = new Vector2(0.5f, 1f);
        stbr.anchoredPosition = new Vector2(0f, -140f);
        stbr.sizeDelta = new Vector2(-56f, 132f);
        lobby.fleshStabilizationContainer = stabBox;

        GameObject stTitleObj = new GameObject("StabTitle", typeof(RectTransform));
        stTitleObj.transform.SetParent(stabBox.transform, false);
        RectTransform sttr = stTitleObj.GetComponent<RectTransform>();
        sttr.anchorMin = new Vector2(0f, 1f);
        sttr.anchorMax = new Vector2(1f, 1f);
        sttr.pivot = new Vector2(0.5f, 1f);
        sttr.anchoredPosition = new Vector2(24f, -14f);
        sttr.sizeDelta = new Vector2(-48f, 22f);
        TextMeshProUGUI stTitle = stTitleObj.AddComponent<TextMeshProUGUI>();
        stTitle.text = "FLESH STABILIZATION (Choose Unlocked Anatomy)";
        stTitle.fontSize = 13;
        stTitle.fontStyle = FontStyles.Bold;
        stTitle.alignment = TextAlignmentOptions.MidlineLeft;
        stTitle.color = new Color(1f, 0.75f, 0.9f, 1f);

        GameObject btnRow = new GameObject("SizeButtonsRow", typeof(RectTransform));
        btnRow.transform.SetParent(stabBox.transform, false);
        RectTransform brr = btnRow.GetComponent<RectTransform>();
        brr.anchorMin = new Vector2(0f, 1f);
        brr.anchorMax = new Vector2(1f, 1f);
        brr.pivot = new Vector2(0.5f, 1f);
        brr.anchoredPosition = new Vector2(0f, -42f);
        brr.sizeDelta = new Vector2(-48f, 36f);
        HorizontalLayoutGroup brHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        brHlg.spacing = 10;
        brHlg.childControlWidth = true;
        brHlg.childControlHeight = true;

        lobby.bodySizeButtons = new Button[4];
        lobby.bodySizeButtonBgs = new Image[4];
        lobby.bodySizeButtonTexts = new TextMeshProUGUI[4];
        string[] initialLabels = { "Small (A-B)", "Medium (C-D)", "Large (D-E)", "XLarge (E-F)" };
        for (int i = 0; i < 4; i++)
        {
            GameObject bObj = new GameObject($"SizeBtn_{i}", typeof(RectTransform));
            bObj.transform.SetParent(btnRow.transform, false);
            Image bBg = bObj.AddComponent<Image>();
            bBg.color = new Color(0.18f, 0.22f, 0.30f, 0.90f);
            Button bBtn = bObj.AddComponent<Button>();
            bBtn.targetGraphic = bBg;

            GameObject btObj = new GameObject("Text", typeof(RectTransform));
            btObj.transform.SetParent(bObj.transform, false);
            RectTransform btrt = btObj.GetComponent<RectTransform>();
            btrt.anchorMin = Vector2.zero;
            btrt.anchorMax = Vector2.one;
            btrt.sizeDelta = Vector2.zero;
            TextMeshProUGUI bTxt = btObj.AddComponent<TextMeshProUGUI>();
            bTxt.text = initialLabels[i];
            bTxt.fontSize = 13;
            bTxt.fontStyle = FontStyles.Bold;
            bTxt.alignment = TextAlignmentOptions.Center;
            bTxt.color = Color.white;

            lobby.bodySizeButtons[i] = bBtn;
            lobby.bodySizeButtonBgs[i] = bBg;
            lobby.bodySizeButtonTexts[i] = bTxt;
        }

        GameObject infoRow = new GameObject("InfoRow", typeof(RectTransform));
        infoRow.transform.SetParent(stabBox.transform, false);
        RectTransform infor = infoRow.GetComponent<RectTransform>();
        infor.anchorMin = new Vector2(0f, 0f);
        infor.anchorMax = new Vector2(1f, 0f);
        infor.pivot = new Vector2(0.5f, 0f);
        infor.anchoredPosition = new Vector2(0f, 14f);
        infor.sizeDelta = new Vector2(-48f, 32f);
        HorizontalLayoutGroup infHlg = infoRow.AddComponent<HorizontalLayoutGroup>();
        infHlg.spacing = 12;
        infHlg.childControlWidth = false;
        infHlg.childControlHeight = true;

        GameObject gTxtObj = new GameObject("GaugeText", typeof(RectTransform));
        gTxtObj.transform.SetParent(infoRow.transform, false);
        RectTransform gtrt = gTxtObj.GetComponent<RectTransform>();
        gtrt.sizeDelta = new Vector2(280f, 32f);
        TextMeshProUGUI gTxt = gTxtObj.AddComponent<TextMeshProUGUI>();
        gTxt.fontSize = 12;
        gTxt.alignment = TextAlignmentOptions.MidlineLeft;
        gTxt.color = new Color(0.85f, 0.85f, 0.95f, 1f);
        lobby.fleshPranaGaugeText = gTxt;

        GameObject dbgBtnObj = new GameObject("DbgFillBtn", typeof(RectTransform));
        dbgBtnObj.transform.SetParent(infoRow.transform, false);
        RectTransform dbgrt = dbgBtnObj.GetComponent<RectTransform>();
        dbgrt.sizeDelta = new Vector2(120f, 30f);
        Image dbgBg = dbgBtnObj.AddComponent<Image>();
        dbgBg.color = new Color(0.55f, 0.20f, 0.45f, 0.95f);
        Button dbgBtn = dbgBtnObj.AddComponent<Button>();
        dbgBtn.targetGraphic = dbgBg;
        lobby.fleshDebugFillBtn = dbgBtn;

        GameObject dbgtObj = new GameObject("Text", typeof(RectTransform));
        dbgtObj.transform.SetParent(dbgBtnObj.transform, false);
        RectTransform dbgtrt = dbgtObj.GetComponent<RectTransform>();
        dbgtrt.anchorMin = Vector2.zero;
        dbgtrt.anchorMax = Vector2.one;
        dbgtrt.sizeDelta = Vector2.zero;
        TextMeshProUGUI dbgTxt = dbgtObj.AddComponent<TextMeshProUGUI>();
        dbgTxt.text = "[DBG] Fill Gauge";
        dbgTxt.fontSize = 12;
        dbgTxt.fontStyle = FontStyles.Bold;
        dbgTxt.alignment = TextAlignmentOptions.Center;
        dbgTxt.color = Color.white;

        GameObject archTxtObj = new GameObject("ArchiveText", typeof(RectTransform));
        archTxtObj.transform.SetParent(infoRow.transform, false);
        RectTransform archrt = archTxtObj.GetComponent<RectTransform>();
        archrt.sizeDelta = new Vector2(400f, 32f);
        TextMeshProUGUI archTxt = archTxtObj.AddComponent<TextMeshProUGUI>();
        archTxt.fontSize = 12;
        archTxt.alignment = TextAlignmentOptions.MidlineRight;
        archTxt.color = new Color(0.70f, 0.85f, 0.75f, 1f);
        lobby.fleshArchiveText = archTxt;

        // Emotion Selection Container
        GameObject emoBox = new GameObject("EmotionSelectionBox", typeof(RectTransform));
        emoBox.transform.SetParent(wPanel.transform, false);
        Image emoBg = emoBox.AddComponent<Image>();
        emoBg.color = new Color(0.11f, 0.12f, 0.18f, 0.95f);
        RectTransform emor = emoBox.GetComponent<RectTransform>();
        emor.anchorMin = new Vector2(0f, 1f);
        emor.anchorMax = new Vector2(1f, 1f);
        emor.pivot = new Vector2(0.5f, 1f);
        emor.anchoredPosition = new Vector2(0f, -282f);
        emor.sizeDelta = new Vector2(-56f, 84f);
        lobby.emotionContainer = emoBox;

        GameObject emoTitleObj = new GameObject("EmoTitle", typeof(RectTransform));
        emoTitleObj.transform.SetParent(emoBox.transform, false);
        RectTransform emotr = emoTitleObj.GetComponent<RectTransform>();
        emotr.anchorMin = new Vector2(0f, 1f);
        emotr.anchorMax = new Vector2(1f, 1f);
        emotr.pivot = new Vector2(0.5f, 1f);
        emotr.anchoredPosition = new Vector2(24f, -12f);
        emotr.sizeDelta = new Vector2(-48f, 20f);
        TextMeshProUGUI emoTitle = emoTitleObj.AddComponent<TextMeshProUGUI>();
        emoTitle.text = "FACIAL EXPRESSION";
        emoTitle.fontSize = 12;
        emoTitle.fontStyle = FontStyles.Bold;
        emoTitle.alignment = TextAlignmentOptions.MidlineLeft;
        emoTitle.color = new Color(0.75f, 0.85f, 1f, 1f);

        GameObject emoBtnRow = new GameObject("EmotionButtonsRow", typeof(RectTransform));
        emoBtnRow.transform.SetParent(emoBox.transform, false);
        RectTransform ebrr = emoBtnRow.GetComponent<RectTransform>();
        ebrr.anchorMin = new Vector2(0f, 0f);
        ebrr.anchorMax = new Vector2(1f, 1f);
        ebrr.offsetMin = new Vector2(24f, 10f);
        ebrr.offsetMax = new Vector2(-24f, -32f);
        HorizontalLayoutGroup emoHlg = emoBtnRow.AddComponent<HorizontalLayoutGroup>();
        emoHlg.spacing = 8;
        emoHlg.childControlWidth = true;
        emoHlg.childControlHeight = true;

        string[] emoLabels = { "Normal", "Smile", "Angry", "Annoyed", "Shocked", "Pain", "O" };
        lobby.emotionButtons = new Button[emoLabels.Length];
        lobby.emotionButtonBgs = new Image[emoLabels.Length];
        lobby.emotionButtonTexts = new TextMeshProUGUI[emoLabels.Length];
        for (int i = 0; i < emoLabels.Length; i++)
        {
            GameObject ebObj = new GameObject($"EmoBtn_{i}", typeof(RectTransform));
            ebObj.transform.SetParent(emoBtnRow.transform, false);
            Image ebBg = ebObj.AddComponent<Image>();
            ebBg.color = new Color(0.18f, 0.22f, 0.30f, 0.90f);
            Button ebBtn = ebObj.AddComponent<Button>();
            ebBtn.targetGraphic = ebBg;

            GameObject ebtObj = new GameObject("Text", typeof(RectTransform));
            ebtObj.transform.SetParent(ebObj.transform, false);
            RectTransform ebtrt = ebtObj.GetComponent<RectTransform>();
            ebtrt.anchorMin = Vector2.zero;
            ebtrt.anchorMax = Vector2.one;
            ebtrt.sizeDelta = Vector2.zero;
            TextMeshProUGUI ebTxt = ebtObj.AddComponent<TextMeshProUGUI>();
            ebTxt.text = emoLabels[i];
            ebTxt.fontSize = 12;
            ebTxt.fontStyle = FontStyles.Bold;
            ebTxt.alignment = TextAlignmentOptions.Center;
            ebTxt.color = Color.white;

            lobby.emotionButtons[i] = ebBtn;
            lobby.emotionButtonBgs[i] = ebBg;
            lobby.emotionButtonTexts[i] = ebTxt;
        }

        // Inventory Items Scroll View
        GameObject scrollObj = new GameObject("ItemsScroll", typeof(RectTransform));
        scrollObj.transform.SetParent(wPanel.transform, false);
        RectTransform scr = scrollObj.GetComponent<RectTransform>();
        scr.anchorMin = new Vector2(0f, 0f);
        scr.anchorMax = new Vector2(1f, 1f);
        scr.pivot = new Vector2(0.5f, 1f);
        scr.offsetMax = new Vector2(-28f, -378f);
        scr.offsetMin = new Vector2(28f, 25f);

        ScrollRect sRect = scrollObj.AddComponent<ScrollRect>();
        sRect.horizontal = false;
        sRect.vertical = true;
        sRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        viewportObj.AddComponent<RectMask2D>();
        RectTransform vpRt = viewportObj.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.sizeDelta = Vector2.zero;
        sRect.viewport = vpRt;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform cntRt = contentObj.GetComponent<RectTransform>();
        cntRt.anchorMin = new Vector2(0f, 1f);
        cntRt.anchorMax = new Vector2(1f, 1f);
        cntRt.pivot = new Vector2(0.5f, 1f);
        cntRt.anchoredPosition = Vector2.zero;
        cntRt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sRect.content = cntRt;
        lobby.wardrobeItemsContainer = cntRt;

        // =====================================================================
        // OVERLAY 2: DECKBUILDER
        // =====================================================================
        GameObject deckOverlayObj = new GameObject("DeckOverlay", typeof(RectTransform));
        deckOverlayObj.transform.SetParent(overlaysContainer.transform, false);
        RectTransform dort = deckOverlayObj.GetComponent<RectTransform>();
        dort.anchorMin = Vector2.zero;
        dort.anchorMax = Vector2.one;
        dort.offsetMin = Vector2.zero;
        dort.offsetMax = Vector2.zero;
        lobby.deckOverlay = deckOverlayObj;

        // Dimmer
        GameObject dDimmer = new GameObject("Dimmer", typeof(RectTransform));
        dDimmer.transform.SetParent(deckOverlayObj.transform, false);
        RectTransform ddrt2 = dDimmer.GetComponent<RectTransform>();
        ddrt2.anchorMin = Vector2.zero;
        ddrt2.anchorMax = Vector2.one;
        ddrt2.offsetMin = Vector2.zero;
        ddrt2.offsetMax = Vector2.zero;
        Image ddImg = dDimmer.AddComponent<Image>();
        ddImg.color = new Color(0.02f, 0.03f, 0.06f, 0.75f);
        ddImg.raycastTarget = true;

        // Deck Modal Panel (Centered, 1720x960)
        GameObject dPanel = new GameObject("DeckPanel", typeof(RectTransform));
        dPanel.transform.SetParent(deckOverlayObj.transform, false);
        RectTransform dprt = dPanel.GetComponent<RectTransform>();
        dprt.anchorMin = new Vector2(0.5f, 0.5f);
        dprt.anchorMax = new Vector2(0.5f, 0.5f);
        dprt.pivot = new Vector2(0.5f, 0.5f);
        dprt.sizeDelta = new Vector2(1720f, 960f);

        Image dPanelBg = dPanel.AddComponent<Image>();
        dPanelBg.color = new Color(0.07f, 0.08f, 0.12f, 0.98f);
        Outline dOutline = dPanel.AddComponent<Outline>();
        dOutline.effectColor = new Color(0.68f, 0.54f, 0.28f, 0.95f);
        dOutline.effectDistance = new Vector2(2f, -2f);

        // Deck Header Title Row
        GameObject dTitleObj = new GameObject("Title", typeof(RectTransform));
        dTitleObj.transform.SetParent(dPanel.transform, false);
        TextMeshProUGUI dTitle = dTitleObj.AddComponent<TextMeshProUGUI>();
        dTitle.text = "WARRIOR DECKBUILDER & ARCHIVE";
        dTitle.fontSize = 28;
        dTitle.fontStyle = FontStyles.Bold;
        dTitle.alignment = TextAlignmentOptions.Left;
        dTitle.color = new Color(0.95f, 0.85f, 0.60f, 1f);
        RectTransform dtr2 = dTitleObj.GetComponent<RectTransform>();
        dtr2.anchorMin = new Vector2(0f, 1f);
        dtr2.anchorMax = new Vector2(0.5f, 1f);
        dtr2.pivot = new Vector2(0f, 1f);
        dtr2.anchoredPosition = new Vector2(34f, -22f);
        dtr2.sizeDelta = new Vector2(0f, 36f);

        GameObject dCountObj = new GameObject("DeckCount", typeof(RectTransform));
        dCountObj.transform.SetParent(dPanel.transform, false);
        TextMeshProUGUI dCount = dCountObj.AddComponent<TextMeshProUGUI>();
        dCount.text = "Active Deck: 3 / 9 Cards";
        dCount.fontSize = 18;
        dCount.fontStyle = FontStyles.Bold;
        dCount.alignment = TextAlignmentOptions.Right;
        dCount.color = new Color(0.95f, 0.80f, 0.35f, 1f);
        RectTransform dcr = dCountObj.GetComponent<RectTransform>();
        dcr.anchorMin = new Vector2(0.4f, 1f);
        dcr.anchorMax = new Vector2(0.85f, 1f);
        dcr.pivot = new Vector2(1f, 1f);
        dcr.anchoredPosition = new Vector2(-220f, -22f);
        dcr.sizeDelta = new Vector2(0f, 32f);
        lobby.deckCountText = dCount;

        lobby.deckCloseBtn = CreateOverlayBackButton(dPanel.transform, "BACK TO CAVE", () => lobby.CloseActiveOverlay());

        // Top Section: Collection Grid & Detail Panel
        GameObject topSectionObj = new GameObject("TopCardSelectionSection", typeof(RectTransform));
        topSectionObj.transform.SetParent(dPanel.transform, false);
        RectTransform tsrt = topSectionObj.GetComponent<RectTransform>();
        tsrt.anchorMin = new Vector2(0f, 0.30f);
        tsrt.anchorMax = new Vector2(1f, 1f);
        tsrt.offsetMin = new Vector2(34f, 12f);
        tsrt.offsetMax = new Vector2(-34f, -72f);

        // Top-Left: Collection Area (~66% width)
        GameObject leftColObj = new GameObject("CollectionArea", typeof(RectTransform));
        leftColObj.transform.SetParent(topSectionObj.transform, false);
        RectTransform lcrt = leftColObj.GetComponent<RectTransform>();
        lcrt.anchorMin = new Vector2(0f, 0f);
        lcrt.anchorMax = new Vector2(0.66f, 1f);
        lcrt.offsetMin = Vector2.zero;
        lcrt.offsetMax = new Vector2(-10f, 0f);

        GameObject colHeadObj = new GameObject("CollectionHeader", typeof(RectTransform));
        colHeadObj.transform.SetParent(leftColObj.transform, false);
        RectTransform chrt = colHeadObj.GetComponent<RectTransform>();
        chrt.anchorMin = new Vector2(0f, 1f);
        chrt.anchorMax = new Vector2(1f, 1f);
        chrt.pivot = new Vector2(0f, 1f);
        chrt.anchoredPosition = Vector2.zero;
        chrt.sizeDelta = new Vector2(0f, 32f);

        GameObject colTitleObj = new GameObject("Title", typeof(RectTransform));
        colTitleObj.transform.SetParent(colHeadObj.transform, false);
        TextMeshProUGUI cht = colTitleObj.AddComponent<TextMeshProUGUI>();
        cht.text = "CARD COLLECTION";
        cht.fontSize = 16;
        cht.fontStyle = FontStyles.Bold;
        cht.color = new Color(0.45f, 0.80f, 0.95f, 1f);
        cht.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform chtr = colTitleObj.GetComponent<RectTransform>();
        chtr.anchorMin = new Vector2(0f, 0f);
        chtr.anchorMax = new Vector2(0.35f, 1f);
        chtr.offsetMin = Vector2.zero;
        chtr.offsetMax = Vector2.zero;

        GameObject filterBar = new GameObject("FilterBar", typeof(RectTransform));
        filterBar.transform.SetParent(colHeadObj.transform, false);
        RectTransform fbrt = filterBar.GetComponent<RectTransform>();
        fbrt.anchorMin = new Vector2(0.35f, 0f);
        fbrt.anchorMax = new Vector2(1f, 1f);
        fbrt.offsetMin = Vector2.zero;
        fbrt.offsetMax = Vector2.zero;
        HorizontalLayoutGroup fbHlg = filterBar.AddComponent<HorizontalLayoutGroup>();
        fbHlg.childControlWidth = false;
        fbHlg.childControlHeight = false;
        fbHlg.spacing = 6f;
        fbHlg.childAlignment = TextAnchor.MiddleRight;

        Action<string, float, DeckCategoryFilter, Action<Button, Image, TextMeshProUGUI>> makeFilterBtn = (label, width, cat, assign) =>
        {
            GameObject bObj = new GameObject($"FilterBtn_{cat}", typeof(RectTransform));
            bObj.transform.SetParent(filterBar.transform, false);
            RectTransform br = bObj.GetComponent<RectTransform>();
            br.sizeDelta = new Vector2(width, 26f);
            Image bBg = bObj.AddComponent<Image>();
            bBg.color = (lobby.currentDeckCategoryFilter == cat) ? new Color(0.68f, 0.54f, 0.28f, 0.95f) : new Color(0.12f, 0.14f, 0.18f, 0.85f);
            Button bBtn = bObj.AddComponent<Button>();
            bBtn.targetGraphic = bBg;
            bBtn.onClick.AddListener(() => lobby.SetDeckCategoryFilter(cat));

            GameObject tObj = new GameObject("Text", typeof(RectTransform));
            tObj.transform.SetParent(bObj.transform, false);
            TextMeshProUGUI txt = tObj.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 11;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = (lobby.currentDeckCategoryFilter == cat) ? Color.white : new Color(0.70f, 0.72f, 0.78f, 1f);
            RectTransform tr = tObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            assign(bBtn, bBg, txt);
        };

        makeFilterBtn("ALL", 52f, DeckCategoryFilter.All, (b, bg, t) => {
            lobby.filterAllBtn = b; lobby.filterAllBg = bg; lobby.filterAllText = t;
        });
        makeFilterBtn("TACTICS", 80f, DeckCategoryFilter.WarriorTactics, (b, bg, t) => {
            lobby.filterTacticsBtn = b; lobby.filterTacticsBg = bg; lobby.filterTacticsText = t;
        });
        makeFilterBtn("DESIRES", 80f, DeckCategoryFilter.LustInstincts, (b, bg, t) => {
            lobby.filterLustBtn = b; lobby.filterLustBg = bg; lobby.filterLustText = t;
        });
        makeFilterBtn("STRUGGLE", 88f, DeckCategoryFilter.StruggleActions, (b, bg, t) => {
            lobby.filterStruggleBtn = b; lobby.filterStruggleBg = bg; lobby.filterStruggleText = t;
        });

        GameObject colScrollObj = new GameObject("CollectionScrollView", typeof(RectTransform));
        colScrollObj.transform.SetParent(leftColObj.transform, false);
        ScrollRect colSr = colScrollObj.AddComponent<ScrollRect>();
        colSr.horizontal = false;
        colSr.vertical = true;
        colSr.movementType = ScrollRect.MovementType.Clamped;
        colSr.scrollSensitivity = 30f;
        RectTransform csrt = colScrollObj.GetComponent<RectTransform>();
        csrt.anchorMin = Vector2.zero;
        csrt.anchorMax = Vector2.one;
        csrt.offsetMin = Vector2.zero;
        csrt.offsetMax = new Vector2(0f, -36f);

        GameObject colViewport = new GameObject("Viewport", typeof(RectTransform));
        colViewport.transform.SetParent(colScrollObj.transform, false);
        colViewport.AddComponent<RectMask2D>();
        RectTransform cvrt2 = colViewport.GetComponent<RectTransform>();
        cvrt2.anchorMin = Vector2.zero;
        cvrt2.anchorMax = Vector2.one;
        cvrt2.sizeDelta = Vector2.zero;
        colSr.viewport = cvrt2;

        GameObject colContent = new GameObject("Content", typeof(RectTransform));
        colContent.transform.SetParent(colViewport.transform, false);
        RectTransform ccrt = colContent.GetComponent<RectTransform>();
        ccrt.anchorMin = new Vector2(0f, 1f);
        ccrt.anchorMax = new Vector2(1f, 1f);
        ccrt.pivot = new Vector2(0.5f, 1f);
        ccrt.anchoredPosition = Vector2.zero;
        ccrt.sizeDelta = Vector2.zero;
        colSr.content = ccrt;

        GridLayoutGroup cglg = colContent.AddComponent<GridLayoutGroup>();
        cglg.cellSize = new Vector2(118f, 165f);
        cglg.spacing = new Vector2(12f, 12f);
        cglg.padding = new RectOffset(6, 6, 6, 6);
        cglg.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter ccsf = colContent.AddComponent<ContentSizeFitter>();
        ccsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        lobby.availableCardsContainer = ccrt;

        // Top-Right: Detail & Equip Panel (~34% width)
        GameObject rightDetailObj = new GameObject("DetailPanel", typeof(RectTransform));
        rightDetailObj.transform.SetParent(topSectionObj.transform, false);
        RectTransform rdrt = rightDetailObj.GetComponent<RectTransform>();
        rdrt.anchorMin = new Vector2(0.66f, 0f);
        rdrt.anchorMax = new Vector2(1f, 1f);
        rdrt.offsetMin = new Vector2(10f, 0f);
        rdrt.offsetMax = Vector2.zero;

        Image dpBg = rightDetailObj.AddComponent<Image>();
        dpBg.color = new Color(0.68f, 0.54f, 0.28f, 0.95f);

        GameObject dpInner = new GameObject("InnerBg", typeof(RectTransform));
        dpInner.transform.SetParent(rightDetailObj.transform, false);
        Image dpInnerImg = dpInner.AddComponent<Image>();
        dpInnerImg.color = new Color(0.07f, 0.08f, 0.12f, 0.98f);
        RectTransform dpirt = dpInner.GetComponent<RectTransform>();
        dpirt.anchorMin = Vector2.zero;
        dpirt.anchorMax = Vector2.one;
        dpirt.offsetMin = new Vector2(2f, 2f);
        dpirt.offsetMax = new Vector2(-2f, -2f);
        dpInner.transform.SetAsFirstSibling();

        GameObject dNameObj = new GameObject("DetailCardName", typeof(RectTransform));
        dNameObj.transform.SetParent(rightDetailObj.transform, false);
        TextMeshProUGUI dNameTxt = dNameObj.AddComponent<TextMeshProUGUI>();
        dNameTxt.text = "Strike";
        dNameTxt.fontSize = 24;
        dNameTxt.fontStyle = FontStyles.Bold;
        dNameTxt.alignment = TextAlignmentOptions.Center;
        dNameTxt.color = new Color(0.96f, 0.88f, 0.50f, 1f);
        RectTransform dnrt = dNameObj.GetComponent<RectTransform>();
        dnrt.anchorMin = new Vector2(0f, 1f);
        dnrt.anchorMax = new Vector2(1f, 1f);
        dnrt.pivot = new Vector2(0.5f, 1f);
        dnrt.anchoredPosition = new Vector2(0f, -12f);
        dnrt.sizeDelta = new Vector2(-20f, 32f);
        lobby.detailCardNameText = dNameTxt;

        GameObject dArtWindow = new GameObject("DetailArtWindow", typeof(RectTransform));
        dArtWindow.transform.SetParent(rightDetailObj.transform, false);
        dArtWindow.AddComponent<RectMask2D>();
        RectTransform dawRt = dArtWindow.GetComponent<RectTransform>();
        dawRt.anchorMin = new Vector2(0.5f, 1f);
        dawRt.anchorMax = new Vector2(0.5f, 1f);
        dawRt.pivot = new Vector2(0.5f, 1f);
        dawRt.anchoredPosition = new Vector2(0f, -48f);
        dawRt.sizeDelta = new Vector2(150f, 150f);

        GameObject dArtObj = new GameObject("Art", typeof(RectTransform));
        dArtObj.transform.SetParent(dArtWindow.transform, false);
        Image dArtImg = dArtObj.AddComponent<Image>();
        dArtImg.preserveAspect = true;
        RectTransform dartRt = dArtObj.GetComponent<RectTransform>();
        dartRt.anchorMin = new Vector2(0.5f, 0.5f);
        dartRt.anchorMax = new Vector2(0.5f, 0.5f);
        dartRt.pivot = new Vector2(0.5f, 0.5f);
        dartRt.anchoredPosition = Vector2.zero;
        dartRt.sizeDelta = new Vector2(280f, 150f);
        lobby.detailCardArtImg = dArtImg;

        GameObject dStatsBox = new GameObject("DetailStatsBox", typeof(RectTransform));
        dStatsBox.transform.SetParent(rightDetailObj.transform, false);
        RectTransform dsrt = dStatsBox.GetComponent<RectTransform>();
        dsrt.anchorMin = new Vector2(0f, 1f);
        dsrt.anchorMax = new Vector2(1f, 1f);
        dsrt.pivot = new Vector2(0.5f, 1f);
        dsrt.anchoredPosition = new Vector2(0f, -206f);
        dsrt.sizeDelta = new Vector2(-16f, 60f);
        VerticalLayoutGroup dStatsVlg = dStatsBox.AddComponent<VerticalLayoutGroup>();
        dStatsVlg.childControlWidth = true;
        dStatsVlg.childControlHeight = false;
        dStatsVlg.spacing = 5f;

        GameObject row1 = new GameObject("Row1", typeof(RectTransform));
        row1.transform.SetParent(dStatsBox.transform, false);
        HorizontalLayoutGroup r1Hlg = row1.AddComponent<HorizontalLayoutGroup>();
        r1Hlg.childControlWidth = false;
        r1Hlg.childControlHeight = false;
        r1Hlg.spacing = 6f;
        r1Hlg.childAlignment = TextAnchor.MiddleCenter;
        RectTransform r1rt = row1.GetComponent<RectTransform>();
        r1rt.sizeDelta = new Vector2(0f, 26f);
        lobby.detailStatsRow1 = row1.transform;

        GameObject row2 = new GameObject("Row2", typeof(RectTransform));
        row2.transform.SetParent(dStatsBox.transform, false);
        HorizontalLayoutGroup r2Hlg = row2.AddComponent<HorizontalLayoutGroup>();
        r2Hlg.childControlWidth = false;
        r2Hlg.childControlHeight = false;
        r2Hlg.spacing = 6f;
        r2Hlg.childAlignment = TextAnchor.MiddleCenter;
        RectTransform r2rt = row2.GetComponent<RectTransform>();
        r2rt.sizeDelta = new Vector2(0f, 26f);
        lobby.detailStatsRow2 = row2.transform;

        GameObject dDescObj = new GameObject("DetailDesc", typeof(RectTransform));
        dDescObj.transform.SetParent(rightDetailObj.transform, false);
        TextMeshProUGUI dDescTxt = dDescObj.AddComponent<TextMeshProUGUI>();
        dDescTxt.fontSize = 22;
        dDescTxt.alignment = TextAlignmentOptions.Top;
        dDescTxt.color = new Color(0.92f, 0.94f, 0.98f, 1f);
        RectTransform ddrt = dDescObj.GetComponent<RectTransform>();
        ddrt.anchorMin = new Vector2(0f, 0f);
        ddrt.anchorMax = new Vector2(1f, 1f);
        ddrt.offsetMin = new Vector2(18f, 75f);
        ddrt.offsetMax = new Vector2(-18f, -274f);
        lobby.detailCardDescText = dDescTxt;

        GameObject dEquipObj = new GameObject("EquipBtn", typeof(RectTransform));
        dEquipObj.transform.SetParent(rightDetailObj.transform, false);
        Image dEquipBg = dEquipObj.AddComponent<Image>();
        dEquipBg.color = new Color(0.22f, 0.55f, 0.35f, 1f);
        Button dEquipBtn = dEquipObj.AddComponent<Button>();
        dEquipBtn.targetGraphic = dEquipBg;
        RectTransform deprt = dEquipObj.GetComponent<RectTransform>();
        deprt.anchorMin = new Vector2(0.5f, 0f);
        deprt.anchorMax = new Vector2(0.5f, 0f);
        deprt.pivot = new Vector2(0.5f, 0f);
        deprt.anchoredPosition = new Vector2(0f, 24f);
        deprt.sizeDelta = new Vector2(260f, 46f);
        lobby.detailEquipBtn = dEquipBtn;
        lobby.detailEquipBtnBg = dEquipBg;

        GameObject dEquipTxtObj = new GameObject("Text", typeof(RectTransform));
        dEquipTxtObj.transform.SetParent(dEquipObj.transform, false);
        TextMeshProUGUI dEquipTxt = dEquipTxtObj.AddComponent<TextMeshProUGUI>();
        dEquipTxt.text = "ADD TO DECK";
        dEquipTxt.fontSize = 18;
        dEquipTxt.fontStyle = FontStyles.Bold;
        dEquipTxt.alignment = TextAlignmentOptions.Center;
        dEquipTxt.color = Color.white;
        RectTransform detr = dEquipTxtObj.GetComponent<RectTransform>();
        detr.anchorMin = Vector2.zero;
        detr.anchorMax = Vector2.one;
        detr.sizeDelta = Vector2.zero;
        lobby.detailEquipBtnText = dEquipTxt;

        dEquipBtn.onClick.AddListener(() =>
        {
            if (CombatManager.Instance != null && lobby.selectedCollectionCard != null)
            {
                if (CombatManager.Instance.AddCardToDeck(lobby.selectedCollectionCard))
                {
                    lobby.RefreshDeck();
                }
            }
        });

        // Bottom Section: Active Deck
        GameObject bottomSecObj = new GameObject("BottomActiveDeckSection", typeof(RectTransform));
        bottomSecObj.transform.SetParent(dPanel.transform, false);
        RectTransform bsrt = bottomSecObj.GetComponent<RectTransform>();
        bsrt.anchorMin = new Vector2(0f, 0f);
        bsrt.anchorMax = new Vector2(1f, 0.30f);
        bsrt.offsetMin = new Vector2(34f, 26f);
        bsrt.offsetMax = new Vector2(-34f, -8f);

        GameObject actHead = new GameObject("ActiveHeader", typeof(RectTransform));
        actHead.transform.SetParent(bottomSecObj.transform, false);
        TextMeshProUGUI aht = actHead.AddComponent<TextMeshProUGUI>();
        aht.text = "ACTIVE DECK (Equipped for Combat) • Hover over a card to reveal [X] to unequip";
        aht.fontSize = 15;
        aht.fontStyle = FontStyles.Bold;
        aht.color = new Color(0.95f, 0.80f, 0.35f, 1f);
        RectTransform ahr = actHead.GetComponent<RectTransform>();
        ahr.anchorMin = new Vector2(0f, 1f);
        ahr.anchorMax = new Vector2(1f, 1f);
        ahr.pivot = new Vector2(0f, 1f);
        ahr.anchoredPosition = Vector2.zero;
        ahr.sizeDelta = new Vector2(0f, 24f);

        GameObject actScrollObj = new GameObject("ActiveScrollView", typeof(RectTransform));
        actScrollObj.transform.SetParent(bottomSecObj.transform, false);
        ScrollRect actSr = actScrollObj.AddComponent<ScrollRect>();
        actSr.horizontal = true;
        actSr.vertical = false;
        actSr.movementType = ScrollRect.MovementType.Clamped;
        actSr.scrollSensitivity = 25f;
        RectTransform asrt = actScrollObj.GetComponent<RectTransform>();
        asrt.anchorMin = Vector2.zero;
        asrt.anchorMax = Vector2.one;
        asrt.offsetMin = Vector2.zero;
        asrt.offsetMax = new Vector2(0f, -26f);

        GameObject actViewport = new GameObject("Viewport", typeof(RectTransform));
        actViewport.transform.SetParent(actScrollObj.transform, false);
        actViewport.AddComponent<RectMask2D>();
        RectTransform avrt = actViewport.GetComponent<RectTransform>();
        avrt.anchorMin = Vector2.zero;
        avrt.anchorMax = Vector2.one;
        avrt.sizeDelta = Vector2.zero;
        actSr.viewport = avrt;

        GameObject actContent = new GameObject("Content", typeof(RectTransform));
        actContent.transform.SetParent(actViewport.transform, false);
        RectTransform acrt = actContent.GetComponent<RectTransform>();
        acrt.anchorMin = new Vector2(0f, 0.5f);
        acrt.anchorMax = new Vector2(0f, 0.5f);
        acrt.pivot = new Vector2(0f, 0.5f);
        acrt.anchoredPosition = Vector2.zero;
        acrt.sizeDelta = new Vector2(0f, 155f);
        actSr.content = acrt;

        HorizontalLayoutGroup ahlg = actContent.AddComponent<HorizontalLayoutGroup>();
        ahlg.spacing = 10;
        ahlg.childControlWidth = false;
        ahlg.childControlHeight = false;
        ahlg.childAlignment = TextAnchor.MiddleLeft;
        ahlg.padding = new RectOffset(6, 6, 4, 4);

        ContentSizeFitter acsf = actContent.AddComponent<ContentSizeFitter>();
        acsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        lobby.activeDeckContainer = acrt;

        // =====================================================================
        // OVERLAY 3: DESIRE TREE (PATHS OF DESIRE)
        // =====================================================================
        GameObject pathsOverlayObj = new GameObject("PathsOverlay", typeof(RectTransform));
        pathsOverlayObj.transform.SetParent(overlaysContainer.transform, false);
        RectTransform port = pathsOverlayObj.GetComponent<RectTransform>();
        port.anchorMin = Vector2.zero;
        port.anchorMax = Vector2.one;
        port.offsetMin = Vector2.zero;
        port.offsetMax = Vector2.zero;
        lobby.pathsOverlay = pathsOverlayObj;

        // Dimmer
        GameObject pDimmer = new GameObject("Dimmer", typeof(RectTransform));
        pDimmer.transform.SetParent(pathsOverlayObj.transform, false);
        RectTransform pdrt = pDimmer.GetComponent<RectTransform>();
        pdrt.anchorMin = Vector2.zero;
        pdrt.anchorMax = Vector2.one;
        pdrt.offsetMin = Vector2.zero;
        pdrt.offsetMax = Vector2.zero;
        Image pdImg = pDimmer.AddComponent<Image>();
        pdImg.color = new Color(0.02f, 0.03f, 0.06f, 0.75f);
        pdImg.raycastTarget = true;

        // Paths Modal Panel (Centered, 1720x960)
        GameObject pPanel = new GameObject("PathsPanel", typeof(RectTransform));
        pPanel.transform.SetParent(pathsOverlayObj.transform, false);
        RectTransform pprt = pPanel.GetComponent<RectTransform>();
        pprt.anchorMin = new Vector2(0.5f, 0.5f);
        pprt.anchorMax = new Vector2(0.5f, 0.5f);
        pprt.pivot = new Vector2(0.5f, 0.5f);
        pprt.sizeDelta = new Vector2(1720f, 960f);

        Image pPanelBg = pPanel.AddComponent<Image>();
        pPanelBg.color = new Color(0.07f, 0.08f, 0.12f, 0.98f);
        Outline pOutline = pPanel.AddComponent<Outline>();
        pOutline.effectColor = new Color(0.85f, 0.45f, 0.85f, 0.85f);
        pOutline.effectDistance = new Vector2(2f, -2f);

        // Top Bar: Path Selectors & Lustid Counter
        GameObject p4TopBar = new GameObject("TreeTopBar", typeof(RectTransform));
        p4TopBar.transform.SetParent(pPanel.transform, false);
        RectTransform p4tbRt = p4TopBar.GetComponent<RectTransform>();
        p4tbRt.anchorMin = new Vector2(0f, 1f);
        p4tbRt.anchorMax = new Vector2(1f, 1f);
        p4tbRt.pivot = new Vector2(0.5f, 1f);
        p4tbRt.anchoredPosition = new Vector2(0f, -24f);
        p4tbRt.sizeDelta = new Vector2(-68f, 44f);

        GameObject p4BtnsRow = new GameObject("PathButtonsRow", typeof(RectTransform));
        p4BtnsRow.transform.SetParent(p4TopBar.transform, false);
        RectTransform p4brRt = p4BtnsRow.GetComponent<RectTransform>();
        p4brRt.anchorMin = new Vector2(0f, 0f);
        p4brRt.anchorMax = new Vector2(0.68f, 1f);
        p4brRt.offsetMin = Vector2.zero;
        p4brRt.offsetMax = Vector2.zero;
        HorizontalLayoutGroup p4brHlg = p4BtnsRow.AddComponent<HorizontalLayoutGroup>();
        p4brHlg.spacing = 10f;
        p4brHlg.childControlWidth = true;
        p4brHlg.childControlHeight = true;

        Action<string, Action<Button, Image, TextMeshProUGUI>> makePathBtn = (label, assign) =>
        {
            GameObject bObj = new GameObject($"Btn_{label.Replace(" ", "")}", typeof(RectTransform));
            bObj.transform.SetParent(p4BtnsRow.transform, false);
            Image bBg = bObj.AddComponent<Image>();
            bBg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
            Button bBtn = bObj.AddComponent<Button>();
            bBtn.targetGraphic = bBg;

            GameObject tObj = new GameObject("Text", typeof(RectTransform));
            tObj.transform.SetParent(bObj.transform, false);
            TextMeshProUGUI txt = tObj.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 15;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            RectTransform tr = tObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            assign(bBtn, bBg, txt);
        };

        makePathBtn("PHALLUS DEVOTEE", (b, bg, t) => {
            lobby.treePathPhallusBtn = b; lobby.treePathPhallusBg = bg; lobby.treePathPhallusText = t;
        });
        makePathBtn("BOUND & SUBMISSIVE", (b, bg, t) => {
            lobby.treePathBoundBtn = b; lobby.treePathBoundBg = bg; lobby.treePathBoundText = t;
        });
        makePathBtn("MONSTER METAMORPHOSIS", (b, bg, t) => {
            lobby.treePathMonsterBtn = b; lobby.treePathMonsterBg = bg; lobby.treePathMonsterText = t;
        });

        // Top Bar Lustid display (Center-Right)
        GameObject p4LustidObj = new GameObject("TreeLustidDisplay", typeof(RectTransform));
        p4LustidObj.transform.SetParent(p4TopBar.transform, false);
        RectTransform p4ldr = p4LustidObj.GetComponent<RectTransform>();
        p4ldr.anchorMin = new Vector2(0.68f, 0f);
        p4ldr.anchorMax = new Vector2(0.88f, 1f);
        p4ldr.offsetMin = new Vector2(10f, 0f);
        p4ldr.offsetMax = Vector2.zero;
        TextMeshProUGUI p4lt = p4LustidObj.AddComponent<TextMeshProUGUI>();
        p4lt.text = "Altar Essence: <color=#FF77DD>0 Lustid</color>";
        p4lt.fontSize = 16;
        p4lt.fontStyle = FontStyles.Bold;
        p4lt.alignment = TextAlignmentOptions.MidlineRight;
        p4lt.color = Color.white;
        lobby.treeHeaderLustidText = p4lt;

        lobby.pathsCloseBtn = CreateOverlayBackButton(pPanel.transform, "BACK TO CAVE", () => lobby.CloseActiveOverlay());

        // Left Column: Tree Nodes Scroll View (~63% width)
        GameObject treeLeftObj = new GameObject("TreeNodesArea", typeof(RectTransform));
        treeLeftObj.transform.SetParent(pPanel.transform, false);
        RectTransform tlaRt = treeLeftObj.GetComponent<RectTransform>();
        tlaRt.anchorMin = new Vector2(0f, 0f);
        tlaRt.anchorMax = new Vector2(0.63f, 1f);
        tlaRt.offsetMin = new Vector2(34f, 28f);
        tlaRt.offsetMax = new Vector2(-12f, -82f);

        Image tlaBg = treeLeftObj.AddComponent<Image>();
        tlaBg.color = new Color(0.08f, 0.09f, 0.12f, 0.90f);
        Outline tlaOutline = treeLeftObj.AddComponent<Outline>();
        tlaOutline.effectColor = new Color(0.35f, 0.20f, 0.40f, 0.60f);
        tlaOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject tlaHeadObj = new GameObject("TreeSectionTitle", typeof(RectTransform));
        tlaHeadObj.transform.SetParent(treeLeftObj.transform, false);
        RectTransform thRt = tlaHeadObj.GetComponent<RectTransform>();
        thRt.anchorMin = new Vector2(0f, 1f);
        thRt.anchorMax = new Vector2(1f, 1f);
        thRt.pivot = new Vector2(0f, 1f);
        thRt.anchoredPosition = new Vector2(16f, -10f);
        thRt.sizeDelta = new Vector2(-32f, 26f);
        TextMeshProUGUI thTxt = tlaHeadObj.AddComponent<TextMeshProUGUI>();
        thTxt.text = "SACRED PROGRESSION TREE (Awaken Keystones to unlock Fetish Cards)";
        thTxt.fontSize = 13;
        thTxt.fontStyle = FontStyles.Bold;
        thTxt.color = new Color(0.95f, 0.85f, 0.40f, 1f);
        thTxt.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject treeScrollObj = new GameObject("TreeScrollView", typeof(RectTransform));
        treeScrollObj.transform.SetParent(treeLeftObj.transform, false);
        ScrollRect treeSr = treeScrollObj.AddComponent<ScrollRect>();
        treeSr.horizontal = false;
        treeSr.vertical = true;
        treeSr.movementType = ScrollRect.MovementType.Clamped;
        treeSr.scrollSensitivity = 25f;
        RectTransform tsrRt = treeScrollObj.GetComponent<RectTransform>();
        tsrRt.anchorMin = Vector2.zero;
        tsrRt.anchorMax = Vector2.one;
        tsrRt.offsetMin = new Vector2(10f, 10f);
        tsrRt.offsetMax = new Vector2(-10f, -40f);

        GameObject treeViewport = new GameObject("Viewport", typeof(RectTransform));
        treeViewport.transform.SetParent(treeScrollObj.transform, false);
        treeViewport.AddComponent<RectMask2D>();
        RectTransform tvRt = treeViewport.GetComponent<RectTransform>();
        tvRt.anchorMin = Vector2.zero;
        tvRt.anchorMax = Vector2.one;
        tvRt.sizeDelta = Vector2.zero;
        treeSr.viewport = tvRt;

        GameObject treeContent = new GameObject("Content", typeof(RectTransform));
        treeContent.transform.SetParent(treeViewport.transform, false);
        RectTransform tcRt = treeContent.GetComponent<RectTransform>();
        tcRt.anchorMin = new Vector2(0f, 0f);
        tcRt.anchorMax = new Vector2(1f, 0f);
        tcRt.pivot = new Vector2(0.5f, 0f);
        tcRt.anchoredPosition = Vector2.zero;
        tcRt.sizeDelta = new Vector2(0f, 1080f);
        treeSr.content = tcRt;
        lobby.treeNodesScrollContent = tcRt;
        lobby.treeScrollRect = treeSr;

        // Tree Inactive / Lock Overlay
        GameObject treeOverlayObj = new GameObject("TreeInactiveOverlay", typeof(RectTransform));
        treeOverlayObj.transform.SetParent(treeLeftObj.transform, false);
        RectTransform toRt = treeOverlayObj.GetComponent<RectTransform>();
        toRt.anchorMin = Vector2.zero;
        toRt.anchorMax = Vector2.one;
        toRt.offsetMin = Vector2.zero;
        toRt.offsetMax = Vector2.zero;
        toRt.SetAsLastSibling();

        Image toBg = treeOverlayObj.AddComponent<Image>();
        toBg.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);
        toBg.raycastTarget = true;

        GameObject toCenter = new GameObject("CenterCard", typeof(RectTransform));
        toCenter.transform.SetParent(treeOverlayObj.transform, false);
        RectTransform tcCardRt = toCenter.GetComponent<RectTransform>();
        tcCardRt.anchorMin = new Vector2(0.5f, 0.5f);
        tcCardRt.anchorMax = new Vector2(0.5f, 0.5f);
        tcCardRt.pivot = new Vector2(0.5f, 0.5f);
        tcCardRt.sizeDelta = new Vector2(440f, 240f);

        Image tcBg = toCenter.AddComponent<Image>();
        tcBg.color = new Color(0.12f, 0.10f, 0.18f, 0.98f);
        Outline tcOutline = toCenter.AddComponent<Outline>();
        tcOutline.effectColor = new Color(0.85f, 0.55f, 0.90f, 0.85f);
        tcOutline.effectDistance = new Vector2(2f, -2f);

        GameObject toTitleObj = new GameObject("Title", typeof(RectTransform));
        toTitleObj.transform.SetParent(toCenter.transform, false);
        RectTransform totRt = toTitleObj.GetComponent<RectTransform>();
        totRt.anchorMin = new Vector2(0f, 1f);
        totRt.anchorMax = new Vector2(1f, 1f);
        totRt.pivot = new Vector2(0.5f, 1f);
        totRt.anchoredPosition = new Vector2(0f, -20f);
        totRt.sizeDelta = new Vector2(-40f, 32f);
        TextMeshProUGUI totTxt = toTitleObj.AddComponent<TextMeshProUGUI>();
        totTxt.fontSize = 18;
        totTxt.fontStyle = FontStyles.Bold;
        totTxt.alignment = TextAlignmentOptions.Center;
        totTxt.color = new Color(0.95f, 0.85f, 0.40f, 1f);

        GameObject toDescObj = new GameObject("Description", typeof(RectTransform));
        toDescObj.transform.SetParent(toCenter.transform, false);
        RectTransform todRt = toDescObj.GetComponent<RectTransform>();
        todRt.anchorMin = new Vector2(0f, 0.35f);
        todRt.anchorMax = new Vector2(1f, 0.82f);
        todRt.offsetMin = new Vector2(20f, 0f);
        todRt.offsetMax = new Vector2(-20f, 0f);
        TextMeshProUGUI todTxt = toDescObj.AddComponent<TextMeshProUGUI>();
        todTxt.fontSize = 13;
        todTxt.alignment = TextAlignmentOptions.Center;
        todTxt.color = new Color(0.85f, 0.85f, 0.92f, 1f);

        GameObject toBtnObj = new GameObject("SelectBtn", typeof(RectTransform));
        toBtnObj.transform.SetParent(toCenter.transform, false);
        RectTransform tobRt = toBtnObj.GetComponent<RectTransform>();
        tobRt.anchorMin = new Vector2(0.5f, 0f);
        tobRt.anchorMax = new Vector2(0.5f, 0f);
        tobRt.pivot = new Vector2(0.5f, 0f);
        tobRt.anchoredPosition = new Vector2(0f, 22f);
        tobRt.sizeDelta = new Vector2(240f, 48f);

        Image tobBg = toBtnObj.AddComponent<Image>();
        tobBg.color = new Color(0.85f, 0.35f, 0.75f, 1f);
        Button tobBtn = toBtnObj.AddComponent<Button>();
        tobBtn.targetGraphic = tobBg;

        GameObject toBtnTxtObj = new GameObject("Text", typeof(RectTransform));
        toBtnTxtObj.transform.SetParent(toBtnObj.transform, false);
        RectTransform tobtRt = toBtnTxtObj.GetComponent<RectTransform>();
        tobtRt.anchorMin = Vector2.zero;
        tobtRt.anchorMax = Vector2.one;
        tobtRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tobtTxt = toBtnTxtObj.AddComponent<TextMeshProUGUI>();
        tobtTxt.text = "SELECT PATH";
        tobtTxt.fontSize = 16;
        tobtTxt.fontStyle = FontStyles.Bold;
        tobtTxt.alignment = TextAlignmentOptions.Center;
        tobtTxt.color = Color.white;

        lobby.treeOverlayPanel = treeOverlayObj;
        lobby.treeOverlayTitleText = totTxt;
        lobby.treeOverlayDescText = todTxt;
        lobby.treeOverlaySelectBtn = tobBtn;
        lobby.treeOverlaySelectBtnBg = tobBg;
        lobby.treeOverlaySelectBtnText = tobtTxt;

        // Right Column: Node Detail / Awakening Inspector (~37% width)
        GameObject treeRightObj = new GameObject("TreeDetailPanel", typeof(RectTransform));
        treeRightObj.transform.SetParent(pPanel.transform, false);
        RectTransform traRt = treeRightObj.GetComponent<RectTransform>();
        traRt.anchorMin = new Vector2(0.63f, 0f);
        traRt.anchorMax = new Vector2(1f, 1f);
        traRt.offsetMin = new Vector2(12f, 28f);
        traRt.offsetMax = new Vector2(-34f, -82f);

        Image traBg = treeRightObj.AddComponent<Image>();
        traBg.color = new Color(0.12f, 0.09f, 0.16f, 0.98f);
        Outline traOutline = treeRightObj.AddComponent<Outline>();
        traOutline.effectColor = new Color(0.85f, 0.70f, 0.30f, 0.85f);
        traOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject dHeadObj = new GameObject("InspectorHeader", typeof(RectTransform));
        dHeadObj.transform.SetParent(treeRightObj.transform, false);
        RectTransform dhr = dHeadObj.GetComponent<RectTransform>();
        dhr.anchorMin = new Vector2(0f, 1f);
        dhr.anchorMax = new Vector2(1f, 1f);
        dhr.pivot = new Vector2(0f, 1f);
        dhr.anchoredPosition = new Vector2(18f, -14f);
        dhr.sizeDelta = new Vector2(-36f, 22f);
        TextMeshProUGUI dht = dHeadObj.AddComponent<TextMeshProUGUI>();
        dht.text = "ALTAR OF AWAKENING";
        dht.fontSize = 12;
        dht.fontStyle = FontStyles.Bold;
        dht.color = new Color(0.85f, 0.70f, 0.30f, 1f);
        dht.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject dtTitleObj = new GameObject("NodeTitle", typeof(RectTransform));
        dtTitleObj.transform.SetParent(treeRightObj.transform, false);
        RectTransform dtrRt = dtTitleObj.GetComponent<RectTransform>();
        dtrRt.anchorMin = new Vector2(0f, 1f);
        dtrRt.anchorMax = new Vector2(1f, 1f);
        dtrRt.pivot = new Vector2(0f, 1f);
        dtrRt.anchoredPosition = new Vector2(18f, -40f);
        dtrRt.sizeDelta = new Vector2(-36f, 32f);
        TextMeshProUGUI dtt = dtTitleObj.AddComponent<TextMeshProUGUI>();
        dtt.text = "Select a Node";
        dtt.fontSize = 19;
        dtt.fontStyle = FontStyles.Bold;
        dtt.color = new Color(1f, 0.90f, 0.45f, 1f);
        lobby.treeDetailTitleText = dtt;

        GameObject dtCatObj = new GameObject("NodeCategory", typeof(RectTransform));
        dtCatObj.transform.SetParent(treeRightObj.transform, false);
        RectTransform dcrRt = dtCatObj.GetComponent<RectTransform>();
        dcrRt.anchorMin = new Vector2(0f, 1f);
        dcrRt.anchorMax = new Vector2(1f, 1f);
        dcrRt.pivot = new Vector2(0f, 1f);
        dcrRt.anchoredPosition = new Vector2(18f, -74f);
        dcrRt.sizeDelta = new Vector2(-36f, 24f);
        TextMeshProUGUI dct = dtCatObj.AddComponent<TextMeshProUGUI>();
        dct.text = "<color=#70D6FF>[PASSIVE TALENT]</color>";
        dct.fontSize = 13;
        dct.fontStyle = FontStyles.Bold;
        lobby.treeDetailCategoryText = dct;

        GameObject dtDescObj = new GameObject("NodeDesc", typeof(RectTransform));
        dtDescObj.transform.SetParent(treeRightObj.transform, false);
        RectTransform ddRt = dtDescObj.GetComponent<RectTransform>();
        ddRt.anchorMin = new Vector2(0f, 1f);
        ddRt.anchorMax = new Vector2(1f, 1f);
        ddRt.pivot = new Vector2(0f, 1f);
        ddRt.anchoredPosition = new Vector2(18f, -110f);
        ddRt.sizeDelta = new Vector2(-36f, 100f);
        TextMeshProUGUI ddt = dtDescObj.AddComponent<TextMeshProUGUI>();
        ddt.text = "Inspect any node on the left to view its awakening properties and bonuses.";
        ddt.fontSize = 14;
        ddt.color = new Color(0.85f, 0.88f, 0.94f, 1f);
        lobby.treeDetailDescText = ddt;

        GameObject dtBonusObj = new GameObject("NodeBonuses", typeof(RectTransform));
        dtBonusObj.transform.SetParent(treeRightObj.transform, false);
        RectTransform dbRt = dtBonusObj.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(0f, 0f);
        dbRt.anchorMax = new Vector2(1f, 1f);
        dbRt.offsetMin = new Vector2(18f, 75f);
        dbRt.offsetMax = new Vector2(-18f, -220f);
        TextMeshProUGUI dbt = dtBonusObj.AddComponent<TextMeshProUGUI>();
        dbt.text = "";
        dbt.fontSize = 13;
        dbt.fontStyle = FontStyles.Bold;
        dbt.color = Color.white;
        lobby.treeDetailBonusText = dbt;

        GameObject pBuyObj = new GameObject("Btn_AwakenNode", typeof(RectTransform));
        pBuyObj.transform.SetParent(treeRightObj.transform, false);
        RectTransform pbrRt2 = pBuyObj.GetComponent<RectTransform>();
        pbrRt2.anchorMin = new Vector2(0f, 0f);
        pbrRt2.anchorMax = new Vector2(1f, 0f);
        pbrRt2.pivot = new Vector2(0.5f, 0f);
        pbrRt2.anchoredPosition = new Vector2(0f, 26f);
        pbrRt2.sizeDelta = new Vector2(-52f, 46f);

        Image pbrBg = pBuyObj.AddComponent<Image>();
        pbrBg.color = new Color(0.65f, 0.22f, 0.60f, 1f);
        Button pbrBtn = pBuyObj.AddComponent<Button>();
        pbrBtn.targetGraphic = pbrBg;
        lobby.treePurchaseBtn = pbrBtn;
        lobby.treePurchaseBtnBg = pbrBg;

        GameObject pbrTxtObj = new GameObject("Text", typeof(RectTransform));
        pbrTxtObj.transform.SetParent(pBuyObj.transform, false);
        TextMeshProUGUI pbrTxt = pbrTxtObj.AddComponent<TextMeshProUGUI>();
        pbrTxt.text = "AWAKEN";
        pbrTxt.fontSize = 16;
        pbrTxt.fontStyle = FontStyles.Bold;
        pbrTxt.alignment = TextAlignmentOptions.Center;
        pbrTxt.color = Color.white;
        RectTransform pbrtr = pbrTxtObj.GetComponent<RectTransform>();
        pbrtr.anchorMin = Vector2.zero;
        pbrtr.anchorMax = Vector2.one;
        pbrtr.sizeDelta = Vector2.zero;
        lobby.treePurchaseBtnText = pbrTxt;

        // =====================================================================
        // OVERLAY 4: SETTINGS & SYSTEM MODAL
        // =====================================================================
        GameObject settingsOverlayObj = new GameObject("SettingsOverlay", typeof(RectTransform));
        settingsOverlayObj.transform.SetParent(overlaysContainer.transform, false);
        RectTransform sort = settingsOverlayObj.GetComponent<RectTransform>();
        sort.anchorMin = Vector2.zero;
        sort.anchorMax = Vector2.one;
        sort.offsetMin = Vector2.zero;
        sort.offsetMax = Vector2.zero;
        lobby.settingsOverlay = settingsOverlayObj;

        // Dimmer
        GameObject sDimmer = new GameObject("Dimmer", typeof(RectTransform));
        sDimmer.transform.SetParent(settingsOverlayObj.transform, false);
        RectTransform sdrt = sDimmer.GetComponent<RectTransform>();
        sdrt.anchorMin = Vector2.zero;
        sdrt.anchorMax = Vector2.one;
        sdrt.offsetMin = Vector2.zero;
        sdrt.offsetMax = Vector2.zero;
        Image sdImg = sDimmer.AddComponent<Image>();
        sdImg.color = new Color(0.02f, 0.03f, 0.06f, 0.75f);
        sdImg.raycastTarget = true;

        // Settings Modal Card (Centered, 600x360)
        GameObject sPanel = new GameObject("SettingsModalCard", typeof(RectTransform));
        sPanel.transform.SetParent(settingsOverlayObj.transform, false);
        RectTransform sprt = sPanel.GetComponent<RectTransform>();
        sprt.anchorMin = new Vector2(0.5f, 0.5f);
        sprt.anchorMax = new Vector2(0.5f, 0.5f);
        sprt.pivot = new Vector2(0.5f, 0.5f);
        sprt.sizeDelta = new Vector2(600f, 360f);

        Image spBg = sPanel.AddComponent<Image>();
        spBg.color = new Color(0.08f, 0.09f, 0.13f, 0.98f);
        Outline spOutline = sPanel.AddComponent<Outline>();
        spOutline.effectColor = new Color(0.75f, 0.65f, 0.40f, 0.85f);
        spOutline.effectDistance = new Vector2(2f, -2f);

        GameObject sTitleObj = new GameObject("Title", typeof(RectTransform));
        sTitleObj.transform.SetParent(sPanel.transform, false);
        TextMeshProUGUI sTitle = sTitleObj.AddComponent<TextMeshProUGUI>();
        sTitle.text = "SETTINGS & SYSTEM";
        sTitle.fontSize = 24;
        sTitle.fontStyle = FontStyles.Bold;
        sTitle.alignment = TextAlignmentOptions.MidlineLeft;
        sTitle.color = new Color(0.95f, 0.85f, 0.60f, 1f);
        RectTransform strt2 = sTitleObj.GetComponent<RectTransform>();
        strt2.anchorMin = new Vector2(0f, 1f);
        strt2.anchorMax = new Vector2(1f, 1f);
        strt2.pivot = new Vector2(0f, 1f);
        strt2.anchoredPosition = new Vector2(32f, -22f);
        strt2.sizeDelta = new Vector2(-120f, 32f);

        // Close X button
        GameObject xBtnObj = new GameObject("CloseXBtn", typeof(RectTransform));
        xBtnObj.transform.SetParent(sPanel.transform, false);
        RectTransform xrt = xBtnObj.GetComponent<RectTransform>();
        xrt.anchorMin = new Vector2(1f, 1f);
        xrt.anchorMax = new Vector2(1f, 1f);
        xrt.pivot = new Vector2(1f, 1f);
        xrt.anchoredPosition = new Vector2(-26f, -20f);
        xrt.sizeDelta = new Vector2(38f, 38f);
        Image xBg = xBtnObj.AddComponent<Image>();
        xBg.color = new Color(0.20f, 0.15f, 0.20f, 0.85f);
        Button xBtn = xBtnObj.AddComponent<Button>();
        xBtn.targetGraphic = xBg;
        lobby.settingsCloseBtn = xBtn;

        GameObject xTxtObj = new GameObject("Text", typeof(RectTransform));
        xTxtObj.transform.SetParent(xBtnObj.transform, false);
        TextMeshProUGUI xTxt = xTxtObj.AddComponent<TextMeshProUGUI>();
        xTxt.text = "X";
        xTxt.fontSize = 18;
        xTxt.fontStyle = FontStyles.Bold;
        xTxt.alignment = TextAlignmentOptions.Center;
        xTxt.color = Color.white;
        RectTransform xtrt = xTxtObj.GetComponent<RectTransform>();
        xtrt.anchorMin = Vector2.zero;
        xtrt.anchorMax = Vector2.one;
        xtrt.sizeDelta = Vector2.zero;

        // Content Area inside Settings Card
        GameObject sContent = new GameObject("ContentArea", typeof(RectTransform));
        sContent.transform.SetParent(sPanel.transform, false);
        RectTransform scrt = sContent.GetComponent<RectTransform>();
        scrt.anchorMin = new Vector2(0f, 0f);
        scrt.anchorMax = new Vector2(1f, 1f);
        scrt.offsetMin = new Vector2(36f, 30f);
        scrt.offsetMax = new Vector2(-36f, -70f);

        // 1. Master Volume Label
        GameObject volLabelObj = new GameObject("VolumeLabel", typeof(RectTransform));
        volLabelObj.transform.SetParent(sContent.transform, false);
        TextMeshProUGUI volLabel = volLabelObj.AddComponent<TextMeshProUGUI>();
        volLabel.text = "Master Volume: 100%";
        volLabel.fontSize = 17;
        volLabel.fontStyle = FontStyles.Bold;
        volLabel.color = Color.white;
        RectTransform vlrt = volLabelObj.GetComponent<RectTransform>();
        vlrt.anchorMin = new Vector2(0f, 1f);
        vlrt.anchorMax = new Vector2(1f, 1f);
        vlrt.pivot = new Vector2(0f, 1f);
        vlrt.anchoredPosition = new Vector2(0f, -10f);
        vlrt.sizeDelta = new Vector2(0f, 26f);
        lobby.settingsMasterVolumeText = volLabel;

        // Master Volume Slider
        GameObject sliderObj = new GameObject("MasterSlider", typeof(RectTransform));
        sliderObj.transform.SetParent(sContent.transform, false);
        RectTransform slrt = sliderObj.GetComponent<RectTransform>();
        slrt.anchorMin = new Vector2(0f, 1f);
        slrt.anchorMax = new Vector2(1f, 1f);
        slrt.pivot = new Vector2(0.5f, 1f);
        slrt.anchoredPosition = new Vector2(0f, -44f);
        slrt.sizeDelta = new Vector2(0f, 28f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = AudioListener.volume;

        GameObject slBgObj = new GameObject("Background", typeof(RectTransform));
        slBgObj.transform.SetParent(sliderObj.transform, false);
        Image slBg = slBgObj.AddComponent<Image>();
        slBg.color = new Color(0.18f, 0.20f, 0.26f, 0.90f);
        RectTransform slbgrt = slBgObj.GetComponent<RectTransform>();
        slbgrt.anchorMin = new Vector2(0f, 0.35f);
        slbgrt.anchorMax = new Vector2(1f, 0.65f);
        slbgrt.offsetMin = Vector2.zero;
        slbgrt.offsetMax = Vector2.zero;

        GameObject slFillArea = new GameObject("Fill Area", typeof(RectTransform));
        slFillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform slfart = slFillArea.GetComponent<RectTransform>();
        slfart.anchorMin = new Vector2(0f, 0.35f);
        slfart.anchorMax = new Vector2(1f, 0.65f);
        slfart.offsetMin = new Vector2(5f, 0f);
        slfart.offsetMax = new Vector2(-5f, 0f);

        GameObject slFill = new GameObject("Fill", typeof(RectTransform));
        slFill.transform.SetParent(slFillArea.transform, false);
        Image slFillImg = slFill.AddComponent<Image>();
        slFillImg.color = new Color(0.85f, 0.45f, 0.85f, 1f);
        RectTransform slfrt = slFill.GetComponent<RectTransform>();
        slfrt.anchorMin = Vector2.zero;
        slfrt.anchorMax = Vector2.one;
        slfrt.sizeDelta = Vector2.zero;
        slider.fillRect = slfrt;

        GameObject slHandleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        slHandleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform slhart = slHandleArea.GetComponent<RectTransform>();
        slhart.anchorMin = Vector2.zero;
        slhart.anchorMax = Vector2.one;
        slhart.offsetMin = new Vector2(14f, 0f);
        slhart.offsetMax = new Vector2(-14f, 0f);

        GameObject slHandle = new GameObject("Handle", typeof(RectTransform));
        slHandle.transform.SetParent(slHandleArea.transform, false);
        Image slHandleImg = slHandle.AddComponent<Image>();
        slHandleImg.color = new Color(1f, 0.88f, 0.40f, 1f);
        RectTransform slhrt = slHandle.GetComponent<RectTransform>();
        slhrt.sizeDelta = new Vector2(22f, 22f);
        slider.handleRect = slhrt;
        slider.targetGraphic = slHandleImg;
        lobby.settingsMasterSlider = slider;

        // 2. Fullscreen Toggle Button
        GameObject fsBtnObj = new GameObject("FullscreenToggleBtn", typeof(RectTransform));
        fsBtnObj.transform.SetParent(sContent.transform, false);
        RectTransform fsrt = fsBtnObj.GetComponent<RectTransform>();
        fsrt.anchorMin = new Vector2(0f, 1f);
        fsrt.anchorMax = new Vector2(1f, 1f);
        fsrt.pivot = new Vector2(0.5f, 1f);
        fsrt.anchoredPosition = new Vector2(0f, -96f);
        fsrt.sizeDelta = new Vector2(0f, 44f);
        Image fsBg = fsBtnObj.AddComponent<Image>();
        fsBg.color = new Color(0.16f, 0.20f, 0.28f, 0.95f);
        Button fsBtn = fsBtnObj.AddComponent<Button>();
        fsBtn.targetGraphic = fsBg;
        lobby.settingsFullscreenToggleBtn = fsBtn;

        GameObject fsTxtObj = new GameObject("Text", typeof(RectTransform));
        fsTxtObj.transform.SetParent(fsBtnObj.transform, false);
        TextMeshProUGUI fsTxt = fsTxtObj.AddComponent<TextMeshProUGUI>();
        fsTxt.text = Screen.fullScreen ? "Fullscreen: [ON]" : "Fullscreen: [OFF]";
        fsTxt.fontSize = 16;
        fsTxt.fontStyle = FontStyles.Bold;
        fsTxt.alignment = TextAlignmentOptions.Center;
        fsTxt.color = Color.white;
        RectTransform fstrt = fsTxtObj.GetComponent<RectTransform>();
        fstrt.anchorMin = Vector2.zero;
        fstrt.anchorMax = Vector2.one;
        fstrt.sizeDelta = Vector2.zero;
        lobby.settingsFullscreenToggleText = fsTxt;

        // 3. Main Menu Button (Red)
        GameObject mmBtnObj = new GameObject("MainMenuBtn", typeof(RectTransform));
        mmBtnObj.transform.SetParent(sContent.transform, false);
        RectTransform mmrt = mmBtnObj.GetComponent<RectTransform>();
        mmrt.anchorMin = new Vector2(0f, 0f);
        mmrt.anchorMax = new Vector2(1f, 0f);
        mmrt.pivot = new Vector2(0.5f, 0f);
        mmrt.anchoredPosition = new Vector2(0f, 8f);
        mmrt.sizeDelta = new Vector2(0f, 44f);
        Image mmBg = mmBtnObj.AddComponent<Image>();
        mmBg.color = new Color(0.55f, 0.20f, 0.25f, 0.95f);
        Button mmBtn = mmBtnObj.AddComponent<Button>();
        mmBtn.targetGraphic = mmBg;
        lobby.settingsMainMenuBtn = mmBtn;

        GameObject mmTxtObj = new GameObject("Text", typeof(RectTransform));
        mmTxtObj.transform.SetParent(mmBtnObj.transform, false);
        TextMeshProUGUI mmTxt = mmTxtObj.AddComponent<TextMeshProUGUI>();
        mmTxt.text = "RETURN TO MAIN MENU";
        mmTxt.fontSize = 16;
        mmTxt.fontStyle = FontStyles.Bold;
        mmTxt.alignment = TextAlignmentOptions.Center;
        mmTxt.color = Color.white;
        RectTransform mmtrt = mmTxtObj.GetComponent<RectTransform>();
        mmtrt.anchorMin = Vector2.zero;
        mmtrt.anchorMax = Vector2.one;
        mmtrt.sizeDelta = Vector2.zero;

        // Hide all overlays initially
        if (wardrobeOverlay != null) wardrobeOverlay.SetActive(false);
        if (deckOverlay != null) deckOverlay.SetActive(false);
        if (pathsOverlay != null) pathsOverlay.SetActive(false);
        if (settingsOverlay != null) settingsOverlay.SetActive(false);
    }

    private static Button CreateOverlayBackButton(Transform parent, string label, Action onClick)
    {
        GameObject btnObj = new GameObject("BackBtn", typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -12f);
        rt.sizeDelta = new Vector2(180f, 40f);

        Image bg = btnObj.AddComponent<Image>();
        Sprite wBtn = LoadUISprite("ui_button.png");
        if (wBtn != null)
        {
            bg.sprite = wBtn;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }
        else
        {
            bg.color = new Color(0.22f, 0.16f, 0.24f, 0.95f);
            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.45f, 0.70f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1.22f, 1.18f, 1.08f, 1f);
        cb.pressedColor = new Color(0.70f, 0.65f, 0.60f, 1f);
        btn.colors = cb;

        if (onClick != null)
        {
            btn.onClick.AddListener(() => onClick.Invoke());
        }

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;

        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 15;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.92f, 0.88f, 0.78f, 1f);
        txt.raycastTarget = false;

        return btn;
    }
}

public class DeckCardHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject removeButton;
    public GameObject focusBorder;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (removeButton != null) removeButton.SetActive(true);
        if (focusBorder != null) focusBorder.SetActive(true);
        transform.localScale = new Vector3(1.05f, 1.05f, 1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (removeButton != null) removeButton.SetActive(false);
        if (focusBorder != null) focusBorder.SetActive(false);
        transform.localScale = Vector3.one;
    }
}
