using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using TMPro;

public class MazeManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public DayNightCycleManager dayNightManager;
    public Transform player;
    public Transform hubCenter;
    public GameObject[] mazeDoors;
    public EnemySpawner enemySpawner;
    public MapManager mapManager;
    
    [Header("UI")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningText;
    public Button returnToHubButton;
    public Button stayInMazeButton;
    public GameObject dawnWarningPanel;
    public TextMeshProUGUI dawnWarningText;
    
    [Header("Zone")]
    public Collider2D hubZone;
    
    [Header("Gestione Tilemap Labirinti")]
    public GameObject labirintoObject;
    public int maxMazeCount = 4;
    public float mazeChangeDelay = 3f;
    public Vector2 hubSpawnPosition = new Vector2(155.5f, 151.7f);
    
    // Stato interno
    private bool playerInHub = true;
    private bool mazeDoorsOpen = true;
    private bool hasChosenToStay = false;
    private int currentMazeNumber = 1;
    private bool isChangingMaze = false;
    
    // Riferimenti tilemap
    private GameObject[] tilemapPrefabs;
    private GameObject currentTilemapInstance;
    
    // Proprieta pubbliche
    public bool IsPlayerInHub => playerInHub;
    public bool AreMazeDoorsOpen => mazeDoorsOpen;
    public int CurrentMazeNumber => currentMazeNumber;
    
    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        if (enemySpawner == null)
            enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
        
        if (mapManager == null)
            mapManager = Object.FindFirstObjectByType<MapManager>();
        
        if (labirintoObject == null)
            labirintoObject = GameObject.Find("Labirinto");
        
        LoadTilemapPrefabs();
        
        if (dayNightManager != null)
        {
            dayNightManager.events.OnDayStart.AddListener(OnDayStart);
            dayNightManager.events.OnSunsetStart.AddListener(OnSunsetStart);
            dayNightManager.events.OnNightStart.AddListener(OnNightStart);
            dayNightManager.events.OnDawnStart.AddListener(OnDawnStart);
        }
        
        if (returnToHubButton != null)
            returnToHubButton.onClick.AddListener(ReturnToHub);
        if (stayInMazeButton != null)
            stayInMazeButton.onClick.AddListener(StayInMaze);
        
        LoadCurrentMaze();
        UpdatePlayerPosition();
        OpenMazeDoors();
        HideAllWarnings();
    }
    
    void LoadTilemapPrefabs()
    {
        tilemapPrefabs = new GameObject[maxMazeCount];
        
        for (int i = 0; i < maxMazeCount; i++)
        {
            string[] possiblePaths = {
                $"Prefab/Tilemaps/Tilemap_{(i + 1):D2}",
                $"Tilemaps/Tilemap_{(i + 1):D2}",
                $"Tilemap_{(i + 1):D2}"
            };
            
            GameObject prefab = null;
            string usedPath = "";
            
            foreach (string path in possiblePaths)
            {
                prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    usedPath = path;
                    break;
                }
            }
            
            if (prefab != null)
            {
                tilemapPrefabs[i] = prefab;
                Debug.Log($"Caricato prefab tilemap {i + 1}: {usedPath}");
            }
            else
            {
                Debug.LogError($"Impossibile caricare il prefab tilemap {i + 1}. Percorsi tentati: {string.Join(", ", possiblePaths)}");
                Debug.LogWarning($"Assicurati che il prefab Tilemap_{(i + 1):D2} sia nella cartella Resources/Prefab/Tilemaps/ o Resources/Tilemaps/");
            }
        }
        
        Debug.Log("=== DEBUG: Contenuto cartella Resources ===");
        GameObject[] allResources = Resources.LoadAll<GameObject>("");
        foreach (GameObject resource in allResources)
        {
            if (resource.name.Contains("Tilemap"))
            {
                Debug.Log($"Trovato risorsa: {resource.name}");
            }
        }
    }
    
    void LoadCurrentMaze()
    {
        if (labirintoObject == null)
        {
            Debug.LogError("Oggetto Labirinto non trovato!");
            return;
        }
        
        if (tilemapPrefabs == null || tilemapPrefabs.Length == 0)
        {
            Debug.LogError("Array prefab tilemap non inizializzato!");
            return;
        }
        
        if (currentMazeNumber < 1 || currentMazeNumber > tilemapPrefabs.Length)
        {
            Debug.LogError($"Numero labirinto non valido: {currentMazeNumber}. Range valido: 1-{tilemapPrefabs.Length}");
            return;
        }
        
        if (currentTilemapInstance != null)
        {
            Debug.Log($"Rimuovendo tilemap esistente: {currentTilemapInstance.name}");
            DestroyImmediate(currentTilemapInstance);
            currentTilemapInstance = null;
        }
        
        GameObject prefabToLoad = tilemapPrefabs[currentMazeNumber - 1];
        
        if (prefabToLoad != null)
        {
            currentTilemapInstance = Instantiate(prefabToLoad, labirintoObject.transform);
            currentTilemapInstance.name = $"Tilemap_{currentMazeNumber:D2}_Instance";
            
            Debug.Log($"Caricata tilemap del labirinto {currentMazeNumber} ({prefabToLoad.name})");
            
            UpdateMapManager();
        }
        else
        {
            Debug.LogError($"Prefab tilemap non valido per il labirinto {currentMazeNumber}");
            
            for (int i = 0; i < tilemapPrefabs.Length; i++)
            {
                if (tilemapPrefabs[i] != null)
                {
                    Debug.LogWarning($"Fallback: caricando il labirinto {i + 1}");
                    currentMazeNumber = i + 1;
                    currentTilemapInstance = Instantiate(tilemapPrefabs[i], labirintoObject.transform);
                    currentTilemapInstance.name = $"Tilemap_{currentMazeNumber:D2}_Fallback";
                    
                    UpdateMapManager();
                    return;
                }
            }
            
            Debug.LogError("Nessun prefab tilemap valido trovato!");
        }
    }
    
    void UpdateMapManager()
    {
        if (mapManager == null)
        {
            Debug.LogWarning("MapManager non trovato, impossibile aggiornare la mappa!");
            return;
        }
        
        Tilemap newTilemap = null;
        if (currentTilemapInstance != null)
        {
            newTilemap = currentTilemapInstance.GetComponentInChildren<Tilemap>();
        }
        
        if (newTilemap == null)
        {
            Debug.LogError("Tilemap non trovata nella nuova istanza!");
            return;
        }
        
        Debug.Log("Aggiornando MapManager e PlayerController con la nuova tilemap...");
        
        mapManager.tilemap = newTilemap;
        
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.tilemap = newTilemap;
                Debug.Log("Riferimento tilemap aggiornato nel PlayerController");
            }
        }
        
        StartCoroutine(RegenerateMapAfterFrame());
    }
    
    IEnumerator RegenerateMapAfterFrame()
    {
        yield return null;
        
        if (mapManager != null)
        {
            // Chiamata diretta al metodo pubblico RecalculateMap()
            mapManager.RecalculateMap();
            
            Debug.Log($"MapManager aggiornato con successo per il labirinto {currentMazeNumber}");
            
            if (player != null)
            {
                PlayerController playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    // Chiamata diretta al metodo pubblico per ricalcolare le distanze
                    playerController.RecalculateDistances();
                    Debug.Log("Distanze BFS ricalcolate per la nuova tilemap");
                }
            }
        }
    }
    
    void Update()
    {
        UpdatePlayerPosition();
    }
    
    void UpdatePlayerPosition()
    {
        if (player == null) return;
        
        bool wasInHub = playerInHub;
        
        if (hubZone != null)
        {
            if (hubZone is Collider2D)
                playerInHub = ((Collider2D)hubZone).bounds.Contains(player.position);
        }
        
        if (wasInHub != playerInHub)
        {
            Debug.Log($"Player ora e {(playerInHub ? "nell'hub" : "nel labirinto")}");
        }
    }
    
    void OnDayStart()
    {
        Debug.Log("Inizia il GIORNO");
        OpenMazeDoors();
        
        if (enemySpawner != null)
            enemySpawner.ClearAllEnemies();
            
        HideAllWarnings();
        hasChosenToStay = false;
        isChangingMaze = false;
    }
    
    void OnSunsetStart()
    {
        Debug.Log("Inizia il TRAMONTO");
        
        if (!playerInHub)
        {
            ShowSunsetWarning();
        }
    }
    
    void OnNightStart()
    {
        Debug.Log("Inizia la NOTTE");
        HideAllWarnings();
        
        if (playerInHub && !hasChosenToStay)
        {
            CloseMazeDoors();
        }
        else
        {
            if (enemySpawner != null)
                enemySpawner.SpawnNightEnemies();
        }
    }
    
    void OnDawnStart()
    {
        Debug.Log("Inizia l'ALBA");
        
        if (isChangingMaze) return;
        isChangingMaze = true;
        
        ShowMazeChangeWarning();
        StartCoroutine(HandleMazeChange());
        
        if (enemySpawner != null)
            enemySpawner.ClearAllEnemies();
    }
    
    IEnumerator HandleMazeChange()
    {
        if (!playerInHub && player != null)
        {
            Debug.Log("Trasportando il player nell'hub prima del cambio labirinto");
            player.position = hubSpawnPosition;
            UpdatePlayerPosition();
        }
        
        yield return new WaitForSeconds(mazeChangeDelay);
        
        HideAllWarnings();
        ChangeMazeTilemap();
    }
    
    void ChangeMazeTilemap()
    {
        int nextMazeNumber = currentMazeNumber + 1;
        if (nextMazeNumber > maxMazeCount)
        {
            nextMazeNumber = 1;
        }
        
        Debug.Log($"Cambiando dal labirinto {currentMazeNumber} al labirinto {nextMazeNumber}");
        
        currentMazeNumber = nextMazeNumber;
        LoadCurrentMaze();
        isChangingMaze = false;
        
        Debug.Log($"Cambio labirinto completato. Ora attivo: Labirinto {currentMazeNumber}");
    }
    
    void ShowSunsetWarning()
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(true);

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
            }
        }
    }
    
    void ShowDawnWarning()
    {
        if (dawnWarningPanel != null)
        {
            dawnWarningPanel.SetActive(true);
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
                door.SetActive(false);
            }
        }
        Debug.Log("Porte del labirinto aperte");
    }
    
    void CloseMazeDoors()
    {
        mazeDoorsOpen = false;
        foreach (GameObject door in mazeDoors)
        {
            if (door != null)
            {
                door.SetActive(true);
            }
        }
        Debug.Log("Porte del labirinto chiuse");
    }
    
    public void ForcePlayerToHub()
    {
        if (player != null)
        {
            player.position = hubSpawnPosition;
            UpdatePlayerPosition();
        }
    }
    
    public void SleepToNextDay()
    {
        Debug.Log("Player ha dormito, saltando la notte e passando direttamente al giorno");
        StartCoroutine(SleepSequence());
    }
    
    IEnumerator SleepSequence()
    {
        if (dayNightManager != null)
        {
            dayNightManager.ForceToDawn();
        }
        
        ShowMazeChangeWarning();
        yield return new WaitForSeconds(mazeChangeDelay);
        HideAllWarnings();
        ChangeMazeTilemap();
        
        if (dayNightManager != null)
        {
            dayNightManager.ForceToDay();
        }
    }
    
    public string GetStatusInfo()
    {
        string enemyInfo = enemySpawner != null ? $"Nemici attivi: {enemySpawner.GetActiveEnemyCount()}" : "Enemy Spawner non trovato";
        return $"Player in Hub: {playerInHub} | Porte aperte: {mazeDoorsOpen} | {enemyInfo} | Ha scelto di rimanere: {hasChosenToStay} | Labirinto: {currentMazeNumber}";
    }
    
    [ContextMenu("Cambia Labirinto Successivo")]
    public void ChangeToNextMaze()
    {
        if (!isChangingMaze)
        {
            int nextMaze = currentMazeNumber + 1;
            if (nextMaze > maxMazeCount) nextMaze = 1;
            
            currentMazeNumber = nextMaze;
            LoadCurrentMaze();
            
            Debug.Log($"Cambiato manualmente al labirinto {currentMazeNumber}");
        }
    }
    
    public void LoadSpecificMaze(int mazeNumber)
    {
        if (mazeNumber >= 1 && mazeNumber <= maxMazeCount && !isChangingMaze)
        {
            currentMazeNumber = mazeNumber;
            LoadCurrentMaze();
            
            Debug.Log($"Caricato specificamente il labirinto {currentMazeNumber}");
        }
        else
        {
            Debug.LogWarning($"Numero labirinto non valido: {mazeNumber} o cambio gia in corso");
        }
    }
}