using UnityEngine;

/// <summary>
/// Manages background music and sound effects in the game.
/// Implements the Singleton pattern for easy access from other scripts.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Background Music Settings")]
    [Tooltip("The AudioSource component used for playing background music.")]
    [SerializeField] private AudioSource _musicAudioSource;
    [Tooltip("The background music clip to play.")]
    [SerializeField] private AudioClip _backgroundMusicClip;
    [Range(0f, 1f)]
    [Tooltip("The volume for the background music (0 = silent, 1 = full volume).")]
    [SerializeField] private float _musicVolume = 0.5f;

    [Header("Sound Effects Settings")]
    [Tooltip("The AudioSource component used for playing sound effects.")]
    [SerializeField] private AudioSource _sfxAudioSource;
    [Range(0f, 1f)]
    [Tooltip("The volume for sound effects (0 = silent, 1 = full volume).")]
    [SerializeField] private float _sfxVolume = 0.7f;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the singleton and sets up audio sources.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: 
            DontDestroyOnLoad(gameObject); // Consider if you want music to persist across scenes without interruption
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize music AudioSource
        if (_musicAudioSource == null)
        {
            _musicAudioSource = gameObject.AddComponent<AudioSource>();
            _musicAudioSource.loop = true; // Background music usually loops
            _musicAudioSource.playOnAwake = false; // Don't play until explicitly told
        }
        _musicAudioSource.volume = _musicVolume;
        _musicAudioSource.clip = _backgroundMusicClip;

        // Initialize SFX AudioSource
        if (_sfxAudioSource == null)
        {
            _sfxAudioSource = gameObject.AddComponent<AudioSource>();
            _sfxAudioSource.loop = false; // Sound effects usually don't loop
            _sfxAudioSource.playOnAwake = false;
        }
        _sfxAudioSource.volume = _sfxVolume;
    }

    /// <summary>
    /// Start is called on the frame when a script is first enabled.
    /// Starts playing the background music.
    /// </summary>
    private void Start()
    {
        PlayBackgroundMusic();
    }

    /// <summary>
    /// Starts playing the assigned background music clip.
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (_musicAudioSource != null && _backgroundMusicClip != null)
        {
            if (!_musicAudioSource.isPlaying)
            {
                _musicAudioSource.Play();
                Debug.Log("AudioManager: Playing background music.");
            }
        }
        else
        {
            Debug.LogWarning("AudioManager: Background music AudioSource or AudioClip not assigned. Cannot play music.");
        }
    }

    /// <summary>
    /// Stops the currently playing background music.
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (_musicAudioSource != null && _musicAudioSource.isPlaying)
        {
            _musicAudioSource.Stop();
            Debug.Log("AudioManager: Stopping background music.");
        }
    }

    /// <summary>
    /// Plays a one-shot sound effect.
    /// </summary>
    /// <param name="clip">The AudioClip to play as a sound effect.</param>
    public void PlaySFX(AudioClip clip)
    {
        if (_sfxAudioSource != null && clip != null)
        {
            _sfxAudioSource.PlayOneShot(clip, _sfxVolume);
            Debug.Log($"AudioManager: Playing SFX: {clip.name}");
        }
        else
        {
            Debug.LogWarning("AudioManager: SFX AudioSource or AudioClip not assigned. Cannot play SFX.");
        }
    }

    /// <summary>
    /// Adjusts the background music volume.
    /// </summary>
    /// <param name="volume">The new volume (0f to 1f).</param>
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        if (_musicAudioSource != null)
        {
            _musicAudioSource.volume = _musicVolume;
        }
    }

    /// <summary>
    /// Adjusts the sound effects volume.
    /// </summary>
    /// <param name="volume">The new volume (0f to 1f).</param>
    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        if (_sfxAudioSource != null)
        {
            _sfxAudioSource.volume = _sfxVolume;
        }
    }
}
