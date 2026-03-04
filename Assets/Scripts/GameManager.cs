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

    // Multiplayer turn tracking
    private bool isMyTurn = true;
    private bool opponentCharacterPicked = false;
    private bool myCharacterPicked = false;
    private SCR_Question opponentQuestion; // Question received from opponent
    private bool waitingForOpponentAnswer = false;

    // Track eliminations for opponent grid sync
    private HashSet<string> opponentEliminatedCharacters = new HashSet<string>();

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
        myCharacterPicked = false;
        opponentCharacterPicked = false;
        opponentEliminatedCharacters.Clear();

        Debug.Log($"[Game] Starting {mode} game");

        if (mode == GameMode.Multiplayer)
        {
            // Host always goes first
            isMyTurn = NetworkManager.Instance.IsHost;
            SetupMultiplayerEvents();
        }
        else
        {
            isMyTurn = true;
        }

        StartGame();
    }

    private void SetupMultiplayerEvents()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnOpponentCharacterSelected += OnOpponentCharacterSelected;
            NetworkManager.Instance.OnQuestionReceived += OnQuestionReceivedFromOpponent;
            NetworkManager.Instance.OnAnswerReceived += OnAnswerReceivedFromOpponent;
            NetworkManager.Instance.OnEliminationReceived += OnEliminationReceivedFromOpponent;
            NetworkManager.Instance.OnOpponentGuess += OnOpponentGuess;
            NetworkManager.Instance.OnGameOver += OnGameOver;
            NetworkManager.Instance.OnPlayerLeft += OnOpponentDisconnected;
        }
    }

    private void RemoveMultiplayerEvents()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnOpponentCharacterSelected -= OnOpponentCharacterSelected;
            NetworkManager.Instance.OnQuestionReceived -= OnQuestionReceivedFromOpponent;
            NetworkManager.Instance.OnAnswerReceived -= OnAnswerReceivedFromOpponent;
            NetworkManager.Instance.OnEliminationReceived -= OnEliminationReceivedFromOpponent;
            NetworkManager.Instance.OnOpponentGuess -= OnOpponentGuess;
            NetworkManager.Instance.OnGameOver -= OnGameOver;
            NetworkManager.Instance.OnPlayerLeft -= OnOpponentDisconnected;
        }
    }

    public void ReturnToMainMenu()
    {
        gameStarted = false;
        isMyTurn = true;
        myCharacterPicked = false;
        opponentCharacterPicked = false;
        waitingForOpponentAnswer = false;
        opponentEliminatedCharacters.Clear();
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
                if (currentGameMode == GameMode.Multiplayer && !isMyTurn)
                {
                    popup?.ShowMessage("Wait for your turn!");
                }
                else
                {
                    EnableQuestionBar();
                }
                break;

            case PopupType.AIAnswer:
                popup?.Hide();
                EliminateCharactersFromAnswer();

                if (currentGameMode == GameMode.SinglePlayer)
                {
                    StartAITurn();
                }
                else
                {
                    // In multiplayer, switch turns after seeing answer
                    SwitchTurns();
                }
                break;

            case PopupType.GuessConfirm:
                ProcessPlayerGuess(popup.GetCurrentCharacter());
                break;

            case PopupType.GameOver:
                ReturnToMainMenu();
                break;

            case PopupType.Message:
                popup?.Hide();
                // After message, check if it's our turn to ask
                if (currentState == GameState.PlayerTurn && !isInGuessMode && isMyTurn)
                {
                    popup?.ShowQuestionSelect();
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
                if (isMyTurn)
                {
                    EnableQuestionBar();
                }
                break;
        }
    }

    private void OnPopupAnswer(bool answer)
    {
        if (currentState == GameState.AIQuestion)
        {
            popup?.Hide();

            if (currentGameMode == GameMode.SinglePlayer)
            {
                QuestionManager.Instance?.MarkQuestionAsAskedAI(currentQuestion);
                AIController.Instance?.ProcessPlayerAnswer(currentQuestion, answer);
                EliminateCharactersForOpponent(currentQuestion, answer);
                StartPlayerTurn();
            }
            else
            {
                // Multiplayer: send answer back to opponent
                QuestionManager.Instance?.MarkQuestionAsAskedAI(opponentQuestion);
                EliminateCharactersForOpponent(opponentQuestion, answer);

                // Send eliminations to opponent
                SendEliminationsToOpponent(opponentQuestion, answer);

                Debug.Log($"[Game] Sending answer to opponent: {answer}");
                NetworkManager.Instance?.SendAnswer(answer);

                // After answering, it becomes our turn to ask
                isMyTurn = true;
                StartPlayerTurn();
            }
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
        myCharacterPicked = true;

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

        // Check if both players have selected characters
        if (currentGameMode == GameMode.Multiplayer)
        {
            if (opponentCharacterPicked)
            {
                // Both selected - start the game
                if (isMyTurn)
                {
                    StartPlayerTurn();
                }
                else
                {
                    ShowWaitingForOpponentQuestion();
                }
            }
            else
            {
                // Wait for opponent to pick
                popup?.ShowMessage("Waiting for opponent to pick a character...");
            }
        }
        else
        {
            // Single player - start immediately
            StartPlayerTurn();
        }
    }

    private void OnOpponentCharacterSelected(string characterName)
    {
        opponentSelectedCharacter = characterList.Find(c => c.characterName == characterName);
        opponentCharacterPicked = true;
        Debug.Log($"[Game] Opponent selected: {characterName}");

        // If we already picked, and opponent just picked, check turn
        if (myCharacterPicked)
        {
            if (isMyTurn)
            {
                // Our turn - dismiss any waiting popup and start
                popup?.Hide();
                StartPlayerTurn();
            }
            else
            {
                // Their turn - show waiting
                popup?.Hide();
                ShowWaitingForOpponentQuestion();
            }
        }
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
        isInGuessMode = false;

        if (currentGameMode == GameMode.Multiplayer && !isMyTurn)
        {
            // Not our turn - should not happen, but safety check
            Debug.LogWarning("[Game] StartPlayerTurn called but not our turn!");
            return;
        }

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
        if (currentGameMode == GameMode.Multiplayer && !isMyTurn)
        {
            Debug.LogWarning("[Game] Tried to send question on opponent's turn!");
            return;
        }

        currentQuestion = question;
        QuestionManager.Instance?.MarkQuestionAsAsked(question);

        StartCoroutine(ProcessPlayerQuestion());
    }

    private IEnumerator ProcessPlayerQuestion()
    {
        DisableQuestionBar();
        isInGuessMode = false;

        if (currentGameMode == GameMode.SinglePlayer)
        {
            popup?.ShowAIThinking();
            yield return new WaitForSeconds(1f);

            bool answer = currentQuestion.MatchesCharacter(opponentSelectedCharacter);
            popup?.ShowAIAnswer(answer);
        }
        else
        {
            // Multiplayer: send question to opponent and wait
            waitingForOpponentAnswer = true;

            // Show waiting message
            popup?.ShowMessage("Waiting for opponent to answer...");

            // Send question to opponent
            bool expectedAnswer = currentQuestion.MatchesCharacter(opponentSelectedCharacter);
            NetworkManager.Instance?.SendQuestion(currentQuestion.QuestionText, expectedAnswer);

            Debug.Log($"[Game] Question sent to opponent: {currentQuestion.QuestionText}");
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
        if (currentGameMode == GameMode.Multiplayer && !isMyTurn)
        {
            Debug.LogWarning("[Game] Cannot guess on opponent's turn!");
            return;
        }

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

        if (currentGameMode == GameMode.Multiplayer)
        {
            // Send guess to opponent
            NetworkManager.Instance?.SendGuess(guessedCharacter.characterName);
        }

        popup?.ShowMessage(isCorrect
            ? $"Correct! It was {guessedCharacter.characterName}!"
            : $"Wrong! It was {opponentSelectedCharacter.characterName}!");

        yield return new WaitForSeconds(2f);

        if (isCorrect)
            PlayerWins();
        else
        {
            if (currentGameMode == GameMode.SinglePlayer)
            {
                StartAITurn();
            }
            else
            {
                // Wrong guess in multiplayer - opponent's turn
                SwitchTurns();
            }
        }
    }

    #endregion

    #region Multiplayer Question/Answer Handling

    private void ShowWaitingForOpponentQuestion()
    {
        currentState = GameState.AITurn; // Using AITurn state for "opponent's turn"
        popup?.ShowMessage("Waiting for opponent to pick a question...");
    }

    private void OnQuestionReceivedFromOpponent(string questionText, bool expectedAnswer)
    {
        Debug.Log($"[Game] Received question from opponent: {questionText}");

        // Find the question object
        opponentQuestion = FindQuestionByText(questionText);

        if (opponentQuestion == null)
        {
            Debug.LogError($"[Game] Could not find question: {questionText}");
            return;
        }

        currentState = GameState.AIQuestion;

        // Show question popup for player to answer
        bool correctIsYes = opponentQuestion.MatchesCharacter(playerSelectedCharacter);
        popup?.ShowAIQuestion(opponentQuestion, correctIsYes);
    }

    private SCR_Question FindQuestionByText(string questionText)
    {
        if (QuestionManager.Instance == null) return null;

        for (int i = 0; i < QuestionManager.Instance.TotalQuestions; i++)
        {
            var q = QuestionManager.Instance.GetQuestionAtIndex(i);
            if (q != null && q.QuestionText == questionText)
                return q;
        }
        return null;
    }

    private void OnAnswerReceivedFromOpponent(bool answer)
    {
        Debug.Log($"[Game] Received answer from opponent: {answer}");

        waitingForOpponentAnswer = false;

        // Show answer popup
        popup?.Hide();
        popup?.ShowAIAnswer(answer);
    }

    private void SwitchTurns()
    {
        isMyTurn = !isMyTurn;
        Debug.Log($"[Game] Turn switched - isMyTurn: {isMyTurn}");

        if (isMyTurn)
        {
            StartPlayerTurn();
        }
        else
        {
            ShowWaitingForOpponentQuestion();
        }
    }

    private void OnOpponentDisconnected()
    {
        popup?.ShowMessage("Opponent disconnected!");
        Invoke(nameof(ReturnToMainMenu), 2f);
    }

    #endregion

    #region Opponent Grid Sync

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

        Debug.Log($"[Game] Opponent grid: eliminated {toEliminate.Count} characters");
    }

    private void SendEliminationsToOpponent(SCR_Question question, bool answerIsYes)
    {
        if (currentGameMode != GameMode.Multiplayer) return;
        if (NetworkManager.Instance == null) return;

        // Calculate which characters the opponent should eliminate on their grid
        var opponentRemaining = new List<SCR_Character>(characterList);

        foreach (var cell in opponentCells)
        {
            if (cell.IsEliminated)
                opponentRemaining.Remove(cell.Character);
        }

        var toEliminate = CharacterFilter.GetCharactersToEliminate(opponentRemaining, question, answerIsYes);

        // Send each elimination to opponent
        foreach (var character in toEliminate)
        {
            NetworkManager.Instance.SendElimination(character.characterName, true);
        }
    }

    private void OnEliminationReceivedFromOpponent(string characterName, bool eliminated)
    {
        Debug.Log($"[Game] Received elimination from opponent: {characterName} - {eliminated}");

        // Find the cell in opponent grid and mark it
        foreach (var cell in opponentCells)
        {
            if (cell.Character != null && cell.Character.characterName == characterName)
            {
                cell.MarkAsEliminated(eliminated);
                Debug.Log($"[Game] Updated opponent grid cell: {characterName}");
                break;
            }
        }
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
            // In multiplayer, wait for opponent's action
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

    private void OnOpponentGuess(string characterId)
    {
        SCR_Character guessed = characterList.Find(c => c.characterName == characterId);

        if (guessed == playerSelectedCharacter)
        {
            // Opponent guessed correctly - we lose
            popup?.ShowMessage($"Opponent guessed: {characterId}\n\nYou Lose!");
            Invoke(nameof(PlayerLoses), 2f);
        }
        else
        {
            // Opponent guessed wrong - continue game
            popup?.ShowMessage($"Opponent guessed wrong: {characterId}");
            // Wrong guess means it's our turn now
            isMyTurn = true;
            Invoke(nameof(StartPlayerTurn), 2f);
        }
    }

    #endregion

    #region Game Over Events

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
