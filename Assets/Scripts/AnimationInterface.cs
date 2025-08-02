using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AnimationTypeConfig
{
    public string animationType;
    public int maxSlots;

    [Header("Timeline Layout")]
    public int leftPadding = 0;
    public int rightPadding = 0;

    [Header("Correct Animation Pattern")]
    [Tooltip("Drag the correct frame prefabs in the right order here. Must match maxSlots count.")]
    public AnimationFramePickup[] correctPatternPrefabs;
}

public class AnimationInterface : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject interfacePanel;

    [Header("Animation Preview")]
    public Image animationPreviewCharacter;
    public Image animationPreviewBackground;
    public float animationSpeed = 0.2f;

    public Sprite animationPreviewBackgroundOnTexture;
    public Sprite animationPreviewBackgroundOffTexture;

    public Animator cameraOverlayAnimator;

    [Header("Timeline")]
    public Transform timelineContainer;
    public GameObject timelineSlotPrefab;

    // Reference to the Horizontal Layout Group
    private HorizontalLayoutGroup timelineLayoutGroup;

    [Header("Editor Lock Control")]
    private bool isEditorLocked = false;

    // Control editor access
    public void SetEditorLocked(bool locked)
    {
        isEditorLocked = locked;

        // If locking while editor is open, close it
        if (locked && isInterfaceOpen)
        {
            ToggleInterface();
        }
    }

    [Header("Animation Configuration")]
    public List<AnimationTypeConfig> animationConfigs = new List<AnimationTypeConfig>
    {
        new AnimationTypeConfig { animationType = "idle", maxSlots = 3, leftPadding = 100, rightPadding = 100, correctPatternPrefabs = new AnimationFramePickup[3] },
        new AnimationTypeConfig { animationType = "walk", maxSlots = 8, leftPadding = 20, rightPadding = 20, correctPatternPrefabs = new AnimationFramePickup[8] },
        new AnimationTypeConfig { animationType = "jump", maxSlots = 4, leftPadding = 80, rightPadding = 80, correctPatternPrefabs = new AnimationFramePickup[4] },
        new AnimationTypeConfig { animationType = "run", maxSlots = 6, leftPadding = 40, rightPadding = 40, correctPatternPrefabs = new AnimationFramePickup[6] }
    };

    [Header("Inventory")]
    public Transform inventoryContent;
    public GameObject frameDisplayPrefab;

    [Header("Animation Creation")]
    public TMP_Dropdown animationTypeDropdown;

    [Header("UI Animation Control")]
    public Animator uiAnimator; // Drag your animator here in the inspector

    [Header("Player Feedback")]
    public TMPro.TextMeshProUGUI feedbackText; // Text to show "X/Y frames correct"

    private List<FrameData> timelineFrames;
    private List<TimelineSlot> timelineSlots;
    private int currentPreviewFrame = 0;
    private float animationTimer = 0f;
    private bool isPlaying = false;
    private bool isInterfaceOpen = false;

    // Make isInterfaceOpen accessible
    public bool IsInterfaceOpen => isInterfaceOpen;

    // Dictionary to store timeline states for each animation type
    private Dictionary<string, List<FrameData>> savedTimelines;
    private string currentAnimationType = "idle"; // Default animation type

    void Start()
    {
        timelineFrames = new List<FrameData>();
        timelineSlots = new List<TimelineSlot>();
        savedTimelines = new Dictionary<string, List<FrameData>>();

        // Get the Horizontal Layout Group component
        timelineLayoutGroup = timelineContainer.GetComponent<HorizontalLayoutGroup>();
        if (timelineLayoutGroup == null)
        {
            Debug.LogError("Timeline container must have a HorizontalLayoutGroup component!");
        }

        // Make the alpha of the animationPreview image 0
        Color color = animationPreviewCharacter.color;
        color.a = 0f;
        animationPreviewCharacter.color = color;

        // Make the animationPreview background the OFF version
        animationPreviewBackground.sprite = animationPreviewBackgroundOffTexture;

        // Initialize with default animation type
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

                // Set initial UI animator state
                UpdateUIAnimatorParameters(currentAnimationType);

                // Recreate timeline with correct slot count for initial animation
                RecreateTimelineForCurrentAnimation();
            }
        }

        SetupInventoryDropHandler();

        // Validate correct patterns in development
        ValidateCorrectPatterns();
    }

    void Update()
    {
        // Only allow TAB toggle if editor is not locked
        if (Input.GetKeyDown(KeyCode.Tab) && !isEditorLocked)
        {
            ToggleInterface();
        }

        // Animation preview logic (keep your existing code)
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
            UpdateFeedbackText(); // Update feedback when opening interface
        }
    }

    void OnAnimationTypeChanged(int dropdownIndex)
    {
        // Save current timeline before switching
        SaveCurrentTimelineState();

        // Get new animation type
        string newAnimationType = animationTypeDropdown.options[dropdownIndex].text.ToLower();
        currentAnimationType = newAnimationType;

        // Update UI animator parameters
        UpdateUIAnimatorParameters(newAnimationType);

        // Recreate timeline with correct slot count for new animation type
        RecreateTimelineForCurrentAnimation();

        // Load timeline state AFTER a small delay to ensure new slots are initialized
        StartCoroutine(LoadTimelineStateDelayed(currentAnimationType));
    }

    IEnumerator LoadTimelineStateDelayed(string animationType)
    {
        // Wait one frame for the new timeline slots to be fully initialized
        yield return null;

        // Now load the timeline state
        LoadTimelineState(animationType);

        // Refresh inventory to show frames for the new animation type
        RefreshInventoryDisplay();
    }

    void RecreateTimelineForCurrentAnimation()
    {
        // Clear existing timeline slots
        ClearAllTimelineSlots();

        // Update layout group settings for current animation type
        UpdateTimelineLayoutForCurrentAnimation();

        // Create new timeline slots based on current animation type
        CreateTimelineSlots();
    }

    void UpdateTimelineLayoutForCurrentAnimation()
    {
        if (timelineLayoutGroup == null) return;

        AnimationTypeConfig config = GetCurrentAnimationConfig();
        if (config != null)
        {
            // Update padding
            timelineLayoutGroup.padding.left = config.leftPadding;
            timelineLayoutGroup.padding.right = config.rightPadding;

            Debug.Log($"Updated timeline layout for {currentAnimationType}: Left={config.leftPadding}, Right={config.rightPadding}");
        }
        else
        {
            Debug.LogWarning($"No config found for animation type: {currentAnimationType}");
            // Set default values
            timelineLayoutGroup.padding.left = 50;
            timelineLayoutGroup.padding.right = 50;
        }
    }

    AnimationTypeConfig GetCurrentAnimationConfig()
    {
        return animationConfigs.Find(c => c.animationType.ToLower() == currentAnimationType.ToLower());
    }

    void ClearAllTimelineSlots()
    {
        // Destroy all existing timeline slot GameObjects
        foreach (TimelineSlot slot in timelineSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }

        // Clear the list
        timelineSlots.Clear();
    }

    int GetMaxSlotsForAnimationType(string animationType)
    {
        AnimationTypeConfig config = animationConfigs.Find(c => c.animationType.ToLower() == animationType.ToLower());
        return config != null ? config.maxSlots : 6; // Default to 6 if not found
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
        }
        else
        {
            // If timeline is empty, we can remove it from saved states
            if (savedTimelines.ContainsKey(currentAnimationType))
            {
                savedTimelines.Remove(currentAnimationType);
            }
        }
    }

    void LoadTimelineState(string animationType)
    {
        Debug.Log($"Loading timeline state for: {animationType}");

        // Get the max slots for this animation type
        int maxSlots = GetMaxSlotsForAnimationType(animationType);
        Debug.Log($"Max slots for {animationType}: {maxSlots}");

        // Initialize empty timeline with correct size
        timelineFrames.Clear();
        for (int i = 0; i < maxSlots; i++)
        {
            timelineFrames.Add(null);
        }

        // Check if we have saved timeline for this animation type
        if (savedTimelines.ContainsKey(animationType))
        {
            List<FrameData> savedTimeline = savedTimelines[animationType];
            Debug.Log($"Found saved timeline for {animationType} with {savedTimeline.Count} slots");

            // Copy the saved timeline (only up to the current max slots)
            for (int i = 0; i < savedTimeline.Count && i < timelineFrames.Count; i++)
            {
                timelineFrames[i] = savedTimeline[i];
                if (savedTimeline[i] != null)
                {
                    Debug.Log($"Restored frame at slot {i}: {savedTimeline[i].frameType}");
                }
            }

            // Update visual slots - AFTER we've restored the frame data
            UpdateTimelineVisuals();

            // Start preview if we have frames
            if (GetActiveFrameCount() > 0)
            {
                StartPreview();
            }
        }
        else
        {
            Debug.Log($"No saved timeline found for {animationType}");
            // Clear timeline visuals since we have no saved state
            ClearTimelineVisuals();
        }

        // Update the animation immediately after loading timeline
        UpdateCurrentAnimation();

        // Update feedback text after loading
        UpdateFeedbackText();
    }

    void ClearTimelineVisuals()
    {
        foreach (TimelineSlot slot in timelineSlots)
        {
            if (slot != null)
            {
                slot.SetFrameFromData(null);
            }
        }
        isPlaying = false;
        animationPreviewCharacter.sprite = null;

        // Make the animationPreview background the grey version
        animationPreviewBackground.sprite = animationPreviewBackgroundOffTexture;

        // Stop the camera overlay animation
        cameraOverlayAnimator.SetBool("isRecording", false);

        // Set alpha to 0 when clearing timeline
        Color color = animationPreviewCharacter.color;
        color.a = 0f;
        animationPreviewCharacter.color = color;
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
        int maxSlots = GetMaxSlotsForAnimationType(currentAnimationType);

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slot = Instantiate(timelineSlotPrefab, timelineContainer);
            TimelineSlot slotScript = slot.GetComponent<TimelineSlot>();
            slotScript.slotIndex = i;
            slotScript.animationInterface = this;
            timelineSlots.Add(slotScript);
        }
    }

    public void RefreshInventoryDisplay()
    {
        // Clear existing inventory display
        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
        }

        // Debug logging
        Debug.Log($"RefreshInventoryDisplay - Current Animation Type: '{currentAnimationType}'");
        Debug.Log($"Total collected frames: {PlayerAnimationManager.Instance.collectedFrames.Count}");

        // Display only frames that match the current animation type
        foreach (FrameData frameData in PlayerAnimationManager.Instance.collectedFrames)
        {
            Debug.Log($"Checking frame with frameType: '{frameData.frameType}' against current: '{currentAnimationType}'");

            // Only show frames that match the current animation type
            if (frameData.frameType.ToLower() == currentAnimationType.ToLower())
            {
                Debug.Log($"Match found! Displaying frame: {frameData.frameType}");

                GameObject frameDisplay = Instantiate(frameDisplayPrefab, inventoryContent);
                Image frameImage = frameDisplay.GetComponent<Image>();

                if (frameImage != null && frameData.GetUISprite() != null)
                {
                    frameImage.sprite = frameData.GetUISprite();
                }

                // Add draggable component to inventory frames
                DraggableFrame draggable = frameDisplay.AddComponent<DraggableFrame>();
                draggable.frameData = frameData;
            }
            else
            {
                Debug.Log($"No match: '{frameData.frameType.ToLower()}' != '{currentAnimationType.ToLower()}'");
            }
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

        // Update feedback text
        UpdateFeedbackText();

        // Update the animation immediately
        UpdateCurrentAnimation();

        // Start the preview animation
        StartPreview();
    }

    void StartPreview()
    {
        // Make the animationPreview background the ON version
        animationPreviewBackground.sprite = animationPreviewBackgroundOnTexture;

        // Play the camera overlay animation
        cameraOverlayAnimator.SetBool("isRecording", true);

        // Set the alpha of the animationPreview back to 100
        Color color = animationPreviewCharacter.color;
        color.a = 1f;
        animationPreviewCharacter.color = color;

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

            animationPreviewCharacter.sprite = activeFrames[currentPreviewFrame].GetAnimationSprite();
        }
        else
        {
            // Make the animationPreview background the OFF version
            animationPreviewBackground.sprite = animationPreviewBackgroundOffTexture;

            // Stop the camera overlay animation
            cameraOverlayAnimator.SetBool("isRecording", false);

            // No frames to show - set sprite to null AND alpha to 0
            animationPreviewCharacter.sprite = null;
            Color color = animationPreviewCharacter.color;
            color.a = 0f;
            animationPreviewCharacter.color = color;
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

        // Update feedback text
        UpdateFeedbackText();

        // Update the animation immediately
        UpdateCurrentAnimation();

        // Restart preview if there are still frames
        if (GetActiveFrameCount() > 0)
        {
            StartPreview();
        }
        else
        {
            isPlaying = false;
            animationPreviewCharacter.sprite = null;

            // Make the animationPreview background the grey version
            animationPreviewBackground.sprite = animationPreviewBackgroundOffTexture;

            // Stop the camera overlay animation
            cameraOverlayAnimator.SetBool("isRecording", false);

            // Set alpha back to 0 when no frames are left
            Color color = animationPreviewCharacter.color;
            color.a = 0f;
            animationPreviewCharacter.color = color;
        }
    }

    public void CreateCustomAnimation()
    {
        List<FrameData> activeFrames = GetActiveFrames();

        // Get the required number of slots for this animation type
        int maxSlots = GetMaxSlotsForAnimationType(currentAnimationType);

        if (activeFrames.Count != maxSlots)
        {
            Debug.Log($"Animation '{currentAnimationType}' requires {maxSlots} frames, but only {activeFrames.Count} are provided.");
            return;
        }

        // Determine animation type based on dropdown or frame analysis
        string animationType = DetermineAnimationType(activeFrames);

        // Determine if this animation should loop
        bool shouldLoop = ShouldAnimationLoop(animationType);

        // Create the custom animation with looping setting
        PlayerAnimationManager.Instance.CreateCustomAnimation(animationType, activeFrames, animationSpeed, shouldLoop);
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

    void SetupInventoryDropHandler()
    {
        // Add a drop handler component to the inventory content area
        if (inventoryContent != null)
        {
            InventoryDropHandler dropHandler = inventoryContent.gameObject.GetComponent<InventoryDropHandler>();
            if (dropHandler == null)
            {
                dropHandler = inventoryContent.gameObject.AddComponent<InventoryDropHandler>();
            }
            dropHandler.animationInterface = this;
        }
    }

    void UpdateCurrentAnimation()
    {
        List<FrameData> activeFrames = GetActiveFrames();
        int maxSlots = GetMaxSlotsForAnimationType(currentAnimationType);

        if (activeFrames.Count == maxSlots)
        {
            // Determine if this animation should loop
            bool shouldLoop = ShouldAnimationLoop(currentAnimationType);

            // Only create/update the animation if timeline is complete
            PlayerAnimationManager.Instance.CreateCustomAnimation(currentAnimationType, activeFrames, animationSpeed, shouldLoop);
        }
        else
        {
            // Remove the animation if timeline is incomplete
            PlayerAnimationManager.Instance.RemoveCustomAnimation(currentAnimationType);
        }
    }

    // Public method to add new animation type configurations at runtime if needed
    public void AddAnimationTypeConfig(string animationType, int maxSlots, int leftPadding = 50, int rightPadding = 50)
    {
        // Check if config already exists
        AnimationTypeConfig existingConfig = animationConfigs.Find(c => c.animationType.ToLower() == animationType.ToLower());

        if (existingConfig != null)
        {
            // Update existing config
            existingConfig.maxSlots = maxSlots;
            existingConfig.leftPadding = leftPadding;
            existingConfig.rightPadding = rightPadding;
        }
        else
        {
            // Add new config
            animationConfigs.Add(new AnimationTypeConfig
            {
                animationType = animationType,
                maxSlots = maxSlots,
                leftPadding = leftPadding,
                rightPadding = rightPadding
            });
        }
    }

    // Utility method to update layout settings at runtime
    public void UpdateLayoutForAnimation(string animationType, int leftPadding, int rightPadding)
    {
        AnimationTypeConfig config = animationConfigs.Find(c => c.animationType.ToLower() == animationType.ToLower());
        if (config != null)
        {
            config.leftPadding = leftPadding;
            config.rightPadding = rightPadding;

            // If this is the current animation, update the layout immediately
            if (currentAnimationType.ToLower() == animationType.ToLower())
            {
                UpdateTimelineLayoutForCurrentAnimation();
            }
        }
    }

    // Method to update UI Animator parameters based on current animation type
    void UpdateUIAnimatorParameters(string animationType)
    {
        if (uiAnimator == null)
        {
            Debug.LogWarning("UI Animator is not assigned in AnimationInterface!");
            return;
        }

        // Reset all animation type parameters to false
        uiAnimator.SetBool("isInIdle", false);
        uiAnimator.SetBool("isInWalk", false);
        uiAnimator.SetBool("isInJump", false);
        uiAnimator.SetBool("isInProne", false);

        // Set the appropriate parameter to true based on current animation type
        switch (animationType.ToLower())
        {
            case "idle":
                uiAnimator.SetBool("isInIdle", true);
                Debug.Log("UI Animator: Set isInIdle to true");
                break;
            case "walk":
                uiAnimator.SetBool("isInWalk", true);
                Debug.Log("UI Animator: Set isInWalk to true");
                break;
            case "jump":
                uiAnimator.SetBool("isInJump", true);
                Debug.Log("UI Animator: Set isInJump to true");
                break;
            case "prone":
                uiAnimator.SetBool("isInProne", true);
                Debug.Log("UI Animator: Set isInProne to true");
                break;
            default:
                Debug.LogWarning($"No UI animation parameter defined for animation type: {animationType}");
                break;
        }
    }

    // Public method to manually trigger UI animation update (useful for debugging)
    public void RefreshUIAnimation()
    {
        UpdateUIAnimatorParameters(currentAnimationType);
    }

    // Method to calculate and update feedback text
    void UpdateFeedbackText()
    {
        if (feedbackText == null) return;

        AnimationTypeConfig config = GetCurrentAnimationConfig();
        if (config == null || config.correctPatternPrefabs == null)
        {
            feedbackText.text = "No pattern defined";
            return;
        }

        int correctCount = GetCorrectFrameCount();
        int totalSlots = config.maxSlots;

        feedbackText.text = $"{correctCount}/{totalSlots} frames correct";

        // Optional: Change color based on progress
        if (correctCount == totalSlots)
        {
            feedbackText.color = Color.green; // Perfect!
        }
        else if (correctCount > totalSlots / 2)
        {
            feedbackText.color = Color.yellow; // Good progress
        }
        else
        {
            feedbackText.color = Color.white; // Default/needs work
        }
    }

    // Calculate how many frames are in the correct position
    int GetCorrectFrameCount()
    {
        AnimationTypeConfig config = GetCurrentAnimationConfig();
        if (config == null || config.correctPatternPrefabs == null) return 0;

        int correctCount = 0;

        for (int i = 0; i < timelineFrames.Count && i < config.correctPatternPrefabs.Length; i++)
        {
            // Check if the frame in this slot matches the correct pattern for THIS SPECIFIC POSITION
            if (timelineFrames[i] != null && config.correctPatternPrefabs[i] != null)
            {
                // Only count as correct if the RIGHT frame is in the RIGHT position
                if (IsFrameCorrectFromPrefab(timelineFrames[i], config.correctPatternPrefabs[i]))
                {
                    correctCount++;
                }
                // If there's a frame but it's the wrong one for this position, don't count it
            }
            // Empty slots don't count as correct (since we expect a specific frame there)
        }

        return correctCount;
    }

    // Method to compare if frame matches the prefab's frame data
    bool IsFrameCorrectFromPrefab(FrameData playerFrame, AnimationFramePickup correctPrefab)
    {
        if (correctPrefab == null) return false;

        // Compare by sprite - each frame should have a unique sprite
        return playerFrame.GetUISprite() == correctPrefab.frameSprite;
    }

    // Public method to manually refresh feedback (useful for debugging)
    public void RefreshFeedback()
    {
        UpdateFeedbackText();
    }

    // Method to validate that correct patterns match slot counts
    void ValidateCorrectPatterns()
    {
        foreach (var config in animationConfigs)
        {
            if (config.correctPatternPrefabs != null && config.correctPatternPrefabs.Length != config.maxSlots)
            {
                Debug.LogWarning($"Animation '{config.animationType}' has {config.maxSlots} slots but {config.correctPatternPrefabs.Length} correct pattern prefabs. These should match!");
            }
        }
    }

    bool ShouldAnimationLoop(string animationType)
    {
        switch (animationType.ToLower())
        {
            case "jump":
                return false; // Jump should not loop
            case "prone":
                return false; // Prone should not loop
            case "idle":
                return true;
            case "walk":
                return true;
            default:
                return true;  // Default to looping for other animations
        }
    }
}