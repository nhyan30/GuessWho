using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Question Bar UI with arrow navigation and send button.
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

        QuestionManager.Instance?.MarkQuestionAsAsked(currentQuestion);
        OnQuestionSent?.Invoke(currentQuestion);

        currentQuestion = QuestionManager.Instance?.GetNextQuestion();
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (QuestionManager.Instance == null) return;

        currentQuestion = QuestionManager.Instance.CurrentQuestion;

        if (questionText != null)
        {
            questionText.text = currentQuestion != null
                ? currentQuestion.QuestionText
                : "No questions";
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;

        if (leftArrowButton != null) leftArrowButton.interactable = active;
        if (rightArrowButton != null) rightArrowButton.interactable = active;
        if (sendButton != null) sendButton.interactable = active;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public SCR_Question GetCurrentQuestion() => currentQuestion;
}
