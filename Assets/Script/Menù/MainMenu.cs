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

    void Awake()
    {
        Application.targetFrameRate = 60;
    }
    
    void Start()
    {   
        if (AudioManager.Instance != null)
    {
        AudioManager.Instance.PlayMusic(AudioManager.Instance.mainMenuMusic);
    }
        SetUpButtons();
    }

    private void SetUpButtons()
    {
        if (easyButton != null)
            easyButton.onClick.AddListener(() =>
            {
                // Suono del pulsante
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayButtonClick();

                StartGame(DifficultyLevel.Easy);
            });

        if (normalButton != null)
            normalButton.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayButtonClick();

                StartGame(DifficultyLevel.Normal);
            });

        if (hardButton != null)
            hardButton.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayButtonClick();

                StartGame(DifficultyLevel.Hard);
            });
    }

    public void StartGame(DifficultyLevel difficulty)
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.SetDifficulty(difficulty);
        }
        else
        {
            //Debug.LogWarning("DifficultyManager non trovato! Creandone uno temporaneo...");
            GameObject tempManager = new GameObject("TempDifficultyManager");
            DifficultyManager manager = tempManager.AddComponent<DifficultyManager>();
            manager.SetDifficulty(difficulty);
        }

        // Ferma la musica del menu
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }
        SaveSystem.NewGame();
    }

    // Metodi alternativi richiamabili direttamente dall'Inspector
    public void StartEasyGame() => StartGame(DifficultyLevel.Easy);
    public void StartNormalGame() => StartGame(DifficultyLevel.Normal);
    public void StartHardGame() => StartGame(DifficultyLevel.Hard);
}