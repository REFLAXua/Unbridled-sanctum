/// <summary>
/// Equipment slots manageable through inventory.
/// Hair, expression, eyebrows are handled by Character Editor (not inventory).
/// </summary>
public enum EquipSlot
{
    CombinedClothes = 0,
    UpperClothes = 1,
    LowerClothes = 2,
    UnderUpper = 3,   // Bra / Top Underwear
    UnderLower = 4,   // Panties / Bottom Underwear
    Accessory = 5,

    [System.Obsolete("Use UnderUpper or UnderLower")]
    Underwear = 4,
    [System.Obsolete("Use CombinedClothes, UpperClothes, or LowerClothes")]
    Clothes = 0
}