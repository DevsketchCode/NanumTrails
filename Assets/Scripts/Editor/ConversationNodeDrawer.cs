using UnityEditor; // Required for PropertyDrawer and EditorGUI
using UnityEngine; // Required for MonoBehaviour, SerializedProperty, etc.

// This attribute links this custom drawer to the ConversationNode class.
[CustomPropertyDrawer(typeof(ConversationManager.ConversationNode))]
public class ConversationNodeDrawer : PropertyDrawer
{
    // This method calculates the height needed to draw the property in the Inspector.
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Get the 'Speaker' property to determine which fields will be visible.
        SerializedProperty speakerProp = property.FindPropertyRelative("Speaker");
        ConversationManager.SpeakerType speaker = (ConversationManager.SpeakerType)speakerProp.enumValueIndex;

        float height = EditorGUIUtility.singleLineHeight; // Start with height for the label and foldout/speaker dropdown

        // Only calculate height for content if the foldout is expanded.
        if (property.isExpanded)
        {
            height += EditorGUIUtility.singleLineHeight; // For Speaker dropdown itself

            // Add height for the Dialogue field if NPC is selected.
            if (speaker == ConversationManager.SpeakerType.NPC)
            {
                // Use GetPropertyHeight to account for TextArea's multi-line height.
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Dialogue"), true);
                height += EditorGUIUtility.singleLineHeight; // For EndConversationAfterNPC checkbox
            }
            // Add height for Player response fields if Player is selected.
            else if (speaker == ConversationManager.SpeakerType.Player)
            {
                // PlayerResponsePositiveText
                height += EditorGUIUtility.singleLineHeight;
                // IsQuestAcceptChoicePositive
                height += EditorGUIUtility.singleLineHeight;
                // IsQuestDeliverChoicePositive
                height += EditorGUIUtility.singleLineHeight;
                // EndConversationOnPositiveChoice
                height += EditorGUIUtility.singleLineHeight;

                // Only add height for NextNodeIndexPositive if not ending conversation and not quest accept/deliver
                bool endOnPositive = property.FindPropertyRelative("EndConversationOnPositiveChoice").boolValue;
                bool isAcceptPositive = property.FindPropertyRelative("IsQuestAcceptChoicePositive").boolValue;
                bool isDeliverPositive = property.FindPropertyRelative("IsQuestDeliverChoicePositive").boolValue;
                if (!endOnPositive && !isAcceptPositive && !isDeliverPositive)
                {
                    height += EditorGUIUtility.singleLineHeight;
                }

                // PlayerResponseAlternateText
                height += EditorGUIUtility.singleLineHeight;
                // IsQuestAcceptChoiceAlternate
                height += EditorGUIUtility.singleLineHeight;
                // IsQuestDeliverChoiceAlternate
                height += EditorGUIUtility.singleLineHeight;
                // EndConversationOnAlternateChoice
                height += EditorGUIUtility.singleLineHeight;

                // Only add height for NextNodeIndexAlternate if not ending conversation and not quest accept/deliver
                bool endOnAlternate = property.FindPropertyRelative("EndConversationOnAlternateChoice").boolValue;
                bool isAcceptAlternate = property.FindPropertyRelative("IsQuestAcceptChoiceAlternate").boolValue;
                bool isDeliverAlternate = property.FindPropertyRelative("IsQuestDeliverChoiceAlternate").boolValue;
                if (!endOnAlternate && !isAcceptAlternate && !isDeliverAlternate)
                {
                    height += EditorGUIUtility.singleLineHeight;
                }
            }
            height += EditorGUIUtility.singleLineHeight * 0.5f; // Small padding at the end of expanded content
        }

        return height;
    }

    // This method draws the property in the Inspector.
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw the label (e.g., "Element 0") and the foldout arrow.
        Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);

        // Only draw the content if the foldout is expanded.
        if (property.isExpanded)
        {
            // Indent the content to align with Unity's default inspector style.
            EditorGUI.indentLevel++;

            // Start drawing content below the foldout label.
            Rect currentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);

            // Draw the Speaker property.
            EditorGUI.PropertyField(currentRect, property.FindPropertyRelative("Speaker"), new GUIContent("Speaker"));
            currentRect.y += EditorGUIUtility.singleLineHeight;

            // Get the 'Speaker' property to determine which fields to draw conditionally.
            SerializedProperty speakerProp = property.FindPropertyRelative("Speaker");
            ConversationManager.SpeakerType speaker = (ConversationManager.SpeakerType)speakerProp.enumValueIndex;

            // Draw fields based on Speaker type.
            if (speaker == ConversationManager.SpeakerType.NPC)
            {
                SerializedProperty dialogueProp = property.FindPropertyRelative("Dialogue");
                float dialogueHeight = EditorGUI.GetPropertyHeight(dialogueProp, true);
                currentRect.height = dialogueHeight;
                EditorGUI.PropertyField(currentRect, dialogueProp, new GUIContent("NPC Dialogue"));
                currentRect.y += dialogueHeight;

                // Draw the 'End Conversation After NPC' checkbox.
                currentRect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(currentRect, property.FindPropertyRelative("EndConversationAfterNPC"), new GUIContent("End Conversation After This"));
                currentRect.y += EditorGUIUtility.singleLineHeight;
            }
            else if (speaker == ConversationManager.SpeakerType.Player)
            {
                // Player Response Positive Text
                EditorGUI.PropertyField(currentRect, property.FindPropertyRelative("PlayerResponsePositiveText"), new GUIContent("Positive Response Text"));
                currentRect.y += EditorGUIUtility.singleLineHeight;

                // Quest Acceptance/Delivery for Positive Choice
                SerializedProperty isAcceptPositiveProp = property.FindPropertyRelative("IsQuestAcceptChoicePositive");
                SerializedProperty isDeliverPositiveProp = property.FindPropertyRelative("IsQuestDeliverChoicePositive");
                SerializedProperty endOnPositiveProp = property.FindPropertyRelative("EndConversationOnPositiveChoice");

                // Ensure only one of accept/deliver/end is checked for a given choice
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(currentRect, isAcceptPositiveProp, new GUIContent("Accept Quest (Positive)"));
                if (EditorGUI.EndChangeCheck() && isAcceptPositiveProp.boolValue)
                {
                    isDeliverPositiveProp.boolValue = false;
                    endOnPositiveProp.boolValue = false;
                }
                currentRect.y += EditorGUIUtility.singleLineHeight;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(currentRect, isDeliverPositiveProp, new GUIContent("Deliver Quest Item (Positive)"));
                if (EditorGUI.EndChangeCheck() && isDeliverPositiveProp.boolValue)
                {
                    isAcceptPositiveProp.boolValue = false;
                    //endOnPositiveProp.boolValue = false;
                }
                currentRect.y += EditorGUIUtility.singleLineHeight;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(currentRect, endOnPositiveProp, new GUIContent("End Conv. on Positive Choice"));
                if (EditorGUI.EndChangeCheck() && endOnPositiveProp.boolValue)
                {
                    isAcceptPositiveProp.boolValue = false;
                    //isDeliverPositiveProp.boolValue = false;
                }
                currentRect.y += EditorGUIUtility.singleLineHeight;

                // Next Node Index Positive (only show if not ending conversation and not quest accept/deliver)
                if (!endOnPositiveProp.boolValue && !isAcceptPositiveProp.boolValue && !isDeliverPositiveProp.boolValue)
                {
                    EditorGUI.PropertyField(currentRect, property.FindPropertyRelative("NextNodeIndexPositive"), new GUIContent("Next Node (Positive)"));
                    currentRect.y += EditorGUIUtility.singleLineHeight;
                }

                // Player Response Alternate Text
                EditorGUI.PropertyField(currentRect, property.FindPropertyRelative("PlayerResponseAlternateText"), new GUIContent("Alternate Response Text"));
                currentRect.y += EditorGUIUtility.singleLineHeight;

                // Quest Acceptance/Delivery for Alternate Choice
                SerializedProperty isAcceptAlternateProp = property.FindPropertyRelative("IsQuestAcceptChoiceAlternate");
                SerializedProperty isDeliverAlternateProp = property.FindPropertyRelative("IsQuestDeliverChoiceAlternate");
                SerializedProperty endOnAlternateProp = property.FindPropertyRelative("EndConversationOnAlternateChoice");

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(currentRect, isAcceptAlternateProp, new GUIContent("Accept Quest (Alternate)"));
                if (EditorGUI.EndChangeCheck() && isAcceptAlternateProp.boolValue)
                {
                    isDeliverAlternateProp.boolValue = false;
                    endOnAlternateProp.boolValue = false;
                }
                currentRect.y += EditorGUIUtility.singleLineHeight;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(currentRect, isDeliverAlternateProp, new GUIContent("Deliver Quest Item (Alternate)"));
                if (EditorGUI.EndChangeCheck() && isDeliverAlternateProp.boolValue)
                {
                    isAcceptAlternateProp.boolValue = false;
                    endOnAlternateProp.boolValue = false;
                }
                currentRect.y += EditorGUIUtility.singleLineHeight;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(currentRect, endOnAlternateProp, new GUIContent("End Conv. on Alternate Choice"));
                if (EditorGUI.EndChangeCheck() && endOnAlternateProp.boolValue)
                {
                    isAcceptAlternateProp.boolValue = false;
                    isDeliverAlternateProp.boolValue = false;
                }
                currentRect.y += EditorGUIUtility.singleLineHeight;

                // Next Node Index Alternate (only show if not ending conversation and not quest accept/deliver)
                if (!endOnAlternateProp.boolValue && !isAcceptAlternateProp.boolValue && !isDeliverAlternateProp.boolValue)
                {
                    EditorGUI.PropertyField(currentRect, property.FindPropertyRelative("NextNodeIndexAlternate"), new GUIContent("Next Node (Alternate)"));
                    currentRect.y += EditorGUIUtility.singleLineHeight;
                }
            }
            EditorGUI.indentLevel--; // Decrease indent after drawing content
        }

        EditorGUI.EndProperty();
    }
}
