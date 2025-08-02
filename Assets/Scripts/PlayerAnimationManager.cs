using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationManager : MonoBehaviour
{
    public static PlayerAnimationManager Instance;

    [Header("Frame Collection")]
    public List<FrameData> collectedFrames;

    [Header("Created Animations")]
    public List<CustomAnimation> createdAnimations;

    [Header("Current Animation State")]
    public CustomAnimation currentAnimation;
    public int currentFrameIndex = 0;
    public float animationTimer = 0f;
    public bool isAnimating = false;
    public bool isPlayingReverse = false;
    public bool isPlayingReverseTransition = false;

    // Public property to check if reverse transition is playing
    public bool IsPlayingReverseTransition => isPlayingReverseTransition;

    // References to player components
    private SpriteRenderer playerSpriteRenderer;
    private PlayerController playerController;
    private Rigidbody2D playerRigidbody;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            collectedFrames = new List<FrameData>();
            createdAnimations = new List<CustomAnimation>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Get player components
        playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerSpriteRenderer = playerController.GetComponent<SpriteRenderer>();
            playerRigidbody = playerController.GetComponent<Rigidbody2D>();
        }

        if (playerSpriteRenderer == null)
        {
            Debug.LogError("Could not find player spriteRenderer");
        }

        if (playerRigidbody == null)
        {
            Debug.LogError("Could not find player Rigidbody2D");
        }
    }

    void Update()
    {
        // Update current animation (with reverse support)
        if (isAnimating && currentAnimation != null && currentAnimation.frames.Count > 0)
        {
            animationTimer += Time.deltaTime;

            if (animationTimer >= currentAnimation.frameRate)
            {
                animationTimer = 0f;

                // Move to the next frame (forward or backward)
                if (isPlayingReverse)
                {
                    currentFrameIndex--;

                    // Handle reverse animation completion
                    if (currentFrameIndex < 0)
                    {
                        if (currentAnimation.isLooping)
                        {
                            currentFrameIndex = currentAnimation.frames.Count - 1;
                        }
                        else
                        {
                            currentFrameIndex = 0;
                            isAnimating = false;
                            isPlayingReverse = false;

                            // After reverse prone animation completes, handle standing animations
                            if (currentAnimation.animationType == "prone")
                            {
                                isPlayingReverseTransition = false; // End the transition lock
                                playerController.HandleAnimations();
                            }
                        }
                    }
                }
                else
                {
                    currentFrameIndex++;

                    // Handle forward animation completion
                    if (currentFrameIndex >= currentAnimation.frames.Count)
                    {
                        if (currentAnimation.isLooping)
                        {
                            currentFrameIndex = 0;
                        }
                        else
                        {
                            currentFrameIndex = currentAnimation.frames.Count - 1;
                            isAnimating = false;
                        }
                    }
                }

                // Update sprite
                UpdatePlayerSprite();
            }
        }
    }

    // UPDATED: Enhanced PlayAnimation method with reverse support
    public void PlayAnimation(string animationType, bool forceRestart = false, bool playReverse = false)
    {
        // Don't interrupt reverse transition animations unless forced
        if (isPlayingReverseTransition && !forceRestart)
        {
            Debug.Log($"Blocked animation '{animationType}' because reverse transition is playing");
            return;
        }

        CustomAnimation targetAnimation = createdAnimations.Find(anim => anim.animationType == animationType);

        if (targetAnimation == null)
        {
            Debug.LogWarning($"No animation found for type: {animationType}");
            return;
        }

        // Only change animation if it's different, we're forcing a restart, or changing direction
        if (currentAnimation != targetAnimation || forceRestart || isPlayingReverse != playReverse)
        {
            currentAnimation = targetAnimation;
            isPlayingReverse = playReverse;

            // Set the reverse transition flag for prone animations
            if (animationType == "prone" && playReverse)
            {
                isPlayingReverseTransition = true;
            }
            else
            {
                isPlayingReverseTransition = false;
            }

            // Set starting frame based on direction
            if (playReverse)
            {
                currentFrameIndex = currentAnimation.frames.Count - 1; // Start from last frame
            }
            else
            {
                currentFrameIndex = 0; // Start from first frame
            }

            animationTimer = 0f;
            isAnimating = true;
            UpdatePlayerSprite();

            Debug.Log($"Playing animation '{animationType}' - Reverse: {playReverse}, Starting frame: {currentFrameIndex}, Transition lock: {isPlayingReverseTransition}");
        }
    }

    public void CollectFrame(AnimationFramePickup frame)
    {
        FrameData frameData = new FrameData(frame.frameType, frame.frameSprite, frame.animationSprite);
        collectedFrames.Add(frameData);
    }

    public void AddFrameToInventory(string frameType, Sprite frameSprite, Sprite animationSprite)
    {
        FrameData frameData = new FrameData(frameType, frameSprite, animationSprite);
        collectedFrames.Add(frameData);
    }

    public void CreateCustomAnimation(string animationType, List<FrameData> frames, float frameRate, bool shouldLoop = true)
    {
        if (frames.Count == 0)
        {
            return;
        }

        // Remove existing animation of the same type
        createdAnimations.RemoveAll(anim => anim.animationType == animationType);

        // Create new animation with specified looping behavior
        CustomAnimation newAnimation = new CustomAnimation(animationType, frames, frameRate, shouldLoop);
        createdAnimations.Add(newAnimation);

        // If this animation type is currently playing, update it immediately
        if (currentAnimation != null && currentAnimation.animationType == animationType)
        {
            currentAnimation = newAnimation;
            currentFrameIndex = isPlayingReverse ? newAnimation.frames.Count - 1 : 0;
            animationTimer = 0f;
            UpdatePlayerSprite();
        }

        // Unlock the corresponding ability
        UnlockAbility(animationType);
    }

    void UnlockAbility(string animationType)
    {
        if (playerController == null) return;

        Debug.Log($"Unlocking ability for animation type: {animationType}");

        switch (animationType.ToLower())
        {
            case "walk":
                playerController.UnlockWalking(1.0f);
                Debug.Log("Walking ability unlocked!");
                break;
            case "jump":
                playerController.UnlockJumping(1.0f);
                Debug.Log("Jumping ability unlocked!");
                break;
            case "prone":
                playerController.UnlockProning(1.0f);
                Debug.Log("Prone ability unlocked!");
                break;
            case "idle":
                if (playerRigidbody != null && playerRigidbody.linearVelocity.magnitude < 0.1f)
                {
                    TriggerIdleAnimation();
                }
                Debug.Log("Idle animation available!");
                break;
            default:
                Debug.LogWarning($"No ability unlock defined for animation type: {animationType}");
                break;
        }
    }

    public void StopAnimation()
    {
        isAnimating = false;
        isPlayingReverse = false;
        isPlayingReverseTransition = false; // Reset transition lock
        currentAnimation = null;
    }

    void UpdatePlayerSprite()
    {
        if (playerSpriteRenderer != null && currentAnimation != null &&
            currentFrameIndex >= 0 && currentFrameIndex < currentAnimation.frames.Count)
        {
            playerSpriteRenderer.sprite = currentAnimation.frames[currentFrameIndex].GetAnimationSprite();
        }
    }

    // Public methods for other scripts to trigger animations
    public void TriggerWalkAnimation()
    {
        PlayAnimation("walk");
    }

    public void TriggerJumpAnimation()
    {
        PlayAnimation("jump", true); // Force restart for jump
    }

    public void TriggerIdleAnimation()
    {
        PlayAnimation("idle");
    }

    public void TriggerProneAnimation()
    {
        PlayAnimation("prone");
    }

    // Check if an animation exists
    public bool HasAnimation(string animationType)
    {
        return createdAnimations.Exists(anim => anim.animationType == animationType);
    }

    public void UseFrame(FrameData frameData)
    {
        collectedFrames.Remove(frameData);
    }

    public void RemoveCustomAnimation(string animationType)
    {
        createdAnimations.RemoveAll(anim => anim.animationType == animationType);

        if (currentAnimation != null && currentAnimation.animationType == animationType)
        {
            StopAnimation();
        }

        LockAbility(animationType);
    }

    void LockAbility(string animationType)
    {
        if (playerController == null) return;

        Debug.Log($"Locking ability for animation type: {animationType}");

        switch (animationType.ToLower())
        {
            case "walk":
                playerController.LockWalking();
                Debug.Log("Walking ability locked!");
                break;
            case "jump":
                playerController.LockJumping();
                Debug.Log("Jumping ability locked!");
                break;
            case "prone":
                playerController.LockProning();
                Debug.Log("Prone ability locked!");
                break;
            case "idle":
                if (currentAnimation != null && currentAnimation.animationType == "idle")
                {
                    StopAnimation();
                }
                Debug.Log("Idle animation removed!");
                break;
            default:
                Debug.LogWarning($"No ability lock defined for animation type: {animationType}");
                break;
        }
    }
}