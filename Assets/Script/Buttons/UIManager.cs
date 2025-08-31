using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject pauseSaveButton;


    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }
}
