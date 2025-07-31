using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimationInterface : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject interfacePanel;

    [Header("Animation Preview")]
    public Image animationPreview;
    public float animationSpeed = 0.2f;

    [Header("Timeline")]
    public Transform timelineContainer;
    public GameObject timelineSlotPrefab;
    public int maxTimelineSlots = 6;

    [Header("Inventory")]
    public Transform inventoryContent;
    public GameObject frameDisplayPrefab;

    [Header("Animation Creation")]
    public TMP_Dropdown animationTypeDropdown;

    private List<FrameData> timelineFrames;
    private List<TimelineSlot> timelineSlots;
    private int currentPreviewFrame = 0;
    private float animationTimer = 0f;
    private bool isPlaying = false;
    private bool isInterfaceOpen = false;

    // Dictionary to store timeline states for each animation type
    private Dictionary<string, List<FrameData>> savedTimelines;
    private string currentAnimationType = "idle"; // Default animation type

    void Start()
    {
        timelineFrames = new List<FrameData>();
        timelineSlots = new List<TimelineSlot>();
        savedTimelines = new Dictionary<string, List<FrameData>>();

        CreateTimelineSlots();
        interfacePanel.SetActive(false);

        // Setup dropdown callback
        if (animationTypeDropdown != null)
        {
            animationTypeDropdown.onValueChanged.AddListener(OnAnimationTypeChanged);
            // Initialize with the first option
            if (animationTypeDropdown.options.Count > 0)
            {
                currentAnimationType = animationTypeDropdown.options[animationTypeDropdown.value].text.ToLower();
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInterface();
        }

        // Animation preview logic
        if (isPlaying && timelineFrames.Count > 0)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= animationSpeed)
            {
                animationTimer = 0f;
                currentPreviewFrame = (currentPreviewFrame + 1) % GetActiveFrameCount();
                UpdatePreview();
            }
        }
    }

    public void ToggleInterface()
    {
        // Save current timeline state before closing
        if (isInterfaceOpen)
        {
            SaveCurrentTimelineState();

            // Create animation if timeline has frames
            if (HasFramesInTimeline())
            {
                CreateCustomAnimation();
            }
        }

        isInterfaceOpen = !isInterfaceOpen;
        interfacePanel.SetActive(isInterfaceOpen);

        if (isInterfaceOpen)
        {
            RefreshInventoryDisplay();
            LoadTimelineState(currentAnimationType);
        }
    }

    void OnAnimationTypeChanged(int dropdownIndex)
    {
        // Save current timeline before switching
        SaveCurrentTimelineState();

        // Get new animation type
        string newAnimationType = animationTypeDropdown.options[dropdownIndex].text.ToLower();
        currentAnimationType = newAnimationType;

        // Load timeline for new animation type
        LoadTimelineState(currentAnimationType);

        Debug.Log($"Switched to {currentAnimationType} animation timeline");
    }

    void SaveCurrentTimelineState()
    {
        // Only save timeline state if we actually have frames
        if (HasFramesInTimeline())
        {
            // Create a copy of current timeline
            List<FrameData> timelineCopy = new List<FrameData>();
            foreach (FrameData frame in timelineFrames)
            {
                timelineCopy.Add(frame); // This can be null, which is fine
            }

            // Save it to the dictionary
            savedTimelines[currentAnimationType] = timelineCopy;
            Debug.Log($"Saved timeline state for {currentAnimationType} with {GetActiveFrameCount()} frames");
        }
        else
        {
            // If timeline is empty, we can remove it from saved states
            if (savedTimelines.ContainsKey(currentAnimationType))
            {
                savedTimelines.Remove(currentAnimationType);
                Debug.Log($"Removed empty timeline for {currentAnimationType}");
            }
        }
    }

    void LoadTimelineState(string animationType)
    {
        // Always clear current timeline first
        ClearTimelineVisuals();

        // Initialize empty timeline
        timelineFrames.Clear();
        for (int i = 0; i < maxTimelineSlots; i++)
        {
            timelineFrames.Add(null);
        }

        // Only load saved timeline if it exists
        if (savedTimelines.ContainsKey(animationType))
        {
            List<FrameData> savedTimeline = savedTimelines[animationType];

            // Copy the saved timeline
            for (int i = 0; i < savedTimeline.Count && i < timelineFrames.Count; i++)
            {
                timelineFrames[i] = savedTimeline[i];
            }

            // Update visual slots
            UpdateTimelineVisuals();

            Debug.Log($"Loaded existing timeline for {animationType} with {GetActiveFrameCount()} frames");

            // Start preview if we have frames
            if (GetActiveFrameCount() > 0)
            {
                StartPreview();
            }
        }
        else
        {
            // Start with completely empty timeline
            Debug.Log($"Starting with empty timeline for {animationType}");
            isPlaying = false;
            animationPreview.sprite = null;
        }
    }

    void ClearTimelineVisuals()
    {
        foreach (TimelineSlot slot in timelineSlots)
        {
            slot.SetFrameFromData(null);
        }
        isPlaying = false;
        animationPreview.sprite = null;
    }

    void UpdateTimelineVisuals()
    {
        for (int i = 0; i < timelineSlots.Count && i < timelineFrames.Count; i++)
        {
            timelineSlots[i].SetFrameFromData(timelineFrames[i]);
        }
    }

    bool HasFramesInTimeline()
    {
        foreach (FrameData frame in timelineFrames)
        {
            if (frame != null) return true;
        }
        return false;
    }

    void CreateTimelineSlots()
    {
        for (int i = 0; i < maxTimelineSlots; i++)
        {
            GameObject slot = Instantiate(timelineSlotPrefab, timelineContainer);
            TimelineSlot slotScript = slot.GetComponent<TimelineSlot>();
            slotScript.slotIndex = i;
            slotScript.animationInterface = this;
            timelineSlots.Add(slotScript);
        }
    }

    void RefreshInventoryDisplay()
    {
        // Clear existing inventory display
        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("Total collected frames: " + PlayerAnimationManager.Instance.collectedFrames.Count);

        // Display all collected frames
        foreach (FrameData frameData in PlayerAnimationManager.Instance.collectedFrames)
        {
            GameObject frameDisplay = Instantiate(frameDisplayPrefab, inventoryContent);
            Image frameImage = frameDisplay.GetComponent<Image>();

            if (frameImage != null && frameData.frameSprite != null)
            {
                frameImage.sprite = frameData.frameSprite;
            }

            // Add draggable component to inventory frames
            DraggableFrame draggable = frameDisplay.AddComponent<DraggableFrame>();
            draggable.frameData = frameData;
        }
    }

    public void AddFrameToTimelineFromData(FrameData frameData, int slotIndex)
    {
        // Ensure the timeline list is big enough
        while (timelineFrames.Count <= slotIndex)
        {
            timelineFrames.Add(null);
        }

        // Add the frame data to the timeline
        timelineFrames[slotIndex] = frameData;

        // Start the preview animation
        StartPreview();
    }

    void StartPreview()
    {
        isPlaying = true;
        currentPreviewFrame = 0;
        UpdatePreview();
    }

    void UpdatePreview()
    {
        // Get all active (non-null) frames
        List<FrameData> activeFrames = new List<FrameData>();

        foreach (FrameData frame in timelineFrames)
        {
            if (frame != null)
            {
                activeFrames.Add(frame);
            }
        }

        // If we have active frames, show the current ones
        if (activeFrames.Count > 0)
        {
            // Make sure currentPreviewFrame is within bounds
            if (currentPreviewFrame >= activeFrames.Count)
            {
                currentPreviewFrame = 0;
            }

            animationPreview.sprite = activeFrames[currentPreviewFrame].frameSprite;
        }
        else
        {
            // No frames to show
            animationPreview.sprite = null;
        }
    }

    int GetActiveFrameCount()
    {
        int count = 0;
        foreach (var frame in timelineFrames)
        {
            if (frame != null) count++;
        }
        return count > 0 ? count : 1;
    }

    public void RemoveFrameFromTimeline(int slotIndex)
    {
        if (slotIndex < timelineFrames.Count)
        {
            timelineFrames[slotIndex] = null;
        }

        // Restart preview if there are still frames
        if (GetActiveFrameCount() > 0)
        {
            StartPreview();
        }
        else
        {
            isPlaying = false;
            animationPreview.sprite = null;
        }
    }

    // NEW METHOD: Create animation from timeline (now called automatically)
    public void CreateCustomAnimation()
    {
        List<FrameData> activeFrames = GetActiveFrames();

        if (activeFrames.Count == 0)
        {
            Debug.LogWarning("No frames in timeline to create animation!");
            return;
        }

        // Determine animation type based on dropdown or frame analysis
        string animationType = DetermineAnimationType(activeFrames);

        // Create the custom animation
        PlayerAnimationManager.Instance.CreateCustomAnimation(animationType, activeFrames, animationSpeed);

        Debug.Log($"Created {animationType} animation with {activeFrames.Count} frames!");

        // Clear the timeline after creating animation (optional - remove if you want to keep frames)
        // ClearTimeline();
    }

    void ClearTimeline()
    {
        for (int i = 0; i < timelineFrames.Count; i++)
        {
            timelineFrames[i] = null;
        }

        ClearTimelineVisuals();
    }

    List<FrameData> GetActiveFrames()
    {
        List<FrameData> activeFrames = new List<FrameData>();
        foreach (FrameData frame in timelineFrames)
        {
            if (frame != null)
            {
                activeFrames.Add(frame);
            }
        }
        return activeFrames;
    }

    string DetermineAnimationType(List<FrameData> frames)
    {
        // Use current dropdown selection
        if (animationTypeDropdown != null)
        {
            // Handle TMP_Dropdown (if using TextMeshPro)
            var tmpDropdown = animationTypeDropdown.GetComponent<TMPro.TMP_Dropdown>();
            if (tmpDropdown != null && tmpDropdown.options.Count > 0)
            {
                return tmpDropdown.options[tmpDropdown.value].text.ToLower();
            }
        }

        // Fallback: use current animation type
        return currentAnimationType;
    }
}