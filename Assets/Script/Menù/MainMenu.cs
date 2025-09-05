using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button easyButton;
    public Button normalButton;
    public Button hardButton;

    [Header("Scene Settings")]
    public string gameScene = "Labirinto";

    void Start()
    {
        SetUpButtons();
    }

    private void SetUpButtons()
    {
        if (easyButton != null)
            easyButton.onClick.AddListener(() => StartGame(DifficultyLevel.Easy));

        if (normalButton != null)
            normalButton.onClick.AddListener(() => StartGame(DifficultyLevel.Normal));

        if (hardButton != null)
            hardButton.onClick.AddListener(() => StartGame(DifficultyLevel.Hard));
    }

    public void StartGame(DifficultyLevel difficulty)
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.SetDifficulty(difficulty);
        }
        else
        {
            Debug.LogWarning("DifficultyManager non trovato! Creandone uno temporaneo...");
            GameObject tempManager = new GameObject("TempDifficultyManager");
            DifficultyManager manager = tempManager.AddComponent<DifficultyManager>();
            manager.SetDifficulty(difficulty);
        }

        SceneManager.LoadScene(gameScene);
    }

    // Metodi alternativi richiamabili direttamente dall'Inspector
    public void StartEasyGame() => StartGame(DifficultyLevel.Easy);
    public void StartNormalGame() => StartGame(DifficultyLevel.Normal);
    public void StartHardGame() => StartGame(DifficultyLevel.Hard);
}
