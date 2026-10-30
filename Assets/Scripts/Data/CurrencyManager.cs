using System;
using UnityEngine;

namespace UnbridledSanctum.Data
{
    /// <summary>
    /// Dual-currency economy manager for Unbridled Sanctum:
    ///  1. Lumieres (Run Currency): Soft currency found during the expedition in combat, chests, and events.
    ///     Resets upon starting a new run.
    ///  2. Lustid (Meta Currency): Permanent erotic essence absorbed by the ancient sanctuary altar.
    ///     Persists across runs in PlayerPrefs and is used for meta-progression in the Sanctuary Cave.
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        private const string PREF_KEY_LUSTID = "Sanctum_Meta_Lustid";

        public static CurrencyManager Instance { get; private set; }

        [Header("Run Currency (Temporary)")]
        [SerializeField]
        private int _currentLumieres = 0;

        [Header("Meta Currency (Persistent)")]
        [SerializeField]
        private int _totalLustid = 0;

        public int CurrentLumieres => _currentLumieres;
        public int TotalLustid => _totalLustid;

        /// <summary>Fired when current run's Lumieres change (new balance).</summary>
        public event Action<int> OnLumieresChanged;

        /// <summary>Fired when persistent Lustid changes (new balance).</summary>
        public event Action<int> OnLustidChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            LoadPersistentCurrencies();
        }

        public static CurrencyManager EnsureExists()
        {
            if (Instance != null) return Instance;

            var existing = FindFirstObjectByType<CurrencyManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            GameObject go = new GameObject("CurrencyManager");
            Instance = go.AddComponent<CurrencyManager>();
            return Instance;
        }

        private void LoadPersistentCurrencies()
        {
            _totalLustid = PlayerPrefs.GetInt(PREF_KEY_LUSTID, 0);
        }

        private void SavePersistentCurrencies()
        {
            PlayerPrefs.SetInt(PREF_KEY_LUSTID, _totalLustid);
            PlayerPrefs.Save();
        }

        // =====================================================================
        // LUMIERES (RUN CURRENCY)
        // =====================================================================

        public void AddLumieres(int amount)
        {
            if (amount <= 0) return;
            _currentLumieres += amount;
            Debug.Log($"[CurrencyManager] +{amount} Lumieres. New balance: {_currentLumieres}");
            OnLumieresChanged?.Invoke(_currentLumieres);
        }

        public bool TrySpendLumieres(int amount)
        {
            if (amount <= 0) return true;
            if (_currentLumieres < amount) return false;

            _currentLumieres -= amount;
            Debug.Log($"[CurrencyManager] -{amount} Lumieres. Remaining: {_currentLumieres}");
            OnLumieresChanged?.Invoke(_currentLumieres);
            return true;
        }

        /// <summary>
        /// Resets temporary run currency to 0 at the start of a descent.
        /// </summary>
        public void ResetRunCurrency()
        {
            _currentLumieres = 0;
            OnLumieresChanged?.Invoke(_currentLumieres);
        }

        // =====================================================================
        // LUSTID (META CURRENCY)
        // =====================================================================

        public void AddLustid(int amount)
        {
            if (amount <= 0) return;
            _totalLustid += amount;
            SavePersistentCurrencies();
            Debug.Log($"[CurrencyManager] +{amount} Lustid! Total meta-balance: {_totalLustid}");
            OnLustidChanged?.Invoke(_totalLustid);
        }

        public bool TrySpendLustid(int amount)
        {
            if (amount <= 0) return true;
            if (_totalLustid < amount) return false;

            _totalLustid -= amount;
            SavePersistentCurrencies();
            Debug.Log($"[CurrencyManager] -{amount} Lustid spent. Remaining: {_totalLustid}");
            OnLustidChanged?.Invoke(_totalLustid);
            return true;
        }

        // =====================================================================
        // RUN CONVERSION & REWARD
        // =====================================================================

        /// <summary>
        /// Converts leftover Lumieres into persistent Lustid when returning to Sanctuary Cave.
        /// Rate: 10 Lumieres -> 1 Lustid (min 1 Lustid if Lumieres > 0).
        /// </summary>
        public int ConvertRunCurrencyToEndExpedition()
        {
            int converted = 0;
            if (_currentLumieres > 0)
            {
                converted = Mathf.Max(1, _currentLumieres / 10);
                AddLustid(converted);
            }
            _currentLumieres = 0;
            OnLumieresChanged?.Invoke(_currentLumieres);
            return converted;
        }
    }
}
