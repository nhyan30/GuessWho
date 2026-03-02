using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages questions for the game.
/// Tracks separately which questions player and AI have asked.
/// </summary>
public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }

    [Header("Questions")]
    [SerializeField] private List<SCR_Question> allQuestions = new List<SCR_Question>();

    private int currentIndex;
    private HashSet<SCR_Question> askedByPlayer = new HashSet<SCR_Question>();
    private HashSet<SCR_Question> askedByAI = new HashSet<SCR_Question>();

    public SCR_Question CurrentQuestion => allQuestions.Count > 0 ? allQuestions[currentIndex] : null;
    public int TotalQuestions => allQuestions.Count;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Gets the next UNASKED question for player navigation.
    /// Skips questions that have been asked by the player.
    /// </summary>
    public SCR_Question GetNextQuestion()
    {
        if (allQuestions.Count == 0) return null;

        // Try to find next unasked question
        int startIndex = currentIndex;
        do
        {
            currentIndex = (currentIndex + 1) % allQuestions.Count;
            if (!askedByPlayer.Contains(allQuestions[currentIndex]))
            {
                return CurrentQuestion;
            }
        } while (currentIndex != startIndex);

        // All questions asked, return current anyway
        return CurrentQuestion;
    }

    /// <summary>
    /// Gets the previous UNASKED question for player navigation.
    /// Skips questions that have been asked by the player.
    /// </summary>
    public SCR_Question GetPreviousQuestion()
    {
        if (allQuestions.Count == 0) return null;

        int startIndex = currentIndex;
        do
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = allQuestions.Count - 1;

            if (!askedByPlayer.Contains(allQuestions[currentIndex]))
            {
                return CurrentQuestion;
            }
        } while (currentIndex != startIndex);

        return CurrentQuestion;
    }

    /// <summary>
    /// Marks a question as asked by the PLAYER.
    /// </summary>
    public void MarkQuestionAsAsked(SCR_Question question)
    {
        if (question != null)
        {
            askedByPlayer.Add(question);
            Debug.Log($"[QM] Player asked: {question.QuestionText}");
        }
    }

    /// <summary>
    /// Marks a question as asked by the AI.
    /// </summary>
    public void MarkQuestionAsAskedAI(SCR_Question question)
    {
        if (question != null)
        {
            askedByAI.Add(question);
            Debug.Log($"[QM] AI asked: {question.QuestionText}");
        }
    }

    /// <summary>
    /// Gets questions that haven't been asked by the PLAYER.
    /// </summary>
    public List<SCR_Question> GetUnaskedQuestions()
    {
        return allQuestions.Where(q => !askedByPlayer.Contains(q)).ToList();
    }

    /// <summary>
    /// Gets questions that haven't been asked by the AI.
    /// </summary>
    public List<SCR_Question> GetUnaskedQuestionsAI()
    {
        return allQuestions.Where(q => !askedByAI.Contains(q)).ToList();
    }

    /// <summary>
    /// Gets questions that haven't been asked by EITHER player or AI.
    /// </summary>
    public List<SCR_Question> GetAvailableQuestions()
    {
        return allQuestions.Where(q => !askedByPlayer.Contains(q) && !askedByAI.Contains(q)).ToList();
    }

    public void ClearAskedHistory()
    {
        askedByPlayer.Clear();
        askedByAI.Clear();
    }

    public void ResetToFirstQuestion()
    {
        currentIndex = 0;

        // Find first unasked question
        if (askedByPlayer.Count > 0)
        {
            for (int i = 0; i < allQuestions.Count; i++)
            {
                if (!askedByPlayer.Contains(allQuestions[i]))
                {
                    currentIndex = i;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Gets the best question for AI to ask (from AI's unasked questions).
    /// </summary>
    public SCR_Question GetBestQuestionForAI(List<SCR_Character> characters)
    {
        if (characters == null || characters.Count <= 1) return null;

        var unaskedForAI = GetUnaskedQuestionsAI();
        if (unaskedForAI.Count == 0) return null;

        SCR_Question best = null;
        float bestScore = -1f;

        foreach (var q in unaskedForAI)
        {
            float score = CalculateScore(q, characters);
            if (score > bestScore)
            {
                bestScore = score;
                best = q;
            }
        }

        return best;
    }

    private float CalculateScore(SCR_Question question, List<SCR_Character> characters)
    {
        int match = 0;
        foreach (var c in characters)
            if (question.MatchesCharacter(c)) match++;

        float ratio = (float)match / characters.Count;
        return 1f - Mathf.Abs(0.5f - ratio);
    }

    /// <summary>
    /// Checks if a question has been asked by the player.
    /// </summary>
    public bool WasAskedByPlayer(SCR_Question question)
    {
        return askedByPlayer.Contains(question);
    }

    /// <summary>
    /// Checks if a question has been asked by the AI.
    /// </summary>
    public bool WasAskedByAI(SCR_Question question)
    {
        return askedByAI.Contains(question);
    }
}
