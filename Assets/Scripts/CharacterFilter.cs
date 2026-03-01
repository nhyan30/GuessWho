using System.Collections.Generic;

/// <summary>
/// Utility class for filtering characters based on questions.
/// </summary>
public static class CharacterFilter
{
    public static List<SCR_Character> GetCharactersToEliminate(
        List<SCR_Character> characters,
        SCR_Question question,
        bool answerIsYes)
    {
        var result = new List<SCR_Character>();

        foreach (var c in characters)
        {
            bool matches = question.MatchesCharacter(c);
            bool shouldEliminate = answerIsYes ? !matches : matches;
            if (shouldEliminate) result.Add(c);
        }

        return result;
    }

    public static List<SCR_Character> GetRemainingCharacters(
        List<SCR_Character> characters,
        SCR_Question question,
        bool answerIsYes)
    {
        var result = new List<SCR_Character>();

        foreach (var c in characters)
        {
            bool matches = question.MatchesCharacter(c);
            bool shouldKeep = answerIsYes ? matches : !matches;
            if (shouldKeep) result.Add(c);
        }

        return result;
    }
}
