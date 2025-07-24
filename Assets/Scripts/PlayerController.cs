using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

/// <summary>
/// Controls player movement on an isometric 2D tilemap using Unity's new Input System.
/// The player moves on the X and Y axis, with input translated to isometric directions.
/// Also handles sprite flipping and animation state changes based on movement direction.
/// Integrates with a VariableJoystick for touch input, with an option to disable it.
/// Also handles triggering conversations with NPCs.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))] // Ensures a Rigidbody2D component is present on the GameObject
[RequireComponent(typeof(SpriteRenderer))] // Ensures a SpriteRenderer component is present
[RequireComponent(typeof(Animator))] // Ensures an Animator component is present for animations
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; } // Singleton Instance

    // Reference to the generated InputSystem_Actions asset.
    private InputSystem_Actions _playerInputActions;

    // Reference to the Rigidbody2D component for physics-based movement.
    private Rigidbody2D _rb;

    // Reference to the SpriteRenderer component for sprite manipulation (flipping).
    private SpriteRenderer _spriteRenderer;

    // Reference to the Animator component for controlling animations.
    private Animator _animator;

    [Header("Movement Settings")]
    [Tooltip("Speed of player movement in Unity units per second.")]
    [SerializeField]
    private float _moveSpeed = 5.0f;

    [Tooltip("The size of one tile in Unity units. Set to 1.0f if 32 pixels = 1 Unity unit, as per your setup.")]
    [SerializeField]
    private float _tileUnitSize = 1.0f;

    [Tooltip("An optional angle (in degrees) by which to bias the isometric movement direction each frame. " +
             "Positive values rotate the movement clockwise, negative values rotate counter-clockwise. " +
             "Use this for fine-tuning after the base 1:2 isometric transformation.")]
    [SerializeField]
    private float _isometricAngleBias = 0.0f; // This field remains for fine-tuning

    [Header("Animation Dead Zones")]
    [Tooltip("The minimum absolute value of horizontal input required to register horizontal movement for animations. Values below this will be treated as zero.")]
    [SerializeField]
    private float _horizontalInputDeadZone = 0.01f; // New: Dead zone for horizontal input

    [Tooltip("The minimum absolute value of vertical input required to register vertical movement for animations. Values below this will be treated as zero.")]
    [SerializeField]
    private float _verticalInputDeadZone = 0.01f; // New: Dead zone for vertical input

    [Header("Joystick Integration")]
    [Tooltip("Drag your VariableJoystick UI element here from the scene.")]
    [SerializeField]
    private VariableJoystick _variableJoystick; // Reference to your VariableJoystick

    [Tooltip("If true, input from the Variable Joystick will be used. If false, only keyboard/gamepad input will be used.")]
    [SerializeField]
    private bool _useJoystickInput = true; // New field to enable/disable joystick input

    [Header("Conversation Integration")]
    [Tooltip("Drag the ConversationManager GameObject here from the scene.")]
    [SerializeField]
    private ConversationManager _conversationManager; // Reference to the ConversationManager

    [Header("UI Integration")] // Consolidated UI Header
    [Tooltip("Drag the QuestUI GameObject here from the scene.")]
    [SerializeField]
    private QuestUI _questUI; // Reference to the QuestUI script

    // Stores the current raw movement input, combined from all sources (for animator check).
    private Vector2 _currentRawInput; // Renamed for clarity

    // Boolean to control if player movement is currently enabled (e.g., paused during conversation).
    private bool _isMovementEnabled = true;

    // NEW: Store the last calculated movement velocity for external access (e.g., NPCFollower)
    public Vector2 CurrentMovementVelocity { get; private set; }

    // Animator parameter hashes for efficiency (avoids string comparisons every frame).
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsFacingBackwardHash = Animator.StringToHash("IsFacingBackward");
    private static readonly int HorizontalDirectionHash = Animator.StringToHash("HorizontalDirection"); // New hash for horizontal
    private static readonly int VerticalDirectionHash = Animator.StringToHash("VerticalDirection"); // New hash for vertical

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes components and sets up input callbacks.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); // If you want the player to persist across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Create an instance of the generated InputSystem_Actions.
        _playerInputActions = new InputSystem_Actions();

        // Get required components attached to this GameObject.
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        // Set the player GameObject's tag to "Player" if it's not already.
        // This is used by ConversationManager to find the PlayerController.
        if (gameObject.tag != "Player")
        {
            Debug.LogWarning("PlayerController: Player GameObject does not have 'Player' tag. Setting it now.");
            gameObject.tag = "Player";
        }

        // Subscribe to the "OpenQuestLog", "OpenFriendsList", and "OpenInventory" actions.
        _playerInputActions.Player.OpenQuestLog.performed += OnOpenQuestLog;
        _playerInputActions.Player.OpenFriendsList.performed += OnOpenFriendsList;
        _playerInputActions.Player.OpenInventory.performed += OnOpenInventory;
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Enables the 'Player' action map so it can receive input.
    /// </summary>
    private void OnEnable()
    {
        if (_playerInputActions == null)
        {
            _playerInputActions = new InputSystem_Actions();
        }
        // Ensure input actions are enabled for keyboard/gamepad.
        _playerInputActions.Player.Enable();
    }

    /// <summary>
    /// Called when the behaviour becomes disabled or inactive.
    /// Disables the 'Player' action map to stop receiving input.
    /// </summary>
    private void OnDisable()
    {
        if (_playerInputActions != null)
        {
            _playerInputActions.Player.Disable();
            // Unsubscribe from the "OpenQuestLog", "OpenFriendsList", and "OpenInventory" actions to prevent memory leaks.
            _playerInputActions.Player.OpenQuestLog.performed -= OnOpenQuestLog;
            _playerInputActions.Player.OpenFriendsList.performed -= OnOpenFriendsList;
            _playerInputActions.Player.OpenInventory.performed -= OnOpenInventory;
        }
    }

    /// <summary>
    /// Sets whether player movement is enabled or disabled.
    /// Used by other scripts (e.g., ConversationManager) to pause/resume player control.
    /// </summary>
    /// <param name="enabled">True to enable movement, false to disable.</param>
    public void SetMovementEnabled(bool enabled)
    {
        _isMovementEnabled = enabled;
        if (!enabled)
        {
            // Stop movement immediately if disabled.
            _currentRawInput = Vector2.zero; // Clear input
            _animator.SetBool(IsMovingHash, false); // Stop walking animation
            _rb.linearVelocity = Vector2.zero; // Ensure rigidbody stops (though MovePosition is used)
            CurrentMovementVelocity = Vector2.zero; // NEW: Reset exposed velocity

            // Also reset direction parameters to 0 when movement is disabled
            _animator.SetFloat(HorizontalDirectionHash, 0f);
            _animator.SetFloat(VerticalDirectionHash, 0f);
        }
    }

    /// <summary>
    /// FixedUpdate is called at a fixed framerate, ideal for physics calculations.
    /// Handles the player's movement, sprite flipping, and animation updates.
    /// </summary>
    private void FixedUpdate()
    {
        // Control the active state of the joystick GameObject based on _useJoystickInput.
        if (_variableJoystick != null)
        {
            _variableJoystick.gameObject.SetActive(_useJoystickInput);
        }

        // If movement is disabled, prevent any input processing for movement.
        if (!_isMovementEnabled)
        {
            _currentRawInput = Vector2.zero; // Ensure no lingering input
            _animator.SetBool(IsMovingHash, false); // Ensure idle animation
            _rb.linearVelocity = Vector2.zero; // Ensure player is fully stopped
            CurrentMovementVelocity = Vector2.zero; // NEW: Reset exposed velocity
            // Ensure animator direction parameters are zero when stopped
            _animator.SetFloat(HorizontalDirectionHash, 0f);
            _animator.SetFloat(VerticalDirectionHash, 0f);
            return;
        }

        // --- Input Gathering ---
        Vector2 inputFromInputActions = _playerInputActions.Player.Move.ReadValue<Vector2>();
        Vector2 inputFromJoystick = Vector2.zero;

        if (_useJoystickInput && _variableJoystick != null)
        {
            inputFromJoystick = _variableJoystick.Direction;
        }

        Vector3 finalMoveDirection = Vector3.zero;

        // Prioritize joystick input if it's active and has a non-zero direction
        if (_useJoystickInput && inputFromJoystick.magnitude > 0.01f) // Use a small threshold for joystick
        {
            // Joystick uses standard cardinal movement
            finalMoveDirection = new Vector3(inputFromJoystick.x, inputFromJoystick.y, 0);
            _currentRawInput = inputFromJoystick; // Store for animator check
        }
        else // Fallback to keyboard/gamepad input
        {
            // Keyboard/gamepad uses isometric movement
            Vector2 rawInput = inputFromInputActions;

            float isoX = (rawInput.x * 2) + (rawInput.y * 2);
            float isoY = (rawInput.x * -1) + (rawInput.y * 1);

            finalMoveDirection = new Vector3(isoX, isoY, 0);

            // Apply angle bias if specified.
            if (_isometricAngleBias != 0.0f)
            {
                Quaternion rotation = Quaternion.Euler(0, 0, _isometricAngleBias);
                finalMoveDirection = rotation * finalMoveDirection;
            }
            _currentRawInput = inputFromInputActions; // Store for animator check
        }

        // Determine if player is moving based on combined input magnitude
        bool isMoving = _currentRawInput.magnitude > 0.01f; // Small threshold for general movement
        _animator.SetBool(IsMovingHash, isMoving);

        // If no movement input, stop further movement calculations.
        if (!isMoving) // Use the isMoving bool to stop
        {
            _rb.linearVelocity = Vector2.zero; // Ensure player stops if no input
            CurrentMovementVelocity = Vector2.zero; // NEW: Reset exposed velocity
            // Ensure animator direction parameters are zero when stopped
            _animator.SetFloat(HorizontalDirectionHash, 0f);
            _animator.SetFloat(VerticalDirectionHash, 0f);
            return;
        }

        // Normalize the vector for consistent speed.
        finalMoveDirection.Normalize();

        // Calculate movement amount.
        Vector3 movement = finalMoveDirection * _moveSpeed * _tileUnitSize * Time.fixedDeltaTime;

        // Apply movement using Rigidbody2D.MovePosition for proper collision.
        _rb.MovePosition(_rb.position + (Vector2)movement);

        // NEW: Update the public CurrentMovementVelocity property
        CurrentMovementVelocity = (Vector2)finalMoveDirection * _moveSpeed * _tileUnitSize; // Velocity in units/sec

        // --- Sprite Flipping and Animation Update ---
        // Pass the raw input to UpdateSpriteDirection to apply dead zones for animation parameters
        UpdateSpriteDirection(_currentRawInput);
    }

    /// <summary>
    /// Updates the player's sprite direction (flipX) and animation state
    /// based on the raw input direction, applying dead zones.
    /// </summary>
    /// <param name="rawInput">The raw input vector (from keyboard/gamepad or joystick) before isometric conversion.</param>
    private void UpdateSpriteDirection(Vector2 rawInput)
    {
        // Determine horizontal flip based on the X component of the raw input.
        if (rawInput.x < -_horizontalInputDeadZone)
        {
            _spriteRenderer.flipX = true; // Flip to face left
            _animator.SetFloat(HorizontalDirectionHash, -1f); // Set animator parameter for left
        }
        else if (rawInput.x > _horizontalInputDeadZone)
        {
            _spriteRenderer.flipX = false; // Face right
            _animator.SetFloat(HorizontalDirectionHash, 1f); // Set animator parameter for right
        }
        else
        {
            _animator.SetFloat(HorizontalDirectionHash, 0f); // Input within dead zone, treat as no horizontal movement
        }

        // Determine animation state based on the Y component of the raw input.
        // As per the request: "Walking Forward is moving down in my map, and Backward is moving up."
        // This means a negative Y input (moving down) corresponds to 'forward' animation,
        // and a positive Y input (moving up) corresponds to 'backward' animation.
        if (rawInput.y > _verticalInputDeadZone) // Moving UP the screen
        {
            _animator.SetBool(IsFacingBackwardHash, true); // Player is moving "up" -> should face/animate backward
            _animator.SetFloat(VerticalDirectionHash, 1f); // Positive Y for "backward" direction
        }
        else if (rawInput.y < -_verticalInputDeadZone) // Moving DOWN the screen
        {
            _animator.SetBool(IsFacingBackwardHash, false); // Player is moving "down" -> should face/animate forward
            _animator.SetFloat(VerticalDirectionHash, -1f); // Negative Y for "forward" direction
        }
        else
        {
            _animator.SetFloat(VerticalDirectionHash, 0f); // Input within dead zone, treat as no vertical movement
        }
    }

    /// <summary>
    /// Sets the player's sprite facing direction based on a given direction vector.
    /// This method is called by external scripts (e.g., FirepitTrigger) to force a specific facing.
    /// </summary>
    /// <param name="direction">The direction vector the player should face.</param>
    public void SetFacingDirection(Vector2 direction)
    {
        // Use a small threshold to determine if there's significant direction to update facing
        float threshold = 0.01f;

        // Determine horizontal flip based on the X component of the direction.
        if (direction.x < -threshold)
        {
            _spriteRenderer.flipX = true; // Flip to face left
        }
        else if (direction.x > threshold)
        {
            _spriteRenderer.flipX = false; // Face right
        }
        // If direction.x is within threshold, maintain last horizontal flip.

        // Determine animation state based on the Y component of the direction.
        // This will influence IsFacingBackwardHash.
        // Consistent with "Walking Forward is moving down, Backward is moving up."
        if (direction.y > threshold) // Direction is upwards (positive Y)
        {
            _animator.SetBool(IsFacingBackwardHash, true); // Should face/animate backward
        }
        else if (direction.y < -threshold) // Direction is downwards (negative Y)
        {
            _animator.SetBool(IsFacingBackwardHash, false); // Should face/animate forward
        }
        // If direction.y is within threshold, maintain last vertical animation state.

        Debug.Log($"PlayerController: Forced facing direction to {direction}");
    }

    /// <summary>
    /// Called when the "OpenQuestLog" input action is performed.
    /// Toggles the visibility of the Quest UI.
    /// </summary>
    /// <param name="context">The Input Action callback context.</param>
    private void OnOpenQuestLog(InputAction.CallbackContext context)
    {
        if (_questUI != null)
        {
            _questUI.ToggleQuestLog();
            // Optionally, pause/resume player movement when quest log is opened/closed
            // For now, conversation manager handles pausing, but you might want to extend this.
            // SetMovementEnabled(!_questUI.IsQuestLogOpen());
        }
        else
        {
            Debug.LogWarning("PlayerController: QuestUI reference is not set. Cannot open quest log.");
        }
    }

    /// <summary>
    /// Called when the "OpenFriendsList" input action is performed.
    /// Toggles the visibility of the Friends List UI.
    /// </summary>
    /// <param name="context">The Input Action callback context.</param>
    private void OnOpenFriendsList(InputAction.CallbackContext context)
    {
        if (_questUI != null)
        {
            _questUI.ToggleFriendsList();
            // Optionally, pause/resume player movement when friends list is opened/closed
            // SetMovementEnabled(!_questUI.IsFriendsListOpen());
        }
        else
        {
            Debug.LogWarning("PlayerController: QuestUI reference is not set. Cannot open friends list.");
        }
    }

    /// <summary>
    /// Called when the "OpenInventory" input action is performed.
    /// Toggles the visibility of the Inventory UI.
    /// </summary>
    /// <param name="context">The Input Action callback context.</param>
    private void OnOpenInventory(InputAction.CallbackContext context)
    {
        Debug.Log("OnOpenInventory method called by Input System.");
        if (_questUI != null)
        {
            _questUI.ToggleInventory();
            // Optionally, pause/resume player movement when inventory is opened/closed
            // SetMovementEnabled(!_questUI.IsInventoryOpen());
        }
        else
        {
            Debug.LogWarning("PlayerController: QuestUI reference is not set. Cannot open inventory.");
        }
    }

    /// <summary>
    /// Called when this collider enters a trigger.
    /// Used to detect collision with NPC trigger and start conversation.
    /// </summary>
    /// <param name="other">The other Collider2D involved in this collision.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collided object has the "NPC" tag.
        // You'll need to set the NPC's GameObject tag to "NPC" in the Inspector.
        if (other.CompareTag("NPC"))
        {
            // Attempt to get the NPCConversationTrigger component from the collided NPC.
            NPCConversationTrigger npcTrigger = other.GetComponent<NPCConversationTrigger>();
            if (npcTrigger != null)
            {
                // Ensure ConversationManager instance exists.
                if (ConversationManager.Instance != null)
                {
                    // Start conversation using data from the specific NPC.
                    ConversationManager.Instance.StartConversation(
                        npcTrigger.GetConversationNodes(),
                        npcTrigger.GetNPCName(),
                        npcTrigger.GetNPCPortrait(),
                        npcTrigger,
                        npcTrigger.GetStartNodeIndex()
                    );
                }
                else
                {
                    Debug.LogWarning("PlayerController: ConversationManager.Instance is not found in the scene! Make sure it's set up.");
                }
            }
            else
            {
                Debug.LogWarning("PlayerController: Collided with NPC tagged object, but no NPCConversationTrigger found on it: " + other.gameObject.name);
            }
        }
    }
}
