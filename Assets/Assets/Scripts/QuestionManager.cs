using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages all questions in the game. Handles question navigation,
/// filtering, and provides questions for both player and AI.
/// Uses Singleton pattern for easy access.
/// </summary>
public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }

    [Header("Question Database")]
    [SerializeField] private List<SCR_Question> allQuestions = new List<SCR_Question>();

    [Header("Settings")]
    [SerializeField] private bool shuffleQuestionsOnStart = true;

    // Current question index for navigation
    private int currentQuestionIndex = 0;

    // Track asked questions to avoid repetition - separate for player and AI
    private HashSet<SCR_Question> askedByPlayer = new HashSet<SCR_Question>();
    private HashSet<SCR_Question> askedByAI = new HashSet<SCR_Question>();

    // Combined set for backwards compatibility
    private HashSet<SCR_Question> askedQuestions => new HashSet<SCR_Question>(askedByPlayer.Concat(askedByAI));

    // Questions organized by category for efficient access
    private Dictionary<QuestionCategory, List<SCR_Question>> questionsByCategory;

    #region Properties
    public SCR_Question CurrentQuestion =>
        allQuestions.Count > 0 ? allQuestions[currentQuestionIndex] : null;

    public int TotalQuestions => allQuestions.Count;
    public int CurrentIndex => currentQuestionIndex;
    public int AskedCount => askedQuestions.Count;
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

    #region Navigation Methods
    /// <summary>
    /// Moves to the next question in the list.
    /// </summary>
    /// <returns>The next question, or null if at the end</returns>
    public SCR_Question GetNextQuestion()
    {
        if (allQuestions.Count == 0) return null;

        currentQuestionIndex = (currentQuestionIndex + 1) % allQuestions.Count;
        return CurrentQuestion;
    }

    /// <summary>
    /// Moves to the previous question in the list.
    /// </summary>
    /// <returns>The previous question, or null if at the beginning</returns>
    public SCR_Question GetPreviousQuestion()
    {
        if (allQuestions.Count == 0) return null;

        currentQuestionIndex--;
        if (currentQuestionIndex < 0)
        {
            currentQuestionIndex = allQuestions.Count - 1;
        }

        return CurrentQuestion;
    }

    /// <summary>
    /// Gets a question at a specific index.
    /// </summary>
    public SCR_Question GetQuestionAtIndex(int index)
    {
        if (index < 0 || index >= allQuestions.Count) return null;
        return allQuestions[index];
    }

    /// <summary>
    /// Resets the question index to the beginning.
    /// </summary>
    public void ResetToFirstQuestion()
    {
        currentQuestionIndex = 0;
    }
    #endregion

    #region Question Tracking
    /// <summary>
    /// Marks a question as asked by the player (to avoid repetition).
    /// </summary>
    public void MarkQuestionAsAsked(SCR_Question question)
    {
        if (question != null)
        {
            askedByPlayer.Add(question);
        }
    }

    /// <summary>
    /// Marks a question as asked by the AI (to avoid repetition).
    /// </summary>
    public void MarkQuestionAsAskedAI(SCR_Question question)
    {
        if (question != null)
        {
            askedByAI.Add(question);
        }
    }

    /// <summary>
    /// Checks if a question has already been asked.
    /// </summary>
    public bool WasQuestionAsked(SCR_Question question)
    {
        return askedByPlayer.Contains(question) || askedByAI.Contains(question);
    }

    /// <summary>
    /// Gets all unasked questions.
    /// </summary>
    public List<SCR_Question> GetUnaskedQuestions()
    {
        return allQuestions.Where(q => !askedByPlayer.Contains(q) && !askedByAI.Contains(q)).ToList();
    }

    /// <summary>
    /// Clears the asked questions history.
    /// </summary>
    public void ClearAskedHistory()
    {
        askedByPlayer.Clear();
        askedByAI.Clear();
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
    /// Uses a scoring system to find questions that eliminate the most characters.
    /// </summary>
    /// <param name="remainingCharacters">List of characters still in play</param>
    /// <returns>The best question to ask</returns>
    public SCR_Question GetBestQuestionForAI(List<SCR_Character> remainingCharacters)
    {
        if (remainingCharacters == null || remainingCharacters.Count <= 1)
            return null;

        SCR_Question bestQuestion = null;
        float bestScore = -1f;

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
        // Use inverted distance from 0.5
        return 1f - Mathf.Abs(0.5f - ratio);
    }
    #endregion
}
