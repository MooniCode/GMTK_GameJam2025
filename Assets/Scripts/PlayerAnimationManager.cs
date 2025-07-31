using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationManager : MonoBehaviour
{
    public static PlayerAnimationManager Instance; // Singleton reference

    [Header("Frame Collection")]
    public List<FrameData> collectedFrames;

    [Header("Created Animations")]
    public List<CustomAnimation> createdAnimations;

    [Header("Current Animation State")]
    public CustomAnimation currentAnimation;
    public int currentFrameIndex = 0;
    public float animationTimer = 0f;
    public bool isAnimating = false;

    // References to player components
    private SpriteRenderer playerSpriteRenderer;
    private PlayerController playerController;
    private Rigidbody2D playerRigidbody;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            collectedFrames = new List<FrameData>(); // Initialize the list
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
        // Update current animation
        if (isAnimating && currentAnimation != null && currentAnimation.frames.Count > 0)
        {
            animationTimer += Time.deltaTime;

            if (animationTimer >= currentAnimation.frameRate)
            {
                animationTimer = 0f;

                // Move to the next frame
                currentFrameIndex++;

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

                // Update sprite
                UpdatePlayerSprite();
            }
        }
    }

    public void CollectFrame(AnimationFramePickup frame)
    {
        // Create a FrameData copy before the original is destroyed
        FrameData frameData = new FrameData(frame.frameType, frame.frameSprite, frame.animationSprite);
        collectedFrames.Add(frameData);
    }

    public void CreateCustomAnimation(string animationType, List<FrameData> frames, float frameRate)
    {
        if (frames.Count == 0)
        {
            return;
        }

        // Remove existing animation of the same type
        createdAnimations.RemoveAll(anim => anim.animationType == animationType);

        // Create new animation
        CustomAnimation newAnimation = new CustomAnimation(animationType, frames, frameRate);
        createdAnimations.Add(newAnimation);

        // If this animation type is currently playing, update it immediately
        if (currentAnimation != null && currentAnimation.animationType == animationType)
        {
            currentAnimation = newAnimation;
            currentFrameIndex = 0; // Reset to first frame
            animationTimer = 0f;
            UpdatePlayerSprite();
        }

        // Unlock the corresponding ability
        UnlockAbility(animationType);
    }

    void UnlockAbility(string animationType)
    {
        if (playerController == null) return;

        switch (animationType.ToLower())
        {
            case "walk":
            case "run":
                playerController.UnlockWalking(1.0f);
                break;
            case "jump":
                playerController.UnlockJumping(1.0f);
                break;
            case "idle":
                // Immediately start idle animation if player is not moving
                if (playerRigidbody != null && playerRigidbody.linearVelocity.magnitude < 0.1f)
                {
                    TriggerIdleAnimation();
                }
                break;
            default:
                // No ability lock defined for animation type
                break;
        }
    }

    public void PlayAnimation(string animationType, bool forceRestart = false)
    {
        CustomAnimation targetAnimation = createdAnimations.Find(anim => anim.animationType == animationType);

        if (targetAnimation == null)
        {
            Debug.LogWarning($"No animation found for type: {animationType}");
            return;
        }

        // Only change animation if it's different or we're forcing a restart
        if (currentAnimation != targetAnimation || forceRestart)
        {
            currentAnimation = targetAnimation;
            currentFrameIndex = 0;
            animationTimer = 0f;
            isAnimating = true;
            UpdatePlayerSprite();
        }
    }

    public void StopAnimation()
    {
        isAnimating = false;
        currentAnimation = null;
    }

    void UpdatePlayerSprite()
    {
        if (playerSpriteRenderer != null && currentAnimation != null && currentFrameIndex < currentAnimation.frames.Count)
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

    // Check if an animation exists
    public bool HasAnimation(string animationType)
    {
        return createdAnimations.Exists(anim => anim.animationType == animationType);
    }

    public void UseFrame(FrameData frameData)
    {
        // Remove the frame from collected frames since it's now being used
        collectedFrames.Remove(frameData);
    }

    public void RemoveCustomAnimation(string animationType)
    {
        // Remove animation from created animations list
        createdAnimations.RemoveAll(anim => anim.animationType == animationType);

        // If the removed animation is currently playing, stop it
        if (currentAnimation != null && currentAnimation.animationType == animationType)
        {
            StopAnimation();
        }

        // Lock the corresponding ability
        LockAbility(animationType);
    }

    void LockAbility(string animationType)
    {
        if (playerController == null) return;

        switch (animationType.ToLower())
        {
            case "walk":
            case "run":
                playerController.LockWalking();
                break;
            case "jump":
                playerController.LockJumping();
                break;
            case "idle":
                // Stop idle animation if it's playing
                if (currentAnimation != null && currentAnimation.animationType == "idle")
                {
                    StopAnimation();
                }
                break;
            default:
                // No ability lock defined for animation type
                break;
        }
    }
}