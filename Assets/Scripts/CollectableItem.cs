using UnityEngine;

/// <summary>
/// Makes a GameObject collectable by the player.
/// Adds the specified ItemData to the InventoryManager when picked up.
/// </summary>
[RequireComponent(typeof(Collider2D))] // Requires a Collider2D to detect trigger. Make sure it's set to 'Is Trigger'.
public class CollectableItem : MonoBehaviour
{
    [Tooltip("The ItemData ScriptableObject that this collectable represents.")]
    [SerializeField]
    private ItemData _itemData; // Corrected: Now references the top-level ItemData ScriptableObject

    [Tooltip("The quantity of the item to add to inventory when collected.")]
    [SerializeField]
    private int _quantity = 1;

    private void Awake()
    {
        // Ensure Collider2D is set to trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"CollectableItem on {gameObject.name}: Collider2D is not set to 'Is Trigger'. Setting it now.");
            col.isTrigger = true;
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// </summary>
    /// <param name="other">The other Collider2D involved in this collision.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collider entering the trigger is the player.
        // Make sure your player GameObject has the tag "Player".
        if (other.CompareTag("Player"))
        {
            if (_itemData == null)
            {
                Debug.LogWarning($"CollectableItem on {gameObject.name}: ItemData is not assigned. Cannot collect.");
                return;
            }

            if (InventoryManager.Instance != null)
            {
                // Corrected: AddItem now takes ItemData and quantity
                if (InventoryManager.Instance.AddItem(_itemData, _quantity))
                {
                    Debug.Log($"Player collected {_quantity} x {_itemData.ItemName}.");
                    // Optionally, play a sound or particle effect here before destroying.
                    Destroy(gameObject); // Destroy the collectable item from the scene.
                }
                else
                {
                    Debug.Log($"Failed to add {_itemData.ItemName} to inventory. (Inventory might be full, if that logic is implemented).");
                }
            }
            else
            {
                Debug.LogError("InventoryManager.Instance not found! Make sure an InventoryManager exists in the scene.");
            }
        }
    }

    // Optional: For visual debugging in editor, draw gizmo
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (col is BoxCollider2D box)
            {
                Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(circle.bounds.center, circle.radius);
            }
        }
        // Corrected: ItemIcon is now directly on ItemData
        if (_itemData != null && _itemData.ItemIcon != null)
        {
            Gizmos.DrawIcon(transform.position + Vector3.up * 0.5f, _itemData.ItemIcon.name, true);
        }
    }
}
