using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnbridledSanctum.Combat
{
    public enum SkillCondition
    {
        Always,
        OnlyIfPlayerHasClothes,
        OnlyIfPlayerNaked,
        OnlyIfLowHp,
        OnlyIfHighSeduction,
        OnlyInNeutralStance,
        OnlyInGrappledStance,
        OnlyInPenetratedStance
    }

    [System.Serializable]
    public class WeightedEnemySkill
    {
        public EnemySkillSO skill;
        [Range(1, 100)]
        public int weight = 10;
        public SkillCondition condition = SkillCondition.Always;

        [Range(0f, 3f)]
        [Tooltip("Multiplier applied to weight when player is naked. 0 = never when naked, 1 = normal, >1 = more likely.")]
        public float likelihoodWhenNaked = 1.0f;
    }

    /// <summary>
    /// ScriptableObject defining an enemy type, stats, scaling, and customizable AI skill pool.
    /// Create via: Right Click > Create > Game > Combat > Enemy Definition
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Combat/Enemy Definition", order = 30)]
    public class EnemyDefinitionSO : ScriptableObject
    {
        [Header("Enemy Identity")]
        public EnemyType enemyType = EnemyType.SlimeGirl;
        public string enemyName = "Enemy";
        public EntityGender gender = EntityGender.Futa;
        public Sprite enemyIcon;

        [Header("Specimen Mutation")]
        [Tooltip("If true, semen from this specific enemy specimen triggers heroine breast growth.")]
        public bool canTriggerBreastGrowth = false;

        [Header("Health & Floor Scaling")]
        public int baseHp = 25;
        public int hpPerFloor = 3;

        [Header("Mana")]
        public int maxMana = 20;
        public int startingMana = 10;

        [Header("Seduction & Defense")]
        public int maxSeductionStacks = 3;
        public int baseShield = 0;

        [Header("Skill Set & AI Behavior")]
        public List<WeightedEnemySkill> skillPool = new List<WeightedEnemySkill>();

        /// <summary>
        /// Selects an action based on available mana, conditions, stance, and relative weights.
        /// </summary>
        public EnemySkill ChooseAction(Combatant enemy, Combatant player, CombatStance stance = CombatStance.Neutral)
        {
            if (skillPool == null || skillPool.Count == 0) return null;

            List<KeyValuePair<EnemySkillSO, int>> validSkills = new List<KeyValuePair<EnemySkillSO, int>>();

            for (int i = 0; i < skillPool.Count; i++)
            {
                var entry = skillPool[i];
                if (entry.skill == null) continue;

                // Check stance compatibility
                if (entry.skill.stanceRequirement != CombatStance.Neutral && entry.skill.stanceRequirement != stance)
                    continue;

                // Check condition
                bool pass = true;
                switch (entry.condition)
                {
                    case SkillCondition.OnlyIfPlayerHasClothes:
                        pass = player.clothesHp > 0;
                        break;
                    case SkillCondition.OnlyIfPlayerNaked:
                        pass = player.clothesHp <= 0;
                        break;
                    case SkillCondition.OnlyIfLowHp:
                        pass = ((float)enemy.currentHp / enemy.maxHp) <= 0.4f;
                        break;
                    case SkillCondition.OnlyIfHighSeduction:
                        pass = enemy.seductionStacks >= 2;
                        break;
                    case SkillCondition.OnlyInNeutralStance:
                        pass = stance == CombatStance.Neutral;
                        break;
                    case SkillCondition.OnlyInGrappledStance:
                        pass = stance == CombatStance.Grappled;
                        break;
                    case SkillCondition.OnlyInPenetratedStance:
                        pass = stance == CombatStance.Penetrated || stance == CombatStance.ThroatLock;
                        break;
                }

                if (pass)
                {
                    bool isNaked = player != null && player.clothesHp <= 0;
                    if (isNaked && entry.likelihoodWhenNaked <= 0.001f)
                    {
                        continue;
                    }

                    float mult = isNaked ? entry.likelihoodWhenNaked : 1f;
                    int finalWeight = Mathf.Max(1, Mathf.RoundToInt(entry.weight * mult));
                    validSkills.Add(new KeyValuePair<EnemySkillSO, int>(entry.skill, finalWeight));
                }
            }

            if (validSkills.Count == 0)
            {
                // Fallback: pick any available skill regardless of conditions
                for (int i = 0; i < skillPool.Count; i++)
                {
                    if (skillPool[i].skill != null)
                    {
                        return skillPool[i].skill.ToEnemySkill();
                    }
                }
                return null;
            }

            // Weighted random selection
            int totalWeight = 0;
            for (int i = 0; i < validSkills.Count; i++) totalWeight += validSkills[i].Value;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int running = 0;
            for (int i = 0; i < validSkills.Count; i++)
            {
                running += validSkills[i].Value;
                if (roll < running)
                {
                    return validSkills[i].Key.ToEnemySkill();
                }
            }

            return validSkills[0].Key.ToEnemySkill();
        }
    }
}
