/// <summary>
/// Equipment slots manageable through inventory.
/// Hair, expression, eyebrows are handled by Character Editor (not inventory).
/// </summary>
public enum EquipSlot
{
    CombinedClothes = 0,
    UpperClothes = 1,
    LowerClothes = 2,
    Underwear = 3,
    Accessory = 4,

    [System.Obsolete("Use CombinedClothes, UpperClothes, or LowerClothes")]
    Clothes = 0
}