using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageFader : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeDuration = 2f;
    public bool fadeOnStart = true;

    private Image image;

    void Start()
    {
        // Get the Image component
        image = GetComponent<Image>();

        if (image == null)
        {
            Debug.LogError("No Image component found on " + gameObject.name);
            return;
        }

        // Start with the image completely transparent
        Color startColor = image.color;
        startColor.a = 0f;
        image.color = startColor;

        // Begin fade in if enabled
        if (fadeOnStart)
        {
            StartFadeIn();
        }
    }

    public void StartFadeIn()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        Color imageColor = image.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);

            imageColor.a = alpha;
            image.color = imageColor;

            yield return null;
        }

        // Ensure the image is fully opaque at the end
        imageColor.a = 1f;
        image.color = imageColor;
    }
}