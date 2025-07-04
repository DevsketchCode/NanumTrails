using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

/// <summary>
/// Controls player movement on an isometric 2D tilemap using Unity's new Input System.
/// The player moves on the X and Y axis, with input translated to isometric directions.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))] // Ensures a Rigidbody2D component is present on the GameObject
public class PlayerController : MonoBehaviour
{
    // Reference to the generated InputSystem_Actions asset.
    // This asset should be created in your Unity project (e.g., Right-click in Project window -> Create -> Input Actions).
    // Make sure it has a "Player" Action Map and a "Move" Action (Type: Value, Control Type: Vector2).
    // The default keyboard bindings for WASD/Arrows should already be set up for a Vector2.
    private InputSystem_Actions _playerInputActions;

    // Reference to the Rigidbody2D component for physics-based movement.
    private Rigidbody2D _rb;

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

    // Stores the current movement input read from the Input System.
    private Vector2 _currentMovementInput;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the InputSystem_Actions and sets up input callbacks.
    /// </summary>
    private void Awake()
    {
        // Create an instance of the generated InputSystem_Actions.
        // This is the primary place for initialization.
        _playerInputActions = new InputSystem_Actions();

        // Get the Rigidbody2D component attached to this GameObject.
        _rb = GetComponent<Rigidbody2D>();

        // Subscribe to the 'performed' and 'canceled' events of the 'Move' action.
        // When the 'Move' action is performed (e.g., key pressed), OnMovePerformed is called.
        // When the 'Move' action is canceled (e.g., key released), OnMoveCanceled is called.
        _playerInputActions.Player.Move.performed += OnMovePerformed;
        _playerInputActions.Player.Move.canceled += OnMoveCanceled;
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Enables the 'Player' action map so it can receive input.
    /// </summary>
    private void OnEnable()
    {
        // Add a null check to ensure _playerInputActions is initialized.
        // This handles cases where OnEnable might be called before Awake,
        // or if the object was re-enabled in a way that bypassed Awake's full execution.
        if (_playerInputActions == null)
        {
            _playerInputActions = new InputSystem_Actions();
            // Re-subscribe to events if we had to re-initialize.
            // This prevents duplicate subscriptions if Awake already ran successfully.
            _playerInputActions.Player.Move.performed -= OnMovePerformed; // Unsubscribe first to prevent duplicates
            _playerInputActions.Player.Move.canceled -= OnMoveCanceled;   // Unsubscribe first to prevent duplicates
            _playerInputActions.Player.Move.performed += OnMovePerformed;
            _playerInputActions.Player.Move.canceled += OnMoveCanceled;
        }

        _playerInputActions.Player.Enable();
    }

    /// <summary>
    /// Called when the behaviour becomes disabled or inactive.
    /// Disables the 'Player' action map to stop receiving input.
    /// </summary>
    private void OnDisable()
    {
        // Only disable if _playerInputActions is not null to prevent errors on shutdown.
        if (_playerInputActions != null)
        {
            _playerInputActions.Player.Disable();
        }
    }

    /// <summary>
    /// Callback function for when the 'Move' input action is performed.
    /// Reads the current input value (Vector2) from the context.
    /// </summary>
    /// <param name="context">The context of the input action.</param>
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Callback function for when the 'Move' input action is canceled.
    /// Resets the current movement input to zero, stopping player movement.
    /// </summary>
    /// <param name="context">The context of the input action.</param>
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        _currentMovementInput = Vector2.zero;
    }

    /// <summary>
    /// FixedUpdate is called at a fixed framerate, ideal for physics calculations.
    /// Handles the player's movement based on input and applies isometric transformation
    /// using Rigidbody2D.MovePosition for proper collision detection.
    /// </summary>
    private void FixedUpdate()
    {
        // If there's no movement input (e.g., no keys pressed), do nothing this frame.
        if (_currentMovementInput == Vector2.zero)
        {
            return;
        }

        // Get the raw 2D input vector (e.g., (1,0) for right, (0,1) for up).
        Vector2 rawInput = _currentMovementInput;

        // Apply isometric transformation for a 1:2 slope (approx. 26.565 degree isometric projection).
        // This transformation directly maps standard cardinal inputs to the correct world coordinates for your map:
        // - Input (1,0) (Right) maps to (2, -1) in world coordinates (Right and Down, 1:2 slope).
        // - Input (-1,0) (Left) maps to (-2, 1) in world coordinates (Left and Up, 1:2 slope).
        // - Input (0,1) (Up) maps to (2, 1) in world coordinates (Up and Right, 1:2 slope).
        // - Input (0,-1) (Down) maps to (-2, -1) in world coordinates (Down and Left, 1:2 slope).
        float isoX = (rawInput.x * 2) + (rawInput.y * 2);
        float isoY = (rawInput.x * -1) + (rawInput.y * 1);

        // Create the initial isometric movement direction vector.
        Vector3 isometricMoveDirection = new Vector3(isoX, isoY, 0);

        // If there's an angle bias, apply rotation to the isometric movement direction.
        if (_isometricAngleBias != 0.0f)
        {
            // Create a rotation quaternion around the Z-axis (for 2D rotation).
            // Quaternion.Euler takes degrees.
            Quaternion rotation = Quaternion.Euler(0, 0, _isometricAngleBias);
            // Apply the rotation to the isometric movement direction.
            isometricMoveDirection = rotation * isometricMoveDirection;
        }

        // Normalize the vector to ensure consistent movement speed regardless of whether input is cardinal or diagonal,
        // and after applying any angle bias.
        isometricMoveDirection.Normalize();

        // Calculate the actual movement amount for this fixed update frame.
        // _moveSpeed: Determines how fast the player moves in Unity units.
        // _tileUnitSize: Scales the movement based on your tile's size in Unity units (1.0f for 32px=1unit).
        // Time.fixedDeltaTime: Makes movement frame-rate independent and aligns with physics updates.
        Vector3 movement = isometricMoveDirection * _moveSpeed * _tileUnitSize * Time.fixedDeltaTime;

        // Apply the calculated movement to the Rigidbody2D's position.
        // MovePosition respects collisions, preventing the player from passing through colliders.
        _rb.MovePosition(_rb.position + (Vector2)movement);
    }
}
