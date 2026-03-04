using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// Controls the main menu with multiplayer support.
/// Handles single player, hosting games, and joining games.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    [SerializeField] private CanvasGroup gameCanvasGroup;
    [SerializeField] private CanvasGroup findGamePanel;
    [SerializeField] private CanvasGroup joinGamePanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button findGameButton;
    [SerializeField] private Button joinGameButton;

    [Header("Find Game Panel")]
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text ipHintText;  // Shows IP address hint
    [SerializeField] private TMP_Text waitingText;
    [SerializeField] private Button cancelFindButton;

    [Header("Join Game Panel")]
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private Button joinConfirmButton;
    [SerializeField] private Button cancelJoinButton;
    [SerializeField] private TMP_Text joinErrorText;
    [SerializeField] private TMP_Text joinHintText;  // Hint text for joining

    [Header("Game Reference")]
    [SerializeField] private GameManager gameManager;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.2f;

    // State
    private bool isMultiplayer = false;
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
        HidePanel(findGamePanel);
        HidePanel(joinGamePanel);

        // Clear error text
        if (joinErrorText != null)
            joinErrorText.text = "";
    }

    private void SetupButtonListeners()
    {
        // Main menu buttons
        if (singlePlayerButton != null)
            singlePlayerButton.onClick.AddListener(OnSinglePlayerClicked);

        if (findGameButton != null)
            findGameButton.onClick.AddListener(OnFindGameClicked);

        if (joinGameButton != null)
            joinGameButton.onClick.AddListener(OnJoinGameClicked);

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
        isMultiplayer = true;
        Debug.Log("[Menu] Finding game - creating room");

        // Show find game panel
        ShowPanel(findGamePanel);

        // Create room via network manager
        if (Networking.NetworkManager.Instance != null)
        {
            Networking.NetworkManager.Instance.OnRoomCreated += OnRoomCreated;
            Networking.NetworkManager.Instance.OnGameReady += OnGameReady;
            Networking.NetworkManager.Instance.OnError += OnNetworkError;
            Networking.NetworkManager.Instance.CreateRoom();
        }
    }

    private void OnJoinGameClicked()
    {
        isMultiplayer = true;
        Debug.Log("[Menu] Join game - showing input");

        // Clear previous input
        if (codeInputField != null)
            codeInputField.text = "";

        if (joinErrorText != null)
            joinErrorText.text = "";

        // Show hint about same network requirement
        if (joinHintText != null)
            joinHintText.text = "Make sure you're on the same WiFi network as the host";

        // Show join game panel
        ShowPanel(joinGamePanel);
    }

    private void OnCancelFindClicked()
    {
        Debug.Log("[Menu] Cancelled finding game");

        if (Networking.NetworkManager.Instance != null)
        {
            Networking.NetworkManager.Instance.OnRoomCreated -= OnRoomCreated;
            Networking.NetworkManager.Instance.OnGameReady -= OnGameReady;
            Networking.NetworkManager.Instance.OnError -= OnNetworkError;
            Networking.NetworkManager.Instance.LeaveRoom();
        }

        if (waitingCoroutine != null)
        {
            StopCoroutine(waitingCoroutine);
            waitingCoroutine = null;
        }

        HidePanel(findGamePanel);
    }

    private void OnJoinConfirmClicked()
    {
        string code = codeInputField?.text ?? "";

        if (code.Length != 6)
        {
            if (joinErrorText != null)
                joinErrorText.text = "Please enter a 6-digit code";
            return;
        }

        Debug.Log($"[Menu] Attempting to join room: {code}");

        if (Networking.NetworkManager.Instance != null)
        {
            Networking.NetworkManager.Instance.OnRoomJoined += OnRoomJoined;
            Networking.NetworkManager.Instance.OnGameReady += OnGameReady;
            Networking.NetworkManager.Instance.OnError += OnNetworkError;
            Networking.NetworkManager.Instance.JoinRoom(code);
        }
    }

    private void OnCancelJoinClicked()
    {
        Debug.Log("[Menu] Cancelled joining game");

        if (Networking.NetworkManager.Instance != null)
        {
            Networking.NetworkManager.Instance.OnRoomJoined -= OnRoomJoined;
            Networking.NetworkManager.Instance.OnGameReady -= OnGameReady;
            Networking.NetworkManager.Instance.OnError -= OnNetworkError;
            Networking.NetworkManager.Instance.LeaveRoom();
        }

        HidePanel(joinGamePanel);
    }

    private void OnCodeInputChanged(string value)
    {
        // Enable/disable join button based on input length
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

        // Show IP hint for debugging/verification
        if (ipHintText != null && Networking.NetworkManager.Instance != null)
        {
            ipHintText.text = $"IP: {Networking.NetworkManager.Instance.HostIPAddress}";
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

        // Hide join panel, show waiting
        HidePanel(joinGamePanel);
        ShowPanel(findGamePanel);

        if (roomCodeText != null)
            roomCodeText.text = "Connected!";

        waitingCoroutine = StartCoroutine(WaitingAnimation());
    }

    private string currentRoomCode;

    private void OnGameReady()
    {
        Debug.Log("[Menu] Game ready!");

        // Clean up network events
        if (Networking.NetworkManager.Instance != null)
        {
            Networking.NetworkManager.Instance.OnRoomCreated -= OnRoomCreated;
            Networking.NetworkManager.Instance.OnRoomJoined -= OnRoomJoined;
            Networking.NetworkManager.Instance.OnGameReady -= OnGameReady;
            Networking.NetworkManager.Instance.OnError -= OnNetworkError;
        }

        if (waitingCoroutine != null)
        {
            StopCoroutine(waitingCoroutine);
            waitingCoroutine = null;
        }

        // Transition to game
        HidePanel(findGamePanel);
        HidePanel(joinGamePanel);

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

    private void ShowPanel(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.gameObject.SetActive(true);
        panel.alpha = 1f;
        panel.blocksRaycasts = true;
    }

    private void HidePanel(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.alpha = 0f;
        panel.blocksRaycasts = false;
        panel.gameObject.SetActive(false);
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

        // Leave network room if in multiplayer
        if (Networking.NetworkManager.Instance != null && Networking.NetworkManager.Instance.IsConnected)
        {
            Networking.NetworkManager.Instance.LeaveRoom();
        }

        // Fade back to menu
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
