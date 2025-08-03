using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [Header("Trampoline Settings")]
    public float bounceForce = 15f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip bounceSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if it's the player
        if (other.GetComponent<PlayerController>() != null)
        {
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();

            // Bounce the player
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, bounceForce);

            // Play sound
            if (audioSource != null && bounceSound != null)
            {
                audioSource.PlayOneShot(bounceSound);
            }
        }
    }
}