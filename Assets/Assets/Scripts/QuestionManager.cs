using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages all questions in the game. Handles question navigation,
/// filtering, and provides questions for both player and AI.
/// Uses Singleton pattern for easy access.
/// 
/// IMPORTANT: The player's question bar only shows questions the PLAYER hasn't asked.
/// Questions asked by AI or opponent do NOT affect the player's question bar.
/// </summary>
public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }

    [Header("Question Database")]
    [SerializeField] private List<SCR_Question> allQuestions = new List<SCR_Question>();

    [Header("Settings")]
    [SerializeField] private bool shuffleQuestionsOnStart = true;

    // Track asked questions - separate for player and AI/Opponent
    // Only askedByCurrentPlayer affects the question bar visibility
    private HashSet<SCR_Question> askedByCurrentPlayer = new HashSet<SCR_Question>();
    private HashSet<SCR_Question> askedByOpponent = new HashSet<SCR_Question>(); // AI or other player

    // Current question index for navigation (within player's unasked questions)
    private int currentUnaskedIndex = 0;

    // Cached list of questions available to the player
    private List<SCR_Question> playerAvailableQuestionsCache;
    private bool cacheDirty = true;

    // Questions organized by category for efficient access
    private Dictionary<QuestionCategory, List<SCR_Question>> questionsByCategory;

    #region Properties

    /// <summary>
    /// Gets the current question available to the player (not asked by player yet).
    /// </summary>
    public SCR_Question CurrentQuestion
    {
        get
        {
            var available = GetPlayerAvailableQuestions();
            if (available.Count == 0) return null;
            if (currentUnaskedIndex >= available.Count) currentUnaskedIndex = 0;
            return available[currentUnaskedIndex];
        }
    }

    public int TotalQuestions => allQuestions.Count;

    /// <summary>
    /// Current index within the player's available questions list (1-based for display).
    /// </summary>
    public int CurrentIndex => currentUnaskedIndex;

    /// <summary>
    /// Total number of questions still available to the player.
    /// </summary>
    public int UnaskedCount => GetPlayerAvailableQuestions().Count;

    /// <summary>
    /// Alias for UnaskedCount for clarity.
    /// </summary>
    public int PlayerAvailableCount => GetPlayerAvailableQuestions().Count;

    /// <summary>
    /// Total number of questions asked by the current player.
    /// </summary>
    public int AskedByPlayerCount => askedByCurrentPlayer.Count;

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
            return;
        }

        InitializeQuestions();
    }
    #endregion

    #region Initialization
    private void InitializeQuestions()
    {
        if (shuffleQuestionsOnStart)
        {
            ShuffleQuestions();
        }

        OrganizeQuestionsByCategory();
        InvalidateCache();
    }

    private void ShuffleQuestions()
    {
        // Fisher-Yates shuffle
        for (int i = allQuestions.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (allQuestions[i], allQuestions[randomIndex]) =
                (allQuestions[randomIndex], allQuestions[i]);
        }
    }

    private void OrganizeQuestionsByCategory()
    {
        questionsByCategory = new Dictionary<QuestionCategory, List<SCR_Question>>();

        foreach (QuestionCategory category in System.Enum.GetValues(typeof(QuestionCategory)))
        {
            questionsByCategory[category] = allQuestions
                .Where(q => q.Category == category)
                .ToList();
        }
    }
    #endregion

    #region Cache Management

    private void InvalidateCache()
    {
        cacheDirty = true;
    }

    private void RefreshCacheIfNeeded()
    {
        if (cacheDirty || playerAvailableQuestionsCache == null)
        {
            // IMPORTANT: Only filter by questions the CURRENT PLAYER has asked
            // Do NOT filter by AI/opponent questions - player can still ask those!
            playerAvailableQuestionsCache = allQuestions
                .Where(q => !askedByCurrentPlayer.Contains(q))
                .ToList();
            cacheDirty = false;
        }
    }

    #endregion

    #region Navigation Methods (Player's Available Questions Only)

    /// <summary>
    /// Gets the list of questions available to the player (not yet asked by player).
    /// This does NOT include questions asked by AI/opponent - player can still ask those!
    /// </summary>
    public List<SCR_Question> GetPlayerAvailableQuestions()
    {
        RefreshCacheIfNeeded();
        return playerAvailableQuestionsCache;
    }

    /// <summary>
    /// Gets ALL unasked questions (not asked by player OR opponent).
    /// Used by AI to select questions.
    /// </summary>
    public List<SCR_Question> GetUnaskedQuestions()
    {
        return allQuestions
            .Where(q => !askedByCurrentPlayer.Contains(q) && !askedByOpponent.Contains(q))
            .ToList();
    }

    /// <summary>
    /// Moves to the next available question for the player.
    /// </summary>
    /// <returns>The next question, or null if none available</returns>
    public SCR_Question GetNextQuestion()
    {
        var available = GetPlayerAvailableQuestions();
        if (available.Count == 0) return null;

        currentUnaskedIndex = (currentUnaskedIndex + 1) % available.Count;
        return CurrentQuestion;
    }

    /// <summary>
    /// Moves to the previous available question for the player.
    /// </summary>
    /// <returns>The previous question, or null if none available</returns>
    public SCR_Question GetPreviousQuestion()
    {
        var available = GetPlayerAvailableQuestions();
        if (available.Count == 0) return null;

        currentUnaskedIndex--;
        if (currentUnaskedIndex < 0)
        {
            currentUnaskedIndex = available.Count - 1;
        }

        return CurrentQuestion;
    }

    /// <summary>
    /// Gets a question at a specific index from the ALL questions list.
    /// Used for finding questions by text from network messages.
    /// </summary>
    public SCR_Question GetQuestionAtIndex(int index)
    {
        if (index < 0 || index >= allQuestions.Count) return null;
        return allQuestions[index];
    }

    /// <summary>
    /// Finds a question by its text content.
    /// </summary>
    public SCR_Question FindQuestionByText(string questionText)
    {
        return allQuestions.FirstOrDefault(q => q.QuestionText == questionText);
    }

    /// <summary>
    /// Resets the question index to the beginning.
    /// </summary>
    public void ResetToFirstQuestion()
    {
        currentUnaskedIndex = 0;
    }

    #endregion

    #region Question Tracking

    /// <summary>
    /// Marks a question as asked by the current player.
    /// This will REMOVE it from the player's question bar.
    /// </summary>
    public void MarkQuestionAsAsked(SCR_Question question)
    {
        if (question != null && askedByCurrentPlayer.Add(question))
        {
            InvalidateCache();

            // Adjust index if needed to stay within bounds
            var available = GetPlayerAvailableQuestions();
            if (currentUnaskedIndex >= available.Count && available.Count > 0)
            {
                currentUnaskedIndex = available.Count - 1;
            }
        }
    }

    /// <summary>
    /// Marks a question as asked by AI (in single player) or opponent (in multiplayer).
    /// This does NOT affect the player's question bar - player can still ask these!
    /// </summary>
    public void MarkQuestionAsAskedAI(SCR_Question question)
    {
        if (question != null)
        {
            askedByOpponent.Add(question);
            // Note: We do NOT invalidate cache here because
            // the player's question bar is not affected
        }
    }

    /// <summary>
    /// Checks if the current player has already asked a question.
    /// </summary>
    public bool WasQuestionAskedByPlayer(SCR_Question question)
    {
        return askedByCurrentPlayer.Contains(question);
    }

    /// <summary>
    /// Checks if a question has already been asked by anyone.
    /// </summary>
    public bool WasQuestionAsked(SCR_Question question)
    {
        return askedByCurrentPlayer.Contains(question) || askedByOpponent.Contains(question);
    }

    /// <summary>
    /// Clears all asked questions history.
    /// </summary>
    public void ClearAskedHistory()
    {
        askedByCurrentPlayer.Clear();
        askedByOpponent.Clear();
        currentUnaskedIndex = 0;
        InvalidateCache();
    }

    #endregion

    #region Category Methods
    /// <summary>
    /// Gets all questions in a specific category.
    /// </summary>
    public List<SCR_Question> GetQuestionsByCategory(QuestionCategory category)
    {
        return questionsByCategory.TryGetValue(category, out var questions)
            ? questions
            : new List<SCR_Question>();
    }
    #endregion

    #region AI Helper Methods
    /// <summary>
    /// Gets the best question for AI to ask based on remaining characters.
    /// AI selects from questions not asked by anyone yet.
    /// </summary>
    /// <param name="remainingCharacters">List of characters still in play</param>
    /// <returns>The best question to ask</returns>
    public SCR_Question GetBestQuestionForAI(List<SCR_Character> remainingCharacters)
    {
        if (remainingCharacters == null || remainingCharacters.Count <= 1)
            return null;

        SCR_Question bestQuestion = null;
        float bestScore = -1f;

        // AI selects from truly unasked questions (not asked by player or AI)
        foreach (var question in GetUnaskedQuestions())
        {
            float score = CalculateQuestionScore(question, remainingCharacters);

            if (score > bestScore)
            {
                bestScore = score;
                bestQuestion = question;
            }
        }

        return bestQuestion;
    }

    /// <summary>
    /// Calculates how good a question is for eliminating characters.
    /// A score of 0.5 means it splits the characters evenly (optimal).
    /// </summary>
    private float CalculateQuestionScore(SCR_Question question, List<SCR_Character> characters)
    {
        int matchCount = 0;
        int totalCount = characters.Count;

        foreach (var character in characters)
        {
            if (question.MatchesCharacter(character))
            {
                matchCount++;
            }
        }

        float ratio = (float)matchCount / totalCount;

        // Score is higher when ratio is closer to 0.5 (splits evenly)
        return 1f - Mathf.Abs(0.5f - ratio);
    }
    #endregion
}
