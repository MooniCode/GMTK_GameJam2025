using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private AudioSource buttonAudioSource;
    [SerializeField] private AudioClip buttonClickSound;

    private void Awake()
    {
        // Singleton pattern - only one AudioManager exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayButtonClick()
    {
        buttonAudioSource.PlayOneShot(buttonClickSound);
    }
}
