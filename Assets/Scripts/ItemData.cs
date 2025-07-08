using UnityEngine;

/// <summary>
/// ScriptableObject to define properties of a specific item type.
/// Create new ItemData assets via Right-Click -> Create -> Inventory -> Item Data.
/// </summary>
[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Tooltip("Unique ID for the item. Good practice to use a string name that matches the asset name.")]
    public string ItemID;

    [Tooltip("Display name of the item in UI.")]
    public string ItemName;

    [Tooltip("Description of the item.")]
    [TextArea(3, 5)]
    public string ItemDescription;

    [Tooltip("Sprite icon to represent the item in UI.")]
    public Sprite ItemIcon;

    // You can add more properties here, e.g., stackable, max stack size, value, item type (consumable, quest, equipment)
    // public bool IsStackable = true;
    // public int MaxStackSize = 99;

    private void OnValidate()
    {
        // Ensure ItemID is set to the asset's name by default for consistency.
        if (string.IsNullOrEmpty(ItemID))
        {
            ItemID = name;
        }
    }
}