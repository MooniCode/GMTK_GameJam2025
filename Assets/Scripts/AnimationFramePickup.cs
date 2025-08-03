using UnityEngine;

public class AnimationFramePickup : MonoBehaviour
{
    [Header("Frame Data")]
    public string frameType;

    [Header("Sprites")]
    public Sprite frameSprite;
    public Sprite animationSprite;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupSound;

    private void Start()
    {
        Debug.Log($"AnimationFramePickup Start() - frameType: {frameType}");

        // Set the visual sprite for the pickup
        if (spriteRenderer != null && frameSprite != null)
        {
            spriteRenderer.sprite = frameSprite;
            Debug.Log($"Set pickup sprite: {frameSprite.name}");
        }

        // Get the player's audio source
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            audioSource = player.GetComponent<AudioSource>();
            Debug.Log("Found player audio source: " + (audioSource != null));
        }
        else
        {
            Debug.LogWarning("Could not find player with 'Player' tag!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"OnTriggerEnter2D called with: {other.name}, tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player picked up frame: {frameType}");

            // Check if PlayerAnimationManager exists
            if (PlayerAnimationManager.Instance == null)
            {
                Debug.LogError("PlayerAnimationManager.Instance is null!");
                return;
            }

            // Add to player's frame collection
            PlayerAnimationManager.Instance.CollectFrame(this);
            Debug.Log($"Frame {frameType} added to collection");

            // Play pickup sound
            if (audioSource != null && pickupSound != null)
            {
                audioSource.PlayOneShot(pickupSound);
                Debug.Log("Played pickup sound");
            }

            // Check if this completes an animation type
            if (FrameCollectionChecker.Instance != null)
            {
                Debug.Log($"Calling CheckAnimationCompletion for {frameType}");
                FrameCollectionChecker.Instance.CheckAnimationCompletion(frameType);
            }
            else
            {
                Debug.LogError("FrameCollectionChecker.Instance is null!");
            }

            // Destroy the frame pickup
            Debug.Log($"Destroying frame pickup: {frameType}");
            Destroy(gameObject);
        }
    }
}