using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TypewriterText : MonoBehaviour
{
    [Header("Text Settings")]
    [TextArea(3, 10)]
    public List<string> textLines = new List<string>();

    [Header("Typing Settings")]
    public float typingSpeed = 0.05f;
    public float delayBetweenLines = 1f;
    public bool startOnAwake = true;
    public bool loopText = false;
    [Range(0f, 10f)]
    public float initialDelay = 2f; // New delay setting

    [Header("Click Protection")]
    [Range(0.1f, 2f)]
    public float clickCooldown = 0.3f; // Minimum time between clicks
    [Range(0f, 2f)]
    public float advanceDelay = 0.5f; // Time to wait after typing finishes before allowing advance
    public bool requireDoubleClickToAdvance = false; // Optional: require double-click to advance

    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip[] typingSounds;
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;
    [Range(0f, 0.2f)]
    public float pitchVariation = 0.1f;
    [Range(0f, 0.3f)]
    public float volumeVariation = 0.1f;

    [Header("Visual Effects")]
    public bool showCursor = true;
    public string cursorCharacter = "|";
    public float cursorBlinkSpeed = 0.5f;

    private TextMeshProUGUI textComponent;
    private Text legacyTextComponent;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool isPaused = false;
    private Coroutine typingCoroutine;
    private int lastSoundIndex = -1;

    // Click protection variables
    private float lastClickTime = 0f;
    private float typingFinishedTime = 0f;
    private bool canAdvance = false;

    void Start()
    {
        // Get text component (supports both TextMeshPro and legacy Text)
        textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
            legacyTextComponent = GetComponent<Text>();

        if (textComponent == null && legacyTextComponent == null)
        {
            Debug.LogError("TypewriterText: No Text or TextMeshProUGUI component found!");
            return;
        }

        // Clear text initially
        SetDisplayText("");

        if (startOnAwake && textLines.Count > 0)
        {
            StartCoroutine(DelayedStart());
        }
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(initialDelay);
        StartTyping();
    }

    public void StartTyping()
    {
        if (textLines.Count == 0) return;

        currentLineIndex = 0;
        StartTypingCurrentLine();
    }

    public void StartTypingCurrentLine()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        canAdvance = false; // Reset advance capability
        typingCoroutine = StartCoroutine(TypeText());
    }

    public void NextLine()
    {
        if (currentLineIndex < textLines.Count - 1)
        {
            currentLineIndex++;
            StartCoroutine(WaitAndTypeNext());
        }
        else if (loopText)
        {
            currentLineIndex = 0;
            StartCoroutine(WaitAndTypeNext());
        }
    }

    private IEnumerator WaitAndTypeNext()
    {
        yield return new WaitForSeconds(delayBetweenLines);
        StartTypingCurrentLine();
    }

    public void SkipToEnd()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            SetDisplayText(textLines[currentLineIndex]);
            isTyping = false;
            typingFinishedTime = Time.time;
            StartCoroutine(EnableAdvanceAfterDelay());
        }
    }

    private IEnumerator EnableAdvanceAfterDelay()
    {
        yield return new WaitForSeconds(advanceDelay);
        canAdvance = true;
    }

    public void PauseTyping()
    {
        isPaused = !isPaused;
    }

    public void AddTextLine(string newLine)
    {
        textLines.Add(newLine);
    }

    public void ClearAllText()
    {
        textLines.Clear();
        SetDisplayText("");
        currentLineIndex = 0;
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        string currentText = textLines[currentLineIndex];
        SetDisplayText("");

        for (int i = 0; i <= currentText.Length; i++)
        {
            // Check for pause
            while (isPaused)
                yield return null;

            SetDisplayText(currentText.Substring(0, i));

            // Play typing sound with variation
            if (audioSource != null && typingSounds != null && typingSounds.Length > 0 && i < currentText.Length)
            {
                PlayRandomTypingSound();
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        typingFinishedTime = Time.time;

        // Enable advancement after delay
        StartCoroutine(EnableAdvanceAfterDelay());

        // Auto-advance to next line after a longer delay (only if we're at the end)
        if (currentLineIndex >= textLines.Count - 1 && !loopText)
        {
            // Don't auto-advance on the last line
            yield break;
        }

        yield return new WaitForSeconds(delayBetweenLines + advanceDelay);
        if (!isTyping) // Make sure we haven't started typing again
        {
            NextLine();
        }
    }

    private IEnumerator BlinkCursor()
    {
        while (true)
        {
            // Add cursor
            string currentText = GetCurrentDisplayText();
            if (!currentText.EndsWith(cursorCharacter))
            {
                SetDisplayText(currentText + cursorCharacter);
            }

            yield return new WaitForSeconds(cursorBlinkSpeed);

            // Remove cursor
            currentText = GetCurrentDisplayText();
            if (currentText.EndsWith(cursorCharacter))
            {
                SetDisplayText(currentText.Substring(0, currentText.Length - 1));
            }

            yield return new WaitForSeconds(cursorBlinkSpeed);
        }
    }

    private void SetDisplayText(string text)
    {
        if (textComponent != null)
            textComponent.text = text;
        else if (legacyTextComponent != null)
            legacyTextComponent.text = text;
    }

    private string GetCurrentDisplayText()
    {
        if (textComponent != null)
            return textComponent.text;
        else if (legacyTextComponent != null)
            return legacyTextComponent.text;
        return "";
    }

    private void PlayRandomTypingSound()
    {
        if (typingSounds == null || typingSounds.Length == 0) return;

        // Pick a random sound that's different from the last one (if we have multiple sounds)
        int soundIndex;
        if (typingSounds.Length == 1)
        {
            soundIndex = 0;
        }
        else
        {
            do
            {
                soundIndex = Random.Range(0, typingSounds.Length);
            }
            while (soundIndex == lastSoundIndex && typingSounds.Length > 1);
        }

        lastSoundIndex = soundIndex;

        // Add some variation to make it feel more natural
        float finalVolume = soundVolume + Random.Range(-volumeVariation, volumeVariation);
        finalVolume = Mathf.Clamp01(finalVolume);

        float originalPitch = audioSource.pitch;
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

        audioSource.PlayOneShot(typingSounds[soundIndex], finalVolume);

        // Reset pitch for next sound
        audioSource.pitch = originalPitch;
    }

    private bool CanProcessClick()
    {
        // Check if enough time has passed since last click
        if (Time.time - lastClickTime < clickCooldown)
            return false;

        // If we're typing, always allow skipping
        if (isTyping)
            return true;

        // If not typing, check if we can advance
        return canAdvance;
    }

    // Public methods for external control
    public bool IsTyping => isTyping;
    public int CurrentLineIndex => currentLineIndex;
    public int TotalLines => textLines.Count;

    void Update()
    {
        // Handle input with click protection
        bool inputReceived = false;

        // Check for spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            inputReceived = true;
        }

        // Check for mouse click
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            inputReceived = true;
        }

        // Process input if received and allowed
        if (inputReceived && CanProcessClick())
        {
            lastClickTime = Time.time;

            if (isTyping)
            {
                SkipToEnd();
            }
            else if (canAdvance)
            {
                canAdvance = false; // Prevent immediate re-advancement
                NextLine();
            }
        }
    }
}