using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Question Bar UI at the bottom of the screen.
/// Handles question display, navigation with arrows, and sending questions.
/// </summary>
public class QuestionBarController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Button sendButton;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color disabledColor = Color.gray;

    [Header("Question Counter")]
    [SerializeField] private TMP_Text questionCounterText;
    [SerializeField] private bool showCounter = true;

    // Events
    public System.Action<SCR_Question> OnQuestionSent;
    public System.Action<SCR_Question> OnQuestionChanged;

    private SCR_Question currentQuestion;
    private bool isActive = true;

    #region Unity Lifecycle
    private void Awake()
    {
        SetupButtonListeners();
    }

    private void Start()
    {
        UpdateDisplay();
    }
    #endregion

    #region Setup
    private void SetupButtonListeners()
    {
        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.RemoveAllListeners();
            leftArrowButton.onClick.AddListener(OnLeftArrowClicked);
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.RemoveAllListeners();
            rightArrowButton.onClick.AddListener(OnRightArrowClicked);
        }

        if (sendButton != null)
        {
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }
    }
    #endregion

    #region Button Handlers
    private void OnLeftArrowClicked()
    {
        if (!isActive || QuestionManager.Instance == null) return;

        currentQuestion = QuestionManager.Instance.GetPreviousQuestion();
        UpdateDisplay();
        OnQuestionChanged?.Invoke(currentQuestion);

        // Optional: Add sound effect or animation
        PlayNavigationFeedback();
    }

    private void OnRightArrowClicked()
    {
        if (!isActive || QuestionManager.Instance == null) return;

        currentQuestion = QuestionManager.Instance.GetNextQuestion();
        UpdateDisplay();
        OnQuestionChanged?.Invoke(currentQuestion);

        // Optional: Add sound effect or animation
        PlayNavigationFeedback();
    }

    private void OnSendButtonClicked()
    {
        if (!isActive || currentQuestion == null) return;

        // Mark question as asked
        QuestionManager.Instance.MarkQuestionAsAsked(currentQuestion);

        // Notify listeners (GameManager will handle the game logic)
        OnQuestionSent?.Invoke(currentQuestion);

        // Move to next question for convenience
        currentQuestion = QuestionManager.Instance.GetNextQuestion();
        UpdateDisplay();
    }
    #endregion

    #region Display Updates
    /// <summary>
    /// Updates the question display with the current question.
    /// </summary>
    public void UpdateDisplay()
    {
        if (QuestionManager.Instance == null) return;

        currentQuestion = QuestionManager.Instance.CurrentQuestion;

        // Update question text
        if (questionText != null)
        {
            questionText.text = currentQuestion != null
                ? currentQuestion.QuestionText
                : "No questions available";
        }

        // Update counter
        if (showCounter && questionCounterText != null)
        {
            int current = QuestionManager.Instance.CurrentIndex + 1;
            int total = QuestionManager.Instance.TotalQuestions;
            questionCounterText.text = $"{current}/{total}";
        }
    }

    /// <summary>
    /// Sets whether the question bar is interactive.
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;

        if (leftArrowButton != null)
        {
            leftArrowButton.interactable = active;
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.interactable = active;
        }

        if (sendButton != null)
        {
            sendButton.interactable = active;
        }
    }

    /// <summary>
    /// Shows or hides the question bar.
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    #endregion

    #region Feedback
    private void PlayNavigationFeedback()
    {
        // Placeholder for sound/animation feedback
        // Add your own implementation:
        // AudioManager.Instance?.PlayClickSound();
    }
    #endregion

    #region Public API
    /// <summary>
    /// Gets the currently displayed question.
    /// </summary>
    public SCR_Question GetCurrentQuestion()
    {
        return currentQuestion;
    }

    /// <summary>
    /// Manually sets a specific question to display.
    /// </summary>
    public void SetQuestion(SCR_Question question)
    {
        currentQuestion = question;
        UpdateDisplay();
    }
    #endregion
}
