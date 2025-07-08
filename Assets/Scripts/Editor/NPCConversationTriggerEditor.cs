using UnityEditor; // Required for Editor and SerializedProperty
using UnityEngine; // Required for MonoBehaviour, etc.
using System.Collections.Generic; // Required for List
using UnityEngine.Events; // Required for UnityEvent (for UnityEvent property field)

// This attribute links this custom editor to the NPCConversationTrigger class.
[CustomEditor(typeof(NPCConversationTrigger))]
public class NPCConversationTriggerEditor : Editor
{
    // SerializedProperty for all fields we want to draw.
    private SerializedProperty _conversationNodesProp;
    private SerializedProperty _npcPortraitSpriteProp;
    private SerializedProperty _npcNameProp; // SerializedProperty for the NPC name string
    private SerializedProperty _initialStartNodeIndexProp;
    private SerializedProperty _questAcceptedNodeIndexProp;
    private SerializedProperty _questCompletedNodeIndexProp;
    private SerializedProperty _questItemRequiredProp;
    private SerializedProperty _onQuestCompletedActionProp;
    private SerializedProperty _hasQuestBeenAcceptedProp;
    private SerializedProperty _hasQuestBeenCompletedProp;

    /// <summary>
    /// Called when the editor becomes enabled (e.g., when the GameObject is selected).
    /// Finds and assigns all serialized properties.
    /// </summary>
    private void OnEnable()
    {
        // Ensure serializedObject is valid before finding properties
        if (serializedObject == null)
        {
            Debug.LogError("NPCConversationTriggerEditor: serializedObject is null in OnEnable.");
            return;
        }

        _conversationNodesProp = serializedObject.FindProperty("_conversationNodes");
        _npcPortraitSpriteProp = serializedObject.FindProperty("_npcPortraitSprite");
        _npcNameProp = serializedObject.FindProperty("_npcName"); // Find the NPC name property
        _initialStartNodeIndexProp = serializedObject.FindProperty("_initialStartNodeIndex");
        _questAcceptedNodeIndexProp = serializedObject.FindProperty("_questAcceptedNodeIndex");
        _questCompletedNodeIndexProp = serializedObject.FindProperty("_questCompletedNodeIndex");
        _questItemRequiredProp = serializedObject.FindProperty("_questItemRequired");
        _onQuestCompletedActionProp = serializedObject.FindProperty("_onQuestCompletedAction");
        _hasQuestBeenAcceptedProp = serializedObject.FindProperty("_hasQuestBeenAccepted");
        _hasQuestBeenCompletedProp = serializedObject.FindProperty("_hasQuestBeenCompleted");

        // Optional: Add debug logs to confirm properties are found
        if (_conversationNodesProp == null) Debug.LogError("NPCConversationTriggerEditor: _conversationNodes property not found!");
        if (_npcNameProp == null) Debug.LogError("NPCConversationTriggerEditor: _npcName property not found!"); // Debug check
        // ... (add similar checks for other properties if needed during debugging)
    }

    /// <summary>
    /// This method draws the Inspector UI for the NPCConversationTrigger.
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Always call this to update the serialized object.
        serializedObject.Update();

        // Draw the NPC Portrait Sprite property.
        EditorGUILayout.PropertyField(_npcPortraitSpriteProp);
        // Draw the NPC Name property.
        EditorGUILayout.PropertyField(_npcNameProp); // Draw the NPC name field

        EditorGUILayout.Space(); // Add some space for readability
        //EditorGUILayout.LabelField("Conversation Start Nodes", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_initialStartNodeIndexProp);
        EditorGUILayout.PropertyField(_questAcceptedNodeIndexProp);
        EditorGUILayout.PropertyField(_questCompletedNodeIndexProp);

        EditorGUILayout.Space();
        //EditorGUILayout.LabelField("Quest Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_questItemRequiredProp);
        EditorGUILayout.PropertyField(_onQuestCompletedActionProp);
        EditorGUILayout.PropertyField(_hasQuestBeenAcceptedProp);
        EditorGUILayout.PropertyField(_hasQuestBeenCompletedProp);

        EditorGUILayout.Space();
        // Check if the _conversationNodesProp is not null before drawing it.
        // This prevents the NullReferenceException if the property isn't found for some reason.
        if (_conversationNodesProp != null)
        {
            EditorGUILayout.PropertyField(_conversationNodesProp, true); // 'true' means draw children recursively
        }
        else
        {
            // Display an error message in the Inspector if the property is missing.
            EditorGUILayout.HelpBox("Conversation Nodes property not found. Ensure '_conversationNodes' field exists in NPCConversationTrigger.cs and is serialized.", MessageType.Error);
        }

        // Apply changes to the serialized object.
        serializedObject.ApplyModifiedProperties();
    }
}
