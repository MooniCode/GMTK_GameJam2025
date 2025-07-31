using UnityEngine;

public class AnimationFramePickup : MonoBehaviour
{
    [Header("Frame Data")]
    public string frameType;
    public Sprite frameSprite;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    private void Start()
    {
        // Set the visual sprite for the pickup
        if (spriteRenderer != null && frameSprite != null)
        {
            spriteRenderer.sprite = frameSprite;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Add to player's frame collection
            PlayerAnimationManager.Instance.CollectFrame(this);
            Destroy(gameObject);
        }
    }
}
