using UnityEngine;
using System.Collections.Generic; // For List<T>
using System; // For Action event

/// <summary>
/// Manages the player's inventory. Singleton pattern.
/// Handles adding, removing, and checking for items.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // Represents an item within the inventory with its quantity.
    [System.Serializable]
    public class InventoryItem
    {
        public ItemData ItemData;
        public int Quantity;

        public InventoryItem(ItemData data, int quantity = 1)
        {
            ItemData = data;
            Quantity = quantity;
        }
    }

    [Tooltip("The list of items currently in the player's inventory.")]
    [SerializeField]
    private List<InventoryItem> _inventory = new List<InventoryItem>();

    // Event for when the inventory changes (useful for UI updates, though not directly used in this quest logic).
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); // Persist inventory across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adds an item to the inventory.
    /// </summary>
    /// <param name="itemData">The ItemData of the item to add.</param>
    /// <param name="quantity">The amount to add.</param>
    /// <returns>True if item was added successfully, false otherwise (e.g., inventory full, not implemented).</returns>
    public bool AddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || quantity <= 0)
        {
            Debug.LogWarning("Attempted to add invalid item or quantity.");
            return false;
        }

        // For simplicity, let's assume items don't stack for now unless they are the same exact ItemData object.
        // You would typically add stacking logic here (e.g., find existing stack and increment quantity).
        foreach (var item in _inventory)
        {
            if (item.ItemData == itemData)
            {
                item.Quantity += quantity;
                Debug.Log($"Added {quantity} x {itemData.ItemName}. Total: {item.Quantity}");
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // If not found, add new entry.
        _inventory.Add(new InventoryItem(itemData, quantity));
        Debug.Log($"Added new item: {itemData.ItemName}. Quantity: {quantity}");
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes an item from the inventory.
    /// </summary>
    /// <param name="itemData">The ItemData of the item to remove.</param>
    /// <param name="quantity">The amount to remove.</param>
    /// <returns>True if item was removed successfully, false if not found or not enough quantity.</returns>
    public bool RemoveItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || quantity <= 0)
        {
            Debug.LogWarning("Attempted to remove invalid item or quantity.");
            return false;
        }

        InventoryItem itemToRemove = null;
        foreach (var item in _inventory)
        {
            if (item.ItemData == itemData)
            {
                itemToRemove = item;
                break;
            }
        }

        if (itemToRemove != null)
        {
            if (itemToRemove.Quantity >= quantity)
            {
                itemToRemove.Quantity -= quantity;
                Debug.Log($"Removed {quantity} x {itemData.ItemName}. Remaining: {itemToRemove.Quantity}");
                if (itemToRemove.Quantity <= 0)
                {
                    _inventory.Remove(itemToRemove);
                    Debug.Log($"{itemData.ItemName} removed completely from inventory.");
                }
                OnInventoryChanged?.Invoke();
                return true;
            }
            else
            {
                Debug.LogWarning($"Not enough {itemData.ItemName} to remove {quantity}. Only {itemToRemove.Quantity} available.");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"Attempted to remove {itemData.ItemName} but it was not found in inventory.");
            return false;
        }
    }

    /// <summary>
    /// Checks if the player has a specific item and quantity in their inventory.
    /// </summary>
    /// <param name="itemData">The ItemData to check for.</param>
    /// <param name="requiredQuantity">The minimum quantity required.</param>
    /// <returns>True if the item is present with at least the required quantity, false otherwise.</returns>
    public bool HasItem(ItemData itemData, int requiredQuantity = 1)
    {
        if (itemData == null || requiredQuantity <= 0) return false;

        foreach (var item in _inventory)
        {
            if (item.ItemData == itemData && item.Quantity >= requiredQuantity)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the current quantity of a specific item in the inventory.
    /// </summary>
    /// <param name="itemData">The ItemData to get the quantity for.</param>
    /// <returns>The quantity of the item, or 0 if not found.</returns>
    public int GetItemQuantity(ItemData itemData)
    {
        if (itemData == null) return 0;

        foreach (var item in _inventory)
        {
            if (item.ItemData == itemData)
            {
                return item.Quantity;
            }
        }
        return 0;
    }

    // Optional: For debugging purposes, you can call this via a button in the Inspector
    [ContextMenu("Log Inventory")]
    public void LogInventory()
    {
        if (_inventory.Count == 0)
        {
            Debug.Log("Inventory is empty.");
            return;
        }

        string log = "Current Inventory:\n";
        foreach (var item in _inventory)
        {
            log += $"- {item.ItemData.ItemName} (ID: {item.ItemData.ItemID}), Quantity: {item.Quantity}\n";
        }
        Debug.Log(log);
    }
}
