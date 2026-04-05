using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the circle wipe animation for page transitions.
/// Animation flow:
/// - AnimateOut: Circle closes to cover the screen (use before switching content)
/// - AnimateIn: Circle opens to reveal the screen (use after switching content)
/// </summary>
public class WipeController : MonoBehaviour
{
    public static WipeController Instance { get; private set; }

    private Animator _animator;
    private Image _image;
    private readonly int _circleSizeId = Shader.PropertyToID("_CircleSize");

    [Header("Settings")]
    [Tooltip("Duration of the wipe animation (should match animation clip length)")]
    [SerializeField] private float wipeAnimationDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Events
    public event Action OnWipeOutComplete;
    public event Action OnWipeInComplete;

    // State
    private bool isAnimating = false;

    // Current animation callback
    private Action currentAnimationCallback;

    public float circleSize = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _image = GetComponent<Image>();

        // Ensure image starts invisible
        if (_image != null)
        {
            _image.raycastTarget = false;
        }
    }

    private void Update()
    {
        // Update the material property for the shader
        if (_image != null)
        {
            _image.materialForRendering.SetFloat(_circleSizeId, circleSize);
        }
    }

    /// <summary>
    /// Plays the wipe OUT animation (circle closes to cover screen).
    /// Used BEFORE switching content between pages.
    /// </summary>
    /// <param name="onComplete">Callback when animation completes</param>
    public void AnimateOut(Action onComplete = null)
    {
        if (isAnimating)
        {
            LogDebug("Animation already in progress, ignoring AnimateOut request");
            return;
        }

        LogDebug("Starting AnimateOut (circle closing)");
        currentAnimationCallback = onComplete;
        isAnimating = true;

        // Enable raycast blocking during animation
        if (_image != null)
        {
            _image.raycastTarget = true;
        }

        // Trigger the animation
        if (_animator != null)
        {
            _animator.SetTrigger("Out");
        }

        // Start coroutine to wait for animation completion
        StartCoroutine(WaitForAnimationComplete(wipeAnimationDuration, OnOutAnimationComplete));
    }

    /// <summary>
    /// Plays the wipe IN animation (circle opens to reveal screen).
    /// Used AFTER switching content between pages.
    /// </summary>
    /// <param name="onComplete">Callback when animation completes</param>
    public void AnimateIn(Action onComplete = null)
    {
        if (isAnimating)
        {
            LogDebug("Animation already in progress, ignoring AnimateIn request");
            return;
        }

        LogDebug("Starting AnimateIn (circle opening)");
        currentAnimationCallback = onComplete;
        isAnimating = true;

        // Enable raycast blocking during animation
        if (_image != null)
        {
            _image.raycastTarget = true;
        }

        // Trigger the animation
        if (_animator != null)
        {
            _animator.SetTrigger("In");
        }

        // Start coroutine to wait for animation completion
        StartCoroutine(WaitForAnimationComplete(wipeAnimationDuration, OnInAnimationComplete));
    }

    /// <summary>
    /// Plays a full transition: Out -> switch -> In
    /// </summary>
    /// <param name="onSwitchContent">Callback to switch content when screen is covered</param>
    /// <param name="onComplete">Callback when entire transition completes</param>
    public void PlayTransition(Action onSwitchContent, Action onComplete = null)
    {
        StartCoroutine(TransitionCoroutine(onSwitchContent, onComplete));
    }

    private System.Collections.IEnumerator TransitionCoroutine(Action onSwitchContent, Action onComplete)
    {
        // Phase 1: Wipe out (cover screen)
        bool outComplete = false;
        AnimateOut(() => outComplete = true);
        yield return new WaitUntil(() => outComplete);

        // Phase 2: Switch content
        LogDebug("Switching content during transition");
        onSwitchContent?.Invoke();
        yield return null; // Wait one frame for content to update

        // Phase 3: Wipe in (reveal screen)
        //bool inComplete = false;
        //AnimateIn(() => inComplete = true);
        //yield return new WaitUntil(() => inComplete);

        yield return new WaitForSeconds(0.5f);
        AnimateIn();

        // Phase 4: Complete
        onComplete?.Invoke();
        LogDebug("Transition complete");
    }

    private System.Collections.IEnumerator WaitForAnimationComplete(float duration, Action onComplete)
    {
        yield return new WaitForSeconds(duration);
        onComplete?.Invoke();
    }

    private void OnOutAnimationComplete()
    {
        LogDebug("AnimateOut complete");
        isAnimating = false;
        OnWipeOutComplete?.Invoke();
        currentAnimationCallback?.Invoke();
        currentAnimationCallback = null;
    }

    private void OnInAnimationComplete()
    {
        LogDebug("AnimateIn complete");
        isAnimating = false;

        // Disable raycast blocking after animation
        if (_image != null)
        {
            _image.raycastTarget = false;
        }

        OnWipeInComplete?.Invoke();
        currentAnimationCallback?.Invoke();
        currentAnimationCallback = null;
    }

    /// <summary>
    /// Checks if an animation is currently playing.
    /// </summary>
    public bool IsAnimating => isAnimating;

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[WipeController] {message}");
    }
}