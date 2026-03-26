using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls AI behavior in the Guess Who game.
/// Handles AI's turn logic, question asking, and answering player questions.
/// </summary>
public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    [Header("AI Settings")]
    [SerializeField] private float thinkingDelayMin = 1.5f;
    [SerializeField] private float thinkingDelayMax = 3.0f;
    [SerializeField] private float answerDelay = 1.0f;
    [SerializeField][Range(0f, 1f)] private float guessConfidenceThreshold = 0.8f;

    [Header("Difficulty")]
    [SerializeField] private AIDifficulty difficulty = AIDifficulty.Normal;

    // AI's knowledge
    private List<SCR_Character> possiblePlayerCharacters = new List<SCR_Character>();
    private SCR_Character aiSelectedCharacter;

    // Events
    public System.Action<SCR_Question> OnAIAskedQuestion;
    public System.Action<bool> OnAIAnsweredQuestion;
    public System.Action<SCR_Character> OnAIGuessedCharacter;

    #region Properties
    public int RemainingPossibleCharacters => possiblePlayerCharacters.Count;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes AI with all characters and the character it selected.
    /// </summary>
    public void Initialize(List<SCR_Character> allCharacters, SCR_Character selectedCharacter)
    {
        aiSelectedCharacter = selectedCharacter;
        possiblePlayerCharacters = new List<SCR_Character>(allCharacters);

        Debug.Log($"[AI] Initialized with {possiblePlayerCharacters.Count} possible characters");
        Debug.Log($"[AI] Selected character: {aiSelectedCharacter.characterName}");
    }

    /// <summary>
    /// Resets AI state for a new game.
    /// </summary>
    public void ResetAI()
    {
        possiblePlayerCharacters.Clear();
        aiSelectedCharacter = null;
    }
    #endregion

    #region AI Turn Logic
    /// <summary>
    /// Starts the AI's turn. Will either ask a question or make a guess.
    /// </summary>
    public void StartAITurn()
    {
        StartCoroutine(AITurnCoroutine());
    }

    private IEnumerator AITurnCoroutine()
    {
        // Simulate thinking time
        float thinkTime = Random.Range(thinkingDelayMin, thinkingDelayMax);
        yield return new WaitForSeconds(thinkTime);

        // Decide: ask question or make guess
        if (ShouldMakeGuess())
        {
            MakeGuess();
        }
        else
        {
            AskQuestion();
        }
    }

    private bool ShouldMakeGuess()
    {
        // Only guess if very confident
        if (possiblePlayerCharacters.Count == 1)
            return true;

        // At higher difficulties, AI might guess earlier if confident
        float confidence = 1f / possiblePlayerCharacters.Count;
        return confidence >= guessConfidenceThreshold;
    }

    private void AskQuestion()
    {
        SCR_Question question = SelectBestQuestion();

        if (question != null)
        {
            Debug.Log($"[AI] Asks: {question.QuestionText}");
            OnAIAskedQuestion?.Invoke(question);
        }
        else
        {
            // No more questions, must guess
            MakeGuess();
        }
    }

    private SCR_Question SelectBestQuestion()
    {
        if (QuestionManager.Instance == null) return null;

        // Get the best question based on current difficulty
        return difficulty switch
        {
            AIDifficulty.Easy => SelectRandomQuestion(),
            AIDifficulty.Normal => QuestionManager.Instance.GetBestQuestionForAI(possiblePlayerCharacters),
            AIDifficulty.Hard => SelectOptimalQuestion(),
            _ => QuestionManager.Instance.GetBestQuestionForAI(possiblePlayerCharacters)
        };
    }

    private SCR_Question SelectRandomQuestion()
    {
        var unasked = QuestionManager.Instance.GetUnaskedQuestions();
        if (unasked.Count == 0) return null;

        return unasked[Random.Range(0, unasked.Count)];
    }

    private SCR_Question SelectOptimalQuestion()
    {
        // For hard difficulty, use more sophisticated selection
        SCR_Question bestQuestion = null;
        float bestScore = -1f;

        var unaskedQuestions = QuestionManager.Instance.GetUnaskedQuestions();

        foreach (var question in unaskedQuestions)
        {
            float score = CalculateOptimalScore(question);
            if (score > bestScore)
            {
                bestScore = score;
                bestQuestion = question;
            }
        }

        return bestQuestion;
    }

    private float CalculateOptimalScore(SCR_Question question)
    {
        // Calculate how well this question splits remaining characters
        int yesCount = 0, noCount = 0;

        foreach (var character in possiblePlayerCharacters)
        {
            if (question.MatchesCharacter(character))
                yesCount++;
            else
                noCount++;
        }

        int total = yesCount + noCount;
        if (total == 0) return 0f;

        float ratio = (float)yesCount / total;

        // Optimal is 0.5 (splits evenly)
        // Also consider how many would be eliminated
        float balanceScore = 1f - Mathf.Abs(0.5f - ratio);
        float eliminationScore = Mathf.Min(yesCount, noCount) / (total * 0.5f);

        return balanceScore * 0.7f + eliminationScore * 0.3f;
    }
    #endregion

    #region Processing Answers
    /// <summary>
    /// Processes the player's answer to AI's question.
    /// </summary>
    public void ProcessPlayerAnswer(SCR_Question question, bool answerIsYes)
    {
        // Update AI's knowledge
        possiblePlayerCharacters = CharacterFilter.GetRemainingCharacters(
            possiblePlayerCharacters,
            question,
            answerIsYes);

        Debug.Log($"[AI] After answer, {possiblePlayerCharacters.Count} characters remain possible");
    }

    /// <summary>
    /// Answers a question from the player about AI's character.
    /// </summary>
    public IEnumerator AnswerPlayerQuestion(SCR_Question question)
    {
        yield return new WaitForSeconds(answerDelay);

        bool answer = question.MatchesCharacter(aiSelectedCharacter);
        string answerText = answer ? "Yes" : "No";

        Debug.Log($"[AI] Answers: {answerText}");
        OnAIAnsweredQuestion?.Invoke(answer);
    }
    #endregion

    #region Guessing
    private void MakeGuess()
    {
        if (possiblePlayerCharacters.Count == 0)
        {
            Debug.LogError("[AI] No characters left to guess!");
            return;
        }

        // Pick the most likely character
        SCR_Character guessedCharacter = possiblePlayerCharacters[0];

        if (possiblePlayerCharacters.Count > 1 && difficulty == AIDifficulty.Hard)
        {
            // For hard difficulty, might have a slight chance to pick wrong
            // to simulate human-like behavior (optional)
        }

        Debug.Log($"[AI] Guesses: {guessedCharacter.characterName}");
        OnAIGuessedCharacter?.Invoke(guessedCharacter);
    }
    #endregion

    #region Public API
    /// <summary>
    /// Gets AI's selected character (for comparison when guessing).
    /// </summary>
    public SCR_Character GetSelectedCharacter()
    {
        return aiSelectedCharacter;
    }

    /// <summary>
    /// Gets the most likely character from remaining possibilities.
    /// Used when AI needs to make a guess.
    /// </summary>
    public SCR_Character GetMostLikelyCharacter()
    {
        if (possiblePlayerCharacters.Count == 0)
        {
            Debug.LogWarning("[AI] No possible characters remaining!");
            return null;
        }

        // For now, just return the first one
        // In a more sophisticated system, we could weight by probability
        return possiblePlayerCharacters[0];
    }

    /// <summary>
    /// Selects a question for the AI to ask.
    /// Public method called by GameManager.
    /// </summary>
    public SCR_Question SelectQuestion()
    {
        return SelectBestQuestion();
    }

    /// <summary>
    /// Sets AI difficulty level.
    /// </summary>
    public void SetDifficulty(AIDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
    }
    #endregion
}

/// <summary>
/// AI difficulty levels that affect decision making.
/// </summary>
public enum AIDifficulty
{
    Easy,    // Random question selection
    Normal,  // Optimized question selection
    Hard     // Advanced elimination strategy
}
