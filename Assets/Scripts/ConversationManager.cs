using UnityEngine;
using UnityEngine.UI; // Required for Image and Button
using TMPro; // Required for TextMeshProUGUI
using System.Collections.Generic; // Required for List
using UnityEngine.Events; // Required for UnityEvent

/// <summary>
/// Manages the conversation flow, displays dialogue, handles player choices,
/// and controls the visibility of the chat UI panels.
/// This is a central manager that receives conversation data from NPCs.
/// </summary>
public class ConversationManager : MonoBehaviour
{
    public static ConversationManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The main GameObject for the NPC's chat panel.")]
    [SerializeField] private GameObject _npcChatShadow;
    [Tooltip("The Image component for the NPC's portrait.")]
    [SerializeField] private Image _npcImage;
    [Tooltip("The TextMeshProUGUI component for the NPC's dialogue.")]
    [SerializeField] private TextMeshProUGUI _npcTextMeshPro;
    [Tooltip("The TextMeshProUGUI component for the NPC's name.")]
    [SerializeField] private TextMeshProUGUI _npcNameTextMeshPro; // This is the UI component for the name
    [Tooltip("The Button to continue NPC dialogue.")]
    [SerializeField] private Button _npcContinueButton;

    [Tooltip("The main GameObject for the Player's chat panel.")]
    [SerializeField] private GameObject _playerChatShadow;
    [Tooltip("The Image component for the Player's portrait.")]
    [SerializeField] private Image _playerImage;
    [Tooltip("The Button for the player's positive response option.")]
    [SerializeField] private Button _playerResponsePositiveButton;
    [Tooltip("The TextMeshProUGUI for the player's positive response text.")]
    [SerializeField] private TextMeshProUGUI _playerResponsePositiveText;
    [Tooltip("The Button for the player's alternate/negative response option.")]
    [SerializeField] private Button _playerResponseAlternateButton;
    [Tooltip("The TextMeshProUGUI for the player's alternate/negative response text.")]
    [SerializeField] private TextMeshProUGUI _playerResponseAlternateText;

    [Header("Player Portrait")]
    [Tooltip("The sprite to display for the Player's portrait (this is global for the player).")]
    [SerializeField] private Sprite _playerPortraitSprite;

    // Private fields to hold the currently active conversation and NPC portrait.
    private List<ConversationNode> _activeConversation;
    private Sprite _activeNpcPortrait;
    private string _activeNpcName; // Store the active NPC's name as a string
    private NPCConversationTrigger _activeNpcTrigger; // Reference to the NPC that started this conversation

    // Current index in the active conversation nodes list.
    private int _currentConversationNodeIndex = -1;

    // Reference to the PlayerController to pause/resume movement.
    private PlayerController _playerController;

    /// <summary>
    /// Enum to define who is speaking in a conversation node.
    /// </summary>
    public enum SpeakerType { NPC, Player }

    /// <summary>
    /// Represents a single node in the conversation tree.
    /// This class is now nested here and also used by NPCConversationTrigger.
    /// </summary>
    [System.Serializable]
    public class ConversationNode
    {
        [Tooltip("Who is speaking in this node.")]
        public SpeakerType Speaker;
        [TextArea(3, 10)]
        [Tooltip("The dialogue text for this node. For Player nodes, this is usually empty as text is on buttons.")]
        public string Dialogue;

        // Fields for Player speaker type
        [Tooltip("For Player nodes: The text for the positive response button.")]
        public string PlayerResponsePositiveText;
        [Tooltip("For Player nodes: The text for the alternate/negative response button.")]
        public string PlayerResponseAlternateText;

        [Tooltip("For Player nodes: The index of the next node if the player chooses the positive response.")]
        public int NextNodeIndexPositive = -1;
        [Tooltip("For Player nodes: The index of the next node if the player chooses the alternate response.")]
        public int NextNodeIndexAlternate = -1;

        // New: Option to end conversation directly from a player choice
        [Tooltip("If true, choosing the positive response will end the conversation.")]
        public bool EndConversationOnPositiveChoice = false;
        [Tooltip("If true, choosing the alternate response will end the conversation.")]
        public bool EndConversationOnAlternateChoice = false;

        // New: Option to end conversation after an NPC dialogue
        [Tooltip("If true, the conversation will end after this NPC dialogue.")]
        public bool EndConversationAfterNPC = false;

        // New: Quest-related flags for Player choices
        [Tooltip("If true, choosing the positive response will mark the quest as accepted for the current NPC.")]
        public bool IsQuestAcceptChoicePositive = false;
        [Tooltip("If true, choosing the alternate response will mark the quest as accepted for the current NPC.")]
        public bool IsQuestAcceptChoiceAlternate = false;

        [Tooltip("If true, choosing the positive response will attempt to deliver the requested item to the current NPC.")]
        public bool IsQuestDeliverChoicePositive = false;
        [Tooltip("If true, choosing the alternate response will attempt to deliver the requested item to the current NPC.")]
        public bool IsQuestDeliverChoiceAlternate = false;
    }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes singleton, UI states, and button listeners.
    /// </summary>
    private void Awake()
    {
        // Implement singleton pattern.
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); // If you want the manager to persist across scenes.
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances.
            return;
        }

        // Ensure UI panels are initially hidden.
        _npcChatShadow.SetActive(false);
        _playerChatShadow.SetActive(false);

        // Add listeners to buttons.
        _npcContinueButton.onClick.AddListener(OnNPCContinueButtonClicked);
        _playerResponsePositiveButton.onClick.AddListener(() => OnPlayerResponseButtonClicked(true));
        _playerResponseAlternateButton.onClick.AddListener(() => OnPlayerResponseButtonClicked(false));

        // Find the PlayerController in the scene.
        // Assumes PlayerController is on a GameObject with the tag "Player".
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            _playerController = playerObject.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogError("ConversationManager: Could not find Player GameObject with tag 'Player'. Make sure your player has this tag!");
        }
    }

    /// <summary>
    /// Starts a new conversation with the provided data and NPC portrait.
    /// </summary>
    /// <param name="conversationData">The list of conversation nodes for this specific NPC.</param>
    /// <param name="npcName">The name of the NPC speaking (as a string).</param>
    /// <param name="npcPortrait">The portrait sprite for the NPC speaking.</param>
    /// <param name="npcTrigger">The NPCConversationTrigger that initiated this conversation.</param>
    /// <param name="startNodeIndex">The index of the first conversation node to display (defaults to 0).</param>
    public void StartConversation(List<ConversationNode> conversationData, string npcName, Sprite npcPortrait, NPCConversationTrigger npcTrigger, int startNodeIndex = 0)
    {
        if (conversationData == null || conversationData.Count == 0)
        {
            Debug.LogWarning("ConversationManager: No conversation nodes provided to start.");
            return;
        }

        _activeConversation = conversationData;
        _activeNpcName = npcName; // Store the NPC name (string)
        _activeNpcPortrait = npcPortrait;
        _activeNpcTrigger = npcTrigger; // Store reference to the active NPC trigger

        // Pause player movement during conversation.
        if (_playerController != null)
        {
            _playerController.SetMovementEnabled(false);
        }

        _currentConversationNodeIndex = startNodeIndex;
        DisplayCurrentNode();
    }

    /// <summary>
    /// Displays the current conversation node based on _currentConversationNodeIndex.
    /// </summary>
    private void DisplayCurrentNode()
    {
        // If index is out of bounds or no active conversation, end conversation.
        if (_activeConversation == null || _currentConversationNodeIndex < 0 || _currentConversationNodeIndex >= _activeConversation.Count)
        {
            EndConversation();
            return;
        }

        ConversationNode currentNode = _activeConversation[_currentConversationNodeIndex];

        // Hide both panels initially.
        _npcChatShadow.SetActive(false);
        _playerChatShadow.SetActive(false);

        if (currentNode.Speaker == SpeakerType.NPC)
        {
            _npcChatShadow.SetActive(true);
            _npcImage.sprite = _activeNpcPortrait; // Use the active NPC portrait
            _npcTextMeshPro.text = currentNode.Dialogue;
            if (_npcNameTextMeshPro != null) // Set NPC name on the UI TextMeshPro component
            {
                _npcNameTextMeshPro.text = _activeNpcName; // Use the stored string name
            }
            _npcContinueButton.gameObject.SetActive(true); // Always show continue button for NPC dialogue
        }
        else if (currentNode.Speaker == SpeakerType.Player)
        {
            _playerChatShadow.SetActive(true);
            _playerImage.sprite = _playerPortraitSprite; // Use the global player portrait

            // Set positive response button text and visibility
            if (!string.IsNullOrEmpty(currentNode.PlayerResponsePositiveText))
            {
                _playerResponsePositiveText.text = currentNode.PlayerResponsePositiveText;
                _playerResponsePositiveButton.gameObject.SetActive(true);
            }
            else
            {
                _playerResponsePositiveText.text = ""; // Clear text if button is hidden
                _playerResponsePositiveButton.gameObject.SetActive(false);
            }

            // Set alternate response button text and visibility
            if (!string.IsNullOrEmpty(currentNode.PlayerResponseAlternateText))
            {
                _playerResponseAlternateText.text = currentNode.PlayerResponseAlternateText;
                _playerResponseAlternateButton.gameObject.SetActive(true);
            }
            else
            {
                _playerResponseAlternateText.text = ""; // Clear text if button is hidden
                _playerResponseAlternateButton.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Called when the NPC's continue button is clicked.
    /// Advances the conversation to the next sequential node or ends it.
    /// </summary>
    private void OnNPCContinueButtonClicked()
    {
        // Check if the current node is valid and is an NPC node.
        if (_activeConversation == null || _currentConversationNodeIndex < 0 || _currentConversationNodeIndex >= _activeConversation.Count)
        {
            EndConversation();
            return;
        }

        ConversationNode currentNode = _activeConversation[_currentConversationNodeIndex];

        if (currentNode.Speaker == SpeakerType.NPC)
        {
            if (currentNode.EndConversationAfterNPC)
            {
                EndConversation();
            }
            else
            {
                _currentConversationNodeIndex++; // Move to the next node in the array
                DisplayCurrentNode();
            }
        }
        else
        {
            Debug.LogWarning("ConversationManager: OnNPCContinueButtonClicked called when current node is not an NPC node.");
        }
    }

    /// <summary>
    /// Called when a player response button is clicked.
    /// Determines the next conversation node based on the chosen response or ends the conversation.
    /// Handles quest acceptance and delivery.
    /// </summary>
    /// <param name="isPositive">True if the positive response was chosen, false for alternate.</param>
    private void OnPlayerResponseButtonClicked(bool isPositive)
    {
        // This method is called when a player choice is made.
        // The current node MUST be a Player node.
        if (_activeConversation == null || _currentConversationNodeIndex < 0 || _currentConversationNodeIndex >= _activeConversation.Count)
        {
            Debug.LogWarning("ConversationManager: OnPlayerResponseButtonClicked called with invalid active conversation state.");
            EndConversation();
            return;
        }

        ConversationNode currentNode = _activeConversation[_currentConversationNodeIndex];

        if (currentNode.Speaker == SpeakerType.Player)
        {
            // Handle Quest Acceptance
            if (isPositive && currentNode.IsQuestAcceptChoicePositive)
            {
                _activeNpcTrigger?.SetQuestAccepted(true);
                Debug.Log($"Quest accepted from {_activeNpcTrigger?.gameObject.name}");
            }
            else if (!isPositive && currentNode.IsQuestAcceptChoiceAlternate)
            {
                _activeNpcTrigger?.SetQuestAccepted(true);
                Debug.Log($"Quest accepted from {_activeNpcTrigger?.gameObject.name}");
            }

            // Handle Quest Delivery
            if (isPositive && currentNode.IsQuestDeliverChoicePositive)
            {
                AttemptQuestDelivery();
            }
            else if (!isPositive && currentNode.IsQuestDeliverChoiceAlternate)
            {
                AttemptQuestDelivery();
            }

            // Determine next node or end conversation
            if (isPositive)
            {
                if (currentNode.EndConversationOnPositiveChoice)
                {
                    EndConversation();
                }
                else
                {
                    _currentConversationNodeIndex = currentNode.NextNodeIndexPositive;
                    DisplayCurrentNode();
                }
            }
            else // Alternate choice
            {
                if (currentNode.EndConversationOnAlternateChoice)
                {
                    EndConversation();
                }
                else
                {
                    _currentConversationNodeIndex = currentNode.NextNodeIndexAlternate;
                    DisplayCurrentNode();
                }
            }
        }
        else
        {
            Debug.LogWarning("ConversationManager: OnPlayerResponseButtonClicked called when current node is not a Player node.");
        }
    }

    /// <summary>
    /// Attempts to deliver the requested quest item to the active NPC.
    /// </summary>
    private void AttemptQuestDelivery()
    {
        if (_activeNpcTrigger == null || _activeNpcTrigger.GetQuestItemRequired() == null)
        {
            Debug.LogWarning("ConversationManager: No active NPC or no quest item required for delivery.");
            return;
        }

        ItemData requiredItem = _activeNpcTrigger.GetQuestItemRequired();

        // Check if the player has the item and the quest has been accepted
        if (_activeNpcTrigger.HasQuestBeenAccepted() && InventoryManager.Instance != null && (InventoryManager.Instance.HasItem(requiredItem) || requiredItem == null))
        {

            if (requiredItem != null)
            {
                InventoryManager.Instance.RemoveItem(requiredItem);
            }
            else
            {
                Debug.LogWarning($"Failed to remove '{requiredItem.ItemName}' from inventory during delivery attempt.");
                // Optionally, display a message to the player that removal failed.
            }

            _activeNpcTrigger.SetQuestCompleted(true); // This will now also add to friends via NPCConversationTrigger
            Debug.Log($"Quest item '{requiredItem.ItemName}' delivered to {_activeNpcTrigger.gameObject.name}. Quest completed!");
            _activeNpcTrigger.TriggerQuestCompletionAction(); // Trigger the custom action

            // NEW: Give quest reward if specified by the NPCConversationTrigger
            _activeNpcTrigger?.GiveQuestReward(); // Call the new method on the NPCConversationTrigger

            EndConversation(); // End conversation after successful delivery
        }
        else
        {
            if (!_activeNpcTrigger.HasQuestBeenAccepted())
            {
                Debug.Log("Quest not yet accepted, cannot deliver item.");
            }
            else
            {
                Debug.Log("Player does not have the required item: " + requiredItem.ItemName);
            }
            // Optionally, display a message to the player that they don't have the item or quest not accepted.
            // For now, the conversation will just continue to the next node based on the player's choice.
        }
    }

    /// <summary>
    /// Ends the current conversation and hides all UI panels.
    /// </summary>
    private void EndConversation()
    {
        _npcChatShadow.SetActive(false);
        _playerChatShadow.SetActive(false);
        _currentConversationNodeIndex = -1; // Reset index
        _activeConversation = null; // Clear active conversation data
        _activeNpcPortrait = null; // Clear active NPC portrait
        _activeNpcName = null; // Clear active NPC name
        _activeNpcTrigger = null; // Clear active NPC trigger reference

        // Resume player movement.
        if (_playerController != null)
        {
            _playerController.SetMovementEnabled(true);
        }
        Debug.Log("Conversation Ended.");
    }
}
