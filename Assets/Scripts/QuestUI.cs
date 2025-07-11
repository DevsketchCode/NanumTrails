using UnityEngine;
using UnityEngine.UI; // Required for ScrollRect, LayoutGroup, and Button
using TMPro; // Required for TextMeshProUGUI
using System.Collections.Generic; // Required for List, Dictionary
using System.Linq; // Required for .ToList()

/// <summary>
/// Manages the display of the quest log UI, friends list UI, and inventory UI.
/// Displays active and completed quests, friends, and inventory items, and handles toggling visibility.
/// This script should be placed on a single, dedicated UI Manager GameObject in the scene.
/// </summary>
public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance { get; private set; }

    [Header("UI References - Quest Log")]
    [Tooltip("The main panel GameObject for the quest log.")]
    [SerializeField] private GameObject _questLogPanel;
    [Tooltip("The Transform parent for the dynamically created quest entries (e.g., the Content GameObject inside your ScrollView).")]
    [SerializeField] private Transform _questEntriesParent;
    [Tooltip("The TextMeshProUGUI prefab used to display a single quest entry.")]
    [SerializeField] private TextMeshProUGUI _questEntryPrefab;
    [Tooltip("The GameObject representing the icon that indicates a quest is available/active. This should be a Button or have a Button component.")]
    [SerializeField] private GameObject _questIcon; // Reference to the Quest Icon GameObject

    [Header("UI References - Friends List")]
    [Tooltip("The main panel GameObject for the friends list.")]
    [SerializeField] private GameObject _friendsPanel; // Separate GameObject for Friends Panel
    [Tooltip("The Transform parent for the dynamically created friends entries (e.g., the Content GameObject inside your Friends ScrollView).")]
    [SerializeField] private Transform _friendsEntriesParent; // For dynamic friends list entries
    [Tooltip("The TextMeshProUGUI prefab used to display a single friends entry.")]
    [SerializeField] private TextMeshProUGUI _friendsEntryPrefab; // NEW: Dedicated prefab for friends entries
    [Tooltip("The GameObject representing the icon that indicates the friends list is available. This should be a Button or have a Button component.")]
    [SerializeField] private GameObject _friendsIcon; // Reference to the Friends Icon GameObject

    [Header("UI References - Inventory")] // New Header for Inventory
    [Tooltip("The main panel GameObject for the inventory.")]
    [SerializeField] private GameObject _inventoryPanel; // New: Separate GameObject for Inventory Panel
    [Tooltip("The Transform parent for the dynamically created inventory entries (e.g., the Content GameObject inside your Inventory ScrollView).")]
    [SerializeField] private Transform _inventoryEntriesParent; // New: For dynamic inventory list entries
    [Tooltip("The TextMeshProUGUI prefab used to display a single inventory entry.")]
    [SerializeField] private TextMeshProUGUI _inventoryEntryPrefab; // NEW: Dedicated prefab for inventory entries
    [Tooltip("The GameObject representing the icon that indicates the inventory is available. This should be a Button or have a Button component.")]
    [SerializeField] private GameObject _inventoryIcon; // New: Reference to the Inventory Icon GameObject

    [Header("UI References - Joy Meter")] // NEW: Joy Meter Header
    [Tooltip("The RectTransform of the parent panel/background for the Joy Meter bar.")]
    [SerializeField] private RectTransform _joyMeterPanelRect; // Reference to the Joy Meter background panel's RectTransform
    [Tooltip("The RectTransform of the image that will fill up the Joy Meter.")]
    [SerializeField] private RectTransform _joyMeterFillRect; // Reference to the Joy Meter fill image's RectTransform
    [Tooltip("The total number of quests that contribute to filling the Joy Meter from 0% to 100%.")]
    [SerializeField] private int _totalQuestsForJoyMeter = 1; // Set this to the total number of quests in your game

    private CanvasGroup _questLogCanvasGroup; // Reference to the CanvasGroup on _questLogPanel
    private CanvasGroup _friendsCanvasGroup; // Reference to the CanvasGroup on _friendsPanel
    private CanvasGroup _inventoryCanvasGroup; // New: Reference to the CanvasGroup on _inventoryPanel

    private float _maxJoyMeterFillWidth; // NEW: Stores the initial full width of the fill image

    /// <summary>
    /// Property to check if the quest log is currently open.
    /// </summary>
    public bool IsQuestLogOpen { get; private set; }
    /// <summary>
    /// Property to check if the friends list is currently open.
    /// </summary>
    public bool IsFriendsListOpen { get; private set; }
    /// <summary>
    /// Property to check if the inventory is currently open.
    /// </summary>
    public bool IsInventoryOpen { get; private set; } // New: Property for inventory state

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the singleton and gets component references.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Get CanvasGroup components from the assigned panels
        _questLogCanvasGroup = _questLogPanel.GetComponent<CanvasGroup>();
        if (_questLogCanvasGroup == null)
        {
            Debug.LogError("QuestUI: Quest Log Panel must have a CanvasGroup component!");
            enabled = false; // Disable script if essential component is missing
            return;
        }

        _friendsCanvasGroup = _friendsPanel.GetComponent<CanvasGroup>();
        if (_friendsCanvasGroup == null)
        {
            Debug.LogError("QuestUI: Friends Panel must have a CanvasGroup component!");
            enabled = false; // Disable script if essential component is missing
            return;
        }

        _inventoryCanvasGroup = _inventoryPanel.GetComponent<CanvasGroup>();
        if (_inventoryCanvasGroup == null)
        {
            Debug.LogError("QuestUI: Inventory Panel must have a CanvasGroup component!");
            enabled = false; // Disable script if essential component is missing
            return;
        }

        // Validate essential dynamic content parents
        if (_questEntriesParent == null)
        {
            Debug.LogError("QuestUI: Quest Entries Parent is not assigned!");
            enabled = false;
            return;
        }
        if (_friendsEntriesParent == null)
        {
            Debug.LogError("QuestUI: Friends Entries Parent is not assigned!");
            enabled = false;
            return;
        }
        if (_inventoryEntriesParent == null)
        {
            Debug.LogError("QuestUI: Inventory Entries Parent is not assigned!");
            enabled = false;
            return;
        }
        if (_questEntryPrefab == null)
        {
            Debug.LogError("QuestUI: Quest Entry Prefab is not assigned!");
            enabled = false;
            return;
        }
        // NEW: Validate friends entry prefab
        if (_friendsEntryPrefab == null)
        {
            Debug.LogError("QuestUI: Friends Entry Prefab is not assigned!");
            enabled = false;
            return;
        }
        // NEW: Validate inventory entry prefab
        if (_inventoryEntryPrefab == null)
        {
            Debug.LogError("QuestUI: Inventory Entry Prefab is not assigned!");
            enabled = false;
            return;
        }
        // NEW: Validate Joy Meter RectTransforms
        if (_joyMeterPanelRect == null)
        {
            Debug.LogError("QuestUI: Joy Meter Panel Rect is not assigned!");
            enabled = false;
            return;
        }
        if (_joyMeterFillRect == null)
        {
            Debug.LogError("QuestUI: Joy Meter Fill Rect is not assigned!");
            enabled = false;
            return;
        }


        // Ensure all panels and icons are hidden initially.
        HideQuestLogImmediately();
        HideFriendsListImmediately();
        HideInventoryImmediately(); // New: Hide inventory panel initially

        // Setup Quest Icon Button Listener
        if (_questIcon != null)
        {
            Button questIconButton = _questIcon.GetComponent<Button>();
            if (questIconButton != null)
            {
                questIconButton.onClick.AddListener(ToggleQuestLog);
            }
            else
            {
                Debug.LogWarning("QuestUI: Quest Icon GameObject does not have a Button component. Cannot set click listener.");
            }
            _questIcon.SetActive(false); // Hide quest icon initially
        }
        else
        {
            Debug.LogWarning("QuestUI: Quest Icon GameObject is not assigned.");
        }

        // Setup Friends Icon Button Listener
        if (_friendsIcon != null)
        {
            Button friendsIconButton = _friendsIcon.GetComponent<Button>();
            if (friendsIconButton != null)
            {
                friendsIconButton.onClick.AddListener(ToggleFriendsList);
            }
            else
            {
                Debug.LogWarning("QuestUI: Friends Icon GameObject does not have a Button component. Cannot set click listener.");
            }
            _friendsIcon.SetActive(false); // Hide friends icon initially
        }
        else
        {
            Debug.LogWarning("QuestUI: Friends Icon GameObject is not assigned.");
        }

        // Setup Inventory Icon Button Listener
        if (_inventoryIcon != null) // New: Setup for Inventory Icon Button
        {
            Button inventoryIconButton = _inventoryIcon.GetComponent<Button>();
            if (inventoryIconButton != null)
            {
                inventoryIconButton.onClick.AddListener(ToggleInventory);
            }
            else
            {
                Debug.LogWarning("QuestUI: Inventory Icon GameObject does not have a Button component. Cannot set click listener.");
            }
            _inventoryIcon.SetActive(false); // Hide inventory icon initially
        }
        else
        {
            Debug.LogWarning("QuestUI: Inventory Icon GameObject is not assigned.");
        }
    }

    /// <summary>
    /// Start is called on the frame when a script is first enabled just before any Update methods are called the first time.
    /// This is a good place to initialize UI elements that depend on layout being built.
    /// </summary>
    private void Start()
    {
        // Capture the initial full width of the fill image. This assumes the fill image is
        // initially set to its 100% width in the editor.
        if (_joyMeterFillRect != null)
        {
            _maxJoyMeterFillWidth = _joyMeterFillRect.rect.width;
            Debug.Log($"Joy Meter: Captured Max Fill Width = {_maxJoyMeterFillWidth:F2}");
        }

        // Initialize Joy Meter to 15% after UI layout has been built
        UpdateJoyMeterDisplay();
    }

    /// <summary>
    /// Toggles the visibility of the quest log.
    /// </summary>
    public void ToggleQuestLog()
    {
        // Close other panels if they are open
        if (IsFriendsListOpen) HideFriendsList();
        if (IsInventoryOpen) HideInventory();

        // Only toggle if there are quests, otherwise just ensure it's hidden
        if (QuestManager.Instance != null && QuestManager.Instance.GetAllQuests().Count > 0)
        {
            IsQuestLogOpen = !IsQuestLogOpen;
            if (IsQuestLogOpen)
            {
                ShowQuestLog();
            }
            else
            {
                HideQuestLog();
            }
        }
        else
        {
            // If no quests, ensure panel is hidden
            HideQuestLog();
            Debug.Log("No active or completed quests to display.");
        }
    }

    /// <summary>
    /// Shows the quest log and updates its content.
    /// </summary>
    private void ShowQuestLog()
    {
        _questLogPanel.SetActive(true);
        _questLogCanvasGroup.alpha = 1f;
        _questLogCanvasGroup.interactable = true;
        _questLogCanvasGroup.blocksRaycasts = true;
        UpdateQuestDisplay();
    }

    /// <summary>
    /// Hides the quest log.
    /// </summary>
    private void HideQuestLog()
    {
        _questLogCanvasGroup.alpha = 0f;
        _questLogCanvasGroup.interactable = false;
        _questLogCanvasGroup.blocksRaycasts = false;
        _questLogPanel.SetActive(false);
    }

    /// <summary>
    /// Hides the quest log immediately without any fade.
    /// Used for initial setup.
    /// </summary>
    private void HideQuestLogImmediately()
    {
        if (_questLogCanvasGroup != null)
        {
            _questLogCanvasGroup.alpha = 0f;
            _questLogCanvasGroup.interactable = false;
            _questLogCanvasGroup.blocksRaycasts = false;
        }
        _questLogPanel.SetActive(false);
        IsQuestLogOpen = false;
    }

    /// <summary>
    /// Toggles the visibility of the friends list.
    /// </summary>
    public void ToggleFriendsList()
    {
        // Close other panels if they are open
        if (IsQuestLogOpen) HideQuestLog();
        if (IsInventoryOpen) HideInventory();

        // Only toggle if there are friends, otherwise just ensure it's hidden
        if (QuestManager.Instance != null && QuestManager.Instance.GetFriends().Count > 0)
        {
            IsFriendsListOpen = !IsFriendsListOpen;
            if (IsFriendsListOpen)
            {
                ShowFriendsList();
            }
            else
            {
                HideFriendsList();
            }
        }
        else
        {
            // If no friends, ensure panel is hidden
            HideFriendsList();
            Debug.Log("No friends to display.");
        }
    }

    /// <summary>
    /// Shows the friends list and updates its content.
    /// </summary>
    private void ShowFriendsList()
    {
        _friendsPanel.SetActive(true);
        _friendsCanvasGroup.alpha = 1f;
        _friendsCanvasGroup.interactable = true;
        _friendsCanvasGroup.blocksRaycasts = true;
        UpdateFriendsDisplay(); // Call a dedicated update for friends
    }

    /// <summary>
    /// Hides the friends list.
    /// </summary>
    private void HideFriendsList()
    {
        _friendsCanvasGroup.alpha = 0f;
        _friendsCanvasGroup.interactable = false;
        _friendsCanvasGroup.blocksRaycasts = false;
        _friendsPanel.SetActive(false);
    }

    /// <summary>
    /// Hides the friends list immediately without any fade.
    /// Used for initial setup.
    /// </summary>
    private void HideFriendsListImmediately()
    {
        if (_friendsCanvasGroup != null)
        {
            _friendsCanvasGroup.alpha = 0f;
            _friendsCanvasGroup.interactable = false;
            _friendsCanvasGroup.blocksRaycasts = false;
        }
        _friendsPanel.SetActive(false);
        IsFriendsListOpen = false;
    }

    /// <summary>
    /// Toggles the visibility of the inventory.
    /// </summary>
    public void ToggleInventory() // New method to toggle inventory
    {
        // Close other panels if they are open
        if (IsQuestLogOpen) HideQuestLog();
        if (IsFriendsListOpen) HideFriendsList();

        // Always toggle the inventory panel, regardless of content
        IsInventoryOpen = !IsInventoryOpen;
        if (IsInventoryOpen)
        {
            ShowInventory();
        }
        else
        {
            HideInventory();
        }
    }

    /// <summary>
    /// Shows the inventory and updates its content.
    /// </summary>
    private void ShowInventory() // New method to show inventory
    {
        _inventoryPanel.SetActive(true);
        _inventoryCanvasGroup.alpha = 1f;
        _inventoryCanvasGroup.interactable = true;
        _inventoryCanvasGroup.blocksRaycasts = true;
        UpdateInventoryDisplay(); // Call a dedicated update for inventory
    }

    /// <summary>
    /// Hides the inventory.
    /// </summary>
    private void HideInventory() // New method to hide inventory
    {
        _inventoryCanvasGroup.alpha = 0f;
        _inventoryCanvasGroup.interactable = false;
        _inventoryCanvasGroup.blocksRaycasts = false;
        _inventoryPanel.SetActive(false);
    }

    /// <summary>
    /// Hides the inventory immediately without any fade.
    /// Used for initial setup.
    /// </summary>
    private void HideInventoryImmediately() // New method to hide inventory immediately
    {
        if (_inventoryCanvasGroup != null)
        {
            _inventoryCanvasGroup.alpha = 0f;
            _inventoryCanvasGroup.interactable = false;
            _inventoryCanvasGroup.blocksRaycasts = false;
        }
        _inventoryPanel.SetActive(false);
        IsInventoryOpen = false;
    }

    /// <summary>
    /// Updates the display of quests in the UI.
    /// Clears existing entries and recreates them based on the QuestManager.
    /// </summary>
    public void UpdateQuestDisplay()
    {
        // Clear existing quest entries
        foreach (Transform child in _questEntriesParent)
        {
            Destroy(child.gameObject);
        }

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("QuestUI: QuestManager.Instance is null. Cannot update quest display.");
            return;
        }

        // Display Quests
        foreach (var quest in QuestManager.Instance.GetAllQuests())
        {
            TextMeshProUGUI questEntry = Instantiate(_questEntryPrefab, _questEntriesParent);
            string questText = $"{quest.NPCName}: {quest.QuestSummary}";

            if (quest.IsCompleted)
            {
                questEntry.text = $"<s>{questText}</s>"; // Apply strikethrough for completed quests
                questEntry.fontStyle = FontStyles.Strikethrough; // Also set font style for consistency
                questEntry.color = Color.gray; // Optionally make completed quests grey
            }
            else
            {
                questEntry.text = questText;
            }
        }
        // If no quests after update, ensure the icon is hidden (though QuestManager handles showing)
        if (QuestManager.Instance.GetAllQuests().Count == 0)
        {
            HideQuestIcon();
        }

        // NEW: Update Joy Meter display after quest list is updated
        UpdateJoyMeterDisplay();
    }

    /// <summary>
    /// Updates the display of the friends list in the UI.
    /// Clears existing entries and recreates them based on the QuestManager.
    /// </summary>
    public void UpdateFriendsDisplay()
    {
        // Clear existing friends entries
        foreach (Transform child in _friendsEntriesParent) // Use _friendsEntriesParent
        {
            Destroy(child.gameObject);
        }

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("QuestUI: QuestManager.Instance is null. Cannot update friends display.");
            return;
        }

        List<string> friends = QuestManager.Instance.GetFriends().ToList();

        if (friends.Count > 0)
        {
            foreach (string friendName in friends)
            {
                TextMeshProUGUI friendEntry = Instantiate(_friendsEntryPrefab, _friendsEntriesParent); // NOW USES _friendsEntryPrefab
                friendEntry.text = friendName;
            }
        }
        else
        {
            // If no friends, display a message by instantiating a new entry
            TextMeshProUGUI noFriendsEntry = Instantiate(_friendsEntryPrefab, _friendsEntriesParent); // NOW USES _friendsEntryPrefab
            noFriendsEntry.text = "None yet.";
        }
        // If no friends after update, ensure the icon is hidden (though QuestManager handles showing)
        if (QuestManager.Instance.GetFriends().Count == 0)
        {
            HideFriendsIcon(); // Assuming you'd want to hide the friends icon if list becomes empty
        }
    }

    /// <summary>
    /// Updates the display of the inventory in the UI.
    /// Clears existing entries and recreates them based on the InventoryManager.
    /// </summary>
    public void UpdateInventoryDisplay() // New: Method to update inventory display
    {
        // Clear existing inventory entries
        foreach (Transform child in _inventoryEntriesParent)
        {
            Destroy(child.gameObject);
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("QuestUI: InventoryManager.Instance is null. Cannot update inventory display.");
            return;
        }

        // Get the inventory contents as a dictionary of ItemData and quantities
        IReadOnlyDictionary<ItemData, int> inventoryContents = InventoryManager.Instance.GetInventoryContents();

        if (inventoryContents.Count > 0)
        {
            foreach (var itemEntry in inventoryContents) // itemEntry is KeyValuePair<ItemData, int>
            {
                ItemData item = itemEntry.Key;
                int quantity = itemEntry.Value;

                TextMeshProUGUI displayItem = Instantiate(_inventoryEntryPrefab, _inventoryEntriesParent); // NOW USES _inventoryEntryPrefab
                displayItem.text = $"{item.ItemName} x{quantity}"; // Display name and quantity
            }
        }
        else
        {
            TextMeshProUGUI noItemsEntry = Instantiate(_inventoryEntryPrefab, _inventoryEntriesParent); // NOW USES _inventoryEntryPrefab
            noItemsEntry.text = "Inventory is Empty.";
            // Also hide the inventory icon if the inventory becomes empty
            HideInventoryIcon();
        }
    }

    /// <summary>
    /// Updates the visual fill level of the Joy Meter based on completed quests.
    /// </summary>
    public void UpdateJoyMeterDisplay() // NEW: Method to update Joy Meter
    {
        if (_joyMeterPanelRect == null || _joyMeterFillRect == null || _totalQuestsForJoyMeter <= 0 || QuestManager.Instance == null)
        {
            Debug.LogWarning("QuestUI: Joy Meter references or total quests not set up correctly. Cannot update display.");
            return;
        }

        int numCompletedQuests = QuestManager.Instance.GetAllQuests().Count(q => q.IsCompleted);
        float progress = (float)numCompletedQuests / _totalQuestsForJoyMeter;

        // Clamp progress between 0 and 1
        progress = Mathf.Clamp01(progress);

        // Apply the 15% minimum visual fill if no quests are completed yet
        float minFillPercentage = 0.15f;
        float actualFillProgress = Mathf.Max(minFillPercentage, progress);

        // Calculate target width based on the CAPTURED MAX FILL WIDTH
        float targetWidth = _maxJoyMeterFillWidth * actualFillProgress;

        Debug.Log($"Joy Meter: Completed Quests = {numCompletedQuests}, Progress = {progress:F2}, Actual Fill Progress = {actualFillProgress:F2}, Max Fill Width = {_maxJoyMeterFillWidth:F2}, Target Width = {targetWidth:F2}");


        // Set the width of the fill image's RectTransform.
        // Ensure _joyMeterFillRect's anchors are set to (0, 0.5) for min and max, and pivot to (0, 0.5) in Inspector
        // for left-aligned scaling.
        _joyMeterFillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
    }

    /// <summary>
    /// Shows the Quest Icon.
    /// </summary>
    public void ShowQuestIcon()
    {
        if (_questIcon != null)
        {
            _questIcon.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the Quest Icon.
    /// </summary>
    public void HideQuestIcon()
    {
        if (_questIcon != null)
        {
            _questIcon.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the Friends Icon.
    /// </summary>
    public void ShowFriendsIcon()
    {
        if (_friendsIcon != null)
        {
            _friendsIcon.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the Friends Icon.
    /// </summary>
    public void HideFriendsIcon()
    {
        if (_friendsIcon != null)
        {
            _friendsIcon.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the Inventory Icon.
    /// </summary>
    public void ShowInventoryIcon()
    {
        if (_inventoryIcon != null)
        {
            _inventoryIcon.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the Inventory Icon.
    /// </summary>
    public void HideInventoryIcon()
    {
        if (_inventoryIcon != null)
        {
            _inventoryIcon.SetActive(false);
        }
    }
}
