using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simplified GameManager that handles all game flow through a single popup system.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Character Data")]
    [SerializeField] private List<SCR_Character> characterList = new List<SCR_Character>();
    [SerializeField] private Transform grid;
    [SerializeField] private GameObject cellPrefab;

    [Header("Popup")]
    [SerializeField] private PopupController popup;

    [Header("Question Bar")]
    [SerializeField] private QuestionBarController questionBar;
    [SerializeField] private Button doAGuessButton;

    [Header("Selected Character Display")]
    [SerializeField] private Image selectedCharacterImage;
    [SerializeField] private TMP_Text selectedCharacterText;

    [Header("AI Character Display")]
    [SerializeField] private Image aiCharacterImage;
    [SerializeField] private TMP_Text aiCharacterText;

    // Game state
    private GameState currentState;
    private SCR_Character playerSelectedCharacter;
    private SCR_Character aiSelectedCharacter;
    private List<Cell> allCells = new List<Cell>();
    private List<SCR_Character> playerRemainingCharacters = new List<SCR_Character>();
    private SCR_Question currentQuestion;

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
        questionBar?.SetVisible(false);
        if (doAGuessButton != null) doAGuessButton.gameObject.SetActive(false);
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

        // Fill grid
        FillGrid();

        // Select AI character
        SelectAICharacter();

        // Start with character selection popup
        currentState = GameState.CharacterSelection;
        popup?.ShowCharacterSelect();
    }

    private void FillGrid()
    {
        allCells.Clear();
        foreach (Transform child in grid) Destroy(child.gameObject);

        for (int i = 0; i < characterList.Count; i++)
        {
            GameObject cellObj = Instantiate(cellPrefab, grid, false);
            Cell cell = cellObj.GetComponent<Cell>();
            if (cell != null)
            {
                cell.SetCell(characterList[i]);
                allCells.Add(cell);
            }
        }
    }

    private void SelectAICharacter()
    {
        aiSelectedCharacter = characterList[Random.Range(0, characterList.Count)];
        AIController.Instance?.Initialize(characterList, aiSelectedCharacter);
        Debug.Log($"AI selected: {aiSelectedCharacter.characterName}");
    }

    #endregion

    #region Popup Event Handlers

    private void OnPopupOkay()
    {
        switch (currentState)
        {
            case GameState.CharacterSelection:
                // User confirmed they want to select a character
                // Close popup and let them click on grid
                popup?.Hide();
                break;

            case GameState.CharacterAgree:
                // User confirmed their character selection
                ConfirmCharacterSelection();
                break;

            case GameState.PlayerTurn:
                // After seeing AI answer
                popup?.Hide();
                StartAITurn();
                break;

            case GameState.GuessConfirm:
                // User confirmed their guess
                ProcessPlayerGuess(popup.GetCurrentCharacter());
                break;

            case GameState.GameOver:
                // Play again
                RestartGame();
                break;
        }
    }

    private void OnPopupNegate()
    {
        switch (currentState)
        {
            case GameState.CharacterAgree:
                // User wants to select a different character
                popup?.Hide();
                currentState = GameState.CharacterSelection;
                break;

            case GameState.GuessConfirm:
                // User cancelled guess
                popup?.Hide();
                currentState = GameState.PlayerTurn;
                break;
        }
    }

    private void OnPopupAnswer(bool answer)
    {
        // Player answered AI's question
        if (currentState == GameState.AIQuestion)
        {
            popup?.Hide();
            AIController.Instance?.ProcessPlayerAnswer(currentQuestion, answer);
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

        // Update player character display
        if (selectedCharacterImage != null)
        {
            selectedCharacterImage.sprite = playerSelectedCharacter.characterSprite;
            selectedCharacterImage.gameObject.SetActive(true);
        }
        if (selectedCharacterText != null)
        {
            selectedCharacterText.text = playerSelectedCharacter.characterName;
        }

        // Start player turn
        StartPlayerTurn();
    }

    #endregion

    #region Player Turn

    private void StartPlayerTurn()
    {
        currentState = GameState.PlayerTurn;

        // Show question select popup first
        popup?.ShowQuestionSelect();
    }

    private void OnPopupOkay_PlayerTurn()
    {
        popup?.Hide();

        // Enable question bar
        questionBar?.SetVisible(true);
        questionBar?.SetActive(true);
        questionBar?.UpdateDisplay();

        // Show guess button
        if (doAGuessButton != null) doAGuessButton.gameObject.SetActive(true);
    }

    private void OnQuestionSent(SCR_Question question)
    {
        if (currentState != GameState.PlayerTurn) return;

        currentQuestion = question;
        StartCoroutine(ProcessPlayerQuestion());
    }

    private IEnumerator ProcessPlayerQuestion()
    {
        // Disable controls
        questionBar?.SetActive(false);
        if (doAGuessButton != null) doAGuessButton.gameObject.SetActive(false);

        // Show thinking
        popup?.ShowAIThinking();
        yield return new WaitForSeconds(1f);

        // Get and show answer
        bool answer = currentQuestion.MatchesCharacter(aiSelectedCharacter);
        popup?.ShowAIAnswer(answer);

        // Wait for popup to be closed (user clicks okay)
        yield return new WaitUntil(() => !popup.IsVisible());

        // Eliminate characters
        EliminateCharacters(currentQuestion, answer);

        // Start AI turn
        StartAITurn();
    }

    private void OnDoAGuessPressed()
    {
        // Disable controls
        questionBar?.SetActive(false);
        if (doAGuessButton != null) doAGuessButton.gameObject.SetActive(false);

        // Show guess mode - clicking on cells will trigger guess
        popup?.ShowMessage("Click on a character to make your guess!");
    }

    public void OnCellClickedForGuess(SCR_Character character)
    {
        if (currentState == GameState.PlayerTurn)
        {
            currentState = GameState.GuessConfirm;
            popup?.ShowGuessConfirm(character);
        }
    }

    private void ProcessPlayerGuess(SCR_Character guessedCharacter)
    {
        StartCoroutine(ProcessGuessCoroutine(guessedCharacter));
    }

    private IEnumerator ProcessGuessCoroutine(SCR_Character guessedCharacter)
    {
        popup?.Hide();

        bool isCorrect = guessedCharacter == aiSelectedCharacter;

        // Show result
        popup?.ShowMessage(isCorrect
            ? $"Correct! It was {guessedCharacter.characterName}!"
            : $"Wrong! It was {aiSelectedCharacter.characterName}!");

        yield return new WaitForSeconds(2f);

        if (isCorrect)
        {
            PlayerWins();
        }
        else
        {
            PlayerLoses();
        }
    }

    #endregion

    #region AI Turn

    private void StartAITurn()
    {
        currentState = GameState.AITurn;
        StartCoroutine(AITurnCoroutine());
    }

    private IEnumerator AITurnCoroutine()
    {
        // Show thinking
        popup?.ShowAIThinking();
        yield return new WaitForSeconds(1.5f);

        // Check if AI should guess
        if (AIController.Instance?.RemainingPossibleCharacters == 1)
        {
            // AI makes final guess
            SCR_Character guess = AIController.Instance.GetMostLikelyCharacter();
            ProcessAIGuess(guess);
        }
        else
        {
            // AI asks question
            SCR_Question aiQuestion = AIController.Instance?.SelectQuestion();
            if (aiQuestion != null)
            {
                currentQuestion = aiQuestion;
                currentState = GameState.AIQuestion;

                // Show AI question
                bool correctIsYes = aiQuestion.MatchesCharacter(playerSelectedCharacter);
                popup?.ShowAIQuestion(aiQuestion, correctIsYes);
            }
        }
    }

    private void ProcessAIGuess(SCR_Character guessedCharacter)
    {
        bool isCorrect = guessedCharacter == playerSelectedCharacter;

        if (isCorrect)
        {
            popup?.ShowMessage($"AI guessed: {guessedCharacter.characterName}\nAI Wins!");
            Invoke(nameof(PlayerLoses), 2f);
        }
        else
        {
            popup?.ShowMessage($"AI guessed wrong: {guessedCharacter.characterName}");
            Invoke(nameof(StartPlayerTurn), 2f);
        }
    }

    #endregion

    #region Character Elimination

    private void EliminateCharacters(SCR_Question question, bool answerIsYes)
    {
        var toEliminate = CharacterFilter.GetCharactersToEliminate(playerRemainingCharacters, question, answerIsYes);

        foreach (var cell in allCells)
        {
            if (toEliminate.Contains(cell.Character))
            {
                cell.MarkAsEliminated(true);
            }
        }

        playerRemainingCharacters = CharacterFilter.GetRemainingCharacters(playerRemainingCharacters, question, answerIsYes);
        Debug.Log($"{playerRemainingCharacters.Count} characters remaining");
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

        // Reset cells
        foreach (var cell in allCells)
        {
            cell.MarkAsEliminated(false);
        }

        // Reset managers
        QuestionManager.Instance?.ClearAskedHistory();
        QuestionManager.Instance?.ResetToFirstQuestion();
        AIController.Instance?.ResetAI();

        StartGame();
    }

    #endregion

    #region Public API

    public GameState GetGameState() => currentState;
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
