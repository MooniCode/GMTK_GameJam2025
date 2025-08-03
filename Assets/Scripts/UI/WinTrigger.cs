using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    public GameObject winScreen;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            winScreen.SetActive(true);
        }
    }
}
