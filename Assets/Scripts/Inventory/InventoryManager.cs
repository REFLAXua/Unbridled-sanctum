using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages the player inventory — a flat list of items.
/// Items in startingItems are added automatically on Start.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("Starting Items (added on Start)")]
    public List<ItemData> startingItems = new List<ItemData>();

    private List<ItemData> items = new List<ItemData>();

    /// <summary>Read-only access to current items.</summary>
    public IReadOnlyList<ItemData> Items => items;

    /// <summary>Fired whenever inventory content changes.</summary>
    public event Action OnInventoryChanged;

    private void Start()
    {
        foreach (var item in startingItems)
        {
            if (item != null && !items.Contains(item))
                items.Add(item);
        }

        // Auto-seed starter weapons if not present
        EnsureDefaultStarterEquipment();

        OnInventoryChanged?.Invoke();
    }

    private void EnsureDefaultStarterEquipment()
    {
        bool hasWeapon = items.Exists(i => i != null && i.equipSlot == EquipSlot.Weapon);
        if (!hasWeapon)
        {
            var weaponAssets = Resources.LoadAll<ItemData>("Items/Weapons");
            if (weaponAssets != null && weaponAssets.Length > 0)
            {
                foreach (var w in weaponAssets)
                {
                    if (w != null && !items.Contains(w))
                    {
                        items.Add(w);
                    }
                }
            }
        }
    }

    public void AddItem(ItemData item)
    {
        if (item == null) return;
        items.Add(item);
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(ItemData item)
    {
        if (items.Remove(item))
        {
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }
}