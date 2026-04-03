using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Static utility class that handles character elimination logic based on questions and answers.
/// 
/// Arabic Edition:
/// Properly handles Hijab logic - characters wearing hijab have their hair covered,
/// so they should be eliminated when hair color questions are asked.
/// </summary>
public static class CharacterFilter
{
    /// <summary>
    /// Gets characters that should be eliminated based on a question and the answer.
    /// </summary>
    /// <param name="characters">Current list of possible characters</param>
    /// <param name="question">The question that was asked</param>
    /// <param name="answerIsYes">The answer received (Yes/No)</param>
    /// <returns>List of characters to eliminate</returns>
    public static List<SCR_Character> GetCharactersToEliminate(
        List<SCR_Character> characters,
        SCR_Question question,
        bool answerIsYes)
    {
        if (characters == null || question == null)
            return new List<SCR_Character>();

        var toEliminate = new List<SCR_Character>();

        foreach (var character in characters)
        {
            bool shouldEliminate = ShouldCharacterBeEliminated(character, question, answerIsYes);
            if (shouldEliminate)
            {
                toEliminate.Add(character);
            }
        }

        return toEliminate;
    }

    /// <summary>
    /// Gets the remaining characters after elimination.
    /// </summary>
    public static List<SCR_Character> GetRemainingCharacters(
        List<SCR_Character> characters,
        SCR_Question question,
        bool answerIsYes)
    {
        return characters
            .Where(c => !ShouldCharacterBeEliminated(c, question, answerIsYes))
            .ToList();
    }

    /// <summary>
    /// Determines if a character should be eliminated based on the question and answer.
    /// 
    /// Special handling for Arabic Edition (Hijab):
    /// - Characters with hijab have their hair covered
    /// - When asking about ANY hair color, characters with hijab shouldn't be eliminated
    ///   because their hair is not visible (cannot confirm or deny hair color)
    /// </summary>
    private static bool ShouldCharacterBeEliminated(
        SCR_Character character,
        SCR_Question question,
        bool answerIsYes)
    {
        if (character == null || question == null)
            return false;

        // Special case: Hair color questions
        // Arabic Edition: Characters with hijab should be eliminated when hair color is asked
        // because their hair is covered and not visible
        if (question.attributeToCheck == CharacterAttribute.HasHairColor)
        {
            // If character has hijab, Don't eliminate them regardless of the answer
            // because we cannot determine their hair color
            if (character.hasHijab)
            {
                return false;
            }

            // Normal hair color comparison for characters without hijab
            bool characterMatches = character.hairColor == question.targetHairColor;
            return characterMatches != answerIsYes;
        }

        // For all other attributes (including HasHijab), use normal matching logic
        bool matches = question.MatchesCharacter(character);
        return matches != answerIsYes;
    }
}