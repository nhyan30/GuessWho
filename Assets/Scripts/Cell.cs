using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] Image characterImage;
    [SerializeField] Image hintHat, hintGlasses, hintBeard, hintMustache, hintEarrings;
    [SerializeField] GameObject crossOutImage;
    SCR_Character character;
    Button cellButton;

    public void SetCell(SCR_Character _character)
    {
        cellButton = GetComponent<Button>();

        character = _character;

        nameText.text = character.name;
        characterImage.sprite = character.characterSprite;
        //DECIDE WHAT YOU WANT
        // hintHat.gameObject.SetActive(character.hasHat);
        // hintGlasses.gameObject.SetActive(character.hasGlasses);
        // hintBeard.gameObject.SetActive(character.hasBeard);
        // hintMustache.gameObject.SetActive(character.hasMustache);
        // hintEarrings.gameObject.SetActive(character.hasEarrings);

        // BASED ON COLOR
        //Hat
        Color hatColor = hintHat.color;
        hatColor.a = character.hasHat ? 1 : 0.5f;
        hintHat.color = hatColor;

        //Glasses
        Color glassesColor = hintGlasses.color;
        glassesColor.a = character.hasGlasses ? 1 : 0.5f;
        hintGlasses.color = glassesColor;

        //Beard
        Color beardColor = hintBeard.color;
        beardColor.a = character.hasBeard ? 1 : 0.5f;
        hintBeard.color = beardColor;

        //Mustache
        Color mustacheColor = hintMustache.color;
        mustacheColor.a = character.hasMustache ? 1 : 0.5f;
        hintMustache.color = mustacheColor;

        //Earrings
        Color earringsColor = hintEarrings.color;
        earringsColor.a = character.hasEarrings ? 1 : 0.5f;
        hintEarrings.color = earringsColor;

        MarkThisCharacter(false);  // Set Crossmark False on Game Start

        cellButton.onClick.AddListener(SelectCharacter);  // Calls SelectCharacter through AddListener on Click
    }

    public void MarkThisCharacter(bool mark)
    {
        crossOutImage.SetActive(mark);
    }

    void SelectCharacter()
    {
        if (GameManager.instance.GetGameState() == GameState.CharacterSelection)
        {
            GameManager.instance.SelectPlayerCharacter(character);
        }
        // LATER DO SOMETHING ELSE 

    }


}
