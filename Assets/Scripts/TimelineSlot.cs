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

    private AnimationFramePickup currentFrame;

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
                SetFrameFromData(draggedFrame.frameData);
                animationInterface.AddFrameToTimelineFromData(draggedFrame.frameData, slotIndex);
            }
        }
    }

    public void SetFrameFromData(FrameData frameData)
    {
        if (frameData != null)
        {
            slotImage.sprite = frameData.frameSprite;
            slotImage.color = Color.white;
        }
        else
        {
            SetEmptySlot();
        }
    }

    void SetEmptySlot()
    {
        currentFrame = null;
        slotImage.sprite = emptySlotSprite;
        slotImage.color = emptySlotColor;
    }

    public void ClearSlot()
    {
        SetEmptySlot();
        animationInterface.RemoveFrameFromTimeline(slotIndex);
    }
}