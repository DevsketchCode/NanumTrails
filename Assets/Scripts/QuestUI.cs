using UnityEngine;
using UnityEngine.UI; // Required for ScrollRect, LayoutGroup, and Button
using TMPro; // Required for TextMeshProUGUI
using System.Collections.Generic; // Required for List, Dictionary
using System.Linq; // Required for .ToList()
using System.Collections; // Required for Coroutines

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
    [SerializeField] private TextMeshProUGUI _friendsEntryPrefab; // Dedicated prefab for friends entries
    [Tooltip("The GameObject representing the icon that indicates the friends list is available. This should be a Button or have a Button component.")]
    [SerializeField] private GameObject _friendsIcon; // Reference to the Friends Icon GameObject

    [Header("UI References - Inventory")] // New Header for Inventory
    [Tooltip("The main panel GameObject for the inventory.")]
    [SerializeField] private GameObject _inventoryPanel; // New: Separate GameObject for Inventory Panel
    [Tooltip("The Transform parent for the dynamically created inventory entries (e.g., the Content GameObject inside your Inventory ScrollView).")]
    [SerializeField] private Transform _inventoryEntriesParent; // New: For dynamic inventory list entries
    [Tooltip("The TextMeshProUGUI prefab used to display a single inventory entry.")]
    [SerializeField] private TextMeshProUGUI _inventoryEntryPrefab; // Dedicated prefab for inventory entries
    [Tooltip("The GameObject representing the icon that indicates the inventory is available. This should be a Button or have a Button component.")]
    [SerializeField] private GameObject _inventoryIcon; // New: Reference to the Inventory Icon GameObject

    [Header("UI References - Joy Meter")] // Joy Meter Header
    [Tooltip("The RectTransform of the parent panel/background for the Joy Meter bar.")]
    [SerializeField] private RectTransform _joyMeterPanelRect; // Reference to the Joy Meter background panel's RectTransform
    [Tooltip("The RectTransform of the image that will fill up the Joy Meter.")]
    [SerializeField] private RectTransform _joyMeterFillRect; // Reference to the Joy Meter fill image's RectTransform
    [Tooltip("The total number of quests that contribute to filling the Joy Meter from 0% to 100%.")]
    [SerializeField] private int _totalQuestsForJoyMeter = 1; // Set this to the total number of quests in your game
    [Tooltip("The Image GameObject that acts as a glow for the Joy Meter fill bar.")]
    [SerializeField] private Image _joyMeterGlowImage; // Glow image for Joy Meter
    [Tooltip("The RectTransform of the Image that acts as a glow for the Joy Meter bar. This will grow with the Joy Meter.")]
    [SerializeField] private RectTransform _joyMeterGlowRect; // NEW: RectTransform for Joy Meter Glow Image
    [Tooltip("An additional width to add to the Joy Meter glow image, making it slightly larger than the fill bar.")]
    [SerializeField] private float _joyMeterGlowExtraWidth = 0f; // NEW: Extra width for the glow image
    [Tooltip("The Particle System for the Joy Meter fill bar that turns on when it grows.")]
    [SerializeField] private ParticleSystem _joyMeterParticleSystem; // Particle System for Joy Meter
    [Tooltip("The duration (in seconds) the Joy Meter particle system should play.")]
    [SerializeField] private float _joyMeterParticleSystemDuration = 1.0f; // Duration for Joy Meter particle system
    [Tooltip("The maximum width (in world units) the Joy Meter particle system's shape should scale to.")]
    [SerializeField] private float _maxParticleSystemWidth = 2.7f; // Max width for particle system shape
    [Tooltip("An additional X-axis offset for the particle system's local position.")]
    [SerializeField] private float _joyMeterParticleSystemXOffset = 0f; // NEW: X-axis offset for particle system
    [Tooltip("The initial percentage (0-1) the Joy Meter should be filled when no friends are added.")]
    [SerializeField] private float _joyMeterInitialFillPercentage = 0.15f; // NEW: Initial fill percentage

    [Header("Glow Effect Settings")] // Header for glow effect
    [Tooltip("The duration (in seconds) for each fade-in or fade-out cycle of the glow effect.")]
    [SerializeField] private float _glowFadeDuration = 0.5f;
    [Tooltip("The number of times the glow effect should fade in and out.")]
    [SerializeField] private int _glowCycles = 2;
    [Tooltip("The GameObject that contains the CanvasGroup for the Quest Button's glow effect (if any).")]
    [SerializeField] private GameObject _questButtonGlowObject;
    [Tooltip("The GameObject that contains the CanvasGroup for the Friends Button's glow effect (if any).")]
    [SerializeField] private GameObject _friendsButtonGlowObject;
    [Tooltip("The GameObject that contains the CanvasGroup for the Inventory Button's glow effect (if any).")]
    [SerializeField] private GameObject _inventoryButtonGlowObject;


    private CanvasGroup _questLogCanvasGroup; // Reference to the CanvasGroup on _questLogPanel
    private CanvasGroup _friendsCanvasGroup; // Reference to the CanvasGroup on _friendsPanel
    private CanvasGroup _inventoryCanvasGroup; // Reference to the CanvasGroup on _inventoryPanel
    private CanvasGroup _joyMeterGlowCanvasGroup; // CanvasGroup for Joy Meter glow

    private float _maxJoyMeterFillWidth; // Stores the initial full width of the fill image

    // Dictionary to keep track of active glow coroutines for each CanvasGroup
    private Dictionary<CanvasGroup, Coroutine> _activeGlowCoroutines = new Dictionary<CanvasGroup, Coroutine>();
    // Dictionary to keep track of active particle system coroutines
    private Dictionary<ParticleSystem, Coroutine> _activeParticleSystemCoroutines = new Dictionary<ParticleSystem, Coroutine>();

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
    public bool IsInventoryOpen { get; private set; } // Property for inventory state

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

        // Get CanvasGroup for Joy Meter Glow
        if (_joyMeterGlowImage != null)
        {
            _joyMeterGlowCanvasGroup = _joyMeterGlowImage.GetComponent<CanvasGroup>();
            if (_joyMeterGlowCanvasGroup == null)
            {
                Debug.LogWarning("QuestUI: Joy Meter Glow Image does not have a CanvasGroup component. Adding one.");
                _joyMeterGlowCanvasGroup = _joyMeterGlowImage.gameObject.AddComponent<CanvasGroup>();
            }
            // Get RectTransform for Joy Meter Glow Image
            _joyMeterGlowRect = _joyMeterGlowImage.GetComponent<RectTransform>();
            if (_joyMeterGlowRect == null)
            {
                Debug.LogError("QuestUI: Joy Meter Glow Image must have a RectTransform component!");
                enabled = false;
                return;
            }
        }

        // Ensure Joy Meter Particle System is stopped initially
        if (_joyMeterParticleSystem != null)
        {
            _joyMeterParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
        // Validate friends entry prefab
        if (_friendsEntryPrefab == null)
        {
            Debug.LogError("QuestUI: Friends Entry Prefab is not assigned!");
            enabled = false;
            return;
        }
        // Validate inventory entry prefab
        if (_inventoryEntryPrefab == null)
        {
            Debug.LogError("QuestUI: Inventory Entry Prefab is not assigned!");
            enabled = false;
            return;
        }
        // Validate Joy Meter RectTransforms
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
        HideInventoryImmediately(); // Hide inventory panel initially

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

        // Ensure all glow objects are initially transparent and inactive
        InitializeGlowObject(_joyMeterGlowImage?.gameObject);
        InitializeGlowObject(_questButtonGlowObject);
        InitializeGlowObject(_friendsButtonGlowObject);
        InitializeGlowObject(_inventoryButtonGlowObject);
    }

    /// <summary>
    /// Initializes a glow GameObject by ensuring it has a CanvasGroup and is hidden.
    /// </summary>
    /// <param name="glowObject">The GameObject to initialize.</param>
    private void InitializeGlowObject(GameObject glowObject)
    {
        if (glowObject != null)
        {
            CanvasGroup cg = glowObject.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = glowObject.AddComponent<CanvasGroup>();
            }
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            glowObject.SetActive(false);
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
        UpdateJoyMeterVisualOnly(); // Call the private method for initial fill
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

        // Only update the visual fill here, do not trigger effects
        UpdateJoyMeterVisualOnly(); // Calling the new method
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
    /// This method only handles the visual resizing of the bar.
    /// </summary>
    private void _UpdateJoyMeterVisualFill() // Renamed to be private and only handle visual fill
    {
        if (_joyMeterPanelRect == null || _joyMeterFillRect == null || QuestManager.Instance == null)
        {
            Debug.LogWarning("QuestUI: Joy Meter references or QuestManager not set up correctly. Cannot update display.");
            return;
        }

        int friendsCount = QuestManager.Instance.GetFriends().Count;
        int totalPossibleFriends = QuestManager.Instance.GetTotalPossibleFriends();

        float progress = 0f;
        if (totalPossibleFriends > 0)
        {
            // Calculate the portion of the meter filled by friends, relative to the remaining 1 - initial fill
            float fillPerFriend = (1f - _joyMeterInitialFillPercentage) / totalPossibleFriends;
            progress = _joyMeterInitialFillPercentage + (friendsCount * fillPerFriend);
        }
        else
        {
            // If no possible friends defined, just use the initial fill percentage
            progress = _joyMeterInitialFillPercentage;
        }

        // Clamp progress between 0 and 1
        progress = Mathf.Clamp01(progress);

        // Calculate target width based on the CAPTURED MAX FILL WIDTH
        float targetWidth = _maxJoyMeterFillWidth * progress;

        Debug.Log($"Joy Meter: Friends = {friendsCount}/{totalPossibleFriends}, Progress = {progress:F2}, Target Width = {targetWidth:F2}");

        // Set the width of the fill image's RectTransform.
        // Ensure _joyMeterFillRect's anchors are set to (0, 0.5) for min and max, and pivot to (0, 0.5) in Inspector
        // for left-aligned scaling.
        _joyMeterFillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);

        // Also set the width of the glow image's RectTransform to match, plus the extra width
        if (_joyMeterGlowRect != null)
        {
            _joyMeterGlowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth + _joyMeterGlowExtraWidth);
        }
    }

    /// <summary>
    /// Public method to update the Joy Meter's visual fill only, without triggering effects.
    /// </summary>
    public void UpdateJoyMeterVisualOnly()
    {
        _UpdateJoyMeterVisualFill();
    }

    /// <summary>
    /// Updates the Joy Meter's visual fill and triggers its associated particle and glow effects.
    /// This method should be called when a friend is added to the list.
    /// </summary>
    public void UpdateJoyMeterAndTriggerEffects()
    {
        UpdateJoyMeterVisualOnly(); // Corrected: Calling the public method UpdateJoyMeterVisualOnly()

        // Then, trigger the particle system and glow effects
        // Recalculate normalized progress from current width, as _UpdateJoyMeterVisualFill might have changed it
        float currentFillWidth = _joyMeterFillRect.rect.width;
        float normalizedProgress = currentFillWidth / _maxJoyMeterFillWidth;

        UpdateJoyMeterParticleSystemVisuals(normalizedProgress);
        if (_joyMeterParticleSystem != null)
        {
            StartParticleSystemEffect(_joyMeterParticleSystem, _joyMeterParticleSystemDuration);
        }

        if (_joyMeterGlowCanvasGroup != null)
        {
            StartGlowEffect(_joyMeterGlowCanvasGroup);
        }
    }

    /// <summary>
    /// Updates the size and position of the Joy Meter's particle system to match the fill bar's width and align it to the left.
    /// </summary>
    /// <param name="normalizedProgress">The normalized progress (0-1) of the Joy Meter fill bar.</param>
    private void UpdateJoyMeterParticleSystemVisuals(float normalizedProgress)
    {
        if (_joyMeterParticleSystem != null)
        {
            var shape = _joyMeterParticleSystem.shape;
            // Scale the X dimension of the box shape based on the normalized progress and the defined max particle system width.
            // The Y and Z dimensions can be set to match the height of the fill rect or a fixed value.
            float particleSystemWidth = _maxParticleSystemWidth * normalizedProgress;
            shape.scale = new Vector3(particleSystemWidth, _joyMeterFillRect.rect.height, shape.scale.z);

            // Adjust the local position of the particle system so its center aligns with the center
            // of the _joyMeterFillRect, then apply the custom offset.
            // This assumes the particle system's pivot/origin is at its center.
            _joyMeterParticleSystem.transform.localPosition = new Vector3((_joyMeterFillRect.rect.width / 2f) + _joyMeterParticleSystemXOffset, 0f, 0f);

            Debug.Log($"Joy Meter Particle System: Resized to width {particleSystemWidth:F2} and positioned at X: {(_joyMeterFillRect.rect.width / 2f) + _joyMeterParticleSystemXOffset:F2}");
        }
    }


    /// <summary>
    /// Shows the Quest Icon and starts its glow effect.
    /// </summary>
    public void ShowQuestIcon()
    {
        if (_questIcon != null)
        {
            _questIcon.SetActive(true);
            // Start glow effect for Quest Button
            if (_questButtonGlowObject != null)
            {
                CanvasGroup glowCG = _questButtonGlowObject.GetComponent<CanvasGroup>();
                if (glowCG != null) StartGlowEffect(glowCG);
            }
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
            // Stop glow effect for Quest Button if active
            if (_questButtonGlowObject != null)
            {
                CanvasGroup glowCG = _questButtonGlowObject.GetComponent<CanvasGroup>();
                if (glowCG != null && _activeGlowCoroutines.ContainsKey(glowCG))
                {
                    StopCoroutine(_activeGlowCoroutines[glowCG]);
                    _activeGlowCoroutines.Remove(glowCG);
                    glowCG.alpha = 0f;
                    glowCG.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Shows the Friends Icon and starts its glow effect.
    /// </summary>
    public void ShowFriendsIcon()
    {
        if (_friendsIcon != null)
        {
            _friendsIcon.SetActive(true);
            // Start glow effect for Friends Button
            if (_friendsButtonGlowObject != null)
            {
                CanvasGroup glowCG = _friendsButtonGlowObject.GetComponent<CanvasGroup>();
                if (glowCG != null) StartGlowEffect(glowCG);
            }
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
            // Stop glow effect for Friends Button if active
            if (_friendsButtonGlowObject != null)
            {
                CanvasGroup glowCG = _friendsButtonGlowObject.GetComponent<CanvasGroup>();
                if (glowCG != null && _activeGlowCoroutines.ContainsKey(glowCG))
                {
                    StopCoroutine(_activeGlowCoroutines[glowCG]);
                    _activeGlowCoroutines.Remove(glowCG);
                    glowCG.alpha = 0f;
                    glowCG.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Shows the Inventory Icon and starts its glow effect.
    /// </summary>
    public void ShowInventoryIcon()
    {
        if (_inventoryIcon != null)
        {
            _inventoryIcon.SetActive(true);
            // Start glow effect for Inventory Button
            if (_inventoryButtonGlowObject != null)
            {
                CanvasGroup glowCG = _inventoryButtonGlowObject.GetComponent<CanvasGroup>();
                if (glowCG != null) StartGlowEffect(glowCG);
            }
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
            // Stop glow effect for Inventory Button if active
            if (_inventoryButtonGlowObject != null)
            {
                CanvasGroup glowCG = _inventoryButtonGlowObject.GetComponent<CanvasGroup>();
                if (glowCG != null && _activeGlowCoroutines.ContainsKey(glowCG))
                {
                    StopCoroutine(_activeGlowCoroutines[glowCG]);
                    _activeGlowCoroutines.Remove(glowCG);
                    glowCG.alpha = 0f;
                    glowCG.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Starts a glow effect on the provided CanvasGroup.
    /// Manages stopping previous glow coroutines for the same CanvasGroup.
    /// </summary>
    /// <param name="glowCanvasGroup">The CanvasGroup of the GameObject to glow.</param>
    public void StartGlowEffect(CanvasGroup glowCanvasGroup)
    {
        if (glowCanvasGroup == null)
        {
            Debug.LogWarning("QuestUI: Cannot start glow effect, CanvasGroup is null.");
            return;
        }

        // Stop any existing glow coroutine for this CanvasGroup
        if (_activeGlowCoroutines.ContainsKey(glowCanvasGroup) && _activeGlowCoroutines[glowCanvasGroup] != null)
        {
            StopCoroutine(_activeGlowCoroutines[glowCanvasGroup]);
            _activeGlowCoroutines.Remove(glowCanvasGroup);
        }

        // Start new glow coroutine
        Coroutine newGlow = StartCoroutine(GlowCoroutine(glowCanvasGroup));
        _activeGlowCoroutines[glowCanvasGroup] = newGlow;
    }

    /// <summary>
    /// Coroutine to handle the fading in and out of a CanvasGroup for a glow effect.
    /// </summary>
    /// <param name="canvasGroup">The CanvasGroup to animate.</param>
    private IEnumerator GlowCoroutine(CanvasGroup canvasGroup)
    {
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.interactable = false; // Glow should not block interaction
        canvasGroup.blocksRaycasts = false; // Glow should not block raycasts

        for (int i = 0; i < _glowCycles; i++)
        {
            // Fade In
            float timer = 0f;
            while (timer < _glowFadeDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / _glowFadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 1f; // Ensure fully opaque

            // Fade Out
            timer = 0f;
            while (timer < _glowFadeDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / _glowFadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 0f; // Ensure fully transparent
        }

        canvasGroup.gameObject.SetActive(false); // Deactivate after all cycles
        if (_activeGlowCoroutines.ContainsKey(canvasGroup))
        {
            _activeGlowCoroutines.Remove(canvasGroup);
        }
    }

    /// <summary>
    /// Starts a particle system effect for a specified duration.
    /// Manages stopping previous particle system coroutines for the same particle system.
    /// </summary>
    /// <param name="particleSystem">The ParticleSystem to play.</param>
    /// <param name="duration">How long (in seconds) the particle system should play.</param>
    public void StartParticleSystemEffect(ParticleSystem particleSystem, float duration)
    {
        if (particleSystem == null)
        {
            Debug.LogWarning("QuestUI: Cannot start particle system effect, ParticleSystem is null.");
            return;
        }

        // Stop any existing particle system coroutine for this ParticleSystem
        if (_activeParticleSystemCoroutines.ContainsKey(particleSystem) && _activeParticleSystemCoroutines[particleSystem] != null)
        {
            StopCoroutine(_activeParticleSystemCoroutines[particleSystem]);
            _activeParticleSystemCoroutines.Remove(particleSystem);
        }

        // Start new particle system coroutine
        Coroutine newParticleSystem = StartCoroutine(ParticleSystemCoroutine(particleSystem, duration));
        _activeParticleSystemCoroutines[particleSystem] = newParticleSystem;
    }

    /// <summary>
    /// Coroutine to handle playing and stopping a particle system after a duration.
    /// </summary>
    /// <param name="particleSystem">The ParticleSystem to animate.</param>
    /// <param name="duration">The duration (in seconds) to play the particle system.</param>
    private IEnumerator ParticleSystemCoroutine(ParticleSystem particleSystem, float duration)
    {
        particleSystem.Play();
        Debug.Log($"Playing particle system '{particleSystem.name}' for {duration} seconds.");
        yield return new WaitForSeconds(duration);
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Debug.Log($"Stopped particle system '{particleSystem.name}'.");

        if (_activeParticleSystemCoroutines.ContainsKey(particleSystem))
        {
            _activeParticleSystemCoroutines.Remove(particleSystem);
        }
    }
}
