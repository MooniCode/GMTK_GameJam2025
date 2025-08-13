using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Player

    [Header("Follow Settings")]
    public float followSpeed = 2f;
    public Vector3 offset = new Vector3(0, 2f, -10f);

    [Header("Dynamic Y Offset")]
    public float lowYThreshold = -2f;
    public float lowYOffset = -1f;

    // Private variables
    private Vector3 targetPosition;
    private Vector3 originalOffset;

    private void Start()
    {
        // Store the original offset
        originalOffset = offset;

        // Set initial position
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Adjust Y offset based on player position
        Vector3 currentOffset = originalOffset;
        if (target.position.y < lowYThreshold)
        {
            currentOffset.y = lowYOffset;
        }

        // Calculate base target position with dynamic offset
        targetPosition = target.position + currentOffset;

        // Move camera to target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }
}