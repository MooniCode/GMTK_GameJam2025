using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyProjectile : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetProjectileDirection();
    }

    void SetProjectileDirection()
    {
        if (spriteRenderer == null) return;

        // Get the projectile's velocity to determine direction
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Flip sprite to face the direction of movement
            // Assuming default sprite faces right - adjust this logic if your sprite faces left by default
            if (rb.linearVelocity.x < 0)
            {
                // Moving left, keep original orientation
                spriteRenderer.flipX = false;
            }
            else
            {
                // Moving right, so flip the sprite
                spriteRenderer.flipX = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Destroy the projectile
            Destroy(gameObject);
            // For now just reload the scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (other.CompareTag("Ground"))
        {
            // Destroy the projectile
            Destroy(gameObject);
        }
    }
}