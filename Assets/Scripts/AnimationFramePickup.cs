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
        // Set the visual sprite for the pickup
        if (spriteRenderer != null && frameSprite != null)
        {
            spriteRenderer.sprite = frameSprite;
        }

        // Get the player's audio source
        audioSource = GameObject.FindWithTag("Player").GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Add to player's frame collection
            PlayerAnimationManager.Instance.CollectFrame(this);

            // Play pickup sounds
            audioSource.PlayOneShot(pickupSound);

            // Destroy the frame pickup
            Destroy(gameObject);
        }
    }
}
