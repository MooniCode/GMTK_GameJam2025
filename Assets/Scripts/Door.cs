using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Sprites")]
    public Sprite closedDoorSprite;
    public Sprite openDoorSprite;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    private SpriteRenderer spriteRenderer;
    private Collider2D doorCollider;
    private bool isOpen = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        doorCollider = GetComponent<Collider2D>();

        // Start with door closed
        CloseDoor();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            OpenDoor();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        if (isOpen) return;

        isOpen = true;
        spriteRenderer.sprite = openDoorSprite;
        doorCollider.enabled = false;

        // Play open sound
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }
    }

    private void CloseDoor()
    {
        if (!isOpen) return;

        isOpen = false;
        spriteRenderer.sprite = closedDoorSprite;
        doorCollider.enabled = true;

        // Play close sound
        if (audioSource != null && closeSound != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
    }
}