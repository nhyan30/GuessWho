using System;
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
    [SerializeField] private GameObject characterImageContainer;

    [Header("Buttons")]
    [SerializeField] private Button okayButton;
    [SerializeField] private Button negateButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private GameObject buttonsContainer;

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
    public event Action OnOkayClicked;
    public event Action OnNegateClicked;
    public event Action<bool> OnAnswerClicked;
    public event Action OnRestartClicked;
    public event Action OnMainMenuClicked;

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

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() =>
            {
                OnRestartClicked?.Invoke();
            });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(() =>
            {
                OnMainMenuClicked?.Invoke();
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
        HideGameOverButtons();
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
        HideGameOverButtons();
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
        HideGameOverButtons();
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
        HideGameOverButtons();
        SetButtons(true, false, true);
        Show();
    }

    public void ShowAIThinking()
    {
        currentType = PopupType.AIThinking;
        SetInfoText(true);
        if (MainMenuController.Instance != null && !MainMenuController.Instance.isMultiplayer)
        {
            SetInfoText("AI is thinking...");
        }
        else
        {
            SetInfoText("Opponent is thinking...");
        }
        SetCharacterImage(null);
        HideAnswerDisplay();
        HideGameOverButtons();
        SetButtons(false, false, false);
        Show();
    }

    public void UpdateAIThinkingToAnswer(bool answer)
    {
        currentType = PopupType.AIAnswer;
        lastAnswer = answer;

        SetInfoText(false);
        ShowAnswerDisplay(answer);
        HideGameOverButtons();
        SetButtons(true, false, true);
    }

    public void ShowAIAnswer(bool answer)
    {
        currentType = PopupType.AIAnswer;
        lastAnswer = answer;
        SetInfoText(false);
        SetCharacterImage(null);
        ShowAnswerDisplay(answer);
        HideGameOverButtons();
        SetButtons(true, false, true);
        Show();
    }

    public void ShowAIQuestion(SCR_Question question, bool correctIsYes)
    {
        currentType = PopupType.AIQuestion;
        SetInfoText(true);
        currentQuestion = question;
        correctAnswerIsYes = correctIsYes;

        if (MainMenuController.Instance != null && !MainMenuController.Instance.isMultiplayer)
        {
            SetInfoText($"{question.QuestionText}");
        }
        else
        {
            SetInfoText($"{question.QuestionText}");
        }
        SetCharacterImage(null);
        HideAnswerDisplay();
        HideGameOverButtons();
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
        HideGameOverButtons();
        SetButtons(true, true, true);
        Show();
    }

    /// <summary>
    /// Shows the AI's guess result with the guessed character's sprite.
    /// Format: "AI guessed: [name]" + character image + "AI Wins!" or wrong message.
    /// </summary>
    public void ShowAIGuessResult(SCR_Character guessedCharacter, bool isCorrect)
    {
        currentType = PopupType.Message;
        SetInfoText(true);

        if (isCorrect)
        {
            SetInfoText($"AI guessed: {guessedCharacter.characterName}\nAI Wins!");
        }
        else
        {
            SetInfoText($"AI guessed wrong: {guessedCharacter.characterName}");
        }

        // Show the guessed character's sprite
        SetCharacterImage(guessedCharacter?.characterSprite);
        HideAnswerDisplay();
        HideGameOverButtons();
        SetButtons(false, false, false);
        Show();
    }

    public void ShowGameOver(bool playerWon, SCR_Character opponentCharacter, bool isMultiplayer)
    {
        currentType = PopupType.GameOver;
        SetInfoText(true);

        string message = playerWon
            ? $"You Win!\nMy character is {opponentCharacter.characterName}"
            : $"You Lose!\nMy character is {opponentCharacter.characterName}";

        SetInfoText(message);
        SetCharacterImage(opponentCharacter?.characterSprite);
        HideAnswerDisplay();

        SetButtons(false, false, false);

        buttonsContainer.SetActive(true);

        ShowGameOverButtons(!isMultiplayer);

        Show();
    }

    private void ShowGameOverButtons(bool showRestart)
    {
        if (restartButton != null)
            restartButton.gameObject.SetActive(showRestart);

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(true);
    }

    private void HideGameOverButtons()
    {
        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(false);
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
        HideGameOverButtons();
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

    private void SetButtons(bool showOkay, bool showNegate, bool bothInteractable, bool correctIsYes = true)
    {
        if (okayButton != null)
        {
            okayButton.gameObject.SetActive(showOkay);
            okayButton.interactable = bothInteractable || correctIsYes;

            if (currentType == PopupType.AIQuestion && showOkay)
            {
                var btnText = okayButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "Yes";
            }
            else if (showOkay)
            {
                var btnText = okayButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "OK";
            }
        }

        if (negateButton != null)
        {
            negateButton.gameObject.SetActive(showNegate);
            negateButton.interactable = bothInteractable || !correctIsYes;

            if (currentType == PopupType.AIQuestion && showNegate)
            {
                var btnText = negateButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "No";
            }
            else if (showNegate)
            {
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