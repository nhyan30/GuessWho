using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// Controls the main menu with fade transitions.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    [SerializeField] private CanvasGroup gameCanvasGroup;
    [SerializeField] private Button startButton;

    [Header("Game Reference")]
    [SerializeField] private GameManager gameManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Setup initial states
        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 1f;
            mainMenuCanvasGroup.blocksRaycasts = true;
        }

        if (gameCanvasGroup != null)
        {
            gameCanvasGroup.alpha = 0f;
            gameCanvasGroup.blocksRaycasts = false;
        }

        // Setup button listener
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
    }

    private void OnStartButtonClicked()
    {
        // Fade out main menu, then fade in game
        Fade(mainMenuCanvasGroup, false, () =>
        {
            Fade(gameCanvasGroup, true, () =>
            {
                // Start the game after fade in completes
                gameManager?.BeginGame();
            });
        });
    }

    /// <summary>
    /// Fades a canvas group in or out.
    /// </summary>
    /// <param name="canvasGroup">The canvas group to fade</param>
    /// <param name="visible">True to fade in, false to fade out</param>
    /// <param name="callback">Optional callback when fade completes</param>
    public void Fade(CanvasGroup canvasGroup, bool visible, UnityAction callback = null)
    {
        if (canvasGroup == null)
        {
            callback?.Invoke();
            return;
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOFade(visible ? 1 : 0, 0.2f).SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                if (visible)
                    canvasGroup.blocksRaycasts = true;
                callback?.Invoke();
            });
    }

    /// <summary>
    /// Returns to main menu with fade transition.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Fade(gameCanvasGroup, false, () =>
        {
            Fade(mainMenuCanvasGroup, true, null);
        });
    }
}
