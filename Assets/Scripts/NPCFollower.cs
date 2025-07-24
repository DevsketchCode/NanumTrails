using UnityEngine;
using System.Collections.Generic; // For Queue
using System.Linq; // For LINQ operations

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
    [SerializeField] private float _stopDistanceThreshold = 0.1f;
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

    // Store a history of the leader's positions and velocities to implement the delay
    private Queue<Vector2> _leaderPositionHistory = new Queue<Vector2>();
    private Queue<Vector2> _leaderVelocityHistory = new Queue<Vector2>(); // To store historical velocities

    private Vector2 _targetPosition; // The calculated position this NPC is trying to reach

    // Animator parameter hashes for efficiency
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsFacingBackwardHash = Animator.StringToHash("IsFacingBackward");
    private static readonly int HorizontalDirectionHash = Animator.StringToHash("HorizontalDirection"); // New hash for horizontal
    private static readonly int VerticalDirectionHash = Animator.StringToHash("VerticalDirection"); // New hash for vertical

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
            UpdateSpriteDirection(directionToLeader);
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
        // Reset direction parameters to 0 when stopped
        _animator.SetFloat(HorizontalDirectionHash, 0f);
        _animator.SetFloat(VerticalDirectionHash, 0f);
        Debug.Log($"NPC {gameObject.name} stopped following. _isFollowing set to FALSE.");

        // Re-enable the CapsuleCollider2D when following stops
        if (_triggerCollider != null)
        {
            _triggerCollider.enabled = true;
            Debug.Log($"NPC {gameObject.name}: CapsuleCollider2D re-enabled.");
        }
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
        Debug.Log($"NPC {gameObject.name} moving to destination: {destination}");
    }

    /// <summary>
    /// FixedUpdate is called at a fixed framerate, ideal for physics calculations.
    /// Handles movement for both following and direct destination modes.
    /// </summary>
    private void FixedUpdate()
    {
        if (_leaderTransform == null && _isFollowing)
        {
            StopFollowing(); // Stop if leader disappears
            return;
        }

        Vector2 currentVelocity = Vector2.zero;
        float distanceToActualLeader = 0f;

        if (_isFollowing)
        {
            // Get current leader's actual position (not delayed) for proximity check
            Vector2 actualLeaderPosition = _leaderTransform.position;
            distanceToActualLeader = Vector2.Distance(_rb.position, actualLeaderPosition);

            // If too close to the actual leader, stop moving to prevent pushing.
            if (distanceToActualLeader < _playerProximityStopDistance)
            {
                _rb.linearVelocity = Vector2.zero;
                _animator.SetBool(IsMovingHash, false);
                // Ensure animator direction parameters are zero when stopped by proximity
                _animator.SetFloat(HorizontalDirectionHash, 0f);
                _animator.SetFloat(VerticalDirectionHash, 0f);
                Debug.Log($"NPC {gameObject.name}: Stopped due to proximity to leader (Distance: {distanceToActualLeader:F2}).");
                // No need to call UpdateSpriteDirection here, as it's handled by setting parameters to 0
                return; // Stop processing movement for this frame
            }

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
                leaderMovementDirection = delayedLeaderVelocity.normalized;
                _targetPosition = delayedLeaderPos - leaderMovementDirection * _followDistance;
            }

            Vector2 currentPosition = _rb.position;
            Vector2 newPosition = Vector2.Lerp(currentPosition, _targetPosition, Time.fixedDeltaTime * _lerpSmoothness);
            _rb.MovePosition(newPosition);

            currentVelocity = (newPosition - currentPosition) / Time.fixedDeltaTime; // Approximate NPC's velocity

            float distanceToTarget = Vector2.Distance(currentPosition, _targetPosition);

            // Stop animation if very close to target and delayed leader is not moving significantly
            if (distanceToTarget < _stopDistanceThreshold && delayedLeaderVelocity.magnitude < 0.01f)
            {
                _rb.linearVelocity = Vector2.zero;
                _animator.SetBool(IsMovingHash, false);
                // Reset direction parameters to 0 when truly stopped
                _animator.SetFloat(HorizontalDirectionHash, 0f);
                _animator.SetFloat(VerticalDirectionHash, 0f);
            }
            else
            {
                _animator.SetBool(IsMovingHash, true);
            }
        }
        else if (_isMovingToDestination)
        {
            Vector2 directionToDestination = _currentDestination - _rb.position;
            if (directionToDestination.magnitude > _stopDistanceThreshold)
            {
                Vector2 moveStep = directionToDestination.normalized * _moveSpeed * Time.fixedDeltaTime;
                _rb.MovePosition(_rb.position + moveStep);
                _animator.SetBool(IsMovingHash, true);
                currentVelocity = moveStep / Time.fixedDeltaTime;
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
                _animator.SetBool(IsMovingHash, false);
                // Reset direction parameters to 0 when reached destination
                _animator.SetFloat(HorizontalDirectionHash, 0f);
                _animator.SetFloat(VerticalDirectionHash, 0f);
                _isMovingToDestination = false; // Reached destination
                Debug.Log($"NPC {gameObject.name} reached destination: {_currentDestination}");
            }
        }
        else
        {
            // Not following and not moving to destination, ensure idle
            _rb.linearVelocity = Vector2.zero;
            _animator.SetBool(IsMovingHash, false);
            // Reset direction parameters to 0 when completely idle
            _animator.SetFloat(HorizontalDirectionHash, 0f);
            _animator.SetFloat(VerticalDirectionHash, 0f);
        }

        // Always update sprite direction and animator parameters based on current velocity
        // This ensures animations update even when moving to destination or when forced to stop by proximity
        UpdateSpriteDirection(currentVelocity);
    }

    /// <summary>
    /// Updates the NPC's sprite direction (flipX) and animation state
    /// based on its current movement velocity or a forced direction.
    /// This method now also sets the HorizontalDirection and VerticalDirection Animator parameters.
    /// </summary>
    /// <param name="direction">The current velocity of the NPC, or a forced direction vector.</param>
    public void UpdateSpriteDirection(Vector2 direction)
    {
        // Horizontal flip and Animator parameter based on X direction
        if (direction.x < -_horizontalVelocityDeadZone)
        {
            _spriteRenderer.flipX = true; // Face left
            _animator.SetFloat(HorizontalDirectionHash, -1f); // Set animator parameter for left
        }
        else if (direction.x > _horizontalVelocityDeadZone)
        {
            _spriteRenderer.flipX = false; // Face right
            _animator.SetFloat(HorizontalDirectionHash, 1f); // Set animator parameter for right
        }
        else
        {
            _animator.SetFloat(HorizontalDirectionHash, 0f); // Velocity within dead zone, treat as no horizontal movement
        }

        // Vertical animation and Animator parameter based on Y direction
        // As per the convention: "Walking Forward is moving down in my map, and Backward is moving up."
        // This means a negative Y velocity corresponds to 'forward' animation,
        // and a positive Y velocity corresponds to 'backward' animation.
        if (direction.y > _verticalVelocityDeadZone) // Moving UP the screen
        {
            _animator.SetBool(IsFacingBackwardHash, true); // Should face/animate backward
            _animator.SetFloat(VerticalDirectionHash, 1f); // Positive Y for "backward" direction
        }
        else if (direction.y < -_verticalVelocityDeadZone) // Moving DOWN the screen
        {
            _animator.SetBool(IsFacingBackwardHash, false); // Should face/animate forward
            _animator.SetFloat(VerticalDirectionHash, -1f); // Negative Y for "forward" direction
        }
        else
        {
            _animator.SetFloat(VerticalDirectionHash, 0f); // Velocity within dead zone, treat as no vertical movement
        }
    }

    /// <summary>
    /// Sets the NPC's sprite facing direction based on a given direction vector.
    /// This method is called by external scripts (e.g., FirepitTrigger) to force a specific facing.
    /// </summary>
    /// <param name="direction">The direction vector the NPC should face.</param>
    public void SetFacingDirection(Vector2 direction)
    {
        // Reuse the existing UpdateSpriteDirection logic by passing the desired direction.
        // We normalize and use a small magnitude to ensure the logic within UpdateSpriteDirection is triggered
        // for sprite flipping and animator parameter setting, even if the NPC is stationary but needs to face a direction.
        if (direction.magnitude > 0.001f) // Ensure there's a valid direction to normalize
        {
            UpdateSpriteDirection(direction.normalized * 0.1f); // Use a small magnitude to trigger the logic
        }
        else
        {
            // If the forced direction is zero, ensure the animator parameters are reset to idle/neutral
            _animator.SetFloat(HorizontalDirectionHash, 0f);
            _animator.SetFloat(VerticalDirectionHash, 0f);
        }
        Debug.Log($"NPCFollower on {gameObject.name}: Forced facing direction to {direction}");
    }
}
