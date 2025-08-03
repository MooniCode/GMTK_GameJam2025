using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FrameCollectionChecker : MonoBehaviour
{
    public static FrameCollectionChecker Instance;

    [Header("Animation Interface Reference")]
    public AnimationInterface animationInterface;

    // Track which animation types have already shown notifications
    private HashSet<string> notifiedAnimationTypes = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("FrameCollectionChecker Instance created!");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("FrameCollectionChecker Start() called");

        // Find AnimationInterface if not assigned
        if (animationInterface == null)
        {
            animationInterface = FindObjectOfType<AnimationInterface>();
            Debug.Log("Auto-found AnimationInterface: " + (animationInterface != null));
        }

        if (animationInterface == null)
        {
            Debug.LogError("FrameCollectionChecker: Could not find AnimationInterface!");
        }
        else
        {
            Debug.Log("AnimationInterface found and assigned!");
        }
    }

    public void CheckAnimationCompletion(string frameType)
    {
        Debug.Log($"CheckAnimationCompletion called for frameType: {frameType}");

        // Don't check again if we've already notified for this animation type
        if (notifiedAnimationTypes.Contains(frameType.ToLower()))
        {
            Debug.Log($"Already notified for {frameType}, skipping...");
            return;
        }

        if (IsAnimationTypeComplete(frameType))
        {
            Debug.Log($"Animation type {frameType} is COMPLETE! Showing notification...");

            // Mark as notified
            notifiedAnimationTypes.Add(frameType.ToLower());

            // Show notification
            if (AnimationCompleteNotification.Instance != null)
            {
                AnimationCompleteNotification.Instance.ShowAnimationCompleteNotification(frameType);
            }
            else
            {
                Debug.LogError("AnimationCompleteNotification.Instance is null!");
            }
        }
        else
        {
            Debug.Log($"Animation type {frameType} is NOT complete yet.");
        }
    }

    bool IsAnimationTypeComplete(string animationType)
    {
        Debug.Log($"Checking if {animationType} is complete...");

        if (animationInterface == null)
        {
            Debug.LogError("animationInterface is null!");
            return false;
        }

        if (PlayerAnimationManager.Instance == null)
        {
            Debug.LogError("PlayerAnimationManager.Instance is null!");
            return false;
        }

        // Get the correct pattern for this animation type
        var config = animationInterface.animationConfigs.Find(c =>
            c.animationType.ToLower() == animationType.ToLower());

        if (config == null)
        {
            Debug.LogWarning($"No config found for animation type: {animationType}");
            return false;
        }

        if (config.correctPatternPrefabs == null)
        {
            Debug.LogWarning($"correctPatternPrefabs is null for animation type: {animationType}");
            return false;
        }

        Debug.Log($"Found config for {animationType} with {config.correctPatternPrefabs.Length} required frames");

        // Get all collected frames of this type
        var collectedFramesOfType = PlayerAnimationManager.Instance.collectedFrames
            .Where(frame => frame.frameType.ToLower() == animationType.ToLower())
            .ToList();

        Debug.Log($"Player has {collectedFramesOfType.Count} frames of type {animationType}");

        // Debug: List all collected frames of this type
        foreach (var frame in collectedFramesOfType)
        {
            Debug.Log($"  - Collected frame: {frame.frameType} with sprite: {(frame.GetUISprite() != null ? frame.GetUISprite().name : "null")}");
        }

        // Check if we have all required frames
        List<Sprite> requiredSprites = new List<Sprite>();
        foreach (var prefab in config.correctPatternPrefabs)
        {
            if (prefab != null)
            {
                requiredSprites.Add(prefab.frameSprite);
                Debug.Log($"  - Required sprite: {prefab.frameSprite.name}");
            }
        }

        Debug.Log($"Total required sprites: {requiredSprites.Count}");

        // Check if we have all required sprites
        int foundCount = 0;
        foreach (var requiredSprite in requiredSprites)
        {
            bool foundMatch = collectedFramesOfType.Any(frame =>
                frame.GetUISprite() == requiredSprite);

            if (foundMatch)
            {
                foundCount++;
                Debug.Log($"  ✓ Found required sprite: {requiredSprite.name}");
            }
            else
            {
                Debug.Log($"  ✗ Missing required sprite: {requiredSprite.name}");
            }
        }

        bool isComplete = foundCount == requiredSprites.Count;
        Debug.Log($"Animation '{animationType}' completion check: {foundCount}/{requiredSprites.Count} frames found. Complete: {isComplete}");

        return isComplete;
    }

    // Method to reset notifications (useful for testing or if player loses frames)
    public void ResetNotifications()
    {
        Debug.Log("Resetting all notifications");
        notifiedAnimationTypes.Clear();
    }

    // Method to manually check all animation types (useful for debugging)
    public void CheckAllAnimationTypes()
    {
        Debug.Log("Manually checking all animation types...");
        if (animationInterface == null) return;

        foreach (var config in animationInterface.animationConfigs)
        {
            CheckAnimationCompletion(config.animationType);
        }
    }

    // DEBUG METHOD - Call this to test the system
    [ContextMenu("Test Check Prone")]
    public void TestCheckProne()
    {
        Debug.Log("Testing prone animation completion check...");
        CheckAnimationCompletion("prone");
    }
}