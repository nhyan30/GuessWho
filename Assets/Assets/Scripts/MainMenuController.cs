using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using Networking;

/// <summary>
/// Controls the main menu with multiplayer support.
/// Handles single player, hosting games, joining games, settings, and help.
/// Uses circle wipe animation for page transitions.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    [SerializeField] private CanvasGroup gameCanvasGroup;
    [SerializeField] private CanvasGroup HostGamePanel;
    [SerializeField] private CanvasGroup joinGamePanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button HostGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button helpButton;

    [Header("Panels")]
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private HelpPanel helpPanel;

    [Header("Find Game Panel")]
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text ipHintText;
    [SerializeField] private TMP_Text waitingText;
    [SerializeField] private Button cancelFindButton;

    [Header("Join Game Panel")]
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private Button joinConfirmButton;
    [SerializeField] private Button cancelJoinButton;
    [SerializeField] private TMP_Text joinErrorText;
    [SerializeField] private TMP_Text joinHintText;

    [Header("Game Reference")]
    [SerializeField] private GameManager gameManager;

    [Header("Wipe Animation")]
    [SerializeField] private WipeController wipeController;
    [SerializeField] private bool useWipeAnimation = true;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.2f;

    // State
    public bool isMultiplayer = false;
    private Coroutine waitingCoroutine;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeUI();
        SetupButtonListeners();
    }

    private void InitializeUI()
    {
        // Main menu visible
        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 1f;
            mainMenuCanvasGroup.blocksRaycasts = true;
        }

        // Game hidden
        if (gameCanvasGroup != null)
        {
            gameCanvasGroup.alpha = 0f;
            gameCanvasGroup.blocksRaycasts = false;
        }

        // Panels hidden
        FadeImmediate(HostGamePanel, false);
        FadeImmediate(joinGamePanel, false);

        // Clear error text
        if (joinErrorText != null)
            joinErrorText.text = "";
    }

    private void SetupButtonListeners()
    {
        // Main menu buttons
        if (singlePlayerButton != null)
            singlePlayerButton.onClick.AddListener(OnSinglePlayerClicked);

        if (HostGameButton != null)
            HostGameButton.onClick.AddListener(OnFindGameClicked);

        if (joinGameButton != null)
            joinGameButton.onClick.AddListener(OnJoinGameClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (helpButton != null)
            helpButton.onClick.AddListener(OnHelpClicked);

        // Find game panel
        if (cancelFindButton != null)
            cancelFindButton.onClick.AddListener(OnCancelFindClicked);

        // Join game panel
        if (joinConfirmButton != null)
            joinConfirmButton.onClick.AddListener(OnJoinConfirmClicked);

        if (cancelJoinButton != null)
            cancelJoinButton.onClick.AddListener(OnCancelJoinClicked);

        // Input field
        if (codeInputField != null)
        {
            codeInputField.characterLimit = 6;
            codeInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            codeInputField.onValueChanged.AddListener(OnCodeInputChanged);
        }
    }

    #region Button Handlers

    private void OnSinglePlayerClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        isMultiplayer = false;
        Debug.Log("[Menu] Starting single player game");

        TransitionToGameplay(GameMode.SinglePlayer);
    }

    private void OnFindGameClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        isMultiplayer = true;
        Debug.Log("[Menu] Finding game - creating room");

        Fade(HostGamePanel, true);

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRoomCreated += OnRoomCreated;
            NetworkManager.Instance.OnGameReady += OnGameReady;
            NetworkManager.Instance.OnError += OnNetworkError;
            NetworkManager.Instance.CreateRoom();
        }
    }

    private void OnJoinGameClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        isMultiplayer = true;
        Debug.Log("[Menu] Join game - showing input");

        if (codeInputField != null)
            codeInputField.text = "";

        if (joinErrorText != null)
            joinErrorText.text = "";

        if (joinHintText != null)
            joinHintText.text = "Make sure you're on the same WiFi network as the host";

        Fade(joinGamePanel, true);
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (settingsPanel != null)
        {
            settingsPanel.Show();
        }
    }

    private void OnHelpClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (helpPanel != null)
        {
            helpPanel.Show();
        }
    }

    private void OnCancelFindClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        Debug.Log("[Menu] Cancelled finding game");

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRoomCreated -= OnRoomCreated;
            NetworkManager.Instance.OnGameReady -= OnGameReady;
            NetworkManager.Instance.OnError -= OnNetworkError;
            NetworkManager.Instance.LeaveRoom();
        }

        if (waitingCoroutine != null)
        {
            StopCoroutine(waitingCoroutine);
            waitingCoroutine = null;
        }

        Fade(HostGamePanel, false);
    }

    private void OnJoinConfirmClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        string code = codeInputField?.text ?? "";

        if (code.Length != 6)
        {
            if (joinErrorText != null)
                joinErrorText.text = "Please enter a 6-digit code";
            return;
        }

        Debug.Log($"[Menu] Attempting to join room: {code}");

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRoomJoined += OnRoomJoined;
            NetworkManager.Instance.OnGameReady += OnGameReady;
            NetworkManager.Instance.OnError += OnNetworkError;
            NetworkManager.Instance.JoinRoom(code);
        }
    }

    private void OnCancelJoinClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        Debug.Log("[Menu] Cancelled joining game");

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRoomJoined -= OnRoomJoined;
            NetworkManager.Instance.OnGameReady -= OnGameReady;
            NetworkManager.Instance.OnError -= OnNetworkError;
            NetworkManager.Instance.LeaveRoom();
        }

        Fade(joinGamePanel, false);
    }

    private void OnCodeInputChanged(string value)
    {
        if (joinConfirmButton != null)
        {
            joinConfirmButton.interactable = value.Length == 6;
        }
    }

    #endregion

    #region Network Events

    private void OnRoomCreated(string roomCode)
    {
        Debug.Log($"[Menu] Room created: {roomCode}");

        if (roomCodeText != null)
            roomCodeText.text = roomCode;

        if (ipHintText != null && NetworkManager.Instance != null)
        {
            ipHintText.text = $"IP: {NetworkManager.Instance.HostIPAddress}";
        }

        waitingCoroutine = StartCoroutine(WaitingAnimation());
    }

    private void OnRoomJoined(bool success)
    {
        if (!success)
        {
            if (joinErrorText != null)
                joinErrorText.text = "Could not join room. Check if host is on same network.";
            return;
        }

        Debug.Log("[Menu] Successfully joined room");
        currentRoomCode = codeInputField?.text ?? "";

        Fade(joinGamePanel, false);
        Fade(HostGamePanel, true);

        if (roomCodeText != null)
            roomCodeText.text = "Connected!";

        waitingCoroutine = StartCoroutine(WaitingAnimation());
    }

    private string currentRoomCode;

    private void OnGameReady()
    {
        Debug.Log("[Menu] Game ready!");

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRoomCreated -= OnRoomCreated;
            NetworkManager.Instance.OnRoomJoined -= OnRoomJoined;
            NetworkManager.Instance.OnGameReady -= OnGameReady;
            NetworkManager.Instance.OnError -= OnNetworkError;
        }

        if (waitingCoroutine != null)
        {
            StopCoroutine(waitingCoroutine);
            waitingCoroutine = null;
        }

        FadeImmediate(HostGamePanel, false);
        FadeImmediate(joinGamePanel, false);

        TransitionToGameplay(GameMode.Multiplayer);
    }

    private void OnNetworkError(string error)
    {
        Debug.LogError($"[Menu] Network error: {error}");

        if (joinErrorText != null)
            joinErrorText.text = error;

        if (waitingCoroutine != null)
        {
            StopCoroutine(waitingCoroutine);
            waitingCoroutine = null;
        }
    }

    private IEnumerator WaitingAnimation()
    {
        int dots = 0;
        while (true)
        {
            dots = (dots + 1) % 4;
            if (waitingText != null)
                waitingText.text = "Waiting for player to join" + new string('.', dots);
            yield return new WaitForSeconds(0.5f);
        }
    }

    #endregion

    #region Page Transitions with Wipe Animation

    /// <summary>
    /// Transitions from Main Menu to Gameplay with wipe animation.
    /// </summary>
    private void TransitionToGameplay(GameMode mode)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (useWipeAnimation && wipeController != null)
        {
            // Use wipe animation transition
            wipeController.PlayTransition(
                onSwitchContent: () =>
                {
                    // Hide main menu, show gameplay
                    SetCanvasGroupVisible(mainMenuCanvasGroup, false);
                    SetCanvasGroupVisible(gameCanvasGroup, true);

                    // Start the game
                    gameManager?.BeginGame(mode);
                },
                onComplete: () =>
                {
                    isTransitioning = false;
                }
            );
        }
        else
        {
            // Fallback to fade transition
            Fade(mainMenuCanvasGroup, false, () =>
            {
                Fade(gameCanvasGroup, true, () =>
                {
                    gameManager?.BeginGame(mode);
                    isTransitioning = false;
                });
            });
        }
    }

    /// <summary>
    /// Returns to main menu from game with wipe animation.
    /// Called by GameManager when returning to main menu.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("[Menu] Returning to main menu");

        if (isTransitioning)
        {
            // Force immediate transition if already transitioning
            isTransitioning = false;
        }

        isMultiplayer = false;

        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            NetworkManager.Instance.LeaveRoom();
        }

        if (useWipeAnimation && wipeController != null)
        {
            // Use wipe animation transition
            wipeController.PlayTransition(
                onSwitchContent: () =>
                {
                    // Hide gameplay, show main menu
                    SetCanvasGroupVisible(gameCanvasGroup, false);
                    SetCanvasGroupVisible(mainMenuCanvasGroup, true);
                },
                onComplete: () =>
                {
                    isTransitioning = false;
                }
            );
        }
        else
        {
            // Fallback to fade transition
            Fade(gameCanvasGroup, false, () =>
            {
                Fade(mainMenuCanvasGroup, true, null);
                isTransitioning = false;
            });
        }
    }

    #endregion

    #region Fade Methods

    private void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    public void Fade(CanvasGroup canvasGroup, bool visible, UnityAction callback = null)
    {
        if (canvasGroup == null)
        {
            callback?.Invoke();
            return;
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOFade(visible ? 1 : 0, fadeDuration).SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                if (visible)
                    canvasGroup.blocksRaycasts = true;
                    canvasGroup.interactable = true;
                callback?.Invoke();
            });
    }

    private void FadeImmediate(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    #endregion
}

/// <summary>
/// Game mode enum for single player vs multiplayer.
/// </summary>
public enum GameMode
{
    SinglePlayer,
    Multiplayer
}