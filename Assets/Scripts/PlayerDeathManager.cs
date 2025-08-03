using System.Collections;
using UnityEngine;

[System.Serializable]
public class DeathAnimationData
{
    [Header("Death Animation Frames")]
    [Tooltip("Drag the death animation sprites in order here")]
    public Sprite[] deathFrames;

    [Header("Animation Settings")]
    public float frameRate = 0.1f;
    public bool shouldLoop = false;
}

public class PlayerDeathManager : MonoBehaviour
{
    [Header("Death Animation")]
    public DeathAnimationData deathAnimation;

    [Header("Death Settings")]
    public float deathDuration = 2f; // Total time before respawn
    public bool freezePlayerDuringDeath = true;

    [Header("Death Sound")]
    public AudioSource audioSource;
    public AudioClip deathSound;

    // References
    private SpriteRenderer playerSpriteRenderer;
    private PlayerController playerController;
    private Rigidbody2D playerRigidbody;
    private PlayerAnimationManager animationManager;

    // Death state
    private bool isDying = false;
    private bool isDead = false;
    private int currentDeathFrame = 0;
    private float deathAnimationTimer = 0f;
    private Vector3 respawnPosition;

    // Store original state to restore after death
    private Sprite originalSprite;
    private CustomAnimation originalAnimation;
    private bool wasAnimating;

    public static PlayerDeathManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Get references
        playerController = FindAnyObjectByType<PlayerController>();
        animationManager = PlayerAnimationManager.Instance;

        if (playerController != null)
        {
            playerSpriteRenderer = playerController.GetComponent<SpriteRenderer>();
            playerRigidbody = playerController.GetComponent<Rigidbody2D>();
        }

        // Set initial respawn position to current position
        respawnPosition = transform.position;

        // Validate death animation
        if (deathAnimation.deathFrames == null || deathAnimation.deathFrames.Length == 0)
        {
            Debug.LogWarning("No death animation frames assigned!");
        }
    }

    void Update()
    {
        // Handle death animation
        if (isDying && !isDead)
        {
            UpdateDeathAnimation();
        }
    }

    public void TriggerDeath()
    {
        if (isDying || isDead) return; // Prevent multiple death triggers

        Debug.Log("Player death triggered!");

        audioSource.PlayOneShot(deathSound);

        isDying = true;
        isDead = false;
        currentDeathFrame = 0;
        deathAnimationTimer = 0f;

        // Store current state
        StoreCurrentState();

        // Stop current animations and disable player controls
        if (animationManager != null)
        {
            animationManager.StopAnimation();
        }

        if (playerController != null && freezePlayerDuringDeath)
        {
            playerController.SetCanMove(false); // You'll need to add this method to PlayerController
        }

        // Stop player movement
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        // Start death animation
        if (deathAnimation.deathFrames != null && deathAnimation.deathFrames.Length > 0)
        {
            playerSpriteRenderer.sprite = deathAnimation.deathFrames[0];
        }

        // Start respawn coroutine
        StartCoroutine(HandleRespawn());
    }

    void UpdateDeathAnimation()
    {
        if (deathAnimation.deathFrames == null || deathAnimation.deathFrames.Length == 0) return;

        deathAnimationTimer += Time.deltaTime;

        if (deathAnimationTimer >= deathAnimation.frameRate)
        {
            deathAnimationTimer = 0f;
            currentDeathFrame++;

            // Handle animation completion
            if (currentDeathFrame >= deathAnimation.deathFrames.Length)
            {
                if (deathAnimation.shouldLoop)
                {
                    currentDeathFrame = 0;
                }
                else
                {
                    currentDeathFrame = deathAnimation.deathFrames.Length - 1; // Stay on last frame
                }
            }

            // Update sprite
            if (playerSpriteRenderer != null)
            {
                playerSpriteRenderer.sprite = deathAnimation.deathFrames[currentDeathFrame];
            }
        }
    }

    void StoreCurrentState()
    {
        if (playerSpriteRenderer != null)
        {
            originalSprite = playerSpriteRenderer.sprite;
        }

        if (animationManager != null)
        {
            originalAnimation = animationManager.currentAnimation;
            wasAnimating = animationManager.isAnimating;
        }
    }

    void RestoreOriginalState()
    {
        if (playerSpriteRenderer != null && originalSprite != null)
        {
            playerSpriteRenderer.sprite = originalSprite;
        }

        if (animationManager != null && originalAnimation != null && wasAnimating)
        {
            // Resume the animation that was playing before death
            animationManager.currentAnimation = originalAnimation;
            animationManager.isAnimating = wasAnimating;
        }
    }

    IEnumerator HandleRespawn()
    {
        // Wait for death duration
        yield return new WaitForSeconds(deathDuration);

        // Respawn player
        RespawnPlayer();
    }

    void RespawnPlayer()
    {
        Debug.Log("Respawning player...");

        // Move player to respawn position
        transform.position = respawnPosition;

        // Reset death state
        isDying = false;
        isDead = false;

        // Restore original sprite/animation state
        RestoreOriginalState();

        // Re-enable player controls
        if (playerController != null)
        {
            playerController.SetCanMove(true); // You'll need to add this method to PlayerController
        }

        // Reset velocity
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        // Resume normal animations
        if (animationManager != null)
        {
            // Let the animation manager handle which animation to play based on current state
            playerController.HandleAnimations(); // Assuming this method exists
        }
    }

    public void SetRespawnPoint(Vector3 newRespawnPosition)
    {
        respawnPosition = newRespawnPosition;
        Debug.Log($"Respawn point set to: {respawnPosition}");
    }

    public bool IsPlayerDying()
    {
        return isDying;
    }

    public bool IsPlayerDead()
    {
        return isDead;
    }
}