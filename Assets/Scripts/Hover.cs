using UnityEngine;

public class SimpleHover : MonoBehaviour
{
    [Header("Hover Settings")]
    [Tooltip("How fast the object moves up and down (cycles per second)")]
    public float frequency = 1f;

    [Tooltip("How far the object moves up and down from center")]
    public float amplitude = 1f;

    [Header("Optional Settings")]
    [Tooltip("Starting phase offset (0-1, where 0.5 starts at the top)")]
    public float phaseOffset = 0f;

    private Vector3 startPosition;
    private float timeOffset;

    void Start()
    {
        // Store the starting position
        startPosition = transform.position;

        // Add random time offset so multiple objects don't sync
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    void Update()
    {
        // Calculate the sine wave
        float sineWave = Mathf.Sin((Time.time * frequency * 2f * Mathf.PI) + timeOffset + (phaseOffset * 2f * Mathf.PI));

        // Apply the hover movement
        transform.position = startPosition + Vector3.up * (sineWave * amplitude);
    }
}