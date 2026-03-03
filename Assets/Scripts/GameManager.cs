using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Networking;

/// <summary>
/// GameManager controls the Guess Who game flow.
/// Supports both Single Player (vs AI) and Multiplayer modes.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Character Data")]
    [SerializeField] private List<SCR_Character> characterList = new List<SCR_Character>();
    [SerializeField] private Transform playerGrid;
    [SerializeField] private Transform opponentGrid;
    [SerializeField] private GameObject playerCellPrefab;
    [SerializeField] private GameObject opponentCellPrefab;

    [Header("Popup")]
    [SerializeField] private PopupController popup;

    [Header("Question Bar")]
    [SerializeField] private QuestionBarController questionBar;
    [SerializeField] private Button doAGuessButton;

    [Header("Player's Character Display")]
    [SerializeField] private Image selectedCharacterImage;
    [SerializeField] private TMP_Text selectedCharacterText;

    [Header("Opponent's Character Display")]
    [SerializeField] private Image opponentCharacterImage;
    [SerializeField] private TMP_Text opponentCharacterText;

    // Game mode
    private GameMode currentGameMode = GameMode.SinglePlayer;

    // Game state
    private GameState currentState;
    private SCR_Character playerSelectedCharacter;
    private SCR_Character opponentSelectedCharacter;
    private List<Cell> playerCells = new List<Cell>();
    private List<OpponentCell> opponentCells = new List<OpponentCell>();
    private List<SCR_Character> playerRemainingCharacters = new List<SCR_Character>();
    private SCR_Question currentQuestion;
    private bool isInGuessMode = false;
    private bool gameStarted = false;
    private bool isMyTurn = true;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        HideAllUI();
        SetupEventListeners();
    }

    private void Start()
    {
        // Game starts via BeginGame() when player clicks menu button
    }

    #endregion

    #region Game Flow Control

    /// <summary>
    /// Called by MainMenuController to start the game.
    /// </summary>
    public void BeginGame(GameMode mode)
    {
        currentGameMode = mode;
        gameStarted = true;
        isMyTurn = true; // Host/player always goes first

        Debug.Log($"[Game] Starting {mode} game");

        if (mode == GameMode.Multiplayer)
        {
            SetupMultiplayerEvents();
        }

        StartGame();
    }

    private void SetupMultiplayerEvents()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnOpponentCharacterSelected += OnOpponentCharacterSelected;
            NetworkManager.Instance.OnTurnChanged += OnTurnChanged;
            NetworkManager.Instance.OnAnswerReceived += OnAnswerReceived;
            NetworkManager.Instance.OnOpponentGuess += OnOpponentGuess;
            NetworkManager.Instance.OnGameOver += OnGameOver;
        }
    }

    private void RemoveMultiplayerEvents()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnOpponentCharacterSelected -= OnOpponentCharacterSelected;
            NetworkManager.Instance.OnTurnChanged -= OnTurnChanged;
            NetworkManager.Instance.OnAnswerReceived -= OnAnswerReceived;
            NetworkManager.Instance.OnOpponentGuess -= OnOpponentGuess;
            NetworkManager.Instance.OnGameOver -= OnGameOver;
        }
    }

    public void ReturnToMainMenu()
    {
        gameStarted = false;
        isMyTurn = true;
        RemoveMultiplayerEvents();

        popup?.Hide();
        HideAllUI();

        foreach (var cell in playerCells)
            cell.MarkAsEliminated(false);

        foreach (var cell in opponentCells)
            cell.MarkAsEliminated(false);

        QuestionManager.Instance?.ClearAskedHistory();
        AIController.Instance?.ResetAI();

        MainMenuController.Instance?.ReturnToMainMenu();
    }

    #endregion

    #region Initialization

    private void HideAllUI()
    {
        selectedCharacterImage?.gameObject.SetActive(false);
        opponentCharacterImage?.gameObject.SetActive(false);
        DisableQuestionBar();
    }

    private void SetupEventListeners()
    {
        if (popup != null)
        {
            popup.OnOkayClicked += OnPopupOkay;
            popup.OnNegateClicked += OnPopupNegate;
            popup.OnAnswerClicked += OnPopupAnswer;
        }

        if (questionBar != null)
        {
            questionBar.OnQuestionSent += OnQuestionSent;
        }

        if (doAGuessButton != null)
        {
            doAGuessButton.onClick.AddListener(OnDoAGuessPressed);
        }
    }

    private void StartGame()
    {
        playerRemainingCharacters = new List<SCR_Character>(characterList);
        playerSelectedCharacter = null;
        opponentSelectedCharacter = null;
        currentQuestion = null;
        isInGuessMode = false;

        FillPlayerGrid();
        FillOpponentGrid();

        if (currentGameMode == GameMode.SinglePlayer)
        {
            SelectAICharacter();
        }

        currentState = GameState.CharacterSelection;
        popup?.ShowCharacterSelect();
    }

    private void FillPlayerGrid()
    {
        playerCells.Clear();
        foreach (Transform child in playerGrid) Destroy(child.gameObject);

        for (int i = 0; i < characterList.Count; i++)
        {
            GameObject cellObj = Instantiate(playerCellPrefab, playerGrid, false);
            Cell cell = cellObj.GetComponent<Cell>();
            if (cell != null)
            {
                cell.SetCell(characterList[i]);
                playerCells.Add(cell);
            }
        }
    }

    private void FillOpponentGrid()
    {
        if (opponentGrid == null || opponentCellPrefab == null) return;

        opponentCells.Clear();
        foreach (Transform child in opponentGrid) Destroy(child.gameObject);

        for (int i = 0; i < characterList.Count; i++)
        {
            GameObject cellObj = Instantiate(opponentCellPrefab, opponentGrid, false);
            OpponentCell cell = cellObj.GetComponent<OpponentCell>();
            if (cell != null)
            {
                cell.SetCell(characterList[i]);
                opponentCells.Add(cell);
            }
        }
    }

    private void SelectAICharacter()
    {
        opponentSelectedCharacter = characterList[Random.Range(0, characterList.Count)];
        AIController.Instance?.Initialize(characterList, opponentSelectedCharacter);
        Debug.Log($"[Game] AI selected: {opponentSelectedCharacter.characterName}");
    }

    #endregion

    #region Popup Event Handlers

    private void OnPopupOkay()
    {
        PopupType popupType = popup.GetCurrentType();

        switch (popupType)
        {
            case PopupType.CharacterSelect:
                popup?.Hide();
                break;

            case PopupType.CharacterAgree:
                ConfirmCharacterSelection();
                break;

            case PopupType.QuestionSelect:
                popup?.Hide();
                EnableQuestionBar();
                break;

            case PopupType.AIAnswer:
                popup?.Hide();
                EliminateCharactersFromAnswer();

                if (currentGameMode == GameMode.SinglePlayer)
                    StartAITurn();
                else
                    StartOpponentTurn();
                break;

            case PopupType.GuessConfirm:
                ProcessPlayerGuess(popup.GetCurrentCharacter());
                break;

            case PopupType.GameOver:
                ReturnToMainMenu();
                break;

            case PopupType.Message:
                popup?.Hide();
                if (currentState == GameState.PlayerTurn && !isInGuessMode)
                {
                    EnableQuestionBar();
                }
                break;
        }
    }

    private void OnPopupNegate()
    {
        PopupType popupType = popup.GetCurrentType();

        switch (popupType)
        {
            case PopupType.CharacterAgree:
                popup?.Hide();
                currentState = GameState.CharacterSelection;
                break;

            case PopupType.GuessConfirm:
                popup?.Hide();
                isInGuessMode = false;
                currentState = GameState.PlayerTurn;
                EnableQuestionBar();
                break;
        }
    }

    private void OnPopupAnswer(bool answer)
    {
        if (currentState == GameState.AIQuestion)
        {
            popup?.Hide();
            QuestionManager.Instance?.MarkQuestionAsAskedAI(currentQuestion);

            if (currentGameMode == GameMode.SinglePlayer)
            {
                AIController.Instance?.ProcessPlayerAnswer(currentQuestion, answer);
                EliminateCharactersForOpponent(currentQuestion, answer);
            }
            else
            {
                NetworkManager.Instance?.SendAnswer(answer);
            }

            StartPlayerTurn();
        }
    }

    #endregion

    #region Character Selection

    public void OnCharacterSelected(SCR_Character character)
    {
        if (currentState != GameState.CharacterSelection) return;

        playerSelectedCharacter = character;
        currentState = GameState.CharacterAgree;
        popup?.ShowCharacterAgree(character);
    }

    private void ConfirmCharacterSelection()
    {
        popup?.Hide();

        if (selectedCharacterImage != null)
        {
            selectedCharacterImage.sprite = playerSelectedCharacter.characterSprite;
            selectedCharacterImage.gameObject.SetActive(true);
        }
        if (selectedCharacterText != null)
        {
            selectedCharacterText.text = playerSelectedCharacter.characterName;
        }

        // Send to opponent in multiplayer
        if (currentGameMode == GameMode.Multiplayer && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendCharacterSelected(playerSelectedCharacter.characterName);
        }

        StartPlayerTurn();
    }

    private void OnOpponentCharacterSelected(string characterName)
    {
        opponentSelectedCharacter = characterList.Find(c => c.characterName == characterName);
        Debug.Log($"[Game] Opponent selected: {characterName}");
    }

    #endregion

    #region Cell Click Handler

    public void OnCellClicked(SCR_Character character)
    {
        if (currentState == GameState.CharacterSelection)
        {
            OnCharacterSelected(character);
        }
        else if (currentState == GameState.PlayerTurn && isInGuessMode)
        {
            currentState = GameState.GuessConfirm;
            popup?.ShowGuessConfirm(character);
        }
    }

    #endregion

    #region Player Turn

    private void StartPlayerTurn()
    {
        currentState = GameState.PlayerTurn;
        isMyTurn = true;
        isInGuessMode = false;
        popup?.ShowQuestionSelect();
    }

    private void EnableQuestionBar()
    {
        questionBar?.SetVisible(true);
        questionBar?.SetActive(true);
        questionBar?.UpdateDisplay();

        if (doAGuessButton != null)
            doAGuessButton.interactable = true;
    }

    private void DisableQuestionBar()
    {
        questionBar?.SetActive(false);
        if (doAGuessButton != null)
            doAGuessButton.interactable = false;
    }

    private void OnQuestionSent(SCR_Question question)
    {
        if (currentState != GameState.PlayerTurn) return;

        currentQuestion = question;
        QuestionManager.Instance?.MarkQuestionAsAsked(question);

        StartCoroutine(ProcessPlayerQuestion());
    }

    private IEnumerator ProcessPlayerQuestion()
    {
        DisableQuestionBar();
        isInGuessMode = false;

        popup?.ShowAIThinking();
        yield return new WaitForSeconds(1f);

        if (currentGameMode == GameMode.SinglePlayer)
        {
            bool answer = currentQuestion.MatchesCharacter(opponentSelectedCharacter);
            popup?.ShowAIAnswer(answer);
        }
        else
        {
            // In multiplayer, wait for opponent's answer
            bool answer = currentQuestion.MatchesCharacter(opponentSelectedCharacter);
            popup?.ShowAIAnswer(answer);

            // Send answer to opponent
            NetworkManager.Instance?.SendAnswer(answer);
        }
    }

    private void EliminateCharactersFromAnswer()
    {
        if (currentQuestion == null) return;

        bool answerIsYes = popup.GetLastAnswer();

        var toEliminate = CharacterFilter.GetCharactersToEliminate(
            playerRemainingCharacters, currentQuestion, answerIsYes);

        foreach (var cell in playerCells)
        {
            if (toEliminate.Contains(cell.Character))
            {
                cell.MarkAsEliminated(true);
            }
        }

        playerRemainingCharacters = CharacterFilter.GetRemainingCharacters(
            playerRemainingCharacters, currentQuestion, answerIsYes);

        Debug.Log($"[Game] {playerRemainingCharacters.Count} characters remaining");
    }

    private void OnDoAGuessPressed()
    {
        isInGuessMode = true;
        DisableQuestionBar();
        popup?.ShowMessage("Click on a character to make your guess!");
    }

    private void ProcessPlayerGuess(SCR_Character guessedCharacter)
    {
        StartCoroutine(ProcessGuessCoroutine(guessedCharacter));
    }

    private IEnumerator ProcessGuessCoroutine(SCR_Character guessedCharacter)
    {
        popup?.Hide();
        isInGuessMode = false;

        bool isCorrect = guessedCharacter == opponentSelectedCharacter;

        popup?.ShowMessage(isCorrect
            ? $"Correct! It was {guessedCharacter.characterName}!"
            : $"Wrong! It was {opponentSelectedCharacter.characterName}!");

        // In multiplayer, send guess result
        if (currentGameMode == GameMode.Multiplayer && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendGameOver(isCorrect, guessedCharacter.characterName);
        }

        yield return new WaitForSeconds(2f);

        if (isCorrect)
            PlayerWins();
        else
            PlayerLoses();
    }

    #endregion

    #region Opponent/AI Turn

    private void StartOpponentTurn()
    {
        currentState = GameState.AITurn;
        isInGuessMode = false;
        isMyTurn = false;

        if (currentGameMode == GameMode.SinglePlayer)
        {
            StartCoroutine(AITurnCoroutine());
        }
        else
        {
            // In multiplayer, wait for opponent
            Debug.Log("[Game] Waiting for opponent's turn");
        }
    }

    private void StartAITurn()
    {
        currentState = GameState.AITurn;
        isInGuessMode = false;
        StartCoroutine(AITurnCoroutine());
    }

    private IEnumerator AITurnCoroutine()
    {
        popup?.ShowAIThinking();
        yield return new WaitForSeconds(1.5f);

        if (AIController.Instance?.RemainingPossibleCharacters == 1)
        {
            SCR_Character guess = AIController.Instance.GetMostLikelyCharacter();
            ProcessAIGuess(guess);
        }
        else
        {
            SCR_Question aiQuestion = AIController.Instance?.SelectQuestion();
            if (aiQuestion != null)
            {
                currentQuestion = aiQuestion;
                currentState = GameState.AIQuestion;

                bool correctIsYes = aiQuestion.MatchesCharacter(playerSelectedCharacter);
                popup?.ShowAIQuestion(aiQuestion, correctIsYes);
            }
            else
            {
                SCR_Character guess = AIController.Instance?.GetMostLikelyCharacter();
                if (guess != null)
                    ProcessAIGuess(guess);
                else
                    StartPlayerTurn();
            }
        }
    }

    private void EliminateCharactersForOpponent(SCR_Question question, bool answerIsYes)
    {
        if (opponentCells.Count == 0) return;

        var opponentRemaining = new List<SCR_Character>(characterList);

        foreach (var cell in opponentCells)
        {
            if (cell.IsEliminated)
                opponentRemaining.Remove(cell.Character);
        }

        var toEliminate = CharacterFilter.GetCharactersToEliminate(opponentRemaining, question, answerIsYes);

        foreach (var cell in opponentCells)
        {
            if (toEliminate.Contains(cell.Character))
            {
                cell.MarkAsEliminated(true);
            }
        }

        Debug.Log($"[Game] Opponent eliminated {toEliminate.Count} characters");
    }

    private void ProcessAIGuess(SCR_Character guessedCharacter)
    {
        bool isCorrect = guessedCharacter == playerSelectedCharacter;

        if (isCorrect)
        {
            popup?.ShowMessage($"AI guessed: {guessedCharacter.characterName}\n\nAI Wins!");
            Invoke(nameof(PlayerLoses), 2f);
        }
        else
        {
            popup?.ShowMessage($"AI guessed wrong: {guessedCharacter.characterName}");
            Invoke(nameof(StartPlayerTurn), 2f);
        }
    }

    #endregion

    #region Multiplayer Events

    private void OnTurnChanged(bool myTurn)
    {
        if (myTurn)
        {
            StartPlayerTurn();
        }
        else
        {
            StartOpponentTurn();
        }
    }

    private void OnAnswerReceived(bool answer)
    {
        // Received answer from opponent
        Debug.Log($"[Game] Opponent answer: {answer}");
    }

    private void OnOpponentGuess(string characterId)
    {
        SCR_Character guessed = characterList.Find(c => c.characterName == characterId);
        if (guessed == playerSelectedCharacter)
        {
            // Opponent guessed correctly
            popup?.ShowMessage($"Opponent guessed: {characterId}\n\nYou Lose!");
            Invoke(nameof(PlayerLoses), 2f);
        }
        else
        {
            // Opponent guessed wrong
            popup?.ShowMessage($"Opponent guessed wrong: {characterId}");
            Invoke(nameof(StartPlayerTurn), 2f);
        }
    }

    private void OnGameOver(bool iWon, string winnerCharacterId)
    {
        if (iWon)
            PlayerWins();
        else
            PlayerLoses();
    }

    #endregion

    #region Win/Loss

    private void PlayerWins()
    {
        currentState = GameState.GameOver;
        ShowOpponentCharacter();
        popup?.ShowGameOver(true, opponentSelectedCharacter);
    }

    private void PlayerLoses()
    {
        currentState = GameState.GameOver;
        ShowOpponentCharacter();
        popup?.ShowGameOver(false, opponentSelectedCharacter);
    }

    private void ShowOpponentCharacter()
    {
        if (opponentCharacterImage != null && opponentSelectedCharacter != null)
        {
            opponentCharacterImage.sprite = opponentSelectedCharacter.characterSprite;
            opponentCharacterImage.gameObject.SetActive(true);
        }
        if (opponentCharacterText != null && opponentSelectedCharacter != null)
        {
            opponentCharacterText.text = opponentSelectedCharacter.characterName;
        }
    }

    #endregion

    #region Public API

    public GameState GetGameState() => currentState;
    public bool IsInGuessMode => isInGuessMode;
    public bool IsMyTurn => isMyTurn;
    public GameMode CurrentGameMode => currentGameMode;
    public SCR_Character GetPlayerCharacter() => playerSelectedCharacter;
    public SCR_Character GetOpponentCharacter() => opponentSelectedCharacter;

    #endregion
}

public enum GameState
{
    CharacterSelection,
    CharacterAgree,
    PlayerTurn,
    AITurn,
    AIQuestion,
    GuessConfirm,
    GameOver
}
