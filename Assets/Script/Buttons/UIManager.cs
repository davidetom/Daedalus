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
        minimapCanvas.gameObject.SetActive(false);
    }

    public void ActivateMinimapCanvas()
    {
        if (DifficultyManager.Instance == null)
        {
            //Debug.LogWarning("DifficultyManager non trovato! Attivando minimappa di default.");
            minimapCanvas.gameObject.SetActive(true);
            return;
        }

        DifficultyLevel currentDifficulty = DifficultyManager.Instance.GetCurrentDifficulty();
        if (currentDifficulty != DifficultyLevel.Hard)
        {
            minimapCanvas.gameObject.SetActive(true);
        }
    }
}