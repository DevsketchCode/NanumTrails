using UnityEngine;

/// <summary>
/// Configuration for animation parameters at a specific firepit spot.
/// Attach this script to the Transform GameObjects designated as player or NPC firepit spots.
/// This script holds the desired animation state for the character occupying this spot.
/// </summary>
public class FirepitSpotAnimationConfig : MonoBehaviour
{
    [Header("Common Animation Settings (at this spot)")]
    [Tooltip("Should the character's 'IsMoving' parameter be set to false? (Recommended: True for idle at firepit)")]
    public bool IsMovingAtSpot = false; // Default to false for idle

    [Header("Player Specific Settings (if player occupies this spot)")]
    [Tooltip("Should the player's 'IsFacingBackward' parameter be set to false (facing forward/down) or true (facing backward/up)?")]
    public bool PlayerIsFacingBackwardAtSpot = false; // Default to false (facing forward/down)
    [Tooltip("Should the player's sprite be flipped on the X-axis?")]
    public bool PlayerFlipSpriteXAtSpot = false; // NEW: Added for player sprite flip

    [Header("NPC Specific Settings (if NPC occupies this spot)")]
    [Tooltip("The 'HorizontalDirection' float for NPCs. 0 for no horizontal movement, -1 for left, 1 for right.")]
    public float NpcHorizontalDirectionAtSpot = 0f; // Default to 0
    [Tooltip("The 'VerticalDirection' float for NPCs. 0 for no vertical movement, -1 for down/forward, 1 for up/backward.")]
    public float NpcVerticalDirectionAtSpot = 0f; // Default to 0
    [Tooltip("Should the NPC's sprite be flipped on the X-axis?")]
    public bool NpcFlipSpriteXAtSpot = false; // NEW: Added for NPC sprite flip

    // This script is purely for data configuration and does not need Awake or ApplyAnimations methods.
    // The FirepitTrigger script will read these values and apply them to the characters.
}
