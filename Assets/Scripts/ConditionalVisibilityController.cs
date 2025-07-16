using UnityEngine;
using System.Collections.Generic; // Required for List

/// <summary>
/// Controls the visibility of SpriteRenderers on child GameObjects and the enabled state of a PolygonCollider2D
/// on the parent GameObject, based on whether the player possesses a specific ItemData in their inventory.
/// This script should be attached to the parent GameObject.
/// </summary>
public class ConditionalVisibilityController : MonoBehaviour
{
    [Header("Conditional Visibility Settings")]
    [Tooltip("Enable this to activate the conditional visibility logic on this GameObject.")]
    [SerializeField] private bool _enableConditionalVisibility = false;

    [Tooltip("The ItemData ScriptableObject to check for in the player's inventory and consume.")]
    [SerializeField] private ItemData _itemForVisibilityCheck;

    [Tooltip("A list of SpriteRenderer components on CHILD GameObjects to enable if the player has the item, and disable if not. " +
             "These must be assigned manually in the Inspector.")]
    [SerializeField] private List<SpriteRenderer> _targetSpriteRenderers = new List<SpriteRenderer>();

    [Tooltip("The PolygonCollider2D component on THIS GameObject to disable if the player has the item, and enable if not. " +
             "If left unassigned, it will try to find a PolygonCollider2D on this GameObject.")]
    [SerializeField] private PolygonCollider2D _targetPolygonCollider;

    // Reference to the CapsuleCollider2D on THIS GameObject that will act as the trigger for updates
    private CapsuleCollider2D _triggerCollider;

    // NEW: Flag to track if this specific object has been activated by item consumption
    private bool _isActivatedByItemConsumption = false;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Gets component references if they are not assigned in the Inspector.
    /// </summary>
    private void Awake()
    {
        // Get the PolygonCollider2D from this GameObject if not assigned.
        if (_targetPolygonCollider == null)
        {
            _targetPolygonCollider = GetComponent<PolygonCollider2D>();
        }

        // Get the CapsuleCollider2D from this GameObject and ensure it's a trigger.
        _triggerCollider = GetComponent<CapsuleCollider2D>();
        if (_triggerCollider == null)
        {
            Debug.LogWarning($"ConditionalVisibilityController on {gameObject.name}: No CapsuleCollider2D found. Conditional visibility will only update on Start().");
        }
        else if (!_triggerCollider.isTrigger)
        {
            Debug.LogWarning($"ConditionalVisibilityController on {gameObject.name}: CapsuleCollider2D is not set to 'Is Trigger'. Setting it now.");
            _triggerCollider.isTrigger = true;
        }

        // Note: _targetSpriteRenderers are expected to be assigned manually in the Inspector
        // as they are on child GameObjects. No automatic GetComponent for children here.
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Updates the visibility and collider state based on the current inventory at start.
    /// </summary>
    private void Start()
    {
        // Initial check for visibility based on item presence or if already activated
        UpdateVisibility();
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// This will now trigger the visibility update and potentially consume an item.
    /// </summary>
    /// <param name="other">The other Collider2D involved in this collision.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only proceed if the entering collider is the player
        if (other.CompareTag("Player"))
        {
            // If conditional visibility is enabled and this object hasn't been activated yet
            if (_enableConditionalVisibility && !_isActivatedByItemConsumption)
            {
                // Check for required references
                if (_itemForVisibilityCheck == null)
                {
                    Debug.LogWarning($"ConditionalVisibilityController on {gameObject.name}: Item for visibility check is not assigned. Cannot consume item or update visibility.");
                    return;
                }

                if (InventoryManager.Instance == null)
                {
                    Debug.LogError($"ConditionalVisibilityController on {gameObject.name}: InventoryManager.Instance not found! Make sure an InventoryManager exists in the scene.");
                    return;
                }

                // Check if the player has the required item
                if (InventoryManager.Instance.HasItem(_itemForVisibilityCheck))
                {
                    // Attempt to remove one instance of the item
                    if (InventoryManager.Instance.RemoveItem(_itemForVisibilityCheck, 1))
                    {
                        _isActivatedByItemConsumption = true; // Mark this object as permanently activated
                        Debug.Log($"ConditionalVisibilityController on {gameObject.name}: Consumed 1 x {_itemForVisibilityCheck.ItemName} to activate object.");
                    }
                    else
                    {
                        Debug.LogWarning($"ConditionalVisibilityController on {gameObject.name}: Failed to remove {_itemForVisibilityCheck.ItemName} from inventory, even though player reportedly has it.");
                    }
                }
                else
                {
                    Debug.Log($"ConditionalVisibilityController on {gameObject.name}: Player does not have {_itemForVisibilityCheck.ItemName}. Cannot activate.");
                }
            }
            // Always update visibility after a trigger, even if not consuming an item,
            // or if it's already activated, to ensure the correct state is displayed.
            UpdateVisibility();
        }
    }

    /// <summary>
    /// Updates the SpriteRenderers (on children) and PolygonCollider2D (on parent)
    /// based on whether the object has been _isActivatedByItemConsumption or
    /// if the player currently possesses the specified inventory item (if not yet activated).
    ///
    /// If _isActivatedByItemConsumption is TRUE:
    /// - All target SpriteRenderers will be ENABLED.
    /// - The target PolygonCollider2D will be DISABLED.
    ///
    /// If _isActivatedByItemConsumption is FALSE:
    ///     If the player HAS the item: (This state will likely be brief before consumption)
    ///     - All target SpriteRenderers will be ENABLED.
    ///     - The target PolygonCollider2D will be DISABLED.
    ///
    ///     If the player DOES NOT HAVE the item:
    ///     - All target SpriteRenderers will be DISABLED.
    ///     - The target PolygonCollider2D will be ENABLED.
    /// </summary>
    public void UpdateVisibility()
    {
        // If conditional visibility is not enabled, do nothing.
        if (!_enableConditionalVisibility)
        {
            // Debug.Log($"ConditionalVisibilityController on {gameObject.name}: Conditional Visibility is disabled.");
            return;
        }

        // If the object has been permanently activated by item consumption,
        // force its state to "active" regardless of current inventory.
        if (_isActivatedByItemConsumption)
        {
            // Enable all assigned SpriteRenderers
            foreach (SpriteRenderer sr in _targetSpriteRenderers)
            {
                if (sr != null && !sr.enabled) // Only change if not already enabled
                {
                    sr.enabled = true;
                    // Debug.Log($"ConditionalVisibilityController on {gameObject.name}: SpriteRenderer {sr.name} enabled (activated).");
                }
            }
            // Disable the PolygonCollider2D on this parent GameObject
            if (_targetPolygonCollider != null && _targetPolygonCollider.enabled) // Only change if not already disabled
            {
                _targetPolygonCollider.enabled = false;
                // Debug.Log($"ConditionalVisibilityController on {gameObject.name}: PolygonCollider2D disabled (activated).");
            }
            return; // Exit, as state is fixed by consumption
        }

        // If not yet activated by consumption, check current inventory
        if (_itemForVisibilityCheck == null)
        {
            Debug.LogWarning($"ConditionalVisibilityController on {gameObject.name}: Item for visibility check is not assigned. Cannot update visibility.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError($"ConditionalVisibilityController on {gameObject.name}: InventoryManager.Instance not found! Make sure an InventoryManager exists in the scene.");
            return;
        }

        bool hasItem = InventoryManager.Instance.HasItem(_itemForVisibilityCheck);

        // Apply visibility and collider state based on whether the item is present (before consumption)
        if (hasItem)
        {
            // Enable all assigned SpriteRenderers
            foreach (SpriteRenderer sr in _targetSpriteRenderers)
            {
                if (sr != null && !sr.enabled)
                {
                    sr.enabled = true;
                    // Debug.Log($"ConditionalVisibilityController on {gameObject.name}: SpriteRenderer {sr.name} enabled because player has {_itemForVisibilityCheck.ItemName}.");
                }
            }
            // Disable the PolygonCollider2D on this parent GameObject
            if (_targetPolygonCollider != null && _targetPolygonCollider.enabled)
            {
                _targetPolygonCollider.enabled = false;
                // Debug.Log($"ConditionalVisibilityController on {gameObject.name}: PolygonCollider2D disabled because player has {_itemForVisibilityCheck.ItemName}.");
            }
        }
        else
        {
            // Disable all assigned SpriteRenderers
            foreach (SpriteRenderer sr in _targetSpriteRenderers)
            {
                if (sr != null && sr.enabled)
                {
                    sr.enabled = false;
                    // Debug.Log($"ConditionalVisibilityController on {gameObject.name}: SpriteRenderer {sr.name} disabled because player does NOT have {_itemForVisibilityCheck.ItemName}.");
                }
            }
            // Enable the PolygonCollider2D on this parent GameObject
            if (_targetPolygonCollider != null && !_targetPolygonCollider.enabled)
            {
                _targetPolygonCollider.enabled = true;
                // Debug.Log($"ConditionalVisibilityController on {gameObject.name}: PolygonCollider2D enabled because player does NOT have {_itemForVisibilityCheck.ItemName}.");
            }
        }
    }
}
