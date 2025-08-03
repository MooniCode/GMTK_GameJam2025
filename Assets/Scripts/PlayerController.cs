using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseWalkSpeed = 5f;
    public float baseCrawlSpeed = 2f; // Crawling is slower than walking
    public float baseJumpHeight = 3f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayerMask = 1;

    [Header("Unlocked Abilities")]
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
    public float crawlSoundInterval = 0.5f; // Crawling sounds are slower

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
    }

    void Update()
    {
        if (!canMove) return; // Death system check

        CheckGrounded();
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

    void HandleInput()
    {
        if (!canMove) return; // Death system check

        // Capture horizontal input for movement (AZERTY keyboard: Q and D)
        // On AZERTY: Q is where A is on QWERTY, D is in the same position
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

        // Update prone state based on S key
        if (pronePressed && !isProne)
        {
            // Start crouching
            isProne = true;
            UpdateColliderSize();
        }
        else if (!proneHeld && isProne)
        {
            // Stop crouching (S key released)
            isProne = false;
            isCrawling = false; // Also stop crawling
            UpdateColliderSize();
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

        float actualSpeed = baseWalkSpeed;

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
            PlayFootStepSound(); // Using same sound for now, but could be different
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

        float actualJumpHeight = baseJumpHeight;

        // Calculate jump velocity needed to reach desired height
        // Using: v = sqrt(2 * g * h) where g is gravity (positive value)
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
            // Shrink collider for prone/crawl position
            col.offset = new Vector2(0.03f, -0.22f);
            col.size = new Vector2(0.78f, 0.53f);
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
            else if (!isCrawling && PlayerAnimationManager.Instance.HasAnimation("prone"))
            {
                // Player is stationary while crouched - play prone animation
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

    // Called by Animation Manager when player completes an animation
    public void UnlockWalking(float qualityMultiplier)
    {
        canWalk = true;
        Debug.Log($"Walking unlocked! canWalk = {canWalk}");
    }

    public void UnlockJumping(float qualityMultiplier)
    {
        canJump = true;
        Debug.Log($"Jumping unlocked! canJump = {canJump}");
    }

    public void UnlockProning(float qualityMultiplier)
    {
        canProne = true;
        Debug.Log($"Prone unlocked! canProne = {canProne}");
    }

    public void UnlockCrawling(float qualityMultiplier)
    {
        canCrawl = true;
        Debug.Log($"Crawling unlocked! canCrawl = {canCrawl}");
    }

    // Called by Animation Manager when animations are removed
    public void LockWalking()
    {
        canWalk = false;
        Debug.Log($"Walking locked! canWalk = {canWalk}");

        // If player is currently moving, stop them
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f && !isProne)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    public void LockJumping()
    {
        canJump = false;
        Debug.Log($"Jumping locked! canJump = {canJump}");
    }

    public void LockProning()
    {
        canProne = false;
        Debug.Log($"Prone locked! canProne = {canProne}");

        // If player is currently prone, force them to stand up
        if (isProne)
        {
            isProne = false;
            isCrawling = false;
            UpdateColliderSize();
            HandleAnimations();
        }
    }

    public void LockCrawling()
    {
        canCrawl = false;
        Debug.Log($"Crawling locked! canCrawl = {canCrawl}");

        // If player is currently crawling, they can stay prone but can't move
        if (isCrawling)
        {
            isCrawling = false;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Stop movement
            HandleAnimations(); // Switch back to prone animation
        }
    }

    // Visualize ground check in scene view
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}