using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like ToList()

/// <summary>
/// Manages all quests and the "Friends" list in the game.
/// This is a singleton that can be accessed from anywhere.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    /// <summary>
    /// Represents a single quest in the game.
    /// </summary>
    [System.Serializable]
    public class Quest
    {
        public string QuestName;
        public string QuestSummary;
        public string NPCName; // The name of the NPC who gave the quest
        public bool IsCompleted;

        public Quest(string name, string summary, string npcName)
        {
            QuestName = name;
            QuestSummary = summary;
            NPCName = npcName;
            IsCompleted = false; // Quests start as not completed
        }
    }

    [Header("Friend Settings")] // NEW: Header for friend-related settings
    [Tooltip("The total number of unique NPCs that can be befriended in the game.")]
    [SerializeField] private int _totalPossibleFriends = 0; // NEW: Total possible friends

    // List to store all active and completed quests.
    private List<Quest> _activeQuests = new List<Quest>();
    // List to store the names of befriended NPCs.
    private List<string> _friends = new List<string>();

    // NEW: List to keep track of NPCs currently following the player, in order.
    private List<NPCFollower> _activeFollowers = new List<NPCFollower>();

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
    /// Adds a new quest to the active quest list.
    /// </summary>
    /// <param name="questName">The name of the quest.</param>
    /// <param name="questSummary">A brief summary of the quest.</param>
    /// <param name="npcName">The name of the NPC who gave the quest.</param>
    public void AddQuest(string questName, string questSummary, string npcName)
    {
        // Prevent adding duplicate quests by name from the same NPC
        if (_activeQuests.Any(q => q.QuestName == questName && q.NPCName == npcName))
        {
            Debug.LogWarning($"Quest '{questName}' from '{npcName}' already exists. Not adding duplicate.");
            return;
        }

        Quest newQuest = new Quest(questName, questSummary, npcName);
        _activeQuests.Add(newQuest);
        Debug.Log($"Quest Added: {newQuest.QuestName} for {newQuest.NPCName}");

        // Show the Quest Icon when a quest is accepted
        if (QuestUI.Instance != null)
        {
            QuestUI.Instance.ShowQuestIcon();
            // Also update the Joy Meter when a quest is added, in case it affects the total count or initial state
            QuestUI.Instance.UpdateJoyMeterVisualOnly(); // Changed to the new public method
        }

        // NEW: Show notification for quest added
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification($"New Quest: {questName}");
        }
    }

    /// <summary>
    /// Marks a quest as completed.
    /// </summary>
    /// <param name="questName">The name of the quest to complete.</param>
    /// <param name="npcName">The name of the NPC associated with the quest (for uniqueness).</param>
    /// <returns>True if the quest was found and marked complete, false otherwise.</returns>
    public bool CompleteQuest(string questName, string npcName)
    {
        Quest questToComplete = _activeQuests.FirstOrDefault(q => q.QuestName == questName && q.NPCName == npcName);
        if (questToComplete != null)
        {
            if (!questToComplete.IsCompleted)
            {
                questToComplete.IsCompleted = true;
                Debug.Log($"Quest Completed: {questToComplete.QuestName} for {questToComplete.NPCName}");

                // Update the Joy Meter's visual fill immediately upon quest completion
                // The effects will be triggered by NPCConversationTrigger when a friend is added.
                if (QuestUI.Instance != null)
                {
                    QuestUI.Instance.UpdateJoyMeterVisualOnly(); // Changed to the new public method
                }

                // NEW: Show notification for quest completed
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.ShowNotification($"Quest Completed: {questName}");
                }
                return true;
            }
            else
            {
                Debug.LogWarning($"Quest '{questName}' from '{npcName}' is already completed.");
            }
        }
        else
        {
            Debug.LogWarning($"Quest '{questName}' from '{npcName}' not found to complete.");
        }
        return false;
    }

    /// <summary>
    /// Adds an NPC's name to the "Friends" list (original overload).
    /// </summary>
    /// <param name="friendName">The name of the NPC to add as a friend.</param>
    public void AddFriend(string friendName)
    {
        if (!_friends.Contains(friendName))
        {
            // If this is the first friend being added, show the Friends Icon
            if (_friends.Count == 0 && QuestUI.Instance != null)
            {
                QuestUI.Instance.ShowFriendsIcon();
            }
            _friends.Add(friendName);
            Debug.Log($"Added {friendName} to Friends list.");

            // NEW: Show notification for new friend
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification($"New Friend: {friendName}");
            }
        }
        else
        {
            Debug.LogWarning($"{friendName} is already in the Friends list.");
        }
    }

    /// <summary>
    /// Adds an NPC's name to the "Friends" list and optionally sets them to follow in a chain (NEW overload).
    /// </summary>
    /// <param name="friendName">The name of the NPC to add as a friend.</param>
    /// <param name="npcFollower">The NPCFollower component of the friend, if they should follow.</param>
    public void AddFriend(string friendName, NPCFollower npcFollower)
    {
        // Call the original AddFriend method to handle the basic friend list addition
        AddFriend(friendName); // This will now trigger the notification for "New Friend"

        // Now handle the following logic if an NPCFollower component was provided
        if (npcFollower != null && PlayerController.Instance != null)
        {
            // Prevent adding the same follower multiple times to the active followers list
            if (!_activeFollowers.Contains(npcFollower))
            {
                Transform leader = null;
                if (_activeFollowers.Count == 0)
                {
                    // First NPC in the chain follows the player
                    leader = PlayerController.Instance.transform;
                    Debug.Log($"NPC {friendName} (first in chain) is now following Player.");
                }
                else
                {
                    // Subsequent NPCs follow the last NPC in the chain
                    leader = _activeFollowers.Last().transform;
                    Debug.Log($"NPC {friendName} is now following {_activeFollowers.Last().name}.");
                }

                npcFollower.SetLeader(leader); // Set the leader for this NPCFollower
                npcFollower.StartFollowing();  // Start this NPC following
                _activeFollowers.Add(npcFollower); // Add this NPC to the active followers list
            }
            else
            {
                Debug.LogWarning($"NPCFollower for {friendName} is already in the active followers list.");
            }
        }
        else if (npcFollower != null && PlayerController.Instance == null)
        {
            Debug.LogWarning($"QuestManager: Cannot set {friendName} to follow. PlayerController.Instance is null.");
        }
    }

    /// <summary>
    /// Gets a read-only list of all quests (active and completed).
    /// </summary>
    public IReadOnlyList<Quest> GetAllQuests()
    {
        return _activeQuests.AsReadOnly();
    }

    /// <summary>
    /// Gets a read-only list of all befriended NPCs.
    /// </summary>
    public IReadOnlyList<string> GetFriends()
    {
        return _friends.AsReadOnly();
    }

    /// <summary>
    /// Returns a read-only list of NPCFollowers currently active in the following chain.
    /// </summary>
    public IReadOnlyList<NPCFollower> GetActiveFollowers() // NEW: Getter for active followers
    {
        return _activeFollowers.AsReadOnly();
    }

    /// <summary>
    /// Returns the total number of possible friends in the game.
    /// </summary>
    public int GetTotalPossibleFriends() // NEW: Getter for total possible friends
    {
        return _totalPossibleFriends;
    }
}
