using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableFrame : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public FrameData frameData;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
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
        // Store original parent and move to canvas root
        originalParent = transform.parent;
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
                wasDroppedOnValidTarget = true;
            }
        }

        if (wasDroppedOnValidTarget)
        {
            // Use the frame so that it gets removed from the inventory
            PlayerAnimationManager.Instance.UseFrame(frameData);

            // Frame was successfully placed in timeline - destroy this inventory item
            Destroy(gameObject);
        }
        else
        {
            // Frame was not dropped on timeline - move back to original parent
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}