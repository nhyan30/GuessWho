using UnityEngine;

/// <summary>
/// Categories for organizing questions in the game.
/// </summary>
public enum QuestionCategory
{
    Gender,         // Male/Female questions
    Accessories,    // Hat, glasses, earrings
    FacialHair,     // Beard, mustache
    HairColor,      // Hair color questions
    SkinTone        // Skin tone questions
}

/// <summary>
/// Types of character attributes that can be queried.
/// </summary>
public enum CharacterAttribute
{
    IsMale,
    HasDarkSkin,
    HasHat,
    HasGlasses,
    HasEarrings,
    HasBeard,
    HasMustache,
    HasHairColor
}

/// <summary>
/// ScriptableObject for question data.
/// Create via: Assets > Create > Guess Who > Question
/// </summary>
[CreateAssetMenu(fileName = "New Question", menuName = "Guess Who/Question")]
public class SCR_Question : ScriptableObject
{
    [Header("Question Text")]
    [TextArea(2, 4)]
    public string QuestionText;

    [Header("Question Category")]
    [Tooltip("Category for organizing and filtering questions")]
    public QuestionCategory Category = QuestionCategory.Gender;

    [Header("Question Type")]
    public CharacterAttribute attributeToCheck;

    [Header("For Boolean Attributes")]
    public bool expectedValue = true;

    [Header("For Hair Color")]
    public SCR_Character.HairColors targetHairColor;

    /// <summary>
    /// Checks if a character matches this question's criteria.
    /// </summary>
    public bool MatchesCharacter(SCR_Character character)
    {
        if (character == null) return false;

        return attributeToCheck switch
        {
            CharacterAttribute.IsMale => character.isMale == expectedValue,
            CharacterAttribute.HasDarkSkin => character.hasDarkSkin == expectedValue,
            CharacterAttribute.HasHat => character.hasHat == expectedValue,
            CharacterAttribute.HasGlasses => character.hasGlasses == expectedValue,
            CharacterAttribute.HasEarrings => character.hasEarrings == expectedValue,
            CharacterAttribute.HasBeard => character.hasBeard == expectedValue,
            CharacterAttribute.HasMustache => character.hasMustache == expectedValue,
            CharacterAttribute.HasHairColor => character.hairColor == targetHairColor,
            _ => false
        };
    }
}
