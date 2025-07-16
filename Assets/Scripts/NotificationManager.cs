using UnityEngine;
using TMPro; // Required for TextMeshProUGUI
using System.Collections; // Required for Coroutines
using UnityEngine.UI; // Required for Image/CanvasGroup

/// <summary>
/// Manages and displays small, temporary UI notifications to the player.
/// This is a singleton that can be accessed from anywhere to show alerts.
/// </summary>
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The TextMeshProUGUI component where the notification message will be displayed.")]
    [SerializeField] private TextMeshProUGUI _notificationText;
    [Tooltip("The CanvasGroup component of the notification UI panel. Used for fading.")]
    [SerializeField] private CanvasGroup _notificationCanvasGroup;
    [Tooltip("The Image component for the notification's background (optional, for visual styling).")]
    [SerializeField] private Image _notificationBackground;

    [Header("Settings")]
    [Tooltip("How long the notification should be displayed on screen (in seconds) for temporary messages.")]
    [SerializeField] private float _displayDuration = 3.0f;
    [Tooltip("How long the fade-in and fade-out animations should take (in seconds) for temporary messages.")]
    [SerializeField] private float _fadeDuration = 0.5f;

    private Coroutine _currentNotificationCoroutine;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the singleton and sets initial UI state.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); // If you want the manager to persist across scenes.
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure the notification UI is initially hidden and fully transparent.
        if (_notificationCanvasGroup != null)
        {
            _notificationCanvasGroup.alpha = 0f; // Fully transparent
            _notificationCanvasGroup.gameObject.SetActive(false); // Hide the GameObject
        }
        else
        {
            Debug.LogError("NotificationManager: _notificationCanvasGroup is not assigned! Notifications will not display correctly.");
        }

        if (_notificationText == null)
        {
            Debug.LogError("NotificationManager: _notificationText is not assigned! Notification messages will be empty.");
        }
    }

    /// <summary>
    /// Displays a temporary notification message to the player.
    /// If a notification is already active, it will be immediately replaced by the new one.
    /// </summary>
    /// <param name="message">The text message to display in the notification.</param>
    public void ShowNotification(string message)
    {
        if (_notificationCanvasGroup == null || _notificationText == null)
        {
            Debug.LogError("NotificationManager: UI references are not set up. Cannot show notification: " + message);
            return;
        }

        // Stop any ongoing notification display coroutine
        if (_currentNotificationCoroutine != null)
        {
            StopCoroutine(_currentNotificationCoroutine);
        }

        // Set the message and start the display coroutine
        _notificationText.text = message;
        _currentNotificationCoroutine = StartCoroutine(DisplayNotificationCoroutine());
        Debug.Log($"Notification: {message}"); // Log the notification for debugging
    }

    /// <summary>
    /// Displays a permanent, non-fading notification message.
    /// This is intended for end-game or critical messages that should remain on screen.
    /// It can optionally use custom UI elements provided by the caller.
    /// </summary>
    /// <param name="message">The text message to display permanently.</param>
    /// <param name="customTextPanel">Optional: A custom TextMeshProUGUI panel to use instead of the default.</param>
    /// <param name="customCanvasGroup">Optional: A custom CanvasGroup to use instead of the default.</param>
    public void ShowPermanentNotification(string message, TextMeshProUGUI customTextPanel = null, CanvasGroup customCanvasGroup = null)
    {
        // Determine which UI elements to use: custom if provided, otherwise default
        TextMeshProUGUI targetText = customTextPanel != null ? customTextPanel : _notificationText;
        CanvasGroup targetCanvasGroup = customCanvasGroup != null ? customCanvasGroup : _notificationCanvasGroup;

        if (targetCanvasGroup == null || targetText == null)
        {
            Debug.LogError("NotificationManager: Target UI references are not set up. Cannot show permanent notification: " + message);
            return;
        }

        // Stop any ongoing temporary notification display coroutine
        if (_currentNotificationCoroutine != null)
        {
            StopCoroutine(_currentNotificationCoroutine);
            _currentNotificationCoroutine = null; // Clear the coroutine reference
        }

        targetText.text = message;
        targetCanvasGroup.alpha = 1f; // Make fully visible
        targetCanvasGroup.gameObject.SetActive(true); // Ensure GameObject is active
        targetCanvasGroup.interactable = true; // Make it interactable (e.g., to block clicks)
        targetCanvasGroup.blocksRaycasts = true; // Make it block raycasts

        Debug.Log($"Permanent Notification: {message}");
    }

    /// <summary>
    /// Coroutine to handle the fading in, display duration, and fading out of the notification.
    /// </summary>
    private IEnumerator DisplayNotificationCoroutine()
    {
        // Ensure the GameObject is active before starting fade-in
        _notificationCanvasGroup.gameObject.SetActive(true);
        _notificationCanvasGroup.interactable = false; // Not interactable for temporary
        _notificationCanvasGroup.blocksRaycasts = false; // Not blocking for temporary

        // Fade In
        float timer = 0f;
        while (timer < _fadeDuration)
        {
            _notificationCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / _fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        _notificationCanvasGroup.alpha = 1f; // Ensure fully visible

        // Display Duration
        yield return new WaitForSeconds(_displayDuration);

        // Fade Out
        timer = 0f;
        while (timer < _fadeDuration)
        {
            _notificationCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        _notificationCanvasGroup.alpha = 0f; // Ensure fully transparent

        // Hide the GameObject after fading out
        _notificationCanvasGroup.gameObject.SetActive(false);
        _currentNotificationCoroutine = null; // Clear the coroutine reference
    }
}
