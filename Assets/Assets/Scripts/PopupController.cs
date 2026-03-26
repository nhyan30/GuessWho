using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single popup controller that handles all popup scenarios.
/// Supports combined "thinking + answer" animation for character elimination.
/// </summary>
public class PopupController : MonoBehaviour
{
    public static PopupController Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private GameObject infoGameObject;
    [SerializeField] private Image characterImage;
    [SerializeField] private Button okayButton;
    [SerializeField] private Button negateButton;
    [SerializeField] private GameObject buttonsContainer;
    [SerializeField] private GameObject characterImageContainer;

    [Header("Answer Display")]
    [SerializeField] private GameObject answerDisplayContainer;
    [SerializeField] private TMP_Text answerText;


    // Current state
    private PopupType currentType;
    private SCR_Character currentCharacter;
    private SCR_Question currentQuestion;
    private bool correctAnswerIsYes;
    private bool lastAnswer;

    // Events
    public event System.Action OnOkayClicked;
    public event System.Action OnNegateClicked;
    public event System.Action<bool> OnAnswerClicked;

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

    public void ShowMessage(string message, bool showOkay)
    {
        currentType = PopupType.Message;
        SetInfoText(true);
        SetInfoText(message);
        SetCharacterImage(null);
        HideAnswerDisplay();
        SetButtons(showOkay, false, showOkay);
        Show();
    }

    public void ShowCharacterSelect()
    {
        currentType = PopupType.CharacterSelect;
        SetInfoText(true);
        SetInfoText("Select a Character");
        SetCharacterImage(null);
        HideAnswerDisplay();
        SetButtons(true, false, true);
        Show();
    }

    public void ShowCharacterAgree(SCR_Character character)
    {
        currentType = PopupType.CharacterAgree;
        SetInfoText(true);
        currentCharacter = character;
        SetInfoText("Is that correct?");
        SetCharacterImage(character?.characterSprite);
        HideAnswerDisplay();
        SetButtons(true, true, true);
        Show();
    }

    public void ShowQuestionSelect()
    {
        currentType = PopupType.QuestionSelect;
        SetInfoText(true);
        SetInfoText("Select a Question to ask!");
        SetCharacterImage(null);
        HideAnswerDisplay();
        SetButtons(true, false, true);
        Show();
    }

    /// <summary>
    /// Shows thinking animation (for combined thinking + answer flow).
    /// Use UpdateAIThinkingToAnswer() to transition to showing the answer.
    /// </summary>
    public void ShowAIThinking()
    {
        currentType = PopupType.AIThinking;
        SetInfoText(true);
        if (MainMenuController.Instance.isMultiplayer == false)
        {
            SetInfoText("AI is thinking...");
        }
        else
        {
            SetInfoText("Opponent is thinking...");
        }
            SetCharacterImage(null);
        HideAnswerDisplay();
        SetButtons(false, false, false);  // No buttons while thinking
        Show();
    }

    /// <summary>
    /// Transitions the thinking popup to show the answer.
    /// Called by GameManager after the thinking delay.
    /// </summary>
    public void UpdateAIThinkingToAnswer(bool answer)
    {
        currentType = PopupType.AIAnswer;
        lastAnswer = answer;

        //SetInfoText("");  // Clear the "thinking" text
        SetInfoText(false);
        ShowAnswerDisplay(answer);
        SetButtons(true, false, true);  // Show OK button
    }

    /// <summary>
    /// Shows the AI's answer directly (without thinking animation).
    /// </summary>
    public void ShowAIAnswer(bool answer)
    {
        currentType = PopupType.AIAnswer;
        lastAnswer = answer;
        //SetInfoText("");
        SetInfoText(false);
        SetCharacterImage(null);
        ShowAnswerDisplay(answer);
        SetButtons(true, false, true);
        Show();
    }

    /// <summary>
    /// Shows when AI/opponent asks a question.
    /// Player must answer Yes or No.
    /// </summary>
    public void ShowAIQuestion(SCR_Question question, bool correctIsYes)
    {
        currentType = PopupType.AIQuestion;
        SetInfoText(true);
        currentQuestion = question;
        correctAnswerIsYes = correctIsYes;

        if (MainMenuController.Instance.isMultiplayer == false)
        {
            SetInfoText($"AI asks:\n{question.QuestionText}");
        }
        else
        {
            SetInfoText($"Opponent asks:\n{question.QuestionText}");
        }
        SetCharacterImage(null);
        HideAnswerDisplay();
        SetButtons(true, true, false, correctIsYes);
        Show();
    }

    public void ShowGuessConfirm(SCR_Character character)
    {
        currentType = PopupType.GuessConfirm;
        SetInfoText(true);
        currentCharacter = character;
        SetInfoText("Make your final guess?");
        SetCharacterImage(character?.characterSprite);
        HideAnswerDisplay();
        SetButtons(true, true, true);
        Show();
    }

    public void ShowGameOver(bool playerWon, SCR_Character opponentCharacter)
    {
        currentType = PopupType.GameOver;
        SetInfoText(true);
        SetInfoText(playerWon ? "You Win!" : "You Lose!");
        SetCharacterImage(opponentCharacter?.characterSprite);
        HideAnswerDisplay();
        SetButtons(true, false, true);
        Show();
    }

    #endregion

    #region Helpers

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

        HideAnswerDisplay();
    }

    private void SetInfoText(string text)
    {
        if (infoText != null)
            infoText.text = text;
    }

    private void SetInfoText(bool active)
    {
        if (infoGameObject != null)
            infoGameObject.SetActive(active);
    }

    private void SetCharacterImage(Sprite sprite)
    {
        if (characterImageContainer != null)
            characterImageContainer.SetActive(sprite != null);

        if (characterImage != null && sprite != null)
            characterImage.sprite = sprite;
    }

    /// <summary>
    /// Shows the answer display (Yes! or No! with appropriate styling).
    /// </summary>
    private void ShowAnswerDisplay(bool answer)
    {
        if (answerDisplayContainer != null)
            answerDisplayContainer.SetActive(true);

        if (answerText != null)
        {
            answerText.text = answer ? "Yes!" : "No!";
            answerText.color = answer ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
        }
    }

    private void HideAnswerDisplay()
    {
        if (answerDisplayContainer != null)
            answerDisplayContainer.SetActive(false);
    }

    /// <summary>
    /// Configures the button visibility and interactability.
    /// </summary>
    /// <param name="showOkay">Show the OK/Yes button</param>
    /// <param name="showNegate">Show the Negate/No button</param>
    /// <param name="bothInteractable">If true, both buttons are interactable. If false, uses correctIsYes.</param>
    /// <param name="correctIsYes">When bothInteractable is false, determines which button is interactable.</param>
    private void SetButtons(bool showOkay, bool showNegate, bool bothInteractable, bool correctIsYes = true)
    {
        if (okayButton != null)
        {
            okayButton.gameObject.SetActive(showOkay);
            okayButton.interactable = bothInteractable || correctIsYes;

            // Update button text for AI question
            if (currentType == PopupType.AIQuestion && showOkay)
            {
                var btnText = okayButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "Yes";
            }
            else if (showOkay)
            {
                // Reset to default "OK" text for other popup types
                var btnText = okayButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "OK";
            }
        }

        if (negateButton != null)
        {
            negateButton.gameObject.SetActive(showNegate);
            negateButton.interactable = bothInteractable || !correctIsYes;

            // Update button text for AI question
            if (currentType == PopupType.AIQuestion && showNegate)
            {
                var btnText = negateButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "No";
            }
            else if (showNegate)
            {
                // Reset to default "Cancel" text for other popup types
                var btnText = negateButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "Cancel";
            }
        }

        if (buttonsContainer != null)
            buttonsContainer.SetActive(showOkay || showNegate);
    }

    #endregion

    #region Public API

    public bool IsVisible() => popupPanel != null ? popupPanel.activeSelf : gameObject.activeSelf;
    public PopupType GetCurrentType() => currentType;
    public SCR_Character GetCurrentCharacter() => currentCharacter;
    public SCR_Question GetCurrentQuestion() => currentQuestion;
    public bool GetLastAnswer() => lastAnswer;

    #endregion
}

/// <summary>
/// Types of popups that can be displayed.
/// </summary>
public enum PopupType
{
    None,
    Message,
    CharacterSelect,
    CharacterAgree,
    QuestionSelect,
    AIThinking,     // Shows thinking animation
    AIAnswer,       // Shows the answer (Yes!/No!)
    AIQuestion,     // Shows question for player to answer
    GuessConfirm,
    GameOver
}