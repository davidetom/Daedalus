using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MazeManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public DayNightCycleManager dayNightManager;
    public Transform player;
    public Transform hubCenter;
    public GameObject[] mazeDoors;
    public EnemySpawner enemySpawner; // Gestore spawn nemici
    
    [Header("UI")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningText;
    public Button returnToHubButton;
    public Button stayInMazeButton;
    public GameObject dawnWarningPanel;
    public TextMeshProUGUI dawnWarningText;
    
    [Header("Zone")]
    public Collider2D hubZone; // Per giochi 2D
    // Se usi 3D, sostituisci con: public Collider hubZone;
    
    [Header("Gestione Scene Labirinti")]
    public int maxMazeCount = 4; // Numero massimo di labirinti
    public float mazeChangeDelay = 3f; // Tempo di attesa prima del cambio scena
    public Vector2 hubSpawnPosition = new Vector2(155.5f, 151.7f); // Posizione nell'hub
    
    // Stato interno
    private bool playerInHub = true;
    private bool mazeDoorsOpen = true;
    private bool hasChosenToStay = false;
    private int currentMazeNumber = 1; // Labirinto attuale
    private bool isChangingMaze = false; // Impedisce cambiamenti multipli
    
    // Proprietà pubbliche
    public bool IsPlayerInHub => playerInHub;
    public bool AreMazeDoorsOpen => mazeDoorsOpen;
    public int CurrentMazeNumber => currentMazeNumber;
    
    void Start()
    {
        // Trova il player se non assegnato
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        // Trova l'enemy spawner
        if (enemySpawner == null)
            enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
        
        // Collega gli eventi del ciclo giorno/notte
        if (dayNightManager != null)
        {
            dayNightManager.events.OnDayStart.AddListener(OnDayStart);
            dayNightManager.events.OnSunsetStart.AddListener(OnSunsetStart);
            dayNightManager.events.OnNightStart.AddListener(OnNightStart);
            dayNightManager.events.OnDawnStart.AddListener(OnDawnStart);
        }
        
        // Configura i pulsanti
        if (returnToHubButton != null)
            returnToHubButton.onClick.AddListener(ReturnToHub);
        if (stayInMazeButton != null)
            stayInMazeButton.onClick.AddListener(StayInMaze);
        
        // Determina il labirinto attuale dalla scena
        DetermineCurrentMaze();
        
        // Inizializza lo stato (partendo dal giorno)
        UpdatePlayerPosition();
        OpenMazeDoors();
        HideAllWarnings();
    }
    
    void Update()
    {
        UpdatePlayerPosition();
    }
    
    void DetermineCurrentMaze()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // Estrae il numero dal nome della scena (es: "Labirinto_01" -> 1)
        if (currentSceneName.Contains("Labirinto_"))
        {
            string numberPart = currentSceneName.Substring(currentSceneName.LastIndexOf('_') + 1);
            if (int.TryParse(numberPart, out int mazeNum))
            {
                currentMazeNumber = mazeNum;
            }
        }
        
        Debug.Log($"Labirinto attuale determinato: {currentMazeNumber}");
    }
    
    void UpdatePlayerPosition()
    {
        if (player == null) return;
        
        bool wasInHub = playerInHub;
        
        // Controlla se il player è nell'hub
        if (hubZone != null)
        {
            // Per Collider2D
            if (hubZone is Collider2D)
                playerInHub = ((Collider2D)hubZone).bounds.Contains(player.position);
        }
        
        // Log cambio posizione
        if (wasInHub != playerInHub)
        {
            Debug.Log($"Player ora è {(playerInHub ? "nell'hub" : "nel labirinto")}");
        }
    }
    
    void OnDayStart()
    {
        Debug.Log("🌅 Inizia il GIORNO");
        OpenMazeDoors();
        
        if (enemySpawner != null)
            enemySpawner.ClearAllEnemies();
            
        HideAllWarnings();
        hasChosenToStay = false;
        isChangingMaze = false; // Reset del flag
    }
    
    void OnSunsetStart()
    {
        Debug.Log("🌇 Inizia il TRAMONTO");
        
        // Mostra l'avviso solo se il player è nel labirinto
        if (!playerInHub)
        {
            ShowSunsetWarning();
        }
    }
    
    void OnNightStart()
    {
        Debug.Log("🌙 Inizia la NOTTE");
        HideAllWarnings();
        
        if (playerInHub && !hasChosenToStay)
        {
            // Player è nell'hub, chiudi le porte del labirinto
            CloseMazeDoors();
        }
        else
        {
            // Player ha scelto di rimanere, spawna nemici
            if (enemySpawner != null)
                enemySpawner.SpawnNightEnemies();
        }
    }
    
    void OnDawnStart()
    {
        Debug.Log("🌄 Inizia l'ALBA");
        
        // Impedisce cambiamenti multipli
        if (isChangingMaze) return;
        isChangingMaze = true;
        
        // Mostra sempre l'avviso del cambio labirinto
        ShowMazeChangeWarning();
        
        // Avvia il processo di cambio labirinto
        StartCoroutine(HandleMazeChange());
        
        // Rimuovi tutti i nemici
        if (enemySpawner != null)
            enemySpawner.ClearAllEnemies();
    }
    
    IEnumerator HandleMazeChange()
    {
        // Se il player è nel labirinto, trasportalo nell'hub prima del cambio
        if (!playerInHub && player != null)
        {
            Debug.Log("Trasportando il player nell'hub prima del cambio labirinto");
            player.position = hubSpawnPosition;
            UpdatePlayerPosition();
        }
        
        // Aspetta il tempo configurato
        yield return new WaitForSeconds(mazeChangeDelay);
        
        // Nasconde l'avviso
        HideAllWarnings();
        
        // Cambia il labirinto
        ChangeMazeScene();
    }
    
    void ChangeMazeScene()
    {
        // Calcola il prossimo labirinto
        int nextMazeNumber = currentMazeNumber + 1;
        if (nextMazeNumber > maxMazeCount)
        {
            nextMazeNumber = 1; // Torna al primo
        }
        
        string nextSceneName = $"Labirinto_{nextMazeNumber:D2}";
        
        Debug.Log($"🔄 Cambiando dal labirinto {currentMazeNumber} al labirinto {nextMazeNumber}");
        
        // Salva la posizione del player per il trasferimento
        PlayerPrefs.SetFloat("PlayerSpawnX", hubSpawnPosition.x);
        PlayerPrefs.SetFloat("PlayerSpawnY", hubSpawnPosition.y);
        PlayerPrefs.SetInt("FromMazeChange", 1); // Flag per indicare che viene da un cambio labirinto
        PlayerPrefs.Save();
        
        // Carica la nuova scena
        SceneManager.LoadScene(nextSceneName);
    }
    
    void ShowSunsetWarning()
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(true);
            if (warningText != null)
                warningText.text = "⚠️ ATTENZIONE ⚠️\nIl labirinto sta per chiudersi!\nTorna all'hub o affronta la notte!";
            
            // Mostra i pulsanti di scelta
            if (returnToHubButton != null)
                returnToHubButton.gameObject.SetActive(true);
            if (stayInMazeButton != null)
                stayInMazeButton.gameObject.SetActive(true);
        }
    }
    
    void ShowMazeChangeWarning()
    {
        if (dawnWarningPanel != null)
        {
            dawnWarningPanel.SetActive(true);
            if (dawnWarningText != null)
            {
                int nextMaze = currentMazeNumber + 1;
                if (nextMaze > maxMazeCount) nextMaze = 1;
                
                dawnWarningText.text = $"🌅 L'alba sta arrivando!\n🔄 Il labirinto sta cambiando...\nCaricamento Labirinto {nextMaze:D2}";
            }
        }
    }
    
    void ShowDawnWarning()
    {
        if (dawnWarningPanel != null)
        {
            dawnWarningPanel.SetActive(true);
            if (dawnWarningText != null)
                dawnWarningText.text = "🌅 L'alba sta arrivando!\nLa notte sta per finire!";
            
            // Nasconde automaticamente dopo 3 secondi
            StartCoroutine(HideWarningAfterDelay(dawnWarningPanel, 3f));
        }
    }
    
    void HideAllWarnings()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);
        if (dawnWarningPanel != null)
            dawnWarningPanel.SetActive(false);
    }
    
    IEnumerator HideWarningAfterDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (panel != null)
            panel.SetActive(false);
    }
    
    void ReturnToHub()
    {
        Debug.Log("Player ha scelto di tornare all'hub");
        hasChosenToStay = false;
        
        // Teletrasporta il player all'hub
        if (player != null)
        {
            player.position = hubSpawnPosition;
        }
        
        HideAllWarnings();
    }
    
    void StayInMaze()
    {
        Debug.Log("Player ha scelto di affrontare la notte");
        hasChosenToStay = true;
        HideAllWarnings();
    }
    
    void OpenMazeDoors()
    {
        mazeDoorsOpen = true;
        foreach (GameObject door in mazeDoors)
        {
            if (door != null)
            {
                door.SetActive(false); // Disattiva le porte
            }
        }
        Debug.Log("🚪 Porte del labirinto aperte");
    }
    
    void CloseMazeDoors()
    {
        mazeDoorsOpen = false;
        foreach (GameObject door in mazeDoors)
        {
            if (door != null)
            {
                door.SetActive(true); // Attiva le porte
            }
        }
        Debug.Log("🔒 Porte del labirinto chiuse");
    }
    
    // Metodi pubblici per controllo esterno
    public void ForcePlayerToHub()
    {
        if (player != null)
        {
            player.position = hubSpawnPosition;
            UpdatePlayerPosition();
        }
    }
    
    // Metodo chiamabile dal letto per passare direttamente al giorno
    public void SleepToNextDay()
    {
        Debug.Log("💤 Player ha dormito, saltando la notte e passando direttamente al giorno");
        
        // Avvia la sequenza di sonno completa
        StartCoroutine(SleepSequence());
    }
    
    IEnumerator SleepSequence()
    {
        // 1. Salta direttamente all'alba nel ciclo giorno/notte
        if (dayNightManager != null)
        {
            dayNightManager.ForceToDawn();
        }
        
        // 2. Mostra l'avviso del cambio labirinto
        ShowMazeChangeWarning();
        
        // 3. Aspetta il tempo configurato per l'avviso
        yield return new WaitForSeconds(mazeChangeDelay);
        
        // 4. Nasconde l'avviso
        HideAllWarnings();
        
        // 5. Salva che il player sta dormendo per gestire il post-cambio scena
        PlayerPrefs.SetInt("PlayerSlept", 1);
        PlayerPrefs.Save();
        
        // 6. Cambia il labirinto
        ChangeMazeScene();
        
        // Nota: il passaggio al giorno verrà gestito dopo il caricamento della scena
    }
    
    public string GetStatusInfo()
    {
        string enemyInfo = enemySpawner != null ? $"Nemici attivi: {enemySpawner.GetActiveEnemyCount()}" : "Enemy Spawner non trovato";
        return $"Player in Hub: {playerInHub} | Porte aperte: {mazeDoorsOpen} | {enemyInfo} | Ha scelto di rimanere: {hasChosenToStay} | Labirinto: {currentMazeNumber}";
    }
    
    // Metodo per gestire il respawn del player dopo il cambio scena
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Se viene da un cambio labirinto, posiziona il player nell'hub
        if (PlayerPrefs.GetInt("FromMazeChange", 0) == 1)
        {
            StartCoroutine(PositionPlayerAfterSceneLoad());
            PlayerPrefs.DeleteKey("FromMazeChange");
        }
    }
    
    IEnumerator PositionPlayerAfterSceneLoad()
    {
        yield return new WaitForEndOfFrame(); // Aspetta che la scena sia completamente caricata
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        if (player != null)
        {
            Vector2 spawnPos = new Vector2(
                PlayerPrefs.GetFloat("PlayerSpawnX", hubSpawnPosition.x),
                PlayerPrefs.GetFloat("PlayerSpawnY", hubSpawnPosition.y)
            );
            
            player.position = spawnPos;
            Debug.Log($"Player posizionato nell'hub dopo il cambio labirinto: {spawnPos}");
        }
        
        // Se il player ha dormito, completa la sequenza di sonno
        bool playerSlept = PlayerPrefs.GetInt("PlayerSlept", 0) == 1;
        if (playerSlept)
        {
            Debug.Log("💤 Completando la sequenza di sonno...");
            
            // Aspetta un frame per assicurarsi che tutto sia inizializzato
            yield return new WaitForEndOfFrame();
            
            // Passa direttamente al giorno saltando il resto dell'alba
            if (dayNightManager != null)
            {
                dayNightManager.ForceToDay(); // Salta direttamente al giorno
            }
            
            // Pulisci il flag
            PlayerPrefs.DeleteKey("PlayerSlept");
        }
        
        PlayerPrefs.DeleteKey("PlayerSpawnX");
        PlayerPrefs.DeleteKey("PlayerSpawnY");
    }
}