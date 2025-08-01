using System.Collections.Generic;
using UnityEngine;

public class StartingFramesManager : MonoBehaviour
{
    [Header("Starting Frame Prefabs")]
    public List<GameObject> startingFramePrefabs;

    void Start()
    {
        // Wait a frame to ensure PlayerAnimationManager is initialized
        StartCoroutine(AddStartingFramesDelayed());
    }

    System.Collections.IEnumerator AddStartingFramesDelayed()
    {
        yield return null; // Wait one frame

        // Make sure PlayerAnimationManager exists
        if (PlayerAnimationManager.Instance != null)
        {
            AddStartingFrames();
        }
        else
        {
            Debug.LogError("PlayerAnimationManager.Instance is null! Make sure it's initialized first.");
        }
    }

    void AddStartingFrames()
    {
        foreach (GameObject framePrefab in startingFramePrefabs)
        {
            if (framePrefab != null)
            {
                // Get the AnimationFramePickup component from the prefab
                AnimationFramePickup framePickup = framePrefab.GetComponent<AnimationFramePickup>();

                if (framePickup != null)
                {
                    // Extract the data from the prefab's AnimationFramePickup component
                    PlayerAnimationManager.Instance.AddFrameToInventory(
                        framePickup.frameType,
                        framePickup.frameSprite,
                        framePickup.animationSprite
                    );

                    Debug.Log($"Added starting frame from prefab: {framePickup.frameType}");
                }
                else
                {
                    Debug.LogWarning($"Prefab {framePrefab.name} doesn't have an AnimationFramePickup component!");
                }
            }
        }
    }
}