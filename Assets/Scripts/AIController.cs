using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles AI logic for the Guess Who game.
/// </summary>
public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private AIDifficulty difficulty = AIDifficulty.Normal;

    private List<SCR_Character> possiblePlayerCharacters = new List<SCR_Character>();
    private SCR_Character aiSelectedCharacter;

    public int RemainingPossibleCharacters => possiblePlayerCharacters.Count;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Initialize(List<SCR_Character> allCharacters, SCR_Character selectedCharacter)
    {
        aiSelectedCharacter = selectedCharacter;
        possiblePlayerCharacters = new List<SCR_Character>(allCharacters);
        Debug.Log($"[AI] Selected: {aiSelectedCharacter.characterName}");
    }

    public void ResetAI()
    {
        possiblePlayerCharacters.Clear();
        aiSelectedCharacter = null;
    }

    public void ProcessPlayerAnswer(SCR_Question question, bool answerIsYes)
    {
        possiblePlayerCharacters = CharacterFilter.GetRemainingCharacters(
            possiblePlayerCharacters, question, answerIsYes);
        Debug.Log($"[AI] {possiblePlayerCharacters.Count} characters possible");
    }

    public SCR_Question SelectQuestion()
    {
        if (QuestionManager.Instance == null) return null;

        return difficulty switch
        {
            AIDifficulty.Easy => SelectRandomQuestion(),
            AIDifficulty.Normal => QuestionManager.Instance.GetBestQuestionForAI(possiblePlayerCharacters),
            AIDifficulty.Hard => QuestionManager.Instance.GetBestQuestionForAI(possiblePlayerCharacters),
            _ => QuestionManager.Instance.GetBestQuestionForAI(possiblePlayerCharacters)
        };
    }

    private SCR_Question SelectRandomQuestion()
    {
        var unasked = QuestionManager.Instance.GetUnaskedQuestionsAI();
        if (unasked.Count == 0) return null;
        return unasked[Random.Range(0, unasked.Count)];
    }

    public SCR_Character GetMostLikelyCharacter()
    {
        if (possiblePlayerCharacters.Count == 0) return null;
        return possiblePlayerCharacters[0];
    }

    public SCR_Character GetSelectedCharacter() => aiSelectedCharacter;
}

public enum AIDifficulty
{
    Easy,
    Normal,
    Hard
}
