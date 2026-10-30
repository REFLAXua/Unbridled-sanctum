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
            if (item != null)
                items.Add(item);
        }
        OnInventoryChanged?.Invoke();
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