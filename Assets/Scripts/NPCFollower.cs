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

    [Header("Delay Settings")]
    [Tooltip("The number of FixedUpdate frames the NPC's movement should lag behind its leader. Higher values mean more delay.")]
    [SerializeField] private int _followDelayFrames = 10;

    private Rigidbody2D _rb;
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    private bool _isFollowing = false;
    private bool _isMovingToDestination = false;
    private Vector2 _currentDestination;

    // Store a history of the leader's positions and velocities to implement the delay
    private Queue<Vector2> _leaderPositionHistory = new Queue<Vector2>();
    private Queue<Vector2> _leaderVelocityHistory = new Queue<Vector2>(); // NEW: To store historical velocities

    private Vector2 _targetPosition; // The calculated position this NPC is trying to reach

    // Animator parameter hashes for efficiency
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsFacingBackwardHash = Animator.StringToHash("IsFacingBackward");

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Gets component references and initializes the position history.
    /// </summary>
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Ensure Rigidbody2D is configured for 2D movement and not affected by gravity
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
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
    }

    /// <summary>
    /// Stops the NPC from following the leader.
    /// </summary>
    public void StopFollowing()
    {
        _isFollowing = false;
        _rb.linearVelocity = Vector2.zero; // Stop any residual movement
        _animator.SetBool(IsMovingHash, false); // Set to idle animation
        Debug.Log($"NPC {gameObject.name} stopped following. _isFollowing set to FALSE.");
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

        if (_isFollowing)
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

            // NEW DIAGNOSTIC LOG: What velocity is NPCFollower getting from the leader *right now*?
            Debug.Log($"NPC {gameObject.name} (NPCFollower): Current Leader Velocity obtained = {currentLeaderVelocity}");


            // Add current leader position and velocity to history
            _leaderPositionHistory.Enqueue(_leaderTransform.position);
            _leaderVelocityHistory.Enqueue(currentLeaderVelocity); // NEW: Enqueue current velocity

            // Remove oldest entries if history is too long
            if (_leaderPositionHistory.Count > _followDelayFrames)
            {
                _leaderPositionHistory.Dequeue();
                _leaderVelocityHistory.Dequeue(); // NEW: Dequeue oldest velocity
            }

            // The target position for the NPC is the oldest position in the history queue
            Vector2 delayedLeaderPos = _leaderPositionHistory.Peek();
            Vector2 delayedLeaderVelocity = _leaderVelocityHistory.Peek(); // NEW: Get delayed velocity

            // Use the delayed leader's velocity to determine its movement direction
            Vector2 leaderMovementDirection = delayedLeaderVelocity.normalized;
            float leaderSpeed = delayedLeaderVelocity.magnitude; // NEW: Use delayed leader speed

            // If the delayed leader is not moving significantly
            if (leaderSpeed < 0.01f)
            {
                _targetPosition = delayedLeaderPos; // NPC aims for the leader's delayed stop position
            }
            else
            {
                // Calculate the position behind the delayed leader based on its movement direction
                _targetPosition = delayedLeaderPos - leaderMovementDirection * _followDistance;
            }

            Vector2 currentPosition = _rb.position;
            Vector2 newPosition = Vector2.Lerp(currentPosition, _targetPosition, Time.fixedDeltaTime * _lerpSmoothness);
            _rb.MovePosition(newPosition);

            currentVelocity = (newPosition - currentPosition) / Time.fixedDeltaTime; // Approximate NPC's velocity

            float distanceToTarget = Vector2.Distance(currentPosition, _targetPosition);

            // Debug logs for following behavior
            Debug.Log($"NPC {gameObject.name} (Following): Leader Pos={_leaderTransform.position}, Delayed Pos={delayedLeaderPos}, " +
                      $"Target Pos={_targetPosition}, Current Pos={currentPosition}, Distance={distanceToTarget:F2}, " +
                      $"Delayed Leader Speed={leaderSpeed:F2}, Queue Count={_leaderPositionHistory.Count}");

            // Stop animation if very close to target and delayed leader is not moving significantly
            if (distanceToTarget < _stopDistanceThreshold && leaderSpeed < _stopDistanceThreshold)
            {
                _rb.linearVelocity = Vector2.zero;
                _animator.SetBool(IsMovingHash, false);
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
                Debug.Log($"NPC {gameObject.name} (Moving to Dest): Current Pos={_rb.position}, Target Dest={_currentDestination}, Distance={directionToDestination.magnitude:F2}");
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
                _animator.SetBool(IsMovingHash, false);
                _isMovingToDestination = false; // Reached destination
                Debug.Log($"NPC {gameObject.name} reached destination: {_currentDestination}");
            }
        }
        else
        {
            // Not following and not moving to destination, ensure idle
            _rb.linearVelocity = Vector2.zero;
            _animator.SetBool(IsMovingHash, false);
        }

        // Update sprite direction based on movement velocity
        UpdateSpriteDirection(currentVelocity);
    }

    /// <summary>
    /// Updates the NPC's sprite direction (flipX) and animation state
    /// based on its current movement velocity.
    /// </summary>
    /// <param name="velocity">The current velocity of the NPC.</param>
    private void UpdateSpriteDirection(Vector2 velocity)
    {
        // Only update if there's significant movement
        if (velocity.magnitude > 0.05f) // Small threshold to avoid flipping on tiny movements
        {
            // Horizontal flip based on X velocity
            if (velocity.x < 0)
            {
                _spriteRenderer.flipX = true; // Face left
            }
            else if (velocity.x > 0)
            {
                _spriteRenderer.flipX = false; // Face right
            }

            // Vertical animation based on Y velocity (for isometric "up" or "down" sprites)
            if (velocity.y > 0)
            {
                _animator.SetBool(IsFacingBackwardHash, true); // Moving "up" the screen
            }
            else if (velocity.y < 0)
            {
                _animator.SetBool(IsFacingBackwardHash, false); // Moving "down" the screen
            }
        }
    }
}
