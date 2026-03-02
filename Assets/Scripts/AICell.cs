using UnityEngine;

/// <summary>
/// Simple cell for AI's mini grid.
/// Only handles showing/hiding the crossout image.
/// Attach this to your AI cell prefab.
/// </summary>
public class AICell : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject crossOutImage;

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
