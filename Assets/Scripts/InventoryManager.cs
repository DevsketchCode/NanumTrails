using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like ToList()

/// <summary>
/// Manages the player's inventory.
/// This is a singleton that can be accessed from anywhere.
/// Stores items as ItemData ScriptableObjects with their quantities.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // Internal storage for items: ItemData (the ScriptableObject) mapped to its quantity.
    private Dictionary<ItemData, int> _inventoryContents = new Dictionary<ItemData, int>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the singleton.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); // If you want the manager to persist across scenes.
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Adds a specified quantity of an item to the inventory.
    /// </summary>
    /// <param name="itemData">The ItemData ScriptableObject to add.</param>
    /// <param name="quantity">The amount of the item to add.</param>
    /// <returns>True if the item was successfully added, false otherwise.</returns>
    public bool AddItem(ItemData itemData, int quantity = 1) // This is the method overload needed
    {
        if (itemData == null)
        {
            Debug.LogWarning("InventoryManager: Attempted to add a null ItemData.");
            return false;
        }
        if (quantity <= 0)
        {
            Debug.LogWarning($"InventoryManager: Attempted to add non-positive quantity ({quantity}) of {itemData.ItemName}.");
            return false;
        }

        if (_inventoryContents.ContainsKey(itemData))
        {
            _inventoryContents[itemData] += quantity;
        }
        else
        {
            _inventoryContents.Add(itemData, quantity);
        }

        Debug.Log($"Added {quantity} x {itemData.ItemName} to inventory. Total: {_inventoryContents[itemData]}");

        // Notify QuestUI to update display and show icon if inventory is no longer empty
        if (QuestUI.Instance != null)
        {
            QuestUI.Instance.UpdateInventoryDisplay();
            QuestUI.Instance.ShowInventoryIcon(); // Show icon when an item is added
        }
        return true;
    }

    /// <summary>
    /// Removes a specified quantity of an item from the inventory.
    /// </summary>
    /// <param name="itemData">The ItemData ScriptableObject to remove.</param>
    /// <param name="quantity">The amount of the item to remove (defaults to 1).</param>
    /// <returns>True if the item was successfully removed, false otherwise.</returns>
    public bool RemoveItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null)
        {
            Debug.LogWarning("InventoryManager: Attempted to remove a null ItemData.");
            return false;
        }
        if (quantity <= 0)
        {
            Debug.LogWarning($"InventoryManager: Attempted to remove non-positive quantity ({quantity}) of {itemData.ItemName}.");
            return false;
        }

        if (_inventoryContents.ContainsKey(itemData))
        {
            _inventoryContents[itemData] -= quantity;
            if (_inventoryContents[itemData] <= 0)
            {
                _inventoryContents.Remove(itemData);
            }
            Debug.Log($"Removed {quantity} x {itemData.ItemName} from inventory.");

            // Notify QuestUI to update display
            if (QuestUI.Instance != null)
            {
                QuestUI.Instance.UpdateInventoryDisplay();
                // Hide icon if inventory becomes empty
                if (_inventoryContents.Count == 0)
                {
                    QuestUI.Instance.HideInventoryIcon();
                }
            }
            return true;
        }
        else
        {
            Debug.LogWarning($"InventoryManager: Item '{itemData.ItemName}' not found in inventory to remove.");
            return false;
        }
    }

    /// <summary>
    /// Checks if the inventory contains at least one of a specific item.
    /// </summary>
    /// <param name="itemData">The ItemData ScriptableObject to check for.</param>
    /// <returns>True if the item is found, false otherwise.</returns>
    public bool HasItem(ItemData itemData)
    {
        if (itemData == null) return false;
        return _inventoryContents.ContainsKey(itemData) && _inventoryContents[itemData] > 0;
    }

    /// <summary>
    /// Gets the quantity of a specific item in the inventory.
    /// </summary>
    /// <param name="itemData">The ItemData ScriptableObject to check.</param>
    /// <returns>The quantity of the item, or 0 if not found.</returns>
    public int GetItemQuantity(ItemData itemData)
    {
        if (itemData == null) return 0;
        _inventoryContents.TryGetValue(itemData, out int quantity);
        return quantity;
    }

    /// <summary>
    /// Gets a read-only dictionary of all items currently in the inventory with their quantities.
    /// </summary>
    public IReadOnlyDictionary<ItemData, int> GetInventoryContents()
    {
        return _inventoryContents;
    }
}
