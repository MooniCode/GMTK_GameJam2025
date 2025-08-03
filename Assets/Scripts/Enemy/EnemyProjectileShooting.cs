using UnityEngine;

public class EnemyProjectileShooter : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 1f;
    public GameObject projectileShootPoint;

    [Header("Player Detection")]
    public bool flipSpriteToFacePlayer = true;

    private GameObject player;
    private bool facingRight = true; // Track which direction we're facing

    void Start()
    {
        // Find the player by tag
        player = GameObject.FindWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("No GameObject with 'Player' tag found!");
        }
    }

    public void ShootProjectile()
    {
        // Check if projectile prefab is assigned
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab is not assigned!");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("Player not found!");
            return;
        }

        // Determine which side the player is on (left or right)
        bool playerIsOnRight = IsPlayerOnRightSide();

        // Flip enemy to face player (flips whole GameObject including children)
        if (flipSpriteToFacePlayer)
        {
            FlipEnemyToFacePlayer(playerIsOnRight);
        }

        // Instantiate the projectile at the shoot point
        GameObject projectile = Instantiate(projectilePrefab, projectileShootPoint.transform.position, transform.rotation);

        // Get the Rigidbody2D component and set horizontal velocity toward player's side
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Shoot horizontally left or right based on player position
            Vector2 shootDirection = playerIsOnRight ? Vector2.right : Vector2.left;
            rb.linearVelocity = shootDirection * projectileSpeed;
        }
        else
        {
            Debug.LogWarning("Projectile prefab doesn't have a Rigidbody2D component!");
        }
    }

    private bool IsPlayerOnRightSide()
    {
        // Simple check: is player's X position greater than enemy's X position?
        return player.transform.position.x > transform.position.x;
    }

    private void FlipEnemyToFacePlayer(bool playerIsOnRight)
    {
        // Only flip if we need to change direction
        if (playerIsOnRight != facingRight)
        {
            facingRight = playerIsOnRight;

            // Flip the entire GameObject (this will flip all children too)
            // Since sprites face left by default, we flip when player is on the right
            Vector3 scale = transform.localScale;
            if (playerIsOnRight)
            {
                // Player on right, flip to face right
                scale.x = Mathf.Abs(scale.x);
            }
            else
            {
                // Player on left, keep default (facing left)
                scale.x = -Mathf.Abs(scale.x);
            }
            transform.localScale = scale;
        }
    }

    // Optional: Call this in Update if you want the enemy to always face the player
    void Update()
    {
        if (player != null && flipSpriteToFacePlayer)
        {
            bool playerIsOnRight = IsPlayerOnRightSide();
            FlipEnemyToFacePlayer(playerIsOnRight);
        }
    }
}