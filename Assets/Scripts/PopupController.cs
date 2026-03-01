using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single popup controller that handles all popup scenarios in the game.
/// Uses one prefab that can show/hide elements based on the current need.
/// </summary>
public class PopupController : MonoBehaviour
{
    public static PopupController Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Button okayButton;
    [SerializeField] private Button negateButton;

    [Header("Optional")]
    [SerializeField] private GameObject buttonsContainer;
    [SerializeField] private GameObject characterImageContainer;

    // Current state
    private PopupType currentType;
    private SCR_Character currentCharacter;
    private SCR_Question currentQuestion;
    private bool correctAnswer; // For AI questions

    // Events
    public event Action OnOkayClicked;
    public event Action OnNegateClicked;
    public event Action<bool> OnAnswerClicked; // For Yes/No answers

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        SetupButtons();
        Hide();
    }

    private void SetupButtons()
    {
        if (okayButton != null)
        {
            okayButton.onClick.AddListener(() =>
            {
                if (currentType == PopupType.AIQuestion)
                {
                    // During AI question, okay = Yes
                    OnAnswerClicked?.Invoke(true);
                }
                else
                {
                    OnOkayClicked?.Invoke();
                }
            });
        }

        if (negateButton != null)
        {
            negateButton.onClick.AddListener(() =>
            {
                if (currentType == PopupType.AIQuestion)
                {
                    // During AI question, negate = No
                    OnAnswerClicked?.Invoke(false);
                }
                else
                {
                    OnNegateClicked?.Invoke();
                }
            });
        }
    }

    #region Show Methods

    /// <summary>
    /// Shows a simple message popup with only an Okay button.
    /// </summary>
    public void ShowMessage(string message)
    {
        currentType = PopupType.Message;
        SetInfoText(message);
        SetCharacterImage(null);
        SetButtons(showOkay: true, showNegate: false);
        Show();
    }

    /// <summary>
    /// Shows the Character Select popup - "Select a Character"
    /// </summary>
    public void ShowCharacterSelect()
    {
        currentType = PopupType.CharacterSelect;
        SetInfoText("Select a Character");
        SetCharacterImage(null);
        SetButtons(showOkay: true, showNegate: false);
        Show();
    }

    /// <summary>
    /// Shows the Character Agree popup - "Is that correct?" with character image
    /// </summary>
    public void ShowCharacterAgree(SCR_Character character)
    {
        currentType = PopupType.CharacterAgree;
        currentCharacter = character;
        SetInfoText("Is that correct?");
        SetCharacterImage(character?.characterSprite);
        SetButtons(showOkay: true, showNegate: true);
        Show();
    }

    /// <summary>
    /// Shows the Question Select popup - "Select a Question to ask!"
    /// </summary>
    public void ShowQuestionSelect()
    {
        currentType = PopupType.QuestionSelect;
        SetInfoText("Select a Question to ask!");
        SetCharacterImage(null);
        SetButtons(showOkay: true, showNegate: false);
        Show();
    }

    /// <summary>
    /// Shows "AI is thinking..." with no buttons
    /// </summary>
    public void ShowAIThinking()
    {
        currentType = PopupType.AIThinking;
        SetInfoText("AI is thinking...");
        SetCharacterImage(null);
        SetButtons(showOkay: false, showNegate: false);
        Show();
    }

    /// <summary>
    /// Shows the AI's answer to player's question
    /// </summary>
    public void ShowAIAnswer(bool answer)
    {
        currentType = PopupType.AIAnswer;
        SetInfoText(answer ? "Yes!" : "No!");
        SetCharacterImage(null);
        SetButtons(showOkay: true, showNegate: false);
        Show();
    }

    /// <summary>
    /// Shows AI's question for player to answer
    /// Only the correct answer button will be interactable
    /// </summary>
    public void ShowAIQuestion(SCR_Question question, bool correctIsYes)
    {
        currentType = PopupType.AIQuestion;
        currentQuestion = question;
        correctAnswer = correctIsYes;
        
        SetInfoText($"AI asks:\n{question.QuestionText}");
        SetCharacterImage(null);
        SetButtons(showOkay: true, showNegate: true);
        
        
        // Only enable the correct answer button
        // The player must answer truthfully based on their character
        Show();
    }

    /// <summary>
    /// Shows the Guess Confirmation popup
    /// </summary>
    public void ShowGuessConfirm(SCR_Character character)
    {
        currentType = PopupType.GuessConfirm;
        currentCharacter = character;
        SetInfoText("Make your final guess?");
        SetCharacterImage(character?.characterSprite);
        SetButtons(showOkay: true, showNegate: true);
        Show();
    }

    /// <summary>
    /// Shows Game Over result
    /// </summary>
    public void ShowGameOver(bool playerWon, SCR_Character aiCharacter)
    {
        currentType = PopupType.GameOver;
        SetInfoText(playerWon ? "You Win!" : "AI Wins!");
        SetCharacterImage(aiCharacter?.characterSprite);
        SetButtons(showOkay: true, showNegate: false);
        Show();
    }

    #endregion

    #region Helper Methods

    private void Show()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void SetInfoText(string text)
    {
        if (infoText != null)
        {
            infoText.text = text;
        }
    }

    private void SetCharacterImage(Sprite sprite)
    {
        if (characterImageContainer != null)
        {
            characterImageContainer.SetActive(sprite != null);
        }
        
        if (characterImage != null && sprite != null)
        {
            characterImage.sprite = sprite;
        }
    }

    private void SetButtons(bool showOkay, bool showNegate)
    {
        if (okayButton != null)
        {
            okayButton.gameObject.SetActive(showOkay);
        }
        
        if (negateButton != null)
        {
            negateButton.gameObject.SetActive(showNegate);
        }

        if (buttonsContainer != null)
        {
            buttonsContainer.SetActive(showOkay || showNegate);
        }
    }

    #endregion

    #region Public API

    public bool IsVisible()
    {
        return popupPanel != null ? popupPanel.activeSelf : gameObject.activeSelf;
    }

    public PopupType GetCurrentType()
    {
        return currentType;
    }

    public SCR_Character GetCurrentCharacter()
    {
        return currentCharacter;
    }

    public SCR_Question GetCurrentQuestion()
    {
        return currentQuestion;
    }

    #endregion
}

/// <summary>
/// Types of popups the game uses
/// </summary>
public enum PopupType
{
    None,
    Message,
    CharacterSelect,
    CharacterAgree,
    QuestionSelect,
    AIThinking,
    AIAnswer,
    AIQuestion,
    GuessConfirm,
    GameOver
}
