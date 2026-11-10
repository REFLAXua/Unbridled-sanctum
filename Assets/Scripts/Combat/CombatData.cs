using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnbridledSanctum.Combat
{
    public enum CombatStance
    {
        Neutral,        // Weapon & distance combat (blade and shield)
        Grappled,       // Close-quarters physical grapple/pin
        Penetrated,     // Sexual union in combat (NSFW, clothes torn)
        ThroatLock      // Oral domination/throat lock hold
    }

    public enum PenetrationSlot
    {
        None = 0,
        Mouth = 1,
        Vagina = 2,
        Anal = 3,
        Sphincter = 3
    }

    public enum EntityGender
    {
        Futa = 0,   // Can grapple and penetrate into available orifices
        Female = 1  // Can only grapple, hold, pin, tease, caress; CANNOT penetrate
    }

    public enum CardCategory
    {
        WarriorTactics, // Core sword & shield combat (physical)
        LustInstinct,   // Ephemeral desire cards from chosen Path
        StruggleAction  // Grapple & clinch maneuvers (break free, caress, reverse)
    }

    public enum LustPathType
    {
        None,                   // Stage I: Denial & Shock (Warrior only)
        PhallusDevotee,         // Path 1: Oral mastery, vaginal clench, semen absorption
        BoundSubmissive,        // Path 2: Bondage, masochism, gagging, counter-strikes
        MonsterTransformation   // Path 3: Breast milk/lactation, womb tattoo, tentacles
    }

    public enum StanceRequirement
    {
        Any,            // Can be played in any stance
        NeutralOnly,    // Can only be played in Neutral stance (e.g. weapon attacks)
        StruggleOnly,   // Can only be played in Grappled, Penetrated, or ThroatLock
        PenetratedOnly  // Can only be played in Penetrated or ThroatLock
    }

    public enum CardTarget
    {
        SingleEnemy,
        Self,
        AllEnemies
    }

    public enum ExternalFinishZone
    {
        None,
        Hands,
        Breasts,
        Thighs,
        Face,
        Belly,
        Ass
    }

    [System.Serializable]
    public class CardData
    {
        public string id;
        public string cardName;
        public int apCost;
        public int cooldownTurns;
        public int hpDamage;
        public int shieldGain;
        public int seductionAmount;
        public CardTarget target;
        public string description;
        public int clothesRepair;
        public Sprite cardArt;

        [Header("Struggle & Desire Properties")]
        public CardCategory category = CardCategory.WarriorTactics;
        public LustPathType requiredPath = LustPathType.None;
        public StanceRequirement stanceRequirement = StanceRequirement.Any;
        public int staminaDamage = 0;
        public int arousalSelfDelta = 0;
        public int arousalTargetDelta = 0;
        public int semenAbsorbedMl = 0;
        public int lustShieldGain = 0;
        public bool isEphemeral = false;
        public PenetrationSlot requiredTargetSlot = PenetrationSlot.None;
        public ExternalFinishZone controlZone = ExternalFinishZone.None;
        public bool isFinisherExecution = false;

        public CardData(string id, string name, int ap, int cd, int dmg, int shield, int seduction, CardTarget target, string desc)
        {
            this.id = id;
            this.cardName = name;
            this.apCost = ap;
            this.cooldownTurns = Mathf.Max(1, cd);
            this.hpDamage = dmg;
            this.shieldGain = shield;
            this.seductionAmount = seduction;
            this.target = target;
            this.description = desc;
        }

        public CardData(string id, string name, int ap, int cd, int dmg, int shield, int seduction, CardTarget target, string desc,
            CardCategory category, LustPathType path = LustPathType.None, StanceRequirement stance = StanceRequirement.Any,
            int staminaDmg = 0, int arousalSelf = 0, int arousalTarget = 0, int semenMl = 0, int lustShield = 0, bool ephemeral = false,
            PenetrationSlot reqSlot = PenetrationSlot.None, ExternalFinishZone finishZone = ExternalFinishZone.None, bool isFinisher = false)
            : this(id, name, ap, cd, dmg, shield, seduction, target, desc)
        {
            this.category = category;
            this.requiredPath = path;
            this.stanceRequirement = stance;
            this.staminaDamage = staminaDmg;
            this.arousalSelfDelta = arousalSelf;
            this.arousalTargetDelta = arousalTarget;
            this.semenAbsorbedMl = semenMl;
            this.lustShieldGain = lustShield;
            this.isEphemeral = ephemeral;
            this.requiredTargetSlot = reqSlot;
            this.controlZone = finishZone;
            this.isFinisherExecution = isFinisher;
        }

        public PenetrationSlot GetRequiredSlot()
        {
            if (requiredTargetSlot != PenetrationSlot.None) return requiredTargetSlot;

            string lid = (id ?? "").ToLowerInvariant();
            string lname = (cardName ?? "").ToLowerInvariant();

            if (lid.Contains("throat") || lid.Contains("swallow") || lname.Contains("throat") || lname.Contains("swallow") || lname.Contains("oral"))
                return PenetrationSlot.Mouth;

            if (lid.Contains("vaginal") || lname.Contains("vaginal") || lname.Contains("cervix") || lname.Contains("womb"))
                return PenetrationSlot.Vagina;

            if (lid.Contains("anal") || lid.Contains("sphincter") || lname.Contains("anal") || lname.Contains("sphincter"))
                return PenetrationSlot.Anal;

            return PenetrationSlot.None;
        }

        public bool RequiresPenetratingTarget()
        {
            if (GetRequiredSlot() != PenetrationSlot.None) return true;
            if (stanceRequirement == StanceRequirement.PenetratedOnly) return true;
            string lid = (id ?? "").ToLowerInvariant();
            if (lid.Contains("counter_clench") || lid.Contains("internal")) return true;
            return false;
        }

        public bool IsValidTargetEnemy(Combatant enemy, CombatStance stance)
        {
            if (enemy == null || enemy.isDefeated) return false;

            PenetrationSlot req = GetRequiredSlot();
            if (req != PenetrationSlot.None)
            {
                // Must target the enemy who is occupying this specific orifice!
                return enemy.occupiedSlot == req;
            }

            if (RequiresPenetratingTarget())
            {
                // Must target an enemy that is penetrating ANY orifice!
                return enemy.occupiedSlot != PenetrationSlot.None;
            }

            if (stanceRequirement == StanceRequirement.StruggleOnly)
            {
                // Must target an enemy that is grappling or penetrating
                return enemy.isGrapplingPlayer || enemy.occupiedSlot != PenetrationSlot.None;
            }

            return true;
        }

        public CardData Clone()
        {
            var clone = new CardData(this.id, this.cardName, this.apCost, this.cooldownTurns, this.hpDamage, this.shieldGain,
                this.seductionAmount, this.target, this.description, this.category, this.requiredPath, this.stanceRequirement,
                this.staminaDamage, this.arousalSelfDelta, this.arousalTargetDelta, this.semenAbsorbedMl, this.lustShieldGain,
                this.isEphemeral, this.requiredTargetSlot, this.controlZone, this.isFinisherExecution);
            clone.clothesRepair = this.clothesRepair;
            clone.cardArt = this.cardArt;
            return clone;
        }
    }

    [System.Serializable]
    public class CardInstance
    {
        public CardData data;
        public int currentCooldown;

        public CardInstance(CardData data)
        {
            this.data = data;
            this.currentCooldown = 0;
        }

        public bool CanPlay(int currentAp)
        {
            return currentCooldown <= 0 && currentAp >= data.apCost;
        }

        public bool CanPlay(int currentAp, CombatStance stance)
        {
            if (currentCooldown > 0 || currentAp < data.apCost) return false;

            switch (data.stanceRequirement)
            {
                case StanceRequirement.NeutralOnly:
                    return stance == CombatStance.Neutral;
                case StanceRequirement.StruggleOnly:
                    return stance != CombatStance.Neutral;
                case StanceRequirement.PenetratedOnly:
                    return stance == CombatStance.Penetrated || stance == CombatStance.ThroatLock;
                case StanceRequirement.Any:
                default:
                    return true;
            }
        }

        public void PutOnCooldown()
        {
            currentCooldown = Mathf.Max(1, data.cooldownTurns);
        }

        public void TickCooldown()
        {
            if (currentCooldown > 0)
            {
                currentCooldown--;
            }
        }
    }

    public enum EnemyType
    {
        SlimeGirl,
        Succubus,
        ElvenScout
    }

    public enum ArmorTargetSlot
    {
        All,
        Upper,
        Lower
    }

    public enum GrappleZone
    {
        None = 0,
        Upper = 1, // Face & Head Hold (Prepares Mouth / Oral penetration)
        Lower = 2  // Thighs & Pelvis Lock (Prepares Vagina / Manual penetration)
    }

    [System.Serializable]
    public class EnemySkill
    {
        public string skillName;
        public int manaCost;
        public int hpDamage;
        public int clothesDamage;
        public ArmorTargetSlot armorTarget;
        public int seductionDamage;
        public int shieldGain;
        public int manaGain;
        public string description;
        public int staminaDamage;
        public int pranaGain;
        public int selfArousalGain;
        public CombatStance stanceRequirement;
        public CombatStance? transitionToStance;
        public PenetrationSlot targetPenetrationSlot;
        public GrappleZone targetGrappleZone;

        public EnemySkill(
            string name,
            int manaCost,
            int hpDmg,
            int clothesDmg,
            int seductionDmg,
            int shieldGain,
            int manaGain,
            string desc,
            ArmorTargetSlot armorTarget = ArmorTargetSlot.All,
            int staminaDmg = 0,
            int pranaGain = 0,
            CombatStance stanceReq = CombatStance.Neutral,
            CombatStance? transitionTo = null,
            int selfArousal = 0,
            PenetrationSlot targetSlot = PenetrationSlot.None,
            GrappleZone targetGrapple = GrappleZone.None)
        {
            this.skillName = name;
            this.manaCost = manaCost;
            this.hpDamage = hpDmg;
            this.clothesDamage = clothesDmg;
            this.armorTarget = armorTarget;
            this.seductionDamage = seductionDmg;
            this.shieldGain = shieldGain;
            this.manaGain = manaGain;
            this.description = desc;
            this.staminaDamage = staminaDmg;
            this.pranaGain = pranaGain;
            this.stanceRequirement = stanceReq;
            this.transitionToStance = transitionTo;
            this.selfArousalGain = selfArousal;
            this.targetPenetrationSlot = targetSlot;
            this.targetGrappleZone = targetGrapple;
        }
    }

    [System.Serializable]
    public class Combatant
    {
        public bool isPlayer;
        public string name;
        public EnemyType enemyType;

        public int baseMaxHp = 30;
        public int currentHp;
        public int maxHp;

        public int baseMaxMana = 0;
        public int currentMana;
        public int maxMana;

        public int currentAp; // Accumulated AP for player
        public int maxAp = 3;

        public int shield;

        [Header("Stamina & Posture (Struggle Engine)")]
        public int baseMaxStamina = 20;
        public int maxStamina = 20;
        public int currentStamina = 20;

        [Header("Gender & Anatomical Capabilities")]
        public EntityGender gender = EntityGender.Futa;
        public bool canPenetrate => true; // Both Futa and Female entities can enter intimate engagement/penetration (fingers, mouth, or phallus based on skills)

        [Header("Grapple & Penetration State")]
        public PenetrationSlot occupiedSlot = PenetrationSlot.None;
        public bool isGrapplingPlayer = false;
        public GrappleZone heldGrappleZone = GrappleZone.None;

        public bool isPenetrating => occupiedSlot != PenetrationSlot.None;
        public bool isGrappling => isGrapplingPlayer && !isPenetrating;

        [Header("Prana & Semen Absorption")]
        public int semenVolumeMl = 0;
        public int semenMouthMl = 0;
        public const int MAX_SEMEN_MOUTH_ML = 100;
        public int semenWombMl = 0;
        public const int MAX_SEMEN_WOMB_ML = 200;
        public int semenAnalMl = 0;
        public const int MAX_SEMEN_ANAL_ML = 150;
        public int lustShield = 0;

        [Header("Dual Armor & Clothes Durability (Outer & Under Layers)")]
        public int upperClothesHp;
        public int maxUpperClothesHp;
        public int lowerClothesHp;
        public int maxLowerClothesHp;

        public int underUpperClothesHp;
        public int maxUnderUpperClothesHp;
        public int underLowerClothesHp;
        public int maxUnderLowerClothesHp;

        public int upperArmorBuff = 1;
        public int lowerArmorBuff = 1;

        public ItemData upperItem;
        public ItemData lowerItem;
        public ItemData underUpperItem;
        public ItemData underLowerItem;
        public ItemData underwearItem; // Backward compatibility alias
        public ItemData accessoryItem;

        public bool isWearingCombinedClothes => upperItem != null && upperItem == lowerItem && (upperItem.IsCombinedArmor || upperItem.equipSlot == EquipSlot.CombinedClothes);

        public bool isOuterUpperTorn => upperClothesHp <= 0;
        public bool isUnderUpperTorn => underUpperClothesHp <= 0;
        public bool isBreastsExposed => isOuterUpperTorn && isUnderUpperTorn;

        public bool isOuterLowerTorn => lowerClothesHp <= 0;
        public bool isUnderLowerTorn => underLowerClothesHp <= 0;
        public bool isGroinExposed => isOuterLowerTorn && isUnderLowerTorn;

        // Total clothes HP helper for backward compatibility & checks
        public int clothesHp => isWearingCombinedClothes 
            ? (upperClothesHp + underUpperClothesHp + underLowerClothesHp) 
            : (upperClothesHp + lowerClothesHp + underUpperClothesHp + underLowerClothesHp);

        public int maxClothesHp => isWearingCombinedClothes 
            ? (maxUpperClothesHp + maxUnderUpperClothesHp + maxUnderLowerClothesHp) 
            : (maxUpperClothesHp + maxLowerClothesHp + maxUnderUpperClothesHp + maxUnderLowerClothesHp);

        public bool isUpperTorn => isBreastsExposed;
        public bool isLowerTorn => isGroinExposed; // Penetration strictly requires groin to be exposed (panties torn)
        public bool isCompletelyStripped => isBreastsExposed && isGroinExposed;

        /// <summary>
        /// Calculates stat contribution from active (intact) clothes and equipped items.
        /// For combined clothes, provides full stat as long as the unified garment is intact.
        /// For separate pieces, gives 100% of the stat while that specific piece is intact.
        /// Underwear and accessories provide stats continuously while equipped.
        /// </summary>
        public int GetActiveClothesStat(System.Func<ItemData, int> selector)
        {
            if (!isPlayer || selector == null) return 0;
            int total = 0;
            if (isWearingCombinedClothes)
            {
                if (upperItem != null && upperClothesHp > 0)
                {
                    total += selector(upperItem);
                }
            }
            else
            {
                if (upperItem != null && upperClothesHp > 0)
                    total += selector(upperItem);
                if (lowerItem != null && lowerClothesHp > 0 && lowerItem != upperItem)
                    total += selector(lowerItem);
            }

            if (underUpperItem != null && underUpperItem != upperItem && underUpperItem != lowerItem && underUpperClothesHp > 0)
            {
                total += selector(underUpperItem);
            }

            if (underLowerItem != null && underLowerItem != upperItem && underLowerItem != lowerItem && underLowerItem != underUpperItem && underLowerClothesHp > 0)
            {
                total += selector(underLowerItem);
            }

            if (underwearItem != null && underwearItem != upperItem && underwearItem != lowerItem && underwearItem != underUpperItem && underwearItem != underLowerItem)
            {
                total += selector(underwearItem);
            }

            if (accessoryItem != null && accessoryItem != upperItem && accessoryItem != lowerItem && accessoryItem != underwearItem)
            {
                total += selector(accessoryItem);
            }

            return total;
        }

        [Header("Meta-Tree Passive Bonuses")]
        public int passiveBonusMaxHp = 0;
        public int passiveBonusSeductionPower = 0;
        public int passiveWillpower = 0;

        public int CurrentBonusMaxHp => GetActiveClothesStat(item => item.bonusMaxHp) + passiveBonusMaxHp;
        public int CurrentBonusMaxMana => GetActiveClothesStat(item => item.bonusMaxMana);

        public int CurrentArmorDamageReduction
        {
            get
            {
                if (!isPlayer) return 0;
                int red = GetActiveClothesStat(item => item.damageReduction);
                if (upperItem == null && upperClothesHp > 0) red += upperArmorBuff;
                if (lowerItem == null && lowerClothesHp > 0) red += lowerArmorBuff;
                return red;
            }
        }

        public int CurrentBonusSeductionPower => GetActiveClothesStat(item => item.bonusSeductionPower) + passiveBonusSeductionPower;
        public int CurrentSeductionResistance => GetActiveClothesStat(item => item.seductionResistance) + passiveWillpower;

        public System.Collections.Generic.List<string> ActivePassiveEffects
        {
            get
            {
                var list = new System.Collections.Generic.List<string>();
                if (upperItem != null && upperClothesHp > 0 && upperItem.passiveEffectIds != null)
                {
                    list.AddRange(upperItem.passiveEffectIds);
                }
                if (lowerItem != null && lowerClothesHp > 0 && lowerItem != upperItem && lowerItem.passiveEffectIds != null)
                {
                    list.AddRange(lowerItem.passiveEffectIds);
                }
                if (underwearItem != null && underwearItem != upperItem && underwearItem != lowerItem && underwearItem.passiveEffectIds != null)
                {
                    list.AddRange(underwearItem.passiveEffectIds);
                }
                if (accessoryItem != null && accessoryItem != upperItem && accessoryItem != lowerItem && accessoryItem != underwearItem && accessoryItem.passiveEffectIds != null)
                {
                    list.AddRange(accessoryItem.passiveEffectIds);
                }
                return list;
            }
        }

        public void RecalculateStats()
        {
            if (!isPlayer) return;
            maxHp = Mathf.Max(1, baseMaxHp + CurrentBonusMaxHp);
            currentHp = Mathf.Clamp(currentHp, 1, maxHp);

            maxMana = Mathf.Max(0, baseMaxMana + CurrentBonusMaxMana);
            currentMana = Mathf.Clamp(currentMana, 0, maxMana);

            hasArmorBuff = CurrentArmorDamageReduction > 0;
        }

        public void GainAp(int amount)
        {
            currentAp = Mathf.Clamp(currentAp + amount, 0, maxAp);
            OnStateChanged?.Invoke();
        }

        public void ConsumeAp(int amount)
        {
            currentAp = Mathf.Max(0, currentAp - amount);
            OnStateChanged?.Invoke();
        }

        public bool hasArmorBuff; // True while any clothes HP remains

        public int seduction; // 0 to 100
        public int arousal { get => seduction; set => seduction = value; } // Seamless alias to seduction
        public int seductionStacks; // current stacks
        public int maxSeductionStacks = 3; // Customizable per enemy/buffs
        public bool isFlustered; // Debuff applied upon filling 100%

        public bool isDefeated;
        public bool defeatedBySeduction;

        public event Action OnStateChanged;

        public static Combatant CreatePlayer(int hp = 30, int upperDurability = 65, int lowerDurability = 65, int underUpperDurability = 35, int underLowerDurability = 35)
        {
            Combatant c = new Combatant
            {
                isPlayer = true,
                name = "Player",
                baseMaxHp = 30,
                maxHp = hp,
                currentHp = hp,
                baseMaxMana = 10,
                maxMana = 10,
                currentMana = 10,
                currentAp = 3,
                maxAp = 3,
                shield = 0,
                baseMaxStamina = 25,
                maxStamina = 25,
                currentStamina = 25,
                semenVolumeMl = 0,
                lustShield = 0,
                maxUpperClothesHp = upperDurability,
                upperClothesHp = upperDurability,
                maxLowerClothesHp = lowerDurability,
                lowerClothesHp = lowerDurability,
                maxUnderUpperClothesHp = underUpperDurability,
                underUpperClothesHp = underUpperDurability,
                maxUnderLowerClothesHp = underLowerDurability,
                underLowerClothesHp = underLowerDurability,
                upperArmorBuff = 1,
                lowerArmorBuff = 1,
                hasArmorBuff = (upperDurability > 0 || lowerDurability > 0 || underUpperDurability > 0 || underLowerDurability > 0),
                seduction = 0,
                seductionStacks = 0,
                isFlustered = false,
                isDefeated = false,
                defeatedBySeduction = false
            };
            c.RecalculateStats();
            return c;
        }

        public static Combatant CreateEnemy(EnemyType type, int floorLevel = 1)
        {
            Combatant c = new Combatant
            {
                isPlayer = false,
                enemyType = type,
                currentAp = 1,
                shield = 0,
                baseMaxStamina = 20 + (floorLevel * 4),
                maxStamina = 20 + (floorLevel * 4),
                currentStamina = 20 + (floorLevel * 4),
                semenVolumeMl = 0,
                lustShield = 0,
                upperClothesHp = 0,
                maxUpperClothesHp = 0,
                lowerClothesHp = 0,
                maxLowerClothesHp = 0,
                hasArmorBuff = false,
                seduction = 0,
                seductionStacks = 0,
                isFlustered = false,
                isDefeated = false,
                defeatedBySeduction = false
            };

            EnemyDefinitionSO def = (CombatManager.Instance != null) ? CombatManager.Instance.GetEnemyDefinition(type) : null;
            if (def != null)
            {
                c.name = def.enemyName;
                c.gender = def.gender;
                c.maxHp = def.baseHp + (floorLevel * def.hpPerFloor);
                c.currentHp = c.maxHp;
                c.maxMana = def.maxMana;
                c.currentMana = def.startingMana;
                c.maxSeductionStacks = def.maxSeductionStacks;
                c.shield = def.baseShield;
                return c;
            }

            c.gender = EntityGender.Futa;

            switch (type)
            {
                case EnemyType.SlimeGirl:
                    c.name = "Slime Girl";
                    c.maxHp = 22 + (floorLevel * 3);
                    c.currentHp = c.maxHp;
                    c.maxMana = 15;
                    c.currentMana = 10;
                    c.baseMaxStamina = 20;
                    c.maxStamina = 20;
                    c.currentStamina = 20;
                    break;

                case EnemyType.Succubus:
                    c.name = "Succubus";
                    c.maxHp = 26 + (floorLevel * 4);
                    c.currentHp = c.maxHp;
                    c.maxMana = 30;
                    c.currentMana = 20;
                    c.baseMaxStamina = 30;
                    c.maxStamina = 30;
                    c.currentStamina = 30;
                    break;

                case EnemyType.ElvenScout:
                    c.name = "Elven Scout";
                    c.gender = EntityGender.Female;
                    c.maxHp = 45 + (floorLevel * 4);
                    c.currentHp = c.maxHp;
                    c.maxMana = 15;
                    c.currentMana = 15;
                    c.baseMaxStamina = 20;
                    c.maxStamina = 20;
                    c.currentStamina = 20;
                    c.maxSeductionStacks = 2; // 2 phases (25 Lust each = 50 total lust threshold)
                    c.upperClothesHp = 25;
                    c.maxUpperClothesHp = 25;
                    c.lowerClothesHp = 30;
                    c.maxLowerClothesHp = 30;
                    c.hasArmorBuff = true;
                    break;
            }

            return c;
        }

        public void StripUpper()
        {
            upperClothesHp = 0;
            OnStateChanged?.Invoke();
        }

        public void StripLower()
        {
            lowerClothesHp = 0;
            OnStateChanged?.Invoke();
        }

        public void StripAll()
        {
            upperClothesHp = 0;
            lowerClothesHp = 0;
            OnStateChanged?.Invoke();
        }

        public int TakeHpDamage(int rawDamage)
        {
            if (rawDamage <= 0 || isDefeated) return 0;

            int damage = rawDamage;

            // Apply Flustered debuff multiplier
            if (isFlustered)
            {
                damage = Mathf.RoundToInt(damage * 1.25f);
            }

            // Apply Clothing Armor buff for Player: reduces incoming HP damage based on intact pieces
            int reduction = CurrentArmorDamageReduction;
            if (isPlayer && reduction > 0 && damage > 1)
            {
                damage = Mathf.Max(1, damage - reduction);
            }

            // Lust Shield absorption first (Prana barrier)
            if (lustShield > 0)
            {
                if (lustShield >= damage)
                {
                    lustShield -= damage;
                    damage = 0;
                }
                else
                {
                    damage -= lustShield;
                    lustShield = 0;
                }
            }

            // Shield absorption
            if (damage > 0 && shield > 0)
            {
                if (shield >= damage)
                {
                    shield -= damage;
                    damage = 0;
                }
                else
                {
                    damage -= shield;
                    shield = 0;
                }
            }

            // Apply remaining damage to HP
            currentHp = Mathf.Max(0, currentHp - damage);

            if (currentHp <= 0)
            {
                isDefeated = true;
                defeatedBySeduction = false;
            }

            OnStateChanged?.Invoke();
            return damage;
        }

        public int TakeClothesDamage(int rawDamage, ArmorTargetSlot targetSlot = ArmorTargetSlot.All)
        {
            if (!isPlayer || isDefeated || clothesHp <= 0 || rawDamage <= 0) return 0;

            int actualDamageTaken = 0;

            if (targetSlot == ArmorTargetSlot.Upper)
            {
                actualDamageTaken = ApplyUpperZoneDamage(rawDamage);
            }
            else if (targetSlot == ArmorTargetSlot.Lower)
            {
                actualDamageTaken = ApplyLowerZoneDamage(rawDamage);
            }
            else // ArmorTargetSlot.All
            {
                int half = Mathf.CeilToInt(rawDamage / 2f);
                actualDamageTaken = ApplyUpperZoneDamage(half) + ApplyLowerZoneDamage(rawDamage - half);
            }

            hasArmorBuff = CurrentArmorDamageReduction > 0;
            RecalculateStats();
            OnStateChanged?.Invoke();
            return actualDamageTaken;
        }

        private int ApplyUpperZoneDamage(int amount)
        {
            if (amount <= 0) return 0;
            int taken = 0;

            // 1. First line of defense: Outer Upper (or Combined suit)
            if (upperClothesHp > 0)
            {
                int dmg = Mathf.Min(upperClothesHp, amount);
                upperClothesHp -= dmg;
                taken += dmg;
                amount -= dmg;
                if (isWearingCombinedClothes) lowerClothesHp = upperClothesHp;
            }

            // 2. Second line of defense: Under Upper (Bra / Lingerie)
            if (amount > 0 && underUpperClothesHp > 0)
            {
                int dmg = Mathf.Min(underUpperClothesHp, amount);
                underUpperClothesHp -= dmg;
                taken += dmg;
                amount -= dmg;
            }

            return taken;
        }

        private int ApplyLowerZoneDamage(int amount)
        {
            if (amount <= 0) return 0;
            int taken = 0;

            // 1. First line of defense: Outer Lower (or Combined suit)
            if (lowerClothesHp > 0)
            {
                int dmg = Mathf.Min(lowerClothesHp, amount);
                lowerClothesHp -= dmg;
                taken += dmg;
                amount -= dmg;
                if (isWearingCombinedClothes) upperClothesHp = lowerClothesHp;
            }

            // 2. Second line of defense: Under Lower (Panties)
            if (amount > 0 && underLowerClothesHp > 0)
            {
                int dmg = Mathf.Min(underLowerClothesHp, amount);
                underLowerClothesHp -= dmg;
                taken += dmg;
                amount -= dmg;
            }

            return taken;
        }

        public int AddSeduction(int rawAmount)
        {
            if (isDefeated) return 0;

            int amount = rawAmount;
            if (isPlayer && CurrentSeductionResistance != 0)
            {
                amount = Mathf.Max(0, amount - CurrentSeductionResistance);
            }

            if (isFlustered)
            {
                amount = Mathf.RoundToInt(amount * 1.25f);
            }

            seduction += amount;

            while (seduction >= 100 && seductionStacks < maxSeductionStacks)
            {
                seduction -= 100;
                seductionStacks++;
                isFlustered = true; // Status effect applied

                if (seductionStacks >= maxSeductionStacks)
                {
                    seduction = 100;
                    isDefeated = true;
                    defeatedBySeduction = true;
                    break;
                }
            }

            OnStateChanged?.Invoke();
            return amount;
        }

        public void AddShield(int amount)
        {
            shield += amount;
            OnStateChanged?.Invoke();
        }

        public void ResetShield()
        {
            shield = 0;
            OnStateChanged?.Invoke();
        }

        public int TakeStaminaDamage(int rawAmount)
        {
            if (rawAmount <= 0 || isDefeated) return 0;
            int dealt = Mathf.Min(currentStamina, rawAmount);
            currentStamina = Mathf.Max(0, currentStamina - rawAmount);
            OnStateChanged?.Invoke();
            return dealt;
        }

        public void RestoreStamina(int amount)
        {
            currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
            OnStateChanged?.Invoke();
        }

        public void AddLustShield(int amount)
        {
            if (amount <= 0) return;
            lustShield += amount;
            OnStateChanged?.Invoke();
        }

        public void ResetLustShield()
        {
            lustShield = 0;
            OnStateChanged?.Invoke();
        }

        public void AddSemen(int amountMl, PenetrationSlot slot = PenetrationSlot.None)
        {
            if (amountMl <= 0) return;
            semenVolumeMl += amountMl;
            switch (slot)
            {
                case PenetrationSlot.Mouth:
                    semenMouthMl = Mathf.Min(MAX_SEMEN_MOUTH_ML, semenMouthMl + amountMl);
                    break;
                case PenetrationSlot.Anal:
                    semenAnalMl = Mathf.Min(MAX_SEMEN_ANAL_ML, semenAnalMl + amountMl);
                    break;
                case PenetrationSlot.Vagina:
                default:
                    semenWombMl = Mathf.Min(MAX_SEMEN_WOMB_ML, semenWombMl + amountMl);
                    break;
            }
            OnStateChanged?.Invoke();
        }

        public void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Automatic fallback art database for combat skill cards.
    /// Provides art from Assets/sprites/Skill/ based on card ID or keyword
    /// if cardArt is not assigned directly in the CardSO Inspector.
    /// </summary>
    public static class SkillArtDatabase
    {
        private static readonly Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        public static Sprite GetSkillSprite(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;

            string id = cardId.ToLowerInvariant();
            if (cachedSprites.TryGetValue(id, out Sprite cached) && cached != null)
                return cached;

            string fileName = null;
            if (id.Contains("strike") || id.Contains("slash") || id.Contains("break")) fileName = "Skill_strike.jpg";
            else if (id.Contains("block") || id.Contains("defend") || id.Contains("fortress") || id.Contains("iron") || id.Contains("barrier") || id.Contains("leverage")) fileName = "Skill_block.jpg";
            else if (id.Contains("flirt") || id.Contains("allure") || id.Contains("tease") || id.Contains("seduce") || id.Contains("breath") || id.Contains("caress")) fileName = "Skill_flirt.jpg";
            else if (id.Contains("throat") || id.Contains("swallow") || id.Contains("clench")) fileName = "Skill_strip.jpg";
            else if (id.Contains("fire") || id.Contains("flame") || id.Contains("blast")) fileName = "Skill_fireball.jpg";
            else if (id.Contains("mend") || id.Contains("patch") || id.Contains("repair") || id.Contains("refresh")) fileName = "Skill_refresh.jpg";
            else if (id.Contains("strip") || id.Contains("undress")) fileName = "Skill_strip.jpg";

            if (fileName == null) return null;

            Sprite s = LoadSkillSprite(fileName);
            if (s != null)
            {
                cachedSprites[id] = s;
            }
            return s;
        }

        private static Sprite LoadSkillSprite(string fileName)
        {
#if UNITY_EDITOR
            Sprite editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/sprites/Skill/{fileName}");
            if (editorSprite != null) return editorSprite;
#endif
            try
            {
                string path = System.IO.Path.Combine(Application.dataPath, "sprites/Skill", fileName);
                if (System.IO.File.Exists(path))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Bilinear;
                        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SkillArtDatabase] Could not load skill image {fileName}: {ex.Message}");
            }
            return null;
        }
    }
}