using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using Networking;

/// <summary>
/// Controls the main menu with multiplayer support.
/// Handles single player, hosting games, joining games, settings, and help.
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
    [SerializeField] private Button settingsButton;    // New: Settings button
    [SerializeField] private Button helpButton;        // New: Help button

    [Header("Panels")]
    [SerializeField] private SettingsPanel settingsPanel;  // Reference to Settings panel
    [SerializeField] private HelpPanel helpPanel;          // Reference to Help panel

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

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.2f;

    // State
    public bool isMultiplayer = false;
    private Coroutine waitingCoroutine;

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
        Fade(HostGamePanel, false);
        Fade(joinGamePanel, false);

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

        // New: Settings and Help buttons
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

        Fade(mainMenuCanvasGroup, false, () =>
        {
            Fade(gameCanvasGroup, true, () =>
            {
                gameManager?.BeginGame(GameMode.SinglePlayer);
            });
        });
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

        Fade(HostGamePanel, false);
        Fade(joinGamePanel, false);

        Fade(mainMenuCanvasGroup, false, () =>
        {
            Fade(gameCanvasGroup, true, () =>
            {
                gameManager?.BeginGame(GameMode.Multiplayer);
            });
        });
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

    #region Fade Methods

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
                callback?.Invoke();
            });
    }
    #endregion

    #region Public Methods

    /// <summary>
    /// Returns to main menu from game.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("[Menu] Returning to main menu");

        isMultiplayer = false;

        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            NetworkManager.Instance.LeaveRoom();
        }

        Fade(gameCanvasGroup, false, () =>
        {
            Fade(mainMenuCanvasGroup, true, null);
        });
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