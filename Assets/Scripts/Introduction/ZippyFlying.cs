using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZippyFlying : MonoBehaviour
{
    [Header("Movement Settings")]
    public float flySpeed = 5f;
    public float sineAmplitude = 0.5f;
    public float sineFrequency = 5f;

    [Header("Stop Settings")]
    public float stopXPosition = 3f;
    public float waitTime = 10f;
    public float slowDownDistance = 2f; // Distance before stop to start slowing

    [Header("Dialogue")]
    public GameObject dialoguePanel;
    public float dialogueAnimationDuration = 0.5f; // Duration for dialogue box animation
    public TypewriterText typewriterText; // Reference to the TypewriterText component

    [Header("Dialogue Lines")]
    [TextArea(2, 5)]
    public List<string> idleDialogueLines = new List<string>
    {
        "Hey there! I'm Zippy, your friendly animation helper!",
        "I see you've lost all your animation frames. Don't worry, I'm here to help!",
        "Here are some IDLE frames to get you started.",
        "Press TAB to open the animation editor and create your idle animation!"
    };

    [TextArea(2, 5)]
    public List<string> walkDialogueLines = new List<string>
    {
        "Excellent work! You've mastered the idle animation!",
        "Now you're ready for the next step.",
        "Here are your WALK frames - these will let you move around!",
        "Create a walk animation and you'll be able to explore the world!",
        "Good luck on your adventure!"
    };

    [Header("Animation Editor Detection")]
    public AnimationInterface animationInterface; // Reference to the AnimationInterface script
    public List<GameObject> idleFramePrefabs; // Idle frame prefabs with AnimationFramePickup components
    public List<GameObject> walkFramePrefabs; // Walk frame prefabs with AnimationFramePickup components

    private Vector3 startPosition;
    private float timeElapsed = 0f;
    private bool isFlying = true;
    private bool isWaiting = false;
    private bool flyingOffScreen = false;
    private float hoverBaseY;
    private float slowDownStartX;
    private bool isAccelerating = false;

    // Introduction state tracking
    private enum IntroState
    {
        FlyingIn,
        ShowingIdleDialogue,
        GivingIdleFrames,
        WaitingForIdleAnimation,
        ShowingWalkDialogue,
        GivingWalkFrames,
        WaitingForWalkDialogue,
        FlyingAway
    }

    private IntroState currentState = IntroState.FlyingIn;
    private bool hasGivenIdleFrames = false;
    private bool hasGivenWalkFrames = false;
    private bool dialogueFinished = false;
    private bool wasAnimationEditorOpen = false;

    void Start()
    {
        startPosition = transform.position;
        slowDownStartX = stopXPosition + slowDownDistance;

        // Set dialogue box off screen
        dialoguePanel.transform.localPosition = new Vector3(0, -300, 0);
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;

        // Handle intro state machine
        HandleIntroState();

        if (isFlying && !isWaiting && !flyingOffScreen)
        {
            // Calculate speed based on distance to stop position
            float currentSpeed = flySpeed;
            if (transform.position.x <= slowDownStartX && transform.position.x > stopXPosition)
            {
                // Slow down as we approach the stop position
                float distanceToStop = transform.position.x - stopXPosition;
                float slowDownProgress = 1f - (distanceToStop / slowDownDistance);
                currentSpeed = Mathf.Lerp(flySpeed, 0f, slowDownProgress);
            }

            // Fly from right to left with calculated speed
            transform.position += Vector3.left * currentSpeed * Time.deltaTime;

            // Add sine wave movement
            float yOffset = Mathf.Sin(timeElapsed * sineFrequency) * sineAmplitude;
            transform.position = new Vector3(transform.position.x, startPosition.y + yOffset, transform.position.z);

            // Check if reached or passed stop position
            if (transform.position.x <= stopXPosition + 0.01f)
            {
                // Snap to exact stop position to avoid floating point drift
                transform.position = new Vector3(stopXPosition, transform.position.y, transform.position.z);
                StartWaiting();
            }
        }
        else if (isWaiting)
        {
            // Hover in place with same sine wave
            float yOffset = Mathf.Sin(timeElapsed * sineFrequency) * sineAmplitude;
            transform.position = new Vector3(stopXPosition, hoverBaseY + yOffset, transform.position.z);
        }
        else if (flyingOffScreen)
        {
            // Fly off screen to the left (after acceleration is complete)
            transform.position += Vector3.left * flySpeed * Time.deltaTime;

            // Continue sine wave while flying off
            float yOffset = Mathf.Sin(timeElapsed * sineFrequency) * sineAmplitude;
            transform.position = new Vector3(transform.position.x, startPosition.y + yOffset, transform.position.z);
        }
    }

    void HandleIntroState()
    {
        switch (currentState)
        {
            case IntroState.FlyingIn:
                // This state is handled by the normal flying logic
                break;

            case IntroState.ShowingIdleDialogue:
                if (!dialogueFinished)
                {
                    StartIdleDialogue();
                    dialogueFinished = true;
                }
                // Wait for dialogue to finish, then move to giving frames
                if (typewriterText != null && !typewriterText.IsTyping &&
                    typewriterText.CurrentLineIndex >= idleDialogueLines.Count - 1)
                {
                    currentState = IntroState.GivingIdleFrames;
                    dialogueFinished = false;
                }
                break;

            case IntroState.GivingIdleFrames:
                if (!hasGivenIdleFrames)
                {
                    GiveIdleFrames();
                    hasGivenIdleFrames = true;
                    currentState = IntroState.WaitingForIdleAnimation;
                }
                break;

            case IntroState.WaitingForIdleAnimation:
                // Check if player has created idle animation AND closed the animation editor
                bool hasIdleAnimation = PlayerAnimationManager.Instance != null &&
                                      PlayerAnimationManager.Instance.HasAnimation("idle");

                bool isAnimationEditorClosed = IsAnimationEditorClosed();

                // Track if the animation editor was opened (so we know they used it)
                if (!isAnimationEditorClosed)
                {
                    wasAnimationEditorOpen = true;
                }

                // Only proceed if they have the animation AND the editor is closed AND they had opened it
                if (hasIdleAnimation && isAnimationEditorClosed && wasAnimationEditorOpen)
                {
                    Debug.Log("Zippy: Player created idle animation and closed editor! Moving to walk dialogue.");
                    currentState = IntroState.ShowingWalkDialogue;
                    wasAnimationEditorOpen = false; // Reset for potential future use
                }
                break;

            case IntroState.ShowingWalkDialogue:
                if (!dialogueFinished)
                {
                    StartWalkDialogue();
                    dialogueFinished = true;
                }
                // Wait for dialogue to finish, then move to giving walk frames
                if (typewriterText != null && !typewriterText.IsTyping &&
                    typewriterText.CurrentLineIndex >= walkDialogueLines.Count - 1)
                {
                    currentState = IntroState.GivingWalkFrames;
                    dialogueFinished = false;
                }
                break;

            case IntroState.GivingWalkFrames:
                if (!hasGivenWalkFrames)
                {
                    GiveWalkFrames();
                    hasGivenWalkFrames = true;
                    currentState = IntroState.WaitingForWalkDialogue;
                }
                break;

            case IntroState.WaitingForWalkDialogue:
                // Wait a moment for player to read the final message, then fly away
                StartCoroutine(FlyAwayAfterDelay(3f));
                currentState = IntroState.FlyingAway;
                break;

            case IntroState.FlyingAway:
                // This state is handled by the flying off screen logic
                break;
        }
    }

    void GiveIdleFrames()
    {
        if (PlayerAnimationManager.Instance == null)
        {
            Debug.LogError("PlayerAnimationManager.Instance is null!");
            return;
        }

        Debug.Log("Zippy: Giving idle frames to player");

        // Give idle frames using prefabs (like StartingFramesManager)
        foreach (GameObject framePrefab in idleFramePrefabs)
        {
            if (framePrefab != null)
            {
                AnimationFramePickup framePickup = framePrefab.GetComponent<AnimationFramePickup>();
                if (framePickup != null)
                {
                    PlayerAnimationManager.Instance.AddFrameToInventory(
                        framePickup.frameType,
                        framePickup.frameSprite,
                        framePickup.animationSprite
                    );
                    Debug.Log($"Zippy gave frame: {framePickup.frameType}");
                }
                else
                {
                    Debug.LogWarning($"Idle prefab {framePrefab.name} doesn't have an AnimationFramePickup component!");
                }
            }
        }
    }

    void GiveWalkFrames()
    {
        if (PlayerAnimationManager.Instance == null)
        {
            Debug.LogError("PlayerAnimationManager.Instance is null!");
            return;
        }

        Debug.Log("Zippy: Giving walk frames to player");

        // Give walk frames using prefabs (like StartingFramesManager)
        foreach (GameObject framePrefab in walkFramePrefabs)
        {
            if (framePrefab != null)
            {
                AnimationFramePickup framePickup = framePrefab.GetComponent<AnimationFramePickup>();
                if (framePickup != null)
                {
                    PlayerAnimationManager.Instance.AddFrameToInventory(
                        framePickup.frameType,
                        framePickup.frameSprite,
                        framePickup.animationSprite
                    );
                    Debug.Log($"Zippy gave frame: {framePickup.frameType}");
                }
                else
                {
                    Debug.LogWarning($"Walk prefab {framePrefab.name} doesn't have an AnimationFramePickup component!");
                }
            }
        }
    }

    void StartIdleDialogue()
    {
        if (typewriterText != null)
        {
            typewriterText.ClearAllText();
            foreach (string line in idleDialogueLines)
            {
                typewriterText.AddTextLine(line);
            }
            typewriterText.StartTyping();
        }
    }

    void StartWalkDialogue()
    {
        if (typewriterText != null)
        {
            typewriterText.ClearAllText();
            foreach (string line in walkDialogueLines)
            {
                typewriterText.AddTextLine(line);
            }
            typewriterText.StartTyping();
        }
    }

    bool IsAnimationEditorClosed()
    {
        if (animationInterface != null)
        {
            return !animationInterface.IsInterfaceOpen; // Note the capital I
        }

        Debug.LogWarning("No AnimationInterface reference set! Please assign the AnimationInterface script.");
        return true;
    }

    IEnumerator FlyAwayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Hide dialogue box before flying away
        StartCoroutine(HideDialogueBox());

        // Start flying away sequence
        isWaiting = false;
        isAccelerating = true;
        StartCoroutine(AccelerateOffScreen());
    }

    void StartWaiting()
    {
        isFlying = false;
        isWaiting = true;
        // Store the current Y position to avoid snapping
        hoverBaseY = transform.position.y;

        // Start the dialogue animation
        StartCoroutine(AnimateDialogueBox());

        // Set state to showing idle dialogue first
        currentState = IntroState.ShowingIdleDialogue;
    }

    IEnumerator AnimateDialogueBox()
    {
        Vector3 startPos = new Vector3(0, -300, 0);
        Vector3 endPos = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < dialogueAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dialogueAnimationDuration;

            // Use an easing curve for smoother animation (optional)
            // t = Mathf.SmoothStep(0, 1, t);

            dialoguePanel.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Ensure final position is exactly at target
        dialoguePanel.transform.localPosition = endPos;
    }

    IEnumerator HideDialogueBox()
    {
        Vector3 startPos = dialoguePanel.transform.localPosition;
        Vector3 endPos = new Vector3(0, -300, 0);
        float elapsed = 0f;

        while (elapsed < dialogueAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dialogueAnimationDuration;

            dialoguePanel.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        dialoguePanel.transform.localPosition = endPos;
    }

    IEnumerator AccelerateOffScreen()
    {
        float accelerationTime = 0.25f;
        float elapsed = 0f;

        while (elapsed < accelerationTime)
        {
            elapsed += Time.deltaTime;
            float speedMultiplier = elapsed / accelerationTime;

            float currentSpeed = Mathf.Lerp(0f, flySpeed, speedMultiplier);
            transform.position += Vector3.left * currentSpeed * Time.deltaTime;

            // Use startPosition.y instead of hoverBaseY
            float yOffset = Mathf.Sin(timeElapsed * sineFrequency) * sineAmplitude;
            transform.position = new Vector3(transform.position.x, startPosition.y + yOffset, transform.position.z);

            yield return null;
        }

        isAccelerating = false;
        flyingOffScreen = true;
    }

    // Public method to manually advance the intro (for testing or dialogue system integration)
    public void AdvanceIntro()
    {
        switch (currentState)
        {
            case IntroState.WaitingForIdleAnimation:
                if (PlayerAnimationManager.Instance != null &&
                    PlayerAnimationManager.Instance.HasAnimation("idle"))
                {
                    currentState = IntroState.GivingWalkFrames;
                }
                break;
        }
    }
}