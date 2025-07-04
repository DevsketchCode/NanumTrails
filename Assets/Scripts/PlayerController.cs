using UnityEngine;

// Removed CharacterController requirement.
// Now requires Rigidbody2D and CapsuleCollider2D for 2D physics interactions.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    // Serialized fields allow you to adjust these values in the Unity Editor.
    [SerializeField] private float speed = 10f; // Movement speed of the player.
    [SerializeField] private float rotationSpeed = 360f; // Speed at which the player rotates to face movement direction.
    [SerializeField] private float _isometricAngleDegrees = 26.565f; // Adjustable isometric angle for movement transformation.

    // New: Settings for snapping player movement to a pixel grid.
    [Header("Pixel Grid Snapping")] // Adds a header in the Inspector for organization.
    [SerializeField] private bool _snapToPixelGrid = false; // Toggle to enable/disable snapping.
    [SerializeField] private float _pixelUnitIncrement = 1.0f; // The size of each pixel increment in world units along the isometric axes.
                                                               // If your tileset is 1 unit = 32 pixels, and you want to snap to 32-pixel increments,
                                                               // then _pixelUnitIncrement should be 1.0f (32 pixels / 32 pixels per unit = 1 unit).
                                                               // Adjust this value to match your game's pixel-to-world unit ratio and desired snap size.


    // Private variables to hold references and input data.
    private InputSystem_Actions _playerInputActions; // Reference to your generated Unity Input System actions.
    private Vector3 _rawInput; // Stores the raw 2D input from the player (e.g., WASD or joystick).
    private Vector3 _isometricMoveDirection; // Stores the input transformed for isometric movement.
    // Changed: Reference to Rigidbody2D instead of CharacterController.
    private Rigidbody2D _rb;
    // New: Reference to CapsuleCollider2D.
    private CapsuleCollider2D _collider;

    // Variables for discrete snapped movement
    private Vector3 _targetGridPosition;
    private bool _isMovingToTarget = false;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to initialize references.
    /// </summary>
    private void Awake()
    {
        _playerInputActions = new InputSystem_Actions(); // Instantiate your input actions.
        // Changed: Get Rigidbody2D and CapsuleCollider2D.
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CapsuleCollider2D>();

        // Important: Configure Rigidbody2D for character movement.
        // Set Body Type to Kinematic if you want to control movement purely through script (setting velocity).
        // Set Body Type to Dynamic if you want physics forces (AddForce) and gravity to affect it.
        // For precise character control, Kinematic is often preferred.
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0; // Disable gravity if not needed for your 2D isometric game.
        _rb.freezeRotation = true; // Prevent physics from rotating the player.
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Used to enable input actions.
    /// </summary>
    private void OnEnable()
    {
        _playerInputActions.Player.Enable(); // Enable the 'Player' action map.
    }

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// Used to disable input actions.
    /// </summary>
    private void OnDisable()
    {
        _playerInputActions.Player.Disable(); // Disable the 'Player' action map.
    }

    /// <summary>
    /// Called once per frame.
    /// This is where the main game logic for movement and rotation happens.
    /// </summary>
    private void Update()
    {
        GatherInput(); // Read player input.

        // Calculate the isometric movement direction using a fixed transformation for the XY plane.
        // This maps 2D screen-space input (X, Y) to 3D world-space (X, Y)
        // to produce diagonal movement consistent with an isometric perspective on the XY plane.
        if (_rawInput == Vector3.zero)
        {
            _isometricMoveDirection = Vector3.zero;
        }
        else
        {
            // Convert the angle from degrees to radians for trigonometric functions.
            float angleRad = _isometricAngleDegrees * Mathf.Deg2Rad;
            float cosAngle = Mathf.Cos(angleRad);
            float sinAngle = Mathf.Sin(angleRad);

            // Apply the isometric transformation for an XY plane based on the angle:
            // This formula transforms cardinal screen inputs into diagonal world movements
            // that align with an isometric grid where the 'up-right' diagonal is at _isometricAngleDegrees
            // from the positive X-axis.
            //
            // Input (1,0) (Right on screen) -> World (cos(angle), -sin(angle)) (Down-Right in typical isometric)
            // Input (0,1) (Up on screen)    -> World (cos(angle), sin(angle)) (Up-Right in typical isometric)
            float isoX = _rawInput.x * cosAngle + _rawInput.y * cosAngle;
            float isoY = _rawInput.x * -sinAngle + _rawInput.y * sinAngle;

            _isometricMoveDirection = new Vector3(isoX, isoY, 0).normalized;
        }

        // Rotate the player character to visually face the direction of isometric movement.
        Look();

        // MovedPlayer() logic is now in FixedUpdate for physics consistency.
    }

    /// <summary>
    /// FixedUpdate is called at a fixed framerate, independent of frame rate.
    /// Used for physics calculations.
    /// </summary>
    private void FixedUpdate()
    {
        MovedPlayer();
    }

    /// <summary>
    /// Reads the raw input from the player's input actions.
    /// </summary>
    private void GatherInput()
    {
        // Read the 2D vector input from the 'Move' action.
        Vector2 input = _playerInputActions.Player.Move.ReadValue<Vector2>();

        // Map the 2D input (X, Y) directly to a 3D vector (X, Y, 0) for movement on the XY plane.
        // The Z-component is set to 0 as movement is constrained to the XY plane.
        _rawInput = new Vector3(input.x, input.y, 0);

        // Debug.Log for monitoring raw input.
        Debug.Log($"Raw Input: {_rawInput}");
    }

    /// <summary>
    /// Rotates the player character to face the calculated isometric movement direction.
    /// </summary>
    private void Look()
    {
        // If there's no isometric movement input, the player should not rotate.
        if (_isometricMoveDirection == Vector3.zero) return;

        // Reverted to 2D sprite rotation logic.
        // This calculates the angle in degrees from the positive X-axis to the movement direction.
        // The -90 adjustment is common if your sprite is drawn facing upwards (along positive Y) by default.
        float angle = Mathf.Atan2(_isometricMoveDirection.y, _isometricMoveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    /// <summary>
    /// Moves the player character using Rigidbody2D.
    /// </summary>
    private void MovedPlayer()
    {
        // --- Snapped movement logic ---
        if (_snapToPixelGrid)
        {
            // If not currently moving to a target and there's new input
            if (!_isMovingToTarget && _rawInput != Vector3.zero)
            {
                // Calculate the next target grid position based on current position and input direction.
                // We move one _pixelUnitIncrement in the calculated isometric direction.
                _targetGridPosition = CalculateSnappedPosition(transform.position + _isometricMoveDirection * _pixelUnitIncrement);
                _isMovingToTarget = true;
            }

            // If currently moving to a target
            if (_isMovingToTarget)
            {
                // Calculate the movement step towards the target for this FixedUpdate frame.
                // Use Rigidbody2D.MovePosition for physics-based movement.
                Vector3 newPosition = Vector3.MoveTowards(_rb.position, _targetGridPosition, speed * Time.fixedDeltaTime);
                _rb.MovePosition(newPosition);

                // Check if we have reached the target position.
                // Using a small epsilon for float comparison to avoid precision issues.
                if (Vector3.Distance(_rb.position, _targetGridPosition) < 0.001f)
                {
                    _rb.position = _targetGridPosition; // Snap exactly to the target to ensure precise alignment.
                    _isMovingToTarget = false; // Movement to this target is complete.
                }
                Debug.Log($"Player moved (snapped): {_rb.position}");
            }
            else // No input and not moving to a target, ensure player is on a grid point.
            {
                SnapCurrentPosition();
            }
        }
        // --- Continuous movement logic (when snapping is off) ---
        else
        {
            if (_isometricMoveDirection == Vector3.zero)
            {
                _rb.linearVelocity = Vector2.zero; // Stop movement if no input.
                return;
            }
            // Use Rigidbody2D.velocity for continuous movement.
            _rb.linearVelocity = _isometricMoveDirection * speed * _rawInput.magnitude;
            Debug.Log($"Player moved (continuous): {_rb.linearVelocity}");
        }
    }

    /// <summary>
    /// Helper method to calculate a snapped world position based on isometric grid.
    /// </summary>
    /// <param name="worldPosition">The world position to snap.</param>
    /// <returns>The snapped world position.</returns>
    private Vector3 CalculateSnappedPosition(Vector3 worldPosition)
    {
        // Convert the isometric angle to radians.
        float angleRad = _isometricAngleDegrees * Mathf.Deg2Rad;
        float cosAngle = Mathf.Cos(angleRad);
        float sinAngle = Mathf.Sin(angleRad);

        // Define the isometric basis vectors in world space.
        // These vectors represent the direction and scale of one unit along the
        // conceptual isometric X and Y axes.
        // basisX: Corresponds to moving 'right' on the screen in isometric terms.
        // basisY: Corresponds to moving 'up' on the screen in isometric terms.
        Vector2 basisX = new Vector2(cosAngle, -sinAngle);
        Vector2 basisY = new Vector2(cosAngle, sinAngle);

        // Calculate the determinant of the transformation matrix formed by basisX and basisY.
        // This is used for the inverse transformation.
        float det = (basisX.x * basisY.y) - (basisX.y * basisY.x);

        // Check for a near-zero determinant to prevent division by zero or extreme values.
        // This can happen if the isometric angle is 0 or 90 degrees, which would not form a valid isometric grid.
        if (Mathf.Abs(det) < 0.0001f)
        {
            Debug.LogError("Isometric angle results in a singular matrix for snapping. Returning original position.");
            return worldPosition; // Fallback: If the matrix is singular, return original position.
        }
        else
        {
            float invDet = 1f / det;

            // Convert the current world position (worldPosition.x, worldPosition.y)
            // into isometric grid coordinates (gridX_float, gridY_float).
            // This is the inverse of the isometric transformation.
            float gridX_float = invDet * (worldPosition.x * basisY.y - worldPosition.y * basisY.x);
            float gridY_float = invDet * (worldPosition.y * basisX.x - worldPosition.x * basisX.y);

            // Snap these float grid coordinates to the nearest multiple of _pixelUnitIncrement.
            float snapped_gridX_val = Mathf.Round(gridX_float / _pixelUnitIncrement) * _pixelUnitIncrement;
            float snapped_gridY_val = Mathf.Round(gridY_float / _pixelUnitIncrement) * _pixelUnitIncrement;

            // Convert the snapped isometric grid coordinates back to world position.
            // This is the forward transformation using the original basis vectors.
            float snappedWorldX = snapped_gridX_val * basisX.x + snapped_gridY_val * basisY.x;
            float snappedWorldY = snapped_gridX_val * basisX.y + snapped_gridY_val * basisY.y;

            return new Vector3(snappedWorldX, snappedWorldY, worldPosition.z);
        }
    }

    /// <summary>
    /// Helper method to snap the player's current position to the nearest isometric grid point.
    /// Called when there is no movement input but snapping is enabled.
    /// </summary>
    private void SnapCurrentPosition()
    {
        Vector3 snappedPos = CalculateSnappedPosition(transform.position);
        // Only set position if it's actually different to avoid unnecessary transform updates and potential jitter.
        if (Vector3.Distance(transform.position, snappedPos) > 0.001f)
        {
            _rb.position = snappedPos; // Use _rb.position for Rigidbody2D.
        }
    }
}
