using UnityEngine;
using System;

/// <summary>
/// Manages post-combat body transformation (Small -> Medium -> Large -> XLarge)
/// driven by semen/prana accumulation, Flesh Stabilization in the Hub,
/// and the progressive Flesh Archive discovery system.
/// </summary>
public class FleshTransformationManager : MonoBehaviour
{
    public static FleshTransformationManager Instance { get; private set; }

    public const string PREF_KEY_PRANA_GAUGE = "Sanctum_Flesh_PranaMl";
    public const string PREF_KEY_MAX_UNLOCKED_SIZE = "Sanctum_Flesh_MaxUnlockedSize";
    public const string PREF_KEY_ARCHIVE_PREFIX = "Sanctum_Flesh_Archive_";

    [Header("Configuration")]
    public int thresholdMl = 200;

    [Header("Dev / Testing Mode")]
    [Tooltip("If true, all body sizes are unlocked in wardrobe for testing.")]
    public bool unlockAllSizesInWardrobe = true;

    [Header("State")]
    [SerializeField] private int _currentPranaMl = 0;
    public int currentPranaMl => _currentPranaMl;

    public BodySizeType maxUnlockedBodySize { get; private set; } = BodySizeType.Small;

    public CharacterState characterState;

    public event Action<int, int> OnPranaGaugeChanged;
    public event Action<BodySizeType, BodySizeType> OnBodySizeTransformed;
    public event Action<string, int, string> OnFleshArchiveUpdated; // species, level, message

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadState();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(CharacterState state)
    {
        characterState = state;
        if (characterState != null)
        {
            // Ensure max unlocked size is at least the initial character's size
            if ((int)characterState.CurrentBodySize > (int)maxUnlockedBodySize)
            {
                maxUnlockedBodySize = characterState.CurrentBodySize;
                SaveState();
            }
        }
    }

    private void LoadState()
    {
        _currentPranaMl = PlayerPrefs.GetInt(PREF_KEY_PRANA_GAUGE, 0);
        int savedMax = PlayerPrefs.GetInt(PREF_KEY_MAX_UNLOCKED_SIZE, (int)BodySizeType.Small);
        maxUnlockedBodySize = (BodySizeType)Mathf.Clamp(savedMax, 0, (int)BodySizeType.XLarge);
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(PREF_KEY_PRANA_GAUGE, _currentPranaMl);
        PlayerPrefs.SetInt(PREF_KEY_MAX_UNLOCKED_SIZE, (int)maxUnlockedBodySize);
        PlayerPrefs.Save();
    }

    public bool CanGrowFurther()
    {
        if (characterState == null) return false;
        return characterState.CurrentBodySize < BodySizeType.XLarge;
    }

    public bool IsTransformationPending => _currentPranaMl >= thresholdMl && CanGrowFurther();

    public const string PREF_KEY_INTERNAL_CUM_PREFIX = "Sanctum_Flesh_InternalCum_";

    /// <summary>
    /// Adds absorbed semen/cum to the transformation gauge and logs enemy internal intake for Flesh Archive.
    /// </summary>
    public void AddPrana(int amount, string enemySpecies = null, bool isInternal = false)
    {
        if (amount <= 0) return;

        _currentPranaMl = Mathf.Clamp(_currentPranaMl + amount, 0, thresholdMl * 2);
        SaveState();
        OnPranaGaugeChanged?.Invoke(_currentPranaMl, thresholdMl);

        if (isInternal && !string.IsNullOrEmpty(enemySpecies))
        {
            RegisterInternalMonsterContact(enemySpecies, amount);
        }
    }

    /// <summary>
    /// Debug function: fills the gauge to 100% capacity WITHOUT triggering the transformation.
    /// </summary>
    public void DebugFillGauge()
    {
        _currentPranaMl = thresholdMl;
        SaveState();
        OnPranaGaugeChanged?.Invoke(_currentPranaMl, thresholdMl);
        Debug.Log($"[FleshTransformationManager] DEBUG: Gauge filled to {_currentPranaMl}/{thresholdMl} ml. Ready for post-combat transformation.");
    }

    /// <summary>
    /// Debug function: empties the transformation gauge.
    /// </summary>
    public void DebugResetGauge()
    {
        _currentPranaMl = 0;
        SaveState();
        OnPranaGaugeChanged?.Invoke(_currentPranaMl, thresholdMl);
        Debug.Log("[FleshTransformationManager] DEBUG: Gauge reset to 0 ml.");
    }

    public BodySizeType GetNextBodySize(BodySizeType current)
    {
        switch (current)
        {
            case BodySizeType.Small: return BodySizeType.Medium;
            case BodySizeType.Medium: return BodySizeType.Large;
            case BodySizeType.Large: return BodySizeType.XLarge;
            default: return BodySizeType.XLarge;
        }
    }

    /// <summary>
    /// Safely executes the body transformation during post-combat transition.
    /// </summary>
    public bool ExecuteTransformation(out BodySizeType oldSize, out BodySizeType newSize)
    {
        oldSize = characterState != null ? characterState.CurrentBodySize : BodySizeType.Small;
        newSize = oldSize;

        if (!IsTransformationPending)
        {
            Debug.LogWarning($"[FleshTransformationManager] ExecuteTransformation rejected: Essence threshold not reached ({_currentPranaMl}/{thresholdMl} ml) or cannot grow further.");
            return false;
        }

        if (!CanGrowFurther()) return false;

        newSize = GetNextBodySize(oldSize);
        if (characterState != null)
        {
            characterState.CurrentBodySize = newSize;
        }

        if ((int)newSize > (int)maxUnlockedBodySize)
        {
            maxUnlockedBodySize = newSize;
        }

        // Consume threshold
        _currentPranaMl = Mathf.Max(0, _currentPranaMl - thresholdMl);
        SaveState();

        OnPranaGaugeChanged?.Invoke(_currentPranaMl, thresholdMl);
        OnBodySizeTransformed?.Invoke(oldSize, newSize);
        return true;
    }

    /// <summary>
    /// Flesh Stabilization: allows player in Sanctuary Hub to switch between any unlocked size without penalty.
    /// </summary>
    public bool StabilizeBodySize(BodySizeType targetSize)
    {
        if (!unlockAllSizesInWardrobe && (int)targetSize > (int)maxUnlockedBodySize) return false;
        if (characterState == null) return false;

        characterState.CurrentBodySize = targetSize;
        return true;
    }

    public bool IsSizeUnlocked(BodySizeType size)
    {
        if (unlockAllSizesInWardrobe) return true;
        return (int)size <= (int)maxUnlockedBodySize;
    }

    // =========================================================================
    // FLESH ARCHIVE (PROGRESSIVE DISCOVERY DRIVEN BY INTERNAL INTAKE)
    // =========================================================================

    public int GetSpeciesContactLevel(string species)
    {
        if (string.IsNullOrEmpty(species)) return 0;
        return PlayerPrefs.GetInt(PREF_KEY_ARCHIVE_PREFIX + species.ToLower().Trim(), 0);
    }

    public int GetSpeciesInternalCumMl(string species)
    {
        if (string.IsNullOrEmpty(species)) return 0;
        return PlayerPrefs.GetInt(PREF_KEY_INTERNAL_CUM_PREFIX + species.ToLower().Trim(), 0);
    }

    public void RegisterInternalMonsterContact(string species, int intakeMl)
    {
        if (string.IsNullOrEmpty(species)) return;
        string key = species.ToLower().Trim();
        int curCum = GetSpeciesInternalCumMl(key) + intakeMl;
        PlayerPrefs.SetInt(PREF_KEY_INTERNAL_CUM_PREFIX + key, curCum);

        int currentLevel = GetSpeciesContactLevel(key);
        int targetLevel = currentLevel;

        if (curCum >= 500 && currentLevel < 3) targetLevel = 3;
        else if (curCum >= 200 && currentLevel < 2) targetLevel = 2;
        else if (curCum >= 75 && currentLevel < 1) targetLevel = 1;

        if (targetLevel > currentLevel)
        {
            PlayerPrefs.SetInt(PREF_KEY_ARCHIVE_PREFIX + key, targetLevel);
            PlayerPrefs.Save();

            string hint = GetSpeciesDiscoveryText(key, targetLevel);
            OnFleshArchiveUpdated?.Invoke(species, targetLevel, hint);
        }
        else
        {
            PlayerPrefs.Save();
        }
    }

    public string GetSpeciesDiscoveryText(string species, int level)
    {
        string norm = species.ToLower().Trim();
        if (norm.Contains("slime"))
        {
            switch (level)
            {
                case 1: return "[ ??? ] - A strange, viscous warmth spreads through your lower abdomen...";
                case 2: return "[ Tissue Mutation ? ] - Muscles grow more supple, chest feels noticeably heavier...";
                case 3: return "[ Slime Essence Analyzed ] - Fluid matrix permanently increases tissue elasticity and semen absorption.";
                default: return "[ Slime Essence Analyzed ] - Complete biological profile unlocked.";
            }
        }
        else if (norm.Contains("succub"))
        {
            switch (level)
            {
                case 1: return "[ ??? ] - An intoxicating heat flushes across your collarbones and chest...";
                case 2: return "[ Libido Catalyst ? ] - Breasts feel sensitive and swollen, reacting eagerly to concentrated cum...";
                case 3: return "[ Succubus Essence Analyzed ] - Pure aphrodisiac semen essence stimulating mammary growth and seduction potency.";
                default: return "[ Succubus Essence Analyzed ] - Complete biological profile unlocked.";
            }
        }
        else
        {
            switch (level)
            {
                case 1: return "[ ??? ] - Supernatural warmth courses through your veins...";
                case 2: return "[ Tissue Mutation ? ] - Your core expands with newfound demonic vigor...";
                case 3: return $"[ {species} Essence Analyzed ] - Complete essence profile recorded.";
                default: return $"[ {species} Essence Analyzed ] - Complete biological profile recorded.";
            }
        }
    }
}
