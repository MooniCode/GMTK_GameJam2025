using UnityEditor.Rendering;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseWalkSpeed = 5f;
    public float baseJumpHeight = 3f;
    public float gravity = -20f;

    [Header("Unlocked Abilities")]
    public bool canWalk = false;
    public bool canJump = false;

    // Components
    private CharacterController controller;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Movement variables
    private Vector3 velocity;
    private bool isGrounded;
    private bool wasMoving = false;
    private bool wasGrounded = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        CheckGrounded();
        HandleInput();
        ApplyGravity();
        HandleAnimations();

        // Move the character
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleInput()
    {
        float horizontal = Input.GetAxis("Horizontal");

        // Walking (only if unlocked)
        if (canWalk && Mathf.Abs(horizontal) > 0.1f)
        {
            Walk(horizontal);
        }
        else
        {
            // Stop horizontal movement
            velocity.x = 0;
        }

        // Jumping (only if unlocked and grounded)
        if (canJump && Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    void Walk(float direction)
    {
        float actualSpeed = baseWalkSpeed;
        velocity.x = direction * actualSpeed;

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
        velocity.y = Mathf.Sqrt(actualJumpHeight * -2f * gravity);

        // Trigger jump animation if available
        if (PlayerAnimationManager.Instance != null &&
            PlayerAnimationManager.Instance.HasAnimation("jump"))
        {
            PlayerAnimationManager.Instance.TriggerJumpAnimation();
        }
    }

    void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep grounded
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    void CheckGrounded()
    {
        wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;

        // If we just landed, stop jump animation and return to appropriate animation
        if (!wasGrounded && isGrounded)
        {
            HandleAnimations();
        }
    }

    void HandleAnimations()
    {
        if (PlayerAnimationManager.Instance == null) return;

        bool isMoving = Mathf.Abs(velocity.x) > 0.1f;

        // Don't interrupt jump animation while in air
        if (!isGrounded && PlayerAnimationManager.Instance.HasAnimation("Jump"))
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
            else if (!isMoving && PlayerAnimationManager.Instance.HasAnimation("Idle"))
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
        Debug.Log("Walking ability unlocked!");
    }

    public void UnlockJumping(float qualityMultiplier)
    {
        canJump = true;
        Debug.Log("Jumping ability unlocked!");
    }
}