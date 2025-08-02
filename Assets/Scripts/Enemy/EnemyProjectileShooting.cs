using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 1f;
    public GameObject projectileShootPoint;

    public void ShootProjectile()
    {
        // Check if projectile prefab is assigned
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab is not assigned!");
            return;
        }

        // Instantiate the projectile at this object's position and rotation
        GameObject projectile = Instantiate(projectilePrefab, projectileShootPoint.transform.position, transform.rotation);

        // Get the Rigidbody2D component and set velocity
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Shoot to the left in 2D space
            rb.linearVelocity = Vector2.left * projectileSpeed;
        }
        else
        {
            Debug.LogWarning("Projectile prefab doesn't have a Rigidbody2D component!");
        }
    }
}