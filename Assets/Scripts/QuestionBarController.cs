using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Question Bar UI with arrow navigation and send button.
/// Only shows questions that haven't been asked by the player yet.
/// </summary>
public class QuestionBarController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Button sendButton;

    // Events
    public System.Action<SCR_Question> OnQuestionSent;

    private SCR_Question currentQuestion;
    private bool isActive;

    private void Awake()
    {
        if (leftArrowButton != null)
            leftArrowButton.onClick.AddListener(OnLeftArrow);

        if (rightArrowButton != null)
            rightArrowButton.onClick.AddListener(OnRightArrow);

        if (sendButton != null)
            sendButton.onClick.AddListener(OnSend);
    }

    private void OnLeftArrow()
    {
        if (!isActive) return;
        currentQuestion = QuestionManager.Instance?.GetPreviousQuestion();
        UpdateDisplay();
    }

    private void OnRightArrow()
    {
        if (!isActive) return;
        currentQuestion = QuestionManager.Instance?.GetNextQuestion();
        UpdateDisplay();
    }

    private void OnSend()
    {
        if (!isActive || currentQuestion == null) return;

        OnQuestionSent?.Invoke(currentQuestion);

        // Move to next question for next turn
        currentQuestion = QuestionManager.Instance?.GetNextQuestion();
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (QuestionManager.Instance == null) return;

        currentQuestion = QuestionManager.Instance.CurrentQuestion;

        if (questionText != null)
        {
            if (currentQuestion == null)
            {
                questionText.text = "No questions available";
            }
            else if (QuestionManager.Instance.WasAskedByPlayer(currentQuestion))
            {
                // This shouldn't happen, but handle it
                questionText.text = "No more questions";
            }
            else
            {
                questionText.text = currentQuestion.QuestionText;
            }
        }

        // Disable send button if no valid question
        if (sendButton != null && isActive)
        {
            sendButton.interactable = currentQuestion != null &&
                                      !QuestionManager.Instance.WasAskedByPlayer(currentQuestion);
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;

        if (leftArrowButton != null) leftArrowButton.interactable = active;
        if (rightArrowButton != null) rightArrowButton.interactable = active;

        if (sendButton != null)
        {
            bool hasQuestion = currentQuestion != null &&
                              !QuestionManager.Instance?.WasAskedByPlayer(currentQuestion) == true;
            sendButton.interactable = active && hasQuestion;
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public SCR_Question GetCurrentQuestion() => currentQuestion;
}
