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

    [Header("Isometric Movement Angle Bias")]
    [Tooltip("An optional angle (in degrees) by which to bias the isometric movement direction each frame. " +
             "Positive values rotate the movement clockwise, negative values rotate counter-clockwise. " +
             "Use this for fine-tuning after the base 1:2 isometric transformation.")]
    [SerializeField]
    private float _isometricAngleBias = 0.0f; // This field remains for fine-tuning

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

    // Stores the current movement input, combined from all sources.
    private Vector2 _currentMovementInput;

    // Boolean to control if player movement is currently enabled (e.g., paused during conversation).
    private bool _isMovementEnabled = true;

    // Animator parameter hashes for efficiency (avoids string comparisons every frame).
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsFacingBackwardHash = Animator.StringToHash("IsFacingBackward");

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes components and sets up input callbacks.
    /// </summary>
    private void Awake()
    {
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
            _currentMovementInput = Vector2.zero;
            _animator.SetBool(IsMovingHash, false); // Stop walking animation
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
            _currentMovementInput = Vector2.zero; // Ensure no lingering input
            _animator.SetBool(IsMovingHash, false); // Ensure idle animation
            return;
        }

        // --- Input Gathering ---
        // Get input from keyboard/gamepad via Input Actions.
        Vector2 inputFromInputActions = _playerInputActions.Player.Move.ReadValue<Vector2>();

        // Get input from the VariableJoystick only if _useJoystickInput is true and joystick reference is set.
        Vector2 inputFromJoystick = Vector2.zero;
        if (_useJoystickInput && _variableJoystick != null)
        {
            inputFromJoystick = _variableJoystick.Direction;
        }

        // Combine inputs: Joystick input takes precedence if it's active AND enabled via _useJoystickInput.
        _currentMovementInput = inputFromJoystick != Vector2.zero ? inputFromJoystick : inputFromInputActions;

        // Set IsMoving animator parameter.
        // If there's any combined input, the player is moving.
        _animator.SetBool(IsMovingHash, _currentMovementInput != Vector2.zero);

        // If no movement input, stop further movement calculations.
        if (_currentMovementInput == Vector2.zero)
        {
            return;
        }

        // Get the raw 2D input vector (which is now combined).
        Vector2 rawInput = _currentMovementInput;

        // Apply isometric transformation for a 1:2 slope.
        float isoX = (rawInput.x * 2) + (rawInput.y * 2);
        float isoY = (rawInput.x * -1) + (rawInput.y * 1);

        // Create the initial isometric movement direction vector.
        Vector3 isometricMoveDirection = new Vector3(isoX, isoY, 0);

        // Apply angle bias if specified.
        if (_isometricAngleBias != 0.0f)
        {
            Quaternion rotation = Quaternion.Euler(0, 0, _isometricAngleBias);
            isometricMoveDirection = rotation * isometricMoveDirection;
        }

        // Normalize the vector for consistent speed.
        isometricMoveDirection.Normalize();

        // Calculate movement amount.
        Vector3 movement = isometricMoveDirection * _moveSpeed * _tileUnitSize * Time.fixedDeltaTime;

        // Apply movement using Rigidbody2D.MovePosition for proper collision.
        _rb.MovePosition(_rb.position + (Vector2)movement);

        // --- Sprite Flipping and Animation Update ---
        UpdateSpriteDirection(isometricMoveDirection);
    }

    /// <summary>
    /// Updates the player's sprite direction (flipX) and animation state
    /// based on the isometric movement direction.
    /// </summary>
    /// <param name="direction">The normalized isometric movement direction.</param>
    private void UpdateSpriteDirection(Vector3 direction)
    {
        // Determine horizontal flip based on isometric X component.
        // If moving left-ish on screen (isometric X is negative), flip the sprite.
        // If moving right-ish on screen (isometric X is positive), don't flip.
        if (direction.x < 0)
        {
            _spriteRenderer.flipX = true; // Flip to face left-front
        }
        else if (direction.x > 0)
        {
            _spriteRenderer.flipX = false; // Face right-front
        }
        // If direction.x is 0 (pure up/down input), maintain last horizontal flip.

        // Determine animation state based on isometric Y component.
        // If moving up-ish on screen (isometric Y is positive), use backward-looking animation.
        // If moving down-ish on screen (isometric Y is negative), use forward-looking animation.
        if (direction.y > 0)
        {
            _animator.SetBool(IsFacingBackwardHash, true); // Player is moving "up" the screen
        }
        else if (direction.y < 0)
        {
            _animator.SetBool(IsFacingBackwardHash, false); // Player is moving "down" the screen
        }
        // If direction.y is 0 (pure left/right input), maintain last vertical animation state.
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
                    // Corrected argument order and type for NPC Name:
                    ConversationManager.Instance.StartConversation(
                        npcTrigger.GetConversationNodes(),
                        npcTrigger.GetNPCName(),        // Pass the NPC name (string)
                        npcTrigger.GetNPCPortrait(),    // Pass the NPC portrait (Sprite)
                        npcTrigger,                     // Pass the NPCConversationTrigger object itself
                        npcTrigger.GetStartNodeIndex()  // Pass the start node index (int)
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
