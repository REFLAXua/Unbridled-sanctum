using UnityEngine;

/// <summary>
/// ScriptableObject defining an item in the game.
/// Create via: Assets > Create > Game > Item Data
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName = "New Item";

    [TextArea(3, 5)]
    public string description = "";

    public Sprite icon;

    public ItemType itemType = ItemType.Equipment;
    public bool isEquippable = false;
    public EquipSlot equipSlot;

    [Header("Armor & Durability")]
    public int durability = 65;

    [Header("Stat Modifiers (Buffs / Debuffs)")]
    [Tooltip("Bonus to Max HP. Can be negative. Minimum total player HP is clamped to 1.")]
    public int bonusMaxHp = 0;

    [Tooltip("Bonus to Max Mana. Can be negative. Reserved for upcoming story event.")]
    public int bonusMaxMana = 0;

    [Tooltip("Armor damage reduction. Reduces incoming physical/HP attack damage.")]
    public int damageReduction = 1;

    [Tooltip("Increases seductive potency (%) dealt by your skills.")]
    public int bonusSeductionPower = 0;

    [Tooltip("Reduces incoming lust/seduction (%) taken from enemies.")]
    public int seductionResistance = 0;

    [Header("Passive & Status Effects")]
    [Tooltip("Unique keys or IDs for future passive effects or statuses granted while intact.")]
    public System.Collections.Generic.List<string> passiveEffectIds = new System.Collections.Generic.List<string>();

    public bool IsCombinedArmor => equipSlot == EquipSlot.CombinedClothes;
    public bool IsUpperArmor => equipSlot == EquipSlot.UpperClothes || equipSlot == EquipSlot.CombinedClothes;
    public bool IsLowerArmor => equipSlot == EquipSlot.LowerClothes || equipSlot == EquipSlot.CombinedClothes;
    public bool IsUnderwear => equipSlot == EquipSlot.UnderUpper || equipSlot == EquipSlot.UnderLower;
    public bool IsArmor => equipSlot == EquipSlot.UpperClothes || equipSlot == EquipSlot.LowerClothes || equipSlot == EquipSlot.CombinedClothes || equipSlot == EquipSlot.UnderUpper || equipSlot == EquipSlot.UnderLower;

    [Header("Paper Doll Layer Controls")]
    [Tooltip("If true, hides the modular breast layer when this upper or combined clothing is equipped.")]
    public bool hidesBreastLayer = true;

    [Header("Body-Size Dependent Sprites (for equippable clothes)")]
    public BodySizeSprite[] spritesByBodySize;

    /// <summary>
    /// Returns the sprite matching the given body size, or null.
    /// </summary>
    public Sprite GetSpriteForSize(BodySizeType size)
    {
        if (spritesByBodySize == null) return null;
        foreach (var entry in spritesByBodySize)
        {
            if (entry.bodySize == size) return entry.sprite;
        }
        return null;
    }
}

[System.Serializable]
public class BodySizeSprite
{
    public BodySizeType bodySize;
    public Sprite sprite;
}