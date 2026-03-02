using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameManager controls the Guess Who game flow.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Character Data")]
    [SerializeField] private List<SCR_Character> characterList = new List<SCR_Character>();
    [SerializeField] private Transform playerGrid;
    [SerializeField] private Transform aiGrid; // Mini grid for AI
    [SerializeField] private GameObject playerCellPrefab;
    [SerializeField] private GameObject aiCellPrefab;

    [Header("Popup")]
    [SerializeField] private PopupController popup;

    [Header("Question Bar")]
    [SerializeField] private QuestionBarController questionBar;
    [SerializeField] private Button doAGuessButton;

    [Header("Player's Character Display")]
    [SerializeField] private Image selectedCharacterImage;
    [SerializeField] private TMP_Text selectedCharacterText;

    [Header("AI's Character Display")]
    [SerializeField] private Image aiCharacterImage;
    [SerializeField] private TMP_Text aiCharacterText;

    // Game state
    private GameState currentState;
    private SCR_Character playerSelectedCharacter;
    private SCR_Character aiSelectedCharacter;
    private List<Cell> playerCells = new List<Cell>();
    private List<AICell> aiCells = new List<AICell>(); // AI's mini grid cells
    private List<SCR_Character> playerRemainingCharacters = new List<SCR_Character>();
    private SCR_Question currentQuestion;
    private bool isInGuessMode = false; // Track if player is in guess mode

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
        StartGame();
    }

    #endregion

    #region Initialization

    private void HideAllUI()
    {
        selectedCharacterImage?.gameObject.SetActive(false);
        aiCharacterImage?.gameObject.SetActive(false);
        DisableQuestionBar();
    }

    private void SetupEventListeners()
    {
        // Popup events
        if (popup != null)
        {
            popup.OnOkayClicked += OnPopupOkay;
            popup.OnNegateClicked += OnPopupNegate;
            popup.OnAnswerClicked += OnPopupAnswer;
        }

        // Question bar events
        if (questionBar != null)
        {
            questionBar.OnQuestionSent += OnQuestionSent;
        }

        // Do a guess button
        if (doAGuessButton != null)
        {
            doAGuessButton.onClick.AddListener(OnDoAGuessPressed);
        }
    }

    private void StartGame()
    {
        // Reset game state
        playerRemainingCharacters = new List<SCR_Character>(characterList);
        playerSelectedCharacter = null;
        aiSelectedCharacter = null;
        currentQuestion = null;
        isInGuessMode = false;

        // Fill grids
        FillPlayerGrid();
        FillAIGrid();

        // Select AI character
        SelectAICharacter();

        // Start with character selection popup
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

    private void FillAIGrid()
    {
        if (aiGrid == null || aiCellPrefab == null) return;

        aiCells.Clear();
        foreach (Transform child in aiGrid) Destroy(child.gameObject);

        for (int i = 0; i < characterList.Count; i++)
        {
            GameObject cellObj = Instantiate(aiCellPrefab, aiGrid, false);
            AICell cell = cellObj.GetComponent<AICell>();
            if (cell != null)
            {
                cell.SetCell(characterList[i]);
                aiCells.Add(cell);
            }
        }
    }

    private void SelectAICharacter()
    {
        aiSelectedCharacter = characterList[Random.Range(0, characterList.Count)];
        AIController.Instance?.Initialize(characterList, aiSelectedCharacter);
        Debug.Log($"[Game] AI selected: {aiSelectedCharacter.characterName}");
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
                StartAITurn();
                break;

            case PopupType.GuessConfirm:
                ProcessPlayerGuess(popup.GetCurrentCharacter());
                break;

            case PopupType.GameOver:
                RestartGame();
                break;

            case PopupType.Message:
                popup?.Hide();
                // If we're in guess mode, DON'T reset it - let player click a character
                // Only re-enable controls if NOT in guess mode
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
                // User cancelled guess - go back to normal player turn
                popup?.Hide();
                isInGuessMode = false;
                EnableQuestionBar();
                break;
        }
    }

    private void OnPopupAnswer(bool answer)
    {
        if (currentState == GameState.AIQuestion)
        {
            popup?.Hide();
            // Mark question as asked for AI
            QuestionManager.Instance?.MarkQuestionAsAskedAI(currentQuestion);
            // Update AI's knowledge
            AIController.Instance?.ProcessPlayerAnswer(currentQuestion, answer);
            // Update AI's mini grid
            EliminateCharactersForAI(currentQuestion, answer);
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

        StartPlayerTurn();
    }

    #endregion

    #region Cell Click Handler

    /// <summary>
    /// Called when a cell is clicked. Behavior depends on current state.
    /// </summary>
    public void OnCellClicked(SCR_Character character)
    {
        if (currentState == GameState.CharacterSelection)
        {
            OnCharacterSelected(character);
        }
        else if (currentState == GameState.PlayerTurn && isInGuessMode)
        {
            // Only allow guess confirmation if in guess mode
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
        // Mark as asked by player
        QuestionManager.Instance?.MarkQuestionAsAsked(question);
        StartCoroutine(ProcessPlayerQuestion());
    }

    private IEnumerator ProcessPlayerQuestion()
    {
        DisableQuestionBar();
        isInGuessMode = false;

        popup?.ShowAIThinking();
        yield return new WaitForSeconds(1f);

        bool answer = currentQuestion.MatchesCharacter(aiSelectedCharacter);
        popup?.ShowAIAnswer(answer);
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

        Debug.Log($"[Game] {playerRemainingCharacters.Count} characters remaining for player");

        if (playerRemainingCharacters.Count == 1)
        {
            Debug.Log($"[Game] Only {playerRemainingCharacters[0].characterName} remains!");
        }
    }

    private void OnDoAGuessPressed()
    {
        // Enter guess mode
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

        bool isCorrect = guessedCharacter == aiSelectedCharacter;

        popup?.ShowMessage(isCorrect
            ? $"Correct! It was {guessedCharacter.characterName}!"
            : $"Wrong! It was {aiSelectedCharacter.characterName}!");

        yield return new WaitForSeconds(2f);

        if (isCorrect)
            PlayerWins();
        else
            PlayerLoses();
    }

    #endregion

    #region AI Turn

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

    private void EliminateCharactersForAI(SCR_Question question, bool answerIsYes)
    {
        if (aiCells.Count == 0) return;

        // Get all characters that AI hasn't eliminated yet
        var aiRemaining = new List<SCR_Character>(characterList);

        // Remove already eliminated characters
        foreach (var cell in aiCells)
        {
            if (cell.IsEliminated)
                aiRemaining.Remove(cell.Character);
        }

        var toEliminate = CharacterFilter.GetCharactersToEliminate(aiRemaining, question, answerIsYes);

        foreach (var cell in aiCells)
        {
            if (toEliminate.Contains(cell.Character))
            {
                cell.MarkAsEliminated(true);
            }
        }

        Debug.Log($"[Game] AI eliminated {toEliminate.Count} characters from mini grid");
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

    #region Win/Loss

    private void PlayerWins()
    {
        currentState = GameState.GameOver;
        ShowAICharacter();
        popup?.ShowGameOver(true, aiSelectedCharacter);
    }

    private void PlayerLoses()
    {
        currentState = GameState.GameOver;
        ShowAICharacter();
        popup?.ShowGameOver(false, aiSelectedCharacter);
    }

    private void ShowAICharacter()
    {
        if (aiCharacterImage != null)
        {
            aiCharacterImage.sprite = aiSelectedCharacter.characterSprite;
            aiCharacterImage.gameObject.SetActive(true);
        }
        if (aiCharacterText != null)
        {
            aiCharacterText.text = aiSelectedCharacter.characterName;
        }
    }

    #endregion

    #region Restart

    private void RestartGame()
    {
        popup?.Hide();
        HideAllUI();

        foreach (var cell in playerCells)
            cell.MarkAsEliminated(false);

        foreach (var cell in aiCells)
            cell.MarkAsEliminated(false);

        QuestionManager.Instance?.ClearAskedHistory();
        AIController.Instance?.ResetAI();

        StartGame();
    }

    #endregion

    #region Public API

    public GameState GetGameState() => currentState;
    public bool IsInGuessMode => isInGuessMode;
    public SCR_Character GetPlayerCharacter() => playerSelectedCharacter;
    public SCR_Character GetAICharacter() => aiSelectedCharacter;

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
