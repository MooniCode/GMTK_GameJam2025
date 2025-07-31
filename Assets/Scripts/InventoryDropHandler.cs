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
                    // Return frame to inventory
                    PlayerAnimationManager.Instance.collectedFrames.Add(draggableFrame.frameData);

                    // Clear the original slot
                    timelineFrame.originalSlot.ClearSlotWithoutReturningToInventory();

                    // Refresh inventory display
                    animationInterface.RefreshInventoryDisplay();

                    // Destroy the dragged item
                    Destroy(eventData.pointerDrag);
                }
                else
                {
                    // This frame came from inventory originally, just return it
                    PlayerAnimationManager.Instance.collectedFrames.Add(draggableFrame.frameData);
                    animationInterface.RefreshInventoryDisplay();
                    Destroy(eventData.pointerDrag);
                }
            }
        }
    }
}