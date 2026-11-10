using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnbridledSanctum.Combat;
using UnbridledSanctum.Data;

namespace UnbridledSanctum.Combat
{
    public enum CombatResultType
    {
        VictoryHp,
        VictorySeduction,
        DefeatHp,
        DefeatSeduction,
        CorruptionSurvival
    }

    public class CombatResult
    {
        public CombatResultType type;
        public Combatant lastSeducedEnemy;
        public int lumieresEarned;
        public int lustidEarned;
        public int goldEarned => lumieresEarned;
        public int totalEnemies;
        public int seducedEnemiesCount;
        public int defeatedHpEnemiesCount;
        public bool isPureSeduction => totalEnemies > 0 && seducedEnemiesCount == totalEnemies;
        public bool isHybrid => seducedEnemiesCount > 0 && defeatedHpEnemiesCount > 0;
        public bool isPureMartialRun; // True if won with zero lust cards played and zero monster seed absorbed
    }

    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        public bool isPureRunActive = true;
        public event Action<bool> OnPureStatusChanged;

        public Combatant player { get; private set; }
        public List<Combatant> enemies { get; private set; } = new List<Combatant>();
        public List<CardInstance> playerDeck { get; private set; } = new List<CardInstance>();
        public List<CardData> unlockedCards { get; private set; } = new List<CardData>();
        public const int MAX_DECK_SIZE = 9;

        public int selectedEnemyIndex { get; private set; } = 0;
        public bool isCombatActive { get; private set; } = false;
        public bool isPlayerTurn { get; private set; } = true;
        public bool isActionInProgress { get; private set; } = false;
        public Combatant currentTurnCombatant { get; private set; }

        public CharacterState characterState { get; private set; }
        public ItemData tornUpperClothesItem { get; private set; }
        public ItemData tornLowerClothesItem { get; private set; }
        public ItemData tornClothesItem => tornUpperClothesItem ?? tornLowerClothesItem;

        [Header("Persistent Player Stats Across Expedition")]
        public int persistentHp = 30;
        public int persistentMaxHp = 30;
        public int persistentUpperClothesHp = 65;
        public int persistentMaxUpperClothesHp = 65;
        public int persistentLowerClothesHp = 65;
        public int persistentMaxLowerClothesHp = 65;
        public int persistentUnderUpperClothesHp = 35;
        public int persistentMaxUnderUpperClothesHp = 35;
        public int persistentUnderLowerClothesHp = 35;
        public int persistentMaxUnderLowerClothesHp = 35;

        public bool IsWearingCombinedClothes
        {
            get
            {
                if (characterState != null && characterState.GetEquippedItem(EquipSlot.CombinedClothes) != null)
                    return true;
                if (player != null && player.isWearingCombinedClothes)
                    return true;
                return false;
            }
        }

        // Legacy compatibility properties
        public int persistentClothesHp => IsWearingCombinedClothes ? persistentUpperClothesHp : (persistentUpperClothesHp + persistentLowerClothesHp);
        public int persistentMaxClothesHp => IsWearingCombinedClothes ? persistentMaxUpperClothesHp : (persistentMaxUpperClothesHp + persistentMaxLowerClothesHp);

        public int persistentSeduction = 0;
        public int persistentSeductionStacks = 0;
        public int persistentShield = 0;
        public int persistentCorruption = 0;
        public int maxCorruption = 3;

        public CombatStance currentStance { get; private set; } = CombatStance.Neutral;
        public LustPathType activeLustPath { get; private set; } = LustPathType.PhallusDevotee;
        public List<CardInstance> equippedDeck { get; private set; } = new List<CardInstance>();
        public List<CardData> baseWarriorDeck { get; private set; } = new List<CardData>();
        public int persistentSemenVolumeMl = 0;
        public int persistentSemenMouthMl = 0;
        public const int MAX_SEMEN_MOUTH_ML = 100;
        public int persistentSemenWombMl = 0;
        public const int MAX_SEMEN_WOMB_ML = 200;
        public int persistentSemenAnalMl = 0;
        public const int MAX_SEMEN_ANAL_ML = 150;

        public void AddPersistentSemen(int amountMl, PenetrationSlot slot = PenetrationSlot.None)
        {
            if (amountMl <= 0) return;
            persistentSemenVolumeMl += amountMl;
            switch (slot)
            {
                case PenetrationSlot.Mouth:
                    persistentSemenMouthMl = Mathf.Min(MAX_SEMEN_MOUTH_ML, persistentSemenMouthMl + amountMl);
                    break;
                case PenetrationSlot.Anal:
                    persistentSemenAnalMl = Mathf.Min(MAX_SEMEN_ANAL_ML, persistentSemenAnalMl + amountMl);
                    break;
                case PenetrationSlot.Vagina:
                default:
                    persistentSemenWombMl = Mathf.Min(MAX_SEMEN_WOMB_ML, persistentSemenWombMl + amountMl);
                    break;
            }
            OnPersistentStatsChanged?.Invoke();
        }
        public ExternalFinishZone activeControlZone = ExternalFinishZone.None;
        private Dictionary<string, int> cardCooldowns = new Dictionary<string, int>();

        [Header("Customizable Content Databases (Inspector)")]
        [Tooltip("Custom starter cards (drag CardSO assets here to configure starter deck).")]
        public List<CardSO> startingDeckAssets = new List<CardSO>();

        [Tooltip("All available cards pool (drag CardSO assets here).")]
        public List<CardSO> cardPoolAssets = new List<CardSO>();

        [Tooltip("Custom enemy definitions with stats and skill sets.")]
        public List<EnemyDefinitionSO> enemyDefinitions = new List<EnemyDefinitionSO>();

        public event Action OnCombatStarted;
        public event Action<bool> OnTurnChanged; // true = player, false = enemy
        public event Action<Combatant> OnCombatantTurnStarted;
        public event Action<Combatant> OnCombatantTurnEnded;
        public event Action<CardInstance, Combatant> OnPlayerSkillTelegraphed;
        public event Action<CardInstance, Combatant> OnPlayerSkillExecuted;
        public event Action OnPlayerActionFinished;
        public event Action<CardInstance, Combatant> OnCardPlayed;
        public event Action<Combatant, EnemySkill> OnEnemySkillTelegraphed;
        public event Action<Combatant, EnemySkill> OnEnemySkillUsed;
        public event Action<CombatResult> OnCombatEnded;
        public event Action<string> OnCombatLogMessage;
        public event Action<Combatant, int, int> OnDamageDealt; // (target, hpDamage, absorbedShield)
        public event Action<Combatant, int> OnSeductionDealt; // (target, seductionAmount)
        public event Action<Combatant, int> OnClothesDamaged; // (target, clothesDamage)
        public event Action<Combatant, int> OnSeductionPhaseChanged; // (combatant, newPhase)
        public event Action OnPlayerClothesStripped;
        public event Action<bool, bool> OnPlayerGarmentStripped; // (bool upperStripped, bool lowerStripped)
        public event Action OnPersistentStatsChanged;
        public event Action<CombatStance> OnStanceChanged;
        public event Action<LustPathType> OnLustPathChanged;
        public event Action OnHandUpdated;

        [Header("Sexual Mechanics & CG Pacing")]
        [Tooltip("Delay between sexual movements for future mini-CG animations.")]
        public float sexualMovementPauseDuration = 1.4f;

        [Tooltip("Dedicated duration for enemy ejaculation sequence, camera shake, and orgasm CG.")]
        public float ejaculationPauseDuration = 2.4f;

        public event Action<Combatant, EnemySkill, PenetrationSlot> OnSexualMovementExecuted;
        public event Action<Combatant, PenetrationSlot, int> OnEnemyEjaculated; // (enemy, slot, pranaMl)
        public event Action<float, float> OnCameraShakeRequested; // (intensity, duration)

        // Extensibility hooks for future mini-CG systems
        public Func<Combatant, EnemySkill, PenetrationSlot, IEnumerator> CustomSexualMovementCGHandler;
        public Func<Combatant, PenetrationSlot, int, IEnumerator> CustomEjaculationCGHandler;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            DesireTreeManager.EnsureExists();

            int savedPath = PlayerPrefs.GetInt("Sanctum_ActiveLustPath", (int)LustPathType.PhallusDevotee);
            activeLustPath = (LustPathType)savedPath;

            InitializeStarterDeck();
        }

        private void OnDestroy()
        {
            if (characterState != null)
            {
                characterState.OnEquipmentChanged -= HandleEquipmentChanged;
            }
        }

        public void Initialize(CharacterState state)
        {
            if (characterState != null)
            {
                characterState.OnEquipmentChanged -= HandleEquipmentChanged;
            }
            characterState = state;
            if (characterState != null)
            {
                characterState.OnEquipmentChanged += HandleEquipmentChanged;
            }
            RecalculatePersistentStats(false);
        }

        private void HandleEquipmentChanged()
        {
            RecalculatePersistentStats(false);
            if (player != null)
            {
                SyncEquippedItemsToPlayer();
                player.RecalculateStats();
            }
            OnPersistentStatsChanged?.Invoke();
        }

        private void InitializeStarterDeck()
        {
            unlockedCards.Clear();
            playerDeck.Clear();

            HashSet<string> seenCardIds = new HashSet<string>();
            // 1. If custom cardPoolAssets are assigned in Inspector, use them!
            if (cardPoolAssets != null && cardPoolAssets.Count > 0)
            {
                for (int i = 0; i < cardPoolAssets.Count; i++)
                {
                    if (cardPoolAssets[i] != null)
                    {
                        var data = cardPoolAssets[i].ToCardData();
                        if (seenCardIds.Add(data.id))
                            unlockedCards.Add(data);
                    }
                }
            }
            else
            {
                // Fallback default cards - Warrior Tactics
                unlockedCards.Add(new CardData("strike", "Strike", ap: 1, cd: 1, dmg: 7, shield: 0, seduction: 0, CardTarget.SingleEnemy, "Swift sword strike dealing 7 HP damage and 8 Stamina posture damage.", CardCategory.WarriorTactics, LustPathType.None, StanceRequirement.NeutralOnly, staminaDmg: 8));
                unlockedCards.Add(new CardData("heavy_slash", "Heavy Slash", ap: 2, cd: 2, dmg: 16, shield: 0, seduction: 0, CardTarget.SingleEnemy, "Crushing overhead sword strike dealing 16 HP damage and 16 Stamina posture damage.", CardCategory.WarriorTactics, LustPathType.None, StanceRequirement.NeutralOnly, staminaDmg: 16));
                unlockedCards.Add(new CardData("block", "Defend", ap: 1, cd: 1, dmg: 0, shield: 7, seduction: 0, CardTarget.Self, "Raise guard with shield, gaining 7 Shield. (Does not absorb Lust).", CardCategory.WarriorTactics, LustPathType.None, StanceRequirement.Any));
                unlockedCards.Add(new CardData("shield_bash", "Shield Bash", ap: 1, cd: 2, dmg: 5, shield: 4, seduction: 0, CardTarget.SingleEnemy, "Slam opponent with shield rim: deals 5 damage, 12 Stamina posture damage, and gives 4 Shield.", CardCategory.WarriorTactics, LustPathType.None, StanceRequirement.NeutralOnly, staminaDmg: 12));
                unlockedCards.Add(new CardData("iron_fortress", "Iron Fortress", ap: 2, cd: 2, dmg: 0, shield: 15, seduction: 0, CardTarget.Self, "Assume an unyielding defensive stance, gaining 15 Shield.", CardCategory.WarriorTactics, LustPathType.None, StanceRequirement.Any));
                unlockedCards.Add(new CardData("mend_clothes", "Quick Patch", ap: 1, cd: 2, dmg: 0, shield: 0, seduction: 0, CardTarget.Self, "Swiftly adjust and bind garments, restoring 15 clothes durability.", CardCategory.WarriorTactics, LustPathType.None, StanceRequirement.Any));
                unlockedCards.Add(new CardData("cold_focus", "Cold Focus", ap: 1, cd: 2, dmg: 0, shield: 6, seduction: 0, CardTarget.Self, "Focus mind with iron discipline, reducing internal Arousal by 25% and gaining 6 Shield.", CardCategory.WarriorTactics, LustPathType.None, StanceRequirement.Any, arousalSelf: -25));
                unlockedCards.Add(new CardData("disengage_kick", "Disengage Kick", ap: 1, cd: 1, dmg: 5, shield: 0, seduction: 0, CardTarget.SingleEnemy, "Deliver a swift push kick to drive back advancing foes. Deals 5 HP damage, 15 Stamina posture damage, and breaks physical holds.", CardCategory.WarriorTactics, LustPathType.None, StanceRequirement.Any, staminaDmg: 15));

                // Path 1: Phallus Devotee (Lust Instincts - 9 Cards)
                unlockedCards.Add(new CardData("breath_of_ecstasy", "Breath of Ecstasy", ap: 0, cd: 1, dmg: 0, shield: 0, seduction: 0, CardTarget.Self, "Exhale softly to cool burning blood, reducing self Arousal by 15% and steadying your mind.", CardCategory.LustInstinct, LustPathType.PhallusDevotee, StanceRequirement.Any, arousalSelf: -15, ephemeral: true));
                unlockedCards.Add(new CardData("enticing_glance", "Enticing Glance", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 20, CardTarget.SingleEnemy, "Cast a sultry, inviting glance that unsettles the foe's resolve (+20% Arousal).", CardCategory.LustInstinct, LustPathType.PhallusDevotee, StanceRequirement.NeutralOnly, arousalTarget: 20, ephemeral: true));
                unlockedCards.Add(new CardData("throat_lock", "Throat Lock", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 25, CardTarget.SingleEnemy, "Wrap lips and throat firmly around the opponent's shaft to seize control (+25% Arousal, 10 Stamina damage).", CardCategory.LustInstinct, LustPathType.PhallusDevotee, StanceRequirement.PenetratedOnly, staminaDmg: 10, arousalTarget: 25, ephemeral: true, reqSlot: PenetrationSlot.Mouth));
                unlockedCards.Add(new CardData("deep_swallow", "Deep Swallow", ap: 2, cd: 2, dmg: 0, shield: 0, seduction: 40, CardTarget.SingleEnemy, "Take full length deep into your throat: provokes ejaculation (+40% Arousal, absorbs 50ml Cum, heals 8 HP).", CardCategory.LustInstinct, LustPathType.PhallusDevotee, StanceRequirement.PenetratedOnly, arousalTarget: 40, semenMl: 50, ephemeral: true, reqSlot: PenetrationSlot.Mouth));
                unlockedCards.Add(new CardData("vaginal_clench", "Vaginal Clench", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 30, CardTarget.SingleEnemy, "Contract inner walls rhythmically around the shaft, dealing 12 Stamina damage, +30% Arousal, and absorbing 30ml Cum.", CardCategory.LustInstinct, LustPathType.PhallusDevotee, StanceRequirement.PenetratedOnly, staminaDmg: 12, arousalTarget: 30, semenMl: 30, ephemeral: true, reqSlot: PenetrationSlot.Vagina));
                unlockedCards.Add(new CardData("womb_suction", "Womb Suction", ap: 2, cd: 2, dmg: 0, shield: 0, seduction: 45, CardTarget.SingleEnemy, "Engage deep uterine peristalsis around the invading head: strips 20 Stamina, deals +45% Arousal, and extracts 60ml Cum.", CardCategory.LustInstinct, LustPathType.PhallusDevotee, StanceRequirement.PenetratedOnly, staminaDmg: 20, arousalTarget: 45, semenMl: 60, ephemeral: true, reqSlot: PenetrationSlot.Vagina));
                unlockedCards.Add(new CardData("lust_barrier", "Lust Barrier", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 15, CardTarget.Self, "Channel absorbed Cum through the Womb Tattoo, converting up to 25ml Cum into Lust Shield.", CardCategory.LustInstinct, LustPathType.PhallusDevotee, StanceRequirement.Any, arousalTarget: 15, lustShield: 0, ephemeral: true));
                unlockedCards.Add(new CardData("cum_surge", "Cum Surge", ap: 1, cd: 2, dmg: 0, shield: 10, seduction: 0, CardTarget.Self, "Transmute stored Cum into an explosive surge of condensed bio-barrier (+10 Shield, converts up to 40ml Cum into Lust Shield).", CardCategory.LustInstinct, LustPathType.PhallusDevotee, StanceRequirement.Any, lustShield: 0, ephemeral: true));
                unlockedCards.Add(new CardData("overwhelming_surrender", "Overwhelming Surrender", ap: 2, cd: 3, dmg: 0, shield: 0, seduction: 50, CardTarget.SingleEnemy, "Surrender fully to the ecstasy of the embedded shaft: inflicts +50% Arousal, and if holding over 80ml Cum, restores 15 HP.", CardCategory.LustInstinct, LustPathType.PhallusDevotee, StanceRequirement.Any, arousalTarget: 50, ephemeral: true));

                // Path 2: Bound & Submissive (Lust Instincts - 9 Cards)
                unlockedCards.Add(new CardData("yielding_curve", "Yielding Curve", ap: 0, cd: 1, dmg: 0, shield: 10, seduction: 0, CardTarget.Self, "Arch back into an alluring, compliant posture, gaining 10 Shield while internal Arousal increases by 10%.", CardCategory.LustInstinct, LustPathType.BoundSubmissive, StanceRequirement.Any, arousalSelf: 10, ephemeral: true));
                unlockedCards.Add(new CardData("bound_kick", "Bound Kick", ap: 1, cd: 1, dmg: 6, shield: 0, seduction: 0, CardTarget.SingleEnemy, "Deliver a sharp, leveraged kick despite being held, dealing 6 HP damage and 16 Stamina posture damage.", CardCategory.LustInstinct, LustPathType.BoundSubmissive, StanceRequirement.StruggleOnly, staminaDmg: 16, ephemeral: true));
                unlockedCards.Add(new CardData("sweet_captivity", "Sweet Captivity", ap: 1, cd: 2, dmg: 0, shield: 0, seduction: 0, CardTarget.Self, "Embrace the crushing weight of physical confinement, generating 12 Lust Shield and restoring 1 AP next turn.", CardCategory.LustInstinct, LustPathType.BoundSubmissive, StanceRequirement.StruggleOnly, lustShield: 12, ephemeral: true));
                unlockedCards.Add(new CardData("spank_reception", "Spank Reception", ap: 1, cd: 1, dmg: 0, shield: 12, seduction: 25, CardTarget.SingleEnemy, "Turn stinging physical blows into thrilling heat: gain 12 Shield, inflame enemy (+25% Arousal), rising self Arousal (+10%).", CardCategory.LustInstinct, LustPathType.BoundSubmissive, StanceRequirement.Any, arousalSelf: 10, arousalTarget: 25, ephemeral: true));
                unlockedCards.Add(new CardData("flushed_whimper", "Flushed Whimper", ap: 1, cd: 2, dmg: 0, shield: 0, seduction: 25, CardTarget.SingleEnemy, "Let out a breathless, flushed whimper that disarms the foe (+25% Arousal) and dampens their next attack.", CardCategory.LustInstinct, LustPathType.BoundSubmissive, StanceRequirement.Any, arousalTarget: 25, ephemeral: true));
                unlockedCards.Add(new CardData("masochistic_ecstasy", "Masochistic Ecstasy", ap: 2, cd: 2, dmg: 0, shield: 0, seduction: 0, CardTarget.Self, "Convert the humiliation of confinement and torn cloth into pure spiritual resilience (+25 Lust Shield, heals 10 HP, -20% Arousal).", CardCategory.LustInstinct, LustPathType.BoundSubmissive, StanceRequirement.StruggleOnly, lustShield: 25, arousalSelf: -20, ephemeral: true));
                unlockedCards.Add(new CardData("submissive_murmur", "Submissive Murmur", ap: 0, cd: 1, dmg: 0, shield: 0, seduction: 20, CardTarget.SingleEnemy, "Whisper submissive praise directly against the enemy's ear, draining 10 Stamina and adding 20% Arousal.", CardCategory.LustInstinct, LustPathType.BoundSubmissive, StanceRequirement.Any, staminaDmg: 10, arousalTarget: 20, ephemeral: true));
                unlockedCards.Add(new CardData("wrist_lock_reverse", "Wrist Lock Reverse", ap: 1, cd: 2, dmg: 8, shield: 0, seduction: 0, CardTarget.SingleEnemy, "Use the opponent's own pinning grip to wrench their wrist: deals 8 HP dmg, 18 Stamina damage, and breaks their hold.", CardCategory.LustInstinct, LustPathType.BoundSubmissive, StanceRequirement.StruggleOnly, staminaDmg: 18, ephemeral: true));
                unlockedCards.Add(new CardData("unbreakable_leash", "Unbreakable Leash", ap: 2, cd: 3, dmg: 0, shield: 0, seduction: 45, CardTarget.SingleEnemy, "Wrap limbs and bindings tightly around the foe in an erotic lock, stripping 15 Stamina and inflicting +45% Arousal.", CardCategory.LustInstinct, LustPathType.BoundSubmissive, StanceRequirement.StruggleOnly, staminaDmg: 15, arousalTarget: 45, ephemeral: true));

                // Path 3: Monster Transformation (Lust Instincts - 9 Cards)
                unlockedCards.Add(new CardData("pheromone_spray", "Pheromone Spray", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 20, CardTarget.SingleEnemy, "Exude sweet, intoxicating succubus pheromones from skin pores (+20% enemy Arousal, 8 Stamina damage).", CardCategory.LustInstinct, LustPathType.MonsterTransformation, StanceRequirement.Any, staminaDmg: 8, arousalTarget: 20, ephemeral: true));
                unlockedCards.Add(new CardData("cushioning_bust", "Cushioning Bust", ap: 1, cd: 1, dmg: 0, shield: 12, seduction: 25, CardTarget.Self, "Press your swelling, sensitive bust against the opponent to cushion force (+12 Shield) while driving them mad (+25% Arousal).", CardCategory.LustInstinct, LustPathType.MonsterTransformation, StanceRequirement.Any, arousalTarget: 25, ephemeral: true));
                unlockedCards.Add(new CardData("milky_relief", "Milky Relief", ap: 2, cd: 2, dmg: 0, shield: 0, seduction: 0, CardTarget.Self, "Release warm maternal secretions to soothe strained muscle and mind: heals 12 HP, restores 15 Stamina, cools Arousal by 25%.", CardCategory.LustInstinct, LustPathType.MonsterTransformation, StanceRequirement.Any, arousalSelf: -25, ephemeral: true));
                unlockedCards.Add(new CardData("womb_pulse", "Womb Pulse", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 30, CardTarget.SingleEnemy, "The demonic Womb Tattoo flares beneath the skin: unleashes an ecstatic shockwave (+30% Arousal) and forms +12 Lust Shield.", CardCategory.LustInstinct, LustPathType.MonsterTransformation, StanceRequirement.Any, arousalTarget: 30, lustShield: 12, ephemeral: true));
                unlockedCards.Add(new CardData("succubus_siphon", "Succubus Siphon", ap: 1, cd: 2, dmg: 0, shield: 0, seduction: 35, CardTarget.SingleEnemy, "Siphon vital heat directly through contact points: deals 15 Stamina damage, +35% Arousal, and regenerates 8 HP.", CardCategory.LustInstinct, LustPathType.MonsterTransformation, StanceRequirement.Any, staminaDmg: 15, arousalTarget: 35, ephemeral: true));
                unlockedCards.Add(new CardData("womb_overdrive", "Womb Overdrive", ap: 2, cd: 3, dmg: 0, shield: 0, seduction: 40, CardTarget.SingleEnemy, "Trigger demonic contractions within the Living Crucible: drains 20 Stamina, +40% Arousal, extracts 50ml Cum, and gives 20 Lust Shield.", CardCategory.LustInstinct, LustPathType.MonsterTransformation, StanceRequirement.PenetratedOnly, staminaDmg: 20, arousalTarget: 40, semenMl: 50, lustShield: 20, ephemeral: true, reqSlot: PenetrationSlot.Vagina));
                unlockedCards.Add(new CardData("tail_caress", "Tail Caress", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 25, CardTarget.SingleEnemy, "A shadowy demonic tail coils around the foe's groin and inner thighs, dealing 12 Stamina posture damage and +25% Arousal.", CardCategory.LustInstinct, LustPathType.MonsterTransformation, StanceRequirement.Any, staminaDmg: 12, arousalTarget: 25, ephemeral: true));
                unlockedCards.Add(new CardData("slime_skin_coating", "Slime Skin Coating", ap: 1, cd: 2, dmg: 0, shield: 15, seduction: 0, CardTarget.Self, "Exude a slick, slippery slime membrane across your body: grants 15 Shield, 10 Lust Shield, and slips out of enemy holds.", CardCategory.LustInstinct, LustPathType.MonsterTransformation, StanceRequirement.Any, lustShield: 10, ephemeral: true));
                unlockedCards.Add(new CardData("primal_metamorphosis", "Primal Metamorphosis", ap: 2, cd: 3, dmg: 0, shield: 0, seduction: 35, CardTarget.Self, "Fully awaken your demonic traits: surge with +25 Lust Shield, heal 12 HP, and send nearby foes into ecstatic frenzy (+35% Arousal).", CardCategory.LustInstinct, LustPathType.MonsterTransformation, StanceRequirement.Any, lustShield: 25, arousalTarget: 35, ephemeral: true));

                // Struggle Actions (Grappled / Penetrated)
                unlockedCards.Add(new CardData("break_free", "Break Free", ap: 1, cd: 0, dmg: 4, shield: 0, seduction: 0, CardTarget.SingleEnemy, "Struggle vigorously with limbs to dislodge the grapple (deals 14 Stamina damage; escapes to Neutral if enemy stamina depletes).", CardCategory.StruggleAction, LustPathType.None, StanceRequirement.StruggleOnly, staminaDmg: 14));
                unlockedCards.Add(new CardData("sensual_caress", "Sensual Caress", ap: 1, cd: 0, dmg: 0, shield: 0, seduction: 35, CardTarget.SingleEnemy, "Caress sensitive flanks and thighs, driving the opponent into ecstatic frenzy (+35% enemy Arousal, +10% self Arousal).", CardCategory.StruggleAction, LustPathType.None, StanceRequirement.StruggleOnly, arousalSelf: 10, arousalTarget: 35));
                unlockedCards.Add(new CardData("shift_leverage", "Shift Leverage", ap: 1, cd: 1, dmg: 0, shield: 8, seduction: 0, CardTarget.Self, "Maneuver your hips and torso to deflect crushing weight, gaining 8 Shield and 8 Lust Shield.", CardCategory.StruggleAction, LustPathType.None, StanceRequirement.StruggleOnly, lustShield: 8));
                unlockedCards.Add(new CardData("struggle_clinch", "Clinch & Hold", ap: 1, cd: 0, dmg: 0, shield: 6, seduction: 15, CardTarget.SingleEnemy, "Clinch close to leverage body weight against the foe: grants 6 Shield, deals 6 Stamina damage, and stirs 15% Arousal.", CardCategory.StruggleAction, LustPathType.None, StanceRequirement.StruggleOnly, staminaDmg: 6, arousalTarget: 15));
                unlockedCards.Add(new CardData("counter_clench", "Counter Clench", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 45, CardTarget.SingleEnemy, "Counter-clamp the embedded shaft with fierce rhythmic spasms, stripping 15 Stamina and dealing 45% Arousal.", CardCategory.StruggleAction, LustPathType.None, StanceRequirement.PenetratedOnly, staminaDmg: 15, arousalTarget: 45, semenMl: 40));

                // External Climax Control Cards (Dictate Climax Zone & Avoid Internal Influx)
                unlockedCards.Add(new CardData("guiding_palms", "Guiding Palms / Handjob", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 35, CardTarget.SingleEnemy, "Wrap both palms rhythmically around the foe's member, milking their stroke to direct climax into your hands (+35% Arousal, 12 Stamina dmg).", CardCategory.LustInstinct, LustPathType.None, StanceRequirement.Any, staminaDmg: 12, arousalTarget: 35, finishZone: ExternalFinishZone.Hands));
                unlockedCards.Add(new CardData("paizuri_clench", "Paizuri Clench", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 40, CardTarget.SingleEnemy, "Squeeze the adversary's shaft snugly between your soft cleavage, guiding their climax onto your chest (+40% Arousal, +15 Lust Shield).", CardCategory.LustInstinct, LustPathType.None, StanceRequirement.Any, arousalTarget: 40, lustShield: 15, finishZone: ExternalFinishZone.Breasts));
                unlockedCards.Add(new CardData("thigh_lock", "Thigh Lock", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 35, CardTarget.SingleEnemy, "Clamp smooth, firm thighs around the enemy's hips and groin, trapping their stroke to climax across your legs (+35% Arousal, +20 Lust Shield).", CardCategory.LustInstinct, LustPathType.None, StanceRequirement.Any, arousalTarget: 35, lustShield: 20, finishZone: ExternalFinishZone.Thighs));
                unlockedCards.Add(new CardData("parted_lips_tease", "Parted Lips Tease", ap: 1, cd: 1, dmg: 0, shield: 0, seduction: 40, CardTarget.SingleEnemy, "Tilt your head back and present parted lips and flushed cheeks, enticing the enemy to discharge across your face (+40% Arousal, +10 Lust Shield).", CardCategory.LustInstinct, LustPathType.None, StanceRequirement.Any, arousalTarget: 40, lustShield: 10, finishZone: ExternalFinishZone.Face));
                unlockedCards.Add(new CardData("milking_stroke", "Milking Stroke", ap: 2, cd: 2, dmg: 0, shield: 0, seduction: 60, CardTarget.SingleEnemy, "Frantically milk the opponent's shaft with expert strokes, instantly forcing an external climax! (+60% Arousal, triggers external finish).", CardCategory.LustInstinct, LustPathType.None, StanceRequirement.Any, arousalTarget: 60, finishZone: ExternalFinishZone.Hands, isFinisher: true));
            }

            // 2. Populate baseWarriorDeck (load saved deck if available, else fallback to starter)
            baseWarriorDeck.Clear();
            string savedDeck = PlayerPrefs.GetString("Sanctum_PlayerEquippedDeck", "");
            if (!string.IsNullOrEmpty(savedDeck))
            {
                string[] parts = savedDeck.Split(',');
                foreach (var id in parts)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    var cd = unlockedCards.Find(c => c.id == id.Trim());
                    if (cd != null && !baseWarriorDeck.Exists(c => c.id == cd.id))
                    {
                        baseWarriorDeck.Add(cd);
                    }
                }
            }

            if (baseWarriorDeck.Count == 0)
            {
                if (startingDeckAssets != null && startingDeckAssets.Count > 0)
                {
                    HashSet<string> seenDeckIds = new HashSet<string>();
                    for (int i = 0; i < startingDeckAssets.Count; i++)
                    {
                        if (startingDeckAssets[i] != null)
                        {
                            var data = startingDeckAssets[i].ToCardData();
                            if (seenDeckIds.Add(data.id))
                                baseWarriorDeck.Add(data);
                        }
                    }
                }
                else
                {
                    // Default 4 warrior starter cards
                    string[] defaultStarter = { "strike", "heavy_slash", "block", "shield_bash" };
                    foreach (var id in defaultStarter)
                    {
                        var cd = unlockedCards.Find(c => c.id == id);
                        if (cd != null) baseWarriorDeck.Add(cd);
                    }
                }
            }

            equippedDeck.Clear();
            foreach (var cd in baseWarriorDeck)
            {
                equippedDeck.Add(new CardInstance(cd));
            }

            playerDeck.Clear();
            foreach (var cd in baseWarriorDeck)
            {
                playerDeck.Add(new CardInstance(cd));
            }
        }

        public void SetActiveLustPath(LustPathType path)
        {
            activeLustPath = path;
            PlayerPrefs.SetInt("Sanctum_ActiveLustPath", (int)path);
            PlayerPrefs.Save();
            OnLustPathChanged?.Invoke(activeLustPath);
            RecalculatePersistentStats(false);
            if (isCombatActive && isPlayerTurn)
            {
                BuildTurnHand();
                OnHandUpdated?.Invoke();
            }
        }

        public void TransitionStance(CombatStance newStance)
        {
            if (currentStance == newStance) return;
            currentStance = newStance;
            OnCombatLogMessage?.Invoke($"COMBAT STANCE SHIFTED: {currentStance.ToString().ToUpper()}!");
            OnStanceChanged?.Invoke(currentStance);
            if (isCombatActive && isPlayerTurn)
            {
                BuildTurnHand();
                OnHandUpdated?.Invoke();
            }
        }

        public bool IsSlotOccupied(PenetrationSlot slot)
        {
            if (slot == PenetrationSlot.None || enemies == null) return false;
            return enemies.Exists(e => e != null && !e.isDefeated && e.occupiedSlot == slot);
        }

        public Combatant GetSlotOccupant(PenetrationSlot slot)
        {
            if (slot == PenetrationSlot.None || enemies == null) return null;
            return enemies.Find(e => e != null && !e.isDefeated && e.occupiedSlot == slot);
        }

        public Combatant GetGrapplingEnemy(GrappleZone zone = GrappleZone.None)
        {
            if (enemies == null) return null;
            if (zone == GrappleZone.None)
                return enemies.Find(e => e != null && !e.isDefeated && e.isGrapplingPlayer);
            return enemies.Find(e => e != null && !e.isDefeated && e.isGrapplingPlayer && e.heldGrappleZone == zone);
        }

        public bool IsGrappleOccupied(GrappleZone zone = GrappleZone.None)
        {
            return GetGrapplingEnemy(zone) != null;
        }

        public bool CanGrapple(Combatant enemy, GrappleZone zone = GrappleZone.None)
        {
            if (enemy == null || enemy.isDefeated) return false;
            if (enemy.isGrapplingPlayer && (zone == GrappleZone.None || enemy.heldGrappleZone == zone)) return true;
            return !IsGrappleOccupied(zone);
        }

        public bool AssignGrapple(Combatant enemy, GrappleZone zone = GrappleZone.None)
        {
            if (enemy == null || enemy.isDefeated) return false;
            if (enemy.isGrapplingPlayer && (zone == GrappleZone.None || enemy.heldGrappleZone == zone)) return true;

            var existing = GetGrapplingEnemy(zone);
            if (existing != null && existing != enemy)
            {
                string zoneTag = zone == GrappleZone.Upper ? "face and upper body" : "thighs and lower body";
                OnCombatLogMessage?.Invoke($"{enemy.name} attempts to pin you, but {existing.name} is already locking your {zoneTag}!");
                return false;
            }

            enemy.isGrapplingPlayer = true;
            enemy.heldGrappleZone = (zone != GrappleZone.None) ? zone : GrappleZone.Lower;
            enemy.occupiedSlot = PenetrationSlot.None;
            TransitionStance(CombatStance.Grappled);

            string zoneLog = enemy.heldGrappleZone == GrappleZone.Upper 
                ? $"{enemy.name} pins your head and face with her hands and weight! [Face Grapple -> Oral Path]" 
                : $"{enemy.name} scissor-locks her thighs tightly around your waist and legs! [Thigh Grapple -> Vaginal Path]";
            OnCombatLogMessage?.Invoke(zoneLog);
            return true;
        }

        public bool CanPenetrateSlot(Combatant enemy, PenetrationSlot slot)
        {
            if (enemy == null || !enemy.canPenetrate || slot == PenetrationSlot.None) return false;
            if (IsSlotOccupied(slot)) return false; // Strictly 1 enemy per slot

            switch (slot)
            {
                case PenetrationSlot.Vagina:
                case PenetrationSlot.Anal:
                    return player != null && player.isLowerTorn;
                case PenetrationSlot.Mouth:
                    return true;
                default:
                    return false;
            }
        }

        public List<PenetrationSlot> GetAvailablePenetrationSlots(Combatant enemy = null)
        {
            var list = new List<PenetrationSlot>();
            if (player == null) return list;
            if (enemy != null && !enemy.canPenetrate) return list;

            if (player.isLowerTorn)
            {
                if (!IsSlotOccupied(PenetrationSlot.Vagina)) list.Add(PenetrationSlot.Vagina);
                if (!IsSlotOccupied(PenetrationSlot.Anal)) list.Add(PenetrationSlot.Anal);
            }

            if (!IsSlotOccupied(PenetrationSlot.Mouth))
            {
                list.Add(PenetrationSlot.Mouth);
            }

            return list;
        }

        public void AssignPenetration(Combatant enemy, PenetrationSlot slot)
        {
            if (enemy == null || slot == PenetrationSlot.None) return;
            if (!enemy.canPenetrate)
            {
                AssignGrapple(enemy);
                return;
            }

            if (IsSlotOccupied(slot))
            {
                OnCombatLogMessage?.Invoke($"Your {slot.ToString().ToUpper()} is already occupied! {enemy.name} cannot enter!");
                AssignGrapple(enemy);
                return;
            }

            enemy.occupiedSlot = slot;
            enemy.isGrapplingPlayer = false;
            enemy.heldGrappleZone = GrappleZone.None;
            TransitionStance(CombatStance.Penetrated);
            OnCombatLogMessage?.Invoke($"{enemy.name} forcefully penetrates your vessel's {slot.ToString().ToUpper()}! [Slot Claimed]");
        }

        public void ReleaseEnemyHold(Combatant enemy)
        {
            if (enemy == null) return;
            PenetrationSlot prevSlot = enemy.occupiedSlot;
            bool wasGrappling = enemy.isGrapplingPlayer;

            enemy.occupiedSlot = PenetrationSlot.None;
            enemy.isGrapplingPlayer = false;
            enemy.heldGrappleZone = GrappleZone.None;

            if (prevSlot != PenetrationSlot.None)
            {
                OnCombatLogMessage?.Invoke($"{enemy.name} was expelled from your {prevSlot.ToString().ToUpper()}! [Slot Freed]");
            }
            else if (wasGrappling)
            {
                OnCombatLogMessage?.Invoke($"{enemy.name}'s hold was broken!");
            }

            UpdateStanceFromCombatants();
        }

        public void ReleaseAllEnemyHolds()
        {
            if (enemies != null)
            {
                foreach (var e in enemies)
                {
                    if (e != null)
                    {
                        e.occupiedSlot = PenetrationSlot.None;
                        e.isGrapplingPlayer = false;
                        e.heldGrappleZone = GrappleZone.None;
                    }
                }
            }
            TransitionStance(CombatStance.Neutral);
        }

        public void UpdateStanceFromCombatants()
        {
            bool anyPenetrating = enemies != null && enemies.Exists(e => !e.isDefeated && e.occupiedSlot != PenetrationSlot.None);
            bool anyGrappling = enemies != null && enemies.Exists(e => !e.isDefeated && e.isGrapplingPlayer);

            if (anyPenetrating)
            {
                TransitionStance(CombatStance.Penetrated);
            }
            else if (anyGrappling)
            {
                TransitionStance(CombatStance.Grappled);
            }
            else
            {
                TransitionStance(CombatStance.Neutral);
            }
        }

        public List<CardData> GetLustCardsForPath(LustPathType path)
        {
            var list = unlockedCards.FindAll(c => c.category == CardCategory.LustInstinct && 
                c.requiredPath == path &&
                (DesireTreeManager.Instance == null || DesireTreeManager.Instance.IsCardUnlocked(c.id)));

            // Fallback: If no cards are unlocked yet, return at least the first base card of the path
            if (list.Count == 0)
            {
                var baseCard = unlockedCards.Find(c => c.category == CardCategory.LustInstinct && c.requiredPath == path);
                if (baseCard != null) list.Add(baseCard);
            }

            return list;
        }

        public void BuildTurnHand()
        {
            Dictionary<string, int> cdMap = new Dictionary<string, int>();
            foreach (var c in playerDeck)
            {
                if (c?.data != null) cdMap[c.data.id] = c.currentCooldown;
            }

            playerDeck.Clear();

            // Dynamic base warrior/struggle slots and lust instinct slots (GDD Section 5)
            // Tier 1: 0% - 30% Arousal -> 4 warrior/struggle, 0 lust instincts
            // Tier 2: 31% - 60% Arousal -> 4 warrior/struggle, 1 lust instinct
            // Tier 3: 61% - 85% Arousal -> 3 warrior/struggle, 2 lust instincts (instincts cost -1 AP)
            // Tier 4: 86% - 100% Arousal -> 2 warrior/struggle, 3 lust instincts (weapon attacks cost +1 AP)
            int baseActionSlots = 4;
            int lustCardCount = 0;

            if (player != null)
            {
                int curArousal = player.arousal;
                if (curArousal >= 86 || player.seductionStacks >= 3)
                {
                    baseActionSlots = 2;
                    lustCardCount = 3;
                }
                else if (curArousal >= 61 || player.seductionStacks == 2)
                {
                    baseActionSlots = 3;
                    lustCardCount = 2;
                }
                else if (curArousal >= 31 || player.seductionStacks == 1)
                {
                    baseActionSlots = 4;
                    lustCardCount = 1;
                }
                else
                {
                    baseActionSlots = 4;
                    lustCardCount = 0;
                }
            }

            // 1. Stance Swap: In struggle/grapple/penetration, weapon strikes are replaced by Struggle Actions
            if (currentStance != CombatStance.Neutral)
            {
                AddStruggleHand(baseActionSlots);
            }
            else
            {
                // 2. Neutral Stance: Strictly Add Neutral/Any cards from equippedDeck (NEVER Struggle cards!)
                // Strictly filter for Neutral-compatible cards!
                List<CardInstance> neutralValid = equippedDeck.FindAll(c => c?.data != null &&
                    c.data.category != CardCategory.StruggleAction &&
                    (c.data.stanceRequirement == StanceRequirement.Any || c.data.stanceRequirement == StanceRequirement.NeutralOnly));

                int count = Mathf.Min(baseActionSlots, neutralValid.Count);
                for (int i = 0; i < count; i++)
                {
                    var inst = new CardInstance(neutralValid[i].data.Clone());
                    inst.currentCooldown = cardCooldowns.TryGetValue(inst.data.id, out int cd) ? cd : 0;
                    playerDeck.Add(inst);
                }

                if (playerDeck.Count < baseActionSlots)
                {
                    string[] fallbackIds = { "strike", "block", "heavy_slash", "shield_bash" };
                    foreach (var fid in fallbackIds)
                    {
                        if (playerDeck.Count >= baseActionSlots) break;
                        CardData cd = unlockedCards.Find(c => c.id == fid);
                        if (cd != null && !playerDeck.Exists(c => c.data.id == fid))
                        {
                            var inst = new CardInstance(cd.Clone());
                            inst.currentCooldown = cardCooldowns.TryGetValue(fid, out int c) ? c : 0;
                            playerDeck.Add(inst);
                        }
                    }
                }
            }

            // 3. Dynamic Ephemeral Lust Cards from Chosen Path
            if (activeLustPath != LustPathType.None && player != null && lustCardCount > 0)
            {
                List<CardData> selectedLustCards = GetSmartContextLustCards(activeLustPath, lustCardCount);
                for (int i = 0; i < selectedLustCards.Count; i++)
                {
                    CardData baseCard = selectedLustCards[i];
                    CardData ephemeralCard = baseCard.Clone();
                    ephemeralCard.isEphemeral = true;

                    // At Tier 3+ (Arousal >= 61% or seductionStacks >= 2): Lust instinct cards cost -1 AP (min 0)!
                    if (player.arousal >= 61 || player.seductionStacks >= 2)
                    {
                        ephemeralCard.apCost = Mathf.Max(0, ephemeralCard.apCost - 1);
                    }

                    CardInstance inst = new CardInstance(ephemeralCard);
                    inst.currentCooldown = cardCooldowns.TryGetValue(inst.data.id, out int cd) ? cd : 0;
                    playerDeck.Add(inst);
                }
            }

            // High Arousal penalty: Above 85% Arousal (Tier 4), the body trembles from feverish ecstasy: physical attacks cost +1 AP
            if (player != null && (player.arousal >= 86 || player.seductionStacks >= 2))
            {
                foreach (var c in playerDeck)
                {
                    if (c.data.category == CardCategory.WarriorTactics && c.data.hpDamage > 0)
                    {
                        c.data.apCost = Mathf.Max(1, c.data.apCost + 1);
                    }
                }
            }
        }

        /// <summary>
        /// Context-Aware validation: Determines if a lust instinct card is 100% playable in the current bodily state.
        /// </summary>
        public bool IsLustCardPlayableInCurrentContext(CardData card)
        {
            if (card == null) return false;

            // 1. Stance compliance check
            switch (card.stanceRequirement)
            {
                case StanceRequirement.NeutralOnly:
                    if (currentStance != CombatStance.Neutral) return false;
                    break;
                case StanceRequirement.StruggleOnly:
                    if (currentStance == CombatStance.Neutral) return false;
                    break;
                case StanceRequirement.PenetratedOnly:
                    if (currentStance != CombatStance.Penetrated && currentStance != CombatStance.ThroatLock) return false;
                    break;
                case StanceRequirement.Any:
                default:
                    break;
            }

            // 2. Specific penetration orifice slot check
            PenetrationSlot reqSlot = card.GetRequiredSlot();
            if (reqSlot != PenetrationSlot.None)
            {
                // Slot must be actively occupied by an enemy right now!
                if (!IsSlotOccupied(reqSlot)) return false;
            }
            else if (card.RequiresPenetratingTarget())
            {
                // Must have at least one penetrating enemy
                if (!IsSlotOccupied(PenetrationSlot.Vagina) && !IsSlotOccupied(PenetrationSlot.Anal) && !IsSlotOccupied(PenetrationSlot.Mouth))
                    return false;
            }

            // 3. Anatomical speech restriction: If mouth is penetrated, verbal murmuring is physically impossible
            if (IsSlotOccupied(PenetrationSlot.Mouth) && card.id == "submissive_murmur")
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Context Priority: Rank cards to prioritize specialized positional techniques when their condition is met.
        /// </summary>
        public int GetCardContextPriority(CardData card)
        {
            if (card == null) return 0;

            PenetrationSlot reqSlot = card.GetRequiredSlot();
            // Highest priority (200): Positional moves for actively occupied orifices (Throat Lock, Vaginal Clench, etc.)
            if (reqSlot != PenetrationSlot.None && IsSlotOccupied(reqSlot))
            {
                return 200;
            }

            // High priority (150): Struggle-locked moves when pinned or grappled (Bound Kick, Sweet Captivity, etc.)
            if (currentStance != CombatStance.Neutral && card.stanceRequirement == StanceRequirement.StruggleOnly)
            {
                return 150;
            }

            // Distance priority (140): Distance lust cards when in Neutral
            if (currentStance == CombatStance.Neutral && card.stanceRequirement == StanceRequirement.NeutralOnly)
            {
                return 140;
            }

            // Baseline priority (100): Universal Any-Stance cards (Moan, Whimper, Shield, Breathing)
            return 100;
        }

        /// <summary>
        /// Smart Context Draw: Filters active Path cards, prioritizes context-fitting moves, and guarantees no dead hands.
        /// </summary>
        public List<CardData> GetSmartContextLustCards(LustPathType path, int count)
        {
            List<CardData> result = new List<CardData>();
            if (path == LustPathType.None || count <= 0) return result;

            // Step 1: Gather player's unlocked cards for active path
            List<CardData> unlockedForPath = GetLustCardsForPath(path);

            // Step 2: Strictly filter for cards playable in current context (stance + orifice slots)
            List<CardData> playableUnlocked = unlockedForPath.FindAll(IsLustCardPlayableInCurrentContext);

            // Step 3: Prioritize specialized positional actions, shuffle ties for variety
            playableUnlocked.Sort((a, b) =>
            {
                int prioA = GetCardContextPriority(a);
                int prioB = GetCardContextPriority(b);
                if (prioA != prioB) return prioB.CompareTo(prioA);
                return UnityEngine.Random.Range(-1, 2);
            });

            foreach (var card in playableUnlocked)
            {
                if (result.Count >= count) break;
                if (!result.Exists(r => r.id == card.id))
                {
                    result.Add(card);
                }
            }

            // Step 4: Fallback from unlocked cards
            // If the uniquely playable cards from unlockedForPath didn't fill 'count',
            // first attempt to fill with any other Any-stance universal cards of this path before repeating duplicates!
            if (result.Count > 0 && result.Count < count)
            {
                var universalAny = unlockedForPath.FindAll(c => c.stanceRequirement == StanceRequirement.Any && !result.Exists(r => r.id == c.id));
                foreach (var uc in universalAny)
                {
                    if (result.Count >= count) break;
                    result.Add(uc);
                }

                if (result.Count < count)
                {
                    int initialCount = result.Count;
                    for (int i = initialCount; i < count; i++)
                    {
                        result.Add(result[i % initialCount]);
                    }
                }
            }
            // Step 5: Emergency posture fallback
            // If zero unlocked cards are playable in the current stance (e.g. only positional moves unlocked, but player is in Neutral),
            // provide the path's base universal Any-stance cards so the player is never card-starved.
            else if (result.Count == 0)
            {
                var universalCards = unlockedCards.FindAll(c => c.category == CardCategory.LustInstinct &&
                    c.requiredPath == path &&
                    c.stanceRequirement == StanceRequirement.Any &&
                    IsLustCardPlayableInCurrentContext(c));

                if (universalCards.Count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        result.Add(universalCards[i % universalCards.Count]);
                    }
                }
                else if (unlockedForPath.Count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        result.Add(unlockedForPath[i % unlockedForPath.Count]);
                    }
                }
            }

            return result;
        }

        private void AddStruggleHand(int maxSlots = 4)
        {
            var struggleList = new List<string> { "break_free", "sensual_caress", "shift_leverage" };

            if (currentStance == CombatStance.Penetrated || currentStance == CombatStance.ThroatLock)
            {
                // In active penetration, Counter Clench acts as the internal or throat clench instinct
                struggleList.Add("counter_clench");
            }
            else
            {
                // In Grappled stance (before penetration), close clinch & leverage replaces weapon strikes
                struggleList.Add("struggle_clinch");
            }

            int count = Mathf.Min(maxSlots, struggleList.Count);
            for (int i = 0; i < count; i++)
            {
                string sid = struggleList[i];
                CardData cd = unlockedCards.Find(c => c.id == sid);
                if (cd != null)
                {
                    CardInstance inst = new CardInstance(cd.Clone());
                    inst.currentCooldown = cardCooldowns.TryGetValue(sid, out int c) ? c : 0;
                    playerDeck.Add(inst);
                }
            }
        }

        public EnemyDefinitionSO GetEnemyDefinition(EnemyType type)
        {
            if (enemyDefinitions != null && enemyDefinitions.Count > 0)
            {
                for (int i = 0; i < enemyDefinitions.Count; i++)
                {
                    if (enemyDefinitions[i] != null && enemyDefinitions[i].enemyType == type)
                        return enemyDefinitions[i];
                }
            }

            // Fallback: auto-load from Resources/Enemies
            EnemyDefinitionSO[] resEnemies = Resources.LoadAll<EnemyDefinitionSO>("Enemies");
            if (resEnemies != null)
            {
                for (int i = 0; i < resEnemies.Length; i++)
                {
                    if (resEnemies[i] != null && resEnemies[i].enemyType == type)
                    {
                        if (enemyDefinitions == null) enemyDefinitions = new List<EnemyDefinitionSO>();
                        if (!enemyDefinitions.Contains(resEnemies[i])) enemyDefinitions.Add(resEnemies[i]);
                        return resEnemies[i];
                    }
                }
            }

            return null;
        }

        public bool AddCardToDeck(CardData card)
        {
            if (card == null) return false;
            if (baseWarriorDeck.Count >= MAX_DECK_SIZE) return false;

            // Prevent duplicate cards in player's deck
            for (int i = 0; i < baseWarriorDeck.Count; i++)
            {
                if (baseWarriorDeck[i] != null && baseWarriorDeck[i].id == card.id)
                    return false;
            }

            baseWarriorDeck.Add(card);
            equippedDeck.Add(new CardInstance(card));
            if (!isCombatActive)
            {
                playerDeck.Add(new CardInstance(card));
            }
            SavePlayerDeck();
            return true;
        }

        public bool RemoveCardFromDeck(int index)
        {
            if (index < 0 || index >= baseWarriorDeck.Count) return false;
            baseWarriorDeck.RemoveAt(index);
            if (index < equippedDeck.Count) equippedDeck.RemoveAt(index);
            if (!isCombatActive && index < playerDeck.Count) playerDeck.RemoveAt(index);
            SavePlayerDeck();
            return true;
        }

        public void SavePlayerDeck()
        {
            List<string> ids = new List<string>();
            foreach (var c in baseWarriorDeck)
            {
                if (c != null) ids.Add(c.id);
            }
            PlayerPrefs.SetString("Sanctum_PlayerEquippedDeck", string.Join(",", ids));
            PlayerPrefs.Save();
        }

        public void StartCombat(List<EnemyType> types, int floorLevel = 1)
        {
            List<Combatant> group = new List<Combatant>();
            int limit = Mathf.Min(types.Count, 3);
            for (int i = 0; i < limit; i++)
            {
                group.Add(Combatant.CreateEnemy(types[i], floorLevel));
            }
            StartCombat(group);
        }

#if UNITY_EDITOR
        [ContextMenu("Test Combat: Elven Scout (Solo)")]
        public void TestCombatElvenScout()
        {
            StartCombat(new List<EnemyType> { EnemyType.ElvenScout }, 1);
        }

        [ContextMenu("Test Combat: Elven Scout Duo")]
        public void TestCombatElvenScoutDuo()
        {
            StartCombat(new List<EnemyType> { EnemyType.ElvenScout, EnemyType.ElvenScout }, 1);
        }
#endif

        public void StartCombat(List<Combatant> enemyGroup)
        {
            StopAllCoroutines();
            isActionInProgress = false;
            activeControlZone = ExternalFinishZone.None;
            isPureRunActive = true;
            OnPureStatusChanged?.Invoke(isPureRunActive);

            if (enemyGroup.Count > 3)
            {
                enemyGroup = enemyGroup.GetRange(0, 3);
            }

            // Player combatant - load persistent stats from expedition
            if (player == null)
            {
                player = Combatant.CreatePlayer(persistentHp, persistentUpperClothesHp, persistentLowerClothesHp, persistentUnderUpperClothesHp, persistentUnderLowerClothesHp);
            }
            player.name = "You";

            SyncEquippedItemsToPlayer();

            // Pull passive bonuses from active Desire Tree
            int passiveHp = 0;
            int passiveStartShield = 0;
            int passiveStartLustShield = 0;
            int passiveSedPower = 0;
            int passiveWill = 0;

            if (DesireTreeManager.Instance != null)
            {
                var bonuses = DesireTreeManager.Instance.GetTotalPassiveBonuses(activeLustPath);
                passiveHp = bonuses.hp;
                passiveStartShield = bonuses.shield;
                passiveStartLustShield = bonuses.cumShield;
                passiveSedPower = bonuses.seduction;
                passiveWill = bonuses.willpower;
            }

            player.passiveBonusMaxHp = passiveHp;
            player.passiveBonusSeductionPower = passiveSedPower;
            player.passiveWillpower = passiveWill;
            player.baseMaxHp = 30;
            player.baseMaxMana = 10;
            player.RecalculateStats();

            persistentMaxHp = player.maxHp;
            persistentHp = Mathf.Clamp(persistentHp > 0 ? persistentHp : player.maxHp, 1, persistentMaxHp);
            player.currentHp = persistentHp;
            player.currentMana = 10;
            player.currentAp = Mathf.Min(player.maxAp, 3);
            player.seduction = persistentSeduction;
            player.seductionStacks = persistentSeductionStacks;
            player.semenMouthMl = persistentSemenMouthMl;
            player.semenWombMl = persistentSemenWombMl;
            player.semenAnalMl = persistentSemenAnalMl;
            player.semenVolumeMl = persistentSemenVolumeMl;
            player.shield = passiveStartShield;
            player.ResetLustShield();
            if (passiveStartLustShield > 0)
            {
                player.AddLustShield(passiveStartLustShield);
            }
            player.isDefeated = false;
            player.defeatedBySeduction = false;

            if (passiveHp > 0 || passiveStartShield > 0 || passiveStartLustShield > 0 || passiveSedPower > 0 || passiveWill > 0)
            {
                OnCombatLogMessage?.Invoke($"[DESIRE TREE PASSIVES] {activeLustPath} active: +{passiveHp} Max HP, +{passiveStartShield} Shield, +{passiveStartLustShield} Lust Shield, +{passiveSedPower}% Seduction, +{passiveWill} Willpower.");
            }

            // Save persistent player stats from expedition
            player.OnStateChanged -= SavePlayerStatsFromCombat;
            player.OnStateChanged += SavePlayerStatsFromCombat;

            // Initialize equippedDeck cleanly from baseWarriorDeck (strictly warrior/tactics, NEVER struggle cards!)
            equippedDeck.Clear();
            foreach (var cardData in baseWarriorDeck)
            {
                if (cardData.category != CardCategory.StruggleAction)
                {
                    equippedDeck.Add(new CardInstance(cardData));
                }
            }

            if (equippedDeck.Count == 0)
            {
                string[] defaultStarter = { "strike", "heavy_slash", "block", "shield_bash" };
                foreach (var id in defaultStarter)
                {
                    var cd = unlockedCards.Find(c => c.id == id);
                    if (cd != null) equippedDeck.Add(new CardInstance(cd));
                }
            }

            foreach (var card in equippedDeck)
            {
                card.currentCooldown = 0;
            }

            cardCooldowns.Clear();
            currentStance = CombatStance.Neutral;
            BuildTurnHand();

            enemies = new List<Combatant>(enemyGroup);
            foreach (var e in enemies)
            {
                if (e != null)
                {
                    e.occupiedSlot = PenetrationSlot.None;
                    e.isGrapplingPlayer = false;
                }
            }
            selectedEnemyIndex = 0;
            isCombatActive = true;
            isPlayerTurn = true;
            currentTurnCombatant = player;

            OnCombatStarted?.Invoke();
            OnCombatantTurnStarted?.Invoke(player);
            OnHandUpdated?.Invoke();
            OnCombatLogMessage?.Invoke($"Combat started! {enemies.Count} enemies appeared! Stance: NEUTRAL");
        }

        public void SelectEnemy(int index)
        {
            if (index >= 0 && index < enemies.Count && !enemies[index].isDefeated)
            {
                selectedEnemyIndex = index;
            }
        }

        public bool PlayCard(int cardIndex, int explicitEnemyIndex = -1)
        {
            if (!isCombatActive || !isPlayerTurn || isActionInProgress) return false;
            if (cardIndex < 0 || cardIndex >= playerDeck.Count) return false;

            if (explicitEnemyIndex >= 0 && explicitEnemyIndex < enemies.Count && !enemies[explicitEnemyIndex].isDefeated)
            {
                SelectEnemy(explicitEnemyIndex);
            }

            CardInstance card = playerDeck[cardIndex];
            if (!card.CanPlay(player.currentAp, currentStance))
            {
                if (card.currentCooldown > 0)
                    OnCombatLogMessage?.Invoke("Cannot play card: On Cooldown!");
                else if (player.currentAp < card.data.apCost)
                    OnCombatLogMessage?.Invoke("Cannot play card: Insufficient AP!");
                else
                    OnCombatLogMessage?.Invoke($"Cannot play card: Requires {card.data.stanceRequirement} (Current: {currentStance})!");
                return false;
            }

            // Target resolution
            Combatant targetEnemy = GetSelectedEnemy();
            if (card.data.target == CardTarget.SingleEnemy)
            {
                if (targetEnemy == null || targetEnemy.isDefeated || !card.data.IsValidTargetEnemy(targetEnemy, currentStance))
                {
                    // Check if another enemy is valid
                    Combatant validCand = enemies.Find(e => !e.isDefeated && card.data.IsValidTargetEnemy(e, currentStance));
                    if (validCand != null && explicitEnemyIndex < 0)
                    {
                        SelectEnemy(enemies.IndexOf(validCand));
                        targetEnemy = validCand;
                    }
                    else
                    {
                        PenetrationSlot reqSlot = card.data.GetRequiredSlot();
                        if (reqSlot != PenetrationSlot.None)
                        {
                            Combatant occupant = GetSlotOccupant(reqSlot);
                            string occupantMsg = occupant != null ? $" (Target must be {occupant.name} in your {reqSlot.ToString().ToUpper()})" : $" (Your {reqSlot.ToString().ToUpper()} is empty)";
                            OnCombatLogMessage?.Invoke($"Invalid Target! '{card.data.cardName}' cannot be used on {targetEnemy?.name}{occupantMsg}!");
                        }
                        else
                        {
                            OnCombatLogMessage?.Invoke($"Invalid Target! '{card.data.cardName}' cannot be used on {targetEnemy?.name}!");
                        }
                        return false;
                    }
                }
            }

            StartCoroutine(ExecutePlayerCardRoutine(card, targetEnemy));
            return true;
        }

        private IEnumerator ExecutePlayerCardRoutine(CardInstance card, Combatant targetEnemy)
        {
            isActionInProgress = true;

            // Pure Run check: Playing any Lust Instinct card compromises pure martial status
            if (card.data.category == CardCategory.LustInstinct)
            {
                if (isPureRunActive)
                {
                    isPureRunActive = false;
                    OnCombatLogMessage?.Invoke("<color=#FF6688><b>[PURE COMPROMISED]</b></color> You gave in to the Vessel's lust instincts!");
                    OnPureStatusChanged?.Invoke(false);
                }
            }

            // 1. Deduct AP & apply CD immediately
            player.currentAp -= card.data.apCost;
            card.PutOnCooldown();
            if (card.data.cooldownTurns > 0)
            {
                cardCooldowns[card.data.id] = card.data.cooldownTurns;
            }

            // 2. Announce telegraph (Header banner shows skill name)
            OnPlayerSkillTelegraphed?.Invoke(card, targetEnemy);
            OnCombatLogMessage?.Invoke($"You activate: {card.data.cardName}...");

            // 3. 1 Second telegraph windup pause
            yield return new WaitForSeconds(1.0f);

            // 4. Apply card effects
            if (card.data.controlZone != ExternalFinishZone.None)
            {
                activeControlZone = card.data.controlZone;
                OnCombatLogMessage?.Invoke($"[CONTROL STANCE] You maneuver your {activeControlZone} to dictate the foe's release!");
            }

            // HP Damage
            if (card.data.hpDamage > 0 && targetEnemy != null && !targetEnemy.isDefeated)
            {
                int prevShield = targetEnemy.shield;
                int dealt = targetEnemy.TakeHpDamage(card.data.hpDamage);
                int absorbed = Mathf.Max(0, prevShield - targetEnemy.shield);

                if (absorbed > 0)
                {
                    OnCombatLogMessage?.Invoke($"{targetEnemy.name}'s shield absorbed {absorbed} damage!");
                }
                if (dealt > 0)
                {
                    OnCombatLogMessage?.Invoke($"You used {card.data.cardName}! Dealt {dealt} damage to {targetEnemy.name}.");
                }
                OnDamageDealt?.Invoke(targetEnemy, dealt, absorbed);

                if (targetEnemy.isDefeated)
                {
                    OnCombatLogMessage?.Invoke($"{targetEnemy.name} was exhausted and retreated from combat!");
                    ReleaseEnemyHold(targetEnemy);
                    AutoSelectNextEnemy();
                }
            }

            // Stamina / Posture Damage (Struggle Engine)
            if (card.data.staminaDamage > 0 && targetEnemy != null && !targetEnemy.isDefeated)
            {
                int sDmg = targetEnemy.TakeStaminaDamage(card.data.staminaDamage);
                OnCombatLogMessage?.Invoke($"Posture damage dealt: {sDmg} Stamina to {targetEnemy.name} [{targetEnemy.currentStamina}/{targetEnemy.maxStamina}]!");

                if (targetEnemy.currentStamina <= 0)
                {
                    OnCombatLogMessage?.Invoke($"{targetEnemy.name}'s balance shattered! She is STAGGERED and off-balance!");
                }
            }

            // Break Free logic
            if (card.data.id == "break_free")
            {
                if (targetEnemy == null || targetEnemy.currentStamina <= 0 || UnityEngine.Random.value < 0.6f)
                {
                    OnCombatLogMessage?.Invoke("You violently kicked off the ground and broke free!");
                    if (targetEnemy != null)
                    {
                        ReleaseEnemyHold(targetEnemy);
                    }
                    else
                    {
                        ReleaseAllEnemyHolds();
                    }
                }
                else
                {
                    OnCombatLogMessage?.Invoke("The opponent clamped down tighter, preventing your escape!");
                }
            }

            // Standard Shield Gain
            if (card.data.shieldGain > 0)
            {
                player.AddShield(card.data.shieldGain);
                OnCombatLogMessage?.Invoke($"You used {card.data.cardName}! Gained {card.data.shieldGain} Shield.");
            }

            // Lust Shield Gain & Semen Transmutation logic
            if (card.data.id == "lust_barrier")
            {
                // Path 1 Semen Transmutation: Converts stored Cum into Lust Shield
                if (player.semenVolumeMl <= 0)
                {
                    OnCombatLogMessage?.Invoke("The Womb Tattoo flickers weakly: No absorbed Cum stored to transmute into a Lust Shield!");
                }
                else
                {
                    int consumed = Mathf.Min(player.semenVolumeMl, 25);
                    player.semenVolumeMl -= consumed;
                    int shieldGain = consumed; // 1ml Cum = 1 Lust Shield
                    player.AddLustShield(shieldGain);
                    OnCombatLogMessage?.Invoke($"Womb Tattoo flared, transmuting {consumed}ml of stored Cum into +{shieldGain} Lust Shield! (Remaining: {player.semenVolumeMl}ml)");
                }
            }
            else if (card.data.id == "cum_surge")
            {
                // Path 1 Explosive Semen Surge: Transmutes stored Cum into a heavy Lust Shield
                if (player.semenVolumeMl <= 0)
                {
                    OnCombatLogMessage?.Invoke("Crucible is dry: No absorbed Cum to surge with! (Only physical shield raised)");
                }
                else
                {
                    int consumed = Mathf.Min(player.semenVolumeMl, 40);
                    player.semenVolumeMl -= consumed;
                    int bonusLustShield = consumed + 5;
                    player.AddLustShield(bonusLustShield);
                    OnCombatLogMessage?.Invoke($"Explosive Semen Surge! Transmuted {consumed}ml of stored Cum into +{bonusLustShield} Lust Shield! (Remaining: {player.semenVolumeMl}ml)");
                }
            }
            else if (card.data.lustShieldGain > 0)
            {
                // Standard Lust Shield for other cards (Sweet Captivity, Womb Pulse, Slime Coating, Primal Metamorphosis)
                player.AddLustShield(card.data.lustShieldGain);
                OnCombatLogMessage?.Invoke($"Lust instincts flared, forming +{card.data.lustShieldGain} Lust Shield!");
            }

            // Semen / Cum Absorption
            if (card.data.semenAbsorbedMl > 0)
            {
                player.AddSemen(card.data.semenAbsorbedMl);
                persistentSemenVolumeMl += card.data.semenAbsorbedMl;
                FleshTransformationManager.Instance?.AddPrana(card.data.semenAbsorbedMl);
                OnCombatLogMessage?.Invoke($"Absorbed {card.data.semenAbsorbedMl}ml of Cum into your Living Crucible! (Total: {player.semenVolumeMl}ml)");
            }

            // Self Arousal Delta
            if (card.data.arousalSelfDelta != 0)
            {
                if (card.data.arousalSelfDelta < 0)
                {
                    player.seduction = Mathf.Clamp(player.seduction + card.data.arousalSelfDelta, 0, 100);
                    OnCombatLogMessage?.Invoke($"You regained composure: Arousal reduced by {-card.data.arousalSelfDelta}% ({player.arousal}%).");
                    player.NotifyStateChanged();
                }
                else
                {
                    int sed = player.AddSeduction(card.data.arousalSelfDelta);
                    OnCombatLogMessage?.Invoke($"Your arousal rose by {sed}%! ({player.arousal}%).");
                }
            }

            // Deep Swallow Heal
            if (card.data.id == "deep_swallow")
            {
                HealPlayer(8);
                OnCombatLogMessage?.Invoke("Absorbed cum nourishes your synthetic flesh! Restored +8 HP.");
            }

            // Path 1: Overwhelming Surrender
            if (card.data.id == "overwhelming_surrender")
            {
                if (player.semenVolumeMl >= 80)
                {
                    HealPlayer(15);
                    OnCombatLogMessage?.Invoke("Total surrender to the embedded seed revitalizes your synthetic core! Healed 15 HP.");
                }
            }

            // Path 2: Sweet Captivity (+1 AP next turn)
            if (card.data.id == "sweet_captivity")
            {
                player.currentAp = Mathf.Min(player.maxAp + 1, player.currentAp + 1);
                OnCombatLogMessage?.Invoke("Embracing captivity surges your nervous system! +1 AP granted.");
            }

            // Path 2: Masochistic Ecstasy
            if (card.data.id == "masochistic_ecstasy")
            {
                HealPlayer(10);
                OnCombatLogMessage?.Invoke("Pain and torn cloth fuel masochistic ecstasy! Restored +10 HP.");
            }

            // Path 2: Wrist Lock Reverse
            if (card.data.id == "wrist_lock_reverse")
            {
                if (targetEnemy != null && (targetEnemy.currentStamina <= 0 || UnityEngine.Random.value < 0.75f))
                {
                    OnCombatLogMessage?.Invoke("You viciously torqued the opponent's wrist and broke free from their pin!");
                    ReleaseEnemyHold(targetEnemy);
                }
            }

            // Path 3: Milky Relief
            if (card.data.id == "milky_relief")
            {
                HealPlayer(12);
                player.currentStamina = Mathf.Min(player.maxStamina, player.currentStamina + 15);
                OnCombatLogMessage?.Invoke("Sweet maternal release soothes nerve strain! Restored +12 HP and +15 Stamina.");
            }

            // Path 3: Succubus Siphon
            if (card.data.id == "succubus_siphon")
            {
                HealPlayer(8);
                OnCombatLogMessage?.Invoke("Succubus touch siphoned vital energy! Restored +8 HP.");
            }

            // Path 3: Slime Skin Coating (slippery escape)
            if (card.data.id == "slime_skin_coating")
            {
                if (targetEnemy != null && (targetEnemy.isGrapplingPlayer || targetEnemy.occupiedSlot != PenetrationSlot.None))
                {
                    OnCombatLogMessage?.Invoke("Slick viscous coating caused you to slide effortlessly out of the opponent's hold!");
                    ReleaseEnemyHold(targetEnemy);
                }
            }

            // Path 3: Primal Metamorphosis
            if (card.data.id == "primal_metamorphosis")
            {
                HealPlayer(12);
                player.currentAp = Mathf.Min(player.maxAp + 2, player.currentAp + 2);
                OnCombatLogMessage?.Invoke("Demonic metamorphosis fully activated! Restored 12 HP and +2 AP!");
                if (enemies != null)
                {
                    foreach (var e in enemies)
                    {
                        if (e != null && !e.isDefeated)
                        {
                            StartCoroutine(ProcessEnemySeductionRoutine(e, 35));
                        }
                    }
                }
            }

            // Clothes Repair
            if (card.data.clothesRepair > 0 || card.data.id == "mend_clothes")
            {
                int repairAmount = card.data.clothesRepair > 0 ? card.data.clothesRepair : 15;
                player.upperClothesHp = Mathf.Min(player.maxUpperClothesHp, player.upperClothesHp + repairAmount);
                player.lowerClothesHp = Mathf.Min(player.maxLowerClothesHp, player.lowerClothesHp + repairAmount);
                if (player.isWearingCombinedClothes)
                {
                    player.lowerClothesHp = player.upperClothesHp;
                }
                player.RecalculateStats();
                OnCombatLogMessage?.Invoke($"Garments adjusted! Restored {repairAmount} durability to Upper ({player.upperClothesHp}/{player.maxUpperClothesHp}) and Lower ({player.lowerClothesHp}/{player.maxLowerClothesHp}).");
            }

            // Target Seduction / Arousal Delta
            int targetArousalDelta = card.data.arousalTargetDelta > 0 ? card.data.arousalTargetDelta : card.data.seductionAmount;
            if (targetArousalDelta > 0 && targetEnemy != null && !targetEnemy.isDefeated)
            {
                int effectiveSed = Mathf.Max(0, targetArousalDelta + player.CurrentBonusSeductionPower);
                OnCombatLogMessage?.Invoke($"You used {card.data.cardName}!");
                yield return StartCoroutine(ProcessEnemySeductionRoutine(targetEnemy, effectiveSed));
            }

            // Finisher Execution Check (e.g. Milking Stroke)
            if (card.data.isFinisherExecution && targetEnemy != null && !targetEnemy.isDefeated)
            {
                OnCombatLogMessage?.Invoke($"[FINISHER TRIGGER] {card.data.cardName} breaks {targetEnemy.name}'s endurance, forcing an immediate explosive climax!");
                yield return StartCoroutine(ExecuteEnemyEjaculationRoutine(targetEnemy, targetEnemy.occupiedSlot));
                targetEnemy.isDefeated = true;
                targetEnemy.defeatedBySeduction = true;
                ReleaseEnemyHold(targetEnemy);
                AutoSelectNextEnemy();
            }

            OnCardPlayed?.Invoke(card, targetEnemy);
            OnPlayerSkillExecuted?.Invoke(card, targetEnemy);

            // 5. Brief settle pause
            yield return new WaitForSeconds(0.4f);

            isActionInProgress = false;
            OnPlayerActionFinished?.Invoke();

            // Check if play finished the enemy or battle
            CheckEndConditions();
        }

        public void ProcessEnemySeduction(Combatant enemy, int sedAmount)
        {
            StartCoroutine(ProcessEnemySeductionRoutine(enemy, sedAmount));
        }

        public IEnumerator ProcessEnemySeductionRoutine(Combatant enemy, int sedAmount)
        {
            if (enemy == null || enemy.isDefeated || sedAmount <= 0) yield break;

            int prevPhase = enemy.seductionStacks;
            int sed = enemy.AddSeduction(sedAmount);
            OnCombatLogMessage?.Invoke($"Raised {enemy.name}'s Arousal by {sed}% [{enemy.arousal}% | Stacks: {enemy.seductionStacks}/{enemy.maxSeductionStacks}].");
            OnSeductionDealt?.Invoke(enemy, sed);

            // Did the enemy complete a lust stage (Enemy Climax / Finish)?
            if (enemy.seductionStacks > prevPhase)
            {
                OnSeductionPhaseChanged?.Invoke(enemy, enemy.seductionStacks);

                // Automatic Ejaculation & Climax Sequence
                yield return StartCoroutine(ExecuteEnemyEjaculationRoutine(enemy, enemy.occupiedSlot));
            }

            // Overstimulation Check: if enemy reaches max stacks -> Climax victory!
            if (enemy.seductionStacks >= enemy.maxSeductionStacks && !enemy.isDefeated)
            {
                enemy.isDefeated = true;
                enemy.defeatedBySeduction = true;
                OnCombatLogMessage?.Invoke($"[OVERSTIMULATION] {enemy.name} collapsed in ecstatic climax, completely overwhelmed by bliss!");
                ReleaseEnemyHold(enemy);
                AutoSelectNextEnemy();
            }
        }

        public IEnumerator ExecuteEnemyEjaculationRoutine(Combatant enemy, PenetrationSlot slot)
        {
            if (enemy == null) yield break;

            // Determine if enemy is currently penetrating the player vessel
            bool isPenetrating = (slot != PenetrationSlot.None) || 
                                 (currentStance == CombatStance.Penetrated || currentStance == CombatStance.ThroatLock);

            int pranaAmount = 80;
            if (slot == PenetrationSlot.Mouth) pranaAmount = 90;
            else if (slot == PenetrationSlot.Anal) pranaAmount = 100;
            else if (slot == PenetrationSlot.Vagina) pranaAmount = 85;

            // Trigger screen / camera shake
            OnCameraShakeRequested?.Invoke(16f, 1.2f);

            if (activeControlZone != ExternalFinishZone.None)
            {
                // External finish forced by player control cards
                int cumVolume = 70;
                int lustShieldGained = 30;
                switch (activeControlZone)
                {
                    case ExternalFinishZone.Hands:
                        lustShieldGained = 35;
                        OnCombatLogMessage?.Invoke($"[EXTERNAL FINISH - HANDJOB] You milk {enemy.name}'s shaft with frantic hand strokes! Sticky cum covers your palms and fingers! (+{lustShieldGained} Lust Shield gained)");
                        break;
                    case ExternalFinishZone.Breasts:
                        lustShieldGained = 40;
                        OnCombatLogMessage?.Invoke($"[EXTERNAL FINISH - PAIZURI] You squeeze {enemy.name}'s pulsing member between your soft breasts! Ropes of thick cum splatter across your chest and cleavage! (+{lustShieldGained} Lust Shield gained)");
                        break;
                    case ExternalFinishZone.Thighs:
                        lustShieldGained = 40;
                        OnCombatLogMessage?.Invoke($"[EXTERNAL FINISH - THIGH LOCK] You clamp your thighs tightly around {enemy.name}'s throbbing groin! Cum drenches your inner thighs and groin! (+{lustShieldGained} Lust Shield gained)");
                        break;
                    case ExternalFinishZone.Face:
                        lustShieldGained = 30;
                        OnCombatLogMessage?.Invoke($"[EXTERNAL FINISH - FACIAL] You tilt your head back as {enemy.name} fires copious cum across your cheeks and parted lips! (+{lustShieldGained} Lust Shield gained)");
                        break;
                    default:
                        lustShieldGained = 30;
                        OnCombatLogMessage?.Invoke($"[EXTERNAL FINISH] {enemy.name} erupts externally, coating your body in warm fluid! (+{lustShieldGained} Lust Shield gained)");
                        break;
                }

                player.AddLustShield(lustShieldGained);
                // External finish does NOT count as internal cum intake for the Flesh Archive!
                FleshTransformationManager.Instance?.AddPrana(cumVolume, enemy?.name, isInternal: false);

                // Reset control zone
                activeControlZone = ExternalFinishZone.None;
            }
            else if (isPenetrating)
            {
                if (enemy.gender == EntityGender.Female)
                {
                    // Female climax in intimate engagement (Kiss / Fingers) - No male/futa semen, does not compromise Pure Run
                    int lustShieldGained = 25;
                    player.AddLustShield(lustShieldGained);
                    player.AddSeduction(20);

                    if (slot == PenetrationSlot.Mouth)
                    {
                        OnCombatLogMessage?.Invoke($"[APHRODISIAC KISS & FEED] {enemy.name} shudders in ecstasy while holding your mouth, forcing potent aphrodisiac down your throat! (+{lustShieldGained} Lust Shield, +20% Arousal)");
                    }
                    else if (slot == PenetrationSlot.Vagina)
                    {
                        OnCombatLogMessage?.Invoke($"[MANUAL CLIMAX & NECTAR] {enemy.name}'s rapid finger-play triggers an intense convulsive orgasm, soaking your intimate cleft with slick elven nectar! (+{lustShieldGained} Lust Shield, +20% Arousal)");
                    }
                    else
                    {
                        OnCombatLogMessage?.Invoke($"[FEMALE CLIMAX] {enemy.name} trembles in overwhelming ecstasy against your vessel! (+{lustShieldGained} Lust Shield gained)");
                    }
                }
                else
                {
                    if (isPureRunActive)
                    {
                        isPureRunActive = false;
                        OnCombatLogMessage?.Invoke("<color=#FF4466><b>[PURE COMPROMISED]</b></color> The Vessel was defiled by internal monster seed!");
                        OnPureStatusChanged?.Invoke(false);
                    }
                    player.AddSemen(pranaAmount, slot);
                    AddPersistentSemen(pranaAmount, slot);
                    FleshTransformationManager.Instance?.AddPrana(pranaAmount, enemy?.name, isInternal: true);
                    int lustShieldGained = Mathf.RoundToInt(pranaAmount * 0.25f);
                    player.AddLustShield(lustShieldGained);

                    if (slot == PenetrationSlot.Mouth)
                    {
                        OnCombatLogMessage?.Invoke($"[THROAT INFLUX & SWALLOW] {enemy.name} reaches climax and pumps {pranaAmount}ml of Cum down your throat! (+{lustShieldGained} Lust Shield gained [Total: {player.semenVolumeMl}ml])");
                    }
                    else if (slot == PenetrationSlot.Anal)
                    {
                        OnCombatLogMessage?.Invoke($"[ANAL CREAMPIE] {enemy.name} reaches climax and floods your Anal tract with {pranaAmount}ml of Cum! (+{lustShieldGained} Lust Shield gained [Total: {player.semenVolumeMl}ml])");
                    }
                    else
                    {
                        string slotName = slot == PenetrationSlot.Vagina ? "Vagina" : "vessel";
                        OnCombatLogMessage?.Invoke($"[CREAMPIE] {enemy.name} reaches climax and floods your {slotName} with {pranaAmount}ml of Cum! (+{lustShieldGained} Lust Shield gained [Total: {player.semenVolumeMl}ml])");
                    }
                }
            }
            else
            {
                if (enemy.gender == EntityGender.Female)
                {
                    OnCombatLogMessage?.Invoke($"[FOE CLIMAX] {enemy.name} shudders in overwhelming ecstatic release! (Lust Stage {enemy.seductionStacks}/{enemy.maxSeductionStacks} reached)");
                }
                else
                {
                    int extLustShield = 20;
                    player.AddLustShield(extLustShield);
                    FleshTransformationManager.Instance?.AddPrana(60, enemy?.name, isInternal: false);
                    OnCombatLogMessage?.Invoke($"[EXTERNAL CLIMAX] {enemy.name} shudders in violent orgasm, discharging cum across the floor and your lower body! (+{extLustShield} Lust Shield gained)");
                }
            }

            // Invoke event for UI animations / future mini-CG scenes
            OnEnemyEjaculated?.Invoke(enemy, slot, pranaAmount);

            // Execute custom CG handler if registered, otherwise wait for standard ejaculation duration
            if (CustomEjaculationCGHandler != null)
            {
                yield return StartCoroutine(CustomEjaculationCGHandler(enemy, slot, pranaAmount));
            }
            else
            {
                yield return new WaitForSeconds(ejaculationPauseDuration);
            }
        }

        public void EndPlayerTurn()
        {
            if (!isCombatActive || !isPlayerTurn || isActionInProgress) return;

            StartCoroutine(ExecuteEnemyTurnsRoutine());
        }

        private IEnumerator ExecuteEnemyTurnsRoutine()
        {
            isActionInProgress = true;
            isPlayerTurn = false;
            currentTurnCombatant = null;
            OnCombatantTurnEnded?.Invoke(player); // Player badge dims in timeline
            OnTurnChanged?.Invoke(false);

            // Execute enemy turns sequentially with exact telegraphed pacing
            // (Note: Player's shield remains active to absorb incoming enemy damage!)
            for (int i = 0; i < enemies.Count; i++)
            {
                Combatant enemy = enemies[i];
                if (enemy.isDefeated || !isCombatActive) continue;

                // 1. Mark this enemy as active in timeline (lights up)
                currentTurnCombatant = enemy;
                OnCombatantTurnStarted?.Invoke(enemy);

                // Expire enemy's remaining shield at start of their turn
                if (enemy.shield > 0)
                {
                    enemy.ResetShield();
                }

                // 2. 1 Second preparation pause (where enemy is taking turn)
                yield return new WaitForSeconds(1.0f);

                // 3. Choose skill & display telegraph banner above the enemy card
                EnemySkill skill = EnemyAI.ChooseAction(enemy, player, currentStance);
                if (skill == null)
                {
                    if (currentStance == CombatStance.Penetrated || currentStance == CombatStance.ThroatLock)
                        skill = new EnemySkill("Intimate Union Thrusts", 0, 0, 0, 25, 0, 5, "Intimate sexual thrusts in combat union", ArmorTargetSlot.All, staminaDmg: 4, pranaGain: 0, stanceReq: CombatStance.Penetrated, transitionTo: null, selfArousal: 15);
                    else if (currentStance == CombatStance.Grappled)
                        skill = new EnemySkill("Struggle Pin", 0, 0, 10, 0, 0, 0, "Bodily struggle hold on the ground", ArmorTargetSlot.All, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Grappled);
                    else
                        skill = new EnemySkill("Basic Attack", 0, 4, 2, 5, 0, 0, "Basic physical attack");
                }

                OnEnemySkillTelegraphed?.Invoke(enemy, skill);
                OnCombatLogMessage?.Invoke($"{enemy.name} prepares to use: {skill.skillName}!");

                // 4. 1 Second pause observing the telegraph
                yield return new WaitForSeconds(1.0f);

                // 5. Attack effect hits the player
                if (skill.shieldGain > 0) enemy.AddShield(skill.shieldGain);

                OnCombatLogMessage?.Invoke($"{enemy.name} executes {skill.skillName}!");

                // Stamina / Posture Damage (Struggle Engine)
                if (skill.staminaDamage > 0)
                {
                    player.currentStamina = Mathf.Max(0, player.currentStamina - skill.staminaDamage);
                    OnCombatLogMessage?.Invoke($"Your stamina was reduced by {skill.staminaDamage}! ({player.currentStamina}/{player.maxStamina} Stamina)");
                    if (player.currentStamina <= 0 && currentStance == CombatStance.Neutral)
                    {
                        enemy.isGrapplingPlayer = true;
                        TransitionStance(CombatStance.Grappled);
                        OnCombatLogMessage?.Invoke($"{enemy.name} pins your exhausted vessel to the floor! STANCE SHIFT -> GRAPPLED!");
                    }
                }

                // Damage to clothes
                if (skill.clothesDamage > 0 && player.clothesHp > 0)
                {
                    bool wasUpperIntact = player.upperClothesHp > 0;
                    bool wasLowerIntact = player.lowerClothesHp > 0;

                    int cDmg = player.TakeClothesDamage(skill.clothesDamage, skill.armorTarget);
                    string targetName = player.isWearingCombinedClothes ? "Combined Outfit" :
                                        (skill.armorTarget == ArmorTargetSlot.Upper ? "Upper Garment" :
                                        (skill.armorTarget == ArmorTargetSlot.Lower ? "Lower Garment" : "All Garments"));
                    OnCombatLogMessage?.Invoke($"Your {targetName} suffered {cDmg} durability damage! [U: {player.upperClothesHp}/{player.maxUpperClothesHp} | L: {player.lowerClothesHp}/{player.maxLowerClothesHp}]");
                    OnClothesDamaged?.Invoke(player, cDmg);

                    if (player.isWearingCombinedClothes)
                    {
                        if ((wasUpperIntact || wasLowerIntact) && (player.upperClothesHp <= 0 || player.lowerClothesHp <= 0))
                        {
                            OnCombatLogMessage?.Invoke("YOUR COMBINED OUTFIT WAS SHREDDED TO PIECES! Its stat buffs were lost!");
                            StripCombinedClothes();
                            yield return new WaitForSeconds(0.4f);
                        }
                    }
                    else
                    {
                        bool anyStripped = false;
                        if (wasUpperIntact && player.upperClothesHp <= 0)
                        {
                            OnCombatLogMessage?.Invoke("YOUR UPPER GARMENT WAS SHREDDED TO PIECES! Its stat buffs were lost!");
                            StripUpperClothes();
                            anyStripped = true;
                        }
                        if (wasLowerIntact && player.lowerClothesHp <= 0)
                        {
                            OnCombatLogMessage?.Invoke("YOUR LOWER GARMENT WAS SHREDDED TO PIECES! Its stat buffs were lost!");
                            StripLowerClothes();
                            anyStripped = true;
                        }
                        if (anyStripped)
                        {
                            yield return new WaitForSeconds(0.4f);
                        }
                    }

                    persistentMaxHp = player.maxHp;
                    persistentHp = player.currentHp;
                }

                // Damage to HP (shield absorbs first - only in Neutral or if skill specifically has hpDamage > 0)
                if (skill.hpDamage > 0)
                {
                    int prevShield = player.shield;
                    int hpDmg = player.TakeHpDamage(skill.hpDamage);
                    int absorbed = Mathf.Max(0, prevShield - player.shield);

                    if (absorbed > 0)
                    {
                        OnCombatLogMessage?.Invoke($"Your shield absorbed {absorbed} damage! ({player.shield} shield remaining)");
                    }

                    if (hpDmg > 0)
                    {
                        OnCombatLogMessage?.Invoke($"You took {hpDmg} HP damage! ({player.currentHp}/{player.maxHp} HP)");
                    }
                    else if (absorbed > 0)
                    {
                        if (skill.seductionDamage == 0)
                            OnCombatLogMessage?.Invoke("Attack was completely blocked by your shield!");
                        else
                            OnCombatLogMessage?.Invoke("Shield absorbed physical strike, but lust bypassed your guard!");
                    }

                    OnDamageDealt?.Invoke(player, hpDmg, absorbed);
                }

                // Seduction / Arousal (Grapple takedowns are purely physical pins - no arousal damage on initial grab!)
                bool isGrappleTakedown = (skill.transitionToStance == CombatStance.Grappled);
                if (skill.seductionDamage > 0 && !isGrappleTakedown)
                {
                    int prevPhase = player.seductionStacks;
                    int sed = player.AddSeduction(skill.seductionDamage);
                    OnCombatLogMessage?.Invoke($"Your arousal rose by {sed}%! ({player.seduction}% | Stacks: {player.seductionStacks}/3)");
                    OnSeductionDealt?.Invoke(player, sed);
                    if (player.seductionStacks != prevPhase)
                    {
                        OnSeductionPhaseChanged?.Invoke(player, player.seductionStacks);
                    }
                }

                // Stance transition caused directly by the skill (e.g. deliberate penetration or pounce)
                if (skill.targetPenetrationSlot != PenetrationSlot.None || skill.transitionToStance == CombatStance.Penetrated)
                {
                    if (!enemy.isGrapplingPlayer)
                    {
                        // Even if another enemy holds the player, this enemy CANNOT immediately penetrate from neutral/unattached state!
                        // He must first grab and pin the character!
                        if (!AssignGrapple(enemy))
                        {
                            OnCombatLogMessage?.Invoke($"{enemy.name} joins the attack against your pinned vessel!");
                        }
                    }
                    else if (!enemy.canPenetrate)
                    {
                        OnCombatLogMessage?.Invoke($"{enemy.name} tightens her grapple hold on you (Female entity)!");
                    }
                    else
                    {
                        PenetrationSlot slot = skill.targetPenetrationSlot;
                        if (slot == PenetrationSlot.None)
                        {
                            var avail = GetAvailablePenetrationSlots(enemy);
                            if (avail.Count > 0) slot = avail[0];
                        }

                        if (slot != PenetrationSlot.None && CanPenetrateSlot(enemy, slot))
                        {
                            AssignPenetration(enemy, slot);
                        }
                        else
                        {
                            OnCombatLogMessage?.Invoke($"{enemy.name} attempts to penetrate, but the target orifice is occupied or protected!");
                            AssignGrapple(enemy, skill.targetGrappleZone);
                        }
                    }
                }
                else if (skill.transitionToStance == CombatStance.Grappled)
                {
                    AssignGrapple(enemy, skill.targetGrappleZone);
                }
                else if (skill.transitionToStance.HasValue && currentStance != skill.transitionToStance.Value)
                {
                    TransitionStance(skill.transitionToStance.Value);
                    OnCombatLogMessage?.Invoke($"STANCE SHIFT -> {skill.transitionToStance.Value.ToString().ToUpper()}!");
                }

                OnEnemySkillUsed?.Invoke(enemy, skill);

                // Exposed state in grapple: If lower clothes shredded during grapple, vessel is vulnerable,
                // BUT enemy does NOT penetrate immediately. Enemy must spend a future turn action to penetrate!
                if (currentStance == CombatStance.Grappled && player.isLowerTorn)
                {
                    // No automatic instant penetration! Player has time to struggle, counter, or break free.
                }

                // Reciprocal Arousal & Climax Check: During Penetrated / ThroatLock stance, in-combat sex stimulates the penetrating enemy!
                if (enemy.occupiedSlot != PenetrationSlot.None)
                {
                    int enemySelfArousal = skill.selfArousalGain > 0 ? skill.selfArousalGain : 15;
                    OnCombatLogMessage?.Invoke($"{enemy.name} experiences intense friction within your synthetic vessel ({enemy.occupiedSlot})!");

                    // Hook for mini-CG / animation between sexual movements
                    OnSexualMovementExecuted?.Invoke(enemy, skill, enemy.occupiedSlot);

                    if (CustomSexualMovementCGHandler != null)
                    {
                        yield return StartCoroutine(CustomSexualMovementCGHandler(enemy, skill, enemy.occupiedSlot));
                    }
                    else
                    {
                        yield return new WaitForSeconds(sexualMovementPauseDuration);
                    }

                    yield return StartCoroutine(ProcessEnemySeductionRoutine(enemy, enemySelfArousal));
                }

                // 6. Another 1 second pause before this enemy's turn concludes
                yield return new WaitForSeconds(1.0f);

                // Enemy's turn ends -> badge dims in timeline
                currentTurnCombatant = null;
                OnCombatantTurnEnded?.Invoke(enemy);

                // Check if player fell
                if (player.isDefeated)
                {
                    isActionInProgress = false;
                    CheckEndConditions();
                    yield break;
                }
            }

            // Enemies finished turn -> Start player's new turn
            isActionInProgress = false;
            StartPlayerTurn();
        }

        private void StartPlayerTurn()
        {
            isPlayerTurn = true;
            currentTurnCombatant = player;

            // Shield from previous round expires at the start of player's next turn
            if (player.shield > 0)
            {
                player.ResetShield();
                OnCombatLogMessage?.Invoke("Remaining shield expired.");
            }

            // Player gains +3 AP (capped at maxAp)
            player.GainAp(3);

            // Tick down card cooldowns across all cards
            var cdKeys = new List<string>(cardCooldowns.Keys);
            foreach (var k in cdKeys)
            {
                if (cardCooldowns[k] > 0)
                {
                    cardCooldowns[k]--;
                }
            }

            foreach (var card in equippedDeck)
            {
                card.TickCooldown();
            }

            // Build dynamic hand for this turn
            BuildTurnHand();

            OnTurnChanged?.Invoke(true);
            OnCombatantTurnStarted?.Invoke(player);
            OnHandUpdated?.Invoke();
            OnCombatLogMessage?.Invoke($"Your turn! Stance: {currentStance.ToString().ToUpper()} | Gained +3 AP (Total: {player.currentAp}/{player.maxAp} AP).");

            AutoSelectNextEnemy();
            CheckEndConditions();
        }

        public void SyncEquippedItemsToPlayer()
        {
            if (player == null || characterState == null) return;

            ItemData upperItem = characterState.GetEquippedItem(EquipSlot.UpperClothes);
            ItemData lowerItem = characterState.GetEquippedItem(EquipSlot.LowerClothes);
            ItemData underUpperItem = characterState.GetEquippedItem(EquipSlot.UnderUpper);
            ItemData underLowerItem = characterState.GetEquippedItem(EquipSlot.UnderLower);
            ItemData accessoryItem = characterState.GetEquippedItem(EquipSlot.Accessory);

            player.upperItem = upperItem;
            player.lowerItem = lowerItem;
            player.underUpperItem = underUpperItem;
            player.underLowerItem = underLowerItem;
            player.underwearItem = underLowerItem != null ? underLowerItem : underUpperItem;
            player.accessoryItem = accessoryItem;

            if (upperItem != null)
            {
                persistentMaxUpperClothesHp = upperItem.durability;
                player.upperArmorBuff = upperItem.damageReduction;
            }
            else
            {
                persistentUpperClothesHp = 0;
                persistentMaxUpperClothesHp = 0;
                player.upperArmorBuff = 0;
            }

            if (lowerItem != null)
            {
                persistentMaxLowerClothesHp = lowerItem.durability;
                player.lowerArmorBuff = lowerItem.damageReduction;
            }
            else
            {
                persistentLowerClothesHp = 0;
                persistentMaxLowerClothesHp = 0;
                player.lowerArmorBuff = 0;
            }

            if (underUpperItem != null)
            {
                persistentMaxUnderUpperClothesHp = underUpperItem.durability;
            }
            else
            {
                persistentUnderUpperClothesHp = 0;
                persistentMaxUnderUpperClothesHp = 0;
            }

            if (underLowerItem != null)
            {
                persistentMaxUnderLowerClothesHp = underLowerItem.durability;
            }
            else
            {
                persistentUnderLowerClothesHp = 0;
                persistentMaxUnderLowerClothesHp = 0;
            }

            if (upperItem != null && upperItem == lowerItem && (upperItem.IsCombinedArmor || upperItem.equipSlot == EquipSlot.CombinedClothes))
            {
                int syncedHp = Mathf.Min(persistentUpperClothesHp, persistentLowerClothesHp);
                if (syncedHp <= 0 && (persistentUpperClothesHp > 0 || persistentLowerClothesHp > 0))
                    syncedHp = Mathf.Max(persistentUpperClothesHp, persistentLowerClothesHp);
                else if (syncedHp <= 0 && tornClothesItem == null)
                    syncedHp = upperItem.durability;

                persistentUpperClothesHp = Mathf.Clamp(syncedHp, 0, persistentMaxUpperClothesHp);
                persistentLowerClothesHp = persistentUpperClothesHp;
            }

            player.maxUpperClothesHp = persistentMaxUpperClothesHp;
            player.upperClothesHp = Mathf.Clamp(persistentUpperClothesHp, 0, persistentMaxUpperClothesHp);
            player.maxLowerClothesHp = persistentMaxLowerClothesHp;
            player.lowerClothesHp = Mathf.Clamp(persistentLowerClothesHp, 0, persistentMaxLowerClothesHp);

            player.maxUnderUpperClothesHp = persistentMaxUnderUpperClothesHp;
            player.underUpperClothesHp = Mathf.Clamp(persistentUnderUpperClothesHp, 0, persistentMaxUnderUpperClothesHp);
            player.maxUnderLowerClothesHp = persistentMaxUnderLowerClothesHp;
            player.underLowerClothesHp = Mathf.Clamp(persistentUnderLowerClothesHp, 0, persistentMaxUnderLowerClothesHp);

            if (player.isWearingCombinedClothes)
            {
                player.lowerClothesHp = player.upperClothesHp;
            }
        }

        public void RecalculatePersistentStats(bool isNewExpedition = false)
        {
            int baseHp = 30;
            int bonusHp = 0;

            if (DesireTreeManager.Instance != null)
            {
                var bonuses = DesireTreeManager.Instance.GetTotalPassiveBonuses(activeLustPath);
                bonusHp += bonuses.hp;
            }

            if (characterState != null)
            {
                ItemData upper = characterState.GetEquippedItem(EquipSlot.UpperClothes);
                ItemData lower = characterState.GetEquippedItem(EquipSlot.LowerClothes);
                ItemData underwear = characterState.GetEquippedItem(EquipSlot.Underwear);
                ItemData accessory = characterState.GetEquippedItem(EquipSlot.Accessory);

                bool isCombined = upper != null && upper == lower && (upper.IsCombinedArmor || upper.equipSlot == EquipSlot.CombinedClothes);

                if (isCombined)
                {
                    if (persistentUpperClothesHp > 0)
                        bonusHp += upper.bonusMaxHp;
                }
                else
                {
                    if (upper != null && persistentUpperClothesHp > 0)
                        bonusHp += upper.bonusMaxHp;
                    if (lower != null && persistentLowerClothesHp > 0 && lower != upper)
                        bonusHp += lower.bonusMaxHp;
                }

                if (underwear != null && underwear != upper && underwear != lower)
                {
                    bonusHp += underwear.bonusMaxHp;
                }

                if (accessory != null && accessory != upper && accessory != lower && accessory != underwear)
                {
                    bonusHp += accessory.bonusMaxHp;
                }
            }

            persistentMaxHp = Mathf.Max(1, baseHp + bonusHp);

            if (isNewExpedition)
            {
                persistentHp = persistentMaxHp;
            }
            else
            {
                persistentHp = Mathf.Clamp(persistentHp, 1, persistentMaxHp);
            }

            OnPersistentStatsChanged?.Invoke();
        }

        public void StripUpperClothes()
        {
            if (characterState != null)
            {
                ItemData u = characterState.GetEquippedItem(EquipSlot.UpperClothes);
                if (u != null)
                {
                    tornUpperClothesItem = u;
                    characterState.UnequipItem(EquipSlot.UpperClothes);
                }
            }
            if (player != null)
            {
                player.upperClothesHp = 0;
                player.RecalculateStats();
            }
            persistentUpperClothesHp = 0;
            RecalculatePersistentStats(false);
            OnPlayerGarmentStripped?.Invoke(true, false);
            if (player != null && player.clothesHp <= 0)
            {
                OnCombatLogMessage?.Invoke("YOUR CLOTHES WERE COMPLETELY TORN TO SHREDS!");
                OnPlayerClothesStripped?.Invoke();
            }
        }

        public void StripLowerClothes()
        {
            if (characterState != null)
            {
                ItemData l = characterState.GetEquippedItem(EquipSlot.LowerClothes);
                if (l != null)
                {
                    tornLowerClothesItem = l;
                    characterState.UnequipItem(EquipSlot.LowerClothes);
                }
            }
            if (player != null)
            {
                player.lowerClothesHp = 0;
                player.RecalculateStats();
            }
            persistentLowerClothesHp = 0;
            RecalculatePersistentStats(false);
            OnPlayerGarmentStripped?.Invoke(false, true);
            if (player != null && player.clothesHp <= 0)
            {
                OnCombatLogMessage?.Invoke("YOUR CLOTHES WERE COMPLETELY TORN TO SHREDS!");
                OnPlayerClothesStripped?.Invoke();
            }
        }

        public void StripCombinedClothes()
        {
            if (characterState != null)
            {
                ItemData comb = characterState.GetEquippedItem(EquipSlot.CombinedClothes);
                if (comb != null)
                {
                    tornUpperClothesItem = comb;
                    tornLowerClothesItem = comb;
                    characterState.UnequipItem(EquipSlot.CombinedClothes);
                }
            }
            if (player != null)
            {
                player.upperClothesHp = 0;
                player.lowerClothesHp = 0;
                player.RecalculateStats();
            }
            persistentUpperClothesHp = 0;
            persistentLowerClothesHp = 0;
            RecalculatePersistentStats(false);
            OnPlayerGarmentStripped?.Invoke(true, true);
            OnCombatLogMessage?.Invoke("YOUR CLOTHES WERE COMPLETELY TORN TO SHREDS!");
            OnPlayerClothesStripped?.Invoke();
        }

        private void StripPlayerClothes()
        {
            if (player != null && player.isWearingCombinedClothes)
            {
                StripCombinedClothes();
            }
            else
            {
                StripUpperClothes();
                StripLowerClothes();
            }
        }

        /// <summary>
        /// Repairs clothes. If reEquip is false, player stays stripped/nude, but the item in inventory is repaired.
        /// </summary>
        public void RepairPlayerClothes(bool reEquip = false)
        {
            if (tornUpperClothesItem != null)
            {
                if (reEquip && characterState != null)
                {
                    characterState.EquipItem(tornUpperClothesItem);
                }
                tornUpperClothesItem = null;
            }

            if (tornLowerClothesItem != null)
            {
                if (reEquip && characterState != null && (tornUpperClothesItem == null || tornLowerClothesItem != tornUpperClothesItem))
                {
                    characterState.EquipItem(tornLowerClothesItem);
                }
                tornLowerClothesItem = null;
            }

            if (player != null)
            {
                player.upperClothesHp = player.maxUpperClothesHp;
                player.lowerClothesHp = player.maxLowerClothesHp;
                player.hasArmorBuff = player.CurrentArmorDamageReduction > 0;
                player.RecalculateStats();
            }
            persistentUpperClothesHp = persistentMaxUpperClothesHp;
            persistentLowerClothesHp = persistentMaxLowerClothesHp;
            RecalculatePersistentStats(false);
        }

        public void ResetPlayerStatsForNewExpedition()
        {
            persistentSeduction = 0;
            persistentSeductionStacks = 0;
            persistentShield = 0;
            persistentCorruption = 0;
            persistentSemenVolumeMl = 0;
            persistentSemenMouthMl = 0;
            persistentSemenWombMl = 0;
            persistentSemenAnalMl = 0;

            if (characterState != null)
            {
                ItemData upper = characterState.GetEquippedItem(EquipSlot.UpperClothes);
                ItemData lower = characterState.GetEquippedItem(EquipSlot.LowerClothes);
                ItemData underUpper = characterState.GetEquippedItem(EquipSlot.UnderUpper);
                ItemData underLower = characterState.GetEquippedItem(EquipSlot.UnderLower);

                if (upper != null)
                {
                    persistentMaxUpperClothesHp = upper.durability;
                    persistentUpperClothesHp = upper.durability;
                }
                else
                {
                    persistentMaxUpperClothesHp = 0;
                    persistentUpperClothesHp = 0;
                }

                if (lower != null)
                {
                    persistentMaxLowerClothesHp = lower.durability;
                    persistentLowerClothesHp = lower.durability;
                }
                else
                {
                    persistentMaxLowerClothesHp = 0;
                    persistentLowerClothesHp = 0;
                }

                if (upper != null && upper == lower && (upper.IsCombinedArmor || upper.equipSlot == EquipSlot.CombinedClothes))
                {
                    persistentLowerClothesHp = persistentUpperClothesHp;
                }

                if (underUpper != null)
                {
                    persistentMaxUnderUpperClothesHp = underUpper.durability;
                    persistentUnderUpperClothesHp = underUpper.durability;
                }
                else
                {
                    persistentMaxUnderUpperClothesHp = 35;
                    persistentUnderUpperClothesHp = 35;
                }

                if (underLower != null)
                {
                    persistentMaxUnderLowerClothesHp = underLower.durability;
                    persistentUnderLowerClothesHp = underLower.durability;
                }
                else
                {
                    persistentMaxUnderLowerClothesHp = 35;
                    persistentUnderLowerClothesHp = 35;
                }
            }
            else
            {
                persistentMaxUpperClothesHp = 65;
                persistentUpperClothesHp = 65;
                persistentMaxLowerClothesHp = 65;
                persistentLowerClothesHp = 65;
                persistentMaxUnderUpperClothesHp = 35;
                persistentUnderUpperClothesHp = 35;
                persistentMaxUnderLowerClothesHp = 35;
                persistentUnderLowerClothesHp = 35;
            }

            RecalculatePersistentStats(isNewExpedition: true);
        }

        public void SavePlayerStatsFromCombat()
        {
            if (player == null) return;
            persistentMaxHp = player.maxHp;
            persistentHp = Mathf.Clamp(player.currentHp, 0, persistentMaxHp);
            persistentUpperClothesHp = Mathf.Clamp(player.upperClothesHp, 0, persistentMaxUpperClothesHp);
            persistentLowerClothesHp = Mathf.Clamp(player.lowerClothesHp, 0, persistentMaxLowerClothesHp);
            persistentUnderUpperClothesHp = Mathf.Clamp(player.underUpperClothesHp, 0, persistentMaxUnderUpperClothesHp);
            persistentUnderLowerClothesHp = Mathf.Clamp(player.underLowerClothesHp, 0, persistentMaxUnderLowerClothesHp);
            if (player.isWearingCombinedClothes)
            {
                persistentLowerClothesHp = persistentUpperClothesHp;
            }
            persistentSeduction = player.seduction;
            persistentSeductionStacks = player.seductionStacks;
            persistentSemenMouthMl = player.semenMouthMl;
            persistentSemenWombMl = player.semenWombMl;
            persistentSemenAnalMl = player.semenAnalMl;
            persistentSemenVolumeMl = player.semenVolumeMl;
            OnPersistentStatsChanged?.Invoke();
        }

        public void HealPlayer(int amount)
        {
            persistentHp = Mathf.Clamp(persistentHp + amount, 0, persistentMaxHp);
            if (player != null)
            {
                player.currentHp = persistentHp;
                player.NotifyStateChanged();
            }
            OnPersistentStatsChanged?.Invoke();
        }

        public void RepairPlayerClothesAmount(int amount)
        {
            persistentUpperClothesHp = Mathf.Clamp(persistentUpperClothesHp + amount, 0, persistentMaxUpperClothesHp);
            persistentLowerClothesHp = Mathf.Clamp(persistentLowerClothesHp + amount, 0, persistentMaxLowerClothesHp);
            if (IsWearingCombinedClothes)
            {
                persistentLowerClothesHp = persistentUpperClothesHp;
            }
            if (player != null)
            {
                player.upperClothesHp = persistentUpperClothesHp;
                player.lowerClothesHp = persistentLowerClothesHp;
                player.hasArmorBuff = player.CurrentArmorDamageReduction > 0;
                player.RecalculateStats();
                player.NotifyStateChanged();
            }
            RecalculatePersistentStats(false);
        }

        public Combatant GetSelectedEnemy()
        {
            if (selectedEnemyIndex >= 0 && selectedEnemyIndex < enemies.Count)
            {
                Combatant e = enemies[selectedEnemyIndex];
                if (!e.isDefeated) return e;
            }
            AutoSelectNextEnemy();
            return (selectedEnemyIndex >= 0 && selectedEnemyIndex < enemies.Count) ? enemies[selectedEnemyIndex] : null;
        }

        private void AutoSelectNextEnemy()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (!enemies[i].isDefeated)
                {
                    selectedEnemyIndex = i;
                    return;
                }
            }
        }

        private void CheckEndConditions()
        {
            if (!isCombatActive) return;

            // 1. Check Player defeat
            if (player.isDefeated)
            {
                isCombatActive = false;
                StopAllCoroutines();
                isActionInProgress = false;

                if (persistentCorruption < maxCorruption)
                {
                    persistentCorruption = Mathf.Min(maxCorruption, persistentCorruption + 1);
                    persistentHp = 1;
                    if (player != null)
                    {
                        player.currentHp = 1;
                        player.isDefeated = false;
                        player.defeatedBySeduction = false;
                        player.seduction = 0;
                        player.seductionStacks = 0;
                        player.isFlustered = false;
                    }
                    persistentSeduction = 0;
                    persistentSeductionStacks = 0;

                    SavePlayerStatsFromCombat();

                    CombatResult result = new CombatResult
                    {
                        type = CombatResultType.CorruptionSurvival,
                        lumieresEarned = 0,
                        lustidEarned = 0
                    };
                    OnCombatLogMessage?.Invoke($"DEFEAT AVERTED! You survived with 1 HP, but dark corruption spreads (+1 Corruption: {persistentCorruption}/{maxCorruption})!");
                    OnCombatEnded?.Invoke(result);
                    return;
                }
                else
                {
                    CombatResult result = new CombatResult();
                    if (player.defeatedBySeduction)
                    {
                        result.type = CombatResultType.DefeatSeduction;
                        OnCombatLogMessage?.Invoke("You succumbed to overwhelming pleasure and collapsed in ecstasy! Total defeat.");
                    }
                    else
                    {
                        result.type = CombatResultType.DefeatHp;
                        OnCombatLogMessage?.Invoke("Your health was depleted and your soul was completely corrupted! Total defeat.");
                    }

                    // Reset stats when awakening back in sanctuary
                    ResetPlayerStatsForNewExpedition();

                    // Repair without auto-equipping (player stays nude)
                    RepairPlayerClothes(reEquip: false);

                    OnCombatEnded?.Invoke(result);
                    return;
                }
            }

            // 2. Check all enemies defeated
            bool allDefeated = true;
            int seducedCount = 0;
            int hpDefeatedCount = 0;
            Combatant lastSeduced = null;
            foreach (var enemy in enemies)
            {
                if (!enemy.isDefeated)
                {
                    allDefeated = false;
                }
                else
                {
                    if (enemy.occupiedSlot != PenetrationSlot.None || enemy.isGrapplingPlayer)
                    {
                        ReleaseEnemyHold(enemy);
                    }

                    if (enemy.defeatedBySeduction)
                    {
                        seducedCount++;
                        lastSeduced = enemy;
                    }
                    else
                    {
                        hpDefeatedCount++;
                    }
                }
            }

            if (allDefeated)
            {
                ReleaseAllEnemyHolds();
                isCombatActive = false;
                StopAllCoroutines();
                isActionInProgress = false;

                // Save persistent player stats from this victory
                SavePlayerStatsFromCombat();

                CombatResult result = new CombatResult
                {
                    totalEnemies = enemies.Count,
                    seducedEnemiesCount = seducedCount,
                    defeatedHpEnemiesCount = hpDefeatedCount,
                    lastSeducedEnemy = lastSeduced
                };

                if (seducedCount == enemies.Count)
                {
                    // 100% Pure Seduction / Overstimulation Victory
                    result.type = CombatResultType.VictorySeduction;
                    result.lumieresEarned = 35 * enemies.Count;
                    result.lustidEarned = 30 * seducedCount + (persistentSemenVolumeMl / 10);
                    OnCombatLogMessage?.Invoke($"[OVERSTIMULATION VICTORY] All {enemies.Count} opponents climaxed and submitted in ecstasy! Earned {result.lustidEarned} Lustid and {result.lumieresEarned} Lumieres!");
                }
                else if (seducedCount > 0)
                {
                    // Tactical Hybrid Victory: some seduced, some exhausted by weapon
                    result.type = CombatResultType.VictorySeduction;
                    result.lumieresEarned = (35 * seducedCount) + (25 * hpDefeatedCount);
                    result.lustidEarned = 30 * seducedCount + (persistentSemenVolumeMl / 10) + 5;
                    OnCombatLogMessage?.Invoke($"[TACTICAL HYBRID VICTORY] {seducedCount} opponent(s) submitted in ecstasy, {hpDefeatedCount} exhausted and retreated! Earned {result.lustidEarned} Lustid and {result.lumieresEarned} Lumieres!");
                }
                else
                {
                    // Pure Martial / HP Victory
                    result.type = CombatResultType.VictoryHp;
                    result.lumieresEarned = 25 * enemies.Count;
                    result.lustidEarned = 10;
                    OnCombatLogMessage?.Invoke($"[MARTIAL VICTORY] All {enemies.Count} opponents exhausted and retreated! Earned {result.lumieresEarned} Lumieres and {result.lustidEarned} Lustid!");

                    if (isPureRunActive)
                    {
                        result.isPureMartialRun = true;
                        int totalPureWins = PlayerPrefs.GetInt("Sanctum_PureCombatWins", 0) + 1;
                        PlayerPrefs.SetInt("Sanctum_PureCombatWins", totalPureWins);
                        PlayerPrefs.Save();
                        OnCombatLogMessage?.Invoke($"<color=#55EEFF><b>[UNTAINTED VICTORY]</b></color> Pure Run intact! ({totalPureWins} pure encounters won). Zero lust cards played, zero monster seed absorbed.");
                    }
                }

                if (result.lustidEarned > 0)
                {
                    CurrencyManager.Instance?.AddLustid(result.lustidEarned);
                }

                OnCombatEnded?.Invoke(result);
            }
        }

        public void Surrender()
        {
            if (!isCombatActive) return;
            isCombatActive = false;
            StopAllCoroutines();
            isActionInProgress = false;

            if (persistentCorruption < maxCorruption)
            {
                persistentCorruption = Mathf.Min(maxCorruption, persistentCorruption + 1);
                persistentHp = 1;
                if (player != null)
                {
                    player.currentHp = 1;
                    player.isDefeated = false;
                    player.defeatedBySeduction = false;
                    player.seduction = 0;
                    player.seductionStacks = 0;
                    player.isFlustered = false;
                }
                persistentSeduction = 0;
                persistentSeductionStacks = 0;
                SavePlayerStatsFromCombat();

                CombatResult result = new CombatResult
                {
                    type = CombatResultType.CorruptionSurvival,
                    lumieresEarned = 0,
                    lustidEarned = 0
                };
                OnCombatLogMessage?.Invoke($"You retreated through the shadows. Dark corruption spreads (+1 Corruption: {persistentCorruption}/{maxCorruption}).");
                OnCombatEnded?.Invoke(result);
            }
            else
            {
                RepairPlayerClothes(reEquip: false);
                ResetPlayerStatsForNewExpedition();

                CombatResult result = new CombatResult
                {
                    type = CombatResultType.DefeatHp,
                    lumieresEarned = 0,
                    lustidEarned = 0
                };
                OnCombatLogMessage?.Invoke("You surrendered and collapsed from total corruption.");
                OnCombatEnded?.Invoke(result);
            }
        }

        public void DebugInstantVictoryHp()
        {
            if (!isCombatActive) return;
            OnCombatLogMessage?.Invoke("[DEBUG BUTTON] Instant Victory (HP) triggered!");
            foreach (var enemy in enemies)
            {
                enemy.currentHp = 0;
                enemy.isDefeated = true;
                enemy.defeatedBySeduction = false;
            }
            CheckEndConditions();
        }

        public void DebugInstantVictorySeduction()
        {
            if (!isCombatActive) return;
            Combatant target = (selectedEnemyIndex >= 0 && selectedEnemyIndex < enemies.Count) ? enemies[selectedEnemyIndex] : (enemies.Count > 0 ? enemies[0] : null);
            if (target != null)
            {
                target.seduction = 100;
                target.seductionStacks = Mathf.Max(3, target.maxSeductionStacks);
                foreach (var enemy in enemies)
                {
                    if (enemy != target)
                    {
                        enemy.currentHp = 0;
                        enemy.isDefeated = true;
                    }
                }
            }
            CheckEndConditions();
        }
    }
}

