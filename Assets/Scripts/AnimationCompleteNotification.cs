using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnimationCompleteNotification : MonoBehaviour
{
    public static AnimationCompleteNotification Instance;

    [Header("Notification UI")]
    public GameObject notificationPanel;
    public Image animationTypeIcon;
    public TextMeshProUGUI notificationText;
    public RectTransform notificationRect;

    [Header("Animation Type Icons")]
    public Sprite idleIcon;
    public Sprite walkIcon;
    public Sprite jumpIcon;
    public Sprite proneIcon;
    public Sprite crawlIcon;

    [Header("Animation Settings")]
    public float displayDuration = 2f;
    public float slideSpeed = 500f;
    public Vector2 hiddenPosition = new Vector2(400f, 0f);
    public Vector2 visiblePosition = new Vector2(-200f, 0f);

    private Coroutine currentNotificationCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("AnimationCompleteNotification Instance created!");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("AnimationCompleteNotification Start() called");

        // Get the RectTransform if not assigned
        if (notificationRect == null && notificationPanel != null)
        {
            notificationRect = notificationPanel.GetComponent<RectTransform>();
            Debug.Log("Auto-assigned notificationRect: " + (notificationRect != null));
        }

        // Hide notification panel at start and position it off-screen
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
            if (notificationRect != null)
            {
                notificationRect.anchoredPosition = hiddenPosition;
                Debug.Log("Set initial position to: " + hiddenPosition);
            }
        }
        else
        {
            Debug.LogError("notificationPanel is null! Please assign it in the inspector.");
        }
    }

    public void ShowAnimationCompleteNotification(string animationType)
    {
        Debug.Log($"ShowAnimationCompleteNotification called for: {animationType}");

        // Stop any existing notification
        if (currentNotificationCoroutine != null)
        {
            Debug.Log("Stopping existing notification");
            StopCoroutine(currentNotificationCoroutine);
        }

        // Start new notification
        currentNotificationCoroutine = StartCoroutine(DisplayNotification(animationType));
    }

    IEnumerator DisplayNotification(string animationType)
    {
        Debug.Log($"DisplayNotification coroutine started for: {animationType}");

        // Set up the notification content
        SetNotificationContent(animationType);

        // Show the panel
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
            Debug.Log("Notification panel activated");
        }
        else
        {
            Debug.LogError("Cannot show notification - panel is null!");
            yield break;
        }

        // Slide in animation
        Debug.Log("Starting slide in animation");
        yield return StartCoroutine(SlideToPosition(visiblePosition));
        Debug.Log("Slide in animation completed");

        // Wait for display duration
        Debug.Log($"Waiting for display duration: {displayDuration} seconds");
        yield return new WaitForSeconds(displayDuration);

        // Slide out animation
        Debug.Log("Starting slide out animation");
        yield return StartCoroutine(SlideToPosition(hiddenPosition));
        Debug.Log("Slide out animation completed");

        // Hide the panel
        notificationPanel.SetActive(false);
        Debug.Log("Notification panel deactivated");

        currentNotificationCoroutine = null;
    }

    IEnumerator SlideToPosition(Vector2 targetPosition)
    {
        if (notificationRect == null)
        {
            Debug.LogError("Cannot slide - notificationRect is null!");
            yield break;
        }

        Vector2 startPosition = notificationRect.anchoredPosition;
        float elapsedTime = 0f;
        float duration = Vector2.Distance(startPosition, targetPosition) / slideSpeed;

        Debug.Log($"Sliding from {startPosition} to {targetPosition} over {duration} seconds");

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Use smooth step for nice easing
            t = t * t * (3f - 2f * t);

            notificationRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        // Ensure we end exactly at the target position
        notificationRect.anchoredPosition = targetPosition;
        Debug.Log($"Slide completed at position: {targetPosition}");
    }

    void SetNotificationContent(string animationType)
    {
        Debug.Log($"Setting notification content for: {animationType}");

        // Set the icon based on animation type
        if (animationTypeIcon != null)
        {
            Sprite iconSprite = GetIconForAnimationType(animationType);
            animationTypeIcon.sprite = iconSprite;
            Debug.Log($"Set icon sprite: {(iconSprite != null ? iconSprite.name : "null")}");
        }
        else
        {
            Debug.LogWarning("animationTypeIcon is null!");
        }

        // Set the text
        if (notificationText != null)
        {
            string formattedAnimationType = CapitalizeFirstLetter(animationType);
            string message = $"All {formattedAnimationType} frames collected! Animation ready to be made.";
            notificationText.text = message;
            Debug.Log($"Set notification text: {message}");
        }
        else
        {
            Debug.LogWarning("notificationText is null!");
        }
    }

    Sprite GetIconForAnimationType(string animationType)
    {
        switch (animationType.ToLower())
        {
            case "idle":
                return idleIcon;
            case "walk":
                return walkIcon;
            case "jump":
                return jumpIcon;
            case "prone":
                return proneIcon;
            case "crawl":
                return crawlIcon;
            default:
                Debug.LogWarning($"No icon defined for animation type: {animationType}");
                return idleIcon; // Fallback
        }
    }

    string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0]) + input.Substring(1).ToLower();
    }

    // Method to manually hide notification (useful for debugging or immediate hiding)
    public void HideNotification()
    {
        if (currentNotificationCoroutine != null)
        {
            StopCoroutine(currentNotificationCoroutine);
            currentNotificationCoroutine = null;
        }

        StartCoroutine(HideImmediately());
    }

    IEnumerator HideImmediately()
    {
        // Slide out quickly
        yield return StartCoroutine(SlideToPosition(hiddenPosition));
        notificationPanel.SetActive(false);
    }

    // DEBUG METHOD - Call this from inspector or another script to test
    [ContextMenu("Test Notification")]
    public void TestNotification()
    {
        Debug.Log("Testing notification manually...");
        ShowAnimationCompleteNotification("prone");
    }
}