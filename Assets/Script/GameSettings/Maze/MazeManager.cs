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
    public SpawnPointGenerator spawnPointGenerator;
    public OuterHubController hubController;

    [Header("UI")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningText;
    public Button returnToHubButton;
    public Button stayInMazeButton;
    public GameObject dawnWarningPanel;
    public TextMeshProUGUI dawnWarningText;
    public TextMeshProUGUI toHubWarningText;
    public GameObject mazeOpenPanel;
    public TextMeshProUGUI mazeOpenWarningText;
    public GameObject mazeClosedPanel;
    public TextMeshProUGUI mazeClosedWarningText;
    public TextMeshProUGUI goodLuckText;


    [Header("Zone")]
    public Collider2D hubZoneOutside;
    public Collider2D hubZoneInside;


    [Header("Gestione Tilemap Labirinti")]
    public GameObject labirintoObject;
    public int maxMazeCount = 4;
    public float mazeChangeDelay = 3f;
    public Vector2 hubSpawnPosition = new Vector2(400.5f, 158.7f);

    [Header("Input Management")]
    public PlayerController playerController;
    public GameObject[] uiElementsToDisable;

    // Stato interno
    public bool playerInOuterHub = false;
    public bool playerInInnerHub = true;
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
    public bool IsPlayerInOuterHub => playerInOuterHub;
    public bool IsPlayerInInnerHub => playerInInnerHub;
    public bool AreMazeDoorsOpen => mazeDoorsOpen;
    public int CurrentMazeNumber => currentMazeNumber;


    #region INIZIALIZZAZIONE

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

        if (spawnPointGenerator == null)
            spawnPointGenerator = FindFirstObjectByType<SpawnPointGenerator>();

        if (hubController == null)
            hubController = Object.FindFirstObjectByType<OuterHubController>();

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

    void HideAllWarnings()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);
        if (dawnWarningPanel != null)
            dawnWarningPanel.SetActive(false);
        if (mazeOpenPanel != null)
            mazeOpenPanel.SetActive(false);
        if (mazeClosedPanel != null)
            mazeClosedPanel.SetActive(false);
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

    // # 1
    void LoadCurrentMaze()
    {
        Debug.Log($"=== CARICAMENTO LABIRINTO {currentMazeNumber} ===");

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
            Debug.Log($"Istanziando prefab: {prefabToLoad.name}");
            currentTilemapInstance = Instantiate(prefabToLoad, labirintoObject.transform);
            currentTilemapInstance.name = $"Tilemap_{currentMazeNumber:D2}_Instance";

            Debug.Log($"Caricata tilemap del labirinto {currentMazeNumber} ({prefabToLoad.name})");

            // Verifica che la tilemap sia stata creata correttamente
            Tilemap newTilemap = currentTilemapInstance.GetComponentInChildren<Tilemap>();
            if (newTilemap != null)
            {
                Debug.Log($"Tilemap trovata: {newTilemap.name}, Bounds: {newTilemap.cellBounds}");
            }
            else
            {
                Debug.LogError("Tilemap non trovata nell'istanza creata!");
            }

            UpdateMapManager();
        }
        else
        {
            Debug.LogError($"Prefab tilemap non valido per il labirinto {currentMazeNumber}");
            // ... resto del codice di fallback rimane uguale ...
        }
    }

    // # 2
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

    // # 3
    IEnumerator RegenerateMapAfterFrame()
    {
        // Attendi 2 frame per assicurarti che tutto sia inizializzato
        yield return null;
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

            // Attendi un altro frame per assicurarti che BFS sia completato
            yield return null;

            // NUOVO: Aggiorna SpawnPointGenerator dopo che MapManager e BFS sono stati ricalcolati
            if (spawnPointGenerator != null && mapManager.tilemap != null)
            {
                Debug.Log($"Avviando aggiornamento spawn points per tilemap: {mapManager.tilemap.name}");

                // Forza l'aggiornamento anche se la tilemap sembra uguale
                spawnPointGenerator.ForceRegenerateSpawns(mapManager.tilemap);

                Debug.Log("Spawn points aggiornati per la tilemap corrente");
            }
            else
            {
                Debug.LogWarning($"SpawnPointGenerator: {(spawnPointGenerator == null ? "NULL" : "OK")}, " +
                            $"MapManager.tilemap: {(mapManager.tilemap == null ? "NULL" : "OK")}");
            }
        }
    }

    #endregion


    #region UPDATE

    void Update()
    {
        UpdatePlayerPosition();
    }

    void UpdatePlayerPosition()
    {
        if (player == null) return;

        bool wasInOuterHub = playerInOuterHub;
        bool wasInInnerHub = playerInInnerHub;

        if (hubZoneOutside != null && hubZoneInside != null)
        {
            if (hubZoneOutside is Collider2D && hubZoneInside is Collider2D)
                playerInOuterHub = ((Collider2D)hubZoneOutside).bounds.Contains(player.position);
            playerInInnerHub = ((Collider2D)hubZoneInside).bounds.Contains(player.position);
        }

        if (!wasInOuterHub && playerInOuterHub)
        {
            Debug.Log("▶ Player è nell'Outer Hub");
        }
        else if (!wasInInnerHub && playerInInnerHub)
        {
            Debug.Log("▶ Player è nell'Inner Hub");
        }
        else if ((wasInOuterHub || wasInInnerHub) && !playerInOuterHub && !playerInInnerHub)
        {
            Debug.Log("▶ Player è entrato nel Labirinto");
        }
    }

    #endregion


    #region EVENTI DI INIZIO GIORNO

    void OnDayStart()
    {
        Debug.Log("Inizia il GIORNO");

        // Aggiorna stato player - durante il giorno può aprire porte
        UpdatePlayerNightTimeState(false);

        // Pulisci nemici
        if (enemySpawner != null)
            enemySpawner.ClearAllEnemies();

        // NUOVO: Controlla se stiamo venendo da un sonno
        bool comingFromSleep = !dayNightManager.IsInitialized() || dayNightManager.startFromDay;

        if (comingFromSleep)
        {
            Debug.Log("OnDayStart: Avvio normale del gioco o post-sonno");
            // Per il sonno, le porte sono gestite da HandleSleepReset()
            // Per l'avvio normale del gioco, apri le porte immediatamente (primo avvio)
            if (!dayNightManager.IsInitialized())
            {
                OpenMazeDoors();
            }
        }
        else
        {
            // Gestione differente per player nell'hub (ciclo normale)
            if (playerInOuterHub || playerInInnerHub)
            {
                ShowDawnWarningsForHubPlayer();
            }
        }

        // Nascondi solo il warning panel del tramonto/notte e il mazeClosed panel se attivi
        if (warningPanel != null)
            warningPanel.SetActive(false);
        if (mazeClosedPanel != null)
            mazeClosedPanel.SetActive(false);

        // Non abilitare input qui se stiamo venendo dal sonno (già fatto in HandleSleepReset)
        if (!comingFromSleep)
        {
            EnablePlayerInputsAndUI();
        }

        hasChosenToStay = false;
        isChangingMaze = false;
    }

    // Mostra dawn warning per player nell'hub
    void ShowDawnWarningsForHubPlayer()
    {
        if (dawnWarningPanel != null)
        {
            dawnWarningPanel.SetActive(true);

            // Mostra solo il maze open warning, non gli altri
            if (dawnWarningText != null)
                dawnWarningText.gameObject.SetActive(false);
            if (toHubWarningText != null)
                toHubWarningText.gameObject.SetActive(false);
            if (mazeOpenWarningText != null)
                mazeOpenWarningText.gameObject.SetActive(true);

            Debug.Log("Mostro maze open warning per player nell'hub");

            // Nascondi il warning dopo 3 secondi
            StartCoroutine(HideMazeOpenWarningForHubPlayer());
        }
    }

    // Nascondi il maze open warning per player nell'hub
    IEnumerator HideMazeOpenWarningForHubPlayer()
    {
        yield return new WaitForSeconds(3f);

        if (mazeOpenWarningText != null)
            mazeOpenWarningText.gameObject.SetActive(false);

        if (dawnWarningPanel != null)
            dawnWarningPanel.SetActive(false);
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

    #endregion


    #region EVENTI DI INIZIO TRAMONTO

    void OnSunsetStart()
    {
        Debug.Log("Inizia il TRAMONTO");

        // Aggiorna stato player - durante il tramonto può ancora aprire porte
        UpdatePlayerNightTimeState(false);

        // Chiudi le porte immediatamente durante il tramonto, sempre
        CloseMazeDoorsImmediately();

        // Gestione differente se il player è nell'hub
        if (playerInOuterHub || playerInInnerHub)
        {
            // Se il player è nell'hub, non mostrare warning e non disabilitare input
            Debug.Log("Player nell'hub - nessun warning al tramonto");
            sunsetChoiceMade = true; // Il player è nell'hub, scelta "fatta"
            hasChosenToStay = false; // Non è rimasto nel labirinto
        }
        else
        {
            // Comportamento originale se il player è nel labirinto
            sunsetChoiceMade = false; // Reset del flag
            hasChosenToStay = false;  // Reset del flag
            ShowSunsetWarning();
            DisablePlayerInputsAndUI();
        }
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

    void ShowSunsetWarning()
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(true);

            // Mostra i testi e bottoni del tramonto
            if (warningText != null)
                warningText.gameObject.SetActive(true);
            if (returnToHubButton != null)
                returnToHubButton.gameObject.SetActive(true);
            if (stayInMazeButton != null)
                stayInMazeButton.gameObject.SetActive(true);
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

    public void ReturnToHub()
    {
        Debug.Log("Player ha scelto di tornare all'hub");
        sunsetChoiceMade = true;
        hasChosenToStay = false;

        HideSunsetWarnings();

        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SafeTransportTo(hubSpawnPosition);
                hubController.UpdateStatusWithPlayerInHub();
            }
            UpdatePlayerPosition();
        }

        EnablePlayerInputsAndUI();
    }

    public void StayInMaze()
    {
        Debug.Log("Player ha scelto di affrontare la notte");
        sunsetChoiceMade = true;
        hasChosenToStay = true;

        HideSunsetWarnings();
        EnablePlayerInputsAndUI();
    }

    void HideSunsetWarnings()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);
        if (warningText != null)
            warningText.gameObject.SetActive(false);
        if (returnToHubButton != null)
            returnToHubButton.gameObject.SetActive(false);
        if (stayInMazeButton != null)
            stayInMazeButton.gameObject.SetActive(false);
    }

    #endregion


    #region EVENTI DI INIZIO NOTTE

    void OnNightStart()
    {
        Debug.Log("Inizia la NOTTE");

        // Aggiorna stato player - durante la notte può attaccare
        UpdatePlayerNightTimeState(true);

        if (!playerInOuterHub && !playerInInnerHub && !sunsetChoiceMade)
        {
            Debug.Log("Nessuna scelta fatta durante il tramonto - player rimane nel labirinto");
            hasChosenToStay = true;
            sunsetChoiceMade = true;

            HideSunsetWarnings();
            EnablePlayerInputsAndUI();
        }

        StartCoroutine(ShowMazeClosedWarnings());

        // Spawn nemici disabilitato se player nell'hub
        if (playerInOuterHub || playerInInnerHub)
        {
            Debug.Log("Player nell'hub - nessun spawn di nemici notturni");
        }
        else
        {
            if (enemySpawner != null)
                enemySpawner.SpawnNightEnemies();
        }
    }

    IEnumerator ShowMazeClosedWarnings()
    {
        yield return null;

        //HideSunsetWarningPanel();

        if (mazeClosedPanel != null)
            mazeClosedPanel.SetActive(true);

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
            goodLuckText.gameObject.SetActive(false);
            Debug.Log("Nascondo good luck text: player tornato nell'hub");
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

        if (mazeClosedPanel != null)
            mazeClosedPanel.SetActive(false);
    }

    #endregion


    #region EVENTI DI INZIO ALBA

    void OnDawnStart()
    {
        Debug.Log("Inizia l'ALBA");

        // Aggiorna stato player - durante l'alba può attaccare
        UpdatePlayerNightTimeState(true);

        if (isChangingMaze) return;
        isChangingMaze = true;

        if (!IsPlayerDead())
            StartCoroutine(HandleReturnToHubAndWarnings());

        if (enemySpawner != null)
            enemySpawner.ClearAllEnemies();

        StartCoroutine(HandleMazeChange());
    }

    IEnumerator HandleReturnToHubAndWarnings()
    {
        yield return StartCoroutine(ShowDawnWarnings());

        yield return null;

        if (player != null && !playerInOuterHub && !playerInInnerHub)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SafeTransportTo(hubSpawnPosition);
                hubController.UpdateStatusWithPlayerInHub();
            }
            UpdatePlayerPosition();
        }

        if (toHubWarningText != null)
            toHubWarningText.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        if (dawnWarningPanel != null)
        {
            dawnWarningPanel.SetActive(false);

            if (dawnWarningText != null)
                dawnWarningText.gameObject.SetActive(false);
        }
    }

    IEnumerator ShowDawnWarnings()
    {
        if (dawnWarningPanel != null)
        {
            dawnWarningPanel.SetActive(true);

            // Mostra sempre il warning dell'alba
            if (dawnWarningText != null)
                dawnWarningText.gameObject.SetActive(true);

            // Mostra il warning di trasporto solo se il player è nel labirinto
            if (toHubWarningText != null)
            {
                bool showTransportWarning = !playerInOuterHub && !playerInInnerHub;
                toHubWarningText.gameObject.SetActive(showTransportWarning);

                if (showTransportWarning)
                    Debug.Log("Player nel labirinto e vivo: mostro warning di trasporto all'hub");
            }
        }

        yield return new WaitForSeconds(1.5f);
    }

    // QUA INSERIRE LOGICA PER IMPOSTARE IL CAMBIO LABIRINTO A FALSE PER MODALITA' FACILE
    // Gestione specifica per il cambio maze quando il player è nell'hub esterno
    IEnumerator HandleMazeChange()
    {
        // Attende il delay normale per il cambio labirinto
        yield return new WaitForSeconds(mazeChangeDelay);

        // PRIMA: Cambia il labirinto e ATTENDI che sia completato
        Debug.Log("Iniziando cambio labirinto per player nell'hub...");
        ChangeMazeTilemap();

        // Attendi che il cambio labirinto sia completamente processato
        yield return StartCoroutine(WaitForMazeChangeCompletion());

        // SECONDA: Ora apri le porte (SOLO dopo che il labirinto è cambiato)
        Debug.Log("Cambio labirinto completato, aprendo le porte per player nell'hub...");
        OpenMazeDoors();

        Debug.Log("Cambio labirinto completato per player nell'hub esterno");

        StartCoroutine(ShowMazeOpenWarningSequence());
    }

    void ChangeMazeTilemap()
    {
        int nextMazeNumber = currentMazeNumber + 1;
        if (nextMazeNumber > maxMazeCount)
            nextMazeNumber = 1;

        Debug.Log($"Cambiando dal labirinto {currentMazeNumber} al labirinto {nextMazeNumber}");

        currentMazeNumber = nextMazeNumber;
        LoadCurrentMaze();
        isChangingMaze = false;

        Debug.Log($"Cambio labirinto completato. Ora attivo: Labirinto {currentMazeNumber}");
    }

    IEnumerator WaitForMazeChangeCompletion()
    {
        Debug.Log("Attendendo completamento cambio labirinto...");

        // Attendi che il MapManager e BFS siano processati
        yield return StartCoroutine(RegenerateMapAfterFrame());

        // Attendi un frame aggiuntivo per sicurezza
        yield return null;

        Debug.Log("Cambio labirinto completamente processato");
    }


    public void OpenMazeDoors()
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

    IEnumerator ShowMazeOpenWarningSequence()
    {
        // Mostra il maze open warning per 3 secondi
        if (mazeOpenPanel != null)
            mazeOpenPanel.SetActive(true);
            
        if (mazeOpenWarningText != null)
        {
            mazeOpenWarningText.gameObject.SetActive(true);
            Debug.Log("Mostro warning: labirinto aperto dopo cambio maze");

            yield return new WaitForSeconds(3f);

            mazeOpenWarningText.gameObject.SetActive(false);
            Debug.Log("Warning labirinto aperto nascosto");
        }

        // INFINE: Disattiva completamente il dawn warning panel
        if (mazeOpenPanel != null)
        {
            mazeOpenPanel.SetActive(false);
            Debug.Log("MazeOpen panel completamente disattivato");
        }
    }

    #endregion


    #region LOGICA DEL LETTO
    public void SleepToNextDay()
    {
        Debug.Log("Player ha dormito, saltando la notte e passando direttamente al giorno");
        StartCoroutine(SleepSequence());
    }

    IEnumerator SleepSequence()
    {
        // 1. Ferma il ciclo attuale del day/night manager
        if (dayNightManager != null)
        {
            dayNightManager.PauseSystem();
            Debug.Log("Sistema day/night messo in pausa per il sonno");
        }

        // 2. Nascondi tutti i warning attuali
        HideAllWarnings();

        // 3. Disabilita input del player durante il sonno
        DisablePlayerInputsAndUI();

        Debug.Log("Player inizia a dormire - cambio labirinto in corso...");

        // 4. PRIMA: Cambia il labirinto mentre il player "dorme"
        ChangeMazeTilemap();

        // 5. Attesa simulazione sonno (aspetto la fine effettiva del cambio labirinto)
        yield return StartCoroutine(WaitForMazeChangeCompletion()); // Tempo del "sonno"

        // 6. DOPO: Usa ResetToDay() che ora non cambierà più il labirinto
        if (dayNightManager != null)
        {
            Debug.Log("Risveglio - resettando il ciclo al giorno");
            dayNightManager.ResetToDay();
        }
        // 7. Riabilita input del player dopo il sonno
        EnablePlayerInputsAndUI();

        // 8. Apri le porte e mostro il mazeOpenWarning
        OpenMazeDoors();
        StartCoroutine(ShowMazeOpenWarningSequence());

        Debug.Log("Sequenza del sonno completata - nuovo giorno iniziato");
    }

    #endregion

    private bool IsPlayerDead()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        return player.IsDead();
    }

    public string GetStatusInfo()
    {
        string enemyInfo = enemySpawner != null ? $"Nemici attivi: {enemySpawner.GetActiveEnemyCount()}" : "Enemy Spawner non trovato";
        return $"Player in Hub: {playerInOuterHub || playerInInnerHub} | Porte aperte: {mazeDoorsOpen} | {enemyInfo} | Ha scelto di rimanere: {hasChosenToStay} | Labirinto: {currentMazeNumber}";
    }

    void UpdatePlayerNightTimeState(bool isNightTime)
    {
        if (playerController != null)
        {
            playerController.isNightTime = isNightTime;
            Debug.Log($"Player isNightTime aggiornato a: {isNightTime}");
        }
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
}