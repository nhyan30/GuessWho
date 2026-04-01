using UnityEngine;

/// <summary>
/// Manages all audio in the game including background music and sound effects.
/// Supports volume control for both music and SFX separately.
/// Persists between scenes using DontDestroyOnLoad.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;       // For background music
    [SerializeField] private AudioSource sfxSource;         // For sound effects

    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundMusic;     // Main menu & gameplay music
    [SerializeField] private AudioClip buttonClickSFX;      // Button click sound

    [Header("Default Volumes")]
    [SerializeField] private float defaultMusicVolume = 0.5f;
    [SerializeField] private float defaultSFXVolume = 0.7f;
    [SerializeField] private float gameplayMusicVolume = 0.3f;  // Reduced volume during gameplay

    // Volume properties
    private float musicVolume;
    private float sfxVolume;
    private bool isInGameplay = false;

    // Keys for PlayerPrefs
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Start playing background music
        PlayBackgroundMusic();
    }

    #endregion

    #region Initialization

    private void InitializeAudio()
    {
        // Create audio sources if not assigned
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // Load saved volumes or use defaults
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSFXVolume);

        // Apply volumes
        ApplyMusicVolume();
        sfxSource.volume = sfxVolume;

        // Assign audio clip
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
        }
    }

    #endregion

    #region Music Control

    /// <summary>
    /// Starts playing background music.
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Stops the background music.
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Pauses the background music.
    /// </summary>
    public void PauseBackgroundMusic()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
        }
    }

    /// <summary>
    /// Resumes the background music.
    /// </summary>
    public void ResumeBackgroundMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }

    /// <summary>
    /// Sets the music volume (0 to 1).
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    /// <summary>
    /// Applies the current music volume based on whether we're in gameplay or menu.
    /// </summary>
    private void ApplyMusicVolume()
    {
        if (musicSource != null)
        {
            // Reduce volume during gameplay
            float effectiveVolume = isInGameplay ? musicVolume * gameplayMusicVolume : musicVolume;
            musicSource.volume = effectiveVolume;
        }
    }

    /// <summary>
    /// Called when entering gameplay to reduce music volume.
    /// </summary>
    public void EnterGameplay()
    {
        isInGameplay = true;
        ApplyMusicVolume();
    }

    /// <summary>
    /// Called when returning to menu to restore music volume.
    /// </summary>
    public void ExitGameplay()
    {
        isInGameplay = false;
        ApplyMusicVolume();
    }

    #endregion

    #region SFX Control

    /// <summary>
    /// Plays a one-shot sound effect.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    /// <summary>
    /// Plays the button click sound effect.
    /// </summary>
    public void PlayButtonClick()
    {
        if (buttonClickSFX != null)
        {
            PlaySFX(buttonClickSFX);
        }
    }

    /// <summary>
    /// Sets the SFX volume (0 to 1).
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.Save();

        // SFX volume is applied when playing, no need to update source
    }

    #endregion

    #region Volume Getters

    /// <summary>
    /// Gets the current music volume (0 to 1).
    /// </summary>
    public float GetMusicVolume()
    {
        return musicVolume;
    }

    /// <summary>
    /// Gets the current SFX volume (0 to 1).
    /// </summary>
    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    #endregion
}