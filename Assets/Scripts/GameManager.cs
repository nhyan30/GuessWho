using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [SerializeField] List<SCR_Character> characterList = new List<SCR_Character>();
    [SerializeField] Transform grid;
    [SerializeField] GameObject cellPrefab;

    SCR_Character selectedCharacter;
    SCR_Character aiSelectedCharacter;
    [Header("UI Content")]
    [SerializeField] GameObject characterSelectPopUp;
    [SerializeField] GameObject questionSelectPopUp;
    [Header("Agree Character Select")]
    [SerializeField] GameObject characterAgreePopup;
    [SerializeField] Button characterAgreeButton;
    [SerializeField] Image characterImage;
    [Header("Selected Character")]
    [SerializeField] Image selectedCharacterImage;
    [SerializeField] TMP_Text selectCharacterText;



    GameState currentState;

    void Awake()
    {
        instance = this;
        characterSelectPopUp.SetActive(false);
        questionSelectPopUp.SetActive(false);
        characterAgreePopup.SetActive(false);
        selectedCharacterImage.gameObject.SetActive(false);
    }

    void Start()
    {

        FillGrid();
        SetGameState(GameState.CharacterSelection);
        characterAgreeButton.onClick.AddListener(AgreeSelection);
    }

    void FillGrid()
    {
        // CLEAR THE GRID
        foreach (Transform child in grid)
        {
            Destroy(child.gameObject);
        }
        // FILL THE GRID
        for (int i = 0; i < characterList.Count; i++)
        {
            GameObject newCell = Instantiate(cellPrefab, grid, false);
            Cell cellScript = newCell.GetComponent<Cell>();
            cellScript.SetCell(characterList[i]);

            // cellScript.SetCell(characterList[i].characterName, 
            // characterList[i].characterSprite, 
            // characterList[i].hasHat, 
            // characterList[i].hasGlasses,
            // characterList[i].hasBeard,
            // characterList[i].hasMustache,
            // characterList[i].hasEarrings);

        }
    }

    void SetGameState(GameState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case GameState.CharacterSelection:
                // SHOW POP UP ABOUT SELECTION
                characterSelectPopUp.SetActive(true);
                // AI SHOULD PICK HIS CHARACTER
                SelectAICharacter();
                break;
            case GameState.PlayerTurn:
                questionSelectPopUp.SetActive(true);
                break;
            case GameState.AiTurn:

                break;
            case GameState.Guessing:

                break;
            case GameState.GameOver:

                break;
        }
    }

    void SelectAICharacter()
    {
        aiSelectedCharacter = characterList[Random.Range(0, characterList.Count)];
        Debug.Log("AI selected: " + aiSelectedCharacter.characterName);
    }

    public void SelectPlayerCharacter(SCR_Character _selectedCharacter)
    {
        selectedCharacter = _selectedCharacter;
        //show a pop up and ask if correct
        characterImage.sprite = selectedCharacter.characterSprite;
        characterAgreePopup.SetActive(true);

    }

    public GameState GetGameState()
    {
        return currentState;
    }

    void AgreeSelection()
    {
        characterAgreePopup.SetActive(false);
        //FILL IMAGE AND NAME IN OUR CORNER
        selectedCharacterImage.sprite = selectedCharacter.characterSprite;
        selectedCharacterImage.gameObject.SetActive(true);
        selectCharacterText.text = selectedCharacter.characterName;

        //CHANGE THE STATE
        SetGameState(GameState.PlayerTurn);

    }

}
