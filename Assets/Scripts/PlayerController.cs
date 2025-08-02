using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseWalkSpeed = 5f;
    public float baseJumpHeight = 3f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayerMask = 1;

    [Header("Unlocked Abilities")]
    public bool canWalk = false;
    public bool canJump = false;
    public bool canProne = false;

    [Header("Audio")]
    public AudioSource playerAudioSource;
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.3f;

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
    private bool isProne = false;

    // Input storage for FixedUpdate
    private float horizontalInput;
    private bool jumpInput;
    private bool proneInput;

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
        CheckGrounded();
        HandleInput();
        HandleAnimations();
    }

    void FixedUpdate()
    {
        // Handle physics-based movement in FixedUpdate
        HandleMovement();
    }

    void HandleInput()
    {
        // Capture input for use in FixedUpdate
        horizontalInput = Input.GetAxis("Horizontal");

        // Jumping input (check for key press)
        if (Input.GetKeyDown(KeyCode.Space) && canJump && isGrounded && !isProne)
        {
            jumpInput = true;
        }

        // Check if S key is being held down (and player can prone and is grounded)
        bool shouldBeProne = Input.GetKey(KeyCode.S) && canProne && isGrounded;

        // If prone state should change, trigger the transition
        if (shouldBeProne != isProne)
        {
            proneInput = true;
        }
    }

    void HandleMovement()
    {
        // Walking (only if unlocked AND not prone AND not playing reverse transition)
        bool canCurrentlyWalk = canWalk && !isProne &&
                               (PlayerAnimationManager.Instance == null ||
                                !PlayerAnimationManager.Instance.IsPlayingReverseTransition);

        if (canCurrentlyWalk && Mathf.Abs(horizontalInput) > 0.1f)
        {
            Walk(horizontalInput);
        }
        else
        {
            // Stop horizontal movement while preserving vertical velocity
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            // Debug feedback for why movement is blocked
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                if (!canWalk)
                    Debug.Log("Movement blocked: Walking not unlocked");
                else if (isProne)
                    Debug.Log("Movement blocked: Player is prone");
                else if (PlayerAnimationManager.Instance != null && PlayerAnimationManager.Instance.IsPlayingReverseTransition)
                    Debug.Log("Movement blocked: Reverse transition playing");
            }
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

        // Handle prone toggle
        if (proneInput)
        {
            ToggleProne();
            proneInput = false; // Reset prone input
        }
    }

    void Walk(float direction)
    {
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

    void ToggleProne()
    {
        bool wasAlreadyProne = isProne;
        isProne = !isProne;

        // Update collider size based on prone state
        UpdateColliderSize();

        // Trigger appropriate animation
        if (isProne)
        {
            // Going into prone position - play normal animation
            if (PlayerAnimationManager.Instance != null &&
                PlayerAnimationManager.Instance.HasAnimation("prone"))
            {
                PlayerAnimationManager.Instance.PlayAnimation("prone", false, false); // forward animation
            }
        }
        else
        {
            // Getting up from prone - play reverse animation
            if (PlayerAnimationManager.Instance != null &&
                PlayerAnimationManager.Instance.HasAnimation("prone"))
            {
                PlayerAnimationManager.Instance.PlayAnimation("prone", false, true); // reverse animation
            }
            else
            {
                // Fallback: Handle other standing animations if no prone animation
                HandleAnimations();
            }
        }
    }

    private void UpdateColliderSize()
    {
        if (col == null) return;

        if (isProne)
        {
            // Shrink collider for prone position
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

        // NEW: Don't interrupt reverse transition animations
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

        // Don't interrupt prone animation while prone
        if (isProne)
        {
            // Make sure prone animation is playing
            if (PlayerAnimationManager.Instance.HasAnimation("prone") &&
                PlayerAnimationManager.Instance.currentAnimation?.animationType != "prone")
            {
                PlayerAnimationManager.Instance.PlayAnimation("prone");
            }
            return;
        }

        // Handle ground animations only when actually grounded, not jumping, and not prone
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

    // Called by Animation Manager when animations are removed
    public void LockWalking()
    {
        canWalk = false;
        Debug.Log($"Walking locked! canWalk = {canWalk}");

        // If player is currently moving, stop them
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
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
            HandleAnimations();
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