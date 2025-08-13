using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseWalkSpeed = 5f;
    public float baseCrawlSpeed = 2f;
    public float baseJumpHeight = 3f;

    [Header("Quality Modifiers")]
    public float walkSpeedQuality = 1.0f;
    public float jumpHeightQuality = 1.0f;
    public float proneColliderQuality = 1.0f;
    public float crawlColliderQuality = 1.0f;

    [Header("Quality Settings")]
    [Range(0.3f, 1.0f)]
    public float minQualityMultiplier = 0.3f; // Minimum performance at lowest quality

    // Collider settings for quality-based prone/crawl
    private Vector2 perfectProneColliderSize = new Vector2(0.78f, 0.53f);
    private Vector2 perfectProneColliderOffset = new Vector2(0.03f, -0.22f);
    private Vector2 poorProneColliderSize = new Vector2(0.78f, 0.73f); // Higher collider for poor quality
    private Vector2 poorProneColliderOffset = new Vector2(0.03f, -0.12f);

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayerMask = 1;

    [Header("Overhead Detection")]
    public Transform overheadCheck;
    public float overheadCheckDistance = 0.5f; // Distance to check above player
    public float overheadCheckWidth = 0.6f; // Width of the overhead check
    public LayerMask overheadLayerMask = 1; // What layers count as obstacles above

    [Header("Unlockable Abilities")]
    public bool canWalk = false;
    public bool canJump = false;
    public bool canProne = false;
    public bool canCrawl = false;

    [Header("Movement Control")]
    private bool canMove = true; // For death system control

    [Header("Audio")]
    public AudioSource playerAudioSource;
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.3f;
    public float crawlSoundInterval = 0.5f; // Crawling sounds slower

    // Components
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private SpriteRenderer spriteRenderer;

    // Collider
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    // Movement variables
    private bool isGrounded;
    private bool wasMoving = false;
    private bool wasGrounded = false;
    private bool isJumping = false;
    private bool isProne = false; // True when crouched (either static prone or crawling)
    private bool isCrawling = false; // True when moving while crouched
    private bool isObstructedAbove = false; // True when there's a collider above preventing standing

    // Input storage for FixedUpdate
    private float horizontalInput;
    private bool jumpInput;
    private bool proneHeld; // S key held down
    private bool pronePressed; // S key just pressed

    // Audio timing
    private float lastFootstepTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerAudioSource = GetComponent<AudioSource>();
        col = GetComponent<BoxCollider2D>();

        // Store original collider values
        if (col != null)
        {
            originalColliderSize = col.size;
            originalColliderOffset = col.offset;
        }

        // Set up rigidbody constraints
        rb.freezeRotation = true;

        // Create ground check point if it doesn't exist
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;
        }

        // Create overhead check point if it doesn't exist
        if (overheadCheck == null)
        {
            GameObject overheadCheckObj = new GameObject("OverheadCheck");
            overheadCheckObj.transform.SetParent(transform);
            overheadCheckObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            overheadCheck = overheadCheckObj.transform;
        }
    }

    void Update()
    {
        if (!canMove) return; // Death system check

        CheckGrounded();
        CheckOverheadObstruction();
        HandleInput();
        HandleAnimations();
    }

    void FixedUpdate()
    {
        if (!canMove) return; // Death system check

        // Handle physics-based movement in FixedUpdate
        HandleMovement();
    }

    // Method to enable/disable player movement (for death system)
    public void SetCanMove(bool canMove)
    {
        this.canMove = canMove;

        // If movement is disabled, stop all input processing
        if (!canMove)
        {
            // Stop any current movement
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Keep Y velocity for gravity
            }
        }
    }

    void CheckOverheadObstruction()
    {
        if (overheadCheck == null) return;

        // Use a box cast to check for obstacles above the player
        // This checks if the player would collide with something if they tried to stand up
        Vector2 boxSize = new Vector2(overheadCheckWidth, 0.1f);
        Vector2 castDirection = Vector2.up;

        // Cast from the player's current position upward
        RaycastHit2D hit = Physics2D.BoxCast(
            overheadCheck.position,
            boxSize,
            0f,
            castDirection,
            overheadCheckDistance,
            overheadLayerMask
        );

        isObstructedAbove = hit.collider != null;

        // Debug visualization (remove in production if desired)
        if (isObstructedAbove)
        {
            Debug.DrawRay(overheadCheck.position, castDirection * hit.distance, Color.red);
        }
        else
        {
            Debug.DrawRay(overheadCheck.position, castDirection * overheadCheckDistance, Color.green);
        }
    }

    void HandleInput()
    {
        if (!canMove) return; // Death system check

        // Quick hack to fix input between different keyboard loayouts
        float qInput = Input.GetKey(KeyCode.A) ? -1f : 0f; // AZERTY Q (physical A position)
        float dInput = Input.GetKey(KeyCode.D) ? 1f : 0f;  // AZERTY D (same position)
        horizontalInput = qInput + dInput;

        // Jumping input (check for key press) - can't jump while prone
        if (Input.GetKeyDown(KeyCode.Space) && canJump && isGrounded && !isProne)
        {
            jumpInput = true;
        }

        // Prone input handling
        proneHeld = Input.GetKey(KeyCode.S) && canProne && isGrounded;
        pronePressed = Input.GetKeyDown(KeyCode.S) && canProne && isGrounded;

        // Update prone state based on S key and overhead obstruction
        if (pronePressed && !isProne)
        {
            // Start crouching
            isProne = true;
            UpdateColliderSize();
        }
        else if (!proneHeld && isProne && !isObstructedAbove)
        {
            // Stop crouching (S key released AND no obstruction above)
            isProne = false;
            isCrawling = false; // Also stop crawling
            UpdateColliderSize();
        }
        else if (!proneHeld && isProne && isObstructedAbove)
        {
            // do nothing
            Debug.Log("Can't stand up - obstruction above!");
        }

        // Update crawling state (only when prone and moving)
        if (isProne && canCrawl && Mathf.Abs(horizontalInput) > 0.1f)
        {
            isCrawling = true;
        }
        else if (isProne)
        {
            isCrawling = false; // Static prone
        }
    }

    void HandleMovement()
    {
        if (!canMove) return; // Death system check

        // Determine current movement capabilities
        bool canCurrentlyWalk = canWalk && !isProne &&
                               (PlayerAnimationManager.Instance == null ||
                                !PlayerAnimationManager.Instance.IsPlayingReverseTransition);

        bool canCurrentlyCrawl = canCrawl && isProne && isCrawling &&
                                (PlayerAnimationManager.Instance == null ||
                                 !PlayerAnimationManager.Instance.IsPlayingReverseTransition);

        // Handle movement based on current state
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            if (canCurrentlyCrawl)
            {
                Crawl(horizontalInput);
            }
            else if (canCurrentlyWalk)
            {
                Walk(horizontalInput);
            }
            else if (isProne && !canCrawl)
            {
                // Player is prone but can't crawl - stop movement
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                Debug.Log("Can't move while prone - need crawl ability!");
            }
            else
            {
                // Stop horizontal movement while preserving vertical velocity
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
        else
        {
            // Stop horizontal movement while preserving vertical velocity
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // Handle jumping (can't jump while prone OR during reverse transition)
        if (jumpInput)
        {
            bool canCurrentlyJump = canJump && isGrounded && !isProne &&
                                   (PlayerAnimationManager.Instance == null ||
                                    !PlayerAnimationManager.Instance.IsPlayingReverseTransition);

            if (canCurrentlyJump)
            {
                Jump();
            }
            jumpInput = false; // Reset jump input
        }
    }

    void Walk(float direction)
    {
        if (!canMove) return; // Death system check

        float actualSpeed = baseWalkSpeed * walkSpeedQuality;

        // Set horizontal velocity while preserving vertical velocity
        rb.linearVelocity = new Vector2(direction * actualSpeed, rb.linearVelocity.y);

        // Play footstep sounds at intervals
        if (Time.time - lastFootstepTime >= footstepInterval && footstepSounds.Length > 0)
        {
            PlayFootStepSound();
            lastFootstepTime = Time.time;
        }

        // Flip sprite based on direction
        if (direction > 0)
            spriteRenderer.flipX = false;
        else if (direction < 0)
            spriteRenderer.flipX = true;
    }

    void Crawl(float direction)
    {
        if (!canMove) return; // Death system check

        float actualSpeed = baseCrawlSpeed;

        // Set horizontal velocity while preserving vertical velocity
        rb.linearVelocity = new Vector2(direction * actualSpeed, rb.linearVelocity.y);

        // Play crawl sounds at intervals (slower than footsteps)
        if (Time.time - lastFootstepTime >= crawlSoundInterval && footstepSounds.Length > 0)
        {
            PlayFootStepSound(); // Same sound as walking
            lastFootstepTime = Time.time;
        }

        // Flip sprite based on direction
        if (direction > 0)
            spriteRenderer.flipX = false;
        else if (direction < 0)
            spriteRenderer.flipX = true;
    }

    private void PlayFootStepSound()
    {
        if (playerAudioSource != null && footstepSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, footstepSounds.Length);
            playerAudioSource.PlayOneShot(footstepSounds[randomIndex]);
        }
    }

    void Jump()
    {
        if (!canMove) return; // Death system check

        float actualJumpHeight = baseJumpHeight * jumpHeightQuality;

        // Calculate jump velocity needed to reach desired height
        float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y * rb.gravityScale) * actualJumpHeight);

        // Set vertical velocity
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);

        // Set jumping state
        isJumping = true;

        // Trigger jump animation if available
        if (PlayerAnimationManager.Instance != null &&
            PlayerAnimationManager.Instance.HasAnimation("jump"))
        {
            PlayerAnimationManager.Instance.TriggerJumpAnimation();
        }
    }

    private void UpdateColliderSize()
    {
        if (col == null) return;

        if (isProne)
        {
            float qualityToUse;

            // Determine which quality to use (prone or crawl)
            if (isCrawling)
            {
                qualityToUse = crawlColliderQuality;
            } 
            else
            {
                qualityToUse = proneColliderQuality;
            }

            // Interpolate between poor and perfect collider settings based on quality
            Vector2 colliderSize = Vector2.Lerp(poorProneColliderSize, perfectProneColliderSize, qualityToUse);
            Vector2 colliderOffset = Vector2.Lerp(poorProneColliderOffset, perfectProneColliderOffset, qualityToUse);

            col.size = colliderSize;
            col.offset = colliderOffset;

            Debug.Log($"Prone collider updated - Quality: {qualityToUse:F2}, Size: {colliderSize}, Offset: {colliderOffset}");
        }
        else
        {
            // Restore original collider size when standing
            col.offset = originalColliderOffset;
            col.size = originalColliderSize;
        }
    }

    void CheckGrounded()
    {
        wasGrounded = isGrounded;

        // Check if player is touching ground using overlap circle
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);

        // Reset jumping state when we land
        if (!wasGrounded && isGrounded)
        {
            isJumping = false;
            HandleAnimations(); // Handle landing animations
        }
    }

    // Make HandleAnimations public so PlayerAnimationManager can call it after reverse prone animation
    public void HandleAnimations()
    {
        if (PlayerAnimationManager.Instance == null) return;

        // Don't interrupt reverse transition animations
        if (PlayerAnimationManager.Instance.IsPlayingReverseTransition)
        {
            Debug.Log("HandleAnimations blocked - reverse transition playing");
            return;
        }

        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;

        // Don't interrupt jump animation while jumping OR falling
        if (isJumping || (!isGrounded && rb.linearVelocity.y != 0))
        {
            // Don't change animations while in the air - let jump animation play
            if (PlayerAnimationManager.Instance.HasAnimation("jump") &&
                PlayerAnimationManager.Instance.currentAnimation?.animationType != "jump")
            {
                PlayerAnimationManager.Instance.TriggerJumpAnimation();
            }
            return;
        }

        // Handle crouched state (prone or crawling)
        if (isProne)
        {
            if (isCrawling && isMoving && PlayerAnimationManager.Instance.HasAnimation("crawl"))
            {
                // Player is moving while crouched - play crawl animation
                if (PlayerAnimationManager.Instance.currentAnimation?.animationType != "crawl")
                {
                    PlayerAnimationManager.Instance.TriggerCrawlAnimation();
                }
            }
            else if (canCrawl && PlayerAnimationManager.Instance.HasAnimation("prone"))
            {
                // Player is stationary while crouched but has crawl ability - use prone animation
                // This shows a static crawl-ready pose instead of animated crawling
                if (PlayerAnimationManager.Instance.currentAnimation?.animationType != "prone")
                {
                    PlayerAnimationManager.Instance.TriggerProneAnimation();
                }
            }
            else if (!canCrawl && PlayerAnimationManager.Instance.HasAnimation("prone"))
            {
                // Player doesn't have crawl ability yet - use prone animation
                if (PlayerAnimationManager.Instance.currentAnimation?.animationType != "prone")
                {
                    PlayerAnimationManager.Instance.TriggerProneAnimation();
                }
            }
            return;
        }

        // Handle standing animations (not crouched, not jumping)
        if (isGrounded && !isJumping && !isProne)
        {
            if (isMoving && canWalk && PlayerAnimationManager.Instance.HasAnimation("walk"))
            {
                // Only start walk animation if we weren't already walking
                if (!wasMoving || PlayerAnimationManager.Instance.currentAnimation?.animationType != "walk")
                {
                    PlayerAnimationManager.Instance.TriggerWalkAnimation();
                }
            }
            else if (!isMoving && PlayerAnimationManager.Instance.HasAnimation("idle"))
            {
                // Only start idle animation if we were moving or don't have a current animation
                if (wasMoving || PlayerAnimationManager.Instance.currentAnimation?.animationType != "idle")
                {
                    PlayerAnimationManager.Instance.TriggerIdleAnimation();
                }
            }
        }

        wasMoving = isMoving;
    }

    // Called when player completes walk animation
    public void UnlockWalking(float qualityMultiplier)
    {
        canWalk = true;
        walkSpeedQuality = Mathf.Clamp(qualityMultiplier, minQualityMultiplier, 1.0f);
        Debug.Log($"Walking unlocked! canWalk = {canWalk}, quality = {walkSpeedQuality:F2}");
    }

    // Called when player completes jump animation
    public void UnlockJumping(float qualityMultiplier)
    {
        canJump = true;
        jumpHeightQuality = Mathf.Clamp(qualityMultiplier, minQualityMultiplier, 1.0f);
        Debug.Log($"Jumping unlocked! canJump = {canJump}, quality = {jumpHeightQuality:F2}");
    }

    // Called when player completes prone animation
    public void UnlockProning(float qualityMultiplier)
    {
        canProne = true;
        proneColliderQuality = Mathf.Clamp(qualityMultiplier, minQualityMultiplier, 1.0f);
        Debug.Log($"Prone unlocked! canProne = {canProne}, quality = {proneColliderQuality:F2}");
    }

    // Called when player completes crawl animation
    public void UnlockCrawling(float qualityMultiplier)
    {
        canCrawl = true;
        crawlColliderQuality = Mathf.Clamp(qualityMultiplier, minQualityMultiplier, 1.0f);
        Debug.Log($"Crawling unlocked! canCrawl = {canCrawl}, quality = {crawlColliderQuality:F2}");
    }

    // Called when player removes walk animation
    public void LockWalking()
    {
        canWalk = false;
        walkSpeedQuality = 1.0f; // Reset to default
        Debug.Log($"Walking locked! canWalk = {canWalk}");

        // If player is currently moving, stop them
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f && !isProne)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    // Called when player removes jump animation
    public void LockJumping()
    {
        canJump = false;
        jumpHeightQuality = 1.0f; // Reset to default
        Debug.Log($"Jumping locked! canJump = {canJump}");
    }

    // Called when player removes prone animation
    public void LockProning()
    {
        canProne = false;
        proneColliderQuality = 1.0f; // Reset to default
        Debug.Log($"Prone locked! canProne = {canProne}");

        // If player is currently prone, force them to stand up (only if not obstructed)
        if (isProne && !isObstructedAbove)
        {
            isProne = false;
            isCrawling = false;
            UpdateColliderSize();
            HandleAnimations();
        }
    }

    // Called when player removes crawl animation
    public void LockCrawling()
    {
        canCrawl = false;
        crawlColliderQuality = 1.0f; // Reset to default
        Debug.Log($"Crawling locked! canCrawl = {canCrawl}");

        // If player is currently crawling, they can stay prone but can't move
        if (isCrawling)
        {
            isCrawling = false;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Stop movement
            HandleAnimations(); // Switch back to prone animation
        }
    }
}