using UnityEngine;
using System.Collections;
using System.IO;

public class ScreenShotTaker : MonoBehaviour
{
    [SerializeField] private KeyCode screenshotKey = KeyCode.F12;
    [SerializeField] private string folderName = "Screenshots";
    [SerializeField] private int superSize = 1; // 1 = normal resolution, 2 = double, etc.

    private string screenshotPath;

    void Start()
    {
        // Create screenshots folder in persistent data path
        screenshotPath = Path.Combine(Application.persistentDataPath, folderName);

        if (!Directory.Exists(screenshotPath))
        {
            Directory.CreateDirectory(screenshotPath);
        }

        Debug.Log($"Screenshots will be saved to: {screenshotPath}");
    }

    void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshot();
        }
    }

    public void TakeScreenshot()
    {
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"Screenshot_{timestamp}.png";
        string fullPath = Path.Combine(screenshotPath, filename);

        ScreenCapture.CaptureScreenshot(fullPath, superSize);

        Debug.Log($"Screenshot saved: {fullPath}");
    }
}