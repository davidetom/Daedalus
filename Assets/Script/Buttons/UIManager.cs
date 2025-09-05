using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public GameObject pauseSaveButton;
    public Canvas minimapCanvas;


    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    public void DisableMinimapCanvas()
    {
        if (DifficultyManager.Instance == null)
        {
            Debug.Log("Difficulty Manager non trovato!");
        }
        DifficultyLevel currentDifficulty = DifficultyManager.Instance.GetCurrentDifficulty();
        if (currentDifficulty != DifficultyLevel.Hard)
        {
            minimapCanvas.gameObject.SetActive(false);
        }
        else
        {
            return;
        }
    }

    public void ActivateMinimapCanvas()
    {
        if (DifficultyManager.Instance == null)
        {
            Debug.Log("Difficulty Manager non trovato!");
        }
        DifficultyLevel currentDifficulty = DifficultyManager.Instance.GetCurrentDifficulty();
        if (currentDifficulty != DifficultyLevel.Hard)
        {
            minimapCanvas.gameObject.SetActive(true);
        }
        else
        {
            return;
        }
    }
}
