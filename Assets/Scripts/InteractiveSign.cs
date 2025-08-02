using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractiveSign : MonoBehaviour
{
    [Header("Sign Configuration")]
    [SerializeField] private GameObject textBox;

    [Header("Sign Text")]
    [SerializeField][TextArea(3, 5)] private string signText = "Enter your sign text here...";

    private TMP_Text textComponent;

    void Start()
    {
        // Hide the text box at start
        if (textBox != null)
        {
            textBox.SetActive(false);

            // Get the Text component from the textBox (assuming it's on the textBox itself or a child)
            textComponent = textBox.GetComponent<TMP_Text>();
            if (textComponent == null)
            {
                textComponent = textBox.GetComponentInChildren<TMP_Text>();
            }

            // Set the initial text
            if (textComponent != null)
            {
                textComponent.text = signText;
            }
            else
            {
                Debug.LogWarning("No Text component found in the assigned textBox or its children!");
            }
        }
        else
        {
            Debug.LogWarning("TextBox not assigned in the inspector!");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ShowTextBox();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HideTextBox();
        }
    }

    private void ShowTextBox()
    {
        if (textBox != null)
        {
            textBox.SetActive(true);

            // Update text in case it was changed in inspector during runtime
            if (textComponent != null)
            {
                textComponent.text = signText;
            }
        }
    }

    private void HideTextBox()
    {
        if (textBox != null)
        {
            textBox.SetActive(false);
        }
    }

    // This allows you to change the text at runtime from other scripts if needed
    public void SetSignText(string newText)
    {
        signText = newText;
        if (textComponent != null)
        {
            textComponent.text = signText;
        }
    }
}