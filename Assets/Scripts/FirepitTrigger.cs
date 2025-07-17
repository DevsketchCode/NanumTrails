using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ to find all NPCFollowers
using TMPro; // Required for TextMeshProUGUI
using UnityEngine.UI; // Required for CanvasGroup
using Unity.Cinemachine;

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
    [Tooltip("The central Transform representing the firepit itself. Player and NPCs will face towards this point.")]
    [SerializeField] private Transform _firepitCenter; // Reference to the firepit's center

    [Tooltip("The speed at which NPCs and the player move to their firepit spots.")]
    [SerializeField] private float _arrivalMoveSpeed = 2.0f; // Note: This speed is not currently used by NPCFollower.GoToDestination.
                                                             // NPCFollower uses its own _moveSpeed. This field is kept for potential future use.
    [Tooltip("The distance threshold for NPCs/player to consider themselves 'arrived' at their spot.")]
    [SerializeField] private float _arrivalThreshold = 0.1f; // Note: This threshold is not currently used by NPCFollower.GoToDestination.
                                                             // NPCFollower uses its own _stopDistanceThreshold. This field is kept for potential future use.

    [Header("End Game Messages")] // Header for end-game messages
    [Tooltip("Message displayed if the player came alone to the firepit.")]
    [TextArea(2, 4)]
    [SerializeField] private string _aloneMessage = "You came alone to the firepit. The warmth is nice, but it's quiet.";
    [Tooltip("Message displayed if the player came with one or more friends, but not all.")]
    [TextArea(2, 4)]
    [SerializeField] private string _someFriendsMessage = "You gathered with {0} friend(s) around the fire. A cozy evening awaits!";
    [Tooltip("Message displayed if the player brought all possible friends.")]
    [TextArea(2, 4)]
    [SerializeField] private string _allFriendsMessage = "You brought ALL your friends to the firepit! A truly joyful gathering!";
    [Tooltip("Fallback message for unexpected scenarios or if total possible friends is zero.")]
    [TextArea(2, 4)]
    [SerializeField] private string _fallbackMessage = "The fire crackles warmly. The journey has ended.";

    [Header("Custom End Game UI (Optional)")] // Header for optional custom UI
    [Tooltip("Optional: Assign a custom TextMeshProUGUI component for the permanent end-game message. " +
             "If left null, the NotificationManager's default text panel will be used.")]
    [SerializeField] private TextMeshProUGUI _customEndGameTextPanel;
    [Tooltip("Optional: Assign a custom CanvasGroup for the permanent end-game message. " +
             "If left null, the NotificationManager's default canvas group will be used.")]
    [SerializeField] private CanvasGroup _customEndGameCanvasGroup;

    [Header("Camera Control")] // NEW: Header for camera control
    [Tooltip("The Cinemachine Virtual Camera that should follow the firepit.")]
    [SerializeField] private CinemachineCamera _virtualCamera;


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

            // 1. Move the Player to their spot and make them face the firepit
            if (_playerFirepitSpot != null && PlayerController.Instance != null && _firepitCenter != null)
            {
                // Stop player control
                PlayerController.Instance.SetMovementEnabled(false);
                // For simplicity and immediate effect, directly set player position.
                PlayerController.Instance.transform.position = _playerFirepitSpot.position;
                Debug.Log("Player moved to firepit spot.");

                // Make player face the firepit
                Vector2 playerDirectionToFire = _firepitCenter.position - _playerFirepitSpot.position;
                PlayerController.Instance.SetFacingDirection(playerDirectionToFire); // Assumes PlayerController has this method
                Debug.Log($"Player facing direction: {playerDirectionToFire}");
            }
            else
            {
                Debug.LogWarning("FirepitTrigger: Player Firepit Spot, PlayerController.Instance, or Firepit Center is null. Player will not move or face.");
            }

            // 2. Command all friendly NPCs to their spots and make them face the firepit
            if (QuestManager.Instance != null && _firepitCenter != null)
            {
                IReadOnlyList<NPCFollower> activeFollower = QuestManager.Instance.GetActiveFollowers();

                // Distribute NPCs to spots
                for (int i = 0; i < activeFollower.Count; i++)
                {
                    if (i < _npcFirepitSpots.Count && _npcFirepitSpots[i] != null)
                    {
                        activeFollower[i].GoToDestination(_npcFirepitSpots[i].position);
                        Debug.Log($"NPC {activeFollower[i].name} moving to spot {i}.");

                        // Make NPC face the firepit
                        Vector2 npcDirectionToFire = _firepitCenter.position - _npcFirepitSpots[i].position;
                        activeFollower[i].SetFacingDirection(npcDirectionToFire); // Assumes NPCFollower has this method
                        Debug.Log($"NPC {activeFollower[i].name} facing direction: {npcDirectionToFire}");
                    }
                    else
                    {
                        Debug.LogWarning($"FirepitTrigger: Not enough NPC Firepit Spots for all active followers, or spot {i} is null. NPC {activeFollower[i].name} will stop following.");
                        // If no designated spot, just stop them from following
                        activeFollower[i].StopFollowing();
                    }
                }

                // NEW: Change Cinemachine camera target to the firepit
                if (_virtualCamera != null)
                {
                    _virtualCamera.Follow = _firepitCenter;
                    Debug.Log("FirepitTrigger: Cinemachine camera now following Firepit Center.");
                }
                else
                {
                    Debug.LogWarning("FirepitTrigger: Virtual Camera is not assigned! Cannot change camera target.");
                }


                // Display end-game notification based on friends count
                if (NotificationManager.Instance != null)
                {
                    int friendsFound = QuestManager.Instance.GetFriends().Count;
                    int totalPossibleFriends = QuestManager.Instance.GetTotalPossibleFriends();
                    string endGameMessage;

                    if (friendsFound == 0)
                    {
                        endGameMessage = _aloneMessage;
                    }
                    else if (friendsFound > 0 && friendsFound < totalPossibleFriends)
                    {
                        // Use string.Format to insert the number of friends into the message
                        endGameMessage = string.Format(_someFriendsMessage, friendsFound);
                    }
                    else if (totalPossibleFriends > 0 && friendsFound >= totalPossibleFriends) // Ensure totalPossibleFriends is not zero to avoid misleading "all friends"
                    {
                        endGameMessage = _allFriendsMessage;
                    }
                    else // Fallback for unexpected cases or if totalPossibleFriends is 0
                    {
                        endGameMessage = _fallbackMessage;
                    }

                    // Pass custom UI elements if they are assigned, otherwise NotificationManager will use its defaults.
                    NotificationManager.Instance.ShowPermanentNotification(endGameMessage, _customEndGameTextPanel, _customEndGameCanvasGroup);
                }
                else
                {
                    Debug.LogError("FirepitTrigger: NotificationManager.Instance not found! Cannot display end-game message.");
                }
            }
            else
            {
                Debug.LogWarning("FirepitTrigger: QuestManager.Instance or Firepit Center is null. Cannot command NPCs to firepit spots or display end-game message.");
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

        if (_firepitCenter != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_firepitCenter.position, 0.5f);
            Handles.Label(_firepitCenter.position + Vector3.up * 0.7f, "Firepit Center");
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
