using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Settings panel with volume sliders for Music and SFX.
/// Supports both Main Menu and Gameplay contexts.
/// In Gameplay context, shows an additional Return button to exit to Main Menu.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup settingsPanel;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeText;
    [SerializeField] private TMP_Text sfxVolumeText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button returnButton;        // Return to Main Menu (for gameplay context)
    [SerializeField] private GameObject returnButtonContainer;  // Container to show/hide return button

    [Header("Context")]
    [SerializeField] private bool isGameplayContext = false;  // Set to true for gameplay settings panel

    // Events
    public event System.Action OnReturnToMainMenu;

    private void Awake()
    {
        SetupSliderListeners();
        SetupButtonListeners();
    }

    private void Start()
    {
        // Initialize sliders with current volume values
        if (AudioManager.Instance != null)
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
                UpdateMusicVolumeText(musicVolumeSlider.value);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
                UpdateSFXVolumeText(sfxVolumeSlider.value);
            }
        }

        // Setup return button visibility based on context
        UpdateReturnButtonVisibility();

        // Start hidden
        Hide();
    }

    private void SetupSliderListeners()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    private void SetupButtonListeners()
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        if (returnButton != null)
        {
            returnButton.onClick.AddListener(OnReturnClicked);
        }
    }

    #region Slider Handlers

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
        UpdateMusicVolumeText(value);

        // Play button sound
        AudioManager.Instance?.PlayButtonClick();
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
        UpdateSFXVolumeText(value);

        // Play button sound to demonstrate SFX volume
        AudioManager.Instance?.PlayButtonClick();
    }

    private void UpdateMusicVolumeText(float value)
    {
        if (musicVolumeText != null)
        {
            int percentage = Mathf.RoundToInt(value * 100);
            musicVolumeText.text = $"{percentage}%";
        }
    }

    private void UpdateSFXVolumeText(float value)
    {
        if (sfxVolumeText != null)
        {
            int percentage = Mathf.RoundToInt(value * 100);
            sfxVolumeText.text = $"{percentage}%";
        }
    }

    #endregion

    #region Button Handlers

    private void OnCancelClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Hide();
    }

    private void OnReturnClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Hide();

        // Notify listeners that player wants to return to main menu
        OnReturnToMainMenu?.Invoke();

        // Direct call to GameManager if available
        if (isGameplayContext && GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
    }

    #endregion

    #region Show/Hide

    public void Show()
    {
        if (settingsPanel != null)
        {
            settingsPanel.alpha = 1f;
            settingsPanel.blocksRaycasts = true;
            settingsPanel.interactable = true;
        }

        // Refresh slider values
        if (AudioManager.Instance != null)
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
            }
        }

        // Update return button visibility
        UpdateReturnButtonVisibility();
    }

    public void Hide()
    {
        if (settingsPanel != null)
        {
            settingsPanel.alpha = 0f;
            settingsPanel.blocksRaycasts = false;
            settingsPanel.interactable = false;
        }
    }

    /// <summary>
    /// Sets whether this panel is in gameplay context.
    /// In gameplay context, the Return button will be visible.
    /// </summary>
    public void SetGameplayContext(bool isGameplay)
    {
        isGameplayContext = isGameplay;
        UpdateReturnButtonVisibility();
    }

    private void UpdateReturnButtonVisibility()
    {
        if (returnButtonContainer != null)
        {
            returnButtonContainer.SetActive(isGameplayContext);
        }
        else if (returnButton != null)
        {
            returnButton.gameObject.SetActive(isGameplayContext);
        }
    }

    #endregion
}