using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Guess Who/Character")]
public class SCR_Character : ScriptableObject
{
    public string characterName;
    public Sprite characterSprite;

    public bool isMale;
    public bool hasDarkSkin;

    public enum HairColors
    {
        Blonde,
        Black,
        Red,
        Brown
    }
    public HairColors hairColor;

    public bool hasHat;
    public bool hasBeard;
    public bool hasMustache;
    public bool hasGlasses;
    public bool hasEarrings;
}
