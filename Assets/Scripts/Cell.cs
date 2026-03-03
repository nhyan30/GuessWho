using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents a single character cell in the player's grid.
/// </summary>
public class Cell : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image characterImage;
    [SerializeField] private GameObject crossOutImage;
    [SerializeField] private Button cellButton;

    [Header("Visual Settings")]
    [SerializeField] private Color eliminatedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [SerializeField] private Color normalColor = Color.white;

    private SCR_Character character;
    private bool isEliminated;

    public SCR_Character Character => character;
    public bool IsEliminated => isEliminated;

    private void Awake()
    {
        if (cellButton == null) cellButton = GetComponent<Button>();
        if (cellButton != null)
        {
            cellButton.onClick.AddListener(OnClick);
        }
    }

    public void SetCell(SCR_Character _character)
    {
        character = _character;

        if (nameText != null)
            nameText.text = character.characterName;

        if (characterImage != null)
            characterImage.sprite = character.characterSprite;

        MarkAsEliminated(false);
    }

    private void OnClick()
    {
        if (isEliminated) return;

        GameManager.Instance?.OnCellClicked(character);
    }

    public void MarkAsEliminated(bool eliminated)
    {
        isEliminated = eliminated;

        if (crossOutImage != null)
            crossOutImage.SetActive(eliminated);

        if (characterImage != null)
            characterImage.color = eliminated ? eliminatedColor : normalColor;

        if (cellButton != null)
            cellButton.interactable = !eliminated;
    }
}
