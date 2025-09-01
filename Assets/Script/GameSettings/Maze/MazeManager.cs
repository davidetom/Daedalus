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
    public Color warningPanelOriginalColor;
    public TextMeshProUGUI warningText;
    public Button returnToHubButton;
    public Button stayInMazeButton;
    public GameObject dawnWarningPanel;
    public TextMeshProUGUI dawnWarningText;
    public TextMeshProUGUI toHubWarningText;
    public TextMeshProUGUI mazeOpenWarningText;
    public TextMeshProUGUI mazeClosedWarningText;
    public TextMeshProUGUI goodLuckText;


    [Header("Zone")]
    public Collider2D hubZone;

    [Header("Gestione Tilemap Labirinti")]
    public GameObject labirintoObject;
    public int maxMazeCount = 4;
    public float mazeChangeDelay = 3f;
    public Vector2 hubSpawnPosition = new Vector2(155.5f, 151.7f);

    [Header("Input Management")]
    public PlayerController playerController;
    public GameObject[] uiElementsToDisable;

    // Stato interno
    private bool playerInHub = true;
    private bool mazeDoorsOpen = true;
    private bool hasChosenToStay = false;
    private int currentMazeNumber = 1;
    private bool isChangingMaze = false;
    private bool inputsDisabled = false;
    private bool originalCanAttackWhileMoving;
    private bool sunsetChoiceMade = false;

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

        if (warningPanel != null)
            warningPanelOriginalColor = warningPanel.GetComponent<Image>().color;

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
        
        // NUOVO: Aggiorna stato player - durante il giorno può aprire porte
        UpdatePlayerNightTimeState(false);
        
        // NON aprire le porte qui - saranno aperte dopo il cambio labirinto
        
        if (enemySpawner != null)
            enemySpawner.ClearAllEnemies();
        
        // NON chiamare HideAllWarnings() qui - il dawn panel deve rimanere attivo
        // Nascondi solo il warning panel del tramonto/notte se attivo
        if (warningPanel != null)
            warningPanel.SetActive(false);
        
        EnablePlayerInputsAndUI();
        hasChosenToStay = false;
        isChangingMaze = false;
    }

    void OnSunsetStart()
    {
        Debug.Log("Inizia il TRAMONTO");
        
        // NUOVO: Aggiorna stato player - durante il tramonto può ancora aprire porte
        UpdatePlayerNightTimeState(false);
        
        // NUOVO: Chiudi le porte immediatamente durante il tramonto, sempre
        CloseMazeDoorsImmediately();
        
        if (!playerInHub)
        {
            sunsetChoiceMade = false; // Reset del flag
            hasChosenToStay = false;  // Reset del flag
            ShowSunsetWarning();
            DisablePlayerInputsAndUI();
        }
        else
        {
            // Se il player è nell'hub, imposta i flag appropriati
            sunsetChoiceMade = true; // Il player è già nell'hub, scelta "fatta"
            hasChosenToStay = false; // Non è rimasto nel labirinto
        }
    }

    void OnNightStart()
    {
        Debug.Log("Inizia la NOTTE");
        
        // NUOVO: Aggiorna stato player - durante la notte può attaccare
        UpdatePlayerNightTimeState(true);
        
        if (!playerInHub && !sunsetChoiceMade)
        {
            Debug.Log("Nessuna scelta fatta durante il tramonto - player rimane nel labirinto");
            hasChosenToStay = true;
            sunsetChoiceMade = true; // Importante: imposta anche questo flag
        }
        
        StartCoroutine(HandleSunsetToNightTransition());
        
        if (!playerInHub)
        {
            if (enemySpawner != null)
                enemySpawner.SpawnNightEnemies();
        }
    }

    void OnDawnStart()
    {
        Debug.Log("Inizia l'ALBA");
        
        // NUOVO: Aggiorna stato player - durante l'alba può attaccare
        UpdatePlayerNightTimeState(true);
        
        if (isChangingMaze) return;
        isChangingMaze = true;
        
        ShowDawnWarnings();
        StartCoroutine(HandleMazeChangeWithWarnings());
        
        if (enemySpawner != null)
            enemySpawner.ClearAllEnemies();
    }

    IEnumerator HandleSunsetToNightTransition()
    {
        // Se il player non ha ancora fatto una scelta, nascondi i testi del tramonto
        if (!sunsetChoiceMade)
        {
            HideSunsetWarningTexts();
            MakeWarningPanelTransparent();
            EnablePlayerInputsAndUI();
        }
        
        // Attende un momento per evitare conflitti
        yield return new WaitForSeconds(0.3f);
        
        // Se il player è nell'hub, attiva il warning panel e assicurati che i testi del tramonto siano nascosti
        if (playerInHub && warningPanel != null && !warningPanel.activeInHierarchy)
        {
            warningPanel.SetActive(true);
            MakeWarningPanelTransparent(); // Inizia trasparente
            HideSunsetWarningTexts(); // Assicurati che i testi del tramonto siano nascosti
        }
        
        StartCoroutine(ShowMazeClosedWarnings());
    }

    IEnumerator HandleMazeChangeWithWarnings()
    {
        bool shouldTransportPlayer = !playerInHub;

        // Dopo 3 secondi: nasconde SOLO il warning di trasporto se presente
        yield return new WaitForSeconds(3f);

        // Trasporta il player se non è nell'hub
        if (shouldTransportPlayer && player != null)
        {
            Debug.Log("Trasportando il player nell'hub dopo 3 secondi");
            player.position = hubSpawnPosition;
            UpdatePlayerPosition();
            
            // Nasconde SOLO il warning di trasporto dopo il trasporto
            if (toHubWarningText != null)
            {
                toHubWarningText.gameObject.SetActive(false);
                Debug.Log("Warning di trasporto nascosto dopo trasporto");
            }
        }

        // Il dawn warning text rimane attivo per altri 2 secondi
        yield return new WaitForSeconds(2f);
        
        // Ora nasconde anche il dawn warning text
        if (dawnWarningText != null)
        {
            dawnWarningText.gameObject.SetActive(false);
            Debug.Log("Dawn warning text nascosto dopo 2 secondi aggiuntivi");
        }

        // Attende il resto del delay per il cambio labirinto (se necessario)
        float remainingDelay = mazeChangeDelay - 5f; // 3 + 2 secondi già trascorsi
        if (remainingDelay > 0)
        {
            yield return new WaitForSeconds(remainingDelay);
        }

        // PRIMA: Cambia il labirinto
        ChangeMazeTilemap();

        // Attesa di 1 secondo prima di aprire le porte
        yield return new WaitForSeconds(1f);

        // SECONDA: Apri le porte
        OpenMazeDoors();

        // Attesa di 1.5 secondi prima del warning
        yield return new WaitForSeconds(1f);

        // TERZA: Mostra il maze open warning
        yield return StartCoroutine(ShowMazeOpenWarningSequence());
    }

    IEnumerator ShowMazeOpenWarningSequence()
    {
        // Mostra il maze open warning per 3 secondi
        if (mazeOpenWarningText != null)
        {
            mazeOpenWarningText.gameObject.SetActive(true);
            Debug.Log("Mostro warning: labirinto aperto dopo cambio maze");
            
            yield return new WaitForSeconds(3f);
            
            mazeOpenWarningText.gameObject.SetActive(false);
            Debug.Log("Warning labirinto aperto nascosto");
        }
        
        // INFINE: Disattiva completamente il dawn warning panel
        if (dawnWarningPanel != null)
        {
            dawnWarningPanel.SetActive(false);
            Debug.Log("Dawn warning panel completamente disattivato");
        }
    }

    IEnumerator WaitForDoorsAndShowOpenWarning()
    {
        // Attende che le porte si aprano
        yield return new WaitForSeconds(0.5f);
        
        // Mostra il maze open warning per 3 secondi
        if (mazeOpenWarningText != null)
        {
            mazeOpenWarningText.gameObject.SetActive(true);
            Debug.Log("Mostro warning: labirinto aperto dopo cambio maze");
            
            yield return new WaitForSeconds(3f);
            
            mazeOpenWarningText.gameObject.SetActive(false);
            Debug.Log("Warning labirinto aperto nascosto");
        }
        
        // INFINE: Disattiva completamente il dawn warning panel
        if (dawnWarningPanel != null)
        {
            dawnWarningPanel.SetActive(false);
            Debug.Log("Dawn warning panel completamente disattivato");
        }
    }

    void ShowDawnWarnings()
    {
        if (dawnWarningPanel != null)
        {
            dawnWarningPanel.SetActive(true);

            // Nascondi il maze open warning che verrà mostrato dopo
            if (mazeOpenWarningText != null)
                mazeOpenWarningText.gameObject.SetActive(false);
            
            // Mostra sempre il warning dell'alba
            if (dawnWarningText != null)
            {
                dawnWarningText.gameObject.SetActive(true);
            }

            // Mostra il warning di trasporto solo se il player è nel labirinto
            if (toHubWarningText != null)
            {
                bool showTransportWarning = !playerInHub;
                toHubWarningText.gameObject.SetActive(showTransportWarning);

                if (showTransportWarning)
                {
                    Debug.Log("Player nel labirinto: mostro warning di trasporto all'hub");
                }
            }
        }
    }

    void DisablePlayerInputsAndUI()
    {
        if (inputsDisabled) return; // Evita doppia disabilitazione

        inputsDisabled = true;

        // Disabilita movimento del player
        if (playerController != null)
        {
            // Salva le impostazioni originali
            originalCanAttackWhileMoving = playerController.canAttackWhileMoving;

            // Impedisci qualsiasi azione del player
            playerController.canAttackWhileMoving = false;
            playerController.canAttack = false;

            // Ferma movimento mobile se attivo
            playerController.StopMovimento();

            Debug.Log("Input del player disabilitati durante il tramonto");
        }

        // Disabilita elementi UI specificati
        foreach (GameObject uiElement in uiElementsToDisable)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(false);
            }
        }

        Debug.Log($"Disabilitati {uiElementsToDisable.Length} elementi UI");
    }

    void EnablePlayerInputsAndUI()
    {
        if (!inputsDisabled) return; // Non era disabilitato

        inputsDisabled = false;

        // Riabilita input del player
        if (playerController != null)
        {
            // Ripristina le impostazioni originali
            playerController.canAttackWhileMoving = originalCanAttackWhileMoving;
            playerController.canAttack = true;

            Debug.Log("Input del player riabilitati");
        }

        // Riabilita elementi UI
        foreach (GameObject uiElement in uiElementsToDisable)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(true);
            }
        }

        Debug.Log($"Riabilitati {uiElementsToDisable.Length} elementi UI");
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
            
            // Assicurati che il pannello sia opaco
            Image panelImage = warningPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = warningPanelOriginalColor;
            }
            
            // Nascondi i warning figli che non servono ora
            if (mazeClosedWarningText != null)
                mazeClosedWarningText.gameObject.SetActive(false);
            if (goodLuckText != null)
                goodLuckText.gameObject.SetActive(false);
            
            // Mostra i testi e bottoni del tramonto
            if (warningText != null)
                warningText.gameObject.SetActive(true);
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

    void MakeWarningPanelTransparent()
    {
        if (warningPanel != null)
        {
            Image panelImage = warningPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                Color transparentColor = warningPanelOriginalColor;
                transparentColor.a = 0f; // Rende completamente trasparente
                panelImage.color = transparentColor;
                Debug.Log("Warning panel reso trasparente");
            }
        }
    }

    void RestoreWarningPanelOpacity()
    {
        if (warningPanel != null)
        {
            Image panelImage = warningPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = warningPanelOriginalColor;
                Debug.Log("Opacità warning panel ripristinata");
            }
        }
    }

    void ReturnToHub()
    {
        Debug.Log("Player ha scelto di tornare all'hub");
        sunsetChoiceMade = true;
        hasChosenToStay = false;

        if (player != null)
        {
            player.position = hubSpawnPosition;
            UpdatePlayerPosition();
        }

        // Nascondi i testi del tramonto
        HideSunsetWarningTexts();
        
        // Rendi il warning panel trasparente
        MakeWarningPanelTransparent();
        
        EnablePlayerInputsAndUI();
        
        // Il warning panel rimane attivo per mostrare i messaggi della notte
    }

    void StayInMaze()
    {
        Debug.Log("Player ha scelto di affrontare la notte");
        sunsetChoiceMade = true;
        hasChosenToStay = true;
        
        // Nascondi immediatamente i testi del tramonto
        HideSunsetWarningTexts();
        
        // Rendi il warning panel trasparente
        MakeWarningPanelTransparent();
        
        // Riabilita gli input del player
        EnablePlayerInputsAndUI();
        
        // Il warning panel rimane attivo per mostrare i messaggi della notte
        // che verranno gestiti da HandleSunsetToNightTransition
    }

    void OpenMazeDoors()
    {
        mazeDoorsOpen = true;
        foreach (GameObject doorObj in mazeDoors)
        {
            if (doorObj != null)
            {
                DoorController door = doorObj.GetComponent<DoorController>();
                if (door != null && door.IsInnerDoor())
                {
                    door.ForceOpen();
                }
            }
        }
        Debug.Log("Porte interne del labirinto aperte");
    }

    void CloseMazeDoorsImmediately()
    {
        mazeDoorsOpen = false;
        foreach (GameObject doorObj in mazeDoors)
        {
            if (doorObj != null)
            {
                DoorController door = doorObj.GetComponent<DoorController>();
                if (door != null && door.IsInnerDoor())
                {
                    door.CloseDoor();
                }
            }
        }
        Debug.Log("Porte interne del labirinto chiuse");
    }

    IEnumerator ShowMazeClosedWarnings()
    {
        // Assicurati che tutti i testi del tramonto siano nascosti prima di mostrare quelli della notte
        HideSunsetWarningTexts();
        
        // NON ripristinare l'opacità - il pannello deve rimanere trasparente
        // I testi sono visibili anche con pannello trasparente
        
        // Mostra sempre il warning di chiusura
        if (mazeClosedWarningText != null)
        {
            mazeClosedWarningText.gameObject.SetActive(true);
            Debug.Log("Mostro warning: labirinto chiuso");
        }
        
        // Mostra good luck SOLO se il player è rimasto nel labirinto (hasChosenToStay = true)
        if (goodLuckText != null && hasChosenToStay)
        {
            goodLuckText.gameObject.SetActive(true);
            Debug.Log("Mostro good luck text: player rimasto nel labirinto");
        }
        else if (goodLuckText != null)
        {
            // Assicurati che good luck sia nascosto se il player non è rimasto
            goodLuckText.gameObject.SetActive(false);
            Debug.Log("Good luck text nascosto: player non rimasto nel labirinto");
        }
        
        yield return new WaitForSeconds(2f);
        
        // Nasconde entrambi i warning dopo 2 secondi
        if (mazeClosedWarningText != null)
        {
            mazeClosedWarningText.gameObject.SetActive(false);
            Debug.Log("Warning labirinto chiuso nascosto");
        }
        
        if (goodLuckText != null)
        {
            goodLuckText.gameObject.SetActive(false);
            Debug.Log("Good luck text nascosto");
        }
        
        // INFINE: Disattiva completamente il warning panel
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
            Debug.Log("Warning panel completamente disattivato dopo maze closed warnings");
        }
    }

    void HideSunsetWarningTexts()
    {
        if (warningPanel != null)
        {
            // Nasconde solo i testi e bottoni principali
            if (warningText != null)
                warningText.gameObject.SetActive(false);
            if (returnToHubButton != null)
                returnToHubButton.gameObject.SetActive(false);
            if (stayInMazeButton != null)
                stayInMazeButton.gameObject.SetActive(false);
        }
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
    
     void UpdatePlayerNightTimeState(bool isNightTime)
    {
        if (playerController != null)
        {
            playerController.isNightTime = isNightTime;
            Debug.Log($"Player isNightTime aggiornato a: {isNightTime}");
        }
    }
}