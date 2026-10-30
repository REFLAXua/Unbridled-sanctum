using System;
using System.Collections.Generic;
using UnityEngine;
using UnbridledSanctum.Data;

namespace UnbridledSanctum.Combat
{
    public enum TreeNodeType
    {
        MinorPassive,
        KeystoneCard
    }

    [System.Serializable]
    public class DesireTreeNode
    {
        public string id;
        public LustPathType path;
        public int tier; // 1, 2, 3
        public TreeNodeType nodeType;
        public string title;
        public string description;
        public int lustidCost;
        public string prerequisiteNodeId;
        public List<string> unlockedCardIds = new List<string>();
        public Vector2 treePos = Vector2.zero;

        // Passive bonuses granted by this node
        public int bonusHp = 0;
        public int bonusShield = 0;
        public int bonusCumShield = 0;
        public int bonusSeductionPower = 0;
        public int bonusWillpower = 0;

        public DesireTreeNode(
            string id,
            LustPathType path,
            int tier,
            TreeNodeType nodeType,
            string title,
            string description,
            int cost,
            string prereqId = null,
            string[] cardIds = null,
            int hp = 0,
            int shield = 0,
            int cumShield = 0,
            int seduction = 0,
            int willpower = 0,
            Vector2? pos = null)
        {
            this.id = id;
            this.path = path;
            this.tier = tier;
            this.nodeType = nodeType;
            this.title = title;
            this.description = description;
            this.lustidCost = cost;
            this.prerequisiteNodeId = prereqId;
            if (cardIds != null) this.unlockedCardIds.AddRange(cardIds);
            this.bonusHp = hp;
            this.bonusShield = shield;
            this.bonusCumShield = cumShield;
            this.bonusSeductionPower = seduction;
            this.bonusWillpower = willpower;
            this.treePos = pos ?? Vector2.zero;
        }
    }

    /// <summary>
    /// Manages the meta-progression tree for the 3 Paths of Desire.
    /// Tracks unlocked passive nodes and keystone fetish cards for each path.
    /// </summary>
    public class DesireTreeManager : MonoBehaviour
    {
        private const string PREF_KEY_TREE_NODES = "Sanctum_UnlockedDesireNodes";

        public static DesireTreeManager Instance { get; private set; }

        private List<DesireTreeNode> allNodes = new List<DesireTreeNode>();
        private HashSet<string> unlockedNodeIds = new HashSet<string>();

        public event Action<string> OnNodePurchased;
        public event Action OnTreeReset;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            InitializeTreeData();
            LoadProgress();
        }

        public static DesireTreeManager EnsureExists()
        {
            if (Instance != null) return Instance;
            var existing = FindFirstObjectByType<DesireTreeManager>();
            if (existing != null) { Instance = existing; return Instance; }

            GameObject go = new GameObject("DesireTreeManager");
            Instance = go.AddComponent<DesireTreeManager>();
            return Instance;
        }

        private void InitializeTreeData()
        {
            allNodes.Clear();

            // =========================================================================
            // PATH 1: PHALLUS DEVOTEE (Oral mastery, vaginal clenches, semen absorption)
            // =========================================================================
            // --- Sub-Branch A: Oral Mastery (Left Column, X = -130) ---
            allNodes.Add(new DesireTreeNode("p1_a1_breath", LustPathType.PhallusDevotee, 1, TreeNodeType.KeystoneCard,
                "Breath of Ecstasy", "Exhale softly to cool burning blood, reducing self Arousal by 15%. Adds [Breath of Ecstasy] to your desire pool.",
                cost: 0, prereqId: null, cardIds: new[] { "breath_of_ecstasy" }, pos: new Vector2(-130f, 135f)));

            allNodes.Add(new DesireTreeNode("p1_a2_throat", LustPathType.PhallusDevotee, 2, TreeNodeType.KeystoneCard,
                "Throat Lock", "Wrap lips and throat firmly around the opponent's shaft to seize control (+25% Arousal, 10 Stamina damage). Unlocks [Throat Lock].",
                cost: 35, prereqId: "p1_a1_breath", cardIds: new[] { "throat_lock" }, pos: new Vector2(-130f, 435f)));

            allNodes.Add(new DesireTreeNode("p1_a3_swallow", LustPathType.PhallusDevotee, 3, TreeNodeType.KeystoneCard,
                "Deep Swallow", "Take full length deep into your throat: provokes ejaculation (+40% Arousal, absorbs 50ml Cum, heals 8 HP). Unlocks [Deep Swallow].",
                cost: 65, prereqId: "p1_a2_throat", cardIds: new[] { "deep_swallow" }, pos: new Vector2(-130f, 735f)));

            // --- Sub-Branch B: Vaginal Coitus (Center Column, X = 0) ---
            allNodes.Add(new DesireTreeNode("p1_b1_glance", LustPathType.PhallusDevotee, 1, TreeNodeType.KeystoneCard,
                "Enticing Glance", "Cast a sultry, inviting glance that unsettles the foe's resolve (+20% Arousal). Unlocks [Enticing Glance].",
                cost: 15, prereqId: null, cardIds: new[] { "enticing_glance" }, pos: new Vector2(0f, 135f)));

            allNodes.Add(new DesireTreeNode("p1_b2_clench", LustPathType.PhallusDevotee, 2, TreeNodeType.KeystoneCard,
                "Vaginal Clench", "Contract inner walls rhythmically around the shaft, dealing 12 Stamina damage, +30% Arousal, and absorbing 30ml Cum. Unlocks [Vaginal Clench].",
                cost: 45, prereqId: "p1_b1_glance", cardIds: new[] { "vaginal_clench" }, pos: new Vector2(0f, 435f)));

            allNodes.Add(new DesireTreeNode("p1_b3_womb", LustPathType.PhallusDevotee, 3, TreeNodeType.KeystoneCard,
                "Womb Suction", "Engage deep uterine peristalsis around the invading head: strips 20 Stamina, deals +45% Arousal, and extracts 60ml Cum. Unlocks [Womb Suction].",
                cost: 75, prereqId: "p1_b2_clench", cardIds: new[] { "womb_suction" }, pos: new Vector2(0f, 735f)));

            // --- Sub-Branch C: Semen Transmutation (Right Column, X = +130) ---
            allNodes.Add(new DesireTreeNode("p1_c1_barrier", LustPathType.PhallusDevotee, 1, TreeNodeType.KeystoneCard,
                "Lust Barrier", "Channel absorbed Cum through the Womb Tattoo, converting stored Cum into Lust Shield. Unlocks [Lust Barrier].",
                cost: 20, prereqId: null, cardIds: new[] { "lust_barrier" }, pos: new Vector2(130f, 135f)));

            allNodes.Add(new DesireTreeNode("p1_c2_surge", LustPathType.PhallusDevotee, 2, TreeNodeType.KeystoneCard,
                "Cum Surge", "Transmute stored Cum into an explosive surge of condensed bio-barrier (+25 Lust Shield, +10 Shield). Unlocks [Cum Surge].",
                cost: 50, prereqId: "p1_c1_barrier", cardIds: new[] { "cum_surge" }, pos: new Vector2(130f, 435f)));

            allNodes.Add(new DesireTreeNode("p1_c3_surrender", LustPathType.PhallusDevotee, 3, TreeNodeType.KeystoneCard,
                "Overwhelming Surrender", "Surrender fully to the ecstasy of the embedded shaft: inflicts +50% Arousal, and if holding over 80ml Cum, restores 15 HP. Unlocks [Overwhelming Surrender].",
                cost: 85, prereqId: "p1_c2_surge", cardIds: new[] { "overwhelming_surrender" }, pos: new Vector2(130f, 735f)));

            // --- Cross-Branch Passives & Synergies ---
            allNodes.Add(new DesireTreeNode("p1_pas_throat_tone", LustPathType.PhallusDevotee, 1, TreeNodeType.MinorPassive,
                "Throat Elasticity", "Tissues in the throat adapt to stretching. +4 Max HP, +5 Willpower.",
                cost: 20, prereqId: "p1_a1_breath", hp: 4, willpower: 5, pos: new Vector2(-65f, 285f)));

            allNodes.Add(new DesireTreeNode("p1_pas_pelvic_seal", LustPathType.PhallusDevotee, 1, TreeNodeType.MinorPassive,
                "Pelvic Floor Tone", "Pelvic floor conditioning enhances absorption. +5 Lust Shield, +5% Seduction Power.",
                cost: 20, prereqId: "p1_c1_barrier", cumShield: 5, seduction: 5, pos: new Vector2(65f, 285f)));

            allNodes.Add(new DesireTreeNode("p1_pas_crucible_flow", LustPathType.PhallusDevotee, 2, TreeNodeType.MinorPassive,
                "Crucible Flow", "Stimulation flows harmoniously between oral and vaginal channels. +8 Lust Shield, +5 Shield.",
                cost: 40, prereqId: "p1_a2_throat", cumShield: 8, shield: 5, pos: new Vector2(-65f, 585f)));

            allNodes.Add(new DesireTreeNode("p1_pas_uterine_force", LustPathType.PhallusDevotee, 2, TreeNodeType.MinorPassive,
                "Living Crucible Awakening", "The Womb Tattoo transmutes seed into pure biological resilience. +8 Max HP, +8% Seduction Power.",
                cost: 40, prereqId: "p1_c2_surge", hp: 8, seduction: 8, pos: new Vector2(65f, 585f)));

            // --- Apex Master Node ---
            allNodes.Add(new DesireTreeNode("p1_apex_devotion", LustPathType.PhallusDevotee, 3, TreeNodeType.KeystoneCard,
                "Womb of Absolute Submission", "Apex devotion to the phallus. Permanent empowerment: +15 Lust Shield, +10 Max HP, +10% Seduction Power.",
                cost: 110, prereqId: "p1_b3_womb", cumShield: 15, hp: 10, seduction: 10, pos: new Vector2(0f, 915f)));

            // =========================================================================
            // PATH 2: BOUND & SUBMISSIVE (Bondage, masochistic ecstasy, reversal pins)
            // =========================================================================
            // --- Sub-Branch A: Ropes & Bondage (Left Column, X = -130) ---
            allNodes.Add(new DesireTreeNode("p2_a1_yielding", LustPathType.BoundSubmissive, 1, TreeNodeType.KeystoneCard,
                "Yielding Curve", "Arch back into an alluring, compliant posture, gaining 10 Shield while internal Arousal increases by 10%. Adds [Yielding Curve] to your desire pool.",
                cost: 0, prereqId: null, cardIds: new[] { "yielding_curve" }, pos: new Vector2(-130f, 135f)));

            allNodes.Add(new DesireTreeNode("p2_a2_kick", LustPathType.BoundSubmissive, 2, TreeNodeType.KeystoneCard,
                "Bound Kick", "Deliver a sharp, leveraged kick despite being held, dealing 6 HP damage and 16 Stamina posture damage. Unlocks [Bound Kick].",
                cost: 35, prereqId: "p2_a1_yielding", cardIds: new[] { "bound_kick" }, pos: new Vector2(-130f, 435f)));

            allNodes.Add(new DesireTreeNode("p2_a3_captivity", LustPathType.BoundSubmissive, 3, TreeNodeType.KeystoneCard,
                "Sweet Captivity", "Embrace the crushing weight of physical confinement, generating 12 Lust Shield and restoring 1 AP next turn. Unlocks [Sweet Captivity].",
                cost: 65, prereqId: "p2_a2_kick", cardIds: new[] { "sweet_captivity" }, pos: new Vector2(-130f, 735f)));

            // --- Sub-Branch B: Masochism & Spanking (Center Column, X = 0) ---
            allNodes.Add(new DesireTreeNode("p2_b1_spank", LustPathType.BoundSubmissive, 1, TreeNodeType.KeystoneCard,
                "Spank Reception", "Turn stinging physical blows into thrilling heat: gain 12 Shield, inflame enemy (+25% Arousal), rising self Arousal (+10%). Unlocks [Spank Reception].",
                cost: 15, prereqId: null, cardIds: new[] { "spank_reception" }, pos: new Vector2(0f, 135f)));

            allNodes.Add(new DesireTreeNode("p2_b2_whimper", LustPathType.BoundSubmissive, 2, TreeNodeType.KeystoneCard,
                "Flushed Whimper", "Let out a breathless, flushed whimper that disarms the foe (+25% Arousal) and dampens their next attack. Unlocks [Flushed Whimper].",
                cost: 45, prereqId: "p2_b1_spank", cardIds: new[] { "flushed_whimper" }, pos: new Vector2(0f, 435f)));

            allNodes.Add(new DesireTreeNode("p2_b3_ecstasy", LustPathType.BoundSubmissive, 3, TreeNodeType.KeystoneCard,
                "Masochistic Ecstasy", "Convert the humiliation of confinement into spiritual resilience (+25 Lust Shield, heals 10 HP, -20% Arousal). Unlocks [Masochistic Ecstasy].",
                cost: 75, prereqId: "p2_b2_whimper", cardIds: new[] { "masochistic_ecstasy" }, pos: new Vector2(0f, 735f)));

            // --- Sub-Branch C: Control & Reverse (Right Column, X = +130) ---
            allNodes.Add(new DesireTreeNode("p2_c1_murmur", LustPathType.BoundSubmissive, 1, TreeNodeType.KeystoneCard,
                "Submissive Murmur", "Whisper submissive praise directly against the enemy's ear, draining 10 Stamina and adding 20% Arousal. Unlocks [Submissive Murmur].",
                cost: 20, prereqId: null, cardIds: new[] { "submissive_murmur" }, pos: new Vector2(130f, 135f)));

            allNodes.Add(new DesireTreeNode("p2_c2_wrist", LustPathType.BoundSubmissive, 2, TreeNodeType.KeystoneCard,
                "Wrist Lock Reverse", "Use the opponent's pinning grip to wrench their wrist: deals 8 HP dmg, 18 Stamina damage, and breaks their hold. Unlocks [Wrist Lock Reverse].",
                cost: 50, prereqId: "p2_c1_murmur", cardIds: new[] { "wrist_lock_reverse" }, pos: new Vector2(130f, 435f)));

            allNodes.Add(new DesireTreeNode("p2_c3_leash", LustPathType.BoundSubmissive, 3, TreeNodeType.KeystoneCard,
                "Unbreakable Leash", "Wrap limbs and bindings tightly around the foe in an erotic lock, stripping 15 Stamina and inflicting +45% Arousal. Unlocks [Unbreakable Leash].",
                cost: 85, prereqId: "p2_c2_wrist", cardIds: new[] { "unbreakable_leash" }, pos: new Vector2(130f, 735f)));

            // --- Cross-Branch Passives & Synergies ---
            allNodes.Add(new DesireTreeNode("p2_pas_skin_toughen", LustPathType.BoundSubmissive, 1, TreeNodeType.MinorPassive,
                "Flesh Hardening", "Skin grows resistant to stinging rope friction. +6 Shield, +4 Max HP.",
                cost: 20, prereqId: "p2_a1_yielding", shield: 6, hp: 4, pos: new Vector2(-65f, 285f)));

            allNodes.Add(new DesireTreeNode("p2_pas_masoch_heat", LustPathType.BoundSubmissive, 1, TreeNodeType.MinorPassive,
                "Masochistic Heat", "Stinging blows inflame adrenaline instead of fear. +5% Seduction Power, +5 Willpower.",
                cost: 20, prereqId: "p2_c1_murmur", seduction: 5, willpower: 5, pos: new Vector2(65f, 285f)));

            allNodes.Add(new DesireTreeNode("p2_pas_struggle_tone", LustPathType.BoundSubmissive, 2, TreeNodeType.MinorPassive,
                "Struggle Counter Conditioning", "Muscles instinctively flex to punish pins. +8 Shield, +5 Willpower.",
                cost: 40, prereqId: "p2_a2_kick", shield: 8, willpower: 5, pos: new Vector2(-65f, 585f)));

            allNodes.Add(new DesireTreeNode("p2_pas_iron_submission", LustPathType.BoundSubmissive, 2, TreeNodeType.MinorPassive,
                "Iron Submission", "Total surrender grants unbreakable posture. +10 Shield, +6 Max HP.",
                cost: 40, prereqId: "p2_c2_wrist", shield: 10, hp: 6, pos: new Vector2(65f, 585f)));

            // --- Apex Master Node ---
            allNodes.Add(new DesireTreeNode("p2_apex_submission", LustPathType.BoundSubmissive, 3, TreeNodeType.KeystoneCard,
                "Ecstatic Confinement", "Apex mastery of submission. Permanent empowerment: +12 Shield, +12 Max HP, +10% Seduction Power.",
                cost: 110, prereqId: "p2_b3_ecstasy", shield: 12, hp: 12, seduction: 10, pos: new Vector2(0f, 915f)));

            // =========================================================================
            // PATH 3: MONSTER TRANSFORMATION (Pheromones, lactation, succubus womb)
            // =========================================================================
            // --- Sub-Branch A: Bust & Pheromones (Left Column, X = -130) ---
            allNodes.Add(new DesireTreeNode("p3_a1_spray", LustPathType.MonsterTransformation, 1, TreeNodeType.KeystoneCard,
                "Pheromone Spray", "Exude sweet, intoxicating succubus pheromones from skin pores (+20% enemy Arousal, 8 Stamina damage). Adds [Pheromone Spray] to your desire pool.",
                cost: 0, prereqId: null, cardIds: new[] { "pheromone_spray" }, pos: new Vector2(-130f, 135f)));

            allNodes.Add(new DesireTreeNode("p3_a2_bust", LustPathType.MonsterTransformation, 2, TreeNodeType.KeystoneCard,
                "Cushioning Bust", "Press your swelling, sensitive bust against the opponent to cushion force (+12 Shield) while driving them mad (+25% Arousal). Unlocks [Cushioning Bust].",
                cost: 35, prereqId: "p3_a1_spray", cardIds: new[] { "cushioning_bust" }, pos: new Vector2(-130f, 435f)));

            allNodes.Add(new DesireTreeNode("p3_a3_relief", LustPathType.MonsterTransformation, 3, TreeNodeType.KeystoneCard,
                "Milky Relief", "Release warm maternal secretions to soothe strained muscle and mind: heals 12 HP, restores 15 Stamina, cools Arousal by 25%. Unlocks [Milky Relief].",
                cost: 65, prereqId: "p3_a2_bust", cardIds: new[] { "milky_relief" }, pos: new Vector2(-130f, 735f)));

            // --- Sub-Branch B: Womb Sigil (Center Column, X = 0) ---
            allNodes.Add(new DesireTreeNode("p3_b1_pulse", LustPathType.MonsterTransformation, 1, TreeNodeType.KeystoneCard,
                "Womb Pulse", "The demonic Womb Tattoo flares beneath the skin: unleashes an ecstatic shockwave (+30% Arousal) and forms +12 Lust Shield. Unlocks [Womb Pulse].",
                cost: 15, prereqId: null, cardIds: new[] { "womb_pulse" }, pos: new Vector2(0f, 135f)));

            allNodes.Add(new DesireTreeNode("p3_b2_siphon", LustPathType.MonsterTransformation, 2, TreeNodeType.KeystoneCard,
                "Succubus Siphon", "Siphon vital heat directly through contact points: deals 15 Stamina damage, +35% Arousal, and regenerates 8 HP. Unlocks [Succubus Siphon].",
                cost: 45, prereqId: "p3_b1_pulse", cardIds: new[] { "succubus_siphon" }, pos: new Vector2(0f, 435f)));

            allNodes.Add(new DesireTreeNode("p3_b3_overdrive", LustPathType.MonsterTransformation, 3, TreeNodeType.KeystoneCard,
                "Womb Overdrive", "Trigger demonic contractions within the Living Crucible: drains 20 Stamina, +40% Arousal, extracts 50ml Cum, and gives 20 Lust Shield. Unlocks [Womb Overdrive].",
                cost: 75, prereqId: "p3_b2_siphon", cardIds: new[] { "womb_overdrive" }, pos: new Vector2(0f, 735f)));

            // --- Sub-Branch C: Demonic Assimilation (Right Column, X = +130) ---
            allNodes.Add(new DesireTreeNode("p3_c1_tail", LustPathType.MonsterTransformation, 1, TreeNodeType.KeystoneCard,
                "Tail Caress", "A shadowy demonic tail coils around the foe's groin and inner thighs, dealing 12 Stamina posture damage and +25% Arousal. Unlocks [Tail Caress].",
                cost: 20, prereqId: null, cardIds: new[] { "tail_caress" }, pos: new Vector2(130f, 135f)));

            allNodes.Add(new DesireTreeNode("p3_c2_slime", LustPathType.MonsterTransformation, 2, TreeNodeType.KeystoneCard,
                "Slime Skin Coating", "Exude a slick, slippery slime membrane across your body: grants 15 Shield, 10 Lust Shield, and slips out of enemy holds. Unlocks [Slime Skin Coating].",
                cost: 50, prereqId: "p3_c1_tail", cardIds: new[] { "slime_skin_coating" }, pos: new Vector2(130f, 435f)));

            allNodes.Add(new DesireTreeNode("p3_c3_primal", LustPathType.MonsterTransformation, 3, TreeNodeType.KeystoneCard,
                "Primal Metamorphosis", "Fully awaken your demonic traits: surge with +25 Lust Shield, heal 12 HP, and send nearby foes into ecstatic frenzy (+35% Arousal). Unlocks [Primal Metamorphosis].",
                cost: 85, prereqId: "p3_c2_slime", cardIds: new[] { "primal_metamorphosis" }, pos: new Vector2(130f, 735f)));

            // --- Cross-Branch Passives & Synergies ---
            allNodes.Add(new DesireTreeNode("p3_pas_mammary_flow", LustPathType.MonsterTransformation, 1, TreeNodeType.MinorPassive,
                "Mammary Swelling", "Chest blossoms with heightened nerve sensitivity. +4 Max HP, +5 Lust Shield.",
                cost: 20, prereqId: "p3_a1_spray", hp: 4, cumShield: 5, pos: new Vector2(-65f, 285f)));

            allNodes.Add(new DesireTreeNode("p3_pas_demon_blood", LustPathType.MonsterTransformation, 1, TreeNodeType.MinorPassive,
                "Demonic Assimilation", "Embracing monster essence cleanses fatigue. +5% Seduction Power, +5 Shield.",
                cost: 20, prereqId: "p3_c1_tail", seduction: 5, shield: 5, pos: new Vector2(65f, 285f)));

            allNodes.Add(new DesireTreeNode("p3_pas_nerve_bloom", LustPathType.MonsterTransformation, 2, TreeNodeType.MinorPassive,
                "Nerve Harmony", "Spinal nerve pathways open to supernatural pleasure. +8 Lust Shield, +6 Shield.",
                cost: 40, prereqId: "p3_a2_bust", cumShield: 8, shield: 6, pos: new Vector2(-65f, 585f)));

            allNodes.Add(new DesireTreeNode("p3_pas_succubus_anatomy", LustPathType.MonsterTransformation, 2, TreeNodeType.MinorPassive,
                "True Demon Anatomy", "Nerve pathways permanently harmonize with demonic sin. +10 Lust Shield, +6 Max HP.",
                cost: 40, prereqId: "p3_c2_slime", cumShield: 10, hp: 6, pos: new Vector2(65f, 585f)));

            // --- Apex Master Node ---
            allNodes.Add(new DesireTreeNode("p3_apex_metamorph", LustPathType.MonsterTransformation, 3, TreeNodeType.KeystoneCard,
                "Apex Metamorphosis", "Apex monster transformation. Permanent empowerment: +15 Lust Shield, +10 Max HP, +10% Seduction Power.",
                cost: 110, prereqId: "p3_b3_overdrive", cumShield: 15, hp: 10, seduction: 10, pos: new Vector2(0f, 915f)));
        }

        private void LoadProgress()
        {
            unlockedNodeIds.Clear();
            string saved = PlayerPrefs.GetString(PREF_KEY_TREE_NODES, "");
            if (!string.IsNullOrEmpty(saved))
            {
                string[] parts = saved.Split(',');
                foreach (var p in parts)
                {
                    if (!string.IsNullOrEmpty(p)) unlockedNodeIds.Add(p.Trim());
                }
            }

            // Always ensure the 0-cost base nodes are unlocked by default
            foreach (var n in allNodes)
            {
                if (n.lustidCost == 0) unlockedNodeIds.Add(n.id);
            }
        }

        private void SaveProgress()
        {
            string saved = string.Join(",", unlockedNodeIds);
            PlayerPrefs.SetString(PREF_KEY_TREE_NODES, saved);
            PlayerPrefs.Save();
        }

        public List<DesireTreeNode> GetNodesForPath(LustPathType path)
        {
            return allNodes.FindAll(n => n.path == path);
        }

        public DesireTreeNode GetNode(string nodeId)
        {
            return allNodes.Find(n => n.id == nodeId);
        }

        public bool IsNodeUnlocked(string nodeId)
        {
            return unlockedNodeIds.Contains(nodeId);
        }

        public bool CanUnlockNode(string nodeId)
        {
            if (IsNodeUnlocked(nodeId)) return false;

            var node = GetNode(nodeId);
            if (node == null) return false;

            // Must have prerequisite node unlocked if required
            if (!string.IsNullOrEmpty(node.prerequisiteNodeId) && !IsNodeUnlocked(node.prerequisiteNodeId))
            {
                return false;
            }

            // Must have enough Lustid
            int currentLustid = CurrencyManager.Instance != null ? CurrencyManager.Instance.TotalLustid : 0;
            return currentLustid >= node.lustidCost;
        }

        public bool TryUnlockNode(string nodeId)
        {
            if (!CanUnlockNode(nodeId)) return false;

            var node = GetNode(nodeId);
            if (node == null) return false;

            if (node.lustidCost > 0)
            {
                if (CurrencyManager.Instance == null || !CurrencyManager.Instance.TrySpendLustid(node.lustidCost))
                {
                    return false;
                }
            }

            unlockedNodeIds.Add(nodeId);
            SaveProgress();

            Debug.Log($"[DesireTreeManager] Node '{node.title}' ({node.id}) successfully unlocked for {node.lustidCost} Lustid!");
            OnNodePurchased?.Invoke(nodeId);
            return true;
        }

        public bool IsCardUnlocked(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return true;

            // Check if any unlocked keystone node grants this card
            foreach (var node in allNodes)
            {
                if (node.nodeType == TreeNodeType.KeystoneCard && node.unlockedCardIds.Contains(cardId))
                {
                    return unlockedNodeIds.Contains(node.id);
                }
            }

            // If not part of any desire tree (e.g. warrior cards, struggle cards), it's available
            return true;
        }

        public (int hp, int shield, int cumShield, int seduction, int willpower) GetTotalPassiveBonuses(LustPathType path)
        {
            int hp = 0, shield = 0, cumShield = 0, seduction = 0, willpower = 0;
            foreach (var node in allNodes)
            {
                if (node.path == path && unlockedNodeIds.Contains(node.id))
                {
                    hp += node.bonusHp;
                    shield += node.bonusShield;
                    cumShield += node.bonusCumShield;
                    seduction += node.bonusSeductionPower;
                    willpower += node.bonusWillpower;
                }
            }
            return (hp, shield, cumShield, seduction, willpower);
        }

        public const string PREF_KEY_COMPLETED_CHAPTER = "Sanctum_CompletedChapter";

        public static int GetCompletedChapter()
        {
            return PlayerPrefs.GetInt(PREF_KEY_COMPLETED_CHAPTER, 1);
        }

        public static void SetCompletedChapter(int chapter)
        {
            PlayerPrefs.SetInt(PREF_KEY_COMPLETED_CHAPTER, chapter);
            PlayerPrefs.Save();
        }

        public int GetRequiredChapterForPath(LustPathType path)
        {
            switch (path)
            {
                case LustPathType.PhallusDevotee:
                    return 1;
                case LustPathType.BoundSubmissive:
                    return 2;
                case LustPathType.MonsterTransformation:
                    return 3;
                default:
                    return 1;
            }
        }

        public bool IsPathUnlockedByChapter(LustPathType path)
        {
            if (PlayerPrefs.GetInt("Sanctum_PathUnlocked_" + path, 0) == 1) return true;

            int completedChapter = GetCompletedChapter();
            int requiredChapter = GetRequiredChapterForPath(path);
            return completedChapter >= requiredChapter;
        }

        public void UnlockPath(LustPathType path)
        {
            PlayerPrefs.SetInt("Sanctum_PathUnlocked_" + path, 1);
            PlayerPrefs.Save();
        }

        public void ResetAllTrees()
        {
            unlockedNodeIds.Clear();
            foreach (var n in allNodes)
            {
                if (n.lustidCost == 0) unlockedNodeIds.Add(n.id);
            }
            SaveProgress();
            OnTreeReset?.Invoke();
        }
    }
}
