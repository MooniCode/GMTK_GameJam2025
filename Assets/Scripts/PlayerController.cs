using UnityEngine;

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

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // Movement variables
    private bool isGrounded;
    private bool wasMoving = false;
    private bool wasGrounded = false;

    // Input storage for FixedUpdate
    private float horizontalInput;
    private bool jumpInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Set up rigidbody constraints
        rb.freezeRotation = true;

        // Create ground check point if it doesn't exist
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -0.18f, 0);
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
        if (Input.GetKeyDown(KeyCode.Space) && canJump && isGrounded)
        {
            jumpInput = true;
        }
    }

    void HandleMovement()
    {
        // Walking (only if unlocked)
        if (canWalk && Mathf.Abs(horizontalInput) > 0.1f)
        {
            Walk(horizontalInput);
        }
        else
        {
            // Stop horizontal movement while preserving vertical velocity
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // Handle jumping
        if (jumpInput)
        {
            Jump();
            jumpInput = false; // Reset jump input
        }
    }

    void Walk(float direction)
    {
        float actualSpeed = baseWalkSpeed;

        // Set horizontal velocity while preserving vertical velocity
        rb.linearVelocity = new Vector2(direction * actualSpeed, rb.linearVelocity.y);

        // Flip sprite based on direction
        if (direction > 0)
            spriteRenderer.flipX = false;
        else if (direction < 0)
            spriteRenderer.flipX = true;
    }

    void Jump()
    {
        float actualJumpHeight = baseJumpHeight;

        // Calculate jump velocity needed to reach desired height
        // Using: v = sqrt(2 * g * h) where g is gravity (positive value)
        float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y * rb.gravityScale) * actualJumpHeight);

        // Set vertical velocity
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);

        // Trigger jump animation if available
        if (PlayerAnimationManager.Instance != null &&
            PlayerAnimationManager.Instance.HasAnimation("jump"))
        {
            PlayerAnimationManager.Instance.TriggerJumpAnimation();
        }
    }

    void CheckGrounded()
    {
        wasGrounded = isGrounded;

        // Check if player is touching ground using overlap circle
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);

        // If we just landed, handle landing
        if (!wasGrounded && isGrounded)
        {
            HandleAnimations();
        }
    }

    void HandleAnimations()
    {
        if (PlayerAnimationManager.Instance == null) return;

        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;

        // Don't interrupt jump animation while in air
        if (!isGrounded && PlayerAnimationManager.Instance.HasAnimation("jump"))
        {
            // Jump animation is already handled in Jump() method
            return;
        }

        // Handle ground animations
        if (isGrounded)
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
    }

    public void UnlockJumping(float qualityMultiplier)
    {
        canJump = true;
    }

    // Called by Animation Manager when animations are removed
    public void LockWalking()
    {
        canWalk = false;

        // If player is currently moving, stop them
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    public void LockJumping()
    {
        canJump = false;
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