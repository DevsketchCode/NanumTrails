using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
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
    [SerializeField] private string _npcName = "NPC";

    [Header("Conversation Start Nodes")]
    [Tooltip("The initial node index for this NPC's conversation when first interacted with.")]
    [SerializeField] private int _initialStartNodeIndex = 0;
    [Tooltip("The node index to start the conversation from if the quest has been accepted but not completed.")]
    [SerializeField] private int _questAcceptedNodeIndex = -1;
    [Tooltip("The node index to start the conversation from if the quest has been completed.")]
    [SerializeField] private int _questCompletedNodeIndex = -1;

    [Header("Quest Settings")]
    [Tooltip("The ItemData of the item this NPC requests for a quest.")]
    [SerializeField] private ItemData _questItemRequired;

    [Tooltip("Action to trigger when the quest is successfully completed (item delivered).")]
    [SerializeField] private UnityEvent _onQuestCompletedAction;

    // Quest state variables.
    [Tooltip("Internal: Has the quest from this NPC been accepted by the player?")]
    [SerializeField] private bool _hasQuestBeenAccepted = false;
    [Tooltip("Internal: Has the quest from this NPC been completed by the player?")]
    [SerializeField] private bool _hasQuestBeenCompleted = false;

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
    /// </summary>
    /// <param name="accepted">True if the quest is accepted, false otherwise.</param>
    public void SetQuestAccepted(bool accepted)
    {
        _hasQuestBeenAccepted = accepted;
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
    /// </summary>
    /// <param name="completed">True if the quest is completed, false otherwise.</param>
    public void SetQuestCompleted(bool completed)
    {
        _hasQuestBeenCompleted = completed;
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
