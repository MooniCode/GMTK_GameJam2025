using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TimelineSlot : MonoBehaviour, IDropHandler
{
    public int slotIndex;
    public AnimationInterface animationInterface;
    public Image slotImage;

    [Header("Slot States")]
    public Sprite emptySlotSprite;
    public Color emptySlotColor = Color.gray;

    private FrameData currentFrameData;
    private GameObject currentDraggableFrame;

    void Start()
    {
        SetEmptySlot();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableFrame draggedFrame = eventData.pointerDrag.GetComponent<DraggableFrame>();
            if (draggedFrame != null && draggedFrame.frameData != null)
            {
                PlaceFrameInSlot(draggedFrame);
            }
        }
    }

    void PlaceFrameInSlot(DraggableFrame incomingFrame)
    {
        // Check if this slot already has a frame
        if (currentFrameData != null)
        {
            // Check if incoming frame is from timeline or inventory
            TimelineSlotFrame incomingTimelineFrame = incomingFrame.GetComponent<TimelineSlotFrame>();

            if (incomingTimelineFrame != null)
            {
                // Both frames are on timeline - swap them
                SwapFrames(incomingFrame);
            }
            else
            {
                // Incoming frame is from inventory - replace existing frame
                ReplaceWithInventoryFrame(incomingFrame);
            }
        }
        else
        {
            // Empty slot - just place the frame
            PlaceFrameInEmptySlot(incomingFrame);
        }
    }

    void SwapFrames(DraggableFrame incomingFrame)
    {
        TimelineSlotFrame incomingTimelineFrame = incomingFrame.GetComponent<TimelineSlotFrame>();

        // Store current frame data
        FrameData tempFrameData = currentFrameData;

        // Set incoming frame to this slot
        SetFrameFromData(incomingFrame.frameData);
        animationInterface.AddFrameToTimelineFromData(incomingFrame.frameData, slotIndex);

        // Update the original slot of the incoming frame with our old frame
        incomingTimelineFrame.originalSlot.SetFrameFromData(tempFrameData);
        incomingTimelineFrame.originalSlot.animationInterface.AddFrameToTimelineFromData(tempFrameData, incomingTimelineFrame.originalSlot.slotIndex);

        Debug.Log($"Swapped {incomingFrame.frameData.frameType} with {tempFrameData.frameType} between timeline slots");

        // Notify successful drop
        incomingFrame.OnSuccessfulDrop();
    }

    void ReplaceWithInventoryFrame(DraggableFrame incomingFrame)
    {
        // Return the current frame to inventory
        PlayerAnimationManager.Instance.collectedFrames.Add(currentFrameData);
        Debug.Log($"Returned displaced frame {currentFrameData.frameType} to inventory");

        // Set the new frame from inventory in this slot
        SetFrameFromData(incomingFrame.frameData);
        animationInterface.AddFrameToTimelineFromData(incomingFrame.frameData, slotIndex);

        // Remove the frame from inventory since it's now used
        PlayerAnimationManager.Instance.UseFrame(incomingFrame.frameData);

        // Refresh inventory display to show the returned frame
        animationInterface.RefreshInventoryDisplay();

        // Notify successful drop
        incomingFrame.OnSuccessfulDrop();
    }

    void PlaceFrameInEmptySlot(DraggableFrame incomingFrame)
    {
        // Set frame in this slot
        SetFrameFromData(incomingFrame.frameData);
        animationInterface.AddFrameToTimelineFromData(incomingFrame.frameData, slotIndex);

        // Check source of dragged frame
        TimelineSlotFrame timelineFrame = incomingFrame.GetComponent<TimelineSlotFrame>();
        if (timelineFrame != null)
        {
            // This came from another timeline slot - clear the original slot
            timelineFrame.originalSlot.ClearSlotWithoutReturningToInventory();
        }
        else
        {
            // This came from inventory - remove from collected frames
            PlayerAnimationManager.Instance.UseFrame(incomingFrame.frameData);
        }

        // Notify successful drop
        incomingFrame.OnSuccessfulDrop();
    }

    public void SetFrameFromData(FrameData frameData)
    {
        // Clean up existing draggable frame
        if (currentDraggableFrame != null)
        {
            Destroy(currentDraggableFrame);
        }

        currentFrameData = frameData;

        if (frameData != null)
        {
            slotImage.sprite = frameData.GetUISprite();
            slotImage.color = Color.white;

            // Create draggable frame for this slot using the same prefab as inventory
            CreateDraggableFrame(frameData);
        }
        else
        {
            SetEmptySlot();
        }

    }

    void CreateDraggableFrame(FrameData frameData)
    {
        // Use the same prefab as inventory
        GameObject frameDisplayPrefab = animationInterface.frameDisplayPrefab;

        if (frameDisplayPrefab != null)
        {
            currentDraggableFrame = Instantiate(frameDisplayPrefab, transform);

            // Set up the draggable frame using UI sprite (same as inventory)
            Image frameImage = currentDraggableFrame.GetComponent<Image>();
            if (frameImage != null)
            {
                frameImage.sprite = frameData.GetUISprite(); // Use UI sprite (same as inventory)
                frameImage.raycastTarget = true;
            }
            else
            {
                Debug.LogError($"No Image component found on instantiated frame prefab!");
            }

            // Ensure it has DraggableFrame component
            DraggableFrame draggable = currentDraggableFrame.GetComponent<DraggableFrame>();
            if (draggable == null)
            {
                draggable = currentDraggableFrame.AddComponent<DraggableFrame>();
            }
            draggable.frameData = frameData;

            // Add marker component to identify timeline frames
            TimelineSlotFrame timelineMarker = currentDraggableFrame.GetComponent<TimelineSlotFrame>();
            if (timelineMarker == null)
            {
                timelineMarker = currentDraggableFrame.AddComponent<TimelineSlotFrame>();
            }
            timelineMarker.originalSlot = this;

            // Remove TimelineSlotDraggable if it exists (we only want DraggableFrame)
            TimelineSlot oldDraggable = currentDraggableFrame.GetComponent<TimelineSlot>();
            if (oldDraggable != null)
            {
                Destroy(oldDraggable);
                Debug.Log("Removed old TimelineSlot component");
            }

            // Make it fill the slot
            RectTransform rectTransform = currentDraggableFrame.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }
            else
            {
                Debug.LogError("No RectTransform found on instantiated frame!");
            }
        }
        else
        {
            Debug.LogError("frameDisplayPrefab is not assigned in AnimationInterface!");
        }
    }

    void SetEmptySlot()
    {
        currentFrameData = null;
        if (currentDraggableFrame != null)
        {
            Destroy(currentDraggableFrame);
            currentDraggableFrame = null;
        }
        slotImage.sprite = emptySlotSprite;
        slotImage.color = emptySlotColor;
    }

    public void ClearSlot()
    {
        // Return frame to inventory if cleared
        if (currentFrameData != null)
        {
            PlayerAnimationManager.Instance.collectedFrames.Add(currentFrameData);
        }

        SetEmptySlot();
        animationInterface.RemoveFrameFromTimeline(slotIndex);
    }

    // New method: Clear slot without returning to inventory (used for swaps/moves)
    public void ClearSlotWithoutReturningToInventory()
    {
        SetEmptySlot();
        animationInterface.RemoveFrameFromTimeline(slotIndex);
    }
}