using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events; // Required for UnityEvent
// Removed TMPro using, as _npcName is now a string, not a TextMeshProUGUI component.

/// <summary>
/// This script is attached to an NPC GameObject that triggers a conversation.
/// It holds the specific conversation data and portrait for this NPC,
/// as well as quest-related information and state.
/// </summary>
public class NPCConversationTrigger : MonoBehaviour
{
    [Tooltip("The unique conversation nodes for this NPC.")]
    [SerializeField] private List<ConversationManager.ConversationNode> _conversationNodes = new List<ConversationManager.ConversationNode>();

    [Tooltip("The portrait sprite to display for this specific NPC during conversation.")]
    [SerializeField] private Sprite _npcPortraitSprite;
    [Tooltip("The NPC's name to display in the chat box.")]
    [SerializeField] private string _npcName = "NPC"; // Corrected: Changed to string for the NPC's name

    [Header("Conversation Start Nodes")]
    [Tooltip("The initial node index for this NPC's conversation when first interacted with.")]
    [SerializeField] private int _initialStartNodeIndex = 0;
    [Tooltip("The node index to start the conversation from if the quest has been accepted but not completed.")]
    [SerializeField] private int _questAcceptedNodeIndex = -1;
    [Tooltip("The node index to start the conversation from if the quest has been completed.")]
    [SerializeField] private int _questCompletedNodeIndex = -1;

    [Header("Quest Settings")]
    [Tooltip("The name of the quest given by this NPC.")]
    [SerializeField] private string _questName = "New Quest"; // New: Quest name
    [Tooltip("A brief summary of the quest given by this NPC.")]
    [TextArea(3, 5)]
    [SerializeField] private string _questSummary = "Find something for me."; // New: Quest summary

    [Tooltip("The ItemData of the item this NPC requests for a quest.")]
    [SerializeField] private ItemData _questItemRequired;

    [Tooltip("Action to trigger when the quest is successfully completed (item delivered).")]
    [SerializeField] private UnityEvent _onQuestCompletedAction;

    [Header("Following Settings")] // NEW: Header for following
    [Tooltip("Reference to the NPCFollower component on this NPC, if it should follow the player after quest completion.")]
    [SerializeField] private NPCFollower _npcFollower;
    [Tooltip("If true, this NPC will start following the player (or the preceding NPC) when their quest is completed.")]
    [SerializeField] private bool _shouldFollowPlayerOnQuestComplete = false;

    // Quest state variables.
    [Tooltip("Internal: Has the quest from this NPC been accepted by the player?")]
    [SerializeField] private bool _hasQuestBeenAccepted = false;
    [Tooltip("Internal: Has the quest from this NPC been completed by the player?")]
    [SerializeField] private bool _hasQuestBeenCompleted = false;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        // Get the NPCFollower component if it exists on this GameObject
        if (_npcFollower == null)
        {
            _npcFollower = GetComponent<NPCFollower>();
        }
    }

    /// <summary>
    /// Returns the conversation data for this NPC.
    /// </summary>
    public List<ConversationManager.ConversationNode> GetConversationNodes()
    {
        return _conversationNodes;
    }

    /// <summary>
    /// Returns the portrait sprite for this NPC.
    /// </summary>
    public Sprite GetNPCPortrait()
    {
        return _npcPortraitSprite;
    }

    /// <summary>
    /// Returns the name of this NPC.
    /// </summary>
    public string GetNPCName() // Corrected: Changed return type to string
    {
        return _npcName;
    }

    /// <summary>
    /// Returns the appropriate starting node index based on the current quest state.
    /// </summary>
    public int GetStartNodeIndex()
    {
        if (_hasQuestBeenCompleted && _questCompletedNodeIndex != -1)
        {
            return _questCompletedNodeIndex;
        }
        else if (_hasQuestBeenAccepted && _questAcceptedNodeIndex != -1)
        {
            return _questAcceptedNodeIndex;
        }
        return _initialStartNodeIndex;
    }

    /// <summary>
    /// Sets the quest acceptance status for this NPC.
    /// If accepted, adds the quest to the QuestManager.
    /// </summary>
    /// <param name="accepted">True if the quest is accepted, false otherwise.</param>
    public void SetQuestAccepted(bool accepted)
    {
        _hasQuestBeenAccepted = accepted;
        if (accepted && QuestManager.Instance != null)
        {
            QuestManager.Instance.AddQuest(_questName, _questSummary, _npcName);
        }
    }

    /// <summary>
    /// Returns the current quest acceptance status.
    /// </summary>
    public bool HasQuestBeenAccepted()
    {
        return _hasQuestBeenAccepted;
    }

    /// <summary>
    /// Sets the quest completion status for this NPC.
    /// If completed, marks the quest as complete in QuestManager and adds NPC to friends.
    /// </summary>
    /// <param name="completed">True if the quest is completed, false otherwise.</param>
    public void SetQuestCompleted(bool completed)
    {
        _hasQuestBeenCompleted = completed;
        if (completed && QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteQuest(_questName, _npcName);

            // NEW: Pass the NPCFollower reference to QuestManager's AddFriend method
            // QuestManager will now handle setting the leader for the NPCFollower.
            if (_shouldFollowPlayerOnQuestComplete && _npcFollower != null)
            {
                QuestManager.Instance.AddFriend(_npcName, _npcFollower);
            }
            else if (_shouldFollowPlayerOnQuestComplete && _npcFollower == null)
            {
                Debug.LogWarning($"NPC {gameObject.name}: Should follow player, but NPCFollower component is missing!");
                QuestManager.Instance.AddFriend(_npcName); // Still add to friends list even if no follower
            }
            else // If not set to follow, just add to friends list
            {
                QuestManager.Instance.AddFriend(_npcName);
            }
        }
    }

    /// <summary>
    /// Returns the ItemData required for this NPC's quest.
    /// </summary>
    public ItemData GetQuestItemRequired()
    {
        return _questItemRequired;
    }

    /// <summary>
    /// Triggers the custom UnityEvent defined for quest completion.
    /// </summary>
    public void TriggerQuestCompletionAction()
    {
        _onQuestCompletedAction?.Invoke();
    }

    /// <summary>
    /// Returns the NPCFollower component associated with this NPC.
    /// </summary>
    public NPCFollower GetNPCFollower()
    {
        return _npcFollower;
    }

    // Optional: Draw a gizmo in the editor to visualize the trigger area.
    private void OnDrawGizmos()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null && trigger.isTrigger)
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f); // Green, semi-transparent
            if (trigger is BoxCollider2D box)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
                Gizmos.DrawCube(box.offset, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (trigger is CircleCollider2D circle)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius * transform.localScale.x);
            }
            // Add more collider types if needed (e.g., PolygonCollider2D)
        }
    }
}
