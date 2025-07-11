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

    // List to store all active and completed quests.
    private List<Quest> _activeQuests = new List<Quest>();
    // List to store the names of befriended NPCs.
    private List<string> _friends = new List<string>();

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
            QuestUI.Instance.UpdateJoyMeterDisplay();
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

                // NEW: Update the Joy Meter display immediately upon quest completion
                if (QuestUI.Instance != null)
                {
                    QuestUI.Instance.UpdateJoyMeterDisplay();
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
    /// Adds an NPC's name to the "Friends" list.
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
        }
        else
        {
            Debug.LogWarning($"{friendName} is already in the Friends list.");
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
}
