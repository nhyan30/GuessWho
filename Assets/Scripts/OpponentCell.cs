using UnityEngine;

/// <summary>
/// Represents a cell in the opponent's grid (AI or multiplayer opponent).
/// Only handles showing/hiding the crossout image.
/// </summary>
public class OpponentCell : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject crossOutImage;
    [SerializeField] private UnityEngine.UI.Image characterImage;

    private SCR_Character character;
    private bool isEliminated;

    public SCR_Character Character => character;
    public bool IsEliminated => isEliminated;

    /// <summary>
    /// Sets the character data for this cell.
    /// </summary>
    public void SetCell(SCR_Character _character)
    {
        character = _character;
        
        if (characterImage != null && character != null)
        {
            characterImage.sprite = character.characterSprite;
        }
        
        MarkAsEliminated(false);
    }

    /// <summary>
    /// Shows or hides the crossout image.
    /// </summary>
    public void MarkAsEliminated(bool eliminated)
    {
        isEliminated = eliminated;

        if (crossOutImage != null)
            crossOutImage.SetActive(eliminated);
    }
}
