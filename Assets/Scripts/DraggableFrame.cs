using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableFrame : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public FrameData frameData;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalPosition;
    private Canvas canvas;
    private bool wasDroppedOnValidTarget = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Store original parent and position
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        // Move to canvas root for proper rendering
        transform.SetParent(canvas.transform, true);

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        wasDroppedOnValidTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Check if we dropped on a valid timeline slot
        if (eventData.pointerEnter != null)
        {
            TimelineSlot timelineSlot = eventData.pointerEnter.GetComponent<TimelineSlot>();
            if (timelineSlot != null)
            {
                // The TimelineSlot will handle the placement logic
                return; // Exit early, let OnSuccessfulDrop handle cleanup
            }
        }

        // Check what type of frame this is
        TimelineSlotFrame timelineFrame = GetComponent<TimelineSlotFrame>();
        if (timelineFrame != null)
        {
            // This came from timeline - always return to inventory when not dropped on a timeline slot
            ReturnToInventory();
        }
        else
        {
            // This came from inventory - return to original position in inventory
            ReturnToOriginalPosition();
        }
    }

    void ReturnToInventory()
    {
        TimelineSlotFrame timelineFrame = GetComponent<TimelineSlotFrame>();
        if (timelineFrame != null)
        {
            // This came from timeline - return to inventory and clear original slot
            PlayerAnimationManager.Instance.collectedFrames.Add(frameData);
            timelineFrame.originalSlot.ClearSlotWithoutReturningToInventory();

            // Refresh inventory to show the returned frame
            FindAnyObjectByType<AnimationInterface>()?.RefreshInventoryDisplay();
        }
        else
        {
            // This came from inventory - just add it back
            PlayerAnimationManager.Instance.collectedFrames.Add(frameData);
            FindAnyObjectByType<AnimationInterface>()?.RefreshInventoryDisplay();
        }

        Destroy(gameObject);
    }

    void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent, false);
        rectTransform.anchoredPosition = originalPosition;
    }

    bool IsChildOf(Transform child, Transform parent)
    {
        Transform current = child;
        while (current != null)
        {
            if (current == parent)
                return true;
            current = current.parent;
        }
        return false;
    }

    // Called by TimelineSlot when frame is successfully dropped
    public void OnSuccessfulDrop()
    {
        wasDroppedOnValidTarget = true;
        Destroy(gameObject);
    }
}