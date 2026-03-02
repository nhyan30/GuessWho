using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents a single character cell in the grid.
/// </summary>
public class Cell : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Image hintHat;
    [SerializeField] private Image hintGlasses;
    [SerializeField] private Image hintBeard;
    [SerializeField] private Image hintMustache;
    [SerializeField] private Image hintEarrings;
    [SerializeField] private GameObject crossOutImage;
    [SerializeField] private Button cellButton;

    [Header("Visual Settings")]
    [SerializeField] private Color eliminatedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private float hintAlphaActive = 1f;
    [SerializeField] private float hintAlphaInactive = 0.3f;

    [Header("Cell Type")]
    [SerializeField] private bool isPlayerGrid = true; // True = player grid, False = AI grid

    private SCR_Character character;
    private bool isEliminated;

    public SCR_Character Character => character;
    public bool IsEliminated => isEliminated;
    public bool IsPlayerGrid => isPlayerGrid;

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

        UpdateHints();
        MarkAsEliminated(false);
    }

    private void UpdateHints()
    {
        if (character == null) return;

        SetHintAlpha(hintHat, character.hasHat);
        SetHintAlpha(hintGlasses, character.hasGlasses);
        SetHintAlpha(hintBeard, character.hasBeard);
        SetHintAlpha(hintMustache, character.hasMustache);
        SetHintAlpha(hintEarrings, character.hasEarrings);
    }

    private void SetHintAlpha(Image hint, bool active)
    {
        if (hint == null) return;
        Color c = hint.color;
        c.a = active ? hintAlphaActive : hintAlphaInactive;
        hint.color = c;
    }

    private void OnClick()
    {
        // Only player grid cells can be clicked
        if (!isPlayerGrid) return;
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

        // Only player grid cells need button interaction
        if (cellButton != null && isPlayerGrid)
            cellButton.interactable = !eliminated;
    }
}
