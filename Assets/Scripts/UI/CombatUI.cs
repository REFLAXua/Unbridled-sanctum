using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnbridledSanctum.Combat;
using UnbridledSanctum.Data;

namespace UnbridledSanctum.UI
{
    /// <summary>
    /// Turn-based RPG Combat UI with:
    /// - Turn Order Tracker at top (illuminates active turn, dims when ended)
    /// - 1-second telegraphed skill displays over enemies and player
    /// - Non-overlapping AP and Shield indicators
    /// - Interactive card hand and enemy target selection
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        [Header("Main Panel")]
        public GameObject combatPanel;

        [Header("Top Turn Order Timeline")]
        public RectTransform turnOrderContainer;
        private List<TurnOrderBadgeUI> turnBadges = new List<TurnOrderBadgeUI>();

        [Header("Combat Stance Indicators (Under Turn Order)")]
        public RectTransform stanceContainer;
        public GameObject[] stanceSquareObjects = new GameObject[3];
        public Image[] stanceBorderImages = new Image[3];
        public Image[] stanceBackgroundImages = new Image[3];
        public Image neutralStanceIcon;
        public Image grappledStanceIcon;
        public Image penetratedStanceIcon;
        public TextMeshProUGUI[] stancePlaceholderLabels = new TextMeshProUGUI[3];
        public TextMeshProUGUI[] stanceNameLabels = new TextMeshProUGUI[3];
        public Sprite neutralStanceSprite;
        public Sprite grappledStanceSprite;
        public Sprite penetratedStanceSprite;

        [Header("Top Banners")]
        public TextMeshProUGUI combatLogText;
        public GameObject playerSkillBanner;
        public TextMeshProUGUI playerSkillBannerText;

        [Header("Player Status Elements (Top-Left HUD)")]
        public GameObject playerStatsPanel;
        public Image playerFlashOverlay;
        public TextMeshProUGUI playerHpText;
        public Image playerHpBar;
        public TextMeshProUGUI playerUpperClothesText;
        public TextMeshProUGUI playerLowerClothesText;
        public TextMeshProUGUI playerClothesText;
        public Image playerClothesBar;
        public TextMeshProUGUI playerSeductionText;
        public Image playerSeductionBar;
        public TextMeshProUGUI playerStacksText;
        public TextMeshProUGUI playerApText;
        public TextMeshProUGUI playerShieldText;
        public GameObject playerShieldBadgeObj;
        public TextMeshProUGUI playerCorruptionText;
        public TextMeshProUGUI playerStatusText;
        public TooltipPopup statTooltipPopup;

        [Header("Collapsible Combat Log (Bottom-Left)")]
        public GameObject combatLogPanel;
        public RectTransform combatLogContent;
        public ScrollRect combatLogScrollRect;
        public Button combatLogToggleBtn;
        public TextMeshProUGUI combatLogToggleBtnText;
        private bool isLogCollapsed = false;

        [Header("Targeting & Keyboard Selection")]
        public int focusedCardIndex = 0;
        public int selectedCardIndex = -1;
        public bool isTargetingMode = false;
        public int focusedEnemyIndex = 0;
        public GameObject playerTargetHighlight;

        [Header("Player Combat Avatar")]
        public RectTransform playerAvatarContainer;
        public GameObject playerAvatarClone;
        public Image playerAvatarBody;
        public Image playerAvatarGroin;
        public Image playerAvatarBreast;
        public Image playerAvatarUnderwear;
        public Image playerAvatarClothes;
        public Image playerAvatarLowerClothes;
        public Image playerAvatarHairBack;
        public Image playerAvatarHairFront;
        public Image playerAvatarEyebrows;
        public Image playerAvatarExpression;
        public Image playerAvatarBlush;
        public Image playerAvatarAccessory;
        public Image playerAvatarHitFlash;
        public Image playerAvatarPinkHaze;
        private Sprite playerBaseEmotion;
        private Coroutine playerEmotionCoroutine;

        [Header("Containers")]
        public RectTransform enemiesContainer;
        public RectTransform cardsContainer;
        public Button endTurnButton;
        public GameObject endTurnFocusBorder;
        public RectTransform endTurnRect;

        [Header("Card Skin")]
        public Sprite cardFrameSprite;
        public Sprite progressbarFrameSprite;

        [Header("Top Header Actions")]
        public Button surrenderButton;
        public Button debugWinHpButton;
        public Button debugWinSeductionButton;
        public Button debugFillGaugeButton;

        [Header("Outcome Modal")]
        public GameObject outcomeModal;
        public TextMeshProUGUI outcomeTitleText;
        public TextMeshProUGUI outcomeDescText;
        public TextMeshProUGUI outcomePranaText;
        public Button outcomeDebugFillGaugeBtn;
        public Button primaryOutcomeBtn;
        public TextMeshProUGUI primaryOutcomeBtnText;
        public Button secondaryOutcomeBtn;
        public TextMeshProUGUI secondaryOutcomeBtnText;
        public Button tertiaryOutcomeBtn;
        public TextMeshProUGUI tertiaryOutcomeBtnText;

        private CombatManager manager;
        private List<EnemyCardUI> enemyCards = new List<EnemyCardUI>();
        private List<CombatCardUI> cardUIs = new List<CombatCardUI>();

        public event Action OnCombatVictoryFinished;
        public event Action OnCombatDefeatToLobby;

        public static CombatUI Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            if (FloatingCombatText.Instance == null)
            {
                if (GetComponent<FloatingCombatText>() == null)
                {
                    gameObject.AddComponent<FloatingCombatText>();
                }
            }

            DisableButtonKeyNavigation();
        }

        public void BindManager(CombatManager combatManager)
        {
            if (manager != null)
            {
                manager.OnCombatStarted -= HandleCombatStarted;
                manager.OnTurnChanged -= HandleTurnChanged;
                manager.OnCombatantTurnStarted -= HandleCombatantTurnStarted;
                manager.OnCombatantTurnEnded -= HandleCombatantTurnEnded;
                manager.OnPlayerSkillTelegraphed -= HandlePlayerSkillTelegraphed;
                manager.OnPlayerSkillExecuted -= HandlePlayerSkillExecuted;
                manager.OnEnemySkillTelegraphed -= HandleEnemySkillTelegraphed;
                manager.OnEnemySkillUsed -= HandleEnemySkillUsed;
                manager.OnCombatEnded -= HandleCombatEnded;
                manager.OnCombatLogMessage -= HandleCombatLog;
                manager.OnPlayerActionFinished -= RefreshAll;
                manager.OnDamageDealt -= HandleDamageDealt;
                manager.OnSeductionDealt -= HandleSeductionDealt;
                manager.OnClothesDamaged -= HandleClothesDamaged;
                manager.OnSeductionPhaseChanged -= HandleSeductionPhaseChanged;
                manager.OnPlayerClothesStripped -= HandlePlayerClothesStripped;
                manager.OnPlayerGarmentStripped -= HandlePlayerGarmentStripped;
                manager.OnStanceChanged -= HandleStanceChanged;
                manager.OnHandUpdated -= HandleHandUpdated;
                manager.OnCameraShakeRequested -= HandleCameraShakeRequested;
                manager.OnEnemyEjaculated -= HandleEnemyEjaculated;
                manager.OnSexualMovementExecuted -= HandleSexualMovementExecuted;
            }

            manager = combatManager;
            if (manager != null)
            {
                manager.OnCombatStarted += HandleCombatStarted;
                manager.OnTurnChanged += HandleTurnChanged;
                manager.OnCombatantTurnStarted += HandleCombatantTurnStarted;
                manager.OnCombatantTurnEnded += HandleCombatantTurnEnded;
                manager.OnPlayerSkillTelegraphed += HandlePlayerSkillTelegraphed;
                manager.OnPlayerSkillExecuted += HandlePlayerSkillExecuted;
                manager.OnEnemySkillTelegraphed += HandleEnemySkillTelegraphed;
                manager.OnEnemySkillUsed += HandleEnemySkillUsed;
                manager.OnCombatEnded += HandleCombatEnded;
                manager.OnCombatLogMessage += HandleCombatLog;
                manager.OnPlayerActionFinished += RefreshAll;
                manager.OnDamageDealt += HandleDamageDealt;
                manager.OnSeductionDealt += HandleSeductionDealt;
                manager.OnClothesDamaged += HandleClothesDamaged;
                manager.OnSeductionPhaseChanged += HandleSeductionPhaseChanged;
                manager.OnPlayerClothesStripped += HandlePlayerClothesStripped;
                manager.OnPlayerGarmentStripped += HandlePlayerGarmentStripped;
                manager.OnStanceChanged += HandleStanceChanged;
                manager.OnHandUpdated += HandleHandUpdated;
                manager.OnCameraShakeRequested += HandleCameraShakeRequested;
                manager.OnEnemyEjaculated += HandleEnemyEjaculated;
                manager.OnSexualMovementExecuted += HandleSexualMovementExecuted;
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(() =>
                {
                    if (manager != null && manager.isPlayerTurn && !manager.isActionInProgress)
                    {
                        manager.EndPlayerTurn();
                    }
                });
            }

            DisableButtonKeyNavigation();

            if (surrenderButton != null)
            {
                surrenderButton.onClick.RemoveAllListeners();
                surrenderButton.onClick.AddListener(() =>
                {
                    // Strictly mouse click only, ignore keyboard Enter/Space
                    if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Space))
                        return;
                    manager?.Surrender();
                });
            }

            if (debugWinHpButton != null)
            {
                debugWinHpButton.onClick.RemoveAllListeners();
                debugWinHpButton.onClick.AddListener(() =>
                {
                    // Strictly mouse click only, ignore keyboard Enter/Space
                    if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Space))
                        return;
                    manager?.DebugInstantVictoryHp();
                });
            }

            if (debugWinSeductionButton != null)
            {
                debugWinSeductionButton.onClick.RemoveAllListeners();
                debugWinSeductionButton.onClick.AddListener(() =>
                {
                    // Strictly mouse click only, ignore keyboard Enter/Space
                    if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Space))
                        return;
                    manager?.DebugInstantVictorySeduction();
                });
            }

            if (debugFillGaugeButton != null)
            {
                debugFillGaugeButton.onClick.RemoveAllListeners();
                debugFillGaugeButton.onClick.AddListener(() =>
                {
                    if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Space))
                        return;
                    if (FleshTransformationManager.Instance != null)
                    {
                        FleshTransformationManager.Instance.DebugFillGauge();
                        if (FloatingCombatText.Instance != null && playerAvatarContainer != null)
                        {
                            FloatingCombatText.Instance.Spawn(playerAvatarContainer, "[DEBUG] Semen Gauge Filled (200/200 ml)!", CombatTextType.Seduction, new Vector2(0f, 160f));
                        }
                        HandleCombatLog("[DEBUG] Transformation Semen Gauge filled to 200/200 ml. Transformation ready for post-combat.");
                        RefreshOutcomePranaDisplay();
                    }
                });
            }
        }

        private void DisableButtonKeyNavigation()
        {
            Button[] buttons = { debugWinHpButton, debugWinSeductionButton, debugFillGaugeButton, surrenderButton, endTurnButton, outcomeDebugFillGaugeBtn };
            foreach (var b in buttons)
            {
                if (b != null)
                {
                    Navigation nav = b.navigation;
                    nav.mode = Navigation.Mode.None;
                    b.navigation = nav;
                }
            }
        }

        private void SetScenePaperDollVisible(bool visible)
        {
            GameUIManager gm = FindFirstObjectByType<GameUIManager>();
            if (gm != null && gm.paperDoll != null)
            {
                gm.paperDoll.gameObject.SetActive(visible);
            }

            GameObject scenePd = GameObject.Find("PaperDollPanel");
            if (scenePd != null && (playerAvatarClone == null || scenePd != playerAvatarClone))
            {
                scenePd.SetActive(visible);
            }

            PaperDollController[] allPd = FindObjectsByType<PaperDollController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pd in allPd)
            {
                if (pd != null && (playerAvatarClone == null || pd.gameObject != playerAvatarClone))
                {
                    pd.gameObject.SetActive(visible);
                }
            }
        }

        public void Show()
        {
            if (combatPanel != null) combatPanel.SetActive(true);
            if (playerSkillBanner != null) playerSkillBanner.SetActive(false);
            if (outcomeModal != null) outcomeModal.SetActive(false);

            if (cardsContainer != null) cardsContainer.gameObject.SetActive(true);
            if (endTurnButton != null) endTurnButton.gameObject.SetActive(true);
            if (playerStatsPanel != null) playerStatsPanel.SetActive(true);
            if (combatLogPanel != null) combatLogPanel.SetActive(true);

            InitOrRefreshPlayerAvatar();
            SetScenePaperDollVisible(false);

            RefreshAll();
        }

        public void Hide()
        {
            if (combatPanel != null) combatPanel.SetActive(false);
            if (outcomeModal != null) outcomeModal.SetActive(false);
            if (playerSkillBanner != null) playerSkillBanner.SetActive(false);

            SetScenePaperDollVisible(true);
        }

        private void LateUpdate()
        {
            if (combatPanel != null && combatPanel.activeInHierarchy)
            {
                SetScenePaperDollVisible(false);
            }
        }

        private void HandleCombatStarted()
        {
            selectedCardIndex = -1;
            focusedCardIndex = 0;
            isTargetingMode = false;

            Show();
            BuildTurnOrderBar();
            InitOrRefreshPlayerAvatar();
            SetScenePaperDollVisible(false);
            BuildEnemyCards();
            BuildPlayerCards();
            RefreshAll();
        }

        private void HandleTurnChanged(bool isPlayer)
        {
            if (isPlayer)
            {
                // New round begins: reset alive badges and activate player badge
                ResetTurnOrderRound();
            }
            RefreshAll();
        }

        private void HandleCombatantTurnStarted(Combatant combatant)
        {
            UpdateTurnOrderActive(combatant);
            RefreshAll();
        }

        private void HandleCombatantTurnEnded(Combatant combatant)
        {
            UpdateTurnOrderEnded(combatant);
            RefreshAll();
        }

        private void HandlePlayerSkillTelegraphed(CardInstance card, Combatant target)
        {
            if (playerSkillBanner != null)
            {
                playerSkillBanner.SetActive(true);
                if (playerSkillBannerText != null)
                {
                    playerSkillBannerText.text = $"<color=#FFDF66><b>[ ACTIVATING: {card.data.cardName.ToUpper()} ]</b></color>\n<size=16>{card.data.description}</size>";
                }
            }
            if (card != null && card.data.hpDamage > 0)
            {
                SetPlayerTemporaryEmotion("Angry 2", 1.4f);
            }
            else if (card != null && card.data.seductionAmount > 0)
            {
                SetPlayerTemporaryEmotion("Smile", 1.4f);
            }
            else if (card != null && card.data.shieldGain > 0)
            {
                SetPlayerTemporaryEmotion("Angry 1", 1.4f);
            }
            RefreshAll();
        }

        private void HandlePlayerSkillExecuted(CardInstance card, Combatant target)
        {
            if (playerSkillBanner != null)
            {
                playerSkillBanner.SetActive(false);
            }
            if (card != null && card.data.hpDamage > 0)
            {
                SetPlayerTemporaryEmotion("Angry 2", 0.85f);
            }
            RefreshAll();
        }

        private void HandleEnemySkillTelegraphed(Combatant enemy, EnemySkill skill)
        {
            foreach (var card in enemyCards)
            {
                if (card != null && card.assignedEnemy == enemy)
                {
                    card.ShowTelegraph(skill);
                }
            }
        }

        private void HandleEnemySkillUsed(Combatant enemy, EnemySkill skill)
        {
            foreach (var card in enemyCards)
            {
                if (card != null && card.assignedEnemy == enemy)
                {
                    card.HideTelegraph();
                }
            }
            RefreshAll();
        }

        private void HandleDamageDealt(Combatant target, int hpDamage, int absorbedShield)
        {
            if (target == null) return;

            Vector3 impactPos;
            if (target.isPlayer)
            {
                impactPos = playerAvatarContainer != null ? playerAvatarContainer.position : transform.position;
                PlayPlayerHitReaction(hpDamage, absorbedShield);
            }
            else
            {
                EnemyCardUI cardUI = FindEnemyCardUI(target);
                impactPos = cardUI != null ? cardUI.transform.position : transform.position;
                if (cardUI != null)
                {
                    cardUI.PlayHitReaction(hpDamage, absorbedShield);
                }
            }

            if (absorbedShield > 0)
            {
                PlayBarrierVFX(impactPos);
            }
            if (hpDamage > 0)
            {
                PlaySlashVFX(impactPos);
            }

            RefreshAll();
        }

        private void HandleSeductionDealt(Combatant target, int amount)
        {
            if (target == null || amount <= 0) return;

            Vector3 impactPos;
            if (target.isPlayer)
            {
                impactPos = playerAvatarContainer != null ? playerAvatarContainer.position : transform.position;
                if (FloatingCombatText.Instance != null && playerAvatarContainer != null)
                {
                    FloatingCombatText.Instance.Spawn(playerAvatarContainer, $"+{amount}% Lust", CombatTextType.Seduction, new Vector2(0f, 180f));
                }
            }
            else
            {
                EnemyCardUI cardUI = FindEnemyCardUI(target);
                impactPos = cardUI != null ? cardUI.transform.position : transform.position;
                if (cardUI != null)
                {
                    cardUI.PlaySeductionReaction(amount);
                }
            }

            PlayHeartsVFX(impactPos);
            RefreshAll();
        }

        private void HandleClothesDamaged(Combatant target, int clothesDamage)
        {
            if (target == null || clothesDamage <= 0) return;

            if (target.isPlayer)
            {
                if (FloatingCombatText.Instance != null && playerAvatarContainer != null)
                {
                    FloatingCombatText.Instance.Spawn(playerAvatarContainer, $"-{clothesDamage} Clothes", CombatTextType.ClothesDamage, new Vector2(0f, 160f));
                }
                if (manager != null && manager.player != null && manager.player.clothesHp > 0)
                {
                    StopCoroutine("PlayerShakeAndFlashRoutine");
                    StartCoroutine(PlayerShakeAndFlashRoutine());
                    SetPlayerTemporaryEmotion("Angry 1", 1.0f);
                }
            }
            RefreshAll();
        }

        private void HandleSeductionPhaseChanged(Combatant combatant, int newPhase)
        {
            if (combatant == null) return;

            if (combatant.isPlayer)
            {
                PlayPlayerPhaseTransition(newPhase);
            }
            else
            {
                EnemyCardUI cardUI = FindEnemyCardUI(combatant);
                if (cardUI != null)
                {
                    cardUI.PlayPhaseTransition(newPhase);
                }
            }
            RefreshAll();
        }

        private void HandlePlayerClothesStripped()
        {
            PlayPlayerClothesStrip(true, true);
            RefreshAll();
        }

        private void HandlePlayerGarmentStripped(bool upper, bool lower)
        {
            PlayPlayerClothesStrip(upper, lower);
            RefreshAll();
        }

        private void HandleCameraShakeRequested(float intensity, float duration)
        {
            TriggerScreenShake(intensity, duration);
        }

        private void HandleEnemyEjaculated(Combatant enemy, PenetrationSlot slot, int pranaMl)
        {
            // 1. Heavy screen shudder / camera shake
            TriggerScreenShake(20f, 1.4f);

            // 2. Avatar reaction: blush & ecstasy
            if (playerAvatarContainer != null)
            {
                StopCoroutine("PlayerShakeAndFlashRoutine");
                StartCoroutine(PlayerShakeAndFlashRoutine());

                if (FloatingCombatText.Instance != null)
                {
                    string targetStr = slot != PenetrationSlot.None ? slot.ToString() : "Vessel";
                    FloatingCombatText.Instance.Spawn(playerAvatarContainer, $"[EJACULATION] +{pranaMl}ml Cum ({targetStr})", CombatTextType.Seduction, new Vector2(0f, 190f));
                }

                SetPlayerTemporaryEmotion("o", 2.2f);
            }

            // 3. Enemy card celebration/climax phase trigger
            if (enemy != null)
            {
                EnemyCardUI cardUI = FindEnemyCardUI(enemy);
                if (cardUI != null)
                {
                    cardUI.PlayPhaseTransition(enemy.seductionStacks);
                }
            }

            RefreshAll();
        }

        private void HandleSexualMovementExecuted(Combatant enemy, EnemySkill skill, PenetrationSlot slot)
        {
            // Avatar sensation hint during sexual movement
            if (playerAvatarContainer != null)
            {
                if (FloatingCombatText.Instance != null && skill != null)
                {
                    FloatingCombatText.Instance.Spawn(playerAvatarContainer, $"♥ {skill.skillName}", CombatTextType.Seduction, new Vector2(0f, 160f));
                }
            }
            RefreshAll();
        }

        public void TriggerScreenShake(float intensity = 14f, float duration = 0.8f)
        {
            StopCoroutine("ScreenShakeRoutine");
            StartCoroutine(ScreenShakeRoutine(intensity, duration));
        }

        private IEnumerator ScreenShakeRoutine(float intensity, float duration)
        {
            Transform camTransform = Camera.main != null ? Camera.main.transform : null;
            Vector3 origCamPos = camTransform != null ? camTransform.localPosition : Vector3.zero;

            RectTransform combatRect = combatPanel != null ? combatPanel.GetComponent<RectTransform>() : null;
            Vector2 origPanelPos = combatRect != null ? combatRect.anchoredPosition : Vector2.zero;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - Mathf.Clamp01(elapsed / duration);
                float x = UnityEngine.Random.Range(-intensity, intensity) * damp;
                float y = UnityEngine.Random.Range(-intensity, intensity) * damp;

                if (camTransform != null)
                {
                    camTransform.localPosition = origCamPos + new Vector3(x * 0.015f, y * 0.015f, 0f);
                }
                if (combatRect != null)
                {
                    combatRect.anchoredPosition = origPanelPos + new Vector2(x, y);
                }
                yield return null;
            }

            if (camTransform != null) camTransform.localPosition = origCamPos;
            if (combatRect != null) combatRect.anchoredPosition = origPanelPos;
        }

        private EnemyCardUI FindEnemyCardUI(Combatant target)
        {
            for (int i = 0; i < enemyCards.Count; i++)
            {
                if (enemyCards[i] != null && enemyCards[i].assignedEnemy == target)
                {
                    return enemyCards[i];
                }
            }
            return null;
        }

        public static Vector2 GetEnemySnakePosition(int index, int totalEnemies)
        {
            if (totalEnemies <= 1) return new Vector2(0f, 70f);
            if (totalEnemies == 2)
            {
                return (index == 0) ? new Vector2(-170f, 65f) : new Vector2(170f, 105f);
            }
            // 3 enemies: V-formation snake (staggered depth & height)
            // Raised significantly so their bottom status plates do not overlap with the player's card hand
            switch (index)
            {
                case 0: return new Vector2(-280f, 65f); // Front-Left (raised high above card hand)
                case 1: return new Vector2(0f, 120f);   // Center-Back (depth)
                case 2: return new Vector2(280f, 75f);  // Front-Right (raised high above card hand)
                default: return Vector2.zero;
            }
        }

        private void SortEnemySiblings()
        {
            if (enemyCards.Count == 3)
            {
                // Enemy 1 is in the center-back (Y = +60), so it should be at sibling 0 (behind)
                if (enemyCards.Count > 1 && enemyCards[1] != null) enemyCards[1].transform.SetSiblingIndex(0);
                if (enemyCards.Count > 0 && enemyCards[0] != null) enemyCards[0].transform.SetSiblingIndex(1);
                if (enemyCards.Count > 2 && enemyCards[2] != null) enemyCards[2].transform.SetSiblingIndex(2);
            }
        }

        // =========================================================================
        // PLAYER COMBAT AVATAR LOGIC
        // =========================================================================

        public void InitOrRefreshPlayerAvatar()
        {
            if (playerAvatarContainer == null) return;

            PaperDollController pd = FindFirstObjectByType<PaperDollController>(FindObjectsInactive.Include);
            if (pd != null)
            {
                if (playerAvatarClone == null)
                {
                    playerAvatarClone = Instantiate(pd.gameObject, playerAvatarContainer);
                    playerAvatarClone.name = "PlayerCombatDoll";
                    playerAvatarClone.SetActive(true);

                    // Remove PaperDollController from clone so it doesn't fight CombatUI
                    PaperDollController cPd = playerAvatarClone.GetComponent<PaperDollController>();
                    if (cPd != null)
                    {
                        DestroyImmediate(cPd);
                    }

                    // Remove buttons / zoom sliders from clone
                    foreach (Transform child in playerAvatarClone.transform)
                    {
                        if (child.name.IndexOf("Zoom", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            child.name.IndexOf("Btn", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Destroy(child.gameObject);
                        }
                    }

                    RectTransform pcr = playerAvatarClone.GetComponent<RectTransform>();
                    pcr.anchorMin = new Vector2(0.5f, 0.5f);
                    pcr.anchorMax = new Vector2(0.5f, 0.5f);
                    pcr.pivot = new Vector2(0.5f, 0.5f);
                    pcr.sizeDelta = new Vector2(720f, 1080f);
                    pcr.localScale = new Vector3(0.92f, 0.92f, 1f);
                    pcr.anchoredPosition = new Vector2(0f, -30f);

                    // Bind ALL layers on the clone by exact child name
                    playerAvatarHairBack = FindImage(playerAvatarClone, "HairBack");
                    playerAvatarBody = FindImage(playerAvatarClone, "Body");
                    playerAvatarGroin = FindImage(playerAvatarClone, "Groin");
                    playerAvatarBreast = FindImage(playerAvatarClone, "Breast");
                    playerAvatarUnderwear = FindImage(playerAvatarClone, "Underwear");
                    playerAvatarLowerClothes = FindImage(playerAvatarClone, "LowerClothes");
                    playerAvatarClothes = FindImage(playerAvatarClone, "Clothes");
                    playerAvatarEyebrows = FindImage(playerAvatarClone, "Eyebrows");
                    playerAvatarHairFront = FindImage(playerAvatarClone, "HairFront");
                    playerAvatarAccessory = FindImage(playerAvatarClone, "Accessory");
                    playerAvatarBlush = FindImage(playerAvatarClone, "Blush");
                    playerAvatarExpression = FindImage(playerAvatarClone, "Expression");

                    // Ensure LowerClothes exists on the clone
                    if (playerAvatarLowerClothes == null && playerAvatarBody != null)
                    {
                        GameObject lObj = new GameObject("LowerClothes", typeof(RectTransform), typeof(Image));
                        lObj.transform.SetParent(playerAvatarClone.transform, false);
                        if (playerAvatarClothes != null)
                        {
                            lObj.transform.SetSiblingIndex(playerAvatarClothes.transform.GetSiblingIndex());
                        }

                        RectTransform lRt = lObj.GetComponent<RectTransform>();
                        RectTransform bodyRt = playerAvatarBody.rectTransform;
                        lRt.anchorMin = bodyRt.anchorMin;
                        lRt.anchorMax = bodyRt.anchorMax;
                        lRt.pivot = bodyRt.pivot;
                        lRt.anchoredPosition = bodyRt.anchoredPosition;
                        lRt.sizeDelta = bodyRt.sizeDelta;

                        playerAvatarLowerClothes = lObj.GetComponent<Image>();
                        playerAvatarLowerClothes.raycastTarget = false;
                        playerAvatarLowerClothes.preserveAspect = true;
                        playerAvatarLowerClothes.enabled = false;
                    }

                    // Ensure Groin exists on the clone
                    if (playerAvatarGroin == null && playerAvatarBody != null)
                    {
                        GameObject gObj = new GameObject("Groin", typeof(RectTransform), typeof(Image));
                        gObj.transform.SetParent(playerAvatarClone.transform, false);
                        gObj.transform.SetSiblingIndex(playerAvatarBody.transform.GetSiblingIndex() + 1);

                        RectTransform gRt = gObj.GetComponent<RectTransform>();
                        RectTransform bodyRt = playerAvatarBody.rectTransform;
                        gRt.anchorMin = bodyRt.anchorMin;
                        gRt.anchorMax = bodyRt.anchorMax;
                        gRt.pivot = bodyRt.pivot;
                        gRt.anchoredPosition = bodyRt.anchoredPosition;
                        gRt.sizeDelta = bodyRt.sizeDelta;

                        playerAvatarGroin = gObj.GetComponent<Image>();
                        playerAvatarGroin.raycastTarget = false;
                        playerAvatarGroin.preserveAspect = true;
                        playerAvatarGroin.enabled = false;
                    }

                    // Ensure Breast exists on the clone
                    if (playerAvatarBreast == null && playerAvatarBody != null)
                    {
                        GameObject bObj = new GameObject("Breast", typeof(RectTransform), typeof(Image));
                        bObj.transform.SetParent(playerAvatarClone.transform, false);
                        bObj.transform.SetSiblingIndex(playerAvatarBody.transform.GetSiblingIndex() + 1);

                        RectTransform bRt = bObj.GetComponent<RectTransform>();
                        RectTransform bodyRt = playerAvatarBody.rectTransform;
                        bRt.anchorMin = bodyRt.anchorMin;
                        bRt.anchorMax = bodyRt.anchorMax;
                        bRt.pivot = bodyRt.pivot;
                        bRt.anchoredPosition = bodyRt.anchoredPosition;
                        bRt.sizeDelta = bodyRt.sizeDelta;

                        playerAvatarBreast = bObj.GetComponent<Image>();
                        playerAvatarBreast.raycastTarget = false;
                        playerAvatarBreast.preserveAspect = true;
                        playerAvatarBreast.enabled = false;
                    }

                    if (playerAvatarExpression != null)
                    {
                        playerAvatarExpression.enabled = true;
                        playerAvatarExpression.gameObject.SetActive(true);
                        if (pd != null && pd.expressionImage != null && pd.expressionImage.material != null)
                        {
                            playerAvatarExpression.material = pd.expressionImage.material;
                        }
                    }

                    // Add Hit Flash overlay
                    GameObject hfObj = new GameObject("PlayerHitFlash", typeof(RectTransform));
                    hfObj.transform.SetParent(playerAvatarClone.transform, false);
                    playerAvatarHitFlash = hfObj.AddComponent<Image>();
                    playerAvatarHitFlash.color = new Color(1f, 0.12f, 0.12f, 0f);
                    playerAvatarHitFlash.raycastTarget = false;
                    RectTransform hfrt = hfObj.GetComponent<RectTransform>();
                    hfrt.anchorMin = Vector2.zero;
                    hfrt.anchorMax = Vector2.one;
                    hfrt.sizeDelta = Vector2.zero;

                    // Add Pink Haze overlay
                    GameObject phObj = new GameObject("PlayerPinkHaze", typeof(RectTransform));
                    phObj.transform.SetParent(playerAvatarClone.transform, false);
                    playerAvatarPinkHaze = phObj.AddComponent<Image>();
                    playerAvatarPinkHaze.color = new Color(1f, 0.35f, 0.85f, 0f);
                    playerAvatarPinkHaze.raycastTarget = false;
                    RectTransform phrt = phObj.GetComponent<RectTransform>();
                    phrt.anchorMin = Vector2.zero;
                    phrt.anchorMax = Vector2.one;
                    phrt.sizeDelta = Vector2.zero;
                }

                // Sync sprites & colors from main PaperDoll (Hair layers disabled for canonical fixed appearance)
                if (playerAvatarHairBack != null) playerAvatarHairBack.enabled = false;
                SyncImg(playerAvatarBody, pd.bodyImage);
                SyncImg(playerAvatarGroin, pd.groinImage);
                SyncImg(playerAvatarBreast, pd.breastImage);
                SyncImg(playerAvatarUnderwear, pd.underwearImage);
                SyncImg(playerAvatarLowerClothes, pd.lowerClothesImage);
                SyncImg(playerAvatarClothes, pd.clothesImage);
                SyncImg(playerAvatarEyebrows, pd.eyebrowsImage);
                if (playerAvatarHairFront != null) playerAvatarHairFront.enabled = false;
                SyncImg(playerAvatarAccessory, pd.accessoryImage);

                // Initial clothes visibility (independent Upper and Lower)
                bool hasUpper = manager != null && manager.player != null && manager.player.upperClothesHp > 0;
                bool hasLower = manager != null && manager.player != null && manager.player.lowerClothesHp > 0;

                if (playerAvatarClothes != null)
                {
                    playerAvatarClothes.enabled = hasUpper && (playerAvatarClothes.sprite != null);
                    playerAvatarClothes.gameObject.SetActive(playerAvatarClothes.enabled);
                }

                if (playerAvatarLowerClothes != null)
                {
                    playerAvatarLowerClothes.enabled = hasLower && (playerAvatarLowerClothes.sprite != null);
                    playerAvatarLowerClothes.gameObject.SetActive(playerAvatarLowerClothes.enabled);
                }

                if (playerAvatarUnderwear != null)
                {
                    playerAvatarUnderwear.enabled = hasLower && (playerAvatarUnderwear.sprite != null);
                    playerAvatarUnderwear.gameObject.SetActive(playerAvatarUnderwear.enabled);
                }

                if (playerAvatarGroin != null)
                {
                    bool lowerCovered = (playerAvatarUnderwear != null && playerAvatarUnderwear.enabled && playerAvatarUnderwear.sprite != null) ||
                                        (playerAvatarLowerClothes != null && playerAvatarLowerClothes.enabled && playerAvatarLowerClothes.sprite != null);
                    playerAvatarGroin.enabled = !lowerCovered && (playerAvatarGroin.sprite != null);
                    playerAvatarGroin.gameObject.SetActive(playerAvatarGroin.enabled);
                }

                // Determine base emotion
                CharacterState cs = manager != null ? manager.characterState : FindFirstObjectByType<CharacterState>();
                if (cs != null && cs.appearanceDatabase != null)
                {
                    if (manager != null && manager.player != null && manager.player.seductionStacks >= 2)
                    {
                        // On phase 2, base emotion switches to o.png
                        playerBaseEmotion = FindEmotionSprite(cs.appearanceDatabase, "o");
                    }
                    else
                    {
                        playerBaseEmotion = cs.selectedExpression;
                        if (playerBaseEmotion == null)
                        {
                            playerBaseEmotion = FindEmotionSprite(cs.appearanceDatabase, "Normal");
                        }
                    }
                }

                if (playerAvatarExpression != null && playerEmotionCoroutine == null)
                {
                    if (playerBaseEmotion != null)
                    {
                        playerAvatarExpression.sprite = playerBaseEmotion;
                    }
                    playerAvatarExpression.enabled = true;
                }

                UpdatePlayerBlush();
            }
        }

        private void UpdatePlayerBlush()
        {
            if (playerAvatarBlush == null) return;
            CharacterState cs = manager != null ? manager.characterState : FindFirstObjectByType<CharacterState>();
            if (cs == null || cs.appearanceDatabase == null || cs.appearanceDatabase.blushStyles == null)
            {
                playerAvatarBlush.enabled = false;
                return;
            }

            int stacks = (manager != null && manager.player != null) ? manager.player.seductionStacks : 0;
            if (stacks == 1 && cs.appearanceDatabase.blushStyles.Count >= 1)
            {
                playerAvatarBlush.sprite = cs.appearanceDatabase.blushStyles[0]; // 1.png
                playerAvatarBlush.enabled = true;
            }
            else if (stacks >= 2 && cs.appearanceDatabase.blushStyles.Count >= 2)
            {
                playerAvatarBlush.sprite = cs.appearanceDatabase.blushStyles[1]; // 2.png
                playerAvatarBlush.enabled = true;
            }
            else
            {
                playerAvatarBlush.enabled = false;
            }
        }

        private bool isClothesStripLock = false;

        public void SetPlayerTemporaryEmotion(string emotionName, float duration, bool isStripEvent = false)
        {
            if (isStripEvent)
            {
                isClothesStripLock = true;
            }
            else if (isClothesStripLock)
            {
                // Clothes were just stripped! Do not allow normal hit reactions (e.g. Crying) to overwrite shocked!
                return;
            }

            if (playerAvatarExpression == null && playerAvatarClone != null)
            {
                playerAvatarExpression = FindImage(playerAvatarClone, "Expression");
            }
            if (playerAvatarExpression == null) return;

            CharacterState cs = manager != null ? manager.characterState : FindFirstObjectByType<CharacterState>();
            CharacterAppearanceDatabase db = cs != null ? cs.appearanceDatabase : null;

            Sprite targetSprite = FindEmotionSprite(db, emotionName);
            if (targetSprite == null)
            {
                Debug.LogWarning($"[CombatUI] Could not find emotion sprite for: '{emotionName}'");
                return;
            }

            if (playerEmotionCoroutine != null) StopCoroutine(playerEmotionCoroutine);
            playerEmotionCoroutine = StartCoroutine(TemporaryEmotionRoutine(targetSprite, duration));
        }

        private IEnumerator TemporaryEmotionRoutine(Sprite tempSprite, float duration)
        {
            if (playerAvatarExpression != null)
            {
                playerAvatarExpression.sprite = tempSprite;
                playerAvatarExpression.enabled = true;
                playerAvatarExpression.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(duration);

            isClothesStripLock = false;

            if (playerAvatarExpression != null)
            {
                CharacterState cs = manager != null ? manager.characterState : FindFirstObjectByType<CharacterState>();
                if (manager != null && manager.player != null && manager.player.seductionStacks >= 2 && cs != null && cs.appearanceDatabase != null)
                {
                    playerBaseEmotion = FindEmotionSprite(cs.appearanceDatabase, "o");
                }
                else if (cs != null && cs.selectedExpression != null)
                {
                    playerBaseEmotion = cs.selectedExpression;
                }

                if (playerBaseEmotion != null)
                {
                    playerAvatarExpression.sprite = playerBaseEmotion;
                    playerAvatarExpression.enabled = true;
                }
            }
            playerEmotionCoroutine = null;
        }

        private Sprite FindEmotionSprite(CharacterAppearanceDatabase db, string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            string cleanKey = key.Trim();
            string normKey = cleanKey.Replace(" ", "").ToLowerInvariant();

            if (normKey == "crying" || normKey == "hurt") cleanKey = "Pain";
            else if (normKey.Contains("angry")) cleanKey = "Angry";
            normKey = cleanKey.Replace(" ", "").ToLowerInvariant();

            // 1. Search database emotionStyles
            if (db != null && db.emotionStyles != null)
            {
                foreach (var s in db.emotionStyles)
                {
                    if (s == null) continue;
                    if (s.name.IndexOf(cleanKey, StringComparison.OrdinalIgnoreCase) >= 0) return s;
                    string sNorm = s.name.Replace(" ", "").ToLowerInvariant();
                    if (sNorm.Contains(normKey) || normKey.Contains(sNorm)) return s;
                }
            }

            // 2. Fallback: Search all loaded sprites
            Sprite[] loaded = Resources.FindObjectsOfTypeAll<Sprite>();
            if (loaded != null)
            {
                foreach (var s in loaded)
                {
                    if (s == null) continue;
                    if (s.name.IndexOf(cleanKey, StringComparison.OrdinalIgnoreCase) >= 0) return s;
                    string sNorm = s.name.Replace(" ", "").ToLowerInvariant();
                    if (sNorm == normKey || sNorm.Contains(normKey)) return s;
                }
            }

#if UNITY_EDITOR
            // 3. Fallback: AssetDatabase search
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{cleanKey} t:Sprite", new[] { "Assets/sprites/Expression/Emotions", "Assets/sprites/Expression" });
            if (guids != null && guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null) return sp;
            }
#endif

            return null;
        }

        public void PlayPlayerHitReaction(int damage, int absorbed)
        {
            if (absorbed > 0 && FloatingCombatText.Instance != null && playerAvatarContainer != null)
            {
                FloatingCombatText.Instance.Spawn(playerAvatarContainer, $"-{absorbed} Block", CombatTextType.ShieldBlock, new Vector2(0f, 180f));
            }

            if (damage > 0 && FloatingCombatText.Instance != null && playerAvatarContainer != null)
            {
                FloatingCombatText.Instance.Spawn(playerAvatarContainer, $"-{damage}", CombatTextType.Damage, new Vector2(0f, 180f));
            }

            if (damage > 0 || absorbed > 0)
            {
                StopCoroutine("PlayerShakeAndFlashRoutine");
                StartCoroutine(PlayerShakeAndFlashRoutine());

                // Crying / hurt emotion on hit for 1 second
                SetPlayerTemporaryEmotion("Crying", 1.0f);
            }
        }

        private IEnumerator PlayerShakeAndFlashRoutine()
        {
            if (playerAvatarContainer == null) yield break;
            Vector2 origPos = playerAvatarContainer.anchoredPosition;

            if (playerAvatarHitFlash != null)
            {
                playerAvatarHitFlash.color = new Color(1f, 0.12f, 0.12f, 0.45f);
            }

            float dur = 0.25f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - (elapsed / dur);
                float x = UnityEngine.Random.Range(-10f, 10f) * damp;
                float y = UnityEngine.Random.Range(-6f, 6f) * damp;
                playerAvatarContainer.anchoredPosition = origPos + new Vector2(x, y);

                if (playerAvatarHitFlash != null)
                {
                    playerAvatarHitFlash.color = new Color(1f, 0.12f, 0.12f, 0.45f * damp);
                }
                yield return null;
            }

            playerAvatarContainer.anchoredPosition = origPos;
            if (playerAvatarHitFlash != null)
            {
                playerAvatarHitFlash.color = new Color(1f, 0.12f, 0.12f, 0f);
            }
        }

        public void PlayPlayerClothesStrip(bool upper = true, bool lower = true)
        {
            if (upper && playerAvatarClothes != null)
            {
                playerAvatarClothes.enabled = false;
                playerAvatarClothes.gameObject.SetActive(false);
            }
            if (lower)
            {
                if (playerAvatarLowerClothes != null)
                {
                    playerAvatarLowerClothes.enabled = false;
                    playerAvatarLowerClothes.gameObject.SetActive(false);
                }
                if (playerAvatarUnderwear != null)
                {
                    playerAvatarUnderwear.enabled = false;
                    playerAvatarUnderwear.gameObject.SetActive(false);
                }
                if (playerAvatarGroin != null && playerAvatarGroin.sprite != null)
                {
                    playerAvatarGroin.enabled = true;
                    playerAvatarGroin.gameObject.SetActive(true);
                }
            }
            if (upper && playerAvatarBreast != null && playerAvatarBreast.sprite != null)
            {
                playerAvatarBreast.enabled = true;
                playerAvatarBreast.gameObject.SetActive(true);
            }
            if (FloatingCombatText.Instance != null && playerAvatarContainer != null)
            {
                string txt = (upper && lower) ? "CLOTHES TORN!" : (upper ? "TOP TORN!" : "BOTTOM TORN!");
                FloatingCombatText.Instance.Spawn(playerAvatarContainer, txt, CombatTextType.ClothesDamage, new Vector2(0f, 200f));
            }
            StopCoroutine("PlayerShakeAndFlashRoutine");
            StartCoroutine(PlayerShakeAndFlashRoutine());
            // User requirement: "emotion duration on clothes strip lasts 1-2 seconds" -> 1.8 seconds with lock!
            SetPlayerTemporaryEmotion("shocked", 1.8f, isStripEvent: true);
        }

        public void PlayPlayerPhaseTransition(int newPhase)
        {
            StopCoroutine("PlayerPhaseTransitionRoutine");
            StartCoroutine(PlayerPhaseTransitionRoutine(newPhase));
        }

        private IEnumerator PlayerPhaseTransitionRoutine(int newPhase)
        {
            if (playerAvatarPinkHaze != null)
            {
                float fadeIn = 0.35f;
                float elapsed = 0f;
                while (elapsed < fadeIn)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0f, 0.75f, elapsed / fadeIn);
                    playerAvatarPinkHaze.color = new Color(1f, 0.35f, 0.85f, alpha);
                    yield return null;
                }

                UpdatePlayerBlush();
                if (newPhase >= 2)
                {
                    CharacterState cs = manager != null ? manager.characterState : FindFirstObjectByType<CharacterState>();
                    if (cs != null && cs.appearanceDatabase != null)
                    {
                        Sprite oSprite = FindEmotionSprite(cs.appearanceDatabase, "o");
                        if (oSprite != null)
                        {
                            playerBaseEmotion = oSprite;
                            if (playerAvatarExpression != null)
                            {
                                playerAvatarExpression.sprite = playerBaseEmotion;
                            }
                        }
                    }
                }

                float fadeOut = 0.45f;
                elapsed = 0f;
                while (elapsed < fadeOut)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0.75f, 0f, elapsed / fadeOut);
                    playerAvatarPinkHaze.color = new Color(1f, 0.35f, 0.85f, alpha);
                    yield return null;
                }
                playerAvatarPinkHaze.color = new Color(1f, 0.35f, 0.85f, 0f);
            }
            else
            {
                UpdatePlayerBlush();
            }
        }

        private static Image FindImage(GameObject root, string nameSubstr)
        {
            if (root == null) return null;
            Transform directChild = root.transform.Find(nameSubstr);
            if (directChild != null && directChild.TryGetComponent<Image>(out var directImg))
            {
                return directImg;
            }

            Image[] images = root.GetComponentsInChildren<Image>(true);
            // 1. Exact match first
            foreach (Image img in images)
            {
                if (string.Equals(img.gameObject.name, nameSubstr, StringComparison.OrdinalIgnoreCase))
                {
                    return img;
                }
            }
            // 2. Fallback substring match
            foreach (Image img in images)
            {
                if (img.gameObject.name.IndexOf(nameSubstr, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return img;
                }
            }
            return null;
        }

        private static void SyncImg(Image target, Image source)
        {
            if (target == null) return;
            if (source != null && source.sprite != null)
            {
                target.sprite = source.sprite;
                target.color = source.color;
                target.material = source.material;
                target.preserveAspect = true;
                target.enabled = source.enabled;
                target.gameObject.SetActive(source.gameObject.activeSelf && source.enabled);
            }
            else
            {
                target.sprite = null;
                target.enabled = false;
                target.gameObject.SetActive(false);
            }
        }

        private IEnumerator FlashPlayerOverlay(Color flashColor)
        {
            if (playerFlashOverlay == null) yield break;

            playerFlashOverlay.color = flashColor;
            playerFlashOverlay.gameObject.SetActive(true);

            float elapsed = 0f;
            float duration = 0.45f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(flashColor.a, 0f, elapsed / duration);
                playerFlashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
                yield return null;
            }

            playerFlashOverlay.gameObject.SetActive(false);
        }

        private void HandleCombatLog(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (combatLogText != null)
            {
                combatLogText.text = message;
            }

            AddCombatLogEntry(message);
        }

        public void AddCombatLogEntry(string message)
        {
            if (combatLogContent == null) return;

            GameObject entryObj = new GameObject("LogEntry", typeof(RectTransform));
            entryObj.transform.SetParent(combatLogContent, false);

            ContentSizeFitter csf = entryObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            TextMeshProUGUI entryTxt = entryObj.AddComponent<TextMeshProUGUI>();
            entryTxt.fontSize = 14f;
            entryTxt.lineSpacing = -2f;
            entryTxt.raycastTarget = false;

            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string formatted = $"<color=#778899>[{timestamp}]</color> ";

            if (message.IndexOf("damage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("dealt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("torn", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                formatted += $"<color=#FF7A7A>{message}</color>";
            }
            else if (message.IndexOf("seduction", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("charm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("lust", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("climax", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                formatted += $"<color=#FF80BF>{message}</color>";
            }
            else if (message.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("guard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("absorbed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                formatted += $"<color=#66D9EF>{message}</color>";
            }
            else if (message.IndexOf("activate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("used", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                formatted += $"<color=#FFE066>{message}</color>";
            }
            else
            {
                formatted += $"<color=#ECEFF4>{message}</color>";
            }

            entryTxt.text = formatted;

            if (combatLogScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                combatLogScrollRect.verticalNormalizedPosition = 0f;
            }

            if (combatLogContent.childCount > 50)
            {
                Destroy(combatLogContent.GetChild(0).gameObject);
            }
        }

        public void ToggleCombatLog()
        {
            isLogCollapsed = !isLogCollapsed;
            if (combatLogPanel != null)
            {
                RectTransform rt = combatLogPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = isLogCollapsed ? new Vector2(360f, 36f) : new Vector2(360f, 195f);
                }
            }
            if (combatLogScrollRect != null)
            {
                combatLogScrollRect.gameObject.SetActive(!isLogCollapsed);
            }
            if (combatLogToggleBtnText != null)
            {
                combatLogToggleBtnText.text = isLogCollapsed ? "^ Expand" : "v Hide";
            }
        }

        public void OnCardPointerEnter(int index)
        {
            if (isTargetingMode) return;
            focusedCardIndex = index;
            UpdateCardVisuals();
        }

        public void OnCardPointerExit(int index)
        {
            if (isTargetingMode) return;
            UpdateCardVisuals();
        }

        public void OnEndTurnPointerEnter()
        {
            if (isTargetingMode) return;
            focusedCardIndex = cardUIs.Count;
            UpdateCardVisuals();
        }

        public void OnEndTurnPointerExit()
        {
            if (isTargetingMode) return;
            UpdateCardVisuals();
        }

        public void UpdateCardVisuals()
        {
            bool isEndTurnFocused = (!isTargetingMode && focusedCardIndex == cardUIs.Count);

            for (int i = 0; i < cardUIs.Count; i++)
            {
                if (cardUIs[i] == null) continue;
                bool isSel = (selectedCardIndex == i);
                bool isFoc = (!isTargetingMode && focusedCardIndex == i);
                cardUIs[i].SetVisualState(isSel, isFoc);
            }

            if (endTurnFocusBorder != null)
            {
                endTurnFocusBorder.SetActive(isEndTurnFocused);
            }
        }

        // =========================================================================
        // KEYBOARD NAVIGATION & CARD-FIRST TARGETING
        // =========================================================================

        private void Update()
        {
            // 0. If outcome modal is showing, allow confirming with Enter or Space
            if (outcomeModal != null && outcomeModal.activeInHierarchy)
            {
                bool confirmPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
                try
                {
                    if (UnityEngine.InputSystem.Keyboard.current != null)
                    {
                        var kb = UnityEngine.InputSystem.Keyboard.current;
                        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                            confirmPressed = true;
                    }
                }
                catch {}

                if (confirmPressed)
                {
                    if (primaryOutcomeBtn != null && primaryOutcomeBtn.gameObject.activeInHierarchy && primaryOutcomeBtn.interactable)
                    {
                        primaryOutcomeBtn.onClick.Invoke();
                        return;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                {
                    if (primaryOutcomeBtn != null && primaryOutcomeBtn.gameObject.activeInHierarchy && primaryOutcomeBtn.interactable)
                    {
                        primaryOutcomeBtn.onClick.Invoke();
                        return;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                {
                    if (secondaryOutcomeBtn != null && secondaryOutcomeBtn.gameObject.activeInHierarchy && secondaryOutcomeBtn.interactable)
                    {
                        secondaryOutcomeBtn.onClick.Invoke();
                        return;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                {
                    if (tertiaryOutcomeBtn != null && tertiaryOutcomeBtn.gameObject.activeInHierarchy && tertiaryOutcomeBtn.interactable)
                    {
                        tertiaryOutcomeBtn.onClick.Invoke();
                        return;
                    }
                }
                return;
            }

            if (manager == null || !manager.isCombatActive || !manager.isPlayerTurn || manager.isActionInProgress) return;
            if (DialogueUI.Instance != null && DialogueUI.Instance.IsDialogueActive) return;

            // Ensure Unity EventSystem never retains focus on header/debug buttons while keyboard navigating
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }

            if (cardUIs == null || cardUIs.Count == 0) return;

            // 1. Cancel targeting
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isTargetingMode || selectedCardIndex >= 0)
                {
                    CancelTargeting();
                    return;
                }
            }

            // --- ONE-HANDED KEYBOARD NAVIGATION (WASD / Arrows / Space / Enter / Q / E / 1..9 / F) ---
            // Direct cycle enemies with Q / E / Tab at any time
            if (Input.GetKeyDown(KeyCode.Q))
            {
                CycleAliveEnemyTarget(-1);
            }
            else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab))
            {
                CycleAliveEnemyTarget(1);
            }

            // Direct End Turn hotkey (F key)
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (endTurnButton != null && endTurnButton.interactable)
                {
                    endTurnButton.onClick.Invoke();
                    return;
                }
            }

            // Numeric Hotkeys (1..9): direct card selection or activation
            for (int k = 0; k < 9; k++)
            {
                KeyCode alpha = KeyCode.Alpha1 + k;
                KeyCode keypad = KeyCode.Keypad1 + k;
                if (Input.GetKeyDown(alpha) || Input.GetKeyDown(keypad))
                {
                    if (k < cardUIs.Count)
                    {
                        focusedCardIndex = k;
                        UpdateCardVisuals();
                        OnCardClicked(k);
                        return;
                    }
                }
            }

            int navCount = cardUIs.Count + (endTurnButton != null ? 1 : 0);

            // 2. State Machine: Not targeting -> moving between cards and End Turn button
            if (!isTargetingMode)
            {
                if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    focusedCardIndex = (focusedCardIndex - 1 + navCount) % navCount;
                    UpdateCardVisuals();
                }
                else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                {
                    focusedCardIndex = (focusedCardIndex + 1) % navCount;
                    UpdateCardVisuals();
                }
                else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                {
                    CycleAliveEnemyTarget(1);
                }
                else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                {
                    CycleAliveEnemyTarget(-1);
                }
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    if (focusedCardIndex >= 0 && focusedCardIndex < cardUIs.Count)
                    {
                        OnCardClicked(focusedCardIndex);
                    }
                    else if (focusedCardIndex == cardUIs.Count)
                    {
                        if (endTurnButton != null && endTurnButton.interactable)
                        {
                            endTurnButton.onClick.Invoke();
                        }
                    }
                }
            }
            // 3. State Machine: In targeting mode -> moving between targets or executing
            else
            {
                if (selectedCardIndex < 0 || selectedCardIndex >= manager.playerDeck.Count)
                {
                    CancelTargeting();
                    return;
                }

                CardInstance card = manager.playerDeck[selectedCardIndex];

                if (card.data.target == CardTarget.SingleEnemy)
                {
                    if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        CycleAliveEnemyTarget(-1);
                    }
                    else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        CycleAliveEnemyTarget(1);
                    }
                    else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                    {
                        OnEnemyClicked(focusedEnemyIndex);
                    }
                }
                else if (card.data.target == CardTarget.Self)
                {
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                    {
                        OnPlayerAvatarClicked();
                    }
                }
            }

            // Animate End Turn button focus (scale & border pulse)
            if (endTurnRect != null)
            {
                bool isEndTurnFocused = (!isTargetingMode && focusedCardIndex == cardUIs.Count);
                float targetScale = isEndTurnFocused ? 1.08f : 1.0f;
                endTurnRect.localScale = Vector3.Lerp(endTurnRect.localScale, Vector3.one * targetScale, Time.deltaTime * 14f);

                if (isEndTurnFocused && endTurnFocusBorder != null)
                {
                    Image bImg = endTurnFocusBorder.GetComponent<Image>();
                    if (bImg != null)
                    {
                        float pulse = 0.70f + 0.30f * Mathf.Sin(Time.time * 8f);
                        bImg.color = new Color(1f, 0.85f, 0.25f, pulse);
                    }
                }
            }

            // Pulse player target aura & halo when targeting Self
            if (playerTargetHighlight != null && playerTargetHighlight.activeSelf)
            {
                float pa = 0.22f + 0.10f * Mathf.Sin(Time.time * 6f);
                Image[] imgs = playerTargetHighlight.GetComponentsInChildren<Image>();
                if (imgs != null && imgs.Length >= 2)
                {
                    imgs[0].color = new Color(0.35f, 0.95f, 0.55f, pa + 0.22f);
                    imgs[1].color = new Color(0.35f, 0.95f, 0.55f, pa);
                }
            }
        }

        public void OnCardClicked(int index)
        {
            if (manager == null || !manager.isCombatActive || !manager.isPlayerTurn || manager.isActionInProgress) return;
            if (index < 0 || index >= manager.playerDeck.Count) return;

            CardInstance card = manager.playerDeck[index];
            if (!card.CanPlay(manager.player.currentAp, manager.currentStance))
            {
                if (card.currentCooldown > 0)
                    HandleCombatLog($"Card '{card.data.cardName}' is on cooldown ({card.currentCooldown}t left)!");
                else if (manager.player.currentAp < card.data.apCost)
                    HandleCombatLog($"Not enough AP to play '{card.data.cardName}' (Requires {card.data.apCost} AP, have {manager.player.currentAp})!");
                else
                    HandleCombatLog($"Cannot play '{card.data.cardName}' in {manager.currentStance} stance! (Requires {card.data.stanceRequirement})");
                return;
            }

            // Clicking the already selected card
            if (selectedCardIndex == index)
            {
                if (card.data.target == CardTarget.Self)
                {
                    manager.PlayCard(index);
                    CancelTargeting();
                    return;
                }
                else
                {
                    CancelTargeting();
                    return;
                }
            }

            selectedCardIndex = index;
            focusedCardIndex = index;

            if (card.data.target == CardTarget.SingleEnemy)
            {
                isTargetingMode = true;
                int validEnemyIdx = GetFirstValidEnemyIndex(card);
                if (validEnemyIdx >= 0)
                {
                    focusedEnemyIndex = validEnemyIdx;
                }
                else if (focusedEnemyIndex < 0 || focusedEnemyIndex >= manager.enemies.Count || manager.enemies[focusedEnemyIndex].isDefeated)
                {
                    focusedEnemyIndex = GetFirstAliveEnemyIndex();
                }
                manager.SelectEnemy(focusedEnemyIndex);

                PenetrationSlot reqSlot = card.data.GetRequiredSlot();
                if (reqSlot != PenetrationSlot.None)
                {
                    Combatant occupant = manager.GetSlotOccupant(reqSlot);
                    if (occupant != null)
                        HandleCombatLog($"Selected '{card.data.cardName}'. Target: {occupant.name} in your {reqSlot.ToString().ToUpper()} [Enter/Click]!");
                    else
                        HandleCombatLog($"Selected '{card.data.cardName}'. [Notice: No opponent in your {reqSlot.ToString().ToUpper()}]");
                }
                else
                {
                    HandleCombatLog($"Selected '{card.data.cardName}'. Target enemy [A/D or click] then Enter!");
                }
                RefreshAll();
            }
            else if (card.data.target == CardTarget.Self)
            {
                isTargetingMode = true;
                HandleCombatLog($"Selected '{card.data.cardName}'. Click Heroine or Card again (or Enter) to apply!");
                RefreshAll();
            }
            else if (card.data.target == CardTarget.AllEnemies)
            {
                manager.PlayCard(index);
                CancelTargeting();
            }
        }

        public void OnEnemyClicked(int enemyIndex)
        {
            if (manager == null || !manager.isCombatActive) return;
            if (enemyIndex < 0 || enemyIndex >= manager.enemies.Count) return;

            Combatant enemy = manager.enemies[enemyIndex];
            if (enemy.isDefeated) return;

            focusedEnemyIndex = enemyIndex;
            manager.SelectEnemy(enemyIndex);

            if (isTargetingMode && selectedCardIndex >= 0)
            {
                CardInstance card = manager.playerDeck[selectedCardIndex];
                if (card.data.target == CardTarget.SingleEnemy)
                {
                    if (!card.data.IsValidTargetEnemy(enemy, manager.currentStance))
                    {
                        PenetrationSlot reqSlot = card.data.GetRequiredSlot();
                        if (reqSlot != PenetrationSlot.None)
                        {
                            Combatant occupant = manager.GetSlotOccupant(reqSlot);
                            string occupantHint = occupant != null ? $" (Currently: {occupant.name})" : " (Slot is empty)";
                            HandleCombatLog($"Invalid target! '{card.data.cardName}' requires the opponent occupying your {reqSlot.ToString().ToUpper()}{occupantHint}!");
                        }
                        else if (card.data.RequiresPenetratingTarget())
                        {
                            HandleCombatLog($"Invalid target! '{card.data.cardName}' can only target a penetrating opponent!");
                        }
                        else
                        {
                            HandleCombatLog($"Cannot use '{card.data.cardName}' on {enemy.name} in current stance!");
                        }
                        return;
                    }

                    manager.PlayCard(selectedCardIndex, enemyIndex);
                    CancelTargeting();
                    return;
                }
            }

            RefreshAll();
        }

        public void OnPlayerAvatarClicked()
        {
            if (manager == null || !manager.isCombatActive || !manager.isPlayerTurn || manager.isActionInProgress) return;

            if (isTargetingMode && selectedCardIndex >= 0)
            {
                CardInstance card = manager.playerDeck[selectedCardIndex];
                if (card.data.target == CardTarget.Self)
                {
                    manager.PlayCard(selectedCardIndex);
                    CancelTargeting();
                }
            }
        }

        public void CancelTargeting()
        {
            selectedCardIndex = -1;
            isTargetingMode = false;
            RefreshAll();
        }

        private void CycleAliveEnemyTarget(int dir)
        {
            if (manager == null || manager.enemies == null || manager.enemies.Count == 0) return;

            int count = manager.enemies.Count;
            int next = focusedEnemyIndex;
            CardInstance card = (isTargetingMode && selectedCardIndex >= 0 && selectedCardIndex < manager.playerDeck.Count)
                ? manager.playerDeck[selectedCardIndex] : null;

            bool filterSlot = card != null && card.data.GetRequiredSlot() != PenetrationSlot.None && HasAnyValidEnemy(card);

            for (int i = 0; i < count; i++)
            {
                next = (next + dir + count) % count;
                Combatant cand = manager.enemies[next];
                if (cand != null && !cand.isDefeated)
                {
                    if (filterSlot && !card.data.IsValidTargetEnemy(cand, manager.currentStance))
                    {
                        continue;
                    }

                    focusedEnemyIndex = next;
                    manager.SelectEnemy(focusedEnemyIndex);
                    RefreshAll();
                    return;
                }
            }
        }

        private int GetFirstValidEnemyIndex(CardInstance card)
        {
            if (manager == null || manager.enemies == null || card == null) return -1;
            for (int i = 0; i < manager.enemies.Count; i++)
            {
                Combatant e = manager.enemies[i];
                if (e != null && !e.isDefeated && card.data.IsValidTargetEnemy(e, manager.currentStance))
                {
                    return i;
                }
            }
            return -1;
        }

        private bool HasAnyValidEnemy(CardInstance card)
        {
            return GetFirstValidEnemyIndex(card) >= 0;
        }

        private int GetFirstAliveEnemyIndex()
        {
            if (manager == null || manager.enemies == null) return 0;
            for (int i = 0; i < manager.enemies.Count; i++)
            {
                if (!manager.enemies[i].isDefeated) return i;
            }
            return 0;
        }

        // =========================================================================
        // PROCEDURAL COMBAT VFX
        // =========================================================================

        public void PlaySlashVFX(Vector3 worldPos)
        {
            if (combatPanel == null) return;
            StartCoroutine(SlashVFXRoutine(worldPos));
        }

        private IEnumerator SlashVFXRoutine(Vector3 worldPos)
        {
            GameObject slashObj = new GameObject("VFX_Slash", typeof(RectTransform));
            slashObj.transform.SetParent(combatPanel.transform, false);
            slashObj.transform.position = worldPos;
            slashObj.transform.SetAsLastSibling();

            RectTransform srt = slashObj.GetComponent<RectTransform>();
            srt.sizeDelta = new Vector2(240f, 16f);
            srt.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-35f, -55f));

            Image img = slashObj.AddComponent<Image>();
            img.color = new Color(1f, 0.85f, 0.40f, 0.95f);
            img.raycastTarget = false;

            float dur = 0.20f;
            float elapsed = 0f;
            Vector3 initScale = new Vector3(0.2f, 2.5f, 1f);
            Vector3 endScale = new Vector3(1.4f, 0.2f, 1f);

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                srt.localScale = Vector3.Lerp(initScale, endScale, t);
                img.color = new Color(1f, Mathf.Lerp(0.9f, 0.3f, t), 0.3f, 1f - t);
                yield return null;
            }
            Destroy(slashObj);
        }

        public void PlayHeartsVFX(Vector3 worldPos)
        {
            if (combatPanel == null) return;
            StartCoroutine(HeartsVFXRoutine(worldPos));
        }

        private IEnumerator HeartsVFXRoutine(Vector3 worldPos)
        {
            int count = 6;
            List<GameObject> hearts = new List<GameObject>();
            List<Vector2> velocities = new List<Vector2>();

            for (int i = 0; i < count; i++)
            {
                GameObject hObj = new GameObject("VFX_Heart", typeof(RectTransform));
                hObj.transform.SetParent(combatPanel.transform, false);
                hObj.transform.position = worldPos + new Vector3(UnityEngine.Random.Range(-25f, 25f), UnityEngine.Random.Range(-20f, 20f), 0f);
                hObj.transform.SetAsLastSibling();

                TextMeshProUGUI txt = hObj.AddComponent<TextMeshProUGUI>();
                txt.text = "<color=#FF66AA>*</color>";
                txt.fontSize = UnityEngine.Random.Range(24, 38);
                txt.color = new Color(1f, UnityEngine.Random.Range(0.3f, 0.55f), UnityEngine.Random.Range(0.7f, 0.95f), 1f);
                txt.alignment = TextAlignmentOptions.Center;
                txt.raycastTarget = false;

                hearts.Add(hObj);
                velocities.Add(new Vector2(UnityEngine.Random.Range(-70f, 70f), UnityEngine.Random.Range(90f, 160f)));
            }

            float dur = 0.55f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;

                for (int i = 0; i < hearts.Count; i++)
                {
                    if (hearts[i] != null)
                    {
                        hearts[i].transform.position += (Vector3)(velocities[i] * Time.deltaTime);
                        TextMeshProUGUI txt = hearts[i].GetComponent<TextMeshProUGUI>();
                        if (txt != null)
                        {
                            Color c = txt.color;
                            c.a = 1f - t;
                            txt.color = c;
                        }
                    }
                }
                yield return null;
            }

            for (int i = 0; i < hearts.Count; i++)
            {
                if (hearts[i] != null) Destroy(hearts[i]);
            }
        }

        public void PlayBarrierVFX(Vector3 worldPos)
        {
            if (combatPanel == null) return;
            StartCoroutine(BarrierVFXRoutine(worldPos));
        }

        private IEnumerator BarrierVFXRoutine(Vector3 worldPos)
        {
            GameObject barObj = new GameObject("VFX_Barrier", typeof(RectTransform));
            barObj.transform.SetParent(combatPanel.transform, false);
            barObj.transform.position = worldPos;
            barObj.transform.SetAsLastSibling();

            RectTransform brt = barObj.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(160f, 160f);

            Image img = barObj.AddComponent<Image>();
            img.color = new Color(0.35f, 0.85f, 1f, 0.75f);
            img.raycastTarget = false;

            float dur = 0.35f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                brt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.4f, t);
                img.color = new Color(0.35f, 0.85f, 1f, Mathf.Lerp(0.85f, 0f, t));
                yield return null;
            }
            Destroy(barObj);
        }

        // =========================================================================
        // TURN ORDER TIMELINE TRACKER
        // =========================================================================

        private void BuildTurnOrderBar()
        {
            if (turnOrderContainer == null || manager == null) return;

            foreach (Transform child in turnOrderContainer)
            {
                Destroy(child.gameObject);
            }
            turnBadges.Clear();

            // 1. Player badge (Order 1)
            GameObject playerBadgeObj = CreateTurnBadge(turnOrderContainer, 1, manager.player);
            TurnOrderBadgeUI pBadge = playerBadgeObj.GetComponent<TurnOrderBadgeUI>();
            turnBadges.Add(pBadge);

            // 2. Enemy badges (Order 2, 3, 4, etc.)
            for (int i = 0; i < manager.enemies.Count; i++)
            {
                GameObject eBadgeObj = CreateTurnBadge(turnOrderContainer, i + 2, manager.enemies[i]);
                TurnOrderBadgeUI eBadge = eBadgeObj.GetComponent<TurnOrderBadgeUI>();
                turnBadges.Add(eBadge);
            }

            // Start of combat: Player is active
            UpdateTurnOrderActive(manager.player);
        }

        public static Sprite GetCombatantTurnIcon(Combatant combatant)
        {
            if (combatant == null || combatant.isPlayer) return null;

            if (CombatManager.Instance != null)
            {
                var def = CombatManager.Instance.GetEnemyDefinition(combatant.enemyType);
                if (def != null && def.enemyIcon != null) return def.enemyIcon;
            }

            string lower = combatant.name.ToLower();
            if (combatant.enemyType == EnemyType.SlimeGirl || lower.Contains("slime"))
            {
                return StatIconDatabase.LoadIcon("Icon_slime0.png");
            }
            else if (combatant.enemyType == EnemyType.Succubus || lower.Contains("succub"))
            {
                if (lower.Contains("1") || lower.Contains("b") || lower.Contains("two") || lower.Contains("2"))
                {
                    Sprite s1 = StatIconDatabase.LoadIcon("Icon_succub1.png");
                    if (s1 != null) return s1;
                }
                return StatIconDatabase.LoadIcon("Icon_succub0.png");
            }
            return null;
        }

        private GameObject CreateTurnBadge(Transform parent, int orderNumber, Combatant combatant)
        {
            GameObject badgeObj = new GameObject($"TurnBadge_{combatant.name}", typeof(RectTransform));
            badgeObj.transform.SetParent(parent, false);

            RectTransform rt = badgeObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(52f, 52f);

            CanvasGroup cg = badgeObj.AddComponent<CanvasGroup>();

            // 1. Active Glowing Highlight Border
            GameObject borderObj = new GameObject("ActiveBorder", typeof(RectTransform));
            borderObj.transform.SetParent(badgeObj.transform, false);
            Image border = borderObj.AddComponent<Image>();
            border.color = new Color(1f, 0.88f, 0.30f, 1f);
            border.raycastTarget = false;
            RectTransform brt = borderObj.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(-3.5f, -3.5f);
            brt.offsetMax = new Vector2(3.5f, 3.5f);
            borderObj.SetActive(false);

            // 2. Permanent Resting Border (Small antique gold / bronze frame wrapping the icon)
            GameObject restingBorderObj = new GameObject("RestingBorder", typeof(RectTransform));
            restingBorderObj.transform.SetParent(badgeObj.transform, false);
            Image restingBorder = restingBorderObj.AddComponent<Image>();
            restingBorder.color = new Color(0.68f, 0.54f, 0.28f, 0.95f);
            restingBorder.raycastTarget = false;
            RectTransform rbrt = restingBorderObj.GetComponent<RectTransform>();
            rbrt.anchorMin = Vector2.zero;
            rbrt.anchorMax = Vector2.one;
            rbrt.offsetMin = new Vector2(-2f, -2f);
            rbrt.offsetMax = new Vector2(2f, 2f);

            // 3. Dark Inner Background (renders over resting border so transparent icons show dark background)
            GameObject bgObj = new GameObject("Background", typeof(RectTransform));
            bgObj.transform.SetParent(badgeObj.transform, false);
            Image bg = bgObj.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.09f, 0.13f, 0.96f);
            bg.raycastTarget = false;
            RectTransform bgrt = bgObj.GetComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero;
            bgrt.anchorMax = Vector2.one;
            bgrt.offsetMin = Vector2.zero;
            bgrt.offsetMax = Vector2.zero;

            // 4. Enemy Icon Image
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(badgeObj.transform, false);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            RectTransform irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(4f, 4f);
            irt.offsetMax = new Vector2(-4f, -4f);

            Sprite enemyIcon = GetCombatantTurnIcon(combatant);
            if (enemyIcon != null)
            {
                iconImg.sprite = enemyIcon;
                iconImg.color = Color.white;
                iconObj.SetActive(true);
            }
            else
            {
                iconObj.SetActive(false);
            }

            // 5. Text Label (Used for player [YOU] or fallback text)
            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(badgeObj.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = combatant.isPlayer ? "YOU" : combatant.name.Substring(0, Math.Min(3, combatant.name.Length)).ToUpper();
            txt.fontSize = 13;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = combatant.isPlayer ? new Color(1f, 0.88f, 0.35f, 1f) : Color.white;
            txt.raycastTarget = false;
            RectTransform tr = txtObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;
            txtObj.SetActive(enemyIcon == null);

            // 6. Corner Turn Order Pill (Bottom-Right)
            GameObject orderPill = new GameObject("OrderNumber", typeof(RectTransform));
            orderPill.transform.SetParent(badgeObj.transform, false);
            Image pillBg = orderPill.AddComponent<Image>();
            pillBg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
            pillBg.raycastTarget = false;
            RectTransform prt = orderPill.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(1f, 0f);
            prt.anchorMax = new Vector2(1f, 0f);
            prt.pivot = new Vector2(1f, 0f);
            prt.anchoredPosition = new Vector2(2f, -2f);
            prt.sizeDelta = new Vector2(18f, 18f);

            GameObject orderTxtObj = new GameObject("Text", typeof(RectTransform));
            orderTxtObj.transform.SetParent(orderPill.transform, false);
            TextMeshProUGUI orderTxt = orderTxtObj.AddComponent<TextMeshProUGUI>();
            orderTxt.text = $"{orderNumber}";
            orderTxt.fontSize = 11;
            orderTxt.fontStyle = FontStyles.Bold;
            orderTxt.alignment = TextAlignmentOptions.Center;
            orderTxt.color = new Color(1f, 0.88f, 0.35f, 1f);
            orderTxt.raycastTarget = false;
            RectTransform ort = orderTxtObj.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero;
            ort.anchorMax = Vector2.one;
            ort.sizeDelta = Vector2.zero;

            // 7. Tooltip on Hover
            StatTooltipTrigger tt = badgeObj.AddComponent<StatTooltipTrigger>();
            tt.tooltipText = $"{orderNumber}. {(combatant.isPlayer ? "Your Turn" : combatant.name)}";

            TurnOrderBadgeUI badge = badgeObj.AddComponent<TurnOrderBadgeUI>();
            badge.combatant = combatant;
            badge.background = bg;
            badge.restingBorder = restingBorderObj;
            badge.border = borderObj;
            badge.iconImage = iconImg;
            badge.label = txt;
            badge.orderNumberText = orderTxt;
            badge.canvasGroup = cg;
            return badgeObj;
        }

        private void ResetTurnOrderRound()
        {
            foreach (var b in turnBadges)
            {
                if (b == null) continue;
                if (b.combatant != null && b.combatant.isDefeated)
                {
                    b.SetDefeated();
                }
                else
                {
                    b.SetWaiting();
                }
            }

            if (manager != null && manager.player != null)
            {
                UpdateTurnOrderActive(manager.player);
            }
        }

        private void UpdateTurnOrderActive(Combatant activeCombatant)
        {
            foreach (var b in turnBadges)
            {
                if (b == null) continue;
                if (b.combatant == activeCombatant)
                {
                    b.SetActiveTurn();
                }
            }
        }

        private void UpdateTurnOrderEnded(Combatant endedCombatant)
        {
            foreach (var b in turnBadges)
            {
                if (b == null) continue;
                if (b.combatant == endedCombatant)
                {
                    b.SetEnded();
                }
            }
        }

        // =========================================================================
        // REFRESH & STATE UPDATES
        // =========================================================================

        public void RefreshAll()
        {
            if (manager == null || manager.player == null) return;

            UpdateStanceIndicators(manager.currentStance);

            Combatant p = manager.player;

            // 1. Top-Left Player Status HUD values
            if (playerHpText != null)
            {
                playerHpText.text = $"{p.currentHp} / {p.maxHp} HP";
                if (playerHpBar != null) playerHpBar.fillAmount = (float)p.currentHp / Mathf.Max(1, p.maxHp);
            }

            if (playerUpperClothesText != null)
            {
                if (p.upperClothesHp > 0)
                    playerUpperClothesText.text = $"{p.upperClothesHp} / {p.maxUpperClothesHp}";
                else
                    playerUpperClothesText.text = $"0 / {p.maxUpperClothesHp} [Torn!]";
                playerUpperClothesText.color = (p.upperClothesHp > 0) ? new Color(0.40f, 0.80f, 1f, 1f) : new Color(0.85f, 0.35f, 0.35f, 0.9f);
            }

            if (playerLowerClothesText != null)
            {
                if (p.lowerClothesHp > 0)
                    playerLowerClothesText.text = $"{p.lowerClothesHp} / {p.maxLowerClothesHp}";
                else
                    playerLowerClothesText.text = $"0 / {p.maxLowerClothesHp} [Torn!]";
                playerLowerClothesText.color = (p.lowerClothesHp > 0) ? new Color(0.40f, 0.80f, 1f, 1f) : new Color(0.85f, 0.35f, 0.35f, 0.9f);
            }

            if (playerClothesText != null && playerClothesText != playerUpperClothesText)
            {
                playerClothesText.text = $"{p.clothesHp} / {p.maxClothesHp}";
                if (playerClothesBar != null) playerClothesBar.fillAmount = (float)p.clothesHp / Mathf.Max(1, p.maxClothesHp);
            }

            if (playerShieldText != null)
            {
                if (p.lustShield > 0)
                {
                    playerShieldText.text = (p.shield > 0) ? $"+{p.shield} (Lust Shield: +{p.lustShield})" : $"Lust Shield: +{p.lustShield}";
                    playerShieldText.color = new Color(1f, 0.45f, 0.85f, 1f);
                }
                else
                {
                    playerShieldText.text = (p.shield > 0) ? $"+{p.shield}" : "0";
                    playerShieldText.color = (p.shield > 0) ? new Color(0.40f, 0.90f, 1f, 1f) : new Color(0.65f, 0.75f, 0.85f, 0.6f);
                }
            }

            if (playerSeductionText != null)
            {
                playerSeductionText.text = $"{p.seduction}%  ({p.seductionStacks}/3)";
                if (playerSeductionBar != null) playerSeductionBar.fillAmount = (float)p.seduction / 100f;
            }

            if (playerApText != null)
            {
                playerApText.text = $"{p.currentAp} / {p.maxAp}";
            }

            if (playerCorruptionText != null)
            {
                int corr = manager != null ? manager.persistentCorruption : 0;
                int maxC = manager != null ? manager.maxCorruption : 3;
                playerCorruptionText.text = $"{corr} / {maxC}";
                playerCorruptionText.color = (corr > 0) ? new Color(0.95f, 0.40f, 0.85f, 1f) : new Color(0.70f, 0.75f, 0.85f, 0.8f);
            }

            if (playerStatusText != null)
            {
                string stanceStr = manager != null ? $"[{manager.currentStance.ToString().ToUpper()}]" : "[NEUTRAL]";
                string cumStr = (p.semenVolumeMl > 0) ? $" [Cum: {p.semenVolumeMl}ml]" : "";
                string flusterStr = p.isFlustered ? " [Flustered]" : "";
                string pureStr = (manager != null && manager.isPureRunActive) ? " <color=#55EEFF>[PURE]</color>" : " <color=#778899>[TAINTED]</color>";
                playerStatusText.text = $"{stanceStr}{flusterStr}{cumStr}{pureStr}";
                playerStatusText.color = (manager != null && manager.currentStance != CombatStance.Neutral) 
                    ? new Color(1f, 0.45f, 0.80f, 1f) 
                    : (p.isFlustered ? new Color(1f, 0.55f, 0.75f, 1f) : new Color(0.7f, 0.8f, 0.9f, 0.8f));
            }

            // Target highlight on player avatar (when Self card is selected)
            if (playerTargetHighlight != null)
            {
                bool isSelfTarget = isTargetingMode && selectedCardIndex >= 0 &&
                                    selectedCardIndex < manager.playerDeck.Count &&
                                    manager.playerDeck[selectedCardIndex].data.target == CardTarget.Self;
                playerTargetHighlight.SetActive(isSelfTarget);
            }

            // End Turn Button interactability
            if (endTurnButton != null)
            {
                endTurnButton.interactable = manager.isPlayerTurn && !manager.isActionInProgress;
            }

            // Refresh Enemy Cards (Only highlight enemies if a SingleEnemy card is selected!)
            bool isEnemyTargeting = isTargetingMode && selectedCardIndex >= 0 &&
                                    selectedCardIndex < manager.playerDeck.Count &&
                                    manager.playerDeck[selectedCardIndex].data.target == CardTarget.SingleEnemy;

            bool isAnyEnemyTurnActive = manager != null && !manager.isPlayerTurn && manager.currentTurnCombatant != null && !manager.currentTurnCombatant.isPlayer;

            for (int i = 0; i < enemyCards.Count; i++)
            {
                if (i < manager.enemies.Count)
                {
                    Combatant enemy = manager.enemies[i];
                    bool isTargeted = isEnemyTargeting && (i == focusedEnemyIndex);
                    bool isActiveTurn = isAnyEnemyTurnActive && (manager.currentTurnCombatant == enemy);
                    enemyCards[i].Refresh(enemy, isTargeted, isEnemyTargeting, isActiveTurn, isAnyEnemyTurnActive);
                }
            }

            // Refresh Cards Hand
            for (int i = 0; i < cardUIs.Count; i++)
            {
                if (i < manager.playerDeck.Count)
                {
                    bool isSel = (selectedCardIndex == i);
                    bool isFoc = (!isTargetingMode && focusedCardIndex == i);
                    cardUIs[i].Refresh(manager.playerDeck[i], manager.player.currentAp, manager.isPlayerTurn, manager.isActionInProgress, manager.currentStance, isSel, isFoc);
                }
            }

            // Sync player combat doll layers
            if (p != null)
            {
                bool showUpper = (p.upperClothesHp > 0 && playerAvatarClothes != null && playerAvatarClothes.sprite != null);
                if (playerAvatarClothes != null)
                {
                    playerAvatarClothes.enabled = showUpper;
                    playerAvatarClothes.gameObject.SetActive(showUpper);
                }

                bool showLower = (p.lowerClothesHp > 0);
                if (playerAvatarLowerClothes != null)
                {
                    bool active = showLower && playerAvatarLowerClothes.sprite != null;
                    playerAvatarLowerClothes.enabled = active;
                    playerAvatarLowerClothes.gameObject.SetActive(active);
                }
                if (playerAvatarUnderwear != null)
                {
                    bool active = showLower && playerAvatarUnderwear.sprite != null;
                    playerAvatarUnderwear.enabled = active;
                    playerAvatarUnderwear.gameObject.SetActive(active);
                }

                if (playerAvatarGroin != null)
                {
                    bool lowerCovered = (playerAvatarUnderwear != null && playerAvatarUnderwear.enabled && playerAvatarUnderwear.sprite != null) ||
                                        (playerAvatarLowerClothes != null && playerAvatarLowerClothes.enabled && playerAvatarLowerClothes.sprite != null);
                    playerAvatarGroin.enabled = !lowerCovered && (playerAvatarGroin.sprite != null);
                    playerAvatarGroin.gameObject.SetActive(playerAvatarGroin.enabled);
                }

                if (playerAvatarBreast != null && playerAvatarBreast.sprite != null)
                {
                    bool hideBreast = showUpper && (p.upperItem != null && p.upperItem.hidesBreastLayer);
                    playerAvatarBreast.enabled = !hideBreast;
                    playerAvatarBreast.gameObject.SetActive(!hideBreast);
                }
            }
            UpdatePlayerBlush();
        }

        private void BuildEnemyCards()
        {
            if (enemiesContainer == null) return;
            foreach (Transform child in enemiesContainer)
            {
                Destroy(child.gameObject);
            }
            enemyCards.Clear();

            for (int i = 0; i < manager.enemies.Count; i++)
            {
                int index = i;
                Combatant enemy = manager.enemies[i];
                GameObject cardObj = CreateEnemyCardObject(enemiesContainer, enemy, () =>
                {
                    OnEnemyClicked(index);
                });

                RectTransform crt = cardObj.GetComponent<RectTransform>();
                if (crt != null)
                {
                    crt.anchoredPosition = GetEnemySnakePosition(i, manager.enemies.Count);
                }
                enemyCards.Add(cardObj.GetComponent<EnemyCardUI>());
            }

            SortEnemySiblings();
        }

        private void BuildPlayerCards()
        {
            if (cardsContainer == null) return;
            foreach (Transform child in cardsContainer)
            {
                Destroy(child.gameObject);
            }
            cardUIs.Clear();

            for (int i = 0; i < manager.playerDeck.Count; i++)
            {
                int index = i;
                GameObject cardObj = CreateCombatCardObject(cardsContainer, manager.playerDeck[i], index, () =>
                {
                    OnCardClicked(index);
                });
                cardUIs.Add(cardObj.GetComponent<CombatCardUI>());
            }
        }

        private void HandleStanceChanged(CombatStance stance)
        {
            BuildPlayerCards();
            UpdateStanceIndicators(stance);
            RefreshAll();
        }

        public void UpdateStanceIndicators(CombatStance currentStance)
        {
            if (stanceSquareObjects == null || stanceSquareObjects.Length < 3) return;

            // 0: Neutral, 1: Grappled, 2: Penetrated (also covers ThroatLock)
            int activeIndex = 0;
            if (currentStance == CombatStance.Grappled) activeIndex = 1;
            else if (currentStance == CombatStance.Penetrated || currentStance == CombatStance.ThroatLock) activeIndex = 2;

            Color[] activeColors = new Color[]
            {
                new Color(0.95f, 0.75f, 0.20f, 1f), // Gold for Neutral
                new Color(1f, 0.55f, 0.15f, 1f),    // Amber for Grappled
                new Color(1f, 0.25f, 0.65f, 1f)     // Radiant Hot Pink for Penetrated
            };

            for (int i = 0; i < 3; i++)
            {
                GameObject sqObj = stanceSquareObjects[i];
                if (sqObj == null) continue;

                bool isActive = (i == activeIndex);

                // Border Highlight
                if (stanceBorderImages != null && i < stanceBorderImages.Length && stanceBorderImages[i] != null)
                {
                    stanceBorderImages[i].color = isActive 
                        ? activeColors[i] 
                        : new Color(0.25f, 0.28f, 0.35f, 0.45f);
                }

                // Inner Background Tint
                if (stanceBackgroundImages != null && i < stanceBackgroundImages.Length && stanceBackgroundImages[i] != null)
                {
                    stanceBackgroundImages[i].color = isActive 
                        ? new Color(0.12f, 0.14f, 0.20f, 0.98f) 
                        : new Color(0.06f, 0.07f, 0.10f, 0.85f);
                }

                // Scale & CanvasGroup Alpha
                sqObj.transform.localScale = isActive ? Vector3.one * 1.08f : Vector3.one * 0.95f;
                CanvasGroup cg = sqObj.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = isActive ? 1.0f : 0.45f;

                // SubLabel Text Highlight
                if (stanceNameLabels != null && i < stanceNameLabels.Length && stanceNameLabels[i] != null)
                {
                    stanceNameLabels[i].color = isActive ? activeColors[i] : new Color(0.55f, 0.58f, 0.65f, 0.6f);
                    stanceNameLabels[i].fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
                }
            }

            // Sync custom sprites to icon image slots if assigned
            if (neutralStanceIcon != null)
            {
                if (neutralStanceSprite != null && neutralStanceIcon.sprite != neutralStanceSprite)
                    neutralStanceIcon.sprite = neutralStanceSprite;
                bool hasSprite = neutralStanceIcon.sprite != null;
                neutralStanceIcon.enabled = hasSprite;
                if (stancePlaceholderLabels != null && stancePlaceholderLabels.Length > 0 && stancePlaceholderLabels[0] != null)
                    stancePlaceholderLabels[0].gameObject.SetActive(!hasSprite);
            }

            if (grappledStanceIcon != null)
            {
                if (grappledStanceSprite != null && grappledStanceIcon.sprite != grappledStanceSprite)
                    grappledStanceIcon.sprite = grappledStanceSprite;
                bool hasSprite = grappledStanceIcon.sprite != null;
                grappledStanceIcon.enabled = hasSprite;
                if (stancePlaceholderLabels != null && stancePlaceholderLabels.Length > 1 && stancePlaceholderLabels[1] != null)
                    stancePlaceholderLabels[1].gameObject.SetActive(!hasSprite);
            }

            if (penetratedStanceIcon != null)
            {
                if (penetratedStanceSprite != null && penetratedStanceIcon.sprite != penetratedStanceSprite)
                    penetratedStanceIcon.sprite = penetratedStanceSprite;
                bool hasSprite = penetratedStanceIcon.sprite != null;
                penetratedStanceIcon.enabled = hasSprite;
                if (stancePlaceholderLabels != null && stancePlaceholderLabels.Length > 2 && stancePlaceholderLabels[2] != null)
                    stancePlaceholderLabels[2].gameObject.SetActive(!hasSprite);
            }
        }

        private void HandleHandUpdated()
        {
            BuildPlayerCards();
            RefreshAll();
        }

        private void HandleCombatEnded(CombatResult result)
        {
            if (outcomeModal == null) return;

            // Deselect and reset all cards completely
            selectedCardIndex = -1;
            focusedCardIndex = -1;
            isTargetingMode = false;
            if (playerTargetHighlight != null) playerTargetHighlight.SetActive(false);
            if (endTurnFocusBorder != null) endTurnFocusBorder.SetActive(false);

            if (cardUIs != null)
            {
                for (int i = 0; i < cardUIs.Count; i++)
                {
                    if (cardUIs[i] != null)
                    {
                        cardUIs[i].SetVisualState(false, false);
                        if (cardUIs[i].visualRoot != null)
                        {
                            Vector2 pos = cardUIs[i].visualRoot.anchoredPosition;
                            pos.y = 0f;
                            cardUIs[i].visualRoot.anchoredPosition = pos;
                            cardUIs[i].visualRoot.localScale = Vector3.one;
                        }
                    }
                }
            }

            // Hide cards and interaction panels, but keep playerStatsPanel visible!
            if (cardsContainer != null) cardsContainer.gameObject.SetActive(false);
            if (endTurnButton != null) endTurnButton.gameObject.SetActive(false);
            if (playerStatsPanel != null) playerStatsPanel.SetActive(true);
            if (combatLogPanel != null) combatLogPanel.SetActive(false);

            Combatant primaryEnemy = result.lastSeducedEnemy;
            if (primaryEnemy == null && manager != null && manager.enemies != null && manager.enemies.Count > 0)
            {
                primaryEnemy = manager.enemies[0];
            }

            if (DialogueUI.Instance != null && DialogueLibrary.HasPostCombatDialogue(primaryEnemy, result))
            {
                DialogueSequence sequence = DialogueLibrary.GetPostCombatDialogue(primaryEnemy, result, manager?.player);
                if (sequence != null)
                {
                    DialogueUI.Instance.PlayDialogue(sequence, onComplete: () => ShowOutcomeModal(result));
                    return;
                }
            }

            ShowOutcomeModal(result);
        }

        public void RefreshOutcomePranaDisplay()
        {
            if (outcomePranaText != null && FleshTransformationManager.Instance != null)
            {
                FleshTransformationManager mgr = FleshTransformationManager.Instance;
                string status = mgr.IsTransformationPending ? "<color=#FF66CC>[TRANSFORMATION READY]</color>" : "<color=#88AAEE>[ACCUMULATING]</color>";
                outcomePranaText.text = $"Semen Essence: <color=#FFAAEE>{mgr.currentPranaMl} / {mgr.thresholdMl} ml</color> {status}";
            }
        }

        private void FinishOutcomeWithTransformationCheck()
        {
            if (outcomeModal != null) outcomeModal.SetActive(false);
            Hide();

            if (FleshTransformationManager.Instance != null && FleshTransformationManager.Instance.IsTransformationPending)
            {
                if (PostCombatTransitionUI.Instance != null)
                {
                    PostCombatTransitionUI.Instance.ShowTransformation(() =>
                    {
                        OnCombatVictoryFinished?.Invoke();
                    });
                    return;
                }
            }

            OnCombatVictoryFinished?.Invoke();
        }

        private void ShowOutcomeModal(CombatResult result)
        {
            if (outcomeModal == null) return;

            Canvas c = outcomeModal.GetComponent<Canvas>();
            if (c == null) c = outcomeModal.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 200;
            if (outcomeModal.GetComponent<GraphicRaycaster>() == null) outcomeModal.AddComponent<GraphicRaycaster>();

            outcomeModal.SetActive(true);
            outcomeModal.transform.SetAsLastSibling();

            RefreshOutcomePranaDisplay();

            if (outcomeDebugFillGaugeBtn != null)
            {
                outcomeDebugFillGaugeBtn.onClick.RemoveAllListeners();
                outcomeDebugFillGaugeBtn.onClick.AddListener(() =>
                {
                    FleshTransformationManager.Instance?.DebugFillGauge();
                    RefreshOutcomePranaDisplay();
                });
            }

            primaryOutcomeBtn.onClick.RemoveAllListeners();
            secondaryOutcomeBtn.onClick.RemoveAllListeners();
            tertiaryOutcomeBtn.onClick.RemoveAllListeners();

            switch (result.type)
            {
                case CombatResultType.VictoryHp:
                    outcomeTitleText.text = "MARTIAL VICTORY: ENEMIES ROUTED";
                    int hpLumi = result.lumieresEarned > 0 ? result.lumieresEarned : 25;
                    outcomeDescText.text = $"The foes were physically subdued and fled deeper into the catacombs!\nYou secured <color=#FFDD55>+{hpLumi} Lumieres</color> and <color=#FF88EE>+{result.lustidEarned} Lustid</color>.";
                    primaryOutcomeBtn.gameObject.SetActive(true);
                    primaryOutcomeBtnText.text = "Collect Spoils & Proceed";
                    secondaryOutcomeBtn.gameObject.SetActive(false);
                    tertiaryOutcomeBtn.gameObject.SetActive(false);
                    primaryOutcomeBtn.onClick.AddListener(() =>
                    {
                        CurrencyManager.Instance?.AddLumieres(hpLumi);
                        FinishOutcomeWithTransformationCheck();
                    });
                    break;

                case CombatResultType.VictorySeduction:
                    string foeName = result.lastSeducedEnemy != null ? result.lastSeducedEnemy.name : "The foe";
                    if (result.isHybrid)
                    {
                        outcomeTitleText.text = "TACTICAL HYBRID VICTORY";
                        outcomeDescText.text = $"{result.seducedEnemiesCount} opponent(s) submitted in ecstatic thrall, while {result.defeatedHpEnemiesCount} were physically routed!\nSecured <color=#FF88EE>+{result.lustidEarned} Lustid</color>.\nChoose how to conclude the encounter with {foeName}:";
                    }
                    else
                    {
                        outcomeTitleText.text = (result.totalEnemies > 1) ? "OVERSTIMULATION VICTORY: PACK SUBMITTED" : "OVERSTIMULATION VICTORY";
                        outcomeDescText.text = (result.totalEnemies > 1)
                            ? $"All {result.totalEnemies} opponents have completely collapsed under your lustful thrall!\nSecured <color=#FF88EE>+{result.lustidEarned} Lustid</color>.\nChoose how to conclude the encounter with {foeName}:"
                            : $"{foeName} has fallen completely under your lustful thrall (3 Climax Stacks reached)!\nSecured <color=#FF88EE>+{result.lustidEarned} Lustid</color>.\nChoose how to conclude the encounter:";
                    }

                    // Choice 1: Leave
                    primaryOutcomeBtn.gameObject.SetActive(true);
                    primaryOutcomeBtnText.text = "[ Leave ] Let Her Go (+10 Lumieres)";
                    primaryOutcomeBtn.onClick.AddListener(() =>
                    {
                        CurrencyManager.Instance?.AddLumieres(10);
                        FinishOutcomeWithTransformationCheck();
                    });

                    // Choice 2: Sex
                    secondaryOutcomeBtn.gameObject.SetActive(true);
                    secondaryOutcomeBtnText.text = "[ Sex ] Indulge in Passion (+35 Lumieres)";
                    secondaryOutcomeBtn.onClick.AddListener(() =>
                    {
                        outcomeTitleText.text = "INTIMATE PASSION";
                        outcomeDescText.text = $"You took tender pleasure with {foeName} amid the subterranean alcove.\nBoth of you depart completely satisfied and replenished.\nAcquired: <color=#FFDD55>+35 Lumieres</color>!";
                        primaryOutcomeBtn.gameObject.SetActive(true);
                        primaryOutcomeBtnText.text = "Finish & Continue";
                        secondaryOutcomeBtn.gameObject.SetActive(false);
                        tertiaryOutcomeBtn.gameObject.SetActive(false);
                        primaryOutcomeBtn.onClick.RemoveAllListeners();
                        primaryOutcomeBtn.onClick.AddListener(() =>
                        {
                            CurrencyManager.Instance?.AddLumieres(35);
                            FleshTransformationManager.Instance?.AddPrana(50, foeName, isInternal: true);
                            FinishOutcomeWithTransformationCheck();
                        });
                    });

                    // Choice 3: Rob
                    tertiaryOutcomeBtn.gameObject.SetActive(true);
                    tertiaryOutcomeBtnText.text = "[ Rob ] Steal Belongings (+45 Lumieres)";
                    tertiaryOutcomeBtn.onClick.AddListener(() =>
                    {
                        outcomeTitleText.text = "ROBBED ENEMY";
                        outcomeDescText.text = $"You took supplies and <color=#FFDD55>+45 Lumieres</color> from the dazed {foeName} before leaving.";
                        primaryOutcomeBtn.gameObject.SetActive(true);
                        primaryOutcomeBtnText.text = "Continue";
                        secondaryOutcomeBtn.gameObject.SetActive(false);
                        tertiaryOutcomeBtn.gameObject.SetActive(false);
                        primaryOutcomeBtn.onClick.RemoveAllListeners();
                        primaryOutcomeBtn.onClick.AddListener(() =>
                        {
                            CurrencyManager.Instance?.AddLumieres(45);
                            FinishOutcomeWithTransformationCheck();
                        });
                    });
                    break;

                case CombatResultType.CorruptionSurvival:
                    int curC = manager != null ? manager.persistentCorruption : 1;
                    int maxCorr = manager != null ? manager.maxCorruption : 3;
                    outcomeTitleText.text = $"CORRUPTED SURVIVAL ({curC}/{maxCorr})";
                    outcomeDescText.text = $"You fell in battle, but dark sanctum corruption held your soul at 1 HP!\nCorruption increased to {curC} / {maxCorr}.\nThe expedition continues!";
                    primaryOutcomeBtn.gameObject.SetActive(true);
                    primaryOutcomeBtnText.text = "Endure Corruption & Proceed (1 HP)";
                    secondaryOutcomeBtn.gameObject.SetActive(false);
                    tertiaryOutcomeBtn.gameObject.SetActive(false);
                    primaryOutcomeBtn.onClick.AddListener(() =>
                    {
                        FinishOutcomeWithTransformationCheck();
                    });
                    break;

                case CombatResultType.DefeatHp:
                    outcomeTitleText.text = "TOTAL DEFEAT: CONSUMED BY CORRUPTION (3/3)";
                    outcomeDescText.text = "Your health was depleted and your body could not withstand further corruption!\nYour garments were restored and you safely awaken back in the Sanctuary Cave.";
                    primaryOutcomeBtn.gameObject.SetActive(true);
                    primaryOutcomeBtnText.text = "Return to Sanctuary Cave";
                    secondaryOutcomeBtn.gameObject.SetActive(false);
                    tertiaryOutcomeBtn.gameObject.SetActive(false);
                    primaryOutcomeBtn.onClick.AddListener(() =>
                    {
                        outcomeModal.SetActive(false);
                        Hide();
                        OnCombatDefeatToLobby?.Invoke();
                    });
                    break;

                case CombatResultType.DefeatSeduction:
                    outcomeTitleText.text = "TOTAL DEFEAT: CONSUMED BY LUST (3/3)";
                    outcomeDescText.text = "Your 3rd Climax Stack was breached and corruption overwhelmed you!\nYour garments were repaired and you awaken hours later back inside the Sanctuary Cave.";
                    primaryOutcomeBtn.gameObject.SetActive(true);
                    primaryOutcomeBtnText.text = "Awaken in Sanctuary Cave";
                    secondaryOutcomeBtn.gameObject.SetActive(false);
                    tertiaryOutcomeBtn.gameObject.SetActive(false);
                    primaryOutcomeBtn.onClick.AddListener(() =>
                    {
                        outcomeModal.SetActive(false);
                        Hide();
                        OnCombatDefeatToLobby?.Invoke();
                    });
                    break;
            }
        }

        // =========================================================================
        // DYNAMIC ENEMY & CARD FACTORIES
        // =========================================================================

        private GameObject CreateEnemyCardObject(Transform parent, Combatant enemy, UnityEngine.Events.UnityAction onClick)
        {
            GameObject cardObj = new GameObject($"Enemy_{enemy.name}", typeof(RectTransform));
            cardObj.transform.SetParent(parent, false);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(430f, 740f);

            // 1. Transparent click hit area covering the entire body
            Image clickArea = cardObj.AddComponent<Image>();
            clickArea.color = new Color(0f, 0f, 0f, 0.001f);
            clickArea.raycastTarget = true;

            Button btn = cardObj.AddComponent<Button>();
            btn.targetGraphic = clickArea;
            btn.onClick.AddListener(onClick);

            // 2. Ground Reticle / Aura for Target Selection (under feet, no box frame)
            // 2. Ground Halo for Target Selection (under feet, sleek ellipse glow, NO text)
            GameObject haloObj = new GameObject("TargetHalo", typeof(RectTransform));
            haloObj.transform.SetParent(cardObj.transform, false);
            Image haloImg = haloObj.AddComponent<Image>();
            haloImg.color = new Color(1f, 0.88f, 0.35f, 0.45f);
            haloImg.raycastTarget = false;
            RectTransform srt = haloObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.12f);
            srt.anchorMax = new Vector2(0.5f, 0.12f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = Vector2.zero;
            srt.sizeDelta = new Vector2(250f, 22f);
            haloObj.SetActive(false);

            // 2b. Character Aura Glow (glows softly behind the body when targeted, NO text)
            GameObject auraObj = new GameObject("TargetAura", typeof(RectTransform));
            auraObj.transform.SetParent(cardObj.transform, false);
            Image auraImg = auraObj.AddComponent<Image>();
            auraImg.color = new Color(1f, 0.85f, 0.25f, 0.22f);
            auraImg.raycastTarget = false;
            RectTransform art = auraObj.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.5f, 0.12f);
            art.anchorMax = new Vector2(0.5f, 0.90f);
            art.pivot = new Vector2(0.5f, 0f);
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = new Vector2(380f, 0f);
            auraObj.SetActive(false);

            // 2c. Ground Halo for Active Turn (under feet, soft warm amber glow)
            GameObject turnHaloObj = new GameObject("TurnHalo", typeof(RectTransform));
            turnHaloObj.transform.SetParent(cardObj.transform, false);
            Image turnHaloImg = turnHaloObj.AddComponent<Image>();
            turnHaloImg.color = new Color(1f, 0.65f, 0.25f, 0.35f);
            turnHaloImg.raycastTarget = false;
            RectTransform thrt = turnHaloObj.GetComponent<RectTransform>();
            thrt.anchorMin = new Vector2(0.5f, 0.12f);
            thrt.anchorMax = new Vector2(0.5f, 0.12f);
            thrt.pivot = new Vector2(0.5f, 0.5f);
            thrt.anchoredPosition = Vector2.zero;
            thrt.sizeDelta = new Vector2(260f, 24f);
            turnHaloObj.SetActive(false);

            // 2d. Character Aura for Active Turn (glows gently behind body)
            GameObject turnAuraObj = new GameObject("TurnAura", typeof(RectTransform));
            turnAuraObj.transform.SetParent(cardObj.transform, false);
            Image turnAuraImg = turnAuraObj.AddComponent<Image>();
            turnAuraImg.color = new Color(1f, 0.58f, 0.20f, 0.18f);
            turnAuraImg.raycastTarget = false;
            RectTransform tart = turnAuraObj.GetComponent<RectTransform>();
            tart.anchorMin = new Vector2(0.5f, 0.12f);
            tart.anchorMax = new Vector2(0.5f, 0.90f);
            tart.pivot = new Vector2(0.5f, 0f);
            tart.anchoredPosition = Vector2.zero;
            tart.sizeDelta = new Vector2(390f, 0f);
            turnAuraObj.SetActive(false);

            // 3. Full-Height Character Sprite (Standing freely, NO card box, NO mask!)
            GameObject pSpriteObj = new GameObject("FullBodySprite", typeof(RectTransform));
            pSpriteObj.transform.SetParent(cardObj.transform, false);
            Image pImg = pSpriteObj.AddComponent<Image>();
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;
            RectTransform psrt = pSpriteObj.GetComponent<RectTransform>();
            psrt.anchorMin = new Vector2(0.5f, 0.12f);
            psrt.anchorMax = new Vector2(0.5f, 0.90f);
            psrt.pivot = new Vector2(0.5f, 0f);
            psrt.anchoredPosition = Vector2.zero;
            psrt.sizeDelta = new Vector2(420f, 0f);

            // Red Hit Flash Overlay on Enemy
            GameObject hfObj = new GameObject("HitFlashOverlay", typeof(RectTransform));
            hfObj.transform.SetParent(pSpriteObj.transform, false);
            Image hfImg = hfObj.AddComponent<Image>();
            hfImg.color = new Color(1f, 0.12f, 0.12f, 0f);
            hfImg.raycastTarget = false;
            RectTransform hfrt = hfObj.GetComponent<RectTransform>();
            hfrt.anchorMin = Vector2.zero;
            hfrt.anchorMax = Vector2.one;
            hfrt.sizeDelta = Vector2.zero;

            // Pink Haze Overlay on Enemy (for seduction phase change)
            GameObject phObj = new GameObject("PinkHazeOverlay", typeof(RectTransform));
            phObj.transform.SetParent(pSpriteObj.transform, false);
            Image phImg = phObj.AddComponent<Image>();
            phImg.color = new Color(1f, 0.35f, 0.85f, 0f);
            phImg.raycastTarget = false;
            RectTransform phrt = phObj.GetComponent<RectTransform>();
            phrt.anchorMin = Vector2.zero;
            phrt.anchorMax = Vector2.one;
            phrt.sizeDelta = Vector2.zero;

            // 4. Overhead Name (Floating above head)
            GameObject nameObj = new GameObject("Name", typeof(RectTransform));
            nameObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = enemy.name;
            nameTxt.fontSize = 20;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.alignment = TextAlignmentOptions.Center;
            nameTxt.color = Color.white;
            nameTxt.raycastTarget = false;
            RectTransform nrt = nameObj.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.5f, 0.91f);
            nrt.anchorMax = new Vector2(0.5f, 0.91f);
            nrt.pivot = new Vector2(0.5f, 0f);
            nrt.anchoredPosition = Vector2.zero;
            nrt.sizeDelta = new Vector2(280f, 32f);

            // 4b. Overhead Active Turn Indicator Pill Badge
            GameObject turnBadgeObj = new GameObject("TurnBadge", typeof(RectTransform));
            turnBadgeObj.transform.SetParent(cardObj.transform, false);
            Image tbBg = turnBadgeObj.AddComponent<Image>();
            tbBg.color = new Color(0.18f, 0.12f, 0.06f, 0.85f);
            tbBg.raycastTarget = false;
            RectTransform tbrt = turnBadgeObj.GetComponent<RectTransform>();
            tbrt.anchorMin = new Vector2(0.5f, 0.955f);
            tbrt.anchorMax = new Vector2(0.5f, 0.955f);
            tbrt.pivot = new Vector2(0.5f, 0.5f);
            tbrt.anchoredPosition = Vector2.zero;
            tbrt.sizeDelta = new Vector2(110f, 22f);

            GameObject tbTxtObj = new GameObject("Text", typeof(RectTransform));
            tbTxtObj.transform.SetParent(turnBadgeObj.transform, false);
            TextMeshProUGUI tbTxt = tbTxtObj.AddComponent<TextMeshProUGUI>();
            tbTxt.text = "> ACTING";
            tbTxt.fontSize = 11;
            tbTxt.fontStyle = FontStyles.Bold;
            tbTxt.alignment = TextAlignmentOptions.Center;
            tbTxt.color = new Color(1f, 0.82f, 0.35f, 1f);
            tbTxt.raycastTarget = false;
            RectTransform tbtr = tbTxtObj.GetComponent<RectTransform>();
            tbtr.anchorMin = Vector2.zero;
            tbtr.anchorMax = Vector2.one;
            tbtr.sizeDelta = Vector2.zero;
            turnBadgeObj.SetActive(false);

            // 5. Overhead Telegraph Banner
            GameObject telObj = new GameObject("TelegraphBanner", typeof(RectTransform));
            telObj.transform.SetParent(cardObj.transform, false);
            Image telBg = telObj.AddComponent<Image>();
            telBg.color = new Color(0.24f, 0.10f, 0.28f, 0.98f);
            RectTransform telRt = telObj.GetComponent<RectTransform>();
            telRt.anchorMin = new Vector2(0.5f, 0.97f);
            telRt.anchorMax = new Vector2(0.5f, 0.97f);
            telRt.pivot = new Vector2(0.5f, 0f);
            telRt.anchoredPosition = Vector2.zero;
            telRt.sizeDelta = new Vector2(230f, 38f);

            GameObject telTxtObj = new GameObject("Text", typeof(RectTransform));
            telTxtObj.transform.SetParent(telObj.transform, false);
            TextMeshProUGUI telTxt = telTxtObj.AddComponent<TextMeshProUGUI>();
            telTxt.text = "Skill";
            telTxt.fontSize = 14;
            telTxt.fontStyle = FontStyles.Bold;
            telTxt.alignment = TextAlignmentOptions.Center;
            telTxt.color = new Color(1f, 0.85f, 0.35f, 1f);
            telTxt.raycastTarget = false;
            RectTransform telTrt = telTxtObj.GetComponent<RectTransform>();
            telTrt.anchorMin = Vector2.zero;
            telTrt.anchorMax = Vector2.one;
            telTrt.sizeDelta = Vector2.zero;
            telObj.SetActive(false);

            // 6. Sleek Floating Status Plate (Under feet)
            GameObject statusPlate = new GameObject("StatusPlate", typeof(RectTransform));
            statusPlate.transform.SetParent(cardObj.transform, false);
            Image spBg = statusPlate.AddComponent<Image>();
            if (progressbarFrameSprite == null) progressbarFrameSprite = LoadUISprite("ui_progressbar_frame.png");
            if (progressbarFrameSprite != null)
            {
                spBg.sprite = progressbarFrameSprite;
                spBg.type = Image.Type.Sliced;
                spBg.color = Color.white;
            }
            else
            {
                spBg.color = new Color(0.08f, 0.10f, 0.14f, 0.88f);
            }
            spBg.raycastTarget = false;
            RectTransform sprt = statusPlate.GetComponent<RectTransform>();
            sprt.anchorMin = new Vector2(0.5f, 0.05f);
            sprt.anchorMax = new Vector2(0.5f, 0.05f);
            sprt.pivot = new Vector2(0.5f, 0.5f);
            sprt.anchoredPosition = Vector2.zero;
            sprt.sizeDelta = new Vector2(260f, 64f);

            // HP Text (Cleanly inset within dark inner cavity, clear of 16px top wooden border)
            GameObject hpObj = new GameObject("HPText", typeof(RectTransform));
            hpObj.transform.SetParent(statusPlate.transform, false);
            TextMeshProUGUI hpTxt = hpObj.AddComponent<TextMeshProUGUI>();
            hpTxt.fontSize = 13;
            hpTxt.fontStyle = FontStyles.Bold;
            hpTxt.alignment = TextAlignmentOptions.Center;
            hpTxt.color = new Color(0.35f, 0.90f, 0.45f, 1f);
            hpTxt.raycastTarget = false;
            RectTransform hrt = hpObj.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.06f, 0.50f);
            hrt.anchorMax = new Vector2(0.94f, 1.0f);
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = new Vector2(0f, -14f);

            // Seduction Text (Cleanly inset within dark inner cavity, clear of 16px bottom wooden border)
            GameObject sedObj = new GameObject("SedText", typeof(RectTransform));
            sedObj.transform.SetParent(statusPlate.transform, false);
            TextMeshProUGUI sedTxt = sedObj.AddComponent<TextMeshProUGUI>();
            sedTxt.fontSize = 12;
            sedTxt.fontStyle = FontStyles.Bold;
            sedTxt.alignment = TextAlignmentOptions.Center;
            sedTxt.color = new Color(1f, 0.50f, 0.80f, 1f);
            sedTxt.raycastTarget = false;
            RectTransform srt2 = sedObj.GetComponent<RectTransform>();
            srt2.anchorMin = new Vector2(0.06f, 0.0f);
            srt2.anchorMax = new Vector2(0.94f, 0.50f);
            srt2.offsetMin = new Vector2(0f, 14f);
            srt2.offsetMax = Vector2.zero;

            GameObject mpObj = new GameObject("MPText", typeof(RectTransform));
            mpObj.transform.SetParent(statusPlate.transform, false);
            TextMeshProUGUI mpTxt = mpObj.AddComponent<TextMeshProUGUI>();
            mpTxt.raycastTarget = false;
            mpObj.SetActive(false);

            // 7. Small Engagement Status Square Badge (Grapple / Penetration)
            GameObject sqObj = new GameObject("StatusSquareBadge", typeof(RectTransform));
            sqObj.transform.SetParent(cardObj.transform, false);
            Image sqBorder = sqObj.AddComponent<Image>();
            sqBorder.color = new Color(1f, 0.55f, 0.15f, 1f); // Amber default
            sqBorder.raycastTarget = false;

            RectTransform sqRt = sqObj.GetComponent<RectTransform>();
            sqRt.anchorMin = new Vector2(0.14f, 0.86f);
            sqRt.anchorMax = new Vector2(0.14f, 0.86f);
            sqRt.pivot = new Vector2(0.5f, 0.5f);
            sqRt.sizeDelta = new Vector2(36f, 36f);
            sqRt.anchoredPosition = Vector2.zero;

            GameObject sqBgObj = new GameObject("InnerBg", typeof(RectTransform));
            sqBgObj.transform.SetParent(sqObj.transform, false);
            Image sqBg = sqBgObj.AddComponent<Image>();
            sqBg.color = new Color(0.12f, 0.08f, 0.14f, 0.96f);
            sqBg.raycastTarget = false;
            RectTransform sqBgRt = sqBgObj.GetComponent<RectTransform>();
            sqBgRt.anchorMin = Vector2.zero;
            sqBgRt.anchorMax = Vector2.one;
            sqBgRt.offsetMin = new Vector2(2f, 2f);
            sqBgRt.offsetMax = new Vector2(-2f, -2f);

            GameObject sqTxtObj = new GameObject("StatusText", typeof(RectTransform));
            sqTxtObj.transform.SetParent(sqBgObj.transform, false);
            TextMeshProUGUI sqTxt = sqTxtObj.AddComponent<TextMeshProUGUI>();
            sqTxt.text = "G";
            sqTxt.fontSize = 20;
            sqTxt.fontStyle = FontStyles.Bold;
            sqTxt.alignment = TextAlignmentOptions.Center;
            sqTxt.color = new Color(1f, 0.75f, 0.30f, 1f);
            sqTxt.raycastTarget = false;
            RectTransform sqTxtRt = sqTxtObj.GetComponent<RectTransform>();
            sqTxtRt.anchorMin = Vector2.zero;
            sqTxtRt.anchorMax = Vector2.one;
            sqTxtRt.sizeDelta = Vector2.zero;

            GameObject sqLblObj = new GameObject("StatusLabel", typeof(RectTransform));
            sqLblObj.transform.SetParent(sqObj.transform, false);
            TextMeshProUGUI sqLbl = sqLblObj.AddComponent<TextMeshProUGUI>();
            sqLbl.text = "GRAPPLE";
            sqLbl.fontSize = 9;
            sqLbl.fontStyle = FontStyles.Bold;
            sqLbl.alignment = TextAlignmentOptions.Center;
            sqLbl.color = new Color(1f, 0.75f, 0.30f, 1f);
            sqLbl.raycastTarget = false;
            RectTransform sqLblRt = sqLblObj.GetComponent<RectTransform>();
            sqLblRt.anchorMin = new Vector2(0.5f, 0f);
            sqLblRt.anchorMax = new Vector2(0.5f, 0f);
            sqLblRt.pivot = new Vector2(0.5f, 1f);
            sqLblRt.anchoredPosition = new Vector2(0f, -2f);
            sqLblRt.sizeDelta = new Vector2(85f, 22f);

            sqObj.SetActive(false);

            EnemyCardUI ui = cardObj.AddComponent<EnemyCardUI>();
            ui.assignedEnemy = enemy;
            ui.targetHalo = haloObj;
            ui.targetHaloImg = haloImg;
            ui.targetAura = auraObj;
            ui.targetAuraImg = auraImg;
            ui.turnHalo = turnHaloObj;
            ui.turnHaloImg = turnHaloImg;
            ui.turnAura = turnAuraObj;
            ui.turnAuraImg = turnAuraImg;
            ui.turnBadge = turnBadgeObj;
            ui.telegraphBox = telObj;
            ui.telegraphText = telTxt;
            ui.nameText = nameTxt;
            ui.portraitImage = pImg;
            ui.portraitTransform = psrt;
            ui.hpText = hpTxt;
            ui.mpText = mpTxt;
            ui.seductionText = sedTxt;
            ui.hitFlashOverlay = hfImg;
            ui.pinkHazeOverlay = phImg;
            ui.statusSquareBadge = sqObj;
            ui.statusSquareBorder = sqBorder;
            ui.statusSquareBg = sqBg;
            ui.statusSquareText = sqTxt;
            ui.statusSquareLabel = sqLbl;
            return cardObj;
        }

        private GameObject CreateCombatCardObject(Transform parent, CardInstance card, int index, UnityEngine.Events.UnityAction onClick)
        {
            GameObject cardObj = new GameObject($"Card_{card.data.id}", typeof(RectTransform));
            cardObj.transform.SetParent(parent, false);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(150f, 210f);

            Canvas cardCanvas = cardObj.AddComponent<Canvas>();
            cardCanvas.overrideSorting = false;
            cardCanvas.sortingOrder = 5;
            cardObj.AddComponent<GraphicRaycaster>();

            // Inner Visual Root (animates Y position and scale without disturbing HorizontalLayoutGroup!)
            GameObject visualRootObj = new GameObject("VisualRoot", typeof(RectTransform));
            visualRootObj.transform.SetParent(cardObj.transform, false);
            RectTransform vrRt = visualRootObj.GetComponent<RectTransform>();
            vrRt.anchorMin = Vector2.zero;
            vrRt.anchorMax = Vector2.one;
            vrRt.offsetMin = Vector2.zero;
            vrRt.offsetMax = Vector2.zero;

            Image bg = visualRootObj.AddComponent<Image>();
            if (cardFrameSprite == null) cardFrameSprite = LoadUISprite("ui_card_frame.png");
            if (cardFrameSprite != null)
            {
                bg.sprite = cardFrameSprite;
                bg.color = Color.white;
            }
            else
            {
                bg.color = Color.black;
            }

            Button btn = cardObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);

            // Selection / Focus Glowing Border (Glows brighter gold on hover/select)
            GameObject borderObj = new GameObject("SelectBorder", typeof(RectTransform));
            borderObj.transform.SetParent(visualRootObj.transform, false);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(1f, 0.90f, 0.35f, 1f);
            borderImg.raycastTarget = false;
            RectTransform brt = borderObj.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(-4.5f, -4.5f);
            brt.offsetMax = new Vector2(4.5f, 4.5f);
            borderObj.transform.SetAsFirstSibling();
            borderObj.SetActive(false);

            // Permanent Resting Card Frame / Border (Styled by Card Category: Warrior Gold, Lust Pink, Struggle Amber)
            GameObject restingBorderObj = new GameObject("RestingBorder", typeof(RectTransform));
            restingBorderObj.transform.SetParent(visualRootObj.transform, false);
            Image restingBorderImg = restingBorderObj.AddComponent<Image>();
            
            Color borderCol = new Color(0.72f, 0.58f, 0.28f, 0.95f); // Warrior gold default
            if (card.data.isEphemeral || card.data.category == CardCategory.LustInstinct)
            {
                borderCol = new Color(1f, 0.35f, 0.75f, 1f); // Radiant Lust Pink
            }
            else if (card.data.category == CardCategory.StruggleAction)
            {
                borderCol = new Color(0.95f, 0.50f, 0.20f, 1f); // Intense Struggle Amber
            }
            restingBorderImg.color = borderCol;
            restingBorderImg.raycastTarget = false;
            restingBorderObj.SetActive(cardFrameSprite == null);
            RectTransform rbrt = restingBorderObj.GetComponent<RectTransform>();
            rbrt.anchorMin = Vector2.zero;
            rbrt.anchorMax = Vector2.one;
            rbrt.offsetMin = new Vector2(-2.5f, -2.5f);
            rbrt.offsetMax = new Vector2(2.5f, 2.5f);
            restingBorderObj.transform.SetAsFirstSibling();

            // Ephemeral Lust Card Ribbon / Badge
            if (card.data.isEphemeral || card.data.category == CardCategory.LustInstinct)
            {
                GameObject ephemObj = new GameObject("EphemBadge", typeof(RectTransform));
                ephemObj.transform.SetParent(visualRootObj.transform, false);
                Image ephemBg = ephemObj.AddComponent<Image>();
                ephemBg.color = new Color(0.95f, 0.20f, 0.65f, 0.95f);
                ephemBg.raycastTarget = false;
                RectTransform eprt = ephemObj.GetComponent<RectTransform>();
                eprt.anchorMin = new Vector2(1f, 1f);
                eprt.anchorMax = new Vector2(1f, 1f);
                eprt.pivot = new Vector2(1f, 1f);
                eprt.anchoredPosition = new Vector2(-5f, -5f);
                eprt.sizeDelta = new Vector2(46f, 18f);

                GameObject ephemTxtObj = new GameObject("Text", typeof(RectTransform));
                ephemTxtObj.transform.SetParent(ephemObj.transform, false);
                TextMeshProUGUI ephemTxt = ephemTxtObj.AddComponent<TextMeshProUGUI>();
                ephemTxt.text = "LUST";
                ephemTxt.fontSize = 11;
                ephemTxt.fontStyle = FontStyles.Bold;
                ephemTxt.alignment = TextAlignmentOptions.Center;
                ephemTxt.color = Color.white;
                ephemTxt.raycastTarget = false;
                RectTransform eptxtRt = ephemTxtObj.GetComponent<RectTransform>();
                eptxtRt.anchorMin = Vector2.zero;
                eptxtRt.anchorMax = Vector2.one;
                eptxtRt.sizeDelta = Vector2.zero;
            }

            // Card Art Window with RectMask2D (proportional cover / un-distorted scaling)
            GameObject artWindow = new GameObject("ArtWindow", typeof(RectTransform));
            artWindow.transform.SetParent(visualRootObj.transform, false);
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
                winRt.offsetMin = new Vector2(3f, 37f);
                winRt.offsetMax = new Vector2(-3f, -3f);
            }

            // Card Art Image (proportional fit inside the window, zoomed to fill height without distortion)
            GameObject artObj = new GameObject("CardArt", typeof(RectTransform));
            artObj.transform.SetParent(artWindow.transform, false);
            Image artImg = artObj.AddComponent<Image>();
            artImg.preserveAspect = true;
            artImg.raycastTarget = false;
            RectTransform artRt = artObj.GetComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;
            Sprite art = card.data.cardArt != null ? card.data.cardArt : SkillArtDatabase.GetSkillSprite(card.data.id);
            if (art != null)
            {
                artImg.sprite = art;
                artImg.color = Color.white;
            }
            else
            {
                artImg.color = Color.black;
            }

            // Gold divider line above NamePlate
            GameObject sepObj = new GameObject("NameSeparator", typeof(RectTransform));
            sepObj.transform.SetParent(visualRootObj.transform, false);
            Image sepImg = sepObj.AddComponent<Image>();
            sepImg.color = new Color(0.72f, 0.58f, 0.28f, 0.95f);
            sepImg.raycastTarget = false;
            RectTransform sepRt = sepObj.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 0f);
            sepRt.anchorMax = new Vector2(1f, 0f);
            sepRt.pivot = new Vector2(0.5f, 0f);
            sepRt.anchoredPosition = new Vector2(0f, 35f);
            sepRt.sizeDelta = new Vector2(0f, 2f);
            sepObj.SetActive(cardFrameSprite == null);

            // AP Cost Badge (Top-Left ornate RPG coin / badge)
            GameObject apObj = new GameObject("ApBadge", typeof(RectTransform));
            apObj.transform.SetParent(visualRootObj.transform, false);

            Image apRim = apObj.AddComponent<Image>();
            apRim.color = borderCol;
            apRim.raycastTarget = false;
            RectTransform apr = apObj.GetComponent<RectTransform>();
            apr.anchorMin = new Vector2(0f, 1f);
            apr.anchorMax = new Vector2(0f, 1f);
            apr.pivot = new Vector2(0f, 1f);
            apr.anchoredPosition = new Vector2(4f, -4f);
            apr.sizeDelta = new Vector2(28f, 28f);

            GameObject apInner = new GameObject("Inner", typeof(RectTransform));
            apInner.transform.SetParent(apObj.transform, false);
            Image apBg = apInner.AddComponent<Image>();
            apBg.color = new Color(0.95f, 0.74f, 0.18f, 1f);
            apBg.raycastTarget = false;
            RectTransform apir = apInner.GetComponent<RectTransform>();
            apir.anchorMin = Vector2.zero;
            apir.anchorMax = Vector2.one;
            apir.offsetMin = new Vector2(2f, 2f);
            apir.offsetMax = new Vector2(-2f, -2f);

            GameObject apTxtObj = new GameObject("Text", typeof(RectTransform));
            apTxtObj.transform.SetParent(apInner.transform, false);
            TextMeshProUGUI apTxt = apTxtObj.AddComponent<TextMeshProUGUI>();
            apTxt.text = $"{card.data.apCost}";
            apTxt.fontSize = 17;
            apTxt.fontStyle = FontStyles.Bold;
            apTxt.alignment = TextAlignmentOptions.Center;
            apTxt.color = Color.black;
            apTxt.raycastTarget = false;
            RectTransform aptr = apTxtObj.GetComponent<RectTransform>();
            aptr.anchorMin = Vector2.zero;
            aptr.anchorMax = Vector2.one;
            aptr.sizeDelta = Vector2.zero;

            // Name Plate (Bottom Banner)
            GameObject nameObj = new GameObject("NamePlate", typeof(RectTransform));
            nameObj.transform.SetParent(visualRootObj.transform, false);
            Image nameBg = nameObj.AddComponent<Image>();
            nameBg.raycastTarget = false;
            RectTransform nrt = nameObj.GetComponent<RectTransform>();

            if (cardFrameSprite != null)
            {
                nameBg.color = Color.clear;
                nrt.anchorMin = new Vector2(0.14f, 0.08f);
                nrt.anchorMax = new Vector2(0.86f, 0.28f);
                nrt.offsetMin = Vector2.zero;
                nrt.offsetMax = Vector2.zero;
            }
            else
            {
                nameBg.color = new Color(0.04f, 0.04f, 0.06f, 1f);
                nrt.anchorMin = new Vector2(0f, 0f);
                nrt.anchorMax = new Vector2(1f, 0f);
                nrt.pivot = new Vector2(0.5f, 0f);
                nrt.anchoredPosition = Vector2.zero;
                nrt.sizeDelta = new Vector2(0f, 36f);
            }

            GameObject nameTxtObj = new GameObject("Text", typeof(RectTransform));
            nameTxtObj.transform.SetParent(nameObj.transform, false);
            TextMeshProUGUI nameTxt = nameTxtObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = card.data.cardName;
            nameTxt.enableAutoSizing = true;
            nameTxt.fontSizeMin = 8f;
            nameTxt.fontSizeMax = 14f;
            nameTxt.lineSpacing = -8;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.alignment = TextAlignmentOptions.Center;
            nameTxt.color = new Color(0.96f, 0.94f, 0.90f, 1f);
            nameTxt.raycastTarget = false;
            RectTransform ntr = nameTxtObj.GetComponent<RectTransform>();
            ntr.anchorMin = Vector2.zero;
            ntr.anchorMax = Vector2.one;
            ntr.offsetMin = new Vector2(4f, 0f);
            ntr.offsetMax = new Vector2(-4f, 0f);

            // Cooldown / Disabled Overlay
            GameObject cdObj = new GameObject("CdOverlay", typeof(RectTransform));
            cdObj.transform.SetParent(visualRootObj.transform, false);
            Image cdBg = cdObj.AddComponent<Image>();
            cdBg.color = new Color(0.05f, 0.05f, 0.08f, 0.88f);
            cdBg.raycastTarget = false;
            RectTransform cdrt = cdObj.GetComponent<RectTransform>();
            cdrt.anchorMin = Vector2.zero;
            cdrt.anchorMax = Vector2.one;
            cdrt.sizeDelta = Vector2.zero;

            GameObject cdTxtObj = new GameObject("CdText", typeof(RectTransform));
            cdTxtObj.transform.SetParent(cdObj.transform, false);
            TextMeshProUGUI cdTxt = cdTxtObj.AddComponent<TextMeshProUGUI>();
            cdTxt.fontSize = 20;
            cdTxt.fontStyle = FontStyles.Bold;
            cdTxt.alignment = TextAlignmentOptions.Center;
            cdTxt.color = new Color(1f, 0.4f, 0.4f, 1f);
            cdTxt.raycastTarget = false;
            RectTransform cdtr = cdTxtObj.GetComponent<RectTransform>();
            cdtr.anchorMin = Vector2.zero;
            cdtr.anchorMax = Vector2.one;
            cdtr.sizeDelta = Vector2.zero;

            CombatCardUI cui = cardObj.AddComponent<CombatCardUI>();
            cui.btn = btn;
            cui.cdOverlay = cdObj;
            cui.cdText = cdTxt;
            cui.selectedBorder = borderObj;
            cui.visualRoot = vrRt;
            cui.cardCanvas = cardCanvas;
            cui.cardIndex = index;
            return cardObj;
        }



        // =========================================================================
        // FULL COMBAT UI DYNAMIC GENERATOR
        // =========================================================================

        public static CombatUI CreateDynamic(Transform parent, CombatManager manager)
        {
            GameObject panelObj = new GameObject("CombatPanel", typeof(RectTransform));
            panelObj.transform.SetParent(parent, false);
            panelObj.AddComponent<FloatingCombatText>();

            RectTransform pr = panelObj.GetComponent<RectTransform>();
            pr.anchorMin = Vector2.zero;
            pr.anchorMax = Vector2.one;
            pr.sizeDelta = Vector2.zero;

            CombatUI ui = panelObj.AddComponent<CombatUI>();
            ui.combatPanel = panelObj;
            if (panelObj.GetComponent<FloatingCombatText>() == null)
            {
                panelObj.AddComponent<FloatingCombatText>();
            }

            // Background Dimmer Image
            Image bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);

            // --- 1. Top Turn Order Header Bar & Header Action Controls ---
            GameObject topBarObj = new GameObject("TopTurnOrderBar", typeof(RectTransform));
            topBarObj.transform.SetParent(panelObj.transform, false);
            Image topBg = topBarObj.AddComponent<Image>();
            topBg.color = new Color(0.08f, 0.10f, 0.14f, 0.95f);
            RectTransform tr = topBarObj.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.anchoredPosition = Vector2.zero;
            tr.sizeDelta = new Vector2(0f, 64f);

            // Timeline Badges Container (CENTERED horizontally on screen)
            GameObject timelineObj = new GameObject("TimelineBadges", typeof(RectTransform));
            timelineObj.transform.SetParent(topBarObj.transform, false);
            RectTransform tlr = timelineObj.GetComponent<RectTransform>();
            tlr.anchorMin = new Vector2(0.5f, 0.5f);
            tlr.anchorMax = new Vector2(0.5f, 0.5f);
            tlr.pivot = new Vector2(0.5f, 0.5f);
            tlr.anchoredPosition = Vector2.zero;
            tlr.sizeDelta = new Vector2(600f, 54f);

            HorizontalLayoutGroup thlg = timelineObj.AddComponent<HorizontalLayoutGroup>();
            thlg.spacing = 10;
            thlg.childAlignment = TextAnchor.MiddleCenter;
            thlg.childControlWidth = false;
            thlg.childControlHeight = false;
            thlg.childForceExpandWidth = false;
            thlg.childForceExpandHeight = false;
            ui.turnOrderContainer = tlr;

            // Header Action Buttons Container (Right aligned)
            GameObject headerControlsObj = new GameObject("HeaderControls", typeof(RectTransform));
            headerControlsObj.transform.SetParent(topBarObj.transform, false);
            RectTransform hcr = headerControlsObj.GetComponent<RectTransform>();
            hcr.anchorMin = new Vector2(1f, 0.5f);
            hcr.anchorMax = new Vector2(1f, 0.5f);
            hcr.pivot = new Vector2(1f, 0.5f);
            hcr.anchoredPosition = new Vector2(-20f, 0f);
            hcr.sizeDelta = new Vector2(510f, 40f);

            HorizontalLayoutGroup hchlg = headerControlsObj.AddComponent<HorizontalLayoutGroup>();
            hchlg.spacing = 10;
            hchlg.childAlignment = TextAnchor.MiddleRight;
            hchlg.childControlWidth = false;
            hchlg.childControlHeight = false;
            hchlg.childForceExpandWidth = false;
            hchlg.childForceExpandHeight = false;

            ui.debugWinHpButton = CreateHeaderActionButton(headerControlsObj.transform, "DebugWinHpBtn", "Win (HP)", new Color(0.18f, 0.45f, 0.25f, 0.95f), new Vector2(105f, 38f));
            ui.debugWinSeductionButton = CreateHeaderActionButton(headerControlsObj.transform, "DebugWinSedBtn", "Win (Sed)", new Color(0.55f, 0.18f, 0.45f, 0.95f), new Vector2(105f, 38f));
            ui.debugFillGaugeButton = CreateHeaderActionButton(headerControlsObj.transform, "DebugFillGaugeBtn", "Fill Gauge", new Color(0.65f, 0.20f, 0.55f, 0.95f), new Vector2(105f, 38f));
            ui.surrenderButton = CreateHeaderActionButton(headerControlsObj.transform, "SurrenderBtn", "Surrender", new Color(0.60f, 0.18f, 0.22f, 0.95f), new Vector2(115f, 38f));

            // --- 1b. Combat Stance Display (3 Squares Directly Under Turn Order Timeline) ---
            GameObject stanceBarObj = new GameObject("StanceDisplayBar", typeof(RectTransform));
            stanceBarObj.transform.SetParent(panelObj.transform, false);
            RectTransform sbr = stanceBarObj.GetComponent<RectTransform>();
            sbr.anchorMin = new Vector2(0.5f, 1f);
            sbr.anchorMax = new Vector2(0.5f, 1f);
            sbr.pivot = new Vector2(0.5f, 1f);
            sbr.anchoredPosition = new Vector2(0f, -66f);
            sbr.sizeDelta = new Vector2(240f, 62f);
            ui.stanceContainer = sbr;

            HorizontalLayoutGroup sbHlg = stanceBarObj.AddComponent<HorizontalLayoutGroup>();
            sbHlg.spacing = 16f;
            sbHlg.childAlignment = TextAnchor.MiddleCenter;
            sbHlg.childControlWidth = false;
            sbHlg.childControlHeight = false;
            sbHlg.childForceExpandWidth = false;
            sbHlg.childForceExpandHeight = false;

            string[] stanceCodes = new string[] { "NEU", "GRP", "PEN" };
            string[] stanceTitles = new string[] { "NEUTRAL", "GRAPPLED", "PENETRATED" };

            ui.stanceSquareObjects = new GameObject[3];
            ui.stanceBorderImages = new Image[3];
            ui.stanceBackgroundImages = new Image[3];
            ui.stancePlaceholderLabels = new TextMeshProUGUI[3];
            ui.stanceNameLabels = new TextMeshProUGUI[3];

            for (int i = 0; i < 3; i++)
            {
                int stanceIdx = i;
                GameObject sqObj = new GameObject($"StanceSquare_{stanceTitles[i]}", typeof(RectTransform));
                sqObj.transform.SetParent(stanceBarObj.transform, false);
                RectTransform sqRt = sqObj.GetComponent<RectTransform>();
                sqRt.sizeDelta = new Vector2(52f, 60f);

                CanvasGroup cg = sqObj.AddComponent<CanvasGroup>();
                cg.alpha = (i == 0) ? 1.0f : 0.45f;

                // Outer Border Frame (50x50)
                GameObject borderObj = new GameObject("Border", typeof(RectTransform));
                borderObj.transform.SetParent(sqObj.transform, false);
                RectTransform bRt = borderObj.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 1f);
                bRt.anchorMax = new Vector2(0.5f, 1f);
                bRt.pivot = new Vector2(0.5f, 1f);
                bRt.anchoredPosition = Vector2.zero;
                bRt.sizeDelta = new Vector2(50f, 50f);
                Image borderImg = borderObj.AddComponent<Image>();
                borderImg.color = (i == 0) ? new Color(0.95f, 0.75f, 0.20f, 1f) : new Color(0.25f, 0.28f, 0.35f, 0.45f);

                // Inner Background (44x44)
                GameObject innerBgObj = new GameObject("InnerBg", typeof(RectTransform));
                innerBgObj.transform.SetParent(borderObj.transform, false);
                RectTransform inRt = innerBgObj.GetComponent<RectTransform>();
                inRt.anchorMin = new Vector2(0.5f, 0.5f);
                inRt.anchorMax = new Vector2(0.5f, 0.5f);
                inRt.pivot = new Vector2(0.5f, 0.5f);
                inRt.sizeDelta = new Vector2(44f, 44f);
                Image innerBgImg = innerBgObj.AddComponent<Image>();
                innerBgImg.color = new Color(0.08f, 0.10f, 0.14f, 0.95f);

                // Stance Icon Image Slot (40x40 - for custom user sprites!)
                GameObject iconObj = new GameObject("IconSlot", typeof(RectTransform));
                iconObj.transform.SetParent(innerBgObj.transform, false);
                RectTransform iconRt = iconObj.GetComponent<RectTransform>();
                iconRt.anchorMin = Vector2.zero;
                iconRt.anchorMax = Vector2.one;
                iconRt.sizeDelta = Vector2.zero;
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                iconImg.enabled = false; // Enabled when user assigns custom sprite

                // Placeholder Text Label (Shown when sprite is not assigned)
                GameObject labelObj = new GameObject("PlaceholderLabel", typeof(RectTransform));
                labelObj.transform.SetParent(innerBgObj.transform, false);
                RectTransform lRt = labelObj.GetComponent<RectTransform>();
                lRt.anchorMin = Vector2.zero;
                lRt.anchorMax = Vector2.one;
                lRt.sizeDelta = Vector2.zero;
                TextMeshProUGUI phLabel = labelObj.AddComponent<TextMeshProUGUI>();
                phLabel.text = stanceCodes[i];
                phLabel.fontSize = 14;
                phLabel.fontStyle = FontStyles.Bold;
                phLabel.alignment = TextAlignmentOptions.Center;
                phLabel.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
                phLabel.raycastTarget = false;

                // Sub-label beneath the box ("NEUTRAL", "GRAPPLED", "PENETRATED")
                GameObject subLabelObj = new GameObject("SubLabel", typeof(RectTransform));
                subLabelObj.transform.SetParent(sqObj.transform, false);
                RectTransform slRt = subLabelObj.GetComponent<RectTransform>();
                slRt.anchorMin = new Vector2(0.5f, 0f);
                slRt.anchorMax = new Vector2(0.5f, 0f);
                slRt.pivot = new Vector2(0.5f, 0f);
                slRt.anchoredPosition = new Vector2(0f, 0f);
                slRt.sizeDelta = new Vector2(64f, 11f);
                TextMeshProUGUI subLabel = subLabelObj.AddComponent<TextMeshProUGUI>();
                subLabel.text = stanceTitles[i];
                subLabel.fontSize = 8;
                subLabel.alignment = TextAlignmentOptions.Center;
                subLabel.color = (i == 0) ? new Color(0.95f, 0.75f, 0.20f, 1f) : new Color(0.55f, 0.58f, 0.65f, 0.6f);
                subLabel.raycastTarget = false;

                ui.stanceSquareObjects[i] = sqObj;
                ui.stanceBorderImages[i] = borderImg;
                ui.stanceBackgroundImages[i] = innerBgImg;
                ui.stancePlaceholderLabels[i] = phLabel;
                ui.stanceNameLabels[i] = subLabel;

                if (i == 0) ui.neutralStanceIcon = iconImg;
                else if (i == 1) ui.grappledStanceIcon = iconImg;
                else if (i == 2) ui.penetratedStanceIcon = iconImg;
            }

            // --- 2. Combat Log Message ---
            GameObject logObj = new GameObject("CombatLog", typeof(RectTransform));
            logObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI logTxt = logObj.AddComponent<TextMeshProUGUI>();
            logTxt.text = "A battle has commenced!";
            logTxt.fontSize = 20;
            logTxt.fontStyle = FontStyles.Bold;
            logTxt.alignment = TextAlignmentOptions.Center;
            logTxt.color = new Color(0.95f, 0.85f, 0.50f, 1f);
            logTxt.raycastTarget = false;
            RectTransform lr = logObj.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0.5f, 1f);
            lr.anchorMax = new Vector2(0.5f, 1f);
            lr.pivot = new Vector2(0.5f, 1f);
            lr.anchoredPosition = new Vector2(0f, -132f);
            lr.sizeDelta = new Vector2(900f, 32f);
            ui.combatLogText = logTxt;

            // --- 3. Player Skill Announcement Banner ---
            GameObject psbObj = new GameObject("PlayerSkillBanner", typeof(RectTransform));
            psbObj.transform.SetParent(panelObj.transform, false);
            Image psbBg = psbObj.AddComponent<Image>();
            psbBg.color = new Color(0.12f, 0.18f, 0.28f, 0.95f);
            psbBg.raycastTarget = false;
            RectTransform psbr = psbObj.GetComponent<RectTransform>();
            psbr.anchorMin = new Vector2(0.5f, 0.85f);
            psbr.anchorMax = new Vector2(0.5f, 0.85f);
            psbr.pivot = new Vector2(0.5f, 0.5f);
            psbr.anchoredPosition = Vector2.zero;
            psbr.sizeDelta = new Vector2(560f, 64f);

            GameObject psbtObj = new GameObject("Text", typeof(RectTransform));
            psbtObj.transform.SetParent(psbObj.transform, false);
            TextMeshProUGUI psbt = psbtObj.AddComponent<TextMeshProUGUI>();
            psbt.fontSize = 20;
            psbt.alignment = TextAlignmentOptions.Center;
            psbt.color = Color.white;
            psbt.raycastTarget = false;
            RectTransform psbtr = psbtObj.GetComponent<RectTransform>();
            psbtr.anchorMin = Vector2.zero;
            psbtr.anchorMax = Vector2.one;
            psbtr.sizeDelta = Vector2.zero;
            psbObj.SetActive(false);
            ui.playerSkillBanner = psbObj;
            ui.playerSkillBannerText = psbt;

            // --- 4a. Player Avatar Container (Heroine on Battlefield - shifted safely right to clear combat log) ---
            GameObject playerAvatarObj = new GameObject("PlayerAvatarContainer", typeof(RectTransform));
            playerAvatarObj.transform.SetParent(panelObj.transform, false);
            RectTransform part = playerAvatarObj.GetComponent<RectTransform>();
            part.anchorMin = new Vector2(0f, 0.5f);
            part.anchorMax = new Vector2(0f, 0.5f);
            part.pivot = new Vector2(0.5f, 0.5f);
            part.anchoredPosition = new Vector2(530f, 40f);
            part.sizeDelta = new Vector2(460f, 720f);
            ui.playerAvatarContainer = part;

            // Click target on player avatar for Self cards
            Image pClick = playerAvatarObj.AddComponent<Image>();
            pClick.color = new Color(0f, 0f, 0f, 0.001f);
            pClick.raycastTarget = true;
            Button pBtn = playerAvatarObj.AddComponent<Button>();
            pBtn.targetGraphic = pClick;
            pBtn.onClick.AddListener(() => ui.OnPlayerAvatarClicked());

            // Player target highlight halo & aura (for Self targeting - sleek glow, NO text)
            GameObject pRetObj = new GameObject("PlayerTargetHighlightGroup", typeof(RectTransform));
            pRetObj.transform.SetParent(playerAvatarObj.transform, false);
            pRetObj.transform.SetAsFirstSibling();

            GameObject pHaloObj = new GameObject("PlayerTargetHalo", typeof(RectTransform));
            pHaloObj.transform.SetParent(pRetObj.transform, false);
            Image pHaloImg = pHaloObj.AddComponent<Image>();
            pHaloImg.color = new Color(0.35f, 0.95f, 0.55f, 0.45f);
            pHaloImg.raycastTarget = false;
            RectTransform pHaloRt = pHaloObj.GetComponent<RectTransform>();
            pHaloRt.anchorMin = new Vector2(0.5f, 0.05f);
            pHaloRt.anchorMax = new Vector2(0.5f, 0.05f);
            pHaloRt.pivot = new Vector2(0.5f, 0.5f);
            pHaloRt.sizeDelta = new Vector2(280f, 24f);

            GameObject pAuraObj = new GameObject("PlayerTargetAura", typeof(RectTransform));
            pAuraObj.transform.SetParent(pRetObj.transform, false);
            Image pAuraImg = pAuraObj.AddComponent<Image>();
            pAuraImg.color = new Color(0.35f, 0.95f, 0.55f, 0.22f);
            pAuraImg.raycastTarget = false;
            RectTransform pAuraRt = pAuraObj.GetComponent<RectTransform>();
            pAuraRt.anchorMin = new Vector2(0.5f, 0.5f);
            pAuraRt.anchorMax = new Vector2(0.5f, 0.5f);
            pAuraRt.pivot = new Vector2(0.5f, 0.5f);
            pAuraRt.sizeDelta = new Vector2(440f, 680f);

            pRetObj.SetActive(false);
            ui.playerTargetHighlight = pRetObj;

            // --- 4b. Player Status HUD (Top Left, transparent background) ---
            GameObject playerStatsObj = new GameObject("PlayerStatsPanel", typeof(RectTransform));
            playerStatsObj.transform.SetParent(panelObj.transform, false);
            RectTransform psr = playerStatsObj.GetComponent<RectTransform>();
            psr.anchorMin = new Vector2(0f, 1f);
            psr.anchorMax = new Vector2(0f, 1f);
            psr.pivot = new Vector2(0f, 1f);
            psr.anchoredPosition = new Vector2(20f, -74f);
            psr.sizeDelta = new Vector2(330f, 440f);
            ui.playerStatsPanel = playerStatsObj;

            VerticalLayoutGroup vlg = playerStatsObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Floating Tooltip Popup (fixed position to the right of enlarged stats panel)
            GameObject tipObj = new GameObject("StatTooltipPopup", typeof(RectTransform));
            tipObj.transform.SetParent(panelObj.transform, false);
            Image tipBg = tipObj.AddComponent<Image>();
            tipBg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);
            Outline tipOutline = tipObj.AddComponent<Outline>();
            tipOutline.effectColor = new Color(0.25f, 0.30f, 0.40f, 0.60f);
            tipOutline.effectDistance = new Vector2(1f, -1f);

            RectTransform tipRt = tipObj.GetComponent<RectTransform>();
            tipRt.sizeDelta = new Vector2(280f, 90f);
            tipRt.anchorMin = new Vector2(0f, 1f);
            tipRt.anchorMax = new Vector2(0f, 1f);
            tipRt.pivot = new Vector2(0f, 1f);
            tipRt.anchoredPosition = new Vector2(340f, -74f);

            GameObject tipTxtObj = new GameObject("Text", typeof(RectTransform));
            tipTxtObj.transform.SetParent(tipObj.transform, false);
            TextMeshProUGUI tipTxt = tipTxtObj.AddComponent<TextMeshProUGUI>();
            tipTxt.fontSize = 13;
            tipTxt.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            tipTxt.raycastTarget = false;
            tipTxt.textWrappingMode = TextWrappingModes.Normal;
            RectTransform tipTxtRt = tipTxtObj.GetComponent<RectTransform>();
            tipTxtRt.anchorMin = Vector2.zero;
            tipTxtRt.anchorMax = Vector2.one;
            tipTxtRt.offsetMin = new Vector2(10f, 8f);
            tipTxtRt.offsetMax = new Vector2(-10f, -8f);

            TooltipPopup tipPopup = tipObj.AddComponent<TooltipPopup>();
            tipPopup.panel = tipObj;
            tipPopup.text = tipTxt;
            tipPopup.rectTransform = tipRt;
            tipPopup.SetFixedPosition(new Vector2(340f, -74f));
            tipObj.SetActive(false);
            ui.statTooltipPopup = tipPopup;

            Vector2 statIconSize = new Vector2(44f, 44f);
            // 7 Stat Rows with Icon Sprites and Tooltips
            ui.playerHpText = StatRowFactory.Create(playerStatsObj.transform, StatIconDatabase.HpIcon, "HP", new Color(0.75f, 0.15f, 0.20f, 0.95f), "Health Points (HP):\nYour vital stamina. If reduced to 0, corruption activates.", "30 / 30", new Color(0.35f, 0.90f, 0.45f, 1f), tipPopup, statIconSize);
            ui.playerUpperClothesText = StatRowFactory.Create(playerStatsObj.transform, StatIconDatabase.TopArmorIcon, "U-ARM", new Color(0.20f, 0.55f, 0.85f, 0.95f), "Upper Armor (U-ARM):\nDurability of your upper garment. Protects your chest and provides damage reduction.", "65 / 65", new Color(0.40f, 0.80f, 1f, 1f), tipPopup, statIconSize);
            ui.playerLowerClothesText = StatRowFactory.Create(playerStatsObj.transform, StatIconDatabase.BottomArmorIcon, "L-ARM", new Color(0.18f, 0.45f, 0.75f, 0.95f), "Lower Armor (L-ARM):\nDurability of your lower garment. Protects your legs and modesty, and provides damage reduction.", "65 / 65", new Color(0.40f, 0.80f, 1f, 1f), tipPopup, statIconSize);
            ui.playerClothesText = ui.playerUpperClothesText;
            ui.playerShieldText = StatRowFactory.Create(playerStatsObj.transform, StatIconDatabase.BlockIcon, "SHD", new Color(0.12f, 0.55f, 0.65f, 0.95f), "Shield Guard:\nAbsorbs incoming physical damage before Health is affected.", "0", new Color(0.40f, 0.90f, 1f, 1f), tipPopup, statIconSize);
            ui.playerSeductionText = StatRowFactory.Create(playerStatsObj.transform, StatIconDatabase.LustIcon, "LUST", new Color(0.75f, 0.20f, 0.55f, 0.95f), "Lust / Seduction:\nReaching 100% triggers climax submission and adds a Climax stack!", "0%  (0/3)", new Color(1f, 0.45f, 0.75f, 1f), tipPopup, statIconSize);
            ui.playerApText = StatRowFactory.Create(playerStatsObj.transform, StatIconDatabase.ApIcon, "AP", new Color(0.80f, 0.65f, 0.12f, 0.95f), "Action Points (AP):\nEnergy spent to play cards each turn. Fully restored every turn.", "3 / 3", new Color(1f, 0.85f, 0.25f, 1f), tipPopup, statIconSize);
            ui.playerCorruptionText = StatRowFactory.Create(playerStatsObj.transform, StatIconDatabase.CorruptionIcon, "CORR", new Color(0.50f, 0.15f, 0.65f, 0.95f), "Corruption:\nDefeat adds +1 Corruption and preserves you with 1 HP. If it reaches 3/3, you are fully consumed!", "0 / 3", new Color(0.85f, 0.45f, 0.95f, 1f), tipPopup, statIconSize);

            // --- 4c. Bottom-Left Collapsible Combat Log Panel ---
            GameObject logPanelObj = new GameObject("CombatLogPanel", typeof(RectTransform));
            logPanelObj.transform.SetParent(panelObj.transform, false);
            Image logBg = logPanelObj.AddComponent<Image>();
            logBg.color = new Color(0.06f, 0.08f, 0.11f, 0.92f);
            RectTransform logRt = logPanelObj.GetComponent<RectTransform>();
            logRt.anchorMin = new Vector2(0f, 0f);
            logRt.anchorMax = new Vector2(0f, 0f);
            logRt.pivot = new Vector2(0f, 0f);
            logRt.anchoredPosition = new Vector2(25f, 25f);
            logRt.sizeDelta = new Vector2(360f, 195f);
            ui.combatLogPanel = logPanelObj;

            // Header Bar
            GameObject logHeader = new GameObject("Header", typeof(RectTransform));
            logHeader.transform.SetParent(logPanelObj.transform, false);
            Image lhBg = logHeader.AddComponent<Image>();
            lhBg.color = new Color(0.10f, 0.13f, 0.18f, 0.95f);
            RectTransform lhr = logHeader.GetComponent<RectTransform>();
            lhr.anchorMin = new Vector2(0f, 1f);
            lhr.anchorMax = new Vector2(1f, 1f);
            lhr.pivot = new Vector2(0.5f, 1f);
            lhr.anchoredPosition = Vector2.zero;
            lhr.sizeDelta = new Vector2(0f, 34f);

            GameObject lhTxtObj = new GameObject("Title", typeof(RectTransform));
            lhTxtObj.transform.SetParent(logHeader.transform, false);
            TextMeshProUGUI lhTxt = lhTxtObj.AddComponent<TextMeshProUGUI>();
            lhTxt.text = "COMBAT LOG";
            lhTxt.fontSize = 15;
            lhTxt.fontStyle = FontStyles.Bold;
            lhTxt.color = new Color(0.95f, 0.85f, 0.45f, 1f);
            lhTxt.alignment = TextAlignmentOptions.MidlineLeft;
            lhTxt.raycastTarget = false;
            RectTransform lhtr = lhTxtObj.GetComponent<RectTransform>();
            lhtr.anchorMin = new Vector2(0f, 0f);
            lhtr.anchorMax = new Vector2(1f, 1f);
            lhtr.offsetMin = new Vector2(12f, 0f);
            lhtr.offsetMax = new Vector2(-100f, 0f);

            // Toggle Button on Header
            GameObject toggleBtnObj = new GameObject("ToggleBtn", typeof(RectTransform));
            toggleBtnObj.transform.SetParent(logHeader.transform, false);
            Image tbBg = toggleBtnObj.AddComponent<Image>();
            tbBg.color = new Color(0.18f, 0.22f, 0.30f, 1f);
            Button tbBtn = toggleBtnObj.AddComponent<Button>();
            tbBtn.targetGraphic = tbBg;
            RectTransform tbr = toggleBtnObj.GetComponent<RectTransform>();
            tbr.anchorMin = new Vector2(1f, 0.5f);
            tbr.anchorMax = new Vector2(1f, 0.5f);
            tbr.pivot = new Vector2(1f, 0.5f);
            tbr.anchoredPosition = new Vector2(-6f, 0f);
            tbr.sizeDelta = new Vector2(86f, 24f);

            GameObject tbTxtObj = new GameObject("Text", typeof(RectTransform));
            tbTxtObj.transform.SetParent(toggleBtnObj.transform, false);
            TextMeshProUGUI tbTxt = tbTxtObj.AddComponent<TextMeshProUGUI>();
            tbTxt.text = "v Hide";
            tbTxt.fontSize = 13;
            tbTxt.fontStyle = FontStyles.Bold;
            tbTxt.alignment = TextAlignmentOptions.Center;
            tbTxt.color = Color.white;
            tbTxt.raycastTarget = false;
            RectTransform tbtr = tbTxtObj.GetComponent<RectTransform>();
            tbtr.anchorMin = Vector2.zero;
            tbtr.anchorMax = Vector2.one;
            tbtr.sizeDelta = Vector2.zero;

            tbBtn.onClick.AddListener(() => ui.ToggleCombatLog());
            ui.combatLogToggleBtn = tbBtn;
            ui.combatLogToggleBtnText = tbTxt;

            // Scroll View for Log
            GameObject svObj = new GameObject("ScrollView", typeof(RectTransform));
            svObj.transform.SetParent(logPanelObj.transform, false);
            RectTransform svr = svObj.GetComponent<RectTransform>();
            svr.anchorMin = Vector2.zero;
            svr.anchorMax = Vector2.one;
            svr.offsetMin = new Vector2(6f, 6f);
            svr.offsetMax = new Vector2(-6f, -36f);

            ScrollRect sRect = svObj.AddComponent<ScrollRect>();
            sRect.horizontal = false;
            sRect.vertical = true;
            sRect.movementType = ScrollRect.MovementType.Clamped;
            sRect.scrollSensitivity = 25f;

            // Viewport
            GameObject vpObj = new GameObject("Viewport", typeof(RectTransform));
            vpObj.transform.SetParent(svObj.transform, false);
            RectTransform vpr = vpObj.GetComponent<RectTransform>();
            vpr.anchorMin = Vector2.zero;
            vpr.anchorMax = Vector2.one;
            vpr.sizeDelta = Vector2.zero;
            RectMask2D mask = vpObj.AddComponent<RectMask2D>();
            sRect.viewport = vpr;

            // Content
            GameObject contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(vpObj.transform, false);
            RectTransform crt = contentObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup clg = contentObj.AddComponent<VerticalLayoutGroup>();
            clg.padding = new RectOffset(6, 6, 4, 4);
            clg.spacing = 3f;
            clg.childControlWidth = true;
            clg.childControlHeight = true;
            clg.childForceExpandWidth = true;
            clg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            sRect.content = crt;
            ui.combatLogScrollRect = sRect;
            ui.combatLogContent = crt;

            // --- 5. Enemies Container (Center/Right area - Free placement for Snake layout) ---
            GameObject enemiesObj = new GameObject("EnemiesContainer", typeof(RectTransform));
            enemiesObj.transform.SetParent(panelObj.transform, false);
            RectTransform er = enemiesObj.GetComponent<RectTransform>();
            er.anchorMin = new Vector2(0.5f, 0.50f);
            er.anchorMax = new Vector2(0.5f, 0.50f);
            er.pivot = new Vector2(0.5f, 0.5f);
            er.anchoredPosition = new Vector2(300f, 50f);
            er.sizeDelta = new Vector2(1100f, 760f);
            ui.enemiesContainer = er;

            // --- 6. Cards Hand Container (Lowered close to bottom edge) ---
            GameObject cardsObj = new GameObject("CardsContainer", typeof(RectTransform));
            cardsObj.transform.SetParent(panelObj.transform, false);
            RectTransform cr = cardsObj.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0.5f, 0f);
            cr.anchorMax = new Vector2(0.5f, 0f);
            cr.pivot = new Vector2(0.5f, 0f);
            cr.anchoredPosition = new Vector2(90f, 10f);
            cr.sizeDelta = new Vector2(800f, 215f);

            HorizontalLayoutGroup chlg = cardsObj.AddComponent<HorizontalLayoutGroup>();
            chlg.spacing = 16;
            chlg.childAlignment = TextAnchor.LowerCenter;
            chlg.childControlWidth = false;
            chlg.childControlHeight = false;
            chlg.childForceExpandWidth = false;
            chlg.childForceExpandHeight = false;
            ui.cardsContainer = cr;

            // --- 7. End Turn Button (Bottom Right) ---
            GameObject etObj = new GameObject("EndTurnButton", typeof(RectTransform));
            etObj.transform.SetParent(panelObj.transform, false);
            Image etBg = etObj.AddComponent<Image>();
            etBg.color = new Color(0.85f, 0.35f, 0.20f, 1f);

            Button etBtn = etObj.AddComponent<Button>();
            etBtn.targetGraphic = etBg;
            Navigation etNav = etBtn.navigation;
            etNav.mode = Navigation.Mode.None;
            etBtn.navigation = etNav;

            RectTransform etr = etObj.GetComponent<RectTransform>();
            etr.anchorMin = new Vector2(1f, 0f);
            etr.anchorMax = new Vector2(1f, 0f);
            etr.pivot = new Vector2(1f, 0f);
            etr.anchoredPosition = new Vector2(-40f, 40f);
            etr.sizeDelta = new Vector2(180f, 65f);

            GameObject etTxtObj = new GameObject("Text", typeof(RectTransform));
            etTxtObj.transform.SetParent(etObj.transform, false);
            TextMeshProUGUI etTxt = etTxtObj.AddComponent<TextMeshProUGUI>();
            etTxt.text = "END TURN";
            etTxt.fontSize = 22;
            etTxt.fontStyle = FontStyles.Bold;
            etTxt.alignment = TextAlignmentOptions.Center;
            etTxt.color = Color.white;
            etTxt.raycastTarget = false;
            RectTransform ettr = etTxtObj.GetComponent<RectTransform>();
            ettr.anchorMin = Vector2.zero;
            ettr.anchorMax = Vector2.one;
            ettr.sizeDelta = Vector2.zero;
            // Glowing Focus Border on End Turn Button
            GameObject etBorderObj = new GameObject("FocusBorder", typeof(RectTransform));
            etBorderObj.transform.SetParent(etObj.transform, false);
            Image etBorderImg = etBorderObj.AddComponent<Image>();
            etBorderImg.color = new Color(1f, 0.85f, 0.25f, 0.95f);
            etBorderImg.raycastTarget = false;
            RectTransform etbRt = etBorderObj.GetComponent<RectTransform>();
            etbRt.anchorMin = Vector2.zero;
            etbRt.anchorMax = Vector2.one;
            etbRt.offsetMin = new Vector2(-4f, -4f);
            etbRt.offsetMax = new Vector2(4f, 4f);
            etBorderObj.transform.SetAsFirstSibling();
            etBorderObj.SetActive(false);
            ui.endTurnFocusBorder = etBorderObj;
            ui.endTurnRect = etr;

            // Pointer hover handler for mouse sync
            etObj.AddComponent<EndTurnButtonUI>();

            ui.endTurnButton = etBtn;

            // --- 8. Outcome Modal (Popup Dialog Centered) ---
            GameObject modalObj = new GameObject("OutcomeModal", typeof(RectTransform));
            modalObj.transform.SetParent(panelObj.transform, false);
            Image mBg = modalObj.AddComponent<Image>();
            mBg.color = new Color(0.10f, 0.12f, 0.17f, 0.98f);

            Canvas modalCanvas = modalObj.AddComponent<Canvas>();
            modalCanvas.overrideSorting = true;
            modalCanvas.sortingOrder = 200;
            modalObj.AddComponent<GraphicRaycaster>();

            RectTransform mr = modalObj.GetComponent<RectTransform>();
            mr.anchorMin = new Vector2(0.5f, 0.5f);
            mr.anchorMax = new Vector2(0.5f, 0.5f);
            mr.pivot = new Vector2(0.5f, 0.5f);
            mr.anchoredPosition = Vector2.zero;
            mr.sizeDelta = new Vector2(680f, 440f);

            // Title
            GameObject mtObj = new GameObject("Title", typeof(RectTransform));
            mtObj.transform.SetParent(modalObj.transform, false);
            TextMeshProUGUI mTitle = mtObj.AddComponent<TextMeshProUGUI>();
            mTitle.fontSize = 28;
            mTitle.fontStyle = FontStyles.Bold;
            mTitle.alignment = TextAlignmentOptions.Center;
            mTitle.color = new Color(0.95f, 0.85f, 0.50f, 1f);
            mTitle.raycastTarget = false;
            RectTransform mtr = mtObj.GetComponent<RectTransform>();
            mtr.anchorMin = new Vector2(0f, 1f);
            mtr.anchorMax = new Vector2(1f, 1f);
            mtr.pivot = new Vector2(0.5f, 1f);
            mtr.anchoredPosition = new Vector2(0f, -20f);
            mtr.sizeDelta = new Vector2(-40f, 40f);
            ui.outcomeTitleText = mTitle;

            // Desc
            GameObject mdObj = new GameObject("Desc", typeof(RectTransform));
            mdObj.transform.SetParent(modalObj.transform, false);
            TextMeshProUGUI mDesc = mdObj.AddComponent<TextMeshProUGUI>();
            mDesc.fontSize = 20;
            mDesc.alignment = TextAlignmentOptions.TopLeft;
            mDesc.color = new Color(0.85f, 0.88f, 0.95f, 1f);
            mDesc.lineSpacing = 10;
            mDesc.raycastTarget = false;
            RectTransform mdr = mdObj.GetComponent<RectTransform>();
            mdr.anchorMin = new Vector2(0f, 0f);
            mdr.anchorMax = new Vector2(1f, 1f);
            mdr.pivot = new Vector2(0.5f, 0.5f);
            mdr.anchoredPosition = new Vector2(0f, 40f);
            mdr.sizeDelta = new Vector2(-50f, -200f);
            ui.outcomeDescText = mDesc;

            // Prana Progress Strip on Outcome Modal
            GameObject pranaStrip = new GameObject("PranaStrip", typeof(RectTransform));
            pranaStrip.transform.SetParent(modalObj.transform, false);
            RectTransform psrt = pranaStrip.GetComponent<RectTransform>();
            psrt.anchorMin = new Vector2(0f, 0f);
            psrt.anchorMax = new Vector2(1f, 0f);
            psrt.pivot = new Vector2(0.5f, 0f);
            psrt.anchoredPosition = new Vector2(0f, 85f);
            psrt.sizeDelta = new Vector2(-40f, 36f);

            HorizontalLayoutGroup pshlg = pranaStrip.AddComponent<HorizontalLayoutGroup>();
            pshlg.spacing = 12;
            pshlg.childAlignment = TextAnchor.MiddleCenter;
            pshlg.childControlWidth = false;
            pshlg.childControlHeight = false;
            pshlg.childForceExpandWidth = false;
            pshlg.childForceExpandHeight = false;

            GameObject pTxtObj = new GameObject("PranaText", typeof(RectTransform));
            pTxtObj.transform.SetParent(pranaStrip.transform, false);
            RectTransform ptrt = pTxtObj.GetComponent<RectTransform>();
            ptrt.sizeDelta = new Vector2(400f, 32f);
            TextMeshProUGUI pTxt = pTxtObj.AddComponent<TextMeshProUGUI>();
            pTxt.fontSize = 15;
            pTxt.fontStyle = FontStyles.Bold;
            pTxt.alignment = TextAlignmentOptions.MidlineLeft;
            pTxt.color = new Color(0.9f, 0.85f, 1f, 1f);
            ui.outcomePranaText = pTxt;

            ui.outcomeDebugFillGaugeBtn = CreateHeaderActionButton(pranaStrip.transform, "OutcomeDebugFillBtn", "[DBG] Fill Gauge", new Color(0.60f, 0.20f, 0.50f, 0.95f), new Vector2(130f, 32f));

            // Buttons Container
            GameObject btnBox = new GameObject("ButtonsBox", typeof(RectTransform));
            btnBox.transform.SetParent(modalObj.transform, false);
            RectTransform bbr = btnBox.GetComponent<RectTransform>();
            bbr.anchorMin = new Vector2(0f, 0f);
            bbr.anchorMax = new Vector2(1f, 0f);
            bbr.pivot = new Vector2(0.5f, 0f);
            bbr.anchoredPosition = new Vector2(0f, 25f);
            bbr.sizeDelta = new Vector2(-40f, 50f);

            HorizontalLayoutGroup mhlg = btnBox.AddComponent<HorizontalLayoutGroup>();
            mhlg.spacing = 15;
            mhlg.childAlignment = TextAnchor.MiddleCenter;
            mhlg.childControlWidth = true;
            mhlg.childControlHeight = true;
            mhlg.childForceExpandWidth = true;
            mhlg.childForceExpandHeight = true;

            // Btn 1
            ui.primaryOutcomeBtn = CreateModalButton(btnBox.transform, "Btn1", new Color(0.22f, 0.55f, 0.35f, 1f), out ui.primaryOutcomeBtnText);
            // Btn 2
            ui.secondaryOutcomeBtn = CreateModalButton(btnBox.transform, "Btn2", new Color(0.65f, 0.25f, 0.55f, 1f), out ui.secondaryOutcomeBtnText);
            // Btn 3
            ui.tertiaryOutcomeBtn = CreateModalButton(btnBox.transform, "Btn3", new Color(0.75f, 0.55f, 0.20f, 1f), out ui.tertiaryOutcomeBtnText);

            modalObj.SetActive(false);
            ui.outcomeModal = modalObj;

            ui.BindManager(manager);
            panelObj.SetActive(false);
            return ui;
        }

        private static Button CreateModalButton(Transform parent, string name, Color color, out TextMeshProUGUI txt)
        {
            GameObject bObj = new GameObject(name, typeof(RectTransform));
            bObj.transform.SetParent(parent, false);
            Image bg = bObj.AddComponent<Image>();
            bg.color = color;

            Button btn = bObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            GameObject tObj = new GameObject("Text", typeof(RectTransform));
            tObj.transform.SetParent(bObj.transform, false);
            txt = tObj.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 18;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;
            RectTransform tr = tObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;
            return btn;
        }

        private static Button CreateHeaderActionButton(Transform parent, string objName, string label, Color bgColor, Vector2 size)
        {
            GameObject btnObj = new GameObject(objName, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image bg = btnObj.AddComponent<Image>();
            bg.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 15;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;

            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            return btn;
        }

        private static Sprite LoadUISprite(string fileName)
        {
#if UNITY_EDITOR
            string path = $"Assets/sprites/UI/{fileName}";
            Sprite spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr != null) return spr;
            UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }
    }
}
