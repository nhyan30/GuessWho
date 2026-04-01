using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Question Bar UI at the bottom of the screen.
/// Handles question display, navigation with arrows, and sending questions.
/// 
/// IMPORTANT: Only shows questions the CURRENT PLAYER hasn't asked yet.
/// - In Single Player: AI questions do NOT affect player's question bar
/// - In Multiplayer: Each player's question bar is independent
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

        // Play button sound
        AudioManager.Instance?.PlayButtonClick();

        currentQuestion = QuestionManager.Instance.GetPreviousQuestion();
        UpdateDisplay();
        OnQuestionChanged?.Invoke(currentQuestion);
    }

    private void OnRightArrowClicked()
    {
        if (!isActive || QuestionManager.Instance == null) return;

        // Play button sound
        AudioManager.Instance?.PlayButtonClick();

        currentQuestion = QuestionManager.Instance.GetNextQuestion();
        UpdateDisplay();
        OnQuestionChanged?.Invoke(currentQuestion);
    }

    private void OnSendButtonClicked()
    {
        if (!isActive || currentQuestion == null) return;

        // Play button sound
        AudioManager.Instance?.PlayButtonClick();

        // Mark question as asked by THIS player
        // This removes it from THIS player's question bar only
        // (not from opponent's/AI's perspective)
        QuestionManager.Instance.MarkQuestionAsAsked(currentQuestion);

        // Notify listeners (GameManager will handle the game logic)
        OnQuestionSent?.Invoke(currentQuestion);

        // Move to next available question
        currentQuestion = QuestionManager.Instance.CurrentQuestion;
        UpdateDisplay();
    }
    #endregion

    #region Display Updates
    /// <summary>
    /// Updates the question display with the current question available to this player.
    /// Shows count of questions this player hasn't asked yet.
    /// </summary>
    public void UpdateDisplay()
    {
        if (QuestionManager.Instance == null) return;

        // Get current question available to this player
        currentQuestion = QuestionManager.Instance.CurrentQuestion;

        // Update question text
        if (questionText != null)
        {
            if (currentQuestion != null)
            {
                questionText.text = currentQuestion.QuestionText;
                questionText.color = normalColor;
            }
            else
            {
                questionText.text = "No questions remaining!";
                questionText.color = disabledColor;
            }
        }

        // Update counter to show questions this player hasn't asked
        if (showCounter && questionCounterText != null)
        {
            int remaining = QuestionManager.Instance.UnaskedCount;

            if (remaining > 0)
            {
                int current = QuestionManager.Instance.CurrentIndex + 1;
                questionCounterText.text = $"{current}/{remaining}";
            }
            else
            {
                questionCounterText.text = "0/0";
            }
        }

        // Update button interactability based on available questions
        bool hasQuestions = currentQuestion != null;
        if (sendButton != null)
        {
            sendButton.interactable = hasQuestions && isActive;
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
            sendButton.interactable = active && currentQuestion != null;
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