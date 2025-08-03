using UnityEngine;

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

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (rb.linearVelocity.x < 0)
            {
                spriteRenderer.flipX = false;
            }
            else
            {
                spriteRenderer.flipX = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Trigger death instead of reloading scene
            if (PlayerDeathManager.Instance != null)
            {
                PlayerDeathManager.Instance.TriggerDeath();
            }

            // Destroy the projectile
            Destroy(gameObject);
        }

        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}