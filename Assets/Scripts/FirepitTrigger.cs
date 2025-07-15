using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ to find all NPCFollowers
#if UNITY_EDITOR // Only include UnityEditor namespace when in the editor
using UnityEditor;
#endif

/// <summary>
/// Triggers an end-game sequence where all friendly NPCs and the player
/// move to designated spots around a firepit.
/// Attach this script to a 2D Collider (set to Is Trigger) in your player's house.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FirepitTrigger : MonoBehaviour
{
    [Header("Firepit Destinations")]
    [Tooltip("The Transform where the player should move to.")]
    [SerializeField] private Transform _playerFirepitSpot;
    [Tooltip("A list of Transforms representing the designated spots for each friendly NPC. " +
             "Ensure the order corresponds to the order you want NPCs to occupy them, or map them by NPC name.")]
    [SerializeField] private List<Transform> _npcFirepitSpots = new List<Transform>();

    [Tooltip("The speed at which NPCs and the player move to their firepit spots.")]
    [SerializeField] private float _arrivalMoveSpeed = 2.0f; // Note: This speed is not currently used by NPCFollower.GoToDestination.
                                                             // NPCFollower uses its own _moveSpeed. This field is kept for potential future use.
    [Tooltip("The distance threshold for NPCs/player to consider themselves 'arrived' at their spot.")]
    [SerializeField] private float _arrivalThreshold = 0.1f; // Note: This threshold is not currently used by NPCFollower.GoToDestination.
                                                             // NPCFollower uses its own _stopDistanceThreshold. This field is kept for potential future use.

    private bool _eventTriggered = false; // To prevent multiple triggers

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures the collider is set to trigger.
    /// </summary>
    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"FirepitTrigger on {gameObject.name}: Collider2D is not set to 'Is Trigger'. Setting it now.");
            col.isTrigger = true;
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// </summary>
    /// <param name="other">The other Collider2D involved in this collision.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collider entering the trigger is the player and the event hasn't been triggered yet.
        if (other.CompareTag("Player") && !_eventTriggered)
        {
            _eventTriggered = true;
            Debug.Log("Player entered Firepit Trigger. Initiating gathering sequence!");

            // 1. Move the Player to their spot
            if (_playerFirepitSpot != null && PlayerController.Instance != null)
            {
                // Stop player control
                PlayerController.Instance.SetMovementEnabled(false);
                // For simplicity and immediate effect, directly set player position.
                // For smooth movement, PlayerController would need a GoToDestination method.
                PlayerController.Instance.transform.position = _playerFirepitSpot.position;
                Debug.Log("Player moved to firepit spot.");
            }
            else
            {
                Debug.LogWarning("FirepitTrigger: Player Firepit Spot or PlayerController.Instance is null. Player will not move to spot.");
            }

            // 2. Command all friendly NPCs to their spots
            if (QuestManager.Instance != null)
            {
                IReadOnlyList<NPCFollower> activeFollowers = QuestManager.Instance.GetActiveFollowers();

                // Distribute NPCs to spots
                for (int i = 0; i < activeFollowers.Count; i++)
                {
                    if (i < _npcFirepitSpots.Count && _npcFirepitSpots[i] != null)
                    {
                        activeFollowers[i].GoToDestination(_npcFirepitSpots[i].position);
                        Debug.Log($"NPC {activeFollowers[i].name} moving to spot {i}.");
                    }
                    else
                    {
                        Debug.LogWarning($"FirepitTrigger: Not enough NPC Firepit Spots for all active followers, or spot {i} is null. NPC {activeFollowers[i].name} will stop following.");
                        // If no designated spot, just stop them from following
                        activeFollowers[i].StopFollowing();
                    }
                }
            }
            else
            {
                Debug.LogWarning("FirepitTrigger: QuestManager.Instance is null. Cannot command NPCs to firepit spots.");
                // As a fallback, stop all NPCFollowers if QuestManager isn't available
                foreach (NPCFollower follower in FindObjectsOfType<NPCFollower>())
                {
                    follower.StopFollowing();
                }
            }

            // Optionally, disable this trigger after it has been used
            gameObject.SetActive(false);
        }
    }

    // Optional: Draw gizmos in the editor to visualize the firepit spots
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR // Only draw gizmos in the Unity Editor
        if (_playerFirepitSpot != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_playerFirepitSpot.position, 0.3f);
            Handles.Label(_playerFirepitSpot.position + Vector3.up * 0.5f, "Player Spot");
        }

        for (int i = 0; i < _npcFirepitSpots.Count; i++)
        {
            if (_npcFirepitSpots[i] != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_npcFirepitSpots[i].position, 0.3f);
                Handles.Label(_npcFirepitSpots[i].position + Vector3.up * 0.5f, $"NPC Spot {i}");
            }
        }

        // Draw the trigger collider itself
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.isTrigger)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // Orange, semi-transparent
            if (col is BoxCollider2D box)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
                Gizmos.DrawCube(box.offset, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius * transform.localScale.x);
            }
        }
#endif
    }
}
