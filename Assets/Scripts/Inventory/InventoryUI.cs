using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Manages the inventory UI panel. Toggle with Escape key.
/// Slots are created programmatically - no prefab needed.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject inventoryPanel;
    public Transform itemGridContainer;

    [Header("Detail Panel")]
    public GameObject detailPanel;
    public TextMeshProUGUI detailItemName;
    public TextMeshProUGUI detailDescription;
    public Button useButton;
    public TextMeshProUGUI useButtonText;

    [Header("Slot Appearance")]
    public Color slotNormalColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
    public Color slotEquippedColor = new Color(0.15f, 0.4f, 0.15f, 0.9f);
    public Color slotHoverColor = new Color(0.3f, 0.3f, 0.4f, 0.9f);
    public Color slotSelectedColor = new Color(0.35f, 0.35f, 0.5f, 0.95f);
    public int slotSize = 160;
    public int slotSpacing = 16;
    public int gridColumns = 4;

    [Header("Dependencies")]
    public InventoryManager inventoryManager;
    public CharacterState characterState;
    public PaperDollController paperDoll;

    private bool isOpen = false;
    private ItemData selectedItem = null;

    private void Start()
    {
        // Ensure grid layout exists
        GridLayoutGroup grid = itemGridContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = itemGridContainer.gameObject.AddComponent<GridLayoutGroup>();

        grid.cellSize = new Vector2(slotSize, slotSize);
        grid.spacing = new Vector2(slotSpacing, slotSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = gridColumns;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.padding = new RectOffset(8, 8, 8, 8);

        inventoryPanel.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);

        inventoryManager.OnInventoryChanged += RefreshGrid;
    }

    private void OnDestroy()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= RefreshGrid;
    }

    // ESC key handling is managed by GameUIManager so that input works even when InventoryPanel is inactive.

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);

        if (!isOpen)
        {
            selectedItem = null;
            if (detailPanel != null) detailPanel.SetActive(false);
            if (paperDoll != null) paperDoll.SetZoomControlsVisible(false);
        }
        else
        {
            RefreshGrid();
            if (paperDoll != null) paperDoll.SetZoomControlsVisible(true);
        }
    }

    public void RefreshGrid()
    {
        // Clear existing slots
        foreach (Transform child in itemGridContainer)
        {
            Destroy(child.gameObject);
        }

        // Create a slot for each inventory item
        foreach (var item in inventoryManager.Items)
        {
            CreateSlot(item);
        }
    }

    private void CreateSlot(ItemData item)
    {
        bool isEquipped = characterState.IsEquipped(item);

        // --- Root slot object ---
        GameObject slotObj = new GameObject(item.itemName, typeof(RectTransform));
        slotObj.transform.SetParent(itemGridContainer, false);

        // Background image
        Image bg = slotObj.AddComponent<Image>();
        bg.color = isEquipped ? slotEquippedColor : slotNormalColor;

        // Button component
        Button btn = slotObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        ColorBlock colors = btn.colors;
        colors.normalColor = isEquipped ? slotEquippedColor : slotNormalColor;
        colors.highlightedColor = slotHoverColor;
        colors.pressedColor = new Color(slotHoverColor.r * 0.85f, slotHoverColor.g * 0.85f, slotHoverColor.b * 0.85f, 0.95f);
        colors.selectedColor = slotSelectedColor;
        btn.colors = colors;

        // --- Icon or text label ---
        if (item.icon != null)
        {
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(slotObj.transform, false);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = item.icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.sizeDelta = Vector2.zero;
            iconRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            // No icon - display item name as text
            GameObject textObj = new GameObject("Label", typeof(RectTransform));
            textObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = item.itemName;
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.05f);
            textRect.anchorMax = new Vector2(0.95f, 0.95f);
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
        }

        // --- Equipped indicator ---
        if (isEquipped)
        {
            GameObject eqObj = new GameObject("EquippedMark", typeof(RectTransform));
            eqObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI eqText = eqObj.AddComponent<TextMeshProUGUI>();
            eqText.text = "E";
            eqText.fontSize = 22;
            eqText.alignment = TextAlignmentOptions.TopRight;
            eqText.color = new Color(0.3f, 1f, 0.3f, 1f);
            eqText.fontStyle = FontStyles.Bold;
            eqText.raycastTarget = false;

            RectTransform eqRect = eqObj.GetComponent<RectTransform>();
            eqRect.anchorMin = Vector2.zero;
            eqRect.anchorMax = Vector2.one;
            eqRect.sizeDelta = new Vector2(-8f, -8f);
            eqRect.anchoredPosition = Vector2.zero;
        }

        // --- Click handler ---
        ItemData captured = item;
        btn.onClick.AddListener(() => SelectItem(captured));
    }

    private void SelectItem(ItemData item)
    {
        selectedItem = item;

        if (detailPanel == null) return;
        detailPanel.SetActive(true);

        if (detailItemName != null)
            detailItemName.text = item.itemName;

        if (detailDescription != null)
            detailDescription.text = item.description;

        // Show use button only for equippable / consumable items
        bool canUse = item.isEquippable || item.itemType == ItemType.Consumable;
        useButton.gameObject.SetActive(canUse);

        if (canUse)
        {
            bool isEquipped = characterState.IsEquipped(item);

            useButtonText.enableWordWrapping = false;
            useButtonText.fontSize = 18;

            if (item.isEquippable)
            {
                useButtonText.text = isEquipped ? "Unequip" : "Equip";
            }
            else
            {
                useButtonText.text = "Use";
            }

            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() => OnUseButtonClicked(item));
        }
    }

    private void OnUseButtonClicked(ItemData item)
    {
        if (!item.isEquippable) return;

        bool isEquipped = characterState.IsEquipped(item);
        if (isEquipped)
            characterState.UnequipItem(item.equipSlot);
        else
            characterState.EquipItem(item);

        // Refresh UI
        RefreshGrid();
        SelectItem(item);
    }
}
