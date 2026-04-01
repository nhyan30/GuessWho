using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Settings panel with volume sliders for Music and SFX.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeText;
    [SerializeField] private TMP_Text sfxVolumeText;
    [SerializeField] private Button cancelButton;

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

    #endregion

    #region Show/Hide

    public void Show()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
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
    }

    public void Hide()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public bool IsVisible()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }

    #endregion
}