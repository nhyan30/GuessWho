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
    private bool correctAnswerIsYes; // For AI questions - which button is correct
    private bool lastAIAnswer; // Store the last AI answer for player's question

    // Events
    public event Action OnOkayClicked;
    public event Action OnNegateClicked;
    public event Action<bool> OnAnswerClicked; // For Yes/No answers (AI questions)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

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
                    // Player clicked Yes on AI question
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
                    // Player clicked No on AI question
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
        SetButtons(showOkay: true, showNegate: false, bothInteractable: true);
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
        SetButtons(showOkay: true, showNegate: false, bothInteractable: true);
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
        SetButtons(showOkay: true, showNegate: true, bothInteractable: true);
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
        SetButtons(showOkay: true, showNegate: false, bothInteractable: true);
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
        SetButtons(showOkay: false, showNegate: false, bothInteractable: true);
        Show();
    }

    /// <summary>
    /// Shows the AI's answer to player's question
    /// </summary>
    public void ShowAIAnswer(bool answer)
    {
        currentType = PopupType.AIAnswer;
        lastAIAnswer = answer; // Store for later retrieval
        SetInfoText(answer ? "Yes!" : "No!");
        SetCharacterImage(null);
        SetButtons(showOkay: true, showNegate: false, bothInteractable: true);
        Show();
    }

    /// <summary>
    /// Shows AI's question for player to answer.
    /// Only the CORRECT answer button will be interactable.
    /// </summary>
    /// <param name="question">The question AI is asking</param>
    /// <param name="correctIsYes">True if "Yes" is the correct answer</param>
    public void ShowAIQuestion(SCR_Question question, bool correctIsYes)
    {
        currentType = PopupType.AIQuestion;
        currentQuestion = question;
        correctAnswerIsYes = correctIsYes;

        SetInfoText($"AI asks:\n{question.QuestionText}");
        SetCharacterImage(null);

        // Show both buttons, but only the correct one is interactable
        SetButtons(showOkay: true, showNegate: true, bothInteractable: false, correctIsYes: correctIsYes);

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
        SetButtons(showOkay: true, showNegate: true, bothInteractable: true);
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
        SetButtons(showOkay: true, showNegate: false, bothInteractable: true);
        Show();
    }

    #endregion

    #region Helper Methods

    private void Show()
    {
        if (popupPanel != null)
            popupPanel.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void SetInfoText(string text)
    {
        if (infoText != null)
            infoText.text = text;
    }

    private void SetCharacterImage(Sprite sprite)
    {
        if (characterImageContainer != null)
            characterImageContainer.SetActive(sprite != null);

        if (characterImage != null && sprite != null)
            characterImage.sprite = sprite;
    }

    /// <summary>
    /// Sets button visibility and interactability.
    /// </summary>
    /// <param name="showOkay">Show okay button</param>
    /// <param name="showNegate">Show negate button</param>
    /// <param name="bothInteractable">If true, both buttons work. If false, use correctIsYes.</param>
    /// <param name="correctIsYes">When bothInteractable is false, true means Yes is correct, false means No is correct.</param>
    private void SetButtons(bool showOkay, bool showNegate, bool bothInteractable, bool correctIsYes = true)
    {
        if (okayButton != null)
        {
            okayButton.gameObject.SetActive(showOkay);
            // If bothInteractable is true, button works. Otherwise, only if correctIsYes is true.
            okayButton.interactable = bothInteractable || correctIsYes;
        }

        if (negateButton != null)
        {
            negateButton.gameObject.SetActive(showNegate);
            // If bothInteractable is true, button works. Otherwise, only if correctIsYes is false.
            negateButton.interactable = bothInteractable || !correctIsYes;
        }

        if (buttonsContainer != null)
            buttonsContainer.SetActive(showOkay || showNegate);
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

    /// <summary>
    /// Gets the last AI answer (Yes/No) for player's question.
    /// </summary>
    public bool GetLastAnswer()
    {
        return lastAIAnswer;
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
