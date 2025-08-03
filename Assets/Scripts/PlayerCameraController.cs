using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Player

    [Header("Follow Settings")]
    public float followSpeed = 2f;
    public Vector3 offset = new Vector3(0, 2f, -10f);

    [Header("Smoothing")]
    public bool useSmoothing = false;
    public float smoothTime = 0.3f;

    [Header("Deadzone")]
    public bool useDeadZone = false;
    public float deadZoneWidth = 2f;
    public float deadZoneHeight = 1f;

    [Header("Bounds")]
    public bool useBounds = false;
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -5f;
    public float maxY = 5f;

    [Header("Look Ahead")]
    public bool useLookAhead = false;
    public float lookAheadDistance = 3f;
    public float lookAheadSpeed = 2f;

    [Header("Dynamic Y Offset")]
    public float lowYThreshold = -2f;
    public float lowYOffset = -1f;

    // Private variables
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;
    private float currentLookAhead = 0f;
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

        // Apply look ahead
        if (useLookAhead)
        {
            HandleLookAhead();
        }

        // Apply deadzone logic
        if (useDeadZone)
        {
            HandleDeadZone();
        }

        // Apply Bounds
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        // Move camera to target position
        if (useSmoothing)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
    }

    private void HandleLookAhead()
    {
        // Get player's horizontal velocity to determine look ahead direction
        Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>();

        if (playerRb != null)
        {
            float horizontalVelocity = playerRb.linearVelocity.x;
            float targetLookAhead = 0f;

            if (Mathf.Abs(horizontalVelocity) > 0.1f)
            {
                targetLookAhead = Mathf.Sign(horizontalVelocity) * lookAheadDistance;
            }

            // Smoothly adjust look ahead
            currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, lookAheadSpeed * Time.deltaTime);
            targetPosition.x += currentLookAhead;
        }
    }

    private void HandleDeadZone()
    {
        Vector3 currentPos = transform.position;
        Vector3 playerPos = target.position;

        // Calculate deadzone bounds
        float leftBound = currentPos.x - deadZoneWidth * 0.5f;
        float rightBound = currentPos.x + deadZoneWidth * 0.5f;
        float bottomBound = currentPos.y - deadZoneHeight * 0.5f;
        float topBound = currentPos.y + deadZoneHeight * 0.5f;

        // Only move camera if player is outside deadzone
        if (playerPos.x < leftBound)
        {
            targetPosition.x = playerPos.x + deadZoneWidth * 0.5f + offset.x;
        }
        else if (playerPos.x > rightBound)
        {
            targetPosition.x = playerPos.x - deadZoneWidth * 0.5f + offset.x;
        }
        else
        {
            targetPosition.x = currentPos.x;
        }

        if (playerPos.y < bottomBound)
        {
            targetPosition.y = playerPos.y + deadZoneHeight * 0.5f + offset.y;
        }
        else if (playerPos.y > topBound)
        {
            targetPosition.y = playerPos.y - deadZoneHeight * 0.5f + offset.y;
        }
        else
        {
            targetPosition.y = currentPos.y;
        }
    }

    // Public methods for external control
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        originalOffset = newOffset;
    }

    public void ShakeCamera(float intensity, float duration)
    {
        StartCoroutine(CameraShake(intensity, duration));
    }

    private System.Collections.IEnumerator CameraShake(float intensity, float duration)
    {
        Vector3 originalPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1, 1f) * intensity;

            transform.position = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw deadzone
        if (useDeadZone)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(deadZoneWidth, deadZoneHeight, 0));
        }

        // Draw bounds
        if (useBounds)
        {
            Gizmos.color = Color.red;
            Vector3 boundsCenter = new Vector3((minX + maxX) + 0.5f, (minY + maxY) * 0.5f, transform.position.z);
            Vector3 boundsSize = new Vector3(maxX - minX, maxY - minY, 0);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }

        // Draw look ahead
        if (useLookAhead)
        {
            Gizmos.color = Color.blue;
            Vector3 lookAheadPos = target.position + Vector3.right * currentLookAhead;
            Gizmos.DrawWireSphere(lookAheadPos, 0.5f);
            Gizmos.DrawLine(targetPosition, lookAheadPos);
        }
    }
}