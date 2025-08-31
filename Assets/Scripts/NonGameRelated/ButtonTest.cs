using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonTest : MonoBehaviour
{
    public void OnMenuButtonClick()
    {
        AudioManager.instance.PlayButtonClick();
        SceneManager.LoadScene(0);
    }
}
