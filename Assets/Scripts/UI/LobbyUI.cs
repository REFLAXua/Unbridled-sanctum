using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnbridledSanctum.Combat;
using UnbridledSanctum.UI;
using UnbridledSanctum.Data;

/// <summary>
/// Manages the Sanctuary Cave (Lobby) window with 3 tabs:
///   1. [ EXPEDITION ]: Narrative overview, depth record, and Start Expedition button.
///   2. [ WARDROBE ]: Equipped clothing, Armor buff status, Strip button, Repair button, and item list.
///   3. [ DECK ]: Deckbuilder to manage up to 9 active combat cards from the collection.
/// Fully in English.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject lobbyPanel;

    [Header("Meta Currency Display")]
    public TextMeshProUGUI lustidBadgeText;

    [Header("Tab Headers")]
    public Button expeditionTabBtn;
    public Button wardrobeTabBtn;
    public Button deckTabBtn;
    public Button pathsTabBtn;
    public Image expeditionTabBg;
    public Image wardrobeTabBg;
    public Image deckTabBg;
    public Image pathsTabBg;

    [Header("Pages")]
    public GameObject expeditionPage;
    public GameObject wardrobePage;
    public GameObject deckPage;
    public GameObject pathsPage;

    [Header("Expedition Page Elements")]
    public TextMeshProUGUI statsText;
    public Button startExpeditionButton;

    [Header("Wardrobe Page Elements")]
    public Transform wardrobeItemsContainer;
    public TextMeshProUGUI equippedClothesText;
    public Button unequipClothesBtn;
    public Button repairClothesBtn;

    [Header("Flesh Stabilization & Archive Elements")]
    public GameObject fleshStabilizationContainer;
    public Button[] bodySizeButtons;
    public Image[] bodySizeButtonBgs;
    public TextMeshProUGUI[] bodySizeButtonTexts;
    public TextMeshProUGUI fleshArchiveText;
    public TextMeshProUGUI fleshPranaGaugeText;
    public Button fleshDebugFillBtn;

    [Header("Deck Page Elements")]
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

    [Header("Desire Tree Page Elements")]
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

    [Header("Tree Detail / Purchase Inspector")]
    public TextMeshProUGUI treeDetailTitleText;
    public TextMeshProUGUI treeDetailCategoryText;
    public TextMeshProUGUI treeDetailDescText;
    public TextMeshProUGUI treeDetailBonusText;
    public Button treePurchaseBtn;
    public Image treePurchaseBtnBg;
    public TextMeshProUGUI treePurchaseBtnText;

    [Header("Tree Overlay / Path Selection")]
    public GameObject treeOverlayPanel;
    public TextMeshProUGUI treeOverlayTitleText;
    public TextMeshProUGUI treeOverlayDescText;
    public Button treeOverlaySelectBtn;
    public Image treeOverlaySelectBtnBg;
    public TextMeshProUGUI treeOverlaySelectBtnText;

    [Header("Dependencies")]
    public CharacterState characterState;
    public InventoryManager inventoryManager;

    public event Action OnStartExpedition;

    private int currentMaxFloor = 1;

    private void Awake()
    {
        if (lobbyPanel == null) lobbyPanel = gameObject;
        DesireTreeManager.EnsureExists();
        int savedPath = PlayerPrefs.GetInt("Sanctum_ActiveLustPath", (int)LustPathType.PhallusDevotee);
        selectedTreePath = (LustPathType)savedPath;
        SetupListeners();
    }

    private void Start()
    {
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
        if (statsText != null)
        {
            statsText.text = $"Highest Depth Explored: Floor {currentMaxFloor}\nStatus: Rested and ready for battle\nAltar Essence: <color=#FF77DD>{lustid} Lustid</color>";
        }
    }

    public void SetupListeners()
    {
        if (startExpeditionButton != null)
        {
            startExpeditionButton.onClick.RemoveAllListeners();
            startExpeditionButton.onClick.AddListener(() =>
            {
                Debug.Log("[LobbyUI] Start Expedition button clicked!");
                OnStartExpedition?.Invoke();
            });
        }

        if (expeditionTabBtn != null)
        {
            expeditionTabBtn.onClick.RemoveAllListeners();
            expeditionTabBtn.onClick.AddListener(() => ShowTab(0));
        }

        if (wardrobeTabBtn != null)
        {
            wardrobeTabBtn.onClick.RemoveAllListeners();
            wardrobeTabBtn.onClick.AddListener(() => ShowTab(1));
        }

        if (deckTabBtn != null)
        {
            deckTabBtn.onClick.RemoveAllListeners();
            deckTabBtn.onClick.AddListener(() => ShowTab(2));
        }

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
                bodySizeButtons[i].onClick.RemoveAllListeners();
                bodySizeButtons[i].onClick.AddListener(() =>
                {
                    if (FleshTransformationManager.Instance != null)
                    {
                        FleshTransformationManager.Instance.StabilizeBodySize((BodySizeType)sizeIdx);
                        RefreshWardrobe();
                    }
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

        if (pathsTabBtn != null)
        {
            pathsTabBtn.onClick.RemoveAllListeners();
            pathsTabBtn.onClick.AddListener(() => ShowTab(3));
        }

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
    }

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

        // Highlight selected path header button
        Color activeGold = new Color(0.95f, 0.85f, 0.35f, 1f);
        Color selectedTabBg = new Color(0.35f, 0.18f, 0.42f, 0.95f);
        Color normalBg = new Color(0.12f, 0.14f, 0.18f, 0.85f);
        Color normalText = new Color(0.75f, 0.78f, 0.85f, 1f);
        Color lockedText = new Color(0.50f, 0.52f, 0.58f, 1f);

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

        // Handle Tree Overlay (covers nodes if viewed path is not currently active)
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

            var nodes = dtm.GetNodesForPath(selectedTreePath);
            if (nodes.Count > 0 && selectedTreeNode == null)
            {
                selectedTreeNode = nodes[0];
            }

            // 1. Background Tier Banners (ambient lines dividing tiers from bottom to top)
            CreateTierDivider(treeNodesScrollContent, 68f, "TIER I: AWAKENING INSTINCTS", new Color(0.95f, 0.80f, 0.40f, 0.25f));
            CreateTierDivider(treeNodesScrollContent, 360f, "TIER II: DEEP PLEASURE & UNION", new Color(0.85f, 0.45f, 0.90f, 0.25f));
            CreateTierDivider(treeNodesScrollContent, 660f, "TIER III: APEX CORRUPTION & DEVOTION", new Color(1f, 0.85f, 0.40f, 0.35f));

            // Sub-Branch Column Headers
            CreateSubBranchHeaders(treeNodesScrollContent, selectedTreePath);

            // 2. Connecting Lines between prerequisite nodes and their children
            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.prerequisiteNodeId)) continue;
                var parent = nodes.Find(n => n.id == node.prerequisiteNodeId);
                if (parent == null) continue;

                CreateTreeConnectingLine(treeNodesScrollContent, parent, node, dtm);
            }

            // 3. Circular Nodes
            foreach (var node in nodes)
            {
                CreateTreeNodeCircle(treeNodesScrollContent, node, node == selectedTreeNode);
            }
        }

        if (selectedTreeNode != null)
        {
            UpdateTreeDetailPanel(selectedTreeNode);
        }
    }

    private void CreateTierDivider(Transform parent, float yPos, string label, Color color)
    {
        GameObject divObj = new GameObject($"TierDivider_{yPos}", typeof(RectTransform));
        divObj.transform.SetParent(parent, false);
        RectTransform rt = divObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(500f, 22f);

        // Divider Line
        GameObject lineObj = new GameObject("Line", typeof(RectTransform));
        lineObj.transform.SetParent(divObj.transform, false);
        RectTransform lrt = lineObj.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0.5f);
        lrt.anchorMax = new Vector2(1f, 0.5f);
        lrt.sizeDelta = new Vector2(0f, 1.5f);
        Image lImg = lineObj.AddComponent<Image>();
        lImg.color = color;
        lImg.raycastTarget = false;

        // Label Badge with subtle dark capsule backing
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
        tt.text = $"— {label} —";
        tt.fontSize = 10f;
        tt.fontStyle = FontStyles.Bold;
        tt.alignment = TextAlignmentOptions.Center;
        tt.color = new Color(color.r, color.g, color.b, 0.90f);
        tt.raycastTarget = false;
    }

    private void CreateSubBranchHeaders(Transform parent, LustPathType path)
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

        CreateSubHeaderBadge(parent, -130f, 30f, subA);
        CreateSubHeaderBadge(parent, 0f, 30f, subB);
        CreateSubHeaderBadge(parent, 130f, 30f, subC);
    }

    private void CreateSubHeaderBadge(Transform parent, float xPos, float yPos, string text)
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
            lineImg.color = new Color(0.28f, 0.92f, 0.48f, 0.85f); // glowing emerald
        }
        else if (fromUnlocked)
        {
            lineImg.color = new Color(0.85f, 0.35f, 0.90f, 0.70f); // active purple
        }
        else
        {
            lineImg.color = new Color(0.20f, 0.22f, 0.28f, 0.50f); // dark locked
        }
    }

    private void CreateTreeNodeCircle(Transform parent, DesireTreeNode node, bool isSelected)
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

        // 1. Selection Aura (Golden pulse if selected)
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

        // 2. Outer Ring / Border
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
            ringColor = new Color(1f, 0.90f, 0.35f, 1f); // bright gold
        else if (isUnlocked)
            ringColor = new Color(0.28f, 0.92f, 0.48f, 1f); // emerald green
        else if (canUnlock)
            ringColor = new Color(0.90f, 0.35f, 0.92f, 1f); // magenta violet
        else
            ringColor = new Color(0.25f, 0.28f, 0.36f, 0.85f); // muted steel slate
        ringImg.color = ringColor;

        // 3. Inner Core
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

        // 4. Center Glyphs / Icons
        GameObject iconObj = new GameObject("CenterIcon", typeof(RectTransform));
        iconObj.transform.SetParent(nodeObj.transform, false);
        RectTransform irt = iconObj.GetComponent<RectTransform>();
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.sizeDelta = Vector2.zero;
        TextMeshProUGUI itxt = iconObj.AddComponent<TextMeshProUGUI>();
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

        // 5. Floating Title & Price Label below circular node
        GameObject labelObj = new GameObject("NodeLabel", typeof(RectTransform));
        labelObj.transform.SetParent(nodeObj.transform, false);
        RectTransform lbrt = labelObj.GetComponent<RectTransform>();
        lbrt.anchorMin = new Vector2(0.5f, 0f);
        lbrt.anchorMax = new Vector2(0.5f, 0f);
        lbrt.pivot = new Vector2(0.5f, 1f);
        lbrt.anchoredPosition = new Vector2(0f, -3f);
        lbrt.sizeDelta = new Vector2(140f, 32f);

        TextMeshProUGUI lbtxt = labelObj.AddComponent<TextMeshProUGUI>();
        lbtxt.alignment = TextAlignmentOptions.Top;
        lbtxt.fontSize = 10f;
        lbtxt.lineSpacing = -15f;
        lbtxt.fontStyle = FontStyles.Bold;
        lbtxt.enableWordWrapping = true;
        lbtxt.raycastTarget = false;

        string titleColor = isSelected ? "#FFE455" : (isUnlocked ? "#D8F8E0" : (canUnlock ? "#FFFFFF" : "#7A8094"));
        string costBadge;
        if (isUnlocked)
            costBadge = "<color=#40EE80>[MASTERED]</color>";
        else if (node.lustidCost == 0)
            costBadge = "<color=#40D0FF>FREE</color>";
        else
            costBadge = canUnlock ? $"<color=#FF77DD>✦ {node.lustidCost}</color>" : $"<color=#777D90>[LOCKED] {node.lustidCost}</color>";

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

            if (selectedTreePath != activePath)
            {
                treePurchaseBtn.interactable = false;
                if (treePurchaseBtnText != null) treePurchaseBtnText.text = "[SELECT PATH FIRST]";
                if (treePurchaseBtnBg != null) treePurchaseBtnBg.color = new Color(0.25f, 0.25f, 0.30f, 0.70f);
            }
            else if (isUnlocked)
            {
                treePurchaseBtn.interactable = false;
                if (treePurchaseBtnText != null) treePurchaseBtnText.text = "[MASTERED]";
                if (treePurchaseBtnBg != null) treePurchaseBtnBg.color = new Color(0.20f, 0.45f, 0.25f, 0.85f);
            }
            else if (canUnlock)
            {
                treePurchaseBtn.interactable = true;
                string costLabel = node.lustidCost > 0 ? $"UNLOCK ({node.lustidCost} LUSTID)" : "AWAKEN (FREE)";
                if (treePurchaseBtnText != null) treePurchaseBtnText.text = costLabel;
                if (treePurchaseBtnBg != null) treePurchaseBtnBg.color = new Color(0.65f, 0.22f, 0.60f, 1f);

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
                if (treePurchaseBtnBg != null) treePurchaseBtnBg.color = new Color(0.25f, 0.25f, 0.30f, 0.7f);
            }
        }
    }

    public void Initialize(CharacterState state, InventoryManager inv)
    {
        characterState = state;
        inventoryManager = inv;

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

        RefreshLustidDisplay();
        SetupListeners();
        ShowTab(0);
        RefreshWardrobe();
    }

    public void Hide()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
    }

    public void ShowTab(int tabIndex)
    {
        if (expeditionPage != null) expeditionPage.SetActive(tabIndex == 0);
        if (wardrobePage != null) wardrobePage.SetActive(tabIndex == 1);
        if (deckPage != null) deckPage.SetActive(tabIndex == 2);
        if (pathsPage != null) pathsPage.SetActive(tabIndex == 3);

        Color activeColor = new Color(0.20f, 0.24f, 0.32f, 1f);
        Color inactiveColor = new Color(0.12f, 0.14f, 0.18f, 0.85f);

        if (expeditionTabBg != null) expeditionTabBg.color = (tabIndex == 0) ? activeColor : inactiveColor;
        if (wardrobeTabBg != null) wardrobeTabBg.color = (tabIndex == 1) ? activeColor : inactiveColor;
        if (deckTabBg != null) deckTabBg.color = (tabIndex == 2) ? activeColor : inactiveColor;
        if (pathsTabBg != null) pathsTabBg.color = (tabIndex == 3) ? activeColor : inactiveColor;

        if (tabIndex == 1) RefreshWardrobe();
        if (tabIndex == 2) RefreshDeck();
        if (tabIndex == 3)
        {
            RefreshDesireTreeUI();
            if (treeScrollRect != null) treeScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void RefreshWardrobe()
    {
        if (characterState == null) return;

        // Equipped Clothes (Combined, Upper, or Lower)
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

        // Flesh Stabilization & Archive
        if (FleshTransformationManager.Instance != null)
        {
            FleshTransformationManager ftm = FleshTransformationManager.Instance;
            if (bodySizeButtons != null)
            {
                string[] sizeLabels = { "Small (A-B)", "Medium (C-D)", "Large (D-E)", "XLarge (E-F)" };
                for (int i = 0; i < bodySizeButtons.Length; i++)
                {
                    BodySizeType size = (BodySizeType)i;
                    bool unlocked = ftm.IsSizeUnlocked(size);
                    bool active = characterState.CurrentBodySize == size;

                    if (bodySizeButtons[i] != null)
                    {
                        bodySizeButtons[i].interactable = unlocked;
                    }

                    if (bodySizeButtonBgs != null && i < bodySizeButtonBgs.Length && bodySizeButtonBgs[i] != null)
                    {
                        if (active)
                            bodySizeButtonBgs[i].color = new Color(0.75f, 0.25f, 0.65f, 1f);
                        else if (unlocked)
                            bodySizeButtonBgs[i].color = new Color(0.18f, 0.22f, 0.30f, 0.90f);
                        else
                            bodySizeButtonBgs[i].color = new Color(0.10f, 0.12f, 0.16f, 0.50f);
                    }

                    if (bodySizeButtonTexts != null && i < bodySizeButtonTexts.Length && bodySizeButtonTexts[i] != null)
                    {
                        if (active)
                            bodySizeButtonTexts[i].text = $"[ACTIVE] {sizeLabels[i]}";
                        else if (unlocked)
                            bodySizeButtonTexts[i].text = sizeLabels[i];
                        else
                            bodySizeButtonTexts[i].text = $"[LOCKED] {sizeLabels[i]}";
                    }
                }
            }

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

        // Inventory Items List
        if (wardrobeItemsContainer != null)
        {
            foreach (Transform child in wardrobeItemsContainer)
                Destroy(child.gameObject);

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
        rr.sizeDelta = new Vector2(0f, 48f);

        Image bg = row.AddComponent<Image>();
        bg.color = isEquipped ? new Color(0.18f, 0.22f, 0.30f, 0.90f) : new Color(0.12f, 0.14f, 0.18f, 0.85f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 4, 4);
        hlg.spacing = 12;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Name & Slot
        string slotLabel = item.IsCombinedArmor ? "Combined Armor" : (item.IsUpperArmor ? "Upper Armor" : (item.IsLowerArmor ? "Lower Armor" : item.equipSlot.ToString()));
        var statsList = new System.Collections.Generic.List<string>();
        statsList.Add($"Dur: {item.durability}");
        if (item.damageReduction != 0) statsList.Add($"Armor {(item.damageReduction > 0 ? "+" : "")}{item.damageReduction}");
        if (item.bonusMaxHp != 0) statsList.Add($"HP {(item.bonusMaxHp > 0 ? "+" : "")}{item.bonusMaxHp}");
        if (item.bonusMaxMana != 0) statsList.Add($"Mana {(item.bonusMaxMana > 0 ? "+" : "")}{item.bonusMaxMana}");
        if (item.bonusSeductionPower != 0) statsList.Add($"Sed {(item.bonusSeductionPower > 0 ? "+" : "")}{item.bonusSeductionPower}%");
        if (item.seductionResistance != 0) statsList.Add($"Res {(item.seductionResistance > 0 ? "+" : "")}{item.seductionResistance}%");
        string statsSummary = string.Join(" | ", statsList);

        GameObject nameObj = new GameObject("Name", typeof(RectTransform));
        nameObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
        nameTxt.text = $"{item.itemName} ({slotLabel}) [{statsSummary}]";
        nameTxt.fontSize = 18;
        nameTxt.alignment = TextAlignmentOptions.MidlineLeft;
        nameTxt.color = Color.white;
        nameTxt.raycastTarget = false;
        nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 0f);

        // Equip / Unequip Button
        GameObject btnObj = new GameObject("EquipBtn", typeof(RectTransform));
        btnObj.transform.SetParent(row.transform, false);
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = isEquipped ? new Color(0.55f, 0.25f, 0.25f, 1f) : new Color(0.25f, 0.55f, 0.35f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 40f);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = isEquipped ? "Unequip" : "Equip";
        btnTxt.fontSize = 17;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color = Color.white;
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

    public void SetDeckCategoryFilter(DeckCategoryFilter filter)
    {
        currentDeckCategoryFilter = filter;
        UpdateFilterButtonsUI();
        RefreshDeck();
    }

    public void UpdateFilterButtonsUI()
    {
        Color activeBg = new Color(0.68f, 0.54f, 0.28f, 0.95f);
        Color inactiveBg = new Color(0.12f, 0.14f, 0.18f, 0.85f);
        Color activeTxt = Color.white;
        Color inactiveTxt = new Color(0.70f, 0.72f, 0.78f, 1f);

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

        // Check which cards are already equipped in the player's active deck (and remove any duplicates in deck)
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

        // Available cards = all unlocked cards EXCEPT those currently equipped in deck (with duplicates removed)
        List<CardData> availableCards = new List<CardData>();
        HashSet<string> seenAvailableIds = new HashSet<string>();
        if (cm.unlockedCards != null)
        {
            for (int i = 0; i < cm.unlockedCards.Count; i++)
            {
                CardData cd = cm.unlockedCards[i];
                if (cd != null && !equippedCardIds.Contains(cd.id) && seenAvailableIds.Add(cd.id))
                {
                    // Category filter
                    if (currentDeckCategoryFilter == DeckCategoryFilter.WarriorTactics && cd.category != CardCategory.WarriorTactics)
                        continue;
                    if (currentDeckCategoryFilter == DeckCategoryFilter.LustInstincts && cd.category != CardCategory.LustInstinct)
                        continue;
                    if (currentDeckCategoryFilter == DeckCategoryFilter.StruggleActions && cd.category != CardCategory.StruggleAction)
                        continue;

                    // Show Warrior tactics, Struggle cards, External Finishers, and cards belonging to the active Lust Path (or universal)
                    if (cd.category == CardCategory.LustInstinct)
                    {
                        if (cd.requiredPath != LustPathType.None && cd.requiredPath != cm.activeLustPath)
                            continue;
                    }
                    availableCards.Add(cd);
                }
            }
        }

        // Ensure we have a valid selected card for the detail panel
        if (selectedCollectionCard == null || (!availableCards.Contains(selectedCollectionCard) && !equippedCardIds.Contains(selectedCollectionCard.id)))
        {
            if (availableCards.Count > 0)
                selectedCollectionCard = availableCards[0];
            else if (cm.baseWarriorDeck.Count > 0 && cm.baseWarriorDeck[0] != null)
                selectedCollectionCard = cm.baseWarriorDeck[0];
            else
                selectedCollectionCard = null;
        }

        // Update detail panel
        if (selectedCollectionCard != null)
        {
            UpdateCardDetailPanel(selectedCollectionCard);
        }

        // 1. Refresh Available Cards Collection (Top Left Grid) - only non-equipped cards
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

        // 2. Refresh Active Deck (Bottom Row)
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

            // Fill empty slots up to MAX_DECK_SIZE
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

        // Clean previous stat badges
        if (detailStatsRow1 != null)
        {
            foreach (Transform child in detailStatsRow1) Destroy(child.gameObject);
            // AP Cost Badge
            CreateStatBadge(detailStatsRow1, StatIconDatabase.ApIcon, $"{card.apCost} AP", new Color(0.96f, 0.82f, 0.27f, 1f));
            // Cooldown Badge
            CreateStatBadge(detailStatsRow1, StatIconDatabase.ManaIcon, $"{card.cooldownTurns}T CD", new Color(0.44f, 0.78f, 0.96f, 1f));
            // Target Badge
            string targetStr = card.target == CardTarget.SingleEnemy ? "Single" : (card.target == CardTarget.Self ? "Self" : "All");
            CreateStatBadge(detailStatsRow1, null, targetStr, new Color(0.77f, 0.63f, 0.96f, 1f));
        }

        if (detailStatsRow2 != null)
        {
            foreach (Transform child in detailStatsRow2) Destroy(child.gameObject);

            if (card.hpDamage > 0)
            {
                CreateStatBadge(detailStatsRow2, StatIconDatabase.HpIcon, $"{card.hpDamage} DMG", new Color(1f, 0.37f, 0.37f, 1f));
            }
            if (card.staminaDamage > 0)
            {
                CreateStatBadge(detailStatsRow2, null, $"{card.staminaDamage} STM", new Color(1f, 0.65f, 0.25f, 1f));
            }
            if (card.shieldGain > 0)
            {
                CreateStatBadge(detailStatsRow2, StatIconDatabase.BlockIcon, $"+{card.shieldGain} SHD", new Color(0.29f, 0.70f, 1f, 1f));
            }
            if (card.lustShieldGain > 0)
            {
                CreateStatBadge(detailStatsRow2, StatIconDatabase.LustIcon, $"+{card.lustShieldGain} CUM SHD", new Color(0.95f, 0.45f, 0.90f, 1f));
            }
            if (card.semenAbsorbedMl > 0)
            {
                CreateStatBadge(detailStatsRow2, null, $"+{card.semenAbsorbedMl}ml CUM", new Color(0.98f, 0.95f, 0.85f, 1f));
            }
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
            {
                CreateStatBadge(detailStatsRow2, StatIconDatabase.ArmorIcon, $"+{card.clothesRepair} CLO", new Color(0.36f, 0.88f, 0.52f, 1f));
            }
            if (card.requiredTargetSlot != PenetrationSlot.None)
            {
                CreateStatBadge(detailStatsRow2, null, $"[{card.requiredTargetSlot.ToString().ToUpper()}]", new Color(0.40f, 0.85f, 1f, 1f));
            }
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

                if (isAlreadyInDeck)
                {
                    detailEquipBtn.interactable = false;
                    if (detailEquipBtnText != null) detailEquipBtnText.text = "EQUIPPED IN DECK";
                    if (detailEquipBtnBg != null) detailEquipBtnBg.color = new Color(0.35f, 0.35f, 0.35f, 0.6f);
                }
                else if (isDeckFull)
                {
                    detailEquipBtn.interactable = false;
                    if (detailEquipBtnText != null) detailEquipBtnText.text = "DECK FULL (9/9)";
                    if (detailEquipBtnBg != null) detailEquipBtnBg.color = new Color(0.35f, 0.35f, 0.35f, 0.6f);
                }
                else
                {
                    detailEquipBtn.interactable = true;
                    if (detailEquipBtnText != null) detailEquipBtnText.text = "ADD TO DECK";
                    if (detailEquipBtnBg != null) detailEquipBtnBg.color = new Color(0.22f, 0.55f, 0.35f, 1f);
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
        GameObject cardObj = new GameObject($"Card_{card.id}", typeof(RectTransform));
        cardObj.transform.SetParent(parent, false);

        RectTransform crt = cardObj.GetComponent<RectTransform>();
        crt.sizeDelta = size;

        // Transparent hit box to capture pointer clicks on the card
        Image hitImg = cardObj.AddComponent<Image>();
        hitImg.color = Color.clear;
        hitImg.raycastTarget = true;

        // Button for selecting / inspecting
        Button btn = cardObj.AddComponent<Button>();
        btn.targetGraphic = hitImg;
        btn.transition = Selectable.Transition.None;
        if (onClick != null)
        {
            btn.onClick.AddListener(() => onClick.Invoke());
        }

        // 1. Permanent Metallic Resting Border (color-coded by CardCategory)
        GameObject restingBorderObj = new GameObject("RestingBorder", typeof(RectTransform));
        restingBorderObj.transform.SetParent(cardObj.transform, false);
        Image restingBorder = restingBorderObj.AddComponent<Image>();
        Color borderTint;
        if (card.category == CardCategory.LustInstinct)
            borderTint = new Color(0.88f, 0.28f, 0.88f, 0.95f); // Lust/Desires: Seductive Magenta
        else if (card.category == CardCategory.StruggleAction)
            borderTint = new Color(0.95f, 0.55f, 0.15f, 0.95f); // Struggle: Amber Orange
        else
            borderTint = new Color(0.68f, 0.54f, 0.28f, 0.95f); // Warrior Tactics: Antique Gold
        restingBorder.color = borderTint;
        restingBorder.raycastTarget = false;
        RectTransform rbrt = restingBorderObj.GetComponent<RectTransform>();
        rbrt.anchorMin = Vector2.zero;
        rbrt.anchorMax = Vector2.one;
        rbrt.offsetMin = new Vector2(-2f, -2f);
        rbrt.offsetMax = new Vector2(2f, 2f);

        // 2. Selection Glow Border
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

        // 3. Card Solid Black Background
        GameObject bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.transform.SetParent(cardObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = Color.black;
        bg.raycastTarget = false;
        RectTransform bgrt = bgObj.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;

        // 4. Masked Art Window (with RectMask2D, un-distorted proportional cover)
        GameObject artWindow = new GameObject("ArtWindow", typeof(RectTransform));
        artWindow.transform.SetParent(cardObj.transform, false);
        artWindow.AddComponent<RectMask2D>();
        RectTransform winRt = artWindow.GetComponent<RectTransform>();
        winRt.anchorMin = Vector2.zero;
        winRt.anchorMax = Vector2.one;
        winRt.offsetMin = new Vector2(3f, 27f);
        winRt.offsetMax = new Vector2(-3f, -3f);

        // 5. Card Art Image (preserveAspect = true, proportional zoom)
        GameObject artObj = new GameObject("Art", typeof(RectTransform));
        artObj.transform.SetParent(artWindow.transform, false);
        Image artImg = artObj.AddComponent<Image>();
        artImg.preserveAspect = true;
        artImg.raycastTarget = false;
        RectTransform artRt = artObj.GetComponent<RectTransform>();
        artRt.anchorMin = new Vector2(0.5f, 0.5f);
        artRt.anchorMax = new Vector2(0.5f, 0.5f);
        artRt.pivot = new Vector2(0.5f, 0.5f);
        artRt.anchoredPosition = Vector2.zero;
        artRt.sizeDelta = new Vector2(size.y * 1.83f, size.y);

        var dtm = DesireTreeManager.Instance ?? DesireTreeManager.EnsureExists();
        bool isTreeLocked = (card.category == CardCategory.LustInstinct && !dtm.IsCardUnlocked(card.id));

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

        // 6. Name Separator Line (gold line above name plate)
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

        // 7. Name Plate
        GameObject namePlate = new GameObject("NamePlate", typeof(RectTransform));
        namePlate.transform.SetParent(cardObj.transform, false);
        Image nameBg = namePlate.AddComponent<Image>();
        nameBg.color = new Color(0.04f, 0.04f, 0.06f, 1f);
        nameBg.raycastTarget = false;
        RectTransform nprt = namePlate.GetComponent<RectTransform>();
        nprt.anchorMin = new Vector2(0f, 0f);
        nprt.anchorMax = new Vector2(1f, 0f);
        nprt.pivot = new Vector2(0.5f, 0f);
        nprt.anchoredPosition = Vector2.zero;
        nprt.sizeDelta = new Vector2(0f, 26f);

        GameObject nameTxtObj = new GameObject("Text", typeof(RectTransform));
        nameTxtObj.transform.SetParent(namePlate.transform, false);
        TextMeshProUGUI nameTxt = nameTxtObj.AddComponent<TextMeshProUGUI>();
        nameTxt.text = isTreeLocked ? $"{card.cardName}\n<size=8><color=#8895B0>[LOCKED]</color></size>" : card.cardName;
        nameTxt.fontSize = isTreeLocked ? 10 : 12;
        nameTxt.lineSpacing = -10;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.color = isTreeLocked ? new Color(0.70f, 0.72f, 0.80f, 0.90f) : new Color(0.96f, 0.94f, 0.90f, 1f);
        nameTxt.raycastTarget = false;
        RectTransform ntr = nameTxtObj.GetComponent<RectTransform>();
        ntr.anchorMin = Vector2.zero;
        ntr.anchorMax = Vector2.one;
        ntr.sizeDelta = Vector2.zero;

        // 8. AP Cost Badge (Top-Left)
        GameObject apBadge = new GameObject("ApBadge", typeof(RectTransform));
        apBadge.transform.SetParent(cardObj.transform, false);
        Image apRim = apBadge.AddComponent<Image>();
        apRim.color = new Color(0.35f, 0.26f, 0.10f, 1f);
        apRim.raycastTarget = false;
        RectTransform aprt = apBadge.GetComponent<RectTransform>();
        aprt.anchorMin = new Vector2(0f, 1f);
        aprt.anchorMax = new Vector2(0f, 1f);
        aprt.pivot = new Vector2(0f, 1f);
        aprt.anchoredPosition = new Vector2(4f, -4f);
        aprt.sizeDelta = new Vector2(24f, 24f);

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

        // 9. Equipped Card: Hover Red [X] Unequip Button + Hover component
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
            crossTxt.text = "✕";
            crossTxt.fontSize = 15;
            crossTxt.fontStyle = FontStyles.Bold;
            crossTxt.alignment = TextAlignmentOptions.Center;
            crossTxt.color = Color.white;
            crossTxt.raycastTarget = false;
            RectTransform ctr = crossTxtObj.GetComponent<RectTransform>();
            ctr.anchorMin = Vector2.zero;
            ctr.anchorMax = Vector2.one;
            ctr.sizeDelta = Vector2.zero;

            unequipObj.SetActive(false); // Shown only on hover!

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

        // Dim border
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

        // Dark background
        GameObject bgObj = new GameObject("Bg", typeof(RectTransform));
        bgObj.transform.SetParent(slotObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.11f, 0.45f);
        bg.raycastTarget = false;
        RectTransform bgrt = bgObj.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;

        // Plus text
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

    /// <summary>
    /// Dynamically constructs the Lobby UI hierarchy in English with Expedition, Wardrobe, and Deck tabs.
    /// </summary>
    public static LobbyUI CreateDynamic(Transform parent, CharacterState state = null, InventoryManager inv = null)
    {
        GameObject panelObj = new GameObject("LobbyPanel", typeof(RectTransform));
        panelObj.transform.SetParent(parent, false);

        RectTransform pr = panelObj.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(1f, 0f);
        pr.anchorMax = new Vector2(1f, 1f);
        pr.pivot = new Vector2(1f, 0.5f);
        pr.anchoredPosition = new Vector2(-40f, 0f);
        pr.sizeDelta = new Vector2(1180f, -50f); // Widened dialog window stretched to the left

        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.10f, 0.14f, 0.96f);

        LobbyUI lobby = panelObj.AddComponent<LobbyUI>();
        lobby.lobbyPanel = panelObj;
        lobby.characterState = state;
        lobby.inventoryManager = inv;

        // Top Tabs Row
        GameObject tabsObj = new GameObject("TabsHeader", typeof(RectTransform));
        tabsObj.transform.SetParent(panelObj.transform, false);
        RectTransform tr = tabsObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -15f);
        tr.sizeDelta = new Vector2(-40f, 48f);

        HorizontalLayoutGroup thlg = tabsObj.AddComponent<HorizontalLayoutGroup>();
        thlg.spacing = 10;
        thlg.childAlignment = TextAnchor.MiddleLeft;
        thlg.childControlWidth = false;
        thlg.childControlHeight = true;
        thlg.childForceExpandWidth = false;
        thlg.childForceExpandHeight = true;

        // Tab 1: Expedition
        GameObject expTabObj = new GameObject("ExpeditionTab", typeof(RectTransform));
        expTabObj.transform.SetParent(tabsObj.transform, false);
        RectTransform expTr = expTabObj.GetComponent<RectTransform>();
        expTr.sizeDelta = new Vector2(200f, 48f);
        Image expBg = expTabObj.AddComponent<Image>();
        expBg.color = new Color(0.20f, 0.24f, 0.32f, 1f);
        Button expBtn = expTabObj.AddComponent<Button>();
        expBtn.targetGraphic = expBg;
        lobby.expeditionTabBtn = expBtn;
        lobby.expeditionTabBg = expBg;

        GameObject expTxtObj = new GameObject("Text", typeof(RectTransform));
        expTxtObj.transform.SetParent(expTabObj.transform, false);
        TextMeshProUGUI et = expTxtObj.AddComponent<TextMeshProUGUI>();
        et.text = "EXPEDITION";
        et.fontSize = 18;
        et.fontStyle = FontStyles.Bold;
        et.alignment = TextAlignmentOptions.Center;
        et.color = Color.white;
        et.raycastTarget = false;
        RectTransform etr = expTxtObj.GetComponent<RectTransform>();
        etr.anchorMin = Vector2.zero;
        etr.anchorMax = Vector2.one;
        etr.sizeDelta = Vector2.zero;

        // Tab 2: Wardrobe
        GameObject wTabObj = new GameObject("WardrobeTab", typeof(RectTransform));
        wTabObj.transform.SetParent(tabsObj.transform, false);
        RectTransform wTr = wTabObj.GetComponent<RectTransform>();
        wTr.sizeDelta = new Vector2(200f, 48f);
        Image wBg = wTabObj.AddComponent<Image>();
        wBg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
        Button wBtn = wTabObj.AddComponent<Button>();
        wBtn.targetGraphic = wBg;
        lobby.wardrobeTabBtn = wBtn;
        lobby.wardrobeTabBg = wBg;

        GameObject wTxtObj = new GameObject("Text", typeof(RectTransform));
        wTxtObj.transform.SetParent(wTabObj.transform, false);
        TextMeshProUGUI wt = wTxtObj.AddComponent<TextMeshProUGUI>();
        wt.text = "WARDROBE";
        wt.fontSize = 18;
        wt.fontStyle = FontStyles.Bold;
        wt.alignment = TextAlignmentOptions.Center;
        wt.color = Color.white;
        wt.raycastTarget = false;
        RectTransform wtr = wTxtObj.GetComponent<RectTransform>();
        wtr.anchorMin = Vector2.zero;
        wtr.anchorMax = Vector2.one;
        wtr.sizeDelta = Vector2.zero;

        // Tab 3: Deck
        GameObject dTabObj = new GameObject("DeckTab", typeof(RectTransform));
        dTabObj.transform.SetParent(tabsObj.transform, false);
        RectTransform dTr = dTabObj.GetComponent<RectTransform>();
        dTr.sizeDelta = new Vector2(200f, 48f);
        Image dBg = dTabObj.AddComponent<Image>();
        dBg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
        Button dBtn = dTabObj.AddComponent<Button>();
        dBtn.targetGraphic = dBg;
        lobby.deckTabBtn = dBtn;
        lobby.deckTabBg = dBg;

        GameObject dTxtObj = new GameObject("Text", typeof(RectTransform));
        dTxtObj.transform.SetParent(dTabObj.transform, false);
        TextMeshProUGUI dt = dTxtObj.AddComponent<TextMeshProUGUI>();
        dt.text = "DECK";
        dt.fontSize = 18;
        dt.fontStyle = FontStyles.Bold;
        dt.alignment = TextAlignmentOptions.Center;
        dt.color = Color.white;
        dt.raycastTarget = false;
        RectTransform dtr = dTxtObj.GetComponent<RectTransform>();
        dtr.anchorMin = Vector2.zero;
        dtr.anchorMax = Vector2.one;
        dtr.sizeDelta = Vector2.zero;

        // Tab 4: Desire Tree
        GameObject pTabObj = new GameObject("PathsTab", typeof(RectTransform));
        pTabObj.transform.SetParent(tabsObj.transform, false);
        RectTransform pTr = pTabObj.GetComponent<RectTransform>();
        pTr.sizeDelta = new Vector2(220f, 48f);
        Image pBg = pTabObj.AddComponent<Image>();
        pBg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
        Button pBtn = pTabObj.AddComponent<Button>();
        pBtn.targetGraphic = pBg;
        lobby.pathsTabBtn = pBtn;
        lobby.pathsTabBg = pBg;

        GameObject pTxtObj = new GameObject("Text", typeof(RectTransform));
        pTxtObj.transform.SetParent(pTabObj.transform, false);
        TextMeshProUGUI pt = pTxtObj.AddComponent<TextMeshProUGUI>();
        pt.text = "DESIRE TREE";
        pt.fontSize = 18;
        pt.fontStyle = FontStyles.Bold;
        pt.alignment = TextAlignmentOptions.Center;
        pt.color = Color.white;
        pt.raycastTarget = false;
        RectTransform ptr = pTxtObj.GetComponent<RectTransform>();
        ptr.anchorMin = Vector2.zero;
        ptr.anchorMax = Vector2.one;
        ptr.sizeDelta = Vector2.zero;

        // Lustid Badge (Top-right pill in Lobby header)
        GameObject lustidBadgeObj = new GameObject("LustidBadge", typeof(RectTransform));
        lustidBadgeObj.transform.SetParent(panelObj.transform, false);
        RectTransform lbr = lustidBadgeObj.GetComponent<RectTransform>();
        lbr.anchorMin = new Vector2(1f, 1f);
        lbr.anchorMax = new Vector2(1f, 1f);
        lbr.pivot = new Vector2(1f, 1f);
        lbr.anchoredPosition = new Vector2(-25f, -15f);
        lbr.sizeDelta = new Vector2(250f, 48f);

        Image lbBg = lustidBadgeObj.AddComponent<Image>();
        lbBg.color = new Color(0.14f, 0.10f, 0.18f, 0.95f);
        Outline lbOutline = lustidBadgeObj.AddComponent<Outline>();
        lbOutline.effectColor = new Color(0.65f, 0.20f, 0.55f, 0.75f);
        lbOutline.effectDistance = new Vector2(1.5f, -1.5f);

        HorizontalLayoutGroup lbHlg = lustidBadgeObj.AddComponent<HorizontalLayoutGroup>();
        lbHlg.padding = new RectOffset(12, 12, 6, 6);
        lbHlg.spacing = 10;
        lbHlg.childAlignment = TextAnchor.MiddleCenter;
        lbHlg.childControlWidth = false;
        lbHlg.childControlHeight = false;

        // Lustid Icon
        GameObject lbIconObj = new GameObject("Icon", typeof(RectTransform));
        lbIconObj.transform.SetParent(lustidBadgeObj.transform, false);
        RectTransform lbIconRt = lbIconObj.GetComponent<RectTransform>();
        lbIconRt.sizeDelta = new Vector2(34f, 34f);
        Image lbIconImg = lbIconObj.AddComponent<Image>();
        lbIconImg.sprite = StatIconDatabase.LustidIcon;
        lbIconImg.preserveAspect = true;

        // Lustid Value Text
        GameObject lbTxtObj = new GameObject("Text", typeof(RectTransform));
        lbTxtObj.transform.SetParent(lustidBadgeObj.transform, false);
        TextMeshProUGUI lbTxt = lbTxtObj.AddComponent<TextMeshProUGUI>();
        lbTxt.text = "0 Lustid";
        lbTxt.fontSize = 20;
        lbTxt.fontStyle = FontStyles.Bold;
        lbTxt.alignment = TextAlignmentOptions.MidlineLeft;
        lbTxt.color = new Color(1f, 0.50f, 0.88f, 1f);
        RectTransform lbTxtRt = lbTxtObj.GetComponent<RectTransform>();
        lbTxtRt.sizeDelta = new Vector2(170f, 36f);
        lobby.lustidBadgeText = lbTxt;

        // =========================================================================
        // PAGE 1: EXPEDITION
        // =========================================================================
        GameObject page1 = new GameObject("ExpeditionPage", typeof(RectTransform));
        page1.transform.SetParent(panelObj.transform, false);
        RectTransform p1r = page1.GetComponent<RectTransform>();
        p1r.anchorMin = Vector2.zero;
        p1r.anchorMax = Vector2.one;
        p1r.sizeDelta = new Vector2(0f, -80f);
        p1r.anchoredPosition = new Vector2(0f, -35f);
        lobby.expeditionPage = page1;

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(page1.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "SANCTUARY CAVE";
        title.fontSize = 32;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.95f, 0.85f, 0.60f, 1f);
        title.raycastTarget = false;

        RectTransform titler = titleObj.GetComponent<RectTransform>();
        titler.anchorMin = new Vector2(0f, 1f);
        titler.anchorMax = new Vector2(1f, 1f);
        titler.pivot = new Vector2(0.5f, 1f);
        titler.anchoredPosition = new Vector2(0f, -15f);
        titler.sizeDelta = new Vector2(0f, 50f);

        // Narrative Description
        GameObject descObj = new GameObject("Description", typeof(RectTransform));
        descObj.transform.SetParent(page1.transform, false);
        TextMeshProUGUI desc = descObj.AddComponent<TextMeshProUGUI>();
        desc.text = "A safe subterranean enclave lit by luminescent moss and crystal veins. Here you can plan your next descent into the catacombs, manage your equipped garments, and prepare your battle deck.";
        desc.fontSize = 20;
        desc.alignment = TextAlignmentOptions.TopLeft;
        desc.color = new Color(0.85f, 0.88f, 0.95f, 1f);
        desc.lineSpacing = 12;
        desc.raycastTarget = false;

        RectTransform dr = descObj.GetComponent<RectTransform>();
        dr.anchorMin = new Vector2(0f, 0.5f);
        dr.anchorMax = new Vector2(1f, 1f);
        dr.pivot = new Vector2(0.5f, 1f);
        dr.anchoredPosition = new Vector2(0f, -75f);
        dr.sizeDelta = new Vector2(-60f, -100f);

        // Stats Card
        GameObject statsObj = new GameObject("StatsCard", typeof(RectTransform));
        statsObj.transform.SetParent(page1.transform, false);
        Image statsBg = statsObj.AddComponent<Image>();
        statsBg.color = new Color(0.12f, 0.15f, 0.20f, 0.90f);

        RectTransform sr = statsObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0f, 0f);
        sr.anchorMax = new Vector2(1f, 0.5f);
        sr.pivot = new Vector2(0.5f, 0.5f);
        sr.anchoredPosition = new Vector2(0f, 75f);
        sr.sizeDelta = new Vector2(-60f, -50f);

        GameObject statsTxtObj = new GameObject("Text", typeof(RectTransform));
        statsTxtObj.transform.SetParent(statsObj.transform, false);
        TextMeshProUGUI st = statsTxtObj.AddComponent<TextMeshProUGUI>();
        st.text = "Highest Depth Explored: Floor 1\nStatus: Rested and ready for battle";
        st.fontSize = 20;
        st.color = new Color(0.90f, 0.85f, 0.70f, 1f);
        st.alignment = TextAlignmentOptions.Center;
        st.raycastTarget = false;

        RectTransform str = statsTxtObj.GetComponent<RectTransform>();
        str.anchorMin = Vector2.zero;
        str.anchorMax = Vector2.one;
        str.sizeDelta = new Vector2(-20f, -20f);
        lobby.statsText = st;

        // Start Expedition Button
        GameObject startBtnObj = new GameObject("StartExpeditionButton", typeof(RectTransform));
        startBtnObj.transform.SetParent(page1.transform, false);
        Image sBg = startBtnObj.AddComponent<Image>();
        sBg.color = new Color(0.22f, 0.55f, 0.35f, 1f);

        Button sBtn = startBtnObj.AddComponent<Button>();
        sBtn.targetGraphic = sBg;

        RectTransform sbr = startBtnObj.GetComponent<RectTransform>();
        sbr.anchorMin = new Vector2(0.5f, 0f);
        sbr.anchorMax = new Vector2(0.5f, 0f);
        sbr.pivot = new Vector2(0.5f, 0f);
        sbr.anchoredPosition = new Vector2(0f, 40f);
        sbr.sizeDelta = new Vector2(380f, 62f);

        GameObject sTxt = new GameObject("Text", typeof(RectTransform));
        sTxt.transform.SetParent(startBtnObj.transform, false);
        TextMeshProUGUI stxr = sTxt.AddComponent<TextMeshProUGUI>();
        stxr.text = "START EXPEDITION";
        stxr.fontSize = 22;
        stxr.fontStyle = FontStyles.Bold;
        stxr.alignment = TextAlignmentOptions.Center;
        stxr.color = Color.white;
        stxr.raycastTarget = false;

        RectTransform stxr2 = sTxt.GetComponent<RectTransform>();
        stxr2.anchorMin = Vector2.zero;
        stxr2.anchorMax = Vector2.one;
        stxr2.sizeDelta = Vector2.zero;
        lobby.startExpeditionButton = sBtn;

        // =========================================================================
        // PAGE 2: WARDROBE
        // =========================================================================
        GameObject page2 = new GameObject("WardrobePage", typeof(RectTransform));
        page2.transform.SetParent(panelObj.transform, false);
        RectTransform p2r = page2.GetComponent<RectTransform>();
        p2r.anchorMin = Vector2.zero;
        p2r.anchorMax = Vector2.one;
        p2r.sizeDelta = new Vector2(0f, -80f);
        p2r.anchoredPosition = new Vector2(0f, -35f);
        lobby.wardrobePage = page2;

        // Title
        GameObject wTitleObj = new GameObject("Title", typeof(RectTransform));
        wTitleObj.transform.SetParent(page2.transform, false);
        TextMeshProUGUI wTitle = wTitleObj.AddComponent<TextMeshProUGUI>();
        wTitle.text = "WARDROBE & EQUIPMENT";
        wTitle.fontSize = 30;
        wTitle.fontStyle = FontStyles.Bold;
        wTitle.alignment = TextAlignmentOptions.Center;
        wTitle.color = new Color(0.95f, 0.85f, 0.60f, 1f);
        wTitle.raycastTarget = false;

        RectTransform wtr2 = wTitleObj.GetComponent<RectTransform>();
        wtr2.anchorMin = new Vector2(0f, 1f);
        wtr2.anchorMax = new Vector2(1f, 1f);
        wtr2.pivot = new Vector2(0.5f, 1f);
        wtr2.anchoredPosition = new Vector2(0f, -15f);
        wtr2.sizeDelta = new Vector2(0f, 45f);

        // Currently Equipped Clothes Box
        GameObject eqBox = new GameObject("EquippedBox", typeof(RectTransform));
        eqBox.transform.SetParent(page2.transform, false);
        Image eqBg = eqBox.AddComponent<Image>();
        eqBg.color = new Color(0.14f, 0.17f, 0.23f, 0.95f);

        RectTransform eqr = eqBox.GetComponent<RectTransform>();
        eqr.anchorMin = new Vector2(0f, 1f);
        eqr.anchorMax = new Vector2(1f, 1f);
        eqr.pivot = new Vector2(0.5f, 1f);
        eqr.anchoredPosition = new Vector2(0f, -70f);
        eqr.sizeDelta = new Vector2(-60f, 60f);

        GameObject eqTxtObj = new GameObject("Text", typeof(RectTransform));
        eqTxtObj.transform.SetParent(eqBox.transform, false);
        TextMeshProUGUI eqTxt = eqTxtObj.AddComponent<TextMeshProUGUI>();
        eqTxt.text = "Equipped Clothes: None";
        eqTxt.fontSize = 18;
        eqTxt.alignment = TextAlignmentOptions.MidlineLeft;
        eqTxt.color = new Color(0.92f, 0.88f, 0.75f, 1f);
        eqTxt.raycastTarget = false;

        RectTransform eqtr = eqTxtObj.GetComponent<RectTransform>();
        eqtr.anchorMin = new Vector2(0f, 0f);
        eqtr.anchorMax = new Vector2(0.65f, 1f);
        eqtr.anchoredPosition = new Vector2(20f, 0f);
        eqtr.sizeDelta = new Vector2(-20f, 0f);
        lobby.equippedClothesText = eqTxt;

        // Unequip Clothes Button
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
        ubr.anchoredPosition = new Vector2(-15f, 0f);
        ubr.sizeDelta = new Vector2(110f, 40f);

        GameObject uTxt = new GameObject("Text", typeof(RectTransform));
        uTxt.transform.SetParent(uBtnObj.transform, false);
        TextMeshProUGUI utxr = uTxt.AddComponent<TextMeshProUGUI>();
        utxr.text = "Strip";
        utxr.fontSize = 18;
        utxr.fontStyle = FontStyles.Bold;
        utxr.alignment = TextAlignmentOptions.Center;
        utxr.color = Color.white;
        utxr.raycastTarget = false;
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
        rbr.anchoredPosition = new Vector2(-135f, 0f);
        rbr.sizeDelta = new Vector2(130f, 40f);

        GameObject rTxt = new GameObject("Text", typeof(RectTransform));
        rTxt.transform.SetParent(rBtnObj.transform, false);
        TextMeshProUGUI rtxr = rTxt.AddComponent<TextMeshProUGUI>();
        rtxr.text = "Repair (Free)";
        rtxr.fontSize = 16;
        rtxr.fontStyle = FontStyles.Bold;
        rtxr.alignment = TextAlignmentOptions.Center;
        rtxr.color = Color.white;
        rtxr.raycastTarget = false;
        RectTransform rtxr2 = rTxt.GetComponent<RectTransform>();
        rtxr2.anchorMin = Vector2.zero;
        rtxr2.anchorMax = Vector2.one;
        rtxr2.sizeDelta = Vector2.zero;
        lobby.repairClothesBtn = rBtn;

        // Flesh Stabilization & Archive Container
        GameObject stabBox = new GameObject("FleshStabilizationBox", typeof(RectTransform));
        stabBox.transform.SetParent(page2.transform, false);
        Image stabBg = stabBox.AddComponent<Image>();
        stabBg.color = new Color(0.11f, 0.12f, 0.18f, 0.95f);
        RectTransform stbr = stabBox.GetComponent<RectTransform>();
        stbr.anchorMin = new Vector2(0f, 1f);
        stbr.anchorMax = new Vector2(1f, 1f);
        stbr.pivot = new Vector2(0.5f, 1f);
        stbr.anchoredPosition = new Vector2(0f, -140f);
        stbr.sizeDelta = new Vector2(-60f, 115f);
        lobby.fleshStabilizationContainer = stabBox;

        // Title Row
        GameObject stTitleObj = new GameObject("StabTitle", typeof(RectTransform));
        stTitleObj.transform.SetParent(stabBox.transform, false);
        RectTransform sttr = stTitleObj.GetComponent<RectTransform>();
        sttr.anchorMin = new Vector2(0f, 1f);
        sttr.anchorMax = new Vector2(1f, 1f);
        sttr.pivot = new Vector2(0.5f, 1f);
        sttr.anchoredPosition = new Vector2(15f, -8f);
        sttr.sizeDelta = new Vector2(-30f, 22f);
        TextMeshProUGUI stTitle = stTitleObj.AddComponent<TextMeshProUGUI>();
        stTitle.text = "FLESH STABILIZATION (Sanctuary Terminal - Choose Unlocked Anatomy)";
        stTitle.fontSize = 13;
        stTitle.fontStyle = FontStyles.Bold;
        stTitle.alignment = TextAlignmentOptions.MidlineLeft;
        stTitle.color = new Color(1f, 0.75f, 0.9f, 1f);

        // Buttons Row
        GameObject btnRow = new GameObject("SizeButtonsRow", typeof(RectTransform));
        btnRow.transform.SetParent(stabBox.transform, false);
        RectTransform brr = btnRow.GetComponent<RectTransform>();
        brr.anchorMin = new Vector2(0f, 1f);
        brr.anchorMax = new Vector2(1f, 1f);
        brr.pivot = new Vector2(0.5f, 1f);
        brr.anchoredPosition = new Vector2(0f, -34f);
        brr.sizeDelta = new Vector2(-20f, 36f);

        HorizontalLayoutGroup brHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        brHlg.spacing = 10;
        brHlg.childControlWidth = true;
        brHlg.childControlHeight = true;
        brHlg.childForceExpandWidth = true;
        brHlg.childForceExpandHeight = true;

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

        // Info / Gauge / Debug Row
        GameObject infoRow = new GameObject("InfoRow", typeof(RectTransform));
        infoRow.transform.SetParent(stabBox.transform, false);
        RectTransform infor = infoRow.GetComponent<RectTransform>();
        infor.anchorMin = new Vector2(0f, 0f);
        infor.anchorMax = new Vector2(1f, 0f);
        infor.pivot = new Vector2(0.5f, 0f);
        infor.anchoredPosition = new Vector2(0f, 6f);
        infor.sizeDelta = new Vector2(-20f, 32f);

        HorizontalLayoutGroup infHlg = infoRow.AddComponent<HorizontalLayoutGroup>();
        infHlg.spacing = 10;
        infHlg.childControlWidth = false;
        infHlg.childControlHeight = true;
        infHlg.childForceExpandWidth = false;
        infHlg.childForceExpandHeight = true;

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
        dbgrt.sizeDelta = new Vector2(120f, 28f);
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

        // Available Inventory Items List (Scrollable, cleanly offset below Flesh Stabilization)
        GameObject scrollObj = new GameObject("ItemsScroll", typeof(RectTransform));
        scrollObj.transform.SetParent(page2.transform, false);
        RectTransform scr = scrollObj.GetComponent<RectTransform>();
        scr.anchorMin = new Vector2(0f, 0f);
        scr.anchorMax = new Vector2(1f, 1f);
        scr.pivot = new Vector2(0.5f, 1f);
        scr.offsetMax = new Vector2(-30f, -270f);
        scr.offsetMin = new Vector2(30f, 15f);

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
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sRect.content = cntRt;
        lobby.wardrobeItemsContainer = cntRt;

        // =========================================================================
        // PAGE 3: DECKBUILDER
        // =========================================================================
        GameObject page3 = new GameObject("DeckPage", typeof(RectTransform));
        page3.transform.SetParent(panelObj.transform, false);
        RectTransform p3r = page3.GetComponent<RectTransform>();
        p3r.anchorMin = Vector2.zero;
        p3r.anchorMax = Vector2.one;
        p3r.sizeDelta = new Vector2(0f, -80f);
        p3r.anchoredPosition = new Vector2(0f, -35f);
        lobby.deckPage = page3;

        // Header Title Row (Title on Left, Deck Count on Right)
        GameObject dTitleObj = new GameObject("Title", typeof(RectTransform));
        dTitleObj.transform.SetParent(page3.transform, false);
        TextMeshProUGUI dTitle = dTitleObj.AddComponent<TextMeshProUGUI>();
        dTitle.text = "DECKBUILDER";
        dTitle.fontSize = 28;
        dTitle.fontStyle = FontStyles.Bold;
        dTitle.alignment = TextAlignmentOptions.Left;
        dTitle.color = new Color(0.95f, 0.85f, 0.60f, 1f);
        dTitle.raycastTarget = false;
        RectTransform dtr2 = dTitleObj.GetComponent<RectTransform>();
        dtr2.anchorMin = new Vector2(0f, 1f);
        dtr2.anchorMax = new Vector2(0.5f, 1f);
        dtr2.pivot = new Vector2(0f, 1f);
        dtr2.anchoredPosition = new Vector2(25f, -8f);
        dtr2.sizeDelta = new Vector2(0f, 32f);

        GameObject dCountObj = new GameObject("DeckCount", typeof(RectTransform));
        dCountObj.transform.SetParent(page3.transform, false);
        TextMeshProUGUI dCount = dCountObj.AddComponent<TextMeshProUGUI>();
        dCount.text = "Active Deck: 3 / 9 Cards";
        dCount.fontSize = 18;
        dCount.fontStyle = FontStyles.Bold;
        dCount.alignment = TextAlignmentOptions.Right;
        dCount.color = new Color(0.95f, 0.80f, 0.35f, 1f);
        dCount.raycastTarget = false;
        RectTransform dcr = dCountObj.GetComponent<RectTransform>();
        dcr.anchorMin = new Vector2(0.5f, 1f);
        dcr.anchorMax = new Vector2(1f, 1f);
        dcr.pivot = new Vector2(1f, 1f);
        dcr.anchoredPosition = new Vector2(-25f, -12f);
        dcr.sizeDelta = new Vector2(0f, 28f);
        lobby.deckCountText = dCount;

        // -------------------------------------------------------------------------
        // TOP SECTION: CARD SELECTION (Left: Collection Grid, Right: Detail Panel)
        // -------------------------------------------------------------------------
        GameObject topSectionObj = new GameObject("TopCardSelectionSection", typeof(RectTransform));
        topSectionObj.transform.SetParent(page3.transform, false);
        RectTransform tsrt = topSectionObj.GetComponent<RectTransform>();
        tsrt.anchorMin = new Vector2(0f, 0.28f);
        tsrt.anchorMax = new Vector2(1f, 1f);
        tsrt.offsetMin = new Vector2(25f, 10f);
        tsrt.offsetMax = new Vector2(-25f, -48f);

        // Top-Left: Collection Area (width ~66%)
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

        // Header Title
        GameObject colTitleObj = new GameObject("Title", typeof(RectTransform));
        colTitleObj.transform.SetParent(colHeadObj.transform, false);
        TextMeshProUGUI cht = colTitleObj.AddComponent<TextMeshProUGUI>();
        cht.text = "CARD COLLECTION";
        cht.fontSize = 16;
        cht.fontStyle = FontStyles.Bold;
        cht.color = new Color(0.45f, 0.80f, 0.95f, 1f);
        cht.alignment = TextAlignmentOptions.MidlineLeft;
        cht.raycastTarget = false;
        RectTransform chtr = colTitleObj.GetComponent<RectTransform>();
        chtr.anchorMin = new Vector2(0f, 0f);
        chtr.anchorMax = new Vector2(0.35f, 1f);
        chtr.offsetMin = Vector2.zero;
        chtr.offsetMax = Vector2.zero;

        // Filter Buttons Bar
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
        fbHlg.childForceExpandWidth = false;
        fbHlg.childForceExpandHeight = false;
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
            txt.raycastTarget = false;
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
        RectTransform cvrt = colViewport.GetComponent<RectTransform>();
        cvrt.anchorMin = Vector2.zero;
        cvrt.anchorMax = Vector2.one;
        cvrt.sizeDelta = Vector2.zero;
        colSr.viewport = cvrt;

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

        // Top-Right: Detail & Equip Panel (width ~34%)
        GameObject rightDetailObj = new GameObject("DetailPanel", typeof(RectTransform));
        rightDetailObj.transform.SetParent(topSectionObj.transform, false);
        RectTransform rdrt = rightDetailObj.GetComponent<RectTransform>();
        rdrt.anchorMin = new Vector2(0.66f, 0f);
        rdrt.anchorMax = new Vector2(1f, 1f);
        rdrt.offsetMin = new Vector2(10f, 0f);
        rdrt.offsetMax = Vector2.zero;

        Image dpBg = rightDetailObj.AddComponent<Image>();
        dpBg.color = new Color(0.68f, 0.54f, 0.28f, 0.95f); // Crisp antique gold outer frame

        // Dark luxury slate interior background
        GameObject dpInner = new GameObject("InnerBg", typeof(RectTransform));
        dpInner.transform.SetParent(rightDetailObj.transform, false);
        Image dpInnerImg = dpInner.AddComponent<Image>();
        dpInnerImg.color = new Color(0.07f, 0.08f, 0.12f, 0.98f);
        dpInnerImg.raycastTarget = false;
        RectTransform dpirt = dpInner.GetComponent<RectTransform>();
        dpirt.anchorMin = Vector2.zero;
        dpirt.anchorMax = Vector2.one;
        dpirt.offsetMin = new Vector2(2f, 2f);
        dpirt.offsetMax = new Vector2(-2f, -2f);
        dpInner.transform.SetAsFirstSibling();

        // 1. Detail Card Name
        GameObject dNameObj = new GameObject("DetailCardName", typeof(RectTransform));
        dNameObj.transform.SetParent(rightDetailObj.transform, false);
        TextMeshProUGUI dNameTxt = dNameObj.AddComponent<TextMeshProUGUI>();
        dNameTxt.text = "Strike";
        dNameTxt.fontSize = 24;
        dNameTxt.fontStyle = FontStyles.Bold;
        dNameTxt.alignment = TextAlignmentOptions.Center;
        dNameTxt.color = new Color(0.96f, 0.88f, 0.50f, 1f);
        dNameTxt.raycastTarget = false;
        RectTransform dnrt = dNameObj.GetComponent<RectTransform>();
        dnrt.anchorMin = new Vector2(0f, 1f);
        dnrt.anchorMax = new Vector2(1f, 1f);
        dnrt.pivot = new Vector2(0.5f, 1f);
        dnrt.anchoredPosition = new Vector2(0f, -12f);
        dnrt.sizeDelta = new Vector2(-20f, 32f);
        lobby.detailCardNameText = dNameTxt;

        // 2. Large Art Preview Window (Framed 150x150)
        GameObject dArtWindow = new GameObject("DetailArtWindow", typeof(RectTransform));
        dArtWindow.transform.SetParent(rightDetailObj.transform, false);
        dArtWindow.AddComponent<RectMask2D>();
        RectTransform dawRt = dArtWindow.GetComponent<RectTransform>();
        dawRt.anchorMin = new Vector2(0.5f, 1f);
        dawRt.anchorMax = new Vector2(0.5f, 1f);
        dawRt.pivot = new Vector2(0.5f, 1f);
        dawRt.anchoredPosition = new Vector2(0f, -48f);
        dawRt.sizeDelta = new Vector2(150f, 150f);

        GameObject dawBorder = new GameObject("Frame", typeof(RectTransform));
        dawBorder.transform.SetParent(rightDetailObj.transform, false);
        Image dawbImg = dawBorder.AddComponent<Image>();
        dawbImg.color = new Color(0.68f, 0.54f, 0.28f, 0.95f);
        dawbImg.raycastTarget = false;
        RectTransform dawbrt = dawBorder.GetComponent<RectTransform>();
        dawbrt.anchorMin = new Vector2(0.5f, 1f);
        dawbrt.anchorMax = new Vector2(0.5f, 1f);
        dawbrt.pivot = new Vector2(0.5f, 1f);
        dawbrt.anchoredPosition = new Vector2(0f, -46f);
        dawbrt.sizeDelta = new Vector2(154f, 154f);
        dawBorder.transform.SetAsFirstSibling();

        GameObject dArtObj = new GameObject("Art", typeof(RectTransform));
        dArtObj.transform.SetParent(dArtWindow.transform, false);
        Image dArtImg = dArtObj.AddComponent<Image>();
        dArtImg.preserveAspect = true;
        dArtImg.raycastTarget = false;
        RectTransform dartRt = dArtObj.GetComponent<RectTransform>();
        dartRt.anchorMin = new Vector2(0.5f, 0.5f);
        dartRt.anchorMax = new Vector2(0.5f, 0.5f);
        dartRt.pivot = new Vector2(0.5f, 0.5f);
        dartRt.anchoredPosition = Vector2.zero;
        dartRt.sizeDelta = new Vector2(280f, 150f);
        lobby.detailCardArtImg = dArtImg;

        // 3. Stats Rows (Row1: AP, CD, Target. Row2: Dmg, Shield, Lust, Clo)
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
        dStatsVlg.childForceExpandWidth = true;
        dStatsVlg.childForceExpandHeight = false;
        dStatsVlg.spacing = 5f;
        dStatsVlg.childAlignment = TextAnchor.MiddleCenter;

        GameObject row1 = new GameObject("Row1", typeof(RectTransform));
        row1.transform.SetParent(dStatsBox.transform, false);
        HorizontalLayoutGroup r1Hlg = row1.AddComponent<HorizontalLayoutGroup>();
        r1Hlg.childControlWidth = false;
        r1Hlg.childControlHeight = false;
        r1Hlg.childForceExpandWidth = false;
        r1Hlg.childForceExpandHeight = false;
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
        r2Hlg.childForceExpandWidth = false;
        r2Hlg.childForceExpandHeight = false;
        r2Hlg.spacing = 6f;
        r2Hlg.childAlignment = TextAnchor.MiddleCenter;
        RectTransform r2rt = row2.GetComponent<RectTransform>();
        r2rt.sizeDelta = new Vector2(0f, 26f);
        lobby.detailStatsRow2 = row2.transform;

        // 4. Description Box (Large readable font preserved)
        GameObject dDescObj = new GameObject("DetailDesc", typeof(RectTransform));
        dDescObj.transform.SetParent(rightDetailObj.transform, false);
        TextMeshProUGUI dDescTxt = dDescObj.AddComponent<TextMeshProUGUI>();
        dDescTxt.fontSize = 24;
        dDescTxt.alignment = TextAlignmentOptions.Top;
        dDescTxt.color = new Color(0.92f, 0.94f, 0.98f, 1f);
        dDescTxt.raycastTarget = false;
        RectTransform ddrt = dDescObj.GetComponent<RectTransform>();
        ddrt.anchorMin = new Vector2(0f, 0f);
        ddrt.anchorMax = new Vector2(1f, 1f);
        ddrt.offsetMin = new Vector2(18f, 75f);
        ddrt.offsetMax = new Vector2(-18f, -274f);
        lobby.detailCardDescText = dDescTxt;

        // 5. "ADD TO DECK" Button (Bottom of Detail Panel)
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
        deprt.anchoredPosition = new Vector2(0f, 16f);
        deprt.sizeDelta = new Vector2(260f, 48f);
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
        dEquipTxt.raycastTarget = false;
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

        // -------------------------------------------------------------------------
        // BOTTOM SECTION: ACTIVE DECK (Equipped for Combat)
        // -------------------------------------------------------------------------
        GameObject bottomSecObj = new GameObject("BottomActiveDeckSection", typeof(RectTransform));
        bottomSecObj.transform.SetParent(page3.transform, false);
        RectTransform bsrt = bottomSecObj.GetComponent<RectTransform>();
        bsrt.anchorMin = new Vector2(0f, 0f);
        bsrt.anchorMax = new Vector2(1f, 0.28f);
        bsrt.offsetMin = new Vector2(25f, 12f);
        bsrt.offsetMax = new Vector2(-25f, -6f);

        GameObject actHead = new GameObject("ActiveHeader", typeof(RectTransform));
        actHead.transform.SetParent(bottomSecObj.transform, false);
        TextMeshProUGUI aht = actHead.AddComponent<TextMeshProUGUI>();
        aht.text = "ACTIVE DECK (Equipped for Combat) • Hover over a card to reveal [X] to unequip";
        aht.fontSize = 16;
        aht.fontStyle = FontStyles.Bold;
        aht.color = new Color(0.95f, 0.80f, 0.35f, 1f);
        aht.raycastTarget = false;
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
        ahlg.childForceExpandWidth = false;
        ahlg.childForceExpandHeight = false;
        ahlg.childAlignment = TextAnchor.MiddleLeft;
        ahlg.padding = new RectOffset(6, 6, 4, 4);

        ContentSizeFitter acsf = actContent.AddComponent<ContentSizeFitter>();
        acsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // =========================================================================
        // PAGE 4: DESIRE TREE (PATHS OF DESIRE)
        // =========================================================================
        GameObject page4 = new GameObject("DesireTreePage", typeof(RectTransform));
        page4.transform.SetParent(panelObj.transform, false);
        RectTransform p4Rt = page4.GetComponent<RectTransform>();
        p4Rt.anchorMin = Vector2.zero;
        p4Rt.anchorMax = Vector2.one;
        p4Rt.offsetMin = new Vector2(25f, 25f);
        p4Rt.offsetMax = new Vector2(-25f, -80f);
        lobby.pathsPage = page4;

        // --- Top Bar: Path Selectors & Lustid Counter ---
        GameObject p4TopBar = new GameObject("TreeTopBar", typeof(RectTransform));
        p4TopBar.transform.SetParent(page4.transform, false);
        RectTransform p4tbRt = p4TopBar.GetComponent<RectTransform>();
        p4tbRt.anchorMin = new Vector2(0f, 1f);
        p4tbRt.anchorMax = new Vector2(1f, 1f);
        p4tbRt.pivot = new Vector2(0.5f, 1f);
        p4tbRt.anchoredPosition = Vector2.zero;
        p4tbRt.sizeDelta = new Vector2(0f, 48f);

        // Path Selector Buttons Row (Left)
        GameObject p4BtnsRow = new GameObject("PathButtonsRow", typeof(RectTransform));
        p4BtnsRow.transform.SetParent(p4TopBar.transform, false);
        RectTransform p4brRt = p4BtnsRow.GetComponent<RectTransform>();
        p4brRt.anchorMin = new Vector2(0f, 0f);
        p4brRt.anchorMax = new Vector2(0.72f, 1f);
        p4brRt.offsetMin = Vector2.zero;
        p4brRt.offsetMax = Vector2.zero;

        HorizontalLayoutGroup p4brHlg = p4BtnsRow.AddComponent<HorizontalLayoutGroup>();
        p4brHlg.spacing = 10f;
        p4brHlg.childControlWidth = true;
        p4brHlg.childControlHeight = true;
        p4brHlg.childForceExpandWidth = true;
        p4brHlg.childForceExpandHeight = true;

        // Path 1: Phallus Devotee
        GameObject p4P1Obj = new GameObject("Btn_PhallusDevotee", typeof(RectTransform));
        p4P1Obj.transform.SetParent(p4BtnsRow.transform, false);
        Image p4P1Bg = p4P1Obj.AddComponent<Image>();
        p4P1Bg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
        Button p4P1Btn = p4P1Obj.AddComponent<Button>();
        p4P1Btn.targetGraphic = p4P1Bg;
        GameObject p4P1TxtObj = new GameObject("Text", typeof(RectTransform));
        p4P1TxtObj.transform.SetParent(p4P1Obj.transform, false);
        TextMeshProUGUI p4P1Txt = p4P1TxtObj.AddComponent<TextMeshProUGUI>();
        p4P1Txt.text = "PHALLUS DEVOTEE";
        p4P1Txt.fontSize = 15;
        p4P1Txt.fontStyle = FontStyles.Bold;
        p4P1Txt.alignment = TextAlignmentOptions.Center;
        p4P1Txt.color = Color.white;
        p4P1Txt.raycastTarget = false;
        RectTransform p4P1tr = p4P1TxtObj.GetComponent<RectTransform>();
        p4P1tr.anchorMin = Vector2.zero;
        p4P1tr.anchorMax = Vector2.one;
        p4P1tr.sizeDelta = Vector2.zero;
        lobby.treePathPhallusBtn = p4P1Btn;
        lobby.treePathPhallusBg = p4P1Bg;
        lobby.treePathPhallusText = p4P1Txt;

        // Path 2: Bound & Submissive
        GameObject p4P2Obj = new GameObject("Btn_BoundSubmissive", typeof(RectTransform));
        p4P2Obj.transform.SetParent(p4BtnsRow.transform, false);
        Image p4P2Bg = p4P2Obj.AddComponent<Image>();
        p4P2Bg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
        Button p4P2Btn = p4P2Obj.AddComponent<Button>();
        p4P2Btn.targetGraphic = p4P2Bg;
        GameObject p4P2TxtObj = new GameObject("Text", typeof(RectTransform));
        p4P2TxtObj.transform.SetParent(p4P2Obj.transform, false);
        TextMeshProUGUI p4P2Txt = p4P2TxtObj.AddComponent<TextMeshProUGUI>();
        p4P2Txt.text = "BOUND & SUBMISSIVE";
        p4P2Txt.fontSize = 15;
        p4P2Txt.fontStyle = FontStyles.Bold;
        p4P2Txt.alignment = TextAlignmentOptions.Center;
        p4P2Txt.color = Color.white;
        p4P2Txt.raycastTarget = false;
        RectTransform p4P2tr = p4P2TxtObj.GetComponent<RectTransform>();
        p4P2tr.anchorMin = Vector2.zero;
        p4P2tr.anchorMax = Vector2.one;
        p4P2tr.sizeDelta = Vector2.zero;
        lobby.treePathBoundBtn = p4P2Btn;
        lobby.treePathBoundBg = p4P2Bg;
        lobby.treePathBoundText = p4P2Txt;

        // Path 3: Monster Transformation
        GameObject p4P3Obj = new GameObject("Btn_MonsterTransformation", typeof(RectTransform));
        p4P3Obj.transform.SetParent(p4BtnsRow.transform, false);
        Image p4P3Bg = p4P3Obj.AddComponent<Image>();
        p4P3Bg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
        Button p4P3Btn = p4P3Obj.AddComponent<Button>();
        p4P3Btn.targetGraphic = p4P3Bg;
        GameObject p4P3TxtObj = new GameObject("Text", typeof(RectTransform));
        p4P3TxtObj.transform.SetParent(p4P3Obj.transform, false);
        TextMeshProUGUI p4P3Txt = p4P3TxtObj.AddComponent<TextMeshProUGUI>();
        p4P3Txt.text = "MONSTER METAMORPHOSIS";
        p4P3Txt.fontSize = 15;
        p4P3Txt.fontStyle = FontStyles.Bold;
        p4P3Txt.alignment = TextAlignmentOptions.Center;
        p4P3Txt.color = Color.white;
        p4P3Txt.raycastTarget = false;
        RectTransform p4P3tr = p4P3TxtObj.GetComponent<RectTransform>();
        p4P3tr.anchorMin = Vector2.zero;
        p4P3tr.anchorMax = Vector2.one;
        p4P3tr.sizeDelta = Vector2.zero;
        lobby.treePathMonsterBtn = p4P3Btn;
        lobby.treePathMonsterBg = p4P3Bg;
        lobby.treePathMonsterText = p4P3Txt;

        // Top Bar Lustid display (Right)
        GameObject p4LustidObj = new GameObject("TreeLustidDisplay", typeof(RectTransform));
        p4LustidObj.transform.SetParent(p4TopBar.transform, false);
        RectTransform p4ldr = p4LustidObj.GetComponent<RectTransform>();
        p4ldr.anchorMin = new Vector2(0.72f, 0f);
        p4ldr.anchorMax = new Vector2(1f, 1f);
        p4ldr.offsetMin = new Vector2(10f, 0f);
        p4ldr.offsetMax = Vector2.zero;
        TextMeshProUGUI p4lt = p4LustidObj.AddComponent<TextMeshProUGUI>();
        p4lt.text = "Altar Essence: <color=#FF77DD>0 Lustid</color>";
        p4lt.fontSize = 16;
        p4lt.fontStyle = FontStyles.Bold;
        p4lt.alignment = TextAlignmentOptions.MidlineRight;
        p4lt.color = Color.white;
        lobby.treeHeaderLustidText = p4lt;

        // --- Main Section (Below Top Bar) ---
        // Left Column: Tree Nodes Scroll View (~64% width)
        GameObject treeLeftObj = new GameObject("TreeNodesArea", typeof(RectTransform));
        treeLeftObj.transform.SetParent(page4.transform, false);
        RectTransform tlaRt = treeLeftObj.GetComponent<RectTransform>();
        tlaRt.anchorMin = new Vector2(0f, 0f);
        tlaRt.anchorMax = new Vector2(0.64f, 1f);
        tlaRt.offsetMin = Vector2.zero;
        tlaRt.offsetMax = new Vector2(-10f, -56f);

        Image tlaBg = treeLeftObj.AddComponent<Image>();
        tlaBg.color = new Color(0.08f, 0.09f, 0.12f, 0.90f);
        Outline tlaOutline = treeLeftObj.AddComponent<Outline>();
        tlaOutline.effectColor = new Color(0.35f, 0.20f, 0.40f, 0.60f);
        tlaOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Subtitle header inside left column
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

        // Vertical Scroll View for Tree Nodes
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

        // Tree Inactive / Lock Overlay (Covers the left scroll area when tree is not active)
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
        todTxt.lineSpacing = 4f;

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
        tobtTxt.raycastTarget = false;

        lobby.treeOverlayPanel = treeOverlayObj;
        lobby.treeOverlayTitleText = totTxt;
        lobby.treeOverlayDescText = todTxt;
        lobby.treeOverlaySelectBtn = tobBtn;
        lobby.treeOverlaySelectBtnBg = tobBg;
        lobby.treeOverlaySelectBtnText = tobtTxt;

        // Right Column: Node Detail / Awakening Inspector (~36% width)
        GameObject treeRightObj = new GameObject("TreeDetailPanel", typeof(RectTransform));
        treeRightObj.transform.SetParent(page4.transform, false);
        RectTransform traRt = treeRightObj.GetComponent<RectTransform>();
        traRt.anchorMin = new Vector2(0.64f, 0f);
        traRt.anchorMax = new Vector2(1f, 1f);
        traRt.offsetMin = new Vector2(10f, 0f);
        traRt.offsetMax = new Vector2(0f, -56f);

        Image traBg = treeRightObj.AddComponent<Image>();
        traBg.color = new Color(0.12f, 0.09f, 0.16f, 0.98f);
        Outline traOutline = treeRightObj.AddComponent<Outline>();
        traOutline.effectColor = new Color(0.85f, 0.70f, 0.30f, 0.85f);
        traOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Header: "ALTAR OF AWAKENING"
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

        // Node Title
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
        dtt.alignment = TextAlignmentOptions.MidlineLeft;
        lobby.treeDetailTitleText = dtt;

        // Node Category
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
        dct.alignment = TextAlignmentOptions.MidlineLeft;
        lobby.treeDetailCategoryText = dct;

        // Separator line
        GameObject sepObj = new GameObject("Separator", typeof(RectTransform));
        sepObj.transform.SetParent(treeRightObj.transform, false);
        RectTransform sepRt = sepObj.GetComponent<RectTransform>();
        sepRt.anchorMin = new Vector2(0f, 1f);
        sepRt.anchorMax = new Vector2(1f, 1f);
        sepRt.pivot = new Vector2(0f, 1f);
        sepRt.anchoredPosition = new Vector2(18f, -104f);
        sepRt.sizeDelta = new Vector2(-36f, 2f);
        Image sepImg = sepObj.AddComponent<Image>();
        sepImg.color = new Color(0.85f, 0.70f, 0.30f, 0.35f);

        // Description text
        GameObject dtDescObj = new GameObject("NodeDesc", typeof(RectTransform));
        dtDescObj.transform.SetParent(treeRightObj.transform, false);
        RectTransform ddRt = dtDescObj.GetComponent<RectTransform>();
        ddRt.anchorMin = new Vector2(0f, 1f);
        ddRt.anchorMax = new Vector2(1f, 1f);
        ddRt.pivot = new Vector2(0f, 1f);
        ddRt.anchoredPosition = new Vector2(18f, -116f);
        ddRt.sizeDelta = new Vector2(-36f, 100f);
        TextMeshProUGUI ddt = dtDescObj.AddComponent<TextMeshProUGUI>();
        ddt.text = "Inspect any node on the left to view its awakening properties and bonuses.";
        ddt.fontSize = 14;
        ddt.color = new Color(0.85f, 0.88f, 0.94f, 1f);
        ddt.alignment = TextAlignmentOptions.TopLeft;
        ddt.enableWordWrapping = true;
        lobby.treeDetailDescText = ddt;

        // Bonuses text
        GameObject dtBonusObj = new GameObject("NodeBonuses", typeof(RectTransform));
        dtBonusObj.transform.SetParent(treeRightObj.transform, false);
        RectTransform dbRt = dtBonusObj.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(0f, 0f);
        dbRt.anchorMax = new Vector2(1f, 1f);
        dbRt.offsetMin = new Vector2(18f, 75f);
        dbRt.offsetMax = new Vector2(-18f, -225f);
        TextMeshProUGUI dbt = dtBonusObj.AddComponent<TextMeshProUGUI>();
        dbt.text = "";
        dbt.fontSize = 13;
        dbt.fontStyle = FontStyles.Bold;
        dbt.color = Color.white;
        dbt.alignment = TextAlignmentOptions.TopLeft;
        dbt.enableWordWrapping = true;
        lobby.treeDetailBonusText = dbt;

        // Bottom Purchase / Master Button
        GameObject pBuyObj = new GameObject("Btn_AwakenNode", typeof(RectTransform));
        pBuyObj.transform.SetParent(treeRightObj.transform, false);
        RectTransform pbrRt2 = pBuyObj.GetComponent<RectTransform>();
        pbrRt2.anchorMin = new Vector2(0f, 0f);
        pbrRt2.anchorMax = new Vector2(1f, 0f);
        pbrRt2.pivot = new Vector2(0.5f, 0f);
        pbrRt2.anchoredPosition = new Vector2(0f, 16f);
        pbrRt2.sizeDelta = new Vector2(-36f, 48f);

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
        pbrTxt.raycastTarget = false;
        RectTransform pbrtr = pbrTxtObj.GetComponent<RectTransform>();
        pbrtr.anchorMin = Vector2.zero;
        pbrtr.anchorMax = Vector2.one;
        pbrtr.sizeDelta = Vector2.zero;
        lobby.treePurchaseBtnText = pbrTxt;

        page2.SetActive(false);
        page3.SetActive(false);
        page4.SetActive(false);
        panelObj.SetActive(false);

        // Connect listeners and initialize
        lobby.SetupListeners();
        lobby.ShowTab(0);

        return lobby;
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
