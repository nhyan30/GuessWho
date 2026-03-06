using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Networking;

/// <summary>
/// GameManager controls the Guess Who game flow.
/// Supports both Single Player (vs AI) and Multiplayer modes.
/// 
/// Bug Fixes Implemented:
/// 1. Wrong guess ends game immediately (player loses)
/// 2. Character elimination uses coroutine for thinking + answer popup
/// 3. Random room codes with IP address display for multiplayer
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

    [Header("Debug")]
    [SerializeField] private TMP_Text turnDebugText;
    [SerializeField] private bool enableDebugLogs = true;

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
    [SerializeField] private bool isMyTurn = true;
    private bool opponentCharacterPicked = false;
    private bool myCharacterPicked = false;
    private SCR_Question opponentQuestion;
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

    private void Update()
    {
        if (turnDebugText != null && currentGameMode == GameMode.Multiplayer)
        {
            string turnInfo = isMyTurn ? "YOUR TURN" : "OPPONENT'S TURN";
            string stateInfo = currentState.ToString();
            string hostInfo = NetworkManager.Instance?.IsHost == true ? " (Host)" : " (Guest)";
            turnDebugText.text = $"{turnInfo}{hostInfo}\nState: {stateInfo}";
        }
        else if (turnDebugText != null)
        {
            turnDebugText.text = "";
        }
    }

    #endregion

    #region Game Flow Control

    public void BeginGame(GameMode mode)
    {
        currentGameMode = mode;
        gameStarted = true;
        myCharacterPicked = false;
        opponentCharacterPicked = false;
        opponentEliminatedCharacters.Clear();

        LogDebug($"Starting {mode} game");

        if (mode == GameMode.Multiplayer)
        {
            isMyTurn = NetworkManager.Instance.IsHost;
            LogDebug($"Is Host: {NetworkManager.Instance.IsHost}, isMyTurn: {isMyTurn}");
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

            LogDebug("Multiplayer events registered");
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

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[Game] {message}");
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
        LogDebug($"AI selected: {opponentSelectedCharacter.characterName}");
    }

    #endregion

    #region Popup Event Handlers

    private void OnPopupOkay()
    {
        PopupType popupType = popup.GetCurrentType();
        LogDebug($"Popup Okay clicked: {popupType}");

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
                    popup?.ShowMessage("Wait for your turn!", false);
                }
                else
                {
                    EnableQuestionBar();
                }
                break;

            case PopupType.AIAnswer:
                popup?.Hide();
                // Elimination already happened during the coroutine
                if (currentGameMode == GameMode.SinglePlayer)
                {
                    StartAITurn();
                }
                else
                {
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
                QuestionManager.Instance?.MarkQuestionAsAskedAI(opponentQuestion);
                EliminateCharactersForOpponent(opponentQuestion, answer);
                SendEliminationsToOpponent(opponentQuestion, answer);

                LogDebug($"Sending answer to opponent: {answer}");
                NetworkManager.Instance?.SendAnswer(answer);

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
            LogDebug($"Sending character selection: {playerSelectedCharacter.characterName}");
            NetworkManager.Instance.SendCharacterSelected(playerSelectedCharacter.characterName);
        }

        // Check if both players have selected characters
        if (currentGameMode == GameMode.Multiplayer)
        {
            if (opponentCharacterPicked)
            {
                LogDebug($"Both characters picked. isMyTurn: {isMyTurn}");
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
                popup?.ShowMessage("Waiting for opponent to pick a character...", false);
            }
        }
        else
        {
            StartPlayerTurn();
        }
    }

    private void OnOpponentCharacterSelected(string characterName)
    {
        opponentSelectedCharacter = characterList.Find(c => c.characterName == characterName);
        opponentCharacterPicked = true;
        LogDebug($"Opponent selected: {characterName}. myCharacterPicked: {myCharacterPicked}");

        if (myCharacterPicked)
        {
            if (isMyTurn)
            {
                popup?.Hide();
                StartPlayerTurn();
            }
            else
            {
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
        LogDebug($"StartPlayerTurn called. isMyTurn: {isMyTurn}");

        if (currentGameMode == GameMode.Multiplayer && !isMyTurn)
        {
            LogDebug("WARNING: StartPlayerTurn called but not our turn!");
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
            LogDebug("WARNING: Tried to send question on opponent's turn!");
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
            // BUG FIX #2: Combined thinking + answer + elimination in one coroutine
            yield return StartCoroutine(ShowThinkingAnswerAndEliminate());
        }
        else
        {
            waitingForOpponentAnswer = true;
            popup?.ShowMessage("Waiting for opponent to answer...", false);

            bool expectedAnswer = currentQuestion.MatchesCharacter(opponentSelectedCharacter);
            LogDebug($"Sending question to opponent: {currentQuestion.QuestionText}");
            NetworkManager.Instance?.SendQuestion(currentQuestion.QuestionText, expectedAnswer);
        }
    }

    /// <summary>
    /// BUG FIX #2: Shows thinking, then answer, then eliminates characters - all in one popup.
    /// This creates a smooth flow where the player sees the opponent "thinking"
    /// before getting the answer, without multiple popup windows.
    /// </summary>
    private IEnumerator ShowThinkingAnswerAndEliminate()
    {
        // Step 1: Show thinking popup
        popup?.ShowAIThinking();
        yield return new WaitForSeconds(1.5f);  // Thinking delay

        // Step 2: Get the answer
        bool answer = currentQuestion.MatchesCharacter(opponentSelectedCharacter);

        // Step 3: Update popup to show answer (in same popup)
        popup?.UpdateAIThinkingToAnswer(answer);
        yield return new WaitForSeconds(0.5f);  // Let player read the answer

        // Step 4: Eliminate characters with visual animation
        yield return StartCoroutine(EliminateCharactersFromAnswerWithAnimation(answer));
    }

    /// <summary>
    /// Eliminates characters from player's grid with visual animation.
    /// </summary>
    private IEnumerator EliminateCharactersFromAnswerWithAnimation(bool answerIsYes)
    {
        if (currentQuestion == null) yield break;

        var toEliminate = CharacterFilter.GetCharactersToEliminate(
            playerRemainingCharacters, currentQuestion, answerIsYes);

        // Animate each elimination
        foreach (var character in toEliminate)
        {
            foreach (var cell in playerCells)
            {
                if (cell.Character == character)
                {
                    cell.MarkAsEliminated(true);
                    //yield return new WaitForSeconds(0.01f);
                    break;
                }
            }
        }

        playerRemainingCharacters = CharacterFilter.GetRemainingCharacters(
            playerRemainingCharacters, currentQuestion, answerIsYes);

        LogDebug($"{playerRemainingCharacters.Count} characters remaining");

        // Show the answer popup for player to acknowledge
        popup?.ShowAIAnswer(answerIsYes);
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

        LogDebug($"{playerRemainingCharacters.Count} characters remaining");
    }

    private void OnDoAGuessPressed()
    {
        if (currentGameMode == GameMode.Multiplayer && !isMyTurn)
        {
            LogDebug("Cannot guess on opponent's turn!");
            return;
        }

        isInGuessMode = true;
        DisableQuestionBar();
        popup?.ShowMessage("Click on a character to make your guess!", true);
    }

    private void ProcessPlayerGuess(SCR_Character guessedCharacter)
    {
        StartCoroutine(ProcessGuessCoroutine(guessedCharacter));
    }

    /// <summary>
    /// BUG FIX #1: Processes the player's guess.
    /// If correct, player wins. If wrong, player loses immediately (game over).
    /// </summary>
    private IEnumerator ProcessGuessCoroutine(SCR_Character guessedCharacter)
    {
        popup?.Hide();
        isInGuessMode = false;

        bool isCorrect = guessedCharacter == opponentSelectedCharacter;

        if (currentGameMode == GameMode.Multiplayer)
        {
            NetworkManager.Instance?.SendGuess(guessedCharacter.characterName);
        }

        // Show the result message
        popup?.ShowMessage(isCorrect
            ? $"Correct! It was {guessedCharacter.characterName}!"
            : $"Wrong! It was {opponentSelectedCharacter.characterName}!", false);

        yield return new WaitForSeconds(2f);

        if (isCorrect)
        {
            // Player guessed correctly - they win!
            PlayerWins();
        }
        else
        {
            // BUG FIX #1: Wrong guess = player loses immediately
            // Previously this would continue to AI turn, but now the game ends
            PlayerLoses();
        }
    }

    #endregion

    #region Multiplayer Question/Answer Handling

    private void ShowWaitingForOpponentQuestion()
    {
        currentState = GameState.AITurn;
        popup?.ShowMessage("Waiting for opponent to pick a question...", false);
    }

    private void OnQuestionReceivedFromOpponent(string questionText, bool expectedAnswer)
    {
        LogDebug($"Received question from opponent: {questionText}");

        opponentQuestion = FindQuestionByText(questionText);

        if (opponentQuestion == null)
        {
            LogDebug($"ERROR: Could not find question: {questionText}");
            return;
        }

        currentState = GameState.AIQuestion;
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
        LogDebug($"Received answer from opponent: {answer}");

        waitingForOpponentAnswer = false;

        // BUG FIX #2: Combined answer + elimination for multiplayer too
        StartCoroutine(ShowAnswerAndEliminateFromOpponent(answer));
    }

    private IEnumerator ShowAnswerAndEliminateFromOpponent(bool answer)
    {
        popup?.Hide();
        popup?.ShowAIAnswer(answer);
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(EliminateCharactersFromAnswerWithAnimation(answer));
    }

    private void SwitchTurns()
    {
        isMyTurn = !isMyTurn;
        LogDebug($"Turn switched - isMyTurn: {isMyTurn}");

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
        popup?.ShowMessage("Opponent disconnected!", false);
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

        LogDebug($"Opponent grid: eliminated {toEliminate.Count} characters");
    }

    private void SendEliminationsToOpponent(SCR_Question question, bool answerIsYes)
    {
        if (currentGameMode != GameMode.Multiplayer) return;
        if (NetworkManager.Instance == null) return;

        var opponentRemaining = new List<SCR_Character>(characterList);

        foreach (var cell in opponentCells)
        {
            if (cell.IsEliminated)
                opponentRemaining.Remove(cell.Character);
        }

        var toEliminate = CharacterFilter.GetCharactersToEliminate(opponentRemaining, question, answerIsYes);

        foreach (var character in toEliminate)
        {
            NetworkManager.Instance.SendElimination(character.characterName, true);
        }
    }

    private void OnEliminationReceivedFromOpponent(string characterName, bool eliminated)
    {
        LogDebug($"Received elimination: {characterName} - {eliminated}");

        foreach (var cell in opponentCells)
        {
            if (cell.Character != null && cell.Character.characterName == characterName)
            {
                cell.MarkAsEliminated(eliminated);
                break;
            }
        }
    }

    #endregion

    #region Opponent/AI Turn

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
            popup?.ShowMessage($"AI guessed: {guessedCharacter.characterName}\n\nAI Wins!", false);
            Invoke(nameof(PlayerLoses), 2f);
        }
        else
        {
            popup?.ShowMessage($"AI guessed wrong: {guessedCharacter.characterName}", false);
            Invoke(nameof(StartPlayerTurn), 2f);
        }
    }

    private void OnOpponentGuess(string characterId)
    {
        SCR_Character guessed = characterList.Find(c => c.characterName == characterId);

        if (guessed == playerSelectedCharacter)
        {
            popup?.ShowMessage($"Opponent guessed: {characterId}\n\nYou Lose!", false);
            Invoke(nameof(PlayerLoses), 2f);
        }
        else
        {
            popup?.ShowMessage($"Opponent guessed wrong: {characterId}", false);
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
