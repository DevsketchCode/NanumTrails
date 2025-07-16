using UnityEngine;
using UnityEngine.SceneManagement; // Required for SceneManager
using System.Collections; // Required for Coroutines
using UnityEngine.UI; // Required for CanvasGroup (for fading UI)

/// <summary>
/// Manages overall game state, including scene loading and application quitting.
/// This version is designed to be placed in EACH scene where its functionality is needed.
/// Includes scene fade-in/fade-out transitions.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Public static property to easily access the GameManager instance in the current scene.
    // Note: This will be null if no GameManager GameObject is in the current scene.
    public static GameManager Instance { get; private set; }

    [Header("Scene Transition Settings")]
    [Tooltip("The CanvasGroup of the UI panel used for fading (e.g., a black image with a CanvasGroup).")]
    [SerializeField] private CanvasGroup _fadePanelCanvasGroup;
    [Tooltip("The duration of the fade-in and fade-out transition (in seconds).")]
    [SerializeField] private float _fadeDuration = 1.0f;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Sets the static Instance reference for the current scene.
    /// </summary>
    private void Awake()
    {
        // Set the static Instance to this component.
        // This means each scene will have its own GameManager.Instance.
        if (Instance != null && Instance != this)
        {
            // Optional: If you want to strictly enforce one per scene,
            // you could destroy duplicates here, but typically for non-persisting
            // managers, you just let the new scene's instance take over.
            // For simplicity in this non-persisting setup, we'll assume
            // only one GameManager is manually placed per scene.
            Debug.LogWarning("GameManager: More than one GameManager found in the current scene. This might lead to unexpected behavior.");
        }
        Instance = this;
        Debug.Log($"GameManager: Instance set for scene: {gameObject.scene.name}");

        // Ensure the fade panel is initially fully transparent and inactive.
        // This initial state is important for when the scene first loads,
        // but it will be overridden by FadeInScene() if a transition is occurring.
        if (_fadePanelCanvasGroup != null)
        {
            _fadePanelCanvasGroup.alpha = 0f;
            _fadePanelCanvasGroup.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("GameManager: Fade Panel Canvas Group is not assigned! Scene transitions will not fade.");
        }
    }

    /// <summary>
    /// Start is called on the frame when a script is first enabled just before any Update methods are called the first time.
    /// This is a good place to initiate scene fade-in.
    /// </summary>
    private void Start()
    {
        // Call this to fade the scene in when it loads
        FadeInScene();
    }

    /// <summary>
    /// Loads a specified scene by its name with a fade transition.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load (e.g., "Level1", "Credits", "TitleScene").</param>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("GameManager: Attempted to load a scene with an empty or null name.");
            return;
        }

        // Start the scene loading coroutine with fade.
        StartCoroutine(LoadSceneWithFade(sceneName));
    }

    /// <summary>
    /// Coroutine to handle fading out, loading the new scene, and fading in.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        if (_fadePanelCanvasGroup == null)
        {
            Debug.LogWarning("GameManager: Fade Panel Canvas Group is null. Loading scene without fade.");
            SceneManager.LoadScene(sceneName);
            yield break; // Exit coroutine
        }

        // --- Fade Out ---
        Debug.Log($"GameManager: Fading out to load scene: {sceneName}...");
        yield return StartCoroutine(FadeCanvasGroup(_fadePanelCanvasGroup, 1f, _fadeDuration));

        // --- Load Scene ---
        Debug.Log($"GameManager: Loading scene: {sceneName}...");
        SceneManager.LoadScene(sceneName);

        // Wait for the next frame to ensure the new scene is loaded and its Awake/Start methods run
        yield return null;

        // The GameManager instance in the NEW scene will handle the fade-in via its Start() method.
    }

    /// <summary>
    /// Initiates the fade-in transition for the current scene.
    /// This should be called from the Start() method of the GameManager in each scene.
    /// </summary>
    public void FadeInScene()
    {
        if (_fadePanelCanvasGroup == null)
        {
            Debug.LogWarning("GameManager: Fade Panel Canvas Group is null. Cannot fade in scene.");
            return;
        }

        // NEW: Set alpha to 1 and activate the GameObject immediately before starting fade-in
        _fadePanelCanvasGroup.alpha = 1f;
        _fadePanelCanvasGroup.gameObject.SetActive(true);

        // Start fade-in from fully opaque (alpha 1) to transparent (alpha 0)
        StartCoroutine(FadeCanvasGroup(_fadePanelCanvasGroup, 0f, _fadeDuration));
        Debug.Log("GameManager: Fading in scene.");
    }


    /// <summary>
    /// Coroutine to smoothly change the alpha of a CanvasGroup.
    /// </summary>
    /// <param name="canvasGroup">The CanvasGroup to fade.</param>
    /// <param name="targetAlpha">The target alpha value (0 for transparent, 1 for opaque).</param>
    /// <param name="duration">The duration of the fade.</param>
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        // The activation logic has been moved to FadeInScene() for the fade-in case.
        // For fade-out, the panel is already active.
        // This 'if' check is still useful if you were to call FadeCanvasGroup directly
        // to fade something in from a completely inactive state.
        if (targetAlpha > startAlpha && !canvasGroup.gameObject.activeSelf)
        {
            canvasGroup.gameObject.SetActive(true);
        }

        while (timer < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = targetAlpha; // Ensure final alpha is exact

        // Deactivate the panel if it's fully transparent
        if (targetAlpha == 0f)
        {
            canvasGroup.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Quits the application.
    /// This will only work in a build; in the editor, it stops Play Mode.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("GameManager: Quitting game...");
#if UNITY_EDITOR
        // If running in the Unity editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // If running in a build
        Application.Quit();
#endif
    }
}
