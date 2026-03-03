using System.Collections.Generic;

/// <summary>
/// Utility class for filtering characters based on questions.
/// </summary>
public static class CharacterFilter
{
    /// <summary>
    /// Gets characters to eliminate based on question and answer.
    /// </summary>
    public static List<SCR_Character> GetCharactersToEliminate(
        List<SCR_Character> characters,
        SCR_Question question,
        bool answerIsYes)
    {
        var result = new List<SCR_Character>();

        foreach (var c in characters)
        {
            bool matches = question.MatchesCharacter(c);
            // If answer is YES, eliminate characters that DON'T match
            // If answer is NO, eliminate characters that DO match
            bool shouldEliminate = answerIsYes ? !matches : matches;
            if (shouldEliminate) result.Add(c);
        }

        return result;
    }

    /// <summary>
    /// Gets remaining characters after elimination.
    /// </summary>
    public static List<SCR_Character> GetRemainingCharacters(
        List<SCR_Character> characters,
        SCR_Question question,
        bool answerIsYes)
    {
        var result = new List<SCR_Character>();

        foreach (var c in characters)
        {
            bool matches = question.MatchesCharacter(c);
            // If answer is YES, keep characters that match
            // If answer is NO, keep characters that don't match
            bool shouldKeep = answerIsYes ? matches : !matches;
            if (shouldKeep) result.Add(c);
        }

        return result;
    }

    /// <summary>
    /// Gets count of characters matching a question.
    /// </summary>
    public static int GetMatchCount(List<SCR_Character> characters, SCR_Question question)
    {
        int count = 0;
        foreach (var c in characters)
        {
            if (question.MatchesCharacter(c)) count++;
        }
        return count;
    }

    /// <summary>
    /// Gets count of characters NOT matching a question.
    /// </summary>
    public static int GetNonMatchCount(List<SCR_Character> characters, SCR_Question question)
    {
        int count = 0;
        foreach (var c in characters)
        {
            if (!question.MatchesCharacter(c)) count++;
        }
        return count;
    }
}
