using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ to find all NPCFollowers
using TMPro; // Required for TextMeshProUGUI
using UnityEngine.UI; // Required for CanvasGroup and Button
using Unity.Cinemachine; // Required for CinemachineCamera
using System.Collections; // Required for Coroutines

#if UNITY_EDITOR // Only include UnityEditor namespace when in the editor
using UnityEditor;
#endif

/// <summary>
/// Triggers an end-game sequence where all friendly NPCs and the player
/// move to designated spots around a firepit, or allows the player to continue exploring.
/// Attach this script to a 2D Collider (set to Is Trigger) in your player's house.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FirepitTrigger : MonoBehaviour
{
    [Header("Firepit Destinations")]
    [Tooltip("The Transform where the player should move to. This Transform should also have a FirepitSpotAnimationConfig component.")]
    [SerializeField] private Transform _playerFirepitSpot;
    [Tooltip("A list of Transforms representing the designated spots for each friendly NPC. " +
             "Ensure the order corresponds to the order you want NPCs to occupy them, or map them by NPC name. " +
             "Each Transform should also have a FirepitSpotAnimationConfig component.")]
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

    [Header("Camera Control")] // Header for camera control
    [Tooltip("The Cinemachine Virtual Camera that should follow the firepit.")]
    [SerializeField] private CinemachineCamera _virtualCamera;

    [Header("Prompt UI Settings")] // NEW: Header for the "Proceed Home" prompt UI
    [Tooltip("The root GameObject of the prompt UI panel (should have a CanvasGroup).")]
    [SerializeField] private GameObject _promptPanel;
    [Tooltip("The TextMeshProUGUI component for the prompt question.")]
    [SerializeField] private TextMeshProUGUI _promptText;
    [Tooltip("The Button for choosing to proceed home.")]
    [SerializeField] private Button _proceedHomeButton;
    [Tooltip("The Button for choosing to continue exploring.")]
    [SerializeField] private Button _continueExploringButton;

    [Header("Blocking Collider")] // NEW: Header for the optional blocking collider
    [Tooltip("Optional: A Collider2D that blocks the player's path, which will be disabled if the player chooses to proceed home.")]
    [SerializeField] private Collider2D _blockingCollider;

    private bool _eventTriggered = false; // To prevent multiple triggers of the initial prompt

    // NEW: Fields to manage NPC arrival
    private List<NPCFollower> _npcsMovingToFirepit = new List<NPCFollower>();
    private int _expectedNpcsToArrive;
    private int _currentNpcsArrived;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures the collider is set to trigger and sets up prompt button listeners.
    /// </summary>
    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"FirepitTrigger on {gameObject.name}: Collider2D is not set to 'Is Trigger'. Setting it now.");
            col.isTrigger = true;
        }

        // Set up listeners for the prompt buttons
        if (_proceedHomeButton != null)
        {
            _proceedHomeButton.onClick.AddListener(OnProceedHomeButtonClicked);
        }
        else
        {
            Debug.LogWarning("FirepitTrigger: Proceed Home Button is not assigned!");
        }

        if (_continueExploringButton != null)
        {
            _continueExploringButton.onClick.AddListener(OnContinueExploringButtonClicked);
        }
        else
        {
            Debug.LogWarning("FirepitTrigger: Continue Exploring Button is not assigned!");
        }

        // Hide the prompt UI initially
        if (_promptPanel != null)
        {
            _promptPanel.SetActive(false);
            CanvasGroup promptCanvasGroup = _promptPanel.GetComponent<CanvasGroup>();
            if (promptCanvasGroup == null)
            {
                promptCanvasGroup = _promptPanel.AddComponent<CanvasGroup>();
            }
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.interactable = false;
            promptCanvasGroup.blocksRaycasts = false;
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
            Debug.Log("Player entered Firepit Trigger. Displaying home/explore prompt.");

            // Disable player movement while the prompt is active
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetMovementEnabled(false);
            }

            // Show the prompt UI
            ShowPromptUI("Do you want to proceed home or continue exploring?");
        }
    }

    /// <summary>
    /// Displays the prompt UI with a given question.
    /// </summary>
    /// <param name="question">The question to display in the prompt.</param>
    private void ShowPromptUI(string question)
    {
        if (_promptPanel != null && _promptText != null)
        {
            _promptText.text = question;
            _promptPanel.SetActive(true);
            CanvasGroup promptCanvasGroup = _promptPanel.GetComponent<CanvasGroup>();
            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = 1f;
                promptCanvasGroup.interactable = true;
                promptCanvasGroup.blocksRaycasts = true;
            }
        }
        else
        {
            Debug.LogError("FirepitTrigger: Prompt UI elements are not assigned! Cannot show prompt.");
            // As a fallback, proceed home if UI is broken
            StartCoroutine(_ExecuteFirepitSequence()); // Start as coroutine
        }
    }

    /// <summary>
    /// Hides the prompt UI.
    /// </summary>
    private void HidePromptUI()
    {
        if (_promptPanel != null)
        {
            CanvasGroup promptCanvasGroup = _promptPanel.GetComponent<CanvasGroup>();
            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = 0f;
                promptCanvasGroup.interactable = false;
                promptCanvasGroup.blocksRaycasts = false;
            }
            _promptPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Callback for the "Proceed Home" button.
    /// Initiates the firepit gathering sequence and disables the blocking collider.
    /// </summary>
    private void OnProceedHomeButtonClicked()
    {
        Debug.Log("FirepitTrigger: 'Proceed Home' button clicked.");
        HidePromptUI();
        Debug.Log("Player chose to proceed home.");

        // Disable the blocking collider if assigned
        if (_blockingCollider != null)
        {
            _blockingCollider.enabled = false;
            Debug.Log($"FirepitTrigger: Blocking collider '{_blockingCollider.name}' disabled.");
        }
        else
        {
            Debug.LogWarning("FirepitTrigger: Blocking collider is not assigned. No collider to disable.");
        }

        StartCoroutine(_ExecuteFirepitSequence()); // Start the sequence as a coroutine
    }

    /// <summary>
    /// Callback for the "Continue Exploring" button.
    /// Allows the player to continue exploring and re-enables movement.
    /// </summary>
    private void OnContinueExploringButtonClicked()
    {
        Debug.Log("FirepitTrigger: 'Continue Exploring' button clicked.");
        HidePromptUI();
        Debug.Log("Player chose to continue exploring.");

        // Re-enable player movement
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetMovementEnabled(true);
        }

        // Reset _eventTriggered so the player can re-enter the trigger and be prompted again
        _eventTriggered = false;

        // The blocking collider remains enabled by default.
    }

    /// <summary>
    /// Executes the main firepit gathering sequence (player/NPC movement, camera, messages).
    /// This is now a coroutine to allow waiting for NPCs to arrive.
    /// </summary>
    private IEnumerator _ExecuteFirepitSequence()
    {
        // 1. Move the Player to their spot and apply animation config
        if (_playerFirepitSpot != null && PlayerController.Instance != null)
        {
            FirepitSpotAnimationConfig playerSpotConfig = _playerFirepitSpot.GetComponent<FirepitSpotAnimationConfig>();

            // Move player to their spot
            PlayerController.Instance.transform.position = _playerFirepitSpot.position;
            Debug.Log("Player moved to firepit spot.");

            if (playerSpotConfig != null)
            {
                // Set player movement state (IsMoving) and sprite flipX
                PlayerController.Instance.SetMovementEnabled(false); // Disable player input and movement
                // Pass the new PlayerFlipSpriteXAtSpot to SetFirepitAnimationState
                PlayerController.Instance.SetFirepitAnimationState(
                    !playerSpotConfig.IsMovingAtSpot, // IsMoving should be false for idle
                    playerSpotConfig.NpcHorizontalDirectionAtSpot, // Player uses NPC HDir/VDir for idle animation
                    playerSpotConfig.NpcVerticalDirectionAtSpot,
                    playerSpotConfig.PlayerFlipSpriteXAtSpot // NEW: Pass the player's X-flip setting
                );

                Vector2 playerFacingDirection;
                if (playerSpotConfig.PlayerIsFacingBackwardAtSpot)
                {
                    playerFacingDirection = new Vector2(-1, 1).normalized;
                }
                else
                {
                    playerFacingDirection = new Vector2(1, -1).normalized;
                }
                PlayerController.Instance.SetFacingDirection(playerFacingDirection);
                Debug.Log($"Player facing direction: {playerFacingDirection} (from config: IsFacingBackward={playerSpotConfig.PlayerIsFacingBackwardAtSpot})");
            }
            else
            {
                Debug.LogWarning($"FirepitTrigger: Player Firepit Spot '{_playerFirepitSpot.name}' is missing FirepitSpotAnimationConfig. Player animations may not be set correctly.");
                PlayerController.Instance.SetMovementEnabled(false);
                if (_firepitCenter != null)
                {
                    Vector2 playerDirectionToFire = _firepitCenter.position - _playerFirepitSpot.position;
                    PlayerController.Instance.SetFacingDirection(playerDirectionToFire);
                }
            }
        }
        else
        {
            Debug.LogWarning("FirepitTrigger: Player Firepit Spot or PlayerController.Instance is null. Player will not move or face.");
        }

        // 2. Command all friendly NPCs to their spots and prepare for arrival tracking
        if (QuestManager.Instance != null && _firepitCenter != null)
        {
            IReadOnlyList<NPCFollower> activeFollowers = QuestManager.Instance.GetActiveFollowers();
            Debug.Log($"FirepitTrigger: Found {activeFollowers.Count} active followers to move to firepit spots.");

            _npcsMovingToFirepit.Clear(); // Clear list from previous runs
            _currentNpcsArrived = 0;
            _expectedNpcsToArrive = activeFollowers.Count;

            // Distribute NPCs to spots
            for (int i = 0; i < activeFollowers.Count; i++)
            {
                if (i < _npcFirepitSpots.Count && _npcFirepitSpots[i] != null)
                {
                    NPCFollower currentNpc = activeFollowers[i];
                    Transform npcSpotTransform = _npcFirepitSpots[i];
                    FirepitSpotAnimationConfig npcSpotConfig = npcSpotTransform.GetComponent<FirepitSpotAnimationConfig>();

                    Debug.Log($"FirepitTrigger: Commanding NPC {currentNpc.name} to destination {npcSpotTransform.position}.");
                    currentNpc.GoToDestination(npcSpotTransform.position); // Move NPC to spot

                    // Subscribe to the NPC's OnDestinationReached event
                    // Using a local function or a lambda that captures variables for the listener
                    // This listener will be removed when the FirepitTrigger GameObject is deactivated.
                    currentNpc.OnDestinationReached.AddListener(() => OnNpcArrivedAtFirepitSpot(currentNpc, npcSpotConfig));
                    _npcsMovingToFirepit.Add(currentNpc); // Track this NPC as moving
                }
                else
                {
                    Debug.LogWarning($"FirepitTrigger: Not enough NPC Firepit Spots for all active followers, or spot {i} is null. NPC {activeFollowers[i].name} will stop following.");
                    activeFollowers[i].StopFollowing();
                }
            }

            // Wait until all NPCs have arrived
            while (_currentNpcsArrived < _expectedNpcsToArrive)
            {
                yield return null; // Wait for the next frame
            }

            Debug.Log("FirepitTrigger: All NPCs have arrived at their firepit spots.");

            // Now that all NPCs have arrived, finalize the sequence
            FinalizeFirepitSequence();
        }
        else
        {
            Debug.LogWarning("FirepitTrigger: QuestManager.Instance or Firepit Center is null. Cannot command NPCs to firepit spots or display end-game message.");
            // As a fallback, stop all NPCFollowers if QuestManager isn't available
            foreach (NPCFollower follower in FindObjectsOfType<NPCFollower>())
            {
                follower.StopFollowing();
            }
            // If no NPCs, just finalize the sequence immediately
            FinalizeFirepitSequence();
        }
    }

    /// <summary>
    /// Callback method for when an NPCFollower reaches its destination.
    /// </summary>
    /// <param name="npc">The NPCFollower that arrived.</param>
    /// <param name="spotConfig">The FirepitSpotAnimationConfig for this NPC's spot.</param>
    private void OnNpcArrivedAtFirepitSpot(NPCFollower npc, FirepitSpotAnimationConfig spotConfig)
    {
        _currentNpcsArrived++;
        Debug.Log($"NPC {npc.name} arrived at firepit spot. Total arrived: {_currentNpcsArrived}/{_expectedNpcsToArrive}");

        if (spotConfig != null)
        {
            // NEW LOG: Log the values read from FirepitSpotAnimationConfig before passing them
            Debug.Log($"FirepitTrigger: For NPC {npc.name}, spotConfig values: IsMovingAtSpot={spotConfig.IsMovingAtSpot}, HDir={spotConfig.NpcHorizontalDirectionAtSpot}, VDir={spotConfig.NpcVerticalDirectionAtSpot}, FlipX={spotConfig.NpcFlipSpriteXAtSpot}");

            // Apply the final animation state for the firepit spot
            // Pass the new NpcFlipSpriteXAtSpot to SetFirepitAnimationState
            npc.SetFirepitAnimationState(
                spotConfig.IsMovingAtSpot, // IsMoving should be true if IsMovingAtSpot is true, false for idle
                spotConfig.NpcHorizontalDirectionAtSpot,
                spotConfig.NpcVerticalDirectionAtSpot,
                spotConfig.NpcFlipSpriteXAtSpot // NEW: Pass the NPC's X-flip setting
            );
            // Tell the NPCFollower that it is now at the firepit spot, so FixedUpdate stops overriding animations
            npc.SetIsAtFirepitSpot(true);
            Debug.Log($"NPC {npc.name} animations set from config and marked as at firepit spot.");
        }
        else
        {
            Debug.LogWarning($"FirepitTrigger: NPC Firepit Spot for '{npc.name}' is missing FirepitSpotAnimationConfig. NPC animations may not be set correctly after arrival.");
            // Fallback: Ensure NPC is stopped and try to face firepit center
            npc.StopFollowing(); // This sets IsMoving to false
            if (_firepitCenter != null)
            {
                // Corrected the ambiguous operator by casting both operands to Vector2
                Vector2 npcDirectionToFire = (Vector2)_firepitCenter.position - (Vector2)npc.transform.position;
                npc.SetFacingDirection(npcDirectionToFire);
            }
            npc.SetIsAtFirepitSpot(true); // Still mark as at spot to stop movement
        }

        // Remove the listener to prevent multiple calls for the same NPC
        // Note: For lambdas, this requires careful handling. For this scenario where
        // the FirepitTrigger itself will be set inactive, the listeners will be cleaned up.
        // For more complex systems, consider storing the UnityAction and removing it explicitly.
        // For now, we'll rely on the parent GameObject's deactivation.
    }


    /// <summary>
    /// Finalizes the firepit sequence after all NPCs have arrived.
    /// </summary>
    private void FinalizeFirepitSequence()
    {
        // Change Cinemachine camera target to the firepit
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
            else if (friendsFound > 0 && totalPossibleFriends > 0 && friendsFound < totalPossibleFriends)
            {
                endGameMessage = string.Format(_someFriendsMessage, friendsFound);
            }
            else if (totalPossibleFriends > 0 && friendsFound >= totalPossibleFriends)
            {
                endGameMessage = _allFriendsMessage;
            }
            else
            {
                endGameMessage = _fallbackMessage;
            }

            NotificationManager.Instance.ShowPermanentNotification(endGameMessage, _customEndGameTextPanel, _customEndGameCanvasGroup);
        }
        else
        {
            Debug.LogError("FirepitTrigger: NotificationManager.Instance not found! Cannot display end-game message.");
        }

        // Optionally, disable this trigger after it has been used for the home sequence
        gameObject.SetActive(false);
    }

    // Optional: Draw gizmos in the editor to visualize the firepit spots
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR // Only draw gizmos in the Unity Editor
        if (_playerFirepitSpot != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_playerFirepitSpot.position, 0.3f);
            UnityEditor.Handles.Label(_playerFirepitSpot.position + Vector3.up * 0.5f, "Player Spot");
        }

        for (int i = 0; i < _npcFirepitSpots.Count; i++)
        {
            if (_npcFirepitSpots[i] != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_npcFirepitSpots[i].position, 0.3f);
                UnityEditor.Handles.Label(_npcFirepitSpots[i].position + Vector3.up * 0.5f, $"NPC Spot {i}");
            }
        }

        if (_firepitCenter != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_firepitCenter.position, 0.5f);
            UnityEditor.Handles.Label(_firepitCenter.position + Vector3.up * 0.7f, "Firepit Center");
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

        // Draw blocking collider if assigned
        if (_blockingCollider != null && _blockingCollider.enabled)
        {
            Gizmos.color = new Color(0, 0, 1, 0.5f); // Blue, semi-transparent
            if (_blockingCollider is BoxCollider2D box)
            {
                Gizmos.matrix = Matrix4x4.TRS(_blockingCollider.transform.position, _blockingCollider.transform.rotation, _blockingCollider.transform.localScale);
                Gizmos.DrawCube(box.offset, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (_blockingCollider is CircleCollider2D circle)
            {
                Gizmos.DrawSphere(_blockingCollider.transform.position + (Vector3)circle.offset, circle.radius * _blockingCollider.transform.localScale.x);
            }
            UnityEditor.Handles.Label(_blockingCollider.transform.position + Vector3.up * 0.5f, "Blocking Collider");
        }
#endif
    }
}
