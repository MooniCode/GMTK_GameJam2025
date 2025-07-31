using UnityEngine;
using UnityEngine.UI;

public class ScrollWheelWithElastic : MonoBehaviour
{
    private ScrollRect scrollRect;
    public float scrollSpeed = 0.1f;
    public float elasticForce = 0.1f;

    void Start()
    {
        scrollRect = GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = false;
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            scrollRect.verticalNormalizedPosition += scroll * scrollSpeed;
        }

        // Add elastic bounce back when out of bounds
        if (scrollRect.verticalNormalizedPosition > 1f)
        {
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(scrollRect.verticalNormalizedPosition, 1f, elasticForce);
        }
        else if (scrollRect.verticalNormalizedPosition < 0f)
        {
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(scrollRect.verticalNormalizedPosition, 0f, elasticForce);
        }
    }
}