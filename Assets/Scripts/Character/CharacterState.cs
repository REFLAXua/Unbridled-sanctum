using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Stores character state: body size, equipped items, and appearance customization.
/// Fires events when state changes so UI and PaperDoll can react.
/// </summary>
public class CharacterState : MonoBehaviour
{
    [Header("Appearance Database")]
    public CharacterAppearanceDatabase appearanceDatabase;

    public const string PREF_KEY_BODY_SIZE = "Sanctum_CharacterBodySize";

    [Header("Body")]
    [SerializeField]
    private BodySizeType _currentBodySize = BodySizeType.Small;

    public BodySizeType CurrentBodySize
    {
        get => _currentBodySize;
        set
        {
            if (_currentBodySize != value)
            {
                _currentBodySize = value;
                PlayerPrefs.SetInt(PREF_KEY_BODY_SIZE, (int)_currentBodySize);
                PlayerPrefs.Save();
                OnBodySizeChanged?.Invoke(_currentBodySize);
            }
        }
    }

    [Header("Appearance Customization")]
    public Sprite selectedHairBack;
    public Sprite selectedHairFront;
    public Sprite selectedExpression;
    public Sprite selectedBlush;
    [Header("Groin Deformation State")]
    public Sprite selectedGroin;
    public Color hairColor = Color.white;
    public Color eyeColor = new Color(0.95f, 0.40f, 0.80f, 1f); // default pink/magenta

    /// <summary>Fired when body size changes.</summary>
    public event Action<BodySizeType> OnBodySizeChanged;

    /// <summary>Fired when any equipment slot changes.</summary>
    public event Action OnEquipmentChanged;

    /// <summary>Fired when hair, face, blush, or colors change.</summary>
    public event Action OnAppearanceChanged;

    private Dictionary<EquipSlot, ItemData> equippedItems = new Dictionary<EquipSlot, ItemData>();

    private void Awake()
    {
        if (PlayerPrefs.HasKey(PREF_KEY_BODY_SIZE))
        {
            _currentBodySize = (BodySizeType)Mathf.Clamp(PlayerPrefs.GetInt(PREF_KEY_BODY_SIZE, (int)BodySizeType.Small), 0, (int)BodySizeType.XLarge);
        }
        else
        {
            _currentBodySize = BodySizeType.Small;
        }
        EnsureDefaultAppearance();
    }

    /// <summary>Initializes character with default non-empty appearance.</summary>
    public void EnsureDefaultAppearance()
    {
        if (appearanceDatabase == null) return;

        bool changed = false;

        if (appearanceDatabase.hairFrontStyles.Count > 0)
        {
            if (selectedHairFront == null || !appearanceDatabase.hairFrontStyles.Contains(selectedHairFront))
            {
                selectedHairFront = appearanceDatabase.hairFrontStyles[0];
                changed = true;
            }
        }
        else if (selectedHairFront != null)
        {
            selectedHairFront = null;
            changed = true;
        }

        if (appearanceDatabase.hairBackStyles.Count > 0)
        {
            if (selectedHairBack == null || !appearanceDatabase.hairBackStyles.Contains(selectedHairBack))
            {
                selectedHairBack = appearanceDatabase.hairBackStyles[0];
                changed = true;
            }
        }
        else if (selectedHairBack != null)
        {
            selectedHairBack = null;
            changed = true;
        }

        if ((selectedExpression == null || !appearanceDatabase.emotionStyles.Contains(selectedExpression) || selectedExpression.name.Equals("Normal", StringComparison.OrdinalIgnoreCase)) && appearanceDatabase.emotionStyles.Count > 0)
        {
            Sprite normal = null;
#if UNITY_EDITOR
            normal = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Expression/Emotions/Normal.png");
#endif
            if (normal == null)
            {
                normal = appearanceDatabase.emotionStyles.Find(s => s != null && s.name.Equals("Normal", StringComparison.OrdinalIgnoreCase));
            }
            if (normal == null)
            {
                normal = appearanceDatabase.emotionStyles[0];
            }
            selectedExpression = normal;
            changed = true;
        }

        if (hairColor == Color.clear || hairColor.a < 0.1f)
        {
            hairColor = Color.white;
            changed = true;
        }

        if (changed)
        {
            OnAppearanceChanged?.Invoke();
        }
    }

    public void SetAppearance(Sprite hairBack, Sprite hairFront, Sprite expression, Sprite blush, Color newHairColor, Color newEyeColor)
    {
        selectedHairBack = hairBack;
        selectedHairFront = hairFront;
        selectedExpression = expression;
        selectedBlush = blush;
        hairColor = newHairColor;
        eyeColor = newEyeColor;

        OnAppearanceChanged?.Invoke();
    }

    public void SetEyeColor(Color newEyeColor)
    {
        eyeColor = newEyeColor;
        OnAppearanceChanged?.Invoke();
    }

    public void SetHairColor(Color newHairColor)
    {
        hairColor = newHairColor;
        OnAppearanceChanged?.Invoke();
    }

    public void SetExpression(Sprite newExpression)
    {
        selectedExpression = newExpression;
        OnAppearanceChanged?.Invoke();
    }

    public void SetBlush(Sprite newBlush)
    {
        selectedBlush = newBlush;
        OnAppearanceChanged?.Invoke();
    }

    public void SetGroin(Sprite newGroin)
    {
        selectedGroin = newGroin;
        OnAppearanceChanged?.Invoke();
    }

    public void SetHair(Sprite hairFront, Sprite hairBack)
    {
        selectedHairFront = hairFront;
        selectedHairBack = hairBack;
        OnAppearanceChanged?.Invoke();
    }

    /// <summary>Randomizes all appearance parameters from database.</summary>
    public void RandomizeAppearance()
    {
        if (appearanceDatabase == null) return;

        // Random body size
        CurrentBodySize = (BodySizeType)UnityEngine.Random.Range(0, 4);

        // Random hair styles
        if (appearanceDatabase.hairFrontStyles.Count > 0)
            selectedHairFront = appearanceDatabase.hairFrontStyles[UnityEngine.Random.Range(0, appearanceDatabase.hairFrontStyles.Count)];

        if (appearanceDatabase.hairBackStyles.Count > 0)
            selectedHairBack = appearanceDatabase.hairBackStyles[UnityEngine.Random.Range(0, appearanceDatabase.hairBackStyles.Count)];

        // Random emotion
        if (appearanceDatabase.emotionStyles.Count > 0)
            selectedExpression = appearanceDatabase.emotionStyles[UnityEngine.Random.Range(0, appearanceDatabase.emotionStyles.Count)];

        // Blush is earned dynamically during combat seduction phases, start as null
        selectedBlush = null;

        // Random hair color preset
        if (appearanceDatabase.hairColorPresets.Length > 0)
            hairColor = appearanceDatabase.hairColorPresets[UnityEngine.Random.Range(0, appearanceDatabase.hairColorPresets.Length)];

        // Random eye color preset
        if (appearanceDatabase.eyeColorPresets.Length > 0)
            eyeColor = appearanceDatabase.eyeColorPresets[UnityEngine.Random.Range(0, appearanceDatabase.eyeColorPresets.Length)];

        OnAppearanceChanged?.Invoke();
    }

    // --- Equipment methods ---

    public ItemData GetEquippedItem(EquipSlot slot)
    {
        if (slot == EquipSlot.UpperClothes)
        {
            if (equippedItems.TryGetValue(EquipSlot.UpperClothes, out ItemData upper) && upper != null) return upper;
            if (equippedItems.TryGetValue(EquipSlot.CombinedClothes, out ItemData comb) && comb != null) return comb;
            return null;
        }

        if (slot == EquipSlot.LowerClothes)
        {
            if (equippedItems.TryGetValue(EquipSlot.LowerClothes, out ItemData lower) && lower != null) return lower;
            if (equippedItems.TryGetValue(EquipSlot.CombinedClothes, out ItemData comb) && comb != null) return comb;
            return null;
        }

        if (slot == EquipSlot.Underwear)
        {
            if (equippedItems.TryGetValue(EquipSlot.UnderLower, out ItemData uLower) && uLower != null) return uLower;
            if (equippedItems.TryGetValue(EquipSlot.UnderUpper, out ItemData uUpper) && uUpper != null) return uUpper;
        }

        equippedItems.TryGetValue(slot, out ItemData item);
        return item;
    }

    public void EquipItem(ItemData item)
    {
        if (item == null || !item.isEquippable) return;

        if (item.equipSlot == EquipSlot.CombinedClothes)
        {
            // Combined clothes replaces both upper and lower
            equippedItems.Remove(EquipSlot.UpperClothes);
            equippedItems.Remove(EquipSlot.LowerClothes);
            equippedItems[EquipSlot.CombinedClothes] = item;
        }
        else if (item.equipSlot == EquipSlot.UpperClothes)
        {
            // Equipping upper clothes replaces any combined clothes
            equippedItems.Remove(EquipSlot.CombinedClothes);
            equippedItems[EquipSlot.UpperClothes] = item;
        }
        else if (item.equipSlot == EquipSlot.LowerClothes)
        {
            // Equipping lower clothes replaces any combined clothes
            equippedItems.Remove(EquipSlot.CombinedClothes);
            equippedItems[EquipSlot.LowerClothes] = item;
        }
        else
        {
            equippedItems[item.equipSlot] = item;
        }

        OnEquipmentChanged?.Invoke();
    }

    public ItemData UnequipItem(EquipSlot slot)
    {
        if (slot == EquipSlot.UpperClothes || slot == EquipSlot.LowerClothes)
        {
            if (equippedItems.TryGetValue(EquipSlot.CombinedClothes, out ItemData comb))
            {
                equippedItems.Remove(EquipSlot.CombinedClothes);
                OnEquipmentChanged?.Invoke();
                return comb;
            }
        }

        if (equippedItems.TryGetValue(slot, out ItemData item))
        {
            equippedItems.Remove(slot);
            OnEquipmentChanged?.Invoke();
            return item;
        }
        return null;
    }

    public bool IsEquipped(ItemData item)
    {
        if (item == null) return false;
        if (item.equipSlot == EquipSlot.CombinedClothes)
        {
            return equippedItems.TryGetValue(EquipSlot.CombinedClothes, out ItemData c) && c == item;
        }
        return equippedItems.TryGetValue(item.equipSlot, out ItemData equipped) && equipped == item;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            OnBodySizeChanged?.Invoke(_currentBodySize);
            OnAppearanceChanged?.Invoke();
        }
    }
#endif
}