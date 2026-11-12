using System;
using UnityEngine;

namespace UnbridledSanctum.Combat
{
    /// <summary>
    /// ScriptableObject defining a player combat card.
    /// Create via: Right Click > Create > Game > Combat > Card
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "Game/Combat/Card", order = 10)]
    public class CardSO : ScriptableObject
    {
        [Header("Card Identity")]
        public string id = "card_id";
        public string cardName = "New Card";
        public Sprite cardArt;

        [Header("Combat Resource Costs")]
        [Range(0, 5)]
        public int apCost = 1;

        [Range(1, 10)]
        public int cooldownTurns = 1;

        [Header("Offense & Defense Values")]
        public int hpDamage = 0;
        public int shieldGain = 0;
        [Range(0, 100)]
        public int seductionAmount = 0;
        public int clothesRepair = 0;

        [Header("Targeting & Description")]
        public CardTarget target = CardTarget.SingleEnemy;

        [Header("Struggle & Desire (GDD v1.5)")]
        public CardStanceCategory stanceCategory = CardStanceCategory.Neutral;
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

        [TextArea(2, 5)]
        public string description = "Card description text.";

        /// <summary>
        /// Converts this ScriptableObject definition into a runtime CardData object.
        /// </summary>
        public CardData ToCardData()
        {
            CardData card = new CardData(
                string.IsNullOrEmpty(id) ? name.ToLowerInvariant() : id,
                cardName,
                apCost,
                cooldownTurns,
                hpDamage,
                shieldGain,
                seductionAmount,
                target,
                description,
                category,
                requiredPath,
                stanceRequirement,
                staminaDamage,
                arousalSelfDelta,
                arousalTargetDelta,
                semenAbsorbedMl,
                lustShieldGain,
                isEphemeral,
                requiredTargetSlot
            );
            card.clothesRepair = clothesRepair;
            card.cardArt = cardArt;
            card.stanceCategory = stanceCategory;
            return card;
        }
    }
}
