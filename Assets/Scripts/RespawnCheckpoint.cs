// Checkpoint script to set respawn points
using UnityEngine;

public class RespawnCheckpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public bool isStartingCheckpoint = false;
    public Color gizmoColor = Color.green;

    void Start()
    {
        if (isStartingCheckpoint)
        {
            // Set this as the initial respawn point
            if (PlayerDeathManager.Instance != null)
            {
                PlayerDeathManager.Instance.SetRespawnPoint(transform.position);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (PlayerDeathManager.Instance != null)
            {
                PlayerDeathManager.Instance.SetRespawnPoint(transform.position);
                Debug.Log($"Checkpoint activated at: {transform.position}");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}