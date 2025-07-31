using UnityEngine;
using System.Collections.Generic; // For Queue
using System.Linq; // For LINQ operations
using System; // Required for Action
using UnityEngine.Events; // For UnityEvent

/// <summary>
/// Controls an NPC's ability to follow a target (typically the player) with a delay,
/// maintaining a set distance, and stopping "behind" the player based on last movement.
/// Also handles moving the NPC to a specific destination.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class NPCFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("The Transform of the object this NPC should follow (e.g., the Player or another NPC).")]
    [SerializeField] private Transform _leaderTransform;
    [Tooltip("The desired distance to maintain behind the leader.")]
    [SerializeField] private float _followDistance = 1.5f;
    [Tooltip("The speed at which the NPC moves while following or moving to a destination.")]
    [SerializeField] private float _moveSpeed = 3.0f;
    [Tooltip("How smoothly the NPC catches up to its leader position. Higher values mean faster, snappier following.")]
    [SerializeField] private float _lerpSmoothness = 5.0f;
    [Tooltip("The threshold distance for the NPC to consider itself 'stopped' at the target follow position.")]
    [SerializeField] private float _stopDistanceThreshold = 0.2f; // Increased from 0.1f for more forgiving arrival
    [Tooltip("The minimum distance the NPC will maintain from its leader (player) when following, to prevent pushing.")]
    [SerializeField] private float _playerProximityStopDistance = 0.5f;

    [Header("Delay Settings")]
    [Tooltip("The number of FixedUpdate frames the NPC's movement should lag behind its leader. Higher values mean more delay.")]
    [SerializeField] private int _followDelayFrames = 10;

    [Header("Animation Dead Zones")]
    [Tooltip("The minimum absolute value of horizontal velocity required to register horizontal movement for animations. Values below this will be treated as zero.")]
    [SerializeField]
    private float _horizontalVelocityDeadZone = 0.01f; // New: Dead zone for horizontal velocity

    [Tooltip("The minimum absolute value of vertical velocity required to register vertical movement for animations. Values below this will be treated as zero.")]
    [SerializeField]
    private float _verticalVelocityDeadZone = 0.01f; // New: Dead zone for vertical velocity

    private Rigidbody2D _rb;
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private CapsuleCollider2D _triggerCollider; // Reference to the CapsuleCollider2D that acts as a trigger

    private bool _isFollowing = false;
    private bool _isMovingToDestination = false;
    private Vector2 _currentDestination;

    // Public getter for _isMovingToDestination, allowing external scripts to check if NPC is still moving to a spot.
    public bool IsMovingToDestinationPublic => _isMovingToDestination;

    // Flag to indicate if the NPC is at a firepit spot, preventing FixedUpdate from overriding animations.
    private bool _isAtFirepitSpot = false;

    // Store a history of the leader's positions and velocities to implement the delay
    private Queue<Vector2> _leaderPositionHistory = new Queue<Vector2>();
    private Queue<Vector2> _leaderVelocityHistory = new Queue<Vector2>(); // To store historical velocities

    private Vector2 _targetPosition; // The calculated position this NPC is trying to reach

    // Stores the last significant movement direction for idle facing
    private Vector2 _lastNonZeroMovementDirection = Vector2.down; // Default to facing down/forward

    // Animator parameter hashes for efficiency
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsFacingBackwardHash = Animator.StringToHash("IsFacingBackward");
    private static readonly int HorizontalDirectionHash = Animator.StringToHash("HorizontalDirection"); // New hash for horizontal
    private static readonly int VerticalDirectionHash = Animator.StringToHash("VerticalDirection"); // New hash for vertical

    // NEW: Event to notify when the NPC has reached its destination
    public UnityEvent OnDestinationReached;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Gets component references and initializes the position history.
    /// </summary>
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _triggerCollider = GetComponent<CapsuleCollider2D>(); // Get the CapsuleCollider2D component

        // Ensure Rigidbody2D is configured for 2D movement and not affected by gravity
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;

        // Ensure the collider is set as a trigger.
        if (_triggerCollider != null && !_triggerCollider.isTrigger)
        {
            Debug.LogWarning($"NPCFollower on {gameObject.name}: CapsuleCollider2D is not set to 'Is Trigger'. Setting it now.");
            _triggerCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// Sets the Transform of the leader this NPC should follow.
    /// </summary>
    /// <param name="leader">The Transform of the player or another NPC to follow.</param>
    public void SetLeader(Transform leader)
    {
        _leaderTransform = leader;
        // Initialize history with current leader position and zero velocity to prevent sudden jumps
        _leaderPositionHistory.Clear();
        _leaderVelocityHistory.Clear(); // Clear velocity history too
        if (_leaderTransform != null)
        {
            // Fill history with current position and zero velocity initially
            for (int i = 0; i < _followDelayFrames; i++)
            {
                _leaderPositionHistory.Enqueue(_leaderTransform.position);
                _leaderVelocityHistory.Enqueue(Vector2.zero); // Start with zero velocity
            }
            Debug.Log($"NPC {gameObject.name}: Leader set to {_leaderTransform.name}. History initialized with {_leaderPositionHistory.Count} entries.");
        }
    }

    /// <summary>
    /// Starts the NPC following its assigned leader.
    /// </summary>
    public void StartFollowing()
    {
        if (_leaderTransform == null)
        {
            Debug.LogWarning($"NPCFollower on {gameObject.name}: Cannot start following, no leader assigned!");
            return;
        }
        _isFollowing = true;
        _isMovingToDestination = false; // Ensure we're not in destination mode
        _isAtFirepitSpot = false; // Not at firepit spot when starting to follow
        Debug.Log($"NPC {gameObject.name} started following {_leaderTransform.name}. _isFollowing set to TRUE.");

        // Immediately set initial sprite direction towards the leader
        if (_leaderTransform != null)
        {
            // First, ensure the NPC's local X scale is positive (facing right by default)
            // if it was initially negative (flipped in editor).
            if (transform.localScale.x < 0)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }

            // Then, apply sprite flipping based on leader's initial relative position
            // This ensures the sprite is correctly oriented towards the leader at the start.
            Vector2 directionToLeader = _leaderTransform.position - transform.position;
            // UpdateSpriteDirection will now handle the animator parameters based on this initial direction
            // Note: This call is for initial visual setup, FixedUpdate will manage during movement.
            UpdateSpriteDirection(directionToLeader);
            // Also update the last non-zero movement direction for idle state
            if (directionToLeader.magnitude > 0.001f)
            {
                _lastNonZeroMovementDirection = directionToLeader.normalized;
            }
        }

        // Disable the CapsuleCollider2D when following starts to prevent re-triggering conversation
        if (_triggerCollider != null)
        {
            _triggerCollider.enabled = false;
            Debug.Log($"NPC {gameObject.name}: CapsuleCollider2D disabled to prevent re-triggering conversation.");
        }
    }

    /// <summary>
    /// Stops the NPC from following the leader.
    /// </summary>
    public void StopFollowing()
    {
        _isFollowing = false;
        _rb.linearVelocity = Vector2.zero; // Stop any residual movement
        _animator.SetBool(IsMovingHash, false); // Set to idle animation

        // When stopping, explicitly set direction floats based on last non-zero direction for facing.
        // This ensures the Animator can pick the correct directional idle.
        float hDirValue = 0f;
        if (_lastNonZeroMovementDirection.x < -_horizontalVelocityDeadZone)
        {
            hDirValue = -1f;
        }
        else if (_lastNonZeroMovementDirection.x > _horizontalVelocityDeadZone)
        {
            hDirValue = 1f;
        }

        float vDirValue = 0f;
        if (_lastNonZeroMovementDirection.y < -_verticalVelocityDeadZone)
        {
            vDirValue = -1f; // Down/Forward
        }
        else if (_lastNonZeroMovementDirection.y > _verticalVelocityDeadZone)
        {
            vDirValue = 1f; // Up/Backward
        }

        _animator.SetFloat(HorizontalDirectionHash, hDirValue);
        _animator.SetFloat(VerticalDirectionHash, vDirValue);
        UpdateSpriteDirection(_lastNonZeroMovementDirection); // Set facing (flipX, IsFacingBackward) based on last movement

        Debug.Log($"NPC {gameObject.name} stopped following. _isFollowing set to FALSE. Animator parameters set to idle. HDir: {hDirValue}, VDir: {vDirValue}");

        // Re-enable the CapsuleCollider2D when following stops
        if (_triggerCollider != null)
        {
            _triggerCollider.enabled = true;
            Debug.Log($"NPC {gameObject.name}: CapsuleCollider2D re-enabled.");
        }
        // Do NOT set _isAtFirepitSpot to false here, as StopFollowing can be called for various reasons.
        // _isAtFirepitSpot is specifically managed by GoToDestination and SetFirepitAnimationState.
    }

    /// <summary>
    /// Moves the NPC to a specific destination, overriding following behavior.
    /// </summary>
    /// <param name="destination">The world position to move to.</param>
    public void GoToDestination(Vector2 destination)
    {
        StopFollowing(); // Stop following before moving to a fixed destination
        _currentDestination = destination;
        _isMovingToDestination = true;
        // Removed: _isAtFirepitSpot = false; // This flag should only be set when the NPC is AT the spot
        Debug.Log($"NPC {gameObject.name} told to move to destination: {destination}. _isMovingToDestination set to TRUE. Current position: {_rb.position}"); // NEW LOG
    }

    /// <summary>
    /// FixedUpdate is called at a fixed framerate, ideal for physics calculations.
    /// Handles movement for both following and direct destination modes.
    /// </summary>
    private void FixedUpdate()
    {
        // If NPC is explicitly at a firepit spot, bypass all movement and animation logic.
        // Its animation state is managed by SetFirepitAnimationState.
        if (_isAtFirepitSpot)
        {
            _rb.linearVelocity = Vector2.zero; // Ensure it's truly stopped
            _animator.SetBool(IsMovingHash, false); // Force IsMoving to false
            // HorizontalDirection and VerticalDirection are already set by SetFirepitAnimationState
            // and should not be overridden by FixedUpdate's velocity calculation.
            // Debug.Log($"NPC {gameObject.name}: FixedUpdate bypassed. _isAtFirepitSpot: {_isAtFirepitSpot}. Animator IsMoving: {_animator.GetBool(IsMovingHash)}, HDir: {_animator.GetFloat(HorizontalDirectionHash)}, VDir: {_animator.GetFloat(VerticalDirectionHash)}");
            return; // Exit FixedUpdate early
        }

        // If not actively following or moving to a destination, ensure idle and return.
        // This prevents constant animation updates for non-follower NPCs.
        if (!_isFollowing && !_isMovingToDestination)
        {
            _rb.linearVelocity = Vector2.zero;
            _animator.SetBool(IsMovingHash, false);
            _animator.SetFloat(HorizontalDirectionHash, 0f);
            _animator.SetFloat(VerticalDirectionHash, 0f);
            // We intentionally do NOT call UpdateSpriteDirection here to avoid constant logs for static NPCs.
            // Their initial facing should be set once (e.g., in Awake/Start if they have a default orientation,
            // or by an external system if needed). The _lastNonZeroMovementDirection will retain its value
            // for when they *do* start moving.
            // Debug.Log($"NPC {gameObject.name}: Not following or moving. Ensuring idle state.");
            return; // Exit FixedUpdate early
        }


        if (_leaderTransform == null && _isFollowing)
        {
            StopFollowing(); // Stop if leader disappears
            return;
        }

        Vector2 currentVelocity = Vector2.zero;
        float distanceToActualLeader = 0f;
        bool isCurrentlyMoving = false; // Flag to determine overall movement state

        if (_isFollowing)
        {
            // Get current leader's actual position (not delayed) for proximity check
            Vector2 actualLeaderPosition = _leaderTransform.position;
            distanceToActualLeader = Vector2.Distance(_rb.position, actualLeaderPosition);

            // If too close to the actual leader, stop moving to prevent pushing.
            if (distanceToActualLeader < _playerProximityStopDistance)
            {
                _rb.linearVelocity = Vector2.zero;
                isCurrentlyMoving = false; // Not moving due to proximity
                // Debug.Log($"NPC {gameObject.name}: Stopped due to proximity to leader (Distance: {distanceToActualLeader:F2}).");
            }
            else
            {
                // Get current leader's velocity
                Vector2 currentLeaderVelocity = Vector2.zero;
                if (PlayerController.Instance != null && _leaderTransform == PlayerController.Instance.transform)
                {
                    currentLeaderVelocity = PlayerController.Instance.CurrentMovementVelocity;
                }
                else
                {
                    Rigidbody2D leaderRb = _leaderTransform.GetComponent<Rigidbody2D>();
                    if (leaderRb != null)
                    {
                        currentLeaderVelocity = leaderRb.linearVelocity;
                    }
                }

                // Add current leader position and velocity to history
                _leaderPositionHistory.Enqueue(_leaderTransform.position);
                _leaderVelocityHistory.Enqueue(currentLeaderVelocity); // Enqueue current velocity

                // Remove oldest entries if history is too long
                if (_leaderPositionHistory.Count > _followDelayFrames)
                {
                    _leaderPositionHistory.Dequeue();
                    _leaderVelocityHistory.Dequeue(); // Dequeue oldest velocity
                }

                // The target position for the NPC is the oldest position in the history queue
                Vector2 delayedLeaderPos = _leaderPositionHistory.Peek();
                Vector2 delayedLeaderVelocity = _leaderVelocityHistory.Peek(); // Get delayed velocity

                Vector2 leaderMovementDirection;
                float leaderSpeed = delayedLeaderVelocity.magnitude;

                // If the delayed leader is not moving significantly, calculate direction from NPC to leader
                if (leaderSpeed < 0.01f)
                {
                    // Calculate direction from NPC's current position to the delayed leader's position
                    Vector2 directionToDelayedLeader = delayedLeaderPos - (Vector2)_rb.position;

                    // Only normalize if the direction is not zero, otherwise use a default direction or handle as stopped
                    if (directionToDelayedLeader.magnitude > 0.001f) // Small threshold to prevent issues with Vector2.zero.normalized
                    {
                        leaderMovementDirection = directionToDelayedLeader.normalized;
                    }
                    else
                    {
                        leaderMovementDirection = Vector2.zero;
                    }

                    _targetPosition = delayedLeaderPos - leaderMovementDirection * _followDistance;
                }
                else
                {
                    // If leader is moving, use its movement direction to calculate "behind"
                    // IMPORTANT: We use the *delayed* leader's velocity here to determine the "behind" position.
                    leaderMovementDirection = delayedLeaderVelocity.normalized;
                    _targetPosition = delayedLeaderPos - leaderMovementDirection * _followDistance;
                }

                Vector2 currentPosition = _rb.position;
                Vector2 newPosition = Vector2.Lerp(currentPosition, _targetPosition, Time.fixedDeltaTime * _lerpSmoothness);
                _rb.MovePosition(newPosition);

                currentVelocity = (newPosition - currentPosition) / Time.fixedDeltaTime; // Approximate NPC's velocity

                float distanceToTarget = Vector2.Distance(currentPosition, _targetPosition);

                // Determine if NPC is actively moving towards its target
                isCurrentlyMoving = currentVelocity.magnitude > 0.01f || distanceToTarget > _stopDistanceThreshold;
            }
        }
        else if (_isMovingToDestination)
        {
            Vector2 directionToDestination = _currentDestination - _rb.position;
            float currentDistance = directionToDestination.magnitude; // Store magnitude for logging

            // Added debug log to track distance and threshold
            Debug.Log($"NPC {gameObject.name}: Moving to destination. Current pos: {_rb.position:F2}, Dest: {_currentDestination:F2}, Distance: {currentDistance:F3}, Threshold: {_stopDistanceThreshold:F3}"); // NEW LOG

            if (currentDistance > _stopDistanceThreshold) // Use currentDistance here
            {
                Vector2 moveStep = directionToDestination.normalized * _moveSpeed * Time.fixedDeltaTime;
                _rb.MovePosition(_rb.position + moveStep);
                currentVelocity = moveStep / Time.fixedDeltaTime;
                isCurrentlyMoving = true; // Moving towards destination
                // Added debug log to track movement
                // Debug.Log($"NPC {gameObject.name}: Moving. Current position: {_rb.position}, Move step: {moveStep}");
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
                _isMovingToDestination = false; // Reached destination
                isCurrentlyMoving = false; // Not moving
                // Added debug log for arrival
                Debug.Log($"NPC {gameObject.name} reached destination: {_currentDestination}. Final position: {_rb.position}. Setting _isMovingToDestination to FALSE."); // NEW LOG
                OnDestinationReached?.Invoke(); // NEW: Invoke event when destination is reached
            }
        }

        // NEW: If the NPC is now at the firepit spot (flag set mid-frame by OnDestinationReached),
        // immediately return to prevent further animation updates in this frame.
        if (_isAtFirepitSpot)
        {
            return;
        }

        // --- Animation Parameter Updates (ONLY if actively moving/following) ---
        // Debug.Log($"NPC {gameObject.name}: IsMoving (BEFORE SET): {_animator.GetBool(IsMovingHash)}. isCurrentlyMoving (calculated): {isCurrentlyMoving}");
        _animator.SetBool(IsMovingHash, isCurrentlyMoving);
        // Debug.Log($"NPC {gameObject.name}: IsMoving (AFTER SET): {_animator.GetBool(IsMovingHash)}");


        if (isCurrentlyMoving)
        {
            // When moving, update last non-zero direction and set animator floats based on current velocity
            if (currentVelocity.magnitude > 0.001f) // Ensure valid direction
            {
                _lastNonZeroMovementDirection = currentVelocity.normalized;
            }

            // Debug.Log($"NPC {gameObject.name}: Raw currentVelocity for animation floats: {currentVelocity:F3}, Magnitude: {currentVelocity.magnitude:F3}");

            // Determine values for animator floats based on current velocity
            float hDirValue = 0f;
            if (currentVelocity.x < -_horizontalVelocityDeadZone)
            {
                hDirValue = -1f;
            }
            else if (currentVelocity.x > _horizontalVelocityDeadZone)
            {
                hDirValue = 1f;
            }

            float vDirValue = 0f;
            if (currentVelocity.y < -_verticalVelocityDeadZone)
            {
                vDirValue = -1f; // Down/Forward
            }
            else if (currentVelocity.y > _verticalVelocityDeadZone)
            {
                vDirValue = 1f; // Up/Backward
            }

            // Debug.Log($"NPC {gameObject.name}: Attempting to set Animator HDir: {hDirValue}, VDir: {vDirValue}. Current Animator values (BEFORE SET): H={_animator.GetFloat(HorizontalDirectionHash)}, V={_animator.GetFloat(VerticalDirectionHash)}");

            _animator.SetFloat(HorizontalDirectionHash, hDirValue);
            _animator.SetFloat(VerticalDirectionHash, vDirValue);

            // Debug.Log($"NPC {gameObject.name}: Animator HDir set to: {_animator.GetFloat(HorizontalDirectionHash)}, VDir set to: {_animator.GetFloat(VerticalDirectionHash)}. (AFTER SCRIPT SET)");

            UpdateSpriteDirection(currentVelocity); // This sets flipX and IsFacingBackward
        }
        else // NPC is NOT moving (but *is* following or moving to destination, it just stopped)
        {
            // Set facing (flipX, IsFacingBackward) based on the last known non-zero movement direction
            UpdateSpriteDirection(_lastNonZeroMovementDirection);

            // Determine values for animator floats based on the LAST non-zero movement direction.
            // This is crucial for the Animator to pick the correct directional idle animation.
            float hDirValue = 0f;
            if (_lastNonZeroMovementDirection.x < -_horizontalVelocityDeadZone)
            {
                hDirValue = -1f;
            }
            else if (_lastNonZeroMovementDirection.x > _horizontalVelocityDeadZone)
            {
                hDirValue = 1f;
            }

            float vDirValue = 0f;
            if (_lastNonZeroMovementDirection.y < -_verticalVelocityDeadZone)
            {
                vDirValue = -1f; // Down/Forward
            }
            else if (_lastNonZeroMovementDirection.y > _verticalVelocityDeadZone)
            {
                vDirValue = 1f; // Up/Backward
            }

            _animator.SetFloat(HorizontalDirectionHash, hDirValue);
            _animator.SetFloat(VerticalDirectionHash, vDirValue);
            // Debug.Log($"NPC {gameObject.name}: Stopped. Setting HDir: {hDirValue}, VDir: {vDirValue}. Last Non-Zero Dir: {_lastNonZeroMovementDirection:F2}");
        }
    }

    /// <summary>
    /// Updates the NPC's sprite direction (flipX) and IsFacingBackward animator parameter
    /// based on a given direction vector. This method does NOT set HorizontalDirection
    /// or VerticalDirection floats, as those are handled directly in FixedUpdate based on movement state.
    /// </summary>
    /// <param name="direction">The direction vector the NPC should face.</param>
    public void UpdateSpriteDirection(Vector2 direction)
    {
        // Use a very small epsilon for comparisons to ensure any non-zero input triggers a flip.
        float epsilon = 0.0001f;

        // Update sprite flip based on X direction. If X is effectively zero, then check Y.
        if (direction.x < -epsilon) // Moving left
        {
            _spriteRenderer.flipX = true; // Face left
        }
        else if (direction.x > epsilon) // Moving right
        {
            _spriteRenderer.flipX = false; // Face right
        }
        // If horizontal direction is effectively zero, use vertical direction for flip.
        else if (direction.y < -epsilon) // Moving down
        {
            _spriteRenderer.flipX = true; // Flip left (as per "down or left, flip left")
        }
        else if (direction.y > epsilon) // Moving up
        {
            _spriteRenderer.flipX = false; // Flip right (as per "up or right, flip right")
        }
        // If both x and y directions are effectively zero, spriteRenderer.flipX retains its last state.


        // Update IsFacingBackward boolean based on Y direction. If Y is effectively zero, then check X.
        // As per the convention: "Walking Forward is moving down in my map, and Backward is moving up."
        // IsBackwards should be true for up or left, false for down or right.
        if (direction.y > epsilon) // Moving UP the screen
        {
            _animator.SetBool(IsFacingBackwardHash, true); // Should face/animate backward
        }
        else if (direction.y < -epsilon) // Moving DOWN the screen
        {
            _animator.SetBool(IsFacingBackwardHash, false); // Should face/animate forward
        }
        else if (direction.x < -epsilon) // Moving left (and vertical is zero)
        {
            _animator.SetBool(IsFacingBackwardHash, true); // Should face/animate backward
        }
        else if (direction.x > epsilon) // Moving right (and vertical is zero)
        {
            _animator.SetBool(IsFacingBackwardHash, false); // Should face/animate forward
        }
        // If both x and y directions are effectively zero, IsFacingBackwardHash maintains its last state.

        Debug.Log($"NPCFollower on {gameObject.name}: UpdateSpriteDirection called with {direction}. IsFacingBackward: {_animator.GetBool(IsFacingBackwardHash)}, Sprite flipX: {_spriteRenderer.flipX}");
    }

    /// <summary>
    /// Sets the NPC's animation parameters and sprite facing for a stationary state,
    /// typically used when arriving at a specific point like the firepit.
    /// </summary>
    /// <param name="isMoving">Should the 'IsMoving' parameter be set to true or false.</param>
    /// <param name="horizontalDir">The value for the 'HorizontalDirection' float parameter.</param>
    /// <param name="verticalDir">The value for the 'VerticalDirection' float parameter.</param>
    public void SetFirepitAnimationState(bool isMoving, float horizontalDir, float verticalDir)
    {
        // Removed: _isAtFirepitSpot = true; // This flag will now be set by FirepitTrigger after arrival
        _isMovingToDestination = false; // Ensure it's not considered moving to destination
        _isFollowing = false; // Ensure it's not considered following

        _rb.linearVelocity = Vector2.zero; // Explicitly stop the NPC's rigidbody

        _animator.SetBool(IsMovingHash, isMoving);
        _animator.SetFloat(HorizontalDirectionHash, horizontalDir);
        _animator.SetFloat(VerticalDirectionHash, verticalDir);

        // Determine sprite flip and IsFacingBackward based on provided directions
        // Use a very small epsilon for comparisons to ensure any non-zero input triggers a flip.
        float epsilon = 0.0001f;

        // Sprite Flip (left/right)
        if (horizontalDir < -epsilon) // Left
        {
            _spriteRenderer.flipX = true;
        }
        else if (horizontalDir > epsilon) // Right
        {
            _spriteRenderer.flipX = false;
        }
        // If horizontal is zero, use vertical for horizontal flip (as per player logic)
        else if (verticalDir < -epsilon) // Down
        {
            _spriteRenderer.flipX = true; // Face left
        }
        else if (verticalDir > epsilon) // Up
        {
            _spriteRenderer.flipX = false; // Face right
        }
        // If both are zero, retain last flipX

        // IsFacingBackward (up/down)
        if (verticalDir > epsilon) // Up (backward)
        {
            _animator.SetBool(IsFacingBackwardHash, true);
        }
        else if (verticalDir < -epsilon) // Down (forward)
        {
            _animator.SetBool(IsFacingBackwardHash, false);
        }
        // If vertical is zero, use horizontal for IsFacingBackward (as per player logic)
        else if (horizontalDir < -epsilon) // Left
        {
            _animator.SetBool(IsFacingBackwardHash, true); // Face backward
        }
        else if (horizontalDir > epsilon) // Right
        {
            _animator.SetBool(IsFacingBackwardHash, false); // Face forward
        }
        // If both are zero, retain last IsFacingBackward

        Debug.Log($"NPC {gameObject.name}: Firepit animation state set. IsMoving: {isMoving}, HDir: {horizontalDir}, VDir: {verticalDir}. IsFacingBackward: {_animator.GetBool(IsFacingBackwardHash)}, Sprite flipX: {_spriteRenderer.flipX}");
    }

    /// <summary>
    /// Sets the NPC's sprite facing direction based on a given direction vector.
    /// This method is called by external scripts (e.g., FirepitTrigger) to force a specific facing.
    /// This method is now primarily for general-purpose facing, and does NOT set the float direction parameters.
    /// </summary>
    /// <param name="direction">The direction vector the NPC should face.</param>
    public void SetFacingDirection(Vector2 direction)
    {
        // Use a very small epsilon to check if the direction is effectively zero.
        float epsilon = 0.0001f;

        // Directly apply sprite flip based on X direction. If X is effectively zero, then check Y.
        if (direction.x < -epsilon) // Direction is left
        {
            _spriteRenderer.flipX = true; // Face left
        }
        else if (direction.x > epsilon) // Direction is right
        {
            _spriteRenderer.flipX = false; // Face right
        }
        else if (direction.y < -epsilon) // Direction is down (and horizontal is zero)
        {
            _spriteRenderer.flipX = true; // Face left (as per "down or left, flip left")
        }
        else if (direction.y > epsilon) // Direction is up (and horizontal is zero)
        {
            _spriteRenderer.flipX = false; // Flip right (as per "up or right, flip right")
        }
        // If both x and y directions are effectively zero, spriteRenderer.flipX retains its last state.


        // Directly apply IsFacingBackward boolean based on Y direction. If Y is effectively zero, then check X.
        // IsBackwards should be true for up or left, false for down or right.
        if (direction.y > epsilon) // Direction is up
        {
            _animator.SetBool(IsFacingBackwardHash, true); // Should face/animate backward
        }
        else if (direction.y < -epsilon) // Direction is down
        {
            _animator.SetBool(IsFacingBackwardHash, false); // Should face/animate forward
        }
        else if (direction.x < -epsilon) // Moving left (and vertical is zero)
        {
            _animator.SetBool(IsFacingBackwardHash, true); // Should face/animate backward
        }
        else if (direction.x > epsilon) // Moving right (and vertical is zero)
        {
            _animator.SetBool(IsFacingBackwardHash, false); // Should face/animate forward
        }
        // If both x and y directions are effectively zero, IsFacingBackwardHash maintains its last state.

        Debug.Log($"NPCFollower on {gameObject.name}: SetFacingDirection called. Direction: {direction}. IsFacingBackward: {_animator.GetBool(IsFacingBackwardHash)}, Sprite flipX: {_spriteRenderer.flipX}");
    }

    // NEW: Public method to set the _isAtFirepitSpot flag
    public void SetIsAtFirepitSpot(bool atSpot)
    {
        _isAtFirepitSpot = atSpot;
        if (atSpot)
        {
            _rb.linearVelocity = Vector2.zero; // Ensure it's truly stopped when at spot
            _animator.SetBool(IsMovingHash, false); // Force IsMoving to false when at spot and not moving
        }
        Debug.Log($"NPC {gameObject.name}: SetIsAtFirepitSpot called. _isAtFirepitSpot set to: {atSpot}");
    }
}
