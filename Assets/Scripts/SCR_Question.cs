using UnityEngine;

/// <summary>
/// ScriptableObject that defines a question that can be asked about a character.
/// Each question has a text display and a filter predicate to check characters.
/// </summary>
[CreateAssetMenu(fileName = "New Question", menuName = "Guess Who/Question")]
public class SCR_Question : ScriptableObject
{
    [Header("Question Details")]
    [TextArea(2, 4)]
    [SerializeField] private string questionText;
    
    [Header("Filter Criteria")]
    [SerializeField] private CharacterAttribute attributeToCheck;
    
    [SerializeField] private bool expectedValue = true;
    
    // For Hair Color specific
    [SerializeField] private SCR_Character.HairColors targetHairColor;

    #region Properties
    public string QuestionText => questionText;
    public CharacterAttribute AttributeToCheck => attributeToCheck;
    #endregion

    /// <summary>
    /// Checks if a character matches the question's criteria.
    /// Used to determine the answer (Yes/No) when asking about a specific character.
    /// </summary>
    /// <param name="character">The character to check</param>
    /// <returns>True if the character matches the criteria</returns>
    public bool MatchesCharacter(SCR_Character character)
    {
        if (character == null) return false;

        return attributeToCheck switch
        {
            CharacterAttribute.IsMale => character.isMale == expectedValue,
            CharacterAttribute.HasDarkSkin => character.hasDarkSkin == expectedValue,
            CharacterAttribute.HasHat => character.hasHat == expectedValue,
            CharacterAttribute.HasBeard => character.hasBeard == expectedValue,
            CharacterAttribute.HasMustache => character.hasMustache == expectedValue,
            CharacterAttribute.HasGlasses => character.hasGlasses == expectedValue,
            CharacterAttribute.HasEarrings => character.hasEarrings == expectedValue,
            CharacterAttribute.HairColor => character.hairColor == targetHairColor,
            _ => false
        };
    }

    /// <summary>
    /// Gets the opposite question (for AI logic)
    /// </summary>
    public bool GetOppositeMatch(SCR_Character character)
    {
        return !MatchesCharacter(character);
    }
}

/// <summary>
/// Categories for organizing questions in the UI
/// </summary>
public enum QuestionCategory
{
    Appearance,     // Hair, skin, etc.
    Accessories,    // Hat, glasses, earrings
    FacialHair,     // Beard, mustache
    Gender
}

/// <summary>
/// Character attributes that can be queried
/// </summary>
public enum CharacterAttribute
{
    IsMale,
    HasDarkSkin,
    HasHat,
    HasBeard,
    HasMustache,
    HasGlasses,
    HasEarrings,
    HairColor
}
