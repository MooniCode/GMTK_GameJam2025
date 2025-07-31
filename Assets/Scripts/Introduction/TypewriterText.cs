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
    [Range(0.01f, 0.2f)]
    public float typingSpeed = 0.05f;
    public float delayBetweenLines = 1f;
    public bool startOnAwake = true;
    public bool loopText = false;
    [Range(0f, 10f)]
    public float initialDelay = 2f; // New delay setting

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
        }
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

        // Auto-advance to next line after a delay
        yield return new WaitForSeconds(delayBetweenLines);
        NextLine();
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

    // Public methods for external control
    public bool IsTyping => isTyping;
    public int CurrentLineIndex => currentLineIndex;
    public int TotalLines => textLines.Count;

    void Update()
    {
        // Optional: Skip typing with spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
                SkipToEnd();
            else
                NextLine();
        }

        // Handle mouse clicks to skip/advance text
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            if (isTyping)
                SkipToEnd();
            else
                NextLine();
        }
    }
}