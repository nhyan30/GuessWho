using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single popup controller that handles all popup scenarios.
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
    [SerializeField] private GameObject buttonsContainer;
    [SerializeField] private GameObject characterImageContainer;

    // Current state
    private PopupType currentType;
    private SCR_Character currentCharacter;
    private SCR_Question currentQuestion;
    private bool correctAnswerIsYes;
    private bool lastAnswer;
    private bool lastAnswerResult;

    // Events
    public event Action OnOkayClicked;
    public event Action OnNegateClicked;
    public event Action<bool> OnAnswerClicked;

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

    public void ShowMessage(string message)
    {
        currentType = PopupType.Message;
        SetInfoText(message);
        SetCharacterImage(null);
        SetButtons(true, false, true);
        Show();
    }

    public void ShowCharacterSelect()
    {
        currentType = PopupType.CharacterSelect;
        SetInfoText("Select a Character");
        SetCharacterImage(null);
        SetButtons(true, false, true);
        Show();
    }

    public void ShowCharacterAgree(SCR_Character character)
    {
        currentType = PopupType.CharacterAgree;
        currentCharacter = character;
        SetInfoText("Is that correct?");
        SetCharacterImage(character?.characterSprite);
        SetButtons(true, true, true);
        Show();
    }

    public void ShowQuestionSelect()
    {
        currentType = PopupType.QuestionSelect;
        SetInfoText("Select a Question to ask!");
        SetCharacterImage(null);
        SetButtons(true, false, true);
        Show();
    }

    public void ShowAIThinking()
    {
        currentType = PopupType.AIThinking;
        SetInfoText("Opponent is thinking...");
        SetCharacterImage(null);
        SetButtons(false, false, true);
        Show();
    }

    public void ShowAIAnswer(bool answer)
    {
        currentType = PopupType.AIAnswer;
        lastAnswer = answer;
        SetInfoText(answer ? "Yes!" : "No!");
        SetCharacterImage(null);
        SetButtons(true, false, true);
        Show();
    }

    public void ShowAIQuestion(SCR_Question question, bool correctIsYes)
    {
        currentType = PopupType.AIQuestion;
        currentQuestion = question;
        correctAnswerIsYes = correctIsYes;

        SetInfoText($"Opponent asks:\n{question.QuestionText}");
        SetCharacterImage(null);
        SetButtons(true, true, false, correctIsYes);
        Show();
    }

    public void ShowGuessConfirm(SCR_Character character)
    {
        currentType = PopupType.GuessConfirm;
        currentCharacter = character;
        SetInfoText("Make your final guess?");
        SetCharacterImage(character?.characterSprite);
        SetButtons(true, true, true);
        Show();
    }

    public void ShowGameOver(bool playerWon, SCR_Character opponentCharacter)
    {
        currentType = PopupType.GameOver;
        SetInfoText(playerWon ? "You Win!" : "You Lose!");
        SetCharacterImage(opponentCharacter?.characterSprite);
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

    private void SetButtons(bool showOkay, bool showNegate, bool bothInteractable, bool correctIsYes = true)
    {
        if (okayButton != null)
        {
            okayButton.gameObject.SetActive(showOkay);
            okayButton.interactable = bothInteractable || correctIsYes;
        }

        if (negateButton != null)
        {
            negateButton.gameObject.SetActive(showNegate);
            negateButton.interactable = bothInteractable || !correctIsYes;
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
