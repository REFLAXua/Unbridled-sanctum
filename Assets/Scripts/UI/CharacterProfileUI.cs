using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnbridledSanctum.Combat;
using UnbridledSanctum.Data;

/// <summary>
/// Unified Character Profile & Inventory Menu (Modal Overlay).
/// Uses authentic 9-sliced wood UI assets from Assets/sprites/UI:
/// - ui_panel_wood.png (Main right window)
/// - ui_slot_wood.png (Inset containers, semen receptacle squares, item slots)
/// - ui_button.png (Tabs, action buttons, size toggles)
/// - ui_progressbar_frame.png (Progress bars & fluid gauges)
/// 
/// Left screen space (X: 0 to ~720) is completely clear for the Heroine PaperDoll.
/// All text and stats are housed within the docked Right Panel (1160px wide).
/// </summary>
public class CharacterProfileUI : MonoBehaviour
{
    public static CharacterProfileUI Instance { get; private set; }

    [Header("Dependencies")]
    public CharacterState characterState;
    public InventoryManager inventoryManager;
    public PaperDollController paperDoll;
    public CombatManager combatManager;

    [Header("Panels")]
    public GameObject rootPanel;
    public GameObject dimmerOverlay;
    public RectTransform rightPanelRect;
    public RectTransform tab1Content; // Vessel Status & Purity
    public RectTransform tab2Content; // Wardrobe & Bag

    [Header("UI Skin Sprites")]
    public Sprite woodPanelSprite;
    public Sprite woodSlotSprite;
    public Sprite woodButtonSprite;
    public Sprite woodProgressFrameSprite;

    [Header("Tab Navigation")]
    public Button tab1Btn;
    public Button tab2Btn;
    public Image tab1BtnBg;
    public Image tab2BtnBg;
    public TextMeshProUGUI tab1BtnText;
    public TextMeshProUGUI tab2BtnText;
    public Button closeBtn;

    [Header("Tab 1: Top Section - Vessel Characteristics & Purity")]
    public TextMeshProUGUI purityStatusText;
    public TextMeshProUGUI purityDescText;
    public TextMeshProUGUI exposureAlertText;
    public TextMeshProUGUI vitalsHpText;
    public TextMeshProUGUI vitalsShieldText;
    public TextMeshProUGUI vitalsLustText;
    public TextMeshProUGUI vitalsCorruptionText;
    public GameObject vitalsCorruptionRow;
    public TextMeshProUGUI vitalsLumieresText;
    public TextMeshProUGUI vitalsClothesStatusText;
    public GameObject vitalsClothesStatusRow;

    [Header("Tab 1: Bottom Section - 3 Semen Receptacles")]
    public Image mouthReceptacleFill;
    public TextMeshProUGUI mouthReceptacleText;
    public TextMeshProUGUI mouthReceptacleStatus;

    public Image wombReceptacleFill;
    public TextMeshProUGUI wombReceptacleText;
    public TextMeshProUGUI wombReceptacleStatus;

    public Image analReceptacleFill;
    public TextMeshProUGUI analReceptacleText;
    public TextMeshProUGUI analReceptacleStatus;

    public TextMeshProUGUI totalSemenText;

    [Header("Tab 2: Inventory & Wardrobe")]
    public Transform inventoryGridContainer;
    public TextMeshProUGUI detailItemName;
    public TextMeshProUGUI detailItemStats;
    public TextMeshProUGUI detailItemDesc;
    public Button equipButton;
    public Image equipButtonBg;
    public TextMeshProUGUI equipButtonText;
    public Button unequipButton;
    public Image unequipButtonBg;
    public TextMeshProUGUI unequipButtonText;
    public Button repairButton;
    public Image repairButtonBg;
    public TextMeshProUGUI repairButtonText;
    public TextMeshProUGUI equippedSummaryText;

    [Header("Body Stabilization")]
    public Button[] bodySizeButtons;
    public Image[] bodySizeButtonBgs;

    private int currentTab = 0;
    private bool isOpen = false;
    private ItemData selectedItem = null;

    private class InventorySlotView
    {
        public GameObject root;
        public Image bg;
        public Button btn;
        public Image iconImg;
        public TextMeshProUGUI nameTxt;
    }

    private List<InventorySlotView> slotViews = new List<InventorySlotView>();

    public bool IsOpen => isOpen;

    private static Sprite s_WhiteSprite;
    public static Sprite WhiteSprite
    {
        get
        {
            if (s_WhiteSprite == null)
            {
                Texture2D t = Texture2D.whiteTexture;
                s_WhiteSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
            }
            return s_WhiteSprite;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LoadUISkinSprites();
    }

    private void Start()
    {
        if (characterState == null) characterState = FindFirstObjectByType<CharacterState>();
        if (inventoryManager == null) inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (paperDoll == null) paperDoll = FindFirstObjectByType<PaperDollController>();
        if (combatManager == null) combatManager = CombatManager.Instance ?? FindFirstObjectByType<CombatManager>();

        if (characterState != null)
        {
            characterState.OnEquipmentChanged += RefreshUI;
            characterState.OnBodySizeChanged += (s) => RefreshUI();
        }

        if (combatManager != null)
        {
            combatManager.OnPersistentStatsChanged += RefreshUI;
        }

        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged += RefreshInventoryTab;
        }

        if (tab1Btn != null) tab1Btn.onClick.AddListener(() => SwitchTab(0));
        if (tab2Btn != null) tab2Btn.onClick.AddListener(() => SwitchTab(1));
        if (closeBtn != null) closeBtn.onClick.AddListener(Close);

        if (rootPanel != null) rootPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (characterState != null)
        {
            characterState.OnEquipmentChanged -= RefreshUI;
        }
        if (combatManager != null)
        {
            combatManager.OnPersistentStatsChanged -= RefreshUI;
        }
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= RefreshInventoryTab;
        }
    }

    private void Update()
    {
        bool tabPressed = false;
        bool escPressed = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame) tabPressed = true;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) escPressed = true;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Tab)) tabPressed = true;
            if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
        }

        if (tabPressed)
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                if (CanOpenMenu())
                {
                    Open(0);
                }
            }
        }
        else if (escPressed && isOpen)
        {
            Close();
        }
    }

    private bool CanOpenMenu()
    {
        if (combatManager != null && combatManager.isCombatActive) return false;

        GameUIManager gum = FindFirstObjectByType<GameUIManager>();
        if (gum != null)
        {
            if (gum.currentState == GameUIManager.GameFlowState.StartMenu ||
                gum.currentState == GameUIManager.GameFlowState.CharacterCreator)
            {
                return false;
            }
        }

        return true;
    }

    public void LoadUISkinSprites()
    {
        if (woodPanelSprite == null) woodPanelSprite = LoadUISprite("ui_panel_wood.png");
        if (woodSlotSprite == null) woodSlotSprite = LoadUISprite("ui_slot_wood.png");
        if (woodButtonSprite == null) woodButtonSprite = LoadUISprite("ui_button.png");
        if (woodProgressFrameSprite == null) woodProgressFrameSprite = LoadUISprite("ui_progressbar_frame.png");
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

    public void Initialize(CharacterState state, InventoryManager inv, PaperDollController doll)
    {
        characterState = state;
        inventoryManager = inv;
        paperDoll = doll;
        combatManager = CombatManager.Instance ?? FindFirstObjectByType<CombatManager>();
    }

    public void Open(int tabIndex = 0)
    {
        if (rootPanel == null) return;
        isOpen = true;
        rootPanel.SetActive(true);
        rootPanel.transform.SetAsLastSibling();

        if (paperDoll != null)
        {
            paperDoll.gameObject.SetActive(true);
            paperDoll.transform.SetAsLastSibling();
            paperDoll.SetExpeditionMode(false);
            paperDoll.SetZoomControlsVisible(true);
        }

        SwitchTab(tabIndex);
        RefreshUI();
    }

    public void Close()
    {
        if (rootPanel == null) return;
        isOpen = false;
        rootPanel.SetActive(false);

        // Hide PaperDoll in Lobby cave view, or restore Expedition Mode on map
        GameUIManager gum = FindFirstObjectByType<GameUIManager>();
        if (paperDoll != null)
        {
            if (gum != null && gum.currentState == GameUIManager.GameFlowState.Lobby)
            {
                paperDoll.gameObject.SetActive(false);
                paperDoll.SetZoomControlsVisible(false);
            }
            else if (gum != null && gum.currentState == GameUIManager.GameFlowState.Expedition)
            {
                paperDoll.SetExpeditionMode(true);
                paperDoll.SetZoomControlsVisible(false);
            }
            else
            {
                paperDoll.gameObject.SetActive(false);
            }
        }
    }

    public static void ApplyButtonHover(Button btn, Color normalCol, Color hoverCol, Color pressCol)
    {
        if (btn == null) return;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = normalCol;
        cb.highlightedColor = hoverCol;
        cb.pressedColor = pressCol;
        cb.selectedColor = normalCol;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
    }

    public void SwitchTab(int tabIndex)
    {
        currentTab = tabIndex;
        if (tab1Content != null) tab1Content.gameObject.SetActive(currentTab == 0);
        if (tab2Content != null) tab2Content.gameObject.SetActive(currentTab == 1);

        Color activeTabBg = (woodButtonSprite != null) ? new Color(1.25f, 1.15f, 0.95f, 1f) : new Color(0.25f, 0.45f, 0.70f, 0.95f);
        Color inactiveTabBg = (woodButtonSprite != null) ? new Color(0.60f, 0.58f, 0.55f, 0.85f) : new Color(0.12f, 0.14f, 0.20f, 0.90f);
        Color hoverTabBg = new Color(1.45f, 1.35f, 1.05f, 1f);
        Color pressTabBg = new Color(0.80f, 0.75f, 0.65f, 1f);

        if (tab1BtnBg != null) tab1BtnBg.color = (currentTab == 0) ? activeTabBg : inactiveTabBg;
        if (tab2BtnBg != null) tab2BtnBg.color = (currentTab == 1) ? activeTabBg : inactiveTabBg;

        if (tab1Btn != null) ApplyButtonHover(tab1Btn, (currentTab == 0) ? activeTabBg : inactiveTabBg, hoverTabBg, pressTabBg);
        if (tab2Btn != null) ApplyButtonHover(tab2Btn, (currentTab == 1) ? activeTabBg : inactiveTabBg, hoverTabBg, pressTabBg);

        if (tab1BtnText != null) tab1BtnText.color = (currentTab == 0) ? new Color(1.0f, 0.92f, 0.60f, 1f) : new Color(0.75f, 0.72f, 0.68f, 0.85f);
        if (tab2BtnText != null) tab2BtnText.color = (currentTab == 1) ? new Color(1.0f, 0.92f, 0.60f, 1f) : new Color(0.75f, 0.72f, 0.68f, 0.85f);

        if (currentTab == 0) RefreshVesselStatusTab();
        else RefreshInventoryTab();
    }

    public void RefreshUI()
    {
        if (!isOpen) return;
        if (currentTab == 0) RefreshVesselStatusTab();
        else RefreshInventoryTab();
    }

    private void RefreshVesselStatusTab()
    {
        CombatManager cm = combatManager != null ? combatManager : CombatManager.Instance;
        if (cm == null) return;

        // Remove recursive repair in UI refresh (fixes StackOverflowException)
        // (Clothes are repaired upon entering lobby via GameUIManager)

        // 1. Purity Status & Description
        bool isPure = cm.isPureRunActive && cm.persistentSemenVolumeMl == 0 && cm.persistentSeductionStacks == 0;
        if (purityStatusText != null)
        {
            if (isPure)
            {
                purityStatusText.text = "<color=#44FF88><b>[PURE VESSEL]</b></color>";
                if (purityDescText != null) purityDescText.text = "The core is untouched by monster seed. No corruption recorded in the Sanctuary archives.";
            }
            else if (cm.persistentCorruption >= cm.maxCorruption)
            {
                purityStatusText.text = "<color=#AA44FF><b>[FALLEN VESSEL]</b></color>";
                if (purityDescText != null) purityDescText.text = "Consumed by cavern lust and submitted to monster supremacy. Senses overwhelmed.";
            }
            else if (cm.persistentSemenVolumeMl > 0)
            {
                purityStatusText.text = $"<color=#FF5588><b>[TAINTED VESSEL]</b></color>";
                if (purityDescText != null) purityDescText.text = $"Defiled by monster semen ({cm.persistentSemenVolumeMl} ml retained in internal receptacles).";
            }
            else
            {
                purityStatusText.text = "<color=#FFAA44><b>[SUBMITTED]</b></color>";
                if (purityDescText != null) purityDescText.text = "Orgasmic climax surrendered during struggle. Willpower shaken.";
            }
        }

        // 2. Exposure & Modesty State
        if (exposureAlertText != null)
        {
            bool chestExposed = cm.persistentUpperClothesHp <= 0 && cm.persistentUnderUpperClothesHp <= 0;
            bool groinExposed = cm.persistentLowerClothesHp <= 0 && cm.persistentUnderLowerClothesHp <= 0;

            if (chestExposed && groinExposed)
                exposureAlertText.text = "<color=#FF2255><b>[COMPLETELY NUDE & DEFENSELESS]</b></color> All modesty lost! Exposed to intimate penetration!";
            else if (groinExposed)
                exposureAlertText.text = "<color=#FF4466><b>[GROIN EXPOSED]</b></color> Panties shredded! Vulnerable to vaginal/anal penetration!";
            else if (chestExposed)
                exposureAlertText.text = "<color=#FFAA33><b>[CHEST EXPOSED]</b></color> Bra shredded! Breasts bared to direct sensual stimulation!";
            else
                exposureAlertText.text = "<color=#44DD88><b>[MODESTY PRESERVED]</b></color> Undergarments securely protecting vessel anatomy.";
        }

        // 3. Vitals & Characteristics Stack
        GameUIManager gum = FindFirstObjectByType<GameUIManager>();
        bool isLobby = (gum != null && gum.currentState == GameUIManager.GameFlowState.Lobby);

        if (vitalsCorruptionRow != null) vitalsCorruptionRow.SetActive(!isLobby);
        if (vitalsClothesStatusRow != null) vitalsClothesStatusRow.SetActive(!isLobby);

        if (vitalsHpText != null) vitalsHpText.text = $"<color=#66FF88><b>{cm.persistentHp} / {cm.persistentMaxHp} HP</b></color>";
        if (vitalsShieldText != null) vitalsShieldText.text = $"<color=#66CCFF><b>{cm.persistentShield} Shield</b></color>";
        if (vitalsLustText != null) vitalsLustText.text = $"<color=#FFAACC><b>{cm.persistentSeduction}%</b></color> ({cm.persistentSeductionStacks}/3 Stacks)";
        
        if (!isLobby)
        {
            if (vitalsCorruptionText != null) vitalsCorruptionText.text = $"<color=#DDAAFF><b>{cm.persistentCorruption} / {cm.maxCorruption}</b></color>";
            if (vitalsClothesStatusText != null)
            {
                bool allIntact = cm.persistentUpperClothesHp > 0 && cm.persistentLowerClothesHp > 0 &&
                                 cm.persistentUnderUpperClothesHp > 0 && cm.persistentUnderLowerClothesHp > 0;
                vitalsClothesStatusText.text = allIntact 
                    ? "<color=#66CCFF><b>All 4 Garments Pristine (100%)</b></color>" 
                    : "<color=#FF7777><b>Garments Damaged or Torn</b></color>";
            }
        }

        if (vitalsLumieresText != null)
        {
            int lum = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentLumieres : 0;
            vitalsLumieresText.text = $"<color=#FFE460><b>{lum}</b></color>";
        }

        // 4. Semen Receptacles (Squares)
        // Throat / Mouth
        int mouth = cm.persistentSemenMouthMl;
        int maxMouth = CombatManager.MAX_SEMEN_MOUTH_ML;
        float mouthPct = (float)mouth / maxMouth;
        if (mouthReceptacleFill != null)
        {
            mouthReceptacleFill.fillAmount = mouthPct;
            mouthReceptacleFill.gameObject.SetActive(mouthPct > 0.001f);
        }
        if (mouthReceptacleText != null) mouthReceptacleText.text = $"<b>{mouth} / {maxMouth} ml</b>\n<size=13><color=#FFEECC>({Mathf.RoundToInt(mouthPct * 100)}% Filled)</color></size>";
        if (mouthReceptacleStatus != null)
        {
            if (mouth == 0) mouthReceptacleStatus.text = "<color=#88DDAA>[Pure & Clear]</color>";
            else if (mouthPct < 0.5f) mouthReceptacleStatus.text = "<color=#FFAACC>[Seed Swallowed]</color>";
            else if (mouthPct < 1.0f) mouthReceptacleStatus.text = "<color=#FF77AA>[Deep Influx Swell]</color>";
            else mouthReceptacleStatus.text = "<color=#FF3366>[Choking Overflow!]</color>";
        }

        // Womb / Uterus
        int womb = cm.persistentSemenWombMl;
        int maxWomb = CombatManager.MAX_SEMEN_WOMB_ML;
        float wombPct = (float)womb / maxWomb;
        if (wombReceptacleFill != null)
        {
            wombReceptacleFill.fillAmount = wombPct;
            wombReceptacleFill.gameObject.SetActive(wombPct > 0.001f);
        }
        if (wombReceptacleText != null) wombReceptacleText.text = $"<b>{womb} / {maxWomb} ml</b>\n<size=13><color=#FFEECC>({Mathf.RoundToInt(wombPct * 100)}% Filled)</color></size>";
        if (wombReceptacleStatus != null)
        {
            if (womb == 0) wombReceptacleStatus.text = "<color=#88DDAA>[Barren & Pure]</color>";
            else if (wombPct < 0.5f) wombReceptacleStatus.text = "<color=#FFAACC>[Seed Imbued]</color>";
            else if (wombPct < 1.0f) wombReceptacleStatus.text = "<color=#FF77AA>[Womb Swelling]</color>";
            else wombReceptacleStatus.text = "<color=#FF3366>[Crucible Flooded!]</color>";
        }

        // Anal / Sphincter
        int anal = cm.persistentSemenAnalMl;
        int maxAnal = CombatManager.MAX_SEMEN_ANAL_ML;
        float analPct = (float)anal / maxAnal;
        if (analReceptacleFill != null)
        {
            analReceptacleFill.fillAmount = analPct;
            analReceptacleFill.gameObject.SetActive(analPct > 0.001f);
        }
        if (analReceptacleText != null) analReceptacleText.text = $"<b>{anal} / {maxAnal} ml</b>\n<size=13><color=#FFEECC>({Mathf.RoundToInt(analPct * 100)}% Filled)</color></size>";
        if (analReceptacleStatus != null)
        {
            if (anal == 0) analReceptacleStatus.text = "<color=#88DDAA>[Tight & Pure]</color>";
            else if (analPct < 0.5f) analReceptacleStatus.text = "<color=#FFAACC>[Seed Filled]</color>";
            else if (analPct < 1.0f) analReceptacleStatus.text = "<color=#FF77AA>[Full Pressure]</color>";
            else analReceptacleStatus.text = "<color=#FF3366>[Overflow Leakage!]</color>";
        }

        if (totalSemenText != null)
        {
            totalSemenText.text = $"Total Semen Absorbed: <color=#FFEEFF><b>{cm.persistentSemenVolumeMl} ml</b></color>";
        }
    }

    private void RefreshInventoryTab()
    {
        if (inventoryGridContainer == null) return;

        if (equippedSummaryText != null && characterState != null)
        {
            ItemData u = characterState.GetEquippedItem(EquipSlot.UpperClothes);
            ItemData l = characterState.GetEquippedItem(EquipSlot.LowerClothes);
            ItemData uu = characterState.GetEquippedItem(EquipSlot.UnderUpper);
            ItemData ul = characterState.GetEquippedItem(EquipSlot.UnderLower);
            ItemData w = characterState.GetEquippedItem(EquipSlot.Weapon);

            string uStr = u != null ? $"<color=#66CCFF>{u.itemName}</color>" : "<color=#8899AA>None</color>";
            string lStr = l != null ? $"<color=#66CCFF>{l.itemName}</color>" : "<color=#8899AA>None</color>";
            string uuStr = uu != null ? $"<color=#AA88FF>{uu.itemName}</color>" : "<color=#8899AA>None</color>";
            string ulStr = ul != null ? $"<color=#AA88FF>{ul.itemName}</color>" : "<color=#8899AA>None</color>";
            string wStr = w != null ? $"<color=#FFCC44>{w.itemName}</color>" : "<color=#8899AA>Unarmed</color>";

            equippedSummaryText.text = $"<b>Equipped:</b> Top: {uStr} | Bot: {lStr} | Bra: {uuStr} | Panties: {ulStr} | Weapon: {wStr}";
        }

        if (inventoryManager != null)
        {
            var items = inventoryManager.Items;

            // Ensure pooled slot views
            while (slotViews.Count < items.Count)
            {
                slotViews.Add(CreateSlotView());
            }

            for (int i = 0; i < slotViews.Count; i++)
            {
                if (i < items.Count)
                {
                    ItemData item = items[i];
                    InventorySlotView view = slotViews[i];
                    view.root.SetActive(true);
                    view.root.name = $"Slot_{item.name}";

                    bool isEquipped = IsItemEquipped(item);
                    bool isSel = (selectedItem == item);

                    Color slotNormal;
                    Color slotHover;
                    Color slotPress;

                    if (isEquipped)
                    {
                        // Подсветка надетой одежды (золотая благородная рамка, аналогично активной Flesh Morphology)
                        slotNormal = new Color(1.30f, 1.20f, 0.95f, 1f);
                        slotHover = new Color(1.48f, 1.38f, 1.10f, 1f);
                        slotPress = new Color(0.90f, 0.82f, 0.70f, 1f);
                    }
                    else if (isSel)
                    {
                        // Выбранный, но не надетый предмет в инвентаре
                        slotNormal = new Color(1.05f, 1.12f, 1.25f, 1f);
                        slotHover = new Color(1.25f, 1.30f, 1.45f, 1f);
                        slotPress = new Color(0.75f, 0.80f, 0.90f, 1f);
                    }
                    else
                    {
                        // Нейтральное состояние: заметно светлее (0.85 вместо 0.65)
                        slotNormal = new Color(0.85f, 0.85f, 0.85f, 1f);
                        slotHover = new Color(1.15f, 1.10f, 1.00f, 1f);
                        slotPress = new Color(0.65f, 0.65f, 0.65f, 1f);
                    }

                    view.bg.color = slotNormal;
                    ApplyButtonHover(view.btn, slotNormal, slotHover, slotPress);

                    view.btn.onClick.RemoveAllListeners();
                    view.btn.onClick.AddListener(() =>
                    {
                        selectedItem = item;
                        RefreshInventoryTab();
                    });

                    // Item Icon
                    if (item.icon != null)
                    {
                        view.iconImg.sprite = item.icon;
                        view.iconImg.gameObject.SetActive(true);
                    }
                    else
                    {
                        view.iconImg.gameObject.SetActive(false);
                    }

                    // Item Name
                    view.nameTxt.text = item.itemName;
                    view.nameTxt.color = isEquipped ? new Color(1.0f, 0.92f, 0.65f, 1f) : (isSel ? Color.white : new Color(0.85f, 0.83f, 0.80f, 0.95f));
                    view.nameTxt.fontStyle = isEquipped ? FontStyles.Bold : FontStyles.Normal;
                }
                else
                {
                    slotViews[i].root.SetActive(false);
                }
            }
        }

        RefreshDetailCard();
        RefreshBodySizeButtons();
    }

    private InventorySlotView CreateSlotView()
    {
        GameObject slotObj = new GameObject("InventorySlot", typeof(RectTransform));
        slotObj.transform.SetParent(inventoryGridContainer, false);
        RectTransform rt = slotObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(105f, 105f);

        Image bg = slotObj.AddComponent<Image>();
        if (woodSlotSprite != null)
        {
            bg.sprite = woodSlotSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        }
        else
        {
            bg.color = new Color(0.20f, 0.22f, 0.28f, 0.95f);
        }

        Button btn = slotObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        // Item Icon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
        iconObj.transform.SetParent(slotObj.transform, false);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.preserveAspect = true;
        RectTransform irt = iconObj.GetComponent<RectTransform>();
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(12, 22);
        irt.offsetMax = new Vector2(-12, -12);

        // Item Name Badge
        GameObject nameObj = new GameObject("NameText", typeof(RectTransform));
        nameObj.transform.SetParent(slotObj.transform, false);
        TextMeshProUGUI txt = nameObj.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 12;
        txt.alignment = TextAlignmentOptions.Bottom;
        RectTransform nrt = nameObj.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0f, 0f);
        nrt.anchorMax = new Vector2(1f, 0.35f);
        nrt.offsetMin = new Vector2(6f, 6f);
        nrt.offsetMax = new Vector2(-6f, 0f);

        return new InventorySlotView
        {
            root = slotObj,
            bg = bg,
            btn = btn,
            iconImg = iconImg,
            nameTxt = txt
        };
    }

    private bool IsItemEquipped(ItemData item)
    {
        if (characterState == null || item == null) return false;
        return characterState.IsEquipped(item);
    }

    private void RefreshDetailCard()
    {
        if (selectedItem == null)
        {
            if (detailItemName != null) detailItemName.text = "Select an item";
            if (detailItemStats != null) detailItemStats.text = "—";
            if (detailItemDesc != null) detailItemDesc.text = "Select any equipment or garment from your inventory to inspect its properties and equip it.";
            if (equipButton != null) equipButton.gameObject.SetActive(false);
            if (unequipButton != null) unequipButton.gameObject.SetActive(false);
            if (repairButton != null) repairButton.gameObject.SetActive(false);
            return;
        }

        if (detailItemName != null) detailItemName.text = $"<color=#FFE4A0><b>{selectedItem.itemName}</b></color>";
        if (detailItemStats != null)
        {
            if (selectedItem.equipSlot == EquipSlot.Weapon)
            {
                string skillName = !string.IsNullOrEmpty(selectedItem.grantedCardId) ? selectedItem.grantedCardId : "None";
                if (CombatManager.Instance != null)
                {
                    var cd = CombatManager.Instance.unlockedCards.Find(c => c.id == selectedItem.grantedCardId);
                    if (cd != null) skillName = cd.cardName;
                }
                string bonus = "";
                if (selectedItem.damageReduction > 0) bonus += $" | Armor: +{selectedItem.damageReduction}";
                if (selectedItem.bonusSeductionPower > 0) bonus += $" | +{selectedItem.bonusSeductionPower}% Seduce";
                if (selectedItem.seductionResistance > 0) bonus += $" | +{selectedItem.seductionResistance}% Lust Res";
                if (selectedItem.bonusMaxHp > 0) bonus += $" | +{selectedItem.bonusMaxHp} HP";

                detailItemStats.text = $"Slot: <color=#FFCC44>Weapon</color> | Innate Action: <color=#66FFCC>{skillName}</color> | Durability: <color=#44FF88>{selectedItem.durability} HP</color>{bonus}";
            }
            else
            {
                detailItemStats.text = $"Slot: <color=#66CCFF>{selectedItem.equipSlot}</color> | Durability: <color=#44FF88>{selectedItem.durability} HP</color> | Armor: <color=#FFDD44>+{selectedItem.damageReduction}</color>";
            }
        }
        if (detailItemDesc != null) detailItemDesc.text = selectedItem.description;

        bool isEq = IsItemEquipped(selectedItem);
        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(!isEq && selectedItem.isEquippable);
            Color eqNorm = (woodButtonSprite != null) ? new Color(0.80f, 1.15f, 0.80f, 1f) : new Color(0.20f, 0.55f, 0.30f, 0.95f);
            Color eqHov = new Color(1.05f, 1.45f, 1.05f, 1f);
            Color eqPress = new Color(0.60f, 0.90f, 0.60f, 1f);
            ApplyButtonHover(equipButton, eqNorm, eqHov, eqPress);
            if (equipButtonBg != null) equipButtonBg.color = eqNorm;

            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(() =>
            {
                if (characterState != null)
                {
                    characterState.EquipItem(selectedItem);
                    CombatManager.Instance?.ResetPlayerStatsForNewExpedition();
                    RefreshUI();
                }
            });
        }

        if (unequipButton != null)
        {
            unequipButton.gameObject.SetActive(isEq);
            Color ueqNorm = (woodButtonSprite != null) ? new Color(1.10f, 0.75f, 0.75f, 1f) : new Color(0.60f, 0.25f, 0.25f, 0.95f);
            Color ueqHov = new Color(1.40f, 0.90f, 0.90f, 1f);
            Color ueqPress = new Color(0.80f, 0.55f, 0.55f, 1f);
            ApplyButtonHover(unequipButton, ueqNorm, ueqHov, ueqPress);
            if (unequipButtonBg != null) unequipButtonBg.color = ueqNorm;

            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(() =>
            {
                if (characterState != null)
                {
                    characterState.UnequipItem(selectedItem.equipSlot);
                    CombatManager.Instance?.ResetPlayerStatsForNewExpedition();
                    RefreshUI();
                }
            });
        }

        if (repairButton != null)
        {
            repairButton.gameObject.SetActive(isEq);
            Color repNorm = (woodButtonSprite != null) ? new Color(0.85f, 1.05f, 1.25f, 1f) : new Color(0.25f, 0.45f, 0.70f, 0.95f);
            Color repHov = new Color(1.05f, 1.30f, 1.50f, 1f);
            Color repPress = new Color(0.65f, 0.80f, 0.95f, 1f);
            ApplyButtonHover(repairButton, repNorm, repHov, repPress);
            if (repairButtonBg != null) repairButtonBg.color = repNorm;

            repairButton.onClick.RemoveAllListeners();
            repairButton.onClick.AddListener(() =>
            {
                if (CombatManager.Instance != null)
                {
                    CombatManager.Instance.RepairPlayerClothes();
                    RefreshUI();
                }
            });
        }
    }

    private void RefreshBodySizeButtons()
    {
        if (bodySizeButtons == null || bodySizeButtonBgs == null || characterState == null) return;
        int activeSize = (int)characterState.CurrentBodySize;

        for (int i = 0; i < bodySizeButtons.Length && i < bodySizeButtonBgs.Length; i++)
        {
            int sizeIdx = i;
            bool isActive = (sizeIdx == activeSize);

            Color normalCol = isActive ? new Color(1.25f, 1.15f, 0.95f, 1f) : new Color(0.65f, 0.65f, 0.65f, 1f);
            Color hoverCol = isActive ? new Color(1.45f, 1.35f, 1.05f, 1f) : new Color(1.05f, 1.00f, 0.90f, 1f);
            Color pressCol = isActive ? new Color(0.85f, 0.80f, 0.70f, 1f) : new Color(0.50f, 0.50f, 0.50f, 1f);

            if (bodySizeButtonBgs[i] != null)
            {
                bodySizeButtonBgs[i].color = normalCol;
            }

            if (bodySizeButtons[i] != null)
            {
                ApplyButtonHover(bodySizeButtons[i], normalCol, hoverCol, pressCol);
                bodySizeButtons[i].onClick.RemoveAllListeners();
                bodySizeButtons[i].onClick.AddListener(() =>
                {
                    characterState.CurrentBodySize = (BodySizeType)sizeIdx;
                    RefreshBodySizeButtons();
                });
            }
        }
    }

    // =========================================================================
    // Dynamic UI Construction (With Assets/sprites/UI Wood Skins & Generous Paddings)
    // =========================================================================
    public static CharacterProfileUI CreateDynamic(Transform parent, CharacterState state, InventoryManager inv, PaperDollController doll)
    {
        GameObject root = new GameObject("CharacterProfileUI", typeof(RectTransform));
        root.transform.SetParent(parent, false);

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        CharacterProfileUI ui = root.AddComponent<CharacterProfileUI>();
        ui.rootPanel = root;
        ui.characterState = state;
        ui.inventoryManager = inv;
        ui.paperDoll = doll;
        ui.LoadUISkinSprites();

        // 1. Dark Vignette Dimmer (Blocks clicks to background scene)
        GameObject dimmer = new GameObject("Dimmer", typeof(RectTransform));
        dimmer.transform.SetParent(root.transform, false);
        Image dimmerImg = dimmer.AddComponent<Image>();
        dimmerImg.color = new Color(0.02f, 0.03f, 0.06f, 0.85f);
        dimmerImg.raycastTarget = true;
        RectTransform drt = dimmer.GetComponent<RectTransform>();
        drt.anchorMin = Vector2.zero;
        drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero;
        drt.offsetMax = Vector2.zero;
        ui.dimmerOverlay = dimmer;

        // 2. Main Right Panel (Docked to right, 1160px wide, leaving X: 0 to ~720 completely clear for PaperDoll)
        GameObject rightPanel = new GameObject("RightMainPanel", typeof(RectTransform));
        rightPanel.transform.SetParent(root.transform, false);
        Image rPanelBg = rightPanel.AddComponent<Image>();
        if (ui.woodPanelSprite != null)
        {
            rPanelBg.sprite = ui.woodPanelSprite;
            rPanelBg.type = Image.Type.Sliced;
            rPanelBg.color = Color.white;
        }
        else
        {
            rPanelBg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);
        }

        RectTransform rprt = rightPanel.GetComponent<RectTransform>();
        rprt.anchorMin = new Vector2(1f, 0f);
        rprt.anchorMax = new Vector2(1f, 1f);
        rprt.pivot = new Vector2(1f, 0.5f);
        rprt.anchoredPosition = new Vector2(-25f, 0f);
        rprt.sizeDelta = new Vector2(1160f, -40f);
        ui.rightPanelRect = rprt;

        // 3. Header Bar inside Right Panel
        GameObject header = new GameObject("HeaderBar", typeof(RectTransform));
        header.transform.SetParent(rightPanel.transform, false);
        RectTransform hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = new Vector2(0f, -16f);
        hrt.sizeDelta = new Vector2(-40f, 58f);

        // Header Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
        titleObj.transform.SetParent(header.transform, false);
        TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.fontSize = 22;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.text = "<color=#FFE4A0>VESSEL STATUS & WARDROBE</color>";
        titleTxt.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform trt = titleObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.02f, 0f);
        trt.anchorMax = new Vector2(0.42f, 1f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        // Tab 1 Button: Vessel Status (Using ui_button.png)
        GameObject tab1Obj = new GameObject("Tab1_VesselStatus", typeof(RectTransform));
        tab1Obj.transform.SetParent(header.transform, false);
        Image tab1Bg = tab1Obj.AddComponent<Image>();
        if (ui.woodButtonSprite != null)
        {
            tab1Bg.sprite = ui.woodButtonSprite;
            tab1Bg.type = Image.Type.Sliced;
            tab1Bg.color = new Color(1.25f, 1.15f, 0.95f, 1f);
        }
        else
        {
            tab1Bg.color = new Color(0.25f, 0.45f, 0.70f, 0.95f);
        }

        Button tab1B = tab1Obj.AddComponent<Button>();
        tab1B.targetGraphic = tab1Bg;
        ui.tab1Btn = tab1B;
        ui.tab1BtnBg = tab1Bg;
        RectTransform tab1Rt = tab1Obj.GetComponent<RectTransform>();
        tab1Rt.anchorMin = new Vector2(0.44f, 0.08f);
        tab1Rt.anchorMax = new Vector2(0.68f, 0.92f);
        tab1Rt.offsetMin = Vector2.zero;
        tab1Rt.offsetMax = Vector2.zero;

        GameObject tab1TxtObj = new GameObject("Text", typeof(RectTransform));
        tab1TxtObj.transform.SetParent(tab1Obj.transform, false);
        TextMeshProUGUI t1Txt = tab1TxtObj.AddComponent<TextMeshProUGUI>();
        t1Txt.fontSize = 17;
        t1Txt.fontStyle = FontStyles.Bold;
        t1Txt.alignment = TextAlignmentOptions.Center;
        t1Txt.text = "1. VESSEL STATUS";
        t1Txt.color = new Color(1.0f, 0.92f, 0.60f, 1f);
        ui.tab1BtnText = t1Txt;
        RectTransform t1trt = tab1TxtObj.GetComponent<RectTransform>();
        t1trt.anchorMin = Vector2.zero;
        t1trt.anchorMax = Vector2.one;
        t1trt.offsetMin = Vector2.zero;
        t1trt.offsetMax = Vector2.zero;

        // Tab 2 Button: Wardrobe & Bag (Using ui_button.png)
        GameObject tab2Obj = new GameObject("Tab2_Inventory", typeof(RectTransform));
        tab2Obj.transform.SetParent(header.transform, false);
        Image tab2Bg = tab2Obj.AddComponent<Image>();
        if (ui.woodButtonSprite != null)
        {
            tab2Bg.sprite = ui.woodButtonSprite;
            tab2Bg.type = Image.Type.Sliced;
            tab2Bg.color = new Color(0.60f, 0.58f, 0.55f, 0.85f);
        }
        else
        {
            tab2Bg.color = new Color(0.12f, 0.14f, 0.20f, 0.90f);
        }

        Button tab2B = tab2Obj.AddComponent<Button>();
        tab2B.targetGraphic = tab2Bg;
        ui.tab2Btn = tab2B;
        ui.tab2BtnBg = tab2Bg;
        RectTransform tab2Rt = tab2Obj.GetComponent<RectTransform>();
        tab2Rt.anchorMin = new Vector2(0.70f, 0.08f);
        tab2Rt.anchorMax = new Vector2(0.92f, 0.92f);
        tab2Rt.offsetMin = Vector2.zero;
        tab2Rt.offsetMax = Vector2.zero;

        GameObject tab2TxtObj = new GameObject("Text", typeof(RectTransform));
        tab2TxtObj.transform.SetParent(tab2Obj.transform, false);
        TextMeshProUGUI t2Txt = tab2TxtObj.AddComponent<TextMeshProUGUI>();
        t2Txt.fontSize = 17;
        t2Txt.fontStyle = FontStyles.Bold;
        t2Txt.alignment = TextAlignmentOptions.Center;
        t2Txt.text = "2. WARDROBE & BAG";
        t2Txt.color = new Color(0.75f, 0.72f, 0.68f, 0.85f);
        ui.tab2BtnText = t2Txt;
        RectTransform t2trt = tab2TxtObj.GetComponent<RectTransform>();
        t2trt.anchorMin = Vector2.zero;
        t2trt.anchorMax = Vector2.one;
        t2trt.offsetMin = Vector2.zero;
        t2trt.offsetMax = Vector2.zero;

        // Close Button [X]
        GameObject closeObj = new GameObject("CloseBtn", typeof(RectTransform));
        closeObj.transform.SetParent(header.transform, false);
        Image closeBg = closeObj.AddComponent<Image>();
        if (ui.woodButtonSprite != null)
        {
            closeBg.sprite = ui.woodButtonSprite;
            closeBg.type = Image.Type.Sliced;
            closeBg.color = new Color(1.1f, 0.45f, 0.45f, 1f);
        }
        else
        {
            closeBg.color = new Color(0.70f, 0.20f, 0.20f, 0.95f);
        }

        Button closeB = closeObj.AddComponent<Button>();
        closeB.targetGraphic = closeBg;
        ApplyButtonHover(closeB, new Color(1.10f, 0.45f, 0.45f, 1f), new Color(1.40f, 0.60f, 0.60f, 1f), new Color(0.75f, 0.25f, 0.25f, 1f));
        ui.closeBtn = closeB;
        RectTransform clrt = closeObj.GetComponent<RectTransform>();
        clrt.anchorMin = new Vector2(0.935f, 0.08f);
        clrt.anchorMax = new Vector2(0.995f, 0.92f);
        clrt.offsetMin = Vector2.zero;
        clrt.offsetMax = Vector2.zero;

        GameObject clTxtObj = new GameObject("Text", typeof(RectTransform));
        clTxtObj.transform.SetParent(closeObj.transform, false);
        TextMeshProUGUI clTxt = clTxtObj.AddComponent<TextMeshProUGUI>();
        clTxt.fontSize = 20;
        clTxt.fontStyle = FontStyles.Bold;
        clTxt.alignment = TextAlignmentOptions.Center;
        clTxt.text = "X";
        clTxt.color = Color.white;
        RectTransform ctlrt = clTxtObj.GetComponent<RectTransform>();
        ctlrt.anchorMin = Vector2.zero;
        ctlrt.anchorMax = Vector2.one;
        ctlrt.offsetMin = Vector2.zero;
        ctlrt.offsetMax = Vector2.zero;

        // 4. Content Container inside Right Panel
        GameObject contentArea = new GameObject("ContentArea", typeof(RectTransform));
        contentArea.transform.SetParent(rightPanel.transform, false);
        RectTransform cart = contentArea.GetComponent<RectTransform>();
        cart.anchorMin = Vector2.zero;
        cart.anchorMax = Vector2.one;
        cart.offsetMin = new Vector2(25f, 25f);
        cart.offsetMax = new Vector2(-25f, -85f);

        // =========================================================================
        // TAB 1: VESSEL STATUS (Top: Characteristics & Purity, Bottom: Receptacles)
        // =========================================================================
        GameObject t1Container = new GameObject("Tab1_Content", typeof(RectTransform));
        t1Container.transform.SetParent(contentArea.transform, false);
        RectTransform t1crt = t1Container.GetComponent<RectTransform>();
        t1crt.anchorMin = Vector2.zero;
        t1crt.anchorMax = Vector2.one;
        t1crt.offsetMin = Vector2.zero;
        t1crt.offsetMax = Vector2.zero;
        ui.tab1Content = t1crt;

        // Top Section: Vessel Characteristics & Purity (Replacing redundant armor bars in lobby)
        GameObject charSection = new GameObject("CharacteristicsSection", typeof(RectTransform));
        charSection.transform.SetParent(t1Container.transform, false);
        Image cSecBg = charSection.AddComponent<Image>();
        if (ui.woodSlotSprite != null)
        {
            cSecBg.sprite = ui.woodSlotSprite;
            cSecBg.type = Image.Type.Sliced;
            cSecBg.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        }
        else
        {
            cSecBg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        }

        RectTransform csrt = charSection.GetComponent<RectTransform>();
        csrt.anchorMin = new Vector2(0f, 0.515f);
        csrt.anchorMax = new Vector2(1f, 1f);
        csrt.offsetMin = Vector2.zero;
        csrt.offsetMax = Vector2.zero;

        // Section Title with generous padding from borders!
        GameObject cTitleObj = new GameObject("SectionTitle", typeof(RectTransform));
        cTitleObj.transform.SetParent(charSection.transform, false);
        TextMeshProUGUI ctTxt = cTitleObj.AddComponent<TextMeshProUGUI>();
        ctTxt.fontSize = 19;
        ctTxt.fontStyle = FontStyles.Bold;
        ctTxt.text = "<color=#FFE4A0>VESSEL CHARACTERISTICS & PURITY</color>";
        RectTransform ctr = cTitleObj.GetComponent<RectTransform>();
        ctr.anchorMin = new Vector2(0f, 1f);
        ctr.anchorMax = new Vector2(1f, 1f);
        ctr.pivot = new Vector2(0f, 1f);
        ctr.anchoredPosition = new Vector2(28f, -20f);
        ctr.sizeDelta = new Vector2(-56f, 32f);

        // Left Sub-Box: Purity & Modesty (With generous inside padding)
        GameObject puritySubBox = new GameObject("PuritySubBox", typeof(RectTransform));
        puritySubBox.transform.SetParent(charSection.transform, false);
        RectTransform psrt = puritySubBox.GetComponent<RectTransform>();
        psrt.anchorMin = new Vector2(0.04f, 0.08f);
        psrt.anchorMax = new Vector2(0.48f, 0.82f);
        psrt.offsetMin = Vector2.zero;
        psrt.offsetMax = Vector2.zero;

        // Purity Banner
        GameObject purObj = new GameObject("PurityStatusText", typeof(RectTransform));
        purObj.transform.SetParent(puritySubBox.transform, false);
        TextMeshProUGUI purTxt = purObj.AddComponent<TextMeshProUGUI>();
        purTxt.fontSize = 22;
        purTxt.fontStyle = FontStyles.Bold;
        purTxt.alignment = TextAlignmentOptions.TopLeft;
        purTxt.text = "<color=#44FF88>[PURE VESSEL]</color>";
        RectTransform purRt = purObj.GetComponent<RectTransform>();
        purRt.anchorMin = new Vector2(0.02f, 0.68f);
        purRt.anchorMax = new Vector2(0.98f, 0.98f);
        purRt.offsetMin = Vector2.zero;
        purRt.offsetMax = Vector2.zero;
        ui.purityStatusText = purTxt;

        // Purity Description
        GameObject purDescObj = new GameObject("PurityDescText", typeof(RectTransform));
        purDescObj.transform.SetParent(puritySubBox.transform, false);
        TextMeshProUGUI pDescTxt = purDescObj.AddComponent<TextMeshProUGUI>();
        pDescTxt.fontSize = 15;
        pDescTxt.alignment = TextAlignmentOptions.TopLeft;
        pDescTxt.color = new Color(0.85f, 0.88f, 0.92f, 0.95f);
        pDescTxt.text = "Untainted by monster seed. Vessel core is pristine.";
        RectTransform pdrt = purDescObj.GetComponent<RectTransform>();
        pdrt.anchorMin = new Vector2(0.02f, 0.28f);
        pdrt.anchorMax = new Vector2(0.98f, 0.66f);
        pdrt.offsetMin = Vector2.zero;
        pdrt.offsetMax = Vector2.zero;
        ui.purityDescText = pDescTxt;

        // Exposure Alert Line
        GameObject expObj = new GameObject("ExposureAlert", typeof(RectTransform));
        expObj.transform.SetParent(puritySubBox.transform, false);
        TextMeshProUGUI expTxt = expObj.AddComponent<TextMeshProUGUI>();
        expTxt.fontSize = 15;
        expTxt.fontStyle = FontStyles.Bold;
        expTxt.alignment = TextAlignmentOptions.BottomLeft;
        expTxt.text = "<color=#44DD88>[MODESTY PRESERVED]</color>";
        RectTransform exprt = expObj.GetComponent<RectTransform>();
        exprt.anchorMin = new Vector2(0.02f, 0.04f);
        exprt.anchorMax = new Vector2(0.98f, 0.24f);
        exprt.offsetMin = Vector2.zero;
        exprt.offsetMax = Vector2.zero;
        ui.exposureAlertText = expTxt;

        // Right Sub-Box: Stats List (With generous padding)
        GameObject statsSubBox = new GameObject("StatsSubBox", typeof(RectTransform));
        statsSubBox.transform.SetParent(charSection.transform, false);
        VerticalLayoutGroup svlg = statsSubBox.AddComponent<VerticalLayoutGroup>();
        svlg.spacing = 10;
        svlg.childForceExpandHeight = false;
        svlg.childControlHeight = false;
        svlg.childForceExpandWidth = true;
        svlg.childControlWidth = true;
        svlg.padding = new RectOffset(20, 20, 12, 12);
        RectTransform ssrt = statsSubBox.GetComponent<RectTransform>();
        ssrt.anchorMin = new Vector2(0.52f, 0.08f);
        ssrt.anchorMax = new Vector2(0.96f, 0.82f);
        ssrt.offsetMin = Vector2.zero;
        ssrt.offsetMax = Vector2.zero;

        ui.vitalsHpText = CreateDetailStatRow(statsSubBox.transform, "Vitality Pool", "34 / 34 HP", out _);
        ui.vitalsShieldText = CreateDetailStatRow(statsSubBox.transform, "Guard Shield", "0 Shield", out _);
        ui.vitalsLustText = CreateDetailStatRow(statsSubBox.transform, "Current Lust", "0% (0/3 Stacks)", out _);
        ui.vitalsCorruptionText = CreateDetailStatRow(statsSubBox.transform, "Corruption Level", "0 / 3", out ui.vitalsCorruptionRow);
        ui.vitalsLumieresText = CreateDetailStatRow(statsSubBox.transform, "Lumieres Carried", "0", out _);
        ui.vitalsClothesStatusText = CreateDetailStatRow(statsSubBox.transform, "Garments Condition", "All 4 Layers Intact (100%)", out ui.vitalsClothesStatusRow);

        // Bottom Section: 3 Semen Receptacles
        GameObject recSection = new GameObject("SemenReceptaclesSection", typeof(RectTransform));
        recSection.transform.SetParent(t1Container.transform, false);
        Image rSecBg = recSection.AddComponent<Image>();
        if (ui.woodSlotSprite != null)
        {
            rSecBg.sprite = ui.woodSlotSprite;
            rSecBg.type = Image.Type.Sliced;
            rSecBg.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        }
        else
        {
            rSecBg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        }

        RectTransform rsrt = recSection.GetComponent<RectTransform>();
        rsrt.anchorMin = new Vector2(0f, 0f);
        rsrt.anchorMax = new Vector2(1f, 0.485f);
        rsrt.offsetMin = Vector2.zero;
        rsrt.offsetMax = Vector2.zero;

        // Receptacle Section Header with Generous Padding!
        GameObject rTitleObj = new GameObject("ReceptacleTitle", typeof(RectTransform));
        rTitleObj.transform.SetParent(recSection.transform, false);
        TextMeshProUGUI rtTxt = rTitleObj.AddComponent<TextMeshProUGUI>();
        rtTxt.fontSize = 19;
        rtTxt.fontStyle = FontStyles.Bold;
        rtTxt.text = "<color=#FFAACC>INTERNAL CRUCIBLE & SEMEN RECEPTACLES</color>";
        RectTransform rtr = rTitleObj.GetComponent<RectTransform>();
        rtr.anchorMin = new Vector2(0f, 1f);
        rtr.anchorMax = new Vector2(0.55f, 1f);
        rtr.pivot = new Vector2(0f, 1f);
        rtr.anchoredPosition = new Vector2(28f, -20f);
        rtr.sizeDelta = new Vector2(0f, 30f);

        GameObject totObj = new GameObject("TotalSemenBadge", typeof(RectTransform));
        totObj.transform.SetParent(recSection.transform, false);
        TextMeshProUGUI totTxt = totObj.AddComponent<TextMeshProUGUI>();
        totTxt.fontSize = 17;
        totTxt.fontStyle = FontStyles.Bold;
        totTxt.alignment = TextAlignmentOptions.MidlineRight;
        totTxt.text = "Total Semen Absorbed: <color=#FFEEFF><b>0 ml</b></color>";
        RectTransform totr = totObj.GetComponent<RectTransform>();
        totr.anchorMin = new Vector2(0.55f, 1f);
        totr.anchorMax = new Vector2(1f, 1f);
        totr.pivot = new Vector2(1f, 1f);
        totr.anchoredPosition = new Vector2(-28f, -20f);
        totr.sizeDelta = new Vector2(0f, 30f);
        ui.totalSemenText = totTxt;

        // 3 Square Semen Receptacles (Horizontal Layout with Generous Padding)
        GameObject sqContainer = new GameObject("SquaresRow", typeof(RectTransform));
        sqContainer.transform.SetParent(recSection.transform, false);
        HorizontalLayoutGroup shlg = sqContainer.AddComponent<HorizontalLayoutGroup>();
        shlg.spacing = 28;
        shlg.childForceExpandWidth = true;
        shlg.childForceExpandHeight = true;
        shlg.childControlWidth = true;
        shlg.childControlHeight = true;
        shlg.padding = new RectOffset(20, 20, 12, 16);
        RectTransform sqrt = sqContainer.GetComponent<RectTransform>();
        sqrt.anchorMin = new Vector2(0.03f, 0.04f);
        sqrt.anchorMax = new Vector2(0.97f, 0.83f);
        sqrt.offsetMin = Vector2.zero;
        sqrt.offsetMax = Vector2.zero;

        // Square 1: Throat / Mouth (100ml)
        CreateReceptacleSquare(sqContainer.transform, "THROAT / MOUTH", "Oral Receptacle (Max 100ml)",
            ui.woodSlotSprite, ui.woodProgressFrameSprite,
            out ui.mouthReceptacleFill, out ui.mouthReceptacleText, out ui.mouthReceptacleStatus);

        // Square 2: Womb / Uterus (200ml)
        CreateReceptacleSquare(sqContainer.transform, "WOMB / UTERUS", "Living Crucible (Max 200ml)",
            ui.woodSlotSprite, ui.woodProgressFrameSprite,
            out ui.wombReceptacleFill, out ui.wombReceptacleText, out ui.wombReceptacleStatus);

        // Square 3: Anal / Sphincter (150ml)
        CreateReceptacleSquare(sqContainer.transform, "ANAL / SPHINCTER", "Anal Tract (Max 150ml)",
            ui.woodSlotSprite, ui.woodProgressFrameSprite,
            out ui.analReceptacleFill, out ui.analReceptacleText, out ui.analReceptacleStatus);

        // =========================================================================
        // TAB 2: INVENTORY & WARDROBE
        // =========================================================================
        GameObject t2Container = new GameObject("Tab2_Content", typeof(RectTransform));
        t2Container.transform.SetParent(contentArea.transform, false);
        RectTransform t2crt = t2Container.GetComponent<RectTransform>();
        t2crt.anchorMin = Vector2.zero;
        t2crt.anchorMax = Vector2.one;
        t2crt.offsetMin = Vector2.zero;
        t2crt.offsetMax = Vector2.zero;
        ui.tab2Content = t2crt;

        // Top Summary Line with proper padding from left border!
        GameObject eqSumObj = new GameObject("EquippedSummaryBox", typeof(RectTransform));
        eqSumObj.transform.SetParent(t2Container.transform, false);
        Image eqSumBg = eqSumObj.AddComponent<Image>();
        if (ui.woodSlotSprite != null)
        {
            eqSumBg.sprite = ui.woodSlotSprite;
            eqSumBg.type = Image.Type.Sliced;
            eqSumBg.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        }
        else
        {
            eqSumBg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        }

        RectTransform esrt = eqSumObj.GetComponent<RectTransform>();
        esrt.anchorMin = new Vector2(0f, 0.92f);
        esrt.anchorMax = new Vector2(1f, 1f);
        esrt.offsetMin = Vector2.zero;
        esrt.offsetMax = Vector2.zero;

        GameObject eqSumTxtObj = new GameObject("Text", typeof(RectTransform));
        eqSumTxtObj.transform.SetParent(eqSumObj.transform, false);
        TextMeshProUGUI eqSumTxt = eqSumTxtObj.AddComponent<TextMeshProUGUI>();
        eqSumTxt.fontSize = 16;
        eqSumTxt.alignment = TextAlignmentOptions.MidlineLeft;
        eqSumTxt.text = "<b>Equipped:</b> Top: None | Bottom: None | Bra: None | Panties: None";
        RectTransform estrt = eqSumTxtObj.GetComponent<RectTransform>();
        estrt.anchorMin = Vector2.zero;
        estrt.anchorMax = Vector2.one;
        estrt.offsetMin = new Vector2(28f, 0f); // Generous padding to the right!
        estrt.offsetMax = new Vector2(-28f, 0f);
        ui.equippedSummaryText = eqSumTxt;

        // Inventory Grid Box (Left half) with Generous Padding!
        GameObject gridBox = new GameObject("InventoryGridBox", typeof(RectTransform));
        gridBox.transform.SetParent(t2Container.transform, false);
        Image gBoxBg = gridBox.AddComponent<Image>();
        if (ui.woodSlotSprite != null)
        {
            gBoxBg.sprite = ui.woodSlotSprite;
            gBoxBg.type = Image.Type.Sliced;
            gBoxBg.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        }
        else
        {
            gBoxBg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        }

        RectTransform gbrt = gridBox.GetComponent<RectTransform>();
        gbrt.anchorMin = new Vector2(0f, 0.13f);
        gbrt.anchorMax = new Vector2(0.56f, 0.90f);
        gbrt.offsetMin = Vector2.zero;
        gbrt.offsetMax = Vector2.zero;

        GameObject gridObj = new GameObject("Grid", typeof(RectTransform));
        gridObj.transform.SetParent(gridBox.transform, false);
        GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(105f, 105f);
        glg.spacing = new Vector2(14f, 14f);
        glg.padding = new RectOffset(24, 24, 24, 24); // Increased paddings!
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;
        RectTransform grt = gridObj.GetComponent<RectTransform>();
        grt.anchorMin = Vector2.zero;
        grt.anchorMax = Vector2.one;
        grt.offsetMin = Vector2.zero;
        grt.offsetMax = Vector2.zero;
        ui.inventoryGridContainer = grt;

        // Item Detail Card (Right half)
        GameObject detailCard = new GameObject("DetailCard", typeof(RectTransform));
        detailCard.transform.SetParent(t2Container.transform, false);
        Image dCardBg = detailCard.AddComponent<Image>();
        if (ui.woodSlotSprite != null)
        {
            dCardBg.sprite = ui.woodSlotSprite;
            dCardBg.type = Image.Type.Sliced;
            dCardBg.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        }
        else
        {
            dCardBg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        }

        RectTransform dcrt = detailCard.GetComponent<RectTransform>();
        dcrt.anchorMin = new Vector2(0.58f, 0.13f);
        dcrt.anchorMax = new Vector2(1f, 0.90f);
        dcrt.offsetMin = Vector2.zero;
        dcrt.offsetMax = Vector2.zero;

        // Detail Name
        GameObject dnObj = new GameObject("ItemName", typeof(RectTransform));
        dnObj.transform.SetParent(detailCard.transform, false);
        TextMeshProUGUI dnTxt = dnObj.AddComponent<TextMeshProUGUI>();
        dnTxt.fontSize = 22;
        dnTxt.fontStyle = FontStyles.Bold;
        dnTxt.text = "<color=#FFE4A0>Select an item</color>";
        RectTransform dnrt = dnObj.GetComponent<RectTransform>();
        dnrt.anchorMin = new Vector2(0.06f, 0.84f);
        dnrt.anchorMax = new Vector2(0.94f, 0.96f);
        dnrt.offsetMin = Vector2.zero;
        dnrt.offsetMax = Vector2.zero;
        ui.detailItemName = dnTxt;

        // Detail Stats
        GameObject dsObj = new GameObject("ItemStats", typeof(RectTransform));
        dsObj.transform.SetParent(detailCard.transform, false);
        TextMeshProUGUI dsTxt = dsObj.AddComponent<TextMeshProUGUI>();
        dsTxt.fontSize = 16;
        dsTxt.text = "Slot: — | Durability: —";
        RectTransform dsrt = dsObj.GetComponent<RectTransform>();
        dsrt.anchorMin = new Vector2(0.06f, 0.72f);
        dsrt.anchorMax = new Vector2(0.94f, 0.83f);
        dsrt.offsetMin = Vector2.zero;
        dsrt.offsetMax = Vector2.zero;
        ui.detailItemStats = dsTxt;

        // Detail Desc
        GameObject ddObj = new GameObject("ItemDesc", typeof(RectTransform));
        ddObj.transform.SetParent(detailCard.transform, false);
        TextMeshProUGUI ddTxt = ddObj.AddComponent<TextMeshProUGUI>();
        ddTxt.fontSize = 15;
        ddTxt.text = "Select any equipment or garment from your inventory to inspect its stats and wear it.";
        ddTxt.color = new Color(0.85f, 0.88f, 0.92f, 0.95f);
        RectTransform ddrt = ddObj.GetComponent<RectTransform>();
        ddrt.anchorMin = new Vector2(0.06f, 0.30f);
        ddrt.anchorMax = new Vector2(0.94f, 0.68f);
        ddrt.offsetMin = Vector2.zero;
        ddrt.offsetMax = Vector2.zero;
        ui.detailItemDesc = ddTxt;

        // Equip Button (Using ui_button.png)
        GameObject eqbObj = new GameObject("EquipBtn", typeof(RectTransform));
        eqbObj.transform.SetParent(detailCard.transform, false);
        Image eqbBg = eqbObj.AddComponent<Image>();
        if (ui.woodButtonSprite != null)
        {
            eqbBg.sprite = ui.woodButtonSprite;
            eqbBg.type = Image.Type.Sliced;
            eqbBg.color = new Color(0.80f, 1.15f, 0.85f, 1f);
        }
        else
        {
            eqbBg.color = new Color(0.20f, 0.55f, 0.85f, 0.95f);
        }

        Button eqB = eqbObj.AddComponent<Button>();
        eqB.targetGraphic = eqbBg;
        ui.equipButton = eqB;
        ui.equipButtonBg = eqbBg;
        RectTransform eqbrt = eqbObj.GetComponent<RectTransform>();
        eqbrt.anchorMin = new Vector2(0.06f, 0.08f);
        eqbrt.anchorMax = new Vector2(0.48f, 0.23f);
        eqbrt.offsetMin = Vector2.zero;
        eqbrt.offsetMax = Vector2.zero;

        GameObject eqbTxtObj = new GameObject("Text", typeof(RectTransform));
        eqbTxtObj.transform.SetParent(eqbObj.transform, false);
        TextMeshProUGUI eqbTxt = eqbTxtObj.AddComponent<TextMeshProUGUI>();
        eqbTxt.fontSize = 18;
        eqbTxt.fontStyle = FontStyles.Bold;
        eqbTxt.alignment = TextAlignmentOptions.Center;
        eqbTxt.text = "EQUIP";
        eqbTxt.color = new Color(0.15f, 0.35f, 0.15f, 1f);
        ui.equipButtonText = eqbTxt;
        RectTransform eqbtrt = eqbTxtObj.GetComponent<RectTransform>();
        eqbtrt.anchorMin = Vector2.zero;
        eqbtrt.anchorMax = Vector2.one;
        eqbtrt.offsetMin = Vector2.zero;
        eqbtrt.offsetMax = Vector2.zero;

        // Unequip Button (Using ui_button.png)
        GameObject ueqbObj = new GameObject("UnequipBtn", typeof(RectTransform));
        ueqbObj.transform.SetParent(detailCard.transform, false);
        Image ueqbBg = ueqbObj.AddComponent<Image>();
        if (ui.woodButtonSprite != null)
        {
            ueqbBg.sprite = ui.woodButtonSprite;
            ueqbBg.type = Image.Type.Sliced;
            ueqbBg.color = new Color(1.15f, 0.80f, 0.80f, 1f);
        }
        else
        {
            ueqbBg.color = new Color(0.75f, 0.30f, 0.30f, 0.95f);
        }

        Button ueqB = ueqbObj.AddComponent<Button>();
        ueqB.targetGraphic = ueqbBg;
        ui.unequipButton = ueqB;
        ui.unequipButtonBg = ueqbBg;
        RectTransform ueqbrt = ueqbObj.GetComponent<RectTransform>();
        ueqbrt.anchorMin = new Vector2(0.52f, 0.08f);
        ueqbrt.anchorMax = new Vector2(0.94f, 0.23f);
        ueqbrt.offsetMin = Vector2.zero;
        ueqbrt.offsetMax = Vector2.zero;

        GameObject ueqbTxtObj = new GameObject("Text", typeof(RectTransform));
        ueqbTxtObj.transform.SetParent(ueqbObj.transform, false);
        TextMeshProUGUI ueqbTxt = ueqbTxtObj.AddComponent<TextMeshProUGUI>();
        ueqbTxt.fontSize = 18;
        ueqbTxt.fontStyle = FontStyles.Bold;
        ueqbTxt.alignment = TextAlignmentOptions.Center;
        ueqbTxt.text = "UNEQUIP";
        ueqbTxt.color = new Color(0.45f, 0.15f, 0.15f, 1f);
        ui.unequipButtonText = ueqbTxt;
        RectTransform ueqbtrt = ueqbTxtObj.GetComponent<RectTransform>();
        ueqbtrt.anchorMin = Vector2.zero;
        ueqbtrt.anchorMax = Vector2.one;
        ueqbtrt.offsetMin = Vector2.zero;
        ueqbtrt.offsetMax = Vector2.zero;

        // Body Stabilization Bar (Bottom of Tab 2)
        GameObject bodyBar = new GameObject("BodyStabilizationBar", typeof(RectTransform));
        bodyBar.transform.SetParent(t2Container.transform, false);
        Image bbBg = bodyBar.AddComponent<Image>();
        if (ui.woodSlotSprite != null)
        {
            bbBg.sprite = ui.woodSlotSprite;
            bbBg.type = Image.Type.Sliced;
            bbBg.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        }
        else
        {
            bbBg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        }

        RectTransform bbrt = bodyBar.GetComponent<RectTransform>();
        bbrt.anchorMin = new Vector2(0f, 0f);
        bbrt.anchorMax = new Vector2(1f, 0.11f);
        bbrt.offsetMin = Vector2.zero;
        bbrt.offsetMax = Vector2.zero;

        GameObject bbTxtObj = new GameObject("Label", typeof(RectTransform));
        bbTxtObj.transform.SetParent(bodyBar.transform, false);
        TextMeshProUGUI bbTxt = bbTxtObj.AddComponent<TextMeshProUGUI>();
        bbTxt.fontSize = 16;
        bbTxt.fontStyle = FontStyles.Bold;
        bbTxt.text = "<color=#88CCFF>Flesh Morphology (Body Size):</color>";
        bbTxt.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform bbtrt = bbTxtObj.GetComponent<RectTransform>();
        bbtrt.anchorMin = new Vector2(0.03f, 0f);
        bbtrt.anchorMax = new Vector2(0.38f, 1f);
        bbtrt.offsetMin = new Vector2(15f, 0f);
        bbtrt.offsetMax = Vector2.zero;

        // 4 Size Buttons (Compact size: 95 x 34 px, neatly centered vertically)
        ui.bodySizeButtons = new Button[4];
        ui.bodySizeButtonBgs = new Image[4];
        string[] sizeLabels = { "Small", "Medium", "Large", "X-Large" };
        for (int i = 0; i < 4; i++)
        {
            GameObject szObj = new GameObject($"SizeBtn_{i}", typeof(RectTransform));
            szObj.transform.SetParent(bodyBar.transform, false);
            Image szBg = szObj.AddComponent<Image>();
            if (ui.woodButtonSprite != null)
            {
                szBg.sprite = ui.woodButtonSprite;
                szBg.type = Image.Type.Sliced;
                szBg.color = (i == 0) ? new Color(1.25f, 1.15f, 0.95f, 1f) : new Color(0.65f, 0.65f, 0.65f, 1f);
            }
            else
            {
                szBg.color = new Color(0.18f, 0.22f, 0.30f, 0.9f);
            }

            Button szB = szObj.AddComponent<Button>();
            szB.targetGraphic = szBg;
            ui.bodySizeButtons[i] = szB;
            ui.bodySizeButtonBgs[i] = szBg;

            RectTransform szrt = szObj.GetComponent<RectTransform>();
            szrt.anchorMin = new Vector2(0.40f, 0.5f);
            szrt.anchorMax = new Vector2(0.40f, 0.5f);
            szrt.pivot = new Vector2(0f, 0.5f);
            szrt.anchoredPosition = new Vector2(i * 125f, 0f); // Fixed compact button spacing
            szrt.sizeDelta = new Vector2(110f, 36f);          // Compact height!

            GameObject szTxtObj = new GameObject("Text", typeof(RectTransform));
            szTxtObj.transform.SetParent(szObj.transform, false);
            TextMeshProUGUI szt = szTxtObj.AddComponent<TextMeshProUGUI>();
            szt.fontSize = 14;
            szt.fontStyle = FontStyles.Bold;
            szt.alignment = TextAlignmentOptions.Center;
            szt.text = sizeLabels[i];
            szt.color = new Color(0.95f, 0.90f, 0.80f, 1f);
            RectTransform sztrt = szTxtObj.GetComponent<RectTransform>();
            sztrt.anchorMin = Vector2.zero;
            sztrt.anchorMax = Vector2.one;
            sztrt.offsetMin = Vector2.zero;
            sztrt.offsetMax = Vector2.zero;
        }

        ui.SwitchTab(0);
        return ui;
    }

    private static TextMeshProUGUI CreateDetailStatRow(Transform parent, string label, string initialValue, out GameObject rowObj)
    {
        GameObject row = new GameObject($"StatRow_{label}", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        rowObj = row;

        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 32f;
        le.preferredHeight = 32f;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 32f);

        GameObject lObj = new GameObject("Label", typeof(RectTransform));
        lObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI lTxt = lObj.AddComponent<TextMeshProUGUI>();
        lTxt.fontSize = 15;
        lTxt.text = label;
        lTxt.color = new Color(0.78f, 0.82f, 0.88f, 0.95f);
        lTxt.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform lrt = lObj.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(0.50f, 1f);
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        GameObject vObj = new GameObject("Val", typeof(RectTransform));
        vObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI vTxt = vObj.AddComponent<TextMeshProUGUI>();
        vTxt.fontSize = 15;
        vTxt.fontStyle = FontStyles.Bold;
        vTxt.text = initialValue;
        vTxt.alignment = TextAlignmentOptions.MidlineRight;
        RectTransform vrt = vObj.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0.50f, 0f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;

        return vTxt;
    }

    private static void CreateReceptacleSquare(
        Transform parent,
        string title,
        string subtitle,
        Sprite slotSprite,
        Sprite progressFrame,
        out Image fillBar,
        out TextMeshProUGUI valText,
        out TextMeshProUGUI statusText)
    {
        // Container Square using ui_slot_wood.png
        GameObject sqObj = new GameObject($"Square_{title}", typeof(RectTransform));
        sqObj.transform.SetParent(parent, false);
        Image sqBg = sqObj.AddComponent<Image>();
        if (slotSprite != null)
        {
            sqBg.sprite = slotSprite;
            sqBg.type = Image.Type.Sliced;
            sqBg.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        }
        else
        {
            sqBg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);
        }

        // Title Top with Generous Padding from Top Edge!
        GameObject tObj = new GameObject("Title", typeof(RectTransform));
        tObj.transform.SetParent(sqObj.transform, false);
        TextMeshProUGUI tTxt = tObj.AddComponent<TextMeshProUGUI>();
        tTxt.fontSize = 16;
        tTxt.fontStyle = FontStyles.Bold;
        tTxt.alignment = TextAlignmentOptions.Center;
        tTxt.text = $"<color=#FFE4A0>{title}</color>";
        RectTransform trt = tObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.05f, 0.82f);
        trt.anchorMax = new Vector2(0.95f, 0.94f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle", typeof(RectTransform));
        subObj.transform.SetParent(sqObj.transform, false);
        TextMeshProUGUI subTxt = subObj.AddComponent<TextMeshProUGUI>();
        subTxt.fontSize = 12;
        subTxt.alignment = TextAlignmentOptions.Center;
        subTxt.text = $"<color=#8899AA>{subtitle}</color>";
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.05f, 0.72f);
        subRt.anchorMax = new Vector2(0.95f, 0.82f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;

        // Inner Progress Well / Frame in Center (Deep dark wooden well)
        GameObject frameObj = new GameObject("FillFrame", typeof(RectTransform));
        frameObj.transform.SetParent(sqObj.transform, false);
        Image fImg = frameObj.AddComponent<Image>();
        if (slotSprite != null)
        {
            fImg.sprite = slotSprite;
            fImg.type = Image.Type.Sliced;
            fImg.color = new Color(0.35f, 0.32f, 0.28f, 1f); // Dark recessed wood border
        }
        else
        {
            fImg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);
        }

        RectTransform frt = frameObj.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0.08f, 0.20f);
        frt.anchorMax = new Vector2(0.92f, 0.70f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        // Inner Well Background (Deep dark void inside the well)
        GameObject wellBg = new GameObject("WellVoid", typeof(RectTransform));
        wellBg.transform.SetParent(frameObj.transform, false);
        Image wImg = wellBg.AddComponent<Image>();
        wImg.color = new Color(0.04f, 0.05f, 0.07f, 0.98f);
        RectTransform wbrt = wellBg.GetComponent<RectTransform>();
        wbrt.anchorMin = Vector2.zero;
        wbrt.anchorMax = Vector2.one;
        wbrt.offsetMin = new Vector2(4f, 4f);
        wbrt.offsetMax = new Vector2(-4f, -4f);

        // Fluid Fill inside Well (Vertical fill from bottom, Pearly White Lust Fluid)
        GameObject fillObj = new GameObject("FluidFill", typeof(RectTransform));
        fillObj.transform.SetParent(wellBg.transform, false);
        Image fill = fillObj.AddComponent<Image>();
        fill.sprite = WhiteSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Bottom;
        fill.color = new Color(0.96f, 0.94f, 0.98f, 0.88f); // Pearly white cum tint
        fill.fillAmount = 0f;
        fill.gameObject.SetActive(false); // Hidden when 0% filled
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillBar = fill;

        // Value Center (Crisp & Readable on top of well)
        GameObject valObj = new GameObject("ValueText", typeof(RectTransform));
        valObj.transform.SetParent(frameObj.transform, false);
        TextMeshProUGUI vTxt = valObj.AddComponent<TextMeshProUGUI>();
        vTxt.fontSize = 17;
        vTxt.fontStyle = FontStyles.Bold;
        vTxt.alignment = TextAlignmentOptions.Center;
        vTxt.text = "0 / 100 ml";
        vTxt.color = Color.white;
        RectTransform vrt = valObj.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        valText = vTxt;

        // Status Bottom with Generous Padding from Bottom Edge!
        GameObject stObj = new GameObject("StatusText", typeof(RectTransform));
        stObj.transform.SetParent(sqObj.transform, false);
        TextMeshProUGUI sTxt = stObj.AddComponent<TextMeshProUGUI>();
        sTxt.fontSize = 13;
        sTxt.fontStyle = FontStyles.Bold;
        sTxt.alignment = TextAlignmentOptions.Center;
        sTxt.text = "<color=#88DDAA>[Pure & Clear]</color>";
        RectTransform srt = stObj.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.05f, 0.04f);
        srt.anchorMax = new Vector2(0.95f, 0.17f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;
        statusText = sTxt;
    }
}
