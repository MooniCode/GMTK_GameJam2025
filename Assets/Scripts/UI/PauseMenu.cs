using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private bool isPaused = false;

    private void Start()
    {
        // Set up button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        pauseMenu.SetActive(isPaused);

        // Pause/unpause the game
        Time.timeScale = isPaused ? 0f : 1f;

        // Optional: Control cursor visibility and lock state
        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Uncomment these if you want to lock cursor when not paused
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;

        // Restore cursor state if needed
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    public void RestartGame()
    {
        // Make sure time scale is back to normal before restarting
        Time.timeScale = 1f;

        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        // Make sure time scale is back to normal
        Time.timeScale = 1f;

#if UNITY_EDITOR
        // Stop playing the scene in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // Quit the application in a build
            Application.Quit();
#endif
    }

    // Public methods for external access
    public void Pause()
    {
        isPaused = true;
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Unpause()
    {
        isPaused = false;
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }

    public bool IsPaused => isPaused;

    private void OnDestroy()
    {
        // Clean up button listeners to prevent memory leaks
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeGame);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartGame);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }
}