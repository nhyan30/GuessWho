using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages questions for the game.
/// </summary>
public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }

    [Header("Questions")]
    [SerializeField] private List<SCR_Question> allQuestions = new List<SCR_Question>();

    private int currentIndex;
    private HashSet<SCR_Question> askedQuestions = new HashSet<SCR_Question>();

    public SCR_Question CurrentQuestion => allQuestions.Count > 0 ? allQuestions[currentIndex] : null;
    public int TotalQuestions => allQuestions.Count;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public SCR_Question GetNextQuestion()
    {
        if (allQuestions.Count == 0) return null;
        currentIndex = (currentIndex + 1) % allQuestions.Count;
        return CurrentQuestion;
    }

    public SCR_Question GetPreviousQuestion()
    {
        if (allQuestions.Count == 0) return null;
        currentIndex--;
        if (currentIndex < 0) currentIndex = allQuestions.Count - 1;
        return CurrentQuestion;
    }

    public void MarkQuestionAsAsked(SCR_Question question)
    {
        if (question != null) askedQuestions.Add(question);
    }

    public List<SCR_Question> GetUnaskedQuestions()
    {
        return allQuestions.Where(q => !askedQuestions.Contains(q)).ToList();
    }

    public void ClearAskedHistory()
    {
        askedQuestions.Clear();
    }

    public void ResetToFirstQuestion()
    {
        currentIndex = 0;
    }

    public SCR_Question GetBestQuestionForAI(List<SCR_Character> characters)
    {
        if (characters == null || characters.Count <= 1) return null;

        SCR_Question best = null;
        float bestScore = -1f;

        foreach (var q in GetUnaskedQuestions())
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
}
