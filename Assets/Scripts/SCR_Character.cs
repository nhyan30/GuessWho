using UnityEngine;

/// <summary>
/// ScriptableObject for character data.
/// Create via: Assets > Create > Guess Who > Character
/// </summary>
[CreateAssetMenu(fileName = "New Character", menuName = "Guess Who/Character")]
public class SCR_Character : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName;
    public Sprite characterSprite;

    [Header("Gender")]
    public bool isMale;

    [Header("Appearance")]
    public bool hasDarkSkin;

    public enum HairColors
    {
        Blonde,
        Black,
        Red,
        Brown
    }
    public HairColors hairColor;

    [Header("Accessories")]
    public bool hasHat;
    public bool hasGlasses;
    public bool hasEarrings;

    [Header("Facial Hair")]
    public bool hasBeard;
    public bool hasMustache;
}
