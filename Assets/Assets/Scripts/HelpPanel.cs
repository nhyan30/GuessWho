using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Help panel that shows "How To Play" instructions.
/// </summary>
public class HelpPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private Button cancelButton;

    [Header("Help Text")]
    [SerializeField] private TMP_Text helpText;

    [TextArea(10, 20)]
    [SerializeField] private string howToPlayText = @"HOW TO PLAY

OBJECTIVE:
Guess your opponent's mystery character before they guess yours!

GAMEPLAY:

1. CHARACTER SELECTION
   - Select a character from the grid to be your mystery character
   - Your opponent (AI or another player) will do the same

2. ASKING QUESTIONS
   - Take turns asking Yes/No questions about the opponent's character
   - Use questions like ""Does your character have glasses?"" or ""Does your character have a hat?""
   - Navigate through questions using the arrow buttons
   - Press SEND to ask the current question

3. ELIMINATING CHARACTERS
   - Based on the answers, characters will be eliminated from your grid
   - Click on a character to cross them out manually if needed

4. MAKING A GUESS
   - When you think you know the opponent's character, press ""Do A Guess""
   - Click on the character you think is the mystery character
   - If correct, you win! If wrong, you lose!

MULTIPLAYER:
   - Host a game to create a room with a 6-digit code
   - Share the code with a friend on the same WiFi network
   - Join a game by entering the host's room code

TIPS:
   - Ask questions that eliminate the most characters
   - Pay attention to the opponent's questions for hints
   - Don't guess too early - make sure you're confident!";

    private void Awake()
    {
        SetupButtonListeners();
    }

    private void Start()
    {
        // Set the help text
        if (helpText != null && !string.IsNullOrEmpty(howToPlayText))
        {
            helpText.text = howToPlayText;
        }

        // Start hidden
        Hide();
    }

    private void SetupButtonListeners()
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }

    #region Button Handlers

    private void OnCancelClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Hide();
    }

    #endregion

    #region Show/Hide

    public void Show()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }
    }

    public bool IsVisible()
    {
        return helpPanel != null && helpPanel.activeSelf;
    }

    #endregion
}
