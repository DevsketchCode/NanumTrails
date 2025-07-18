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
    private SerializedProperty _questNameProp; // SerializedProperty for quest name
    private SerializedProperty _questSummaryProp; // SerializedProperty for quest summary
    private SerializedProperty _questItemRequiredProp;
    private SerializedProperty _onQuestCompletedActionProp;
    private SerializedProperty _questRewardItemProp; // SerializedProperty for quest reward item
    private SerializedProperty _questRewardQuantityProp; // SerializedProperty for quest reward quantity
    private SerializedProperty _npcFollowerProp; // SerializedProperty for NPCFollower
    private SerializedProperty _shouldFollowPlayerOnQuestCompleteProp; // SerializedProperty for shouldFollowPlayer
    private SerializedProperty _enableConditionalVisibilityProp; // SerializedProperty for enableConditionalVisibility
    private SerializedProperty _itemForVisibilityCheckProp; // SerializedProperty for itemForVisibilityCheck
    private SerializedProperty _targetSpriteRendererProp; // SerializedProperty for targetSpriteRenderer
    private SerializedProperty _targetPolygonColliderProp; // SerializedProperty for targetPolygonCollider
    private SerializedProperty _hasQuestBeenAcceptedProp;
    private SerializedProperty _hasQuestBeenCompletedProp;

    /// <summary>
    /// Called when the editor becomes enabled (e.g., when the GameObject is selected).
    /// Finds and assigns all serialized properties.
    /// </summary>
    private void OnEnable()
    {
        // Add a null check for target object. This prevents the error if the editor
        // tries to draw an inspector for a null or invalid object.
        if (target == null)
        {
            // Debug.LogWarning("NPCConversationTriggerEditor: Target object is null in OnEnable. Skipping property initialization.");
            return;
        }

        // Ensure serializedObject is valid before finding properties
        if (serializedObject == null)
        {
            Debug.LogError("NPCConversationTriggerEditor: serializedObject is null in OnEnable for target " + target.name + ".");
            return;
        }

        _conversationNodesProp = serializedObject.FindProperty("_conversationNodes");
        _npcPortraitSpriteProp = serializedObject.FindProperty("_npcPortraitSprite");
        _npcNameProp = serializedObject.FindProperty("_npcName");
        _initialStartNodeIndexProp = serializedObject.FindProperty("_initialStartNodeIndex");
        _questAcceptedNodeIndexProp = serializedObject.FindProperty("_questAcceptedNodeIndex");
        _questCompletedNodeIndexProp = serializedObject.FindProperty("_questCompletedNodeIndex");
        _questNameProp = serializedObject.FindProperty("_questName");
        _questSummaryProp = serializedObject.FindProperty("_questSummary");
        _questItemRequiredProp = serializedObject.FindProperty("_questItemRequired");
        _onQuestCompletedActionProp = serializedObject.FindProperty("_onQuestCompletedAction");
        _questRewardItemProp = serializedObject.FindProperty("_questRewardItem");
        _questRewardQuantityProp = serializedObject.FindProperty("_questRewardQuantity");
        _npcFollowerProp = serializedObject.FindProperty("_npcFollower");
        _shouldFollowPlayerOnQuestCompleteProp = serializedObject.FindProperty("_shouldFollowPlayerOnQuestComplete");

        // NEW: Ensure these conditional visibility properties are found
        _enableConditionalVisibilityProp = serializedObject.FindProperty("_enableConditionalVisibility");
        _itemForVisibilityCheckProp = serializedObject.FindProperty("_itemForVisibilityCheck");
        _targetSpriteRendererProp = serializedObject.FindProperty("_targetSpriteRenderer");
        _targetPolygonColliderProp = serializedObject.FindProperty("_targetPolygonCollider");

        _hasQuestBeenAcceptedProp = serializedObject.FindProperty("_hasQuestBeenAccepted");
        _hasQuestBeenCompletedProp = serializedObject.FindProperty("_hasQuestBeenCompleted");

        // Optional: Add debug logs to confirm properties are found
        // if (_conversationNodesProp == null) Debug.LogError("NPCConversationTriggerEditor: _conversationNodes property not found!");
        // if (_npcNameProp == null) Debug.LogError("NPCConversationTriggerEditor: _npcName property not found!");
        // if (_questNameProp == null) Debug.LogError("NPCConversationTriggerEditor: _questName property not found!");
        // if (_questSummaryProp == null) Debug.LogError("NPCConversationTriggerEditor: _questSummary property not found!");
        // if (_questRewardItemProp == null) Debug.LogError("NPCConversationTriggerEditor: _questRewardItem property not found!");
        // if (_questRewardQuantityProp == null) Debug.LogError("NPCConversationTriggerEditor: _questRewardQuantity property not found!");
        // if (_npcFollowerProp == null) Debug.LogError("NPCConversationTriggerEditor: _npcFollower property not found!");
        // if (_shouldFollowPlayerOnQuestCompleteProp == null) Debug.LogError("NPCConversationTriggerEditor: _shouldFollowPlayerOnQuestComplete property not found!");
        // if (_enableConditionalVisibilityProp == null) Debug.LogError("NPCConversationTriggerEditor: _enableConditionalVisibility property not found!");
        // if (_itemForVisibilityCheckProp == null) Debug.LogError("NPCConversationTriggerEditor: _itemForVisibilityCheck property not found!");
        // if (_targetSpriteRendererProp == null) Debug.LogError("NPCConversationTriggerEditor: _targetSpriteRenderer property not found!");
        // if (_targetPolygonColliderProp == null) Debug.LogError("NPCConversationTriggerEditor: _targetPolygonCollider property not found!");
    }

    /// <summary>
    /// This method draws the Inspector UI for the NPCConversationTrigger.
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Add a null check for target object at the start of OnInspectorGUI as well.
        if (target == null)
        {
            // If the target is null, there's nothing to draw.
            return;
        }

        // Always call this to update the serialized object.
        serializedObject.Update();

        // Draw properties with null checks for robustness
        if (_npcPortraitSpriteProp != null) EditorGUILayout.PropertyField(_npcPortraitSpriteProp);
        else EditorGUILayout.HelpBox("NPC Portrait Sprite property not found!", MessageType.Error);

        if (_npcNameProp != null) EditorGUILayout.PropertyField(_npcNameProp);
        else EditorGUILayout.HelpBox("NPC Name property not found!", MessageType.Error);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Conversation Start Nodes", EditorStyles.boldLabel);
        if (_initialStartNodeIndexProp != null) EditorGUILayout.PropertyField(_initialStartNodeIndexProp);
        else EditorGUILayout.HelpBox("Initial Start Node Index property not found!", MessageType.Error);
        if (_questAcceptedNodeIndexProp != null) EditorGUILayout.PropertyField(_questAcceptedNodeIndexProp);
        else EditorGUILayout.HelpBox("Quest Accepted Node Index property not found!", MessageType.Error);
        if (_questCompletedNodeIndexProp != null) EditorGUILayout.PropertyField(_questCompletedNodeIndexProp);
        else EditorGUILayout.HelpBox("Quest Completed Node Index property not found!", MessageType.Error);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quest Settings", EditorStyles.boldLabel);
        if (_questNameProp != null) EditorGUILayout.PropertyField(_questNameProp);
        else EditorGUILayout.HelpBox("Quest Name property not found!", MessageType.Error);
        if (_questSummaryProp != null) EditorGUILayout.PropertyField(_questSummaryProp);
        else EditorGUILayout.HelpBox("Quest Summary property not found!", MessageType.Error);
        if (_questItemRequiredProp != null) EditorGUILayout.PropertyField(_questItemRequiredProp);
        else EditorGUILayout.HelpBox("Quest Item Required property not found!", MessageType.Error);
        if (_onQuestCompletedActionProp != null) EditorGUILayout.PropertyField(_onQuestCompletedActionProp);
        else EditorGUILayout.HelpBox("On Quest Completed Action property not found!", MessageType.Error);
        if (_hasQuestBeenAcceptedProp != null) EditorGUILayout.PropertyField(_hasQuestBeenAcceptedProp);
        else EditorGUILayout.HelpBox("Has Quest Been Accepted property not found!", MessageType.Error);
        if (_hasQuestBeenCompletedProp != null) EditorGUILayout.PropertyField(_hasQuestBeenCompletedProp);
        else EditorGUILayout.HelpBox("Has Quest Been Completed property not found!", MessageType.Error);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quest Reward", EditorStyles.boldLabel);
        if (_questRewardItemProp != null) EditorGUILayout.PropertyField(_questRewardItemProp);
        else EditorGUILayout.HelpBox("Quest Reward Item property not found! Ensure '_questRewardItem' is serialized in NPCConversationTrigger.cs", MessageType.Error);
        if (_questRewardQuantityProp != null) EditorGUILayout.PropertyField(_questRewardQuantityProp);
        else EditorGUILayout.HelpBox("Quest Reward Quantity property not found! Ensure '_questRewardQuantity' is serialized in NPCConversationTrigger.cs", MessageType.Error);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Following Settings", EditorStyles.boldLabel);
        if (_npcFollowerProp != null) EditorGUILayout.PropertyField(_npcFollowerProp);
        else EditorGUILayout.HelpBox("NPC Follower property not found!", MessageType.Error);
        if (_shouldFollowPlayerOnQuestCompleteProp != null) EditorGUILayout.PropertyField(_shouldFollowPlayerOnQuestCompleteProp);
        else EditorGUILayout.HelpBox("Should Follow Player On Quest Complete property not found!", MessageType.Error);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Conditional Visibility", EditorStyles.boldLabel);
        if (_enableConditionalVisibilityProp != null)
        {
            EditorGUILayout.PropertyField(_enableConditionalVisibilityProp);
            // Only show item and component fields if conditional visibility is enabled
            if (_enableConditionalVisibilityProp.boolValue)
            {
                EditorGUI.indentLevel++;
                if (_itemForVisibilityCheckProp != null) EditorGUILayout.PropertyField(_itemForVisibilityCheckProp, new GUIContent("Item to Check For"));
                else EditorGUILayout.HelpBox("Item For Visibility Check property not found!", MessageType.Error);
                if (_targetSpriteRendererProp != null) EditorGUILayout.PropertyField(_targetSpriteRendererProp, new GUIContent("Sprite Renderer to Affect"));
                else EditorGUILayout.HelpBox("Target Sprite Renderer property not found!", MessageType.Error);
                if (_targetPolygonColliderProp != null) EditorGUILayout.PropertyField(_targetPolygonColliderProp, new GUIContent("Polygon Collider to Affect"));
                else EditorGUILayout.HelpBox("Target Polygon Collider property not found!", MessageType.Error);
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Enable Conditional Visibility property not found!", MessageType.Error);
        }

        EditorGUILayout.Space();
        // Check if the _conversationNodesProp is not null before drawing it.
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
