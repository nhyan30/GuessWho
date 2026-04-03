using UnityEngine;

/// <summary>
/// ScriptableObject for character data.
/// Create via: Assets > Create > Guess Who > Character
/// 
/// Arabic Edition:
/// - HasHijab replaces HasEarrings
/// - Characters with hijab have their hair covered (hair color not visible)
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
    [Tooltip("Hijab - If true, the character's hair is covered (Arabic Edition)")]
    public bool hasHijab;    // Replaced hasEarrings for Arabic edition

    [Header("Facial Hair")]
    public bool hasBeard;
    public bool hasMustache;
}