using System;
using UnityEngine;

namespace UnbridledSanctum.Combat
{
    /// <summary>
    /// ScriptableObject defining an enemy skill/action.
    /// Create via: Right Click > Create > Game > Combat > Enemy Skill
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemySkill", menuName = "Game/Combat/Enemy Skill", order = 20)]
    public class EnemySkillSO : ScriptableObject
    {
        [Header("Skill Identity")]
        public string skillName = "New Skill";
        public Sprite intentIcon;

        [Header("Resource Cost & Gain")]
        public int manaCost = 0;
        public int manaGain = 0;

        [Header("Offense Values")]
        public int hpDamage = 0;
        public int clothesDamage = 0;
        public ArmorTargetSlot armorTarget = ArmorTargetSlot.All;
        public int seductionDamage = 0;

        [Header("Defense Values")]
        public int shieldGain = 0;

        [Header("Struggle & Stance Mechanics")]
        public int staminaDamage = 0;
        public int pranaGain = 0;
        public int selfArousalGain = 0;
        public CombatStance stanceRequirement = CombatStance.Neutral;
        public bool causesStanceTransition = false;
        public CombatStance targetTransitionStance = CombatStance.Grappled;
        public PenetrationSlot targetPenetrationSlot = PenetrationSlot.None;

        [Header("Description")]
        [TextArea(2, 4)]
        public string description = "Skill intent description shown to the player.";

        /// <summary>
        /// Converts this ScriptableObject definition into a runtime EnemySkill object.
        /// </summary>
        public EnemySkill ToEnemySkill()
        {
            return new EnemySkill(
                skillName,
                manaCost,
                hpDamage,
                clothesDamage,
                seductionDamage,
                shieldGain,
                manaGain,
                description,
                armorTarget,
                staminaDamage,
                pranaGain,
                stanceRequirement,
                causesStanceTransition ? (CombatStance?)targetTransitionStance : null,
                selfArousalGain,
                targetPenetrationSlot
            );
        }
    }
}
