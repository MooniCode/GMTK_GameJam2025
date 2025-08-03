using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDropHandler : MonoBehaviour, IDropHandler
{
    public AnimationInterface animationInterface;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableFrame draggableFrame = eventData.pointerDrag.GetComponent<DraggableFrame>();
            if (draggableFrame != null)
            {
                // Check if this frame came from a timeline slot
                TimelineSlotFrame timelineFrame = draggableFrame.GetComponent<TimelineSlotFrame>();
                if (timelineFrame != null)
                {
                    // ONLY timeline frames should be returned to inventory
                    PlayerAnimationManager.Instance.collectedFrames.Add(draggableFrame.frameData);
                    // Clear the original slot
                    timelineFrame.originalSlot.ClearSlotWithoutReturningToInventory();
                    // Refresh inventory display
                    animationInterface.RefreshInventoryDisplay();
                }
                else
                {
                    // This frame came from inventory originally - DON'T add it back!
                    // It's already in the collectedFrames list
                    // Just refresh the display to clean up any visual issues
                    animationInterface.RefreshInventoryDisplay();
                }

                // Mark as successfully dropped and destroy the dragged object
                draggableFrame.OnSuccessfulDrop();
            }
        }
    }
}