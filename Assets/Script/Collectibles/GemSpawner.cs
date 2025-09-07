using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GemSpawner : MonoBehaviour
{
    [Header("Debug")]
    public bool enableDebug = false;

    [Header("References")]
    public MapManager mapManager;
    public DayNightCycleManager dayNightManager;
    public PlayerController playerController;
    public InventoryManager inventoryManager;
    public GameObject gemCollectedPanel;
    public TextMeshProUGUI LightGemText;
    public TextMeshProUGUI NightGemText;
    public TextMeshProUGUI ZombieGemText;
    public TextMeshProUGUI BloodGemText;
    public TextMeshProUGUI FogGemText;

    [Header("Collision Detection")]
    public DynamicCoinGenerator coinGenerator; // Reference al coin generator

    [Header("Gem Prefabs")]
    public GameObject yellowGemPrefab; // Gemma del Sole
    public GameObject blueGemPrefab;   // Gemma della Notte
    public GameObject greenGemPrefab;  // Gemma degli Zombie (droppata dai nemici)
    public GameObject redGemPrefab;    // Gemma del Sangue (dopo 5 morti)
    public GameObject grayGemPrefab;   // Gemma della Nebbia (visibile solo con powerup)

    [Header("Item References")]
    public Item yellowGem;
    public Item blueGem;
    public Item greenGem;
    public Item redGem;
    public Item grayGem;
    public Item Goggles;

    [Header("Center of Labyrinth")]
    public Vector3 labyrinthCenter = new Vector3(155f, 155f, 0f);

    [Header("Yellow Gem Settings (Sun)")]
    [Range(10, 200)]
    public int yellowGemMinDistance = 30;
    [Range(10, 200)]
    public int yellowGemMaxDistance = 100;
    public int maxYellowGemsOnMap = 30;
    public float yellowGemSpawnDelay = 5f; // Delay dall'inizio del giorno

    [Header("Blue Gem Settings (Night)")]
    [Range(10, 200)]
    public int blueGemMinDistance = 80;
    [Range(10, 200)]
    public int blueGemMaxDistance = 130;
    public int maxBlueGemsOnMap = 30;
    public float blueGemSpawnDelay = 3f; // Delay dall'inizio della notte

    [Header("Green Gem Settings (Zombie)")]
    [Range(0f, 1f)]
    public float greenGemDropChance = 0.15f; // 15% di probabilità

    [Header("Red Gem Settings (Blood)")]
    public int deathsRequiredForRedGem = 5;
    public Vector3 redGemSpawnPosition = new Vector3(0f, 0f, 0f); // Posizione fissa
    public int currentPlayerDeaths = 0;
    private bool redGemSpawned = false;

    [Header("Gray Gem Settings (Fog)")]
    [Range(10, 200)]
    public int grayGemMinDistance = 130;
    [Range(10, 200)]
    public int grayGemMaxDistance = 150;
    public bool fogVisibilityUnlocked = false; // Settato da un powerup
    public int maxGrayGemsOnMap = 2;

    [Header("Spawn Settings")]
    public float spawnCheckInterval = 1f; // Intervallo controllo spawn
    public int maxSpawnAttempts = 50; // Max tentativi per trovare posizione valida

    [Header("LOD (Level of Detail) Settings")]
    [Tooltip("Distanza BFS massima entro cui le gemme vengono istanziate")]
    public int gemSpawnDistance = 15;
    [Tooltip("Distanza BFS oltre cui le gemme vengono distrutte")]
    public int gemDespawnDistance = 20;
    [Tooltip("Intervallo per controllo LOD")]
    public float lodUpdateInterval = 0.5f;

    [Header("Gem Collection Tracking")]
    private bool yellowGemCollected = false;
    private bool blueGemCollected = false;
    private bool greenGemCollected = false;
    private bool grayGemCollected = false;
    private bool redGemCollected = false;

    private GameObject redGemInstance;

    // NUOVO: Strutture dati per ottimizzazione LOD
    [System.Serializable]
    public class GemSpawnData
    {
        public Vector3 worldPosition;
        public Vector2Int arrayPosition;
        public GameObject instancedGem; // null se non istanziata

        public GemSpawnData(Vector3 worldPos, Vector2Int arrayPos)
        {
            worldPosition = worldPos;
            arrayPosition = arrayPos;
            instancedGem = null;
        }

        public bool IsInstanced => instancedGem != null;
    }

    // Liste delle posizioni per ogni tipo di gemma
    private List<GemSpawnData> yellowGemPositions = new List<GemSpawnData>();
    private List<GemSpawnData> blueGemPositions = new List<GemSpawnData>();
    private List<GemSpawnData> grayGemPositions = new List<GemSpawnData>();

    // State tracking - ora traccia solo gemme istanziate
    private List<GameObject> activeYellowGems = new List<GameObject>();
    private List<GameObject> activeBlueGems = new List<GameObject>();
    private List<GameObject> activeGrayGems = new List<GameObject>();
    private List<GameObject> activeGreenGems = new List<GameObject>();


    private Coroutine spawnCoroutine;
    private Coroutine lodCoroutine;

    private static Dictionary<Vector2Int, GameObject> occupiedPositions = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        InitializeReferences();

        // Trova il coin generator se non assegnato
        if (coinGenerator == null)
            coinGenerator = FindFirstObjectByType<DynamicCoinGenerator>();

        StartSpawnCoroutine();
        StartLODCoroutine(); // NUOVO: Avvia controllo LOD

        // Sottoscrivi agli eventi del ciclo giorno/notte
        if (dayNightManager != null)
        {
            dayNightManager.events.OnDayStart.AddListener(OnDayStart);
            dayNightManager.events.OnNightStart.AddListener(OnNightStart);
        }

        // AGGIUNTO: Se il gioco inizia dal giorno, genera posizioni gemme gialle dopo l'inizializzazione
        StartCoroutine(CheckInitialDaySpawn());
    }

    IEnumerator CheckInitialDaySpawn()
    {
        // Aspetta qualche frame per assicurarsi che tutto sia inizializzato
        yield return new WaitForSeconds(0.5f);

        // Se siamo ancora nel primo giorno e non abbiamo ancora raccolto gemme gialle
        if (dayNightManager != null && dayNightManager.IsDay && !yellowGemCollected)
        {
            if (enableDebug)
                Debug.Log("GemSpawner: Generando posizioni gemme gialle per avvio iniziale dal giorno");

            GenerateYellowGemPositions();
        }
    }

    void InitializeReferences()
    {
        if (mapManager == null)
            mapManager = FindFirstObjectByType<MapManager>();

        if (dayNightManager == null)
            dayNightManager = FindFirstObjectByType<DayNightCycleManager>();

        if (playerController == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerController = playerObj.GetComponent<PlayerController>();
        }

        if (gemCollectedPanel != null)
            gemCollectedPanel.SetActive(false);

        if (LightGemText != null)
            LightGemText.gameObject.SetActive(false);

        if (NightGemText != null)
            NightGemText.gameObject.SetActive(false);

        if (ZombieGemText != null)
            ZombieGemText.gameObject.SetActive(false);

        if (BloodGemText != null)
            BloodGemText.gameObject.SetActive(false);
        
        if (FogGemText != null)
            FogGemText.gameObject.SetActive(false);
    }

    // NUOVO: Coroutine per gestione LOD
    void StartLODCoroutine()
    {
        if (lodCoroutine != null)
            StopCoroutine(lodCoroutine);

        lodCoroutine = StartCoroutine(LODUpdateRoutine());
    }

    IEnumerator LODUpdateRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(lodUpdateInterval);

            if (playerController != null && mapManager != null && mapManager.wallCalculated)
            {
                UpdateGemLOD();
            }
        }
    }

    void UpdateGemLOD()
    {
        if (playerController == null || mapManager == null)
            return;

        Vector2Int playerPos = mapManager.WorldToArrayCoordinates(playerController.transform.position);

        // NUOVO: Controlla se il player è nell'inner hub (fuori dai bounds del labirinto)
        bool playerInInnerHub = !mapManager.IsValidArrayCoordinate(playerPos);

        // Aggiorna LOD per gemme gialle solo durante il giorno
        if (!yellowGemCollected && yellowGemPositions.Count > 0 && dayNightManager != null && dayNightManager.IsDay)
        {
            UpdateGemTypeLODWithHubCheck(yellowGemPositions, yellowGemPrefab, activeYellowGems, playerPos, playerInInnerHub);
        }

        // Aggiorna LOD per gemme blu solo durante la notte
        if (!blueGemCollected && blueGemPositions.Count > 0 && dayNightManager != null && !dayNightManager.IsDay)
        {
            UpdateGemTypeLODWithHubCheck(blueGemPositions, blueGemPrefab, activeBlueGems, playerPos, playerInInnerHub);
        }

        // Aggiorna LOD per gemme grigie (sempre attive se sbloccate)
        if (!grayGemCollected && grayGemPositions.Count > 0 && fogVisibilityUnlocked)
            UpdateGemTypeLOD(grayGemPositions, grayGemPrefab, activeGrayGems, playerPos);
    }

    void UpdateGemTypeLODWithHubCheck(List<GemSpawnData> gemPositions, GameObject gemPrefab, List<GameObject> activeGems, Vector2Int playerPos, bool playerInInnerHub)
    {
        if (playerInInnerHub)
        {
            // Se il player è nell'inner hub (fuori dal labirinto), non spawnare nessuna gemma
            // e despawna tutte quelle attive
            for (int i = 0; i < gemPositions.Count; i++)
            {
                GemSpawnData gemData = gemPositions[i];
                if (gemData.IsInstanced)
                {
                    DestroyGem(gemData, activeGems);
                }
            }
            return;
        }

        // Comportamento normale quando il player è nel labirinto
        for (int i = 0; i < gemPositions.Count; i++)
        {
            GemSpawnData gemData = gemPositions[i];

            // Ottieni distanza BFS dalla posizione della gemma
            int distance = GetBFSDistance(gemData.arrayPosition);

            // Se la gemma non è istanziata ma è abbastanza vicina, istanziala
            if (!gemData.IsInstanced && distance != -1 && distance <= gemSpawnDistance)
            {
                InstantiateGem(gemData, gemPrefab, activeGems);
            }
            // Se la gemma è istanziata ma troppo lontana, distruggila
            else if (gemData.IsInstanced && (distance == -1 || distance > gemDespawnDistance))
            {
                DestroyGem(gemData, activeGems);
            }
        }
    }

    // NUOVO: Gestione LOD per un tipo specifico di gemma
    void UpdateGemTypeLOD(List<GemSpawnData> gemPositions, GameObject gemPrefab, List<GameObject> activeGems, Vector2Int playerPos)
    {
        for (int i = 0; i < gemPositions.Count; i++)
        {
            GemSpawnData gemData = gemPositions[i];

            // Ottieni distanza BFS dalla posizione della gemma
            int distance = GetBFSDistance(gemData.arrayPosition);

            // Se la gemma non è istanziata ma è abbastanza vicina, istanziala
            if (!gemData.IsInstanced && distance != -1 && distance <= gemSpawnDistance)
            {
                InstantiateGem(gemData, gemPrefab, activeGems);
            }
            // Se la gemma è istanziata ma troppo lontana, distruggila
            else if (gemData.IsInstanced && (distance == -1 || distance > gemDespawnDistance))
            {
                DestroyGem(gemData, activeGems);
            }
        }
    }

    // NUOVO: Istanzia una gemma
    void InstantiateGem(GemSpawnData gemData, GameObject gemPrefab, List<GameObject> activeGems)
    {
        GameObject gem = Instantiate(gemPrefab, gemData.worldPosition, Quaternion.identity);
        gemData.instancedGem = gem;
        activeGems.Add(gem);

        RegisterOccupiedPosition(gemData.arrayPosition, gem);

        if (enableDebug)
            Debug.Log($"GemSpawner LOD: Istanziata gemma a {gemData.worldPosition}");
    }

    // NUOVO: Distrugge una gemma istanziata
    void DestroyGem(GemSpawnData gemData, List<GameObject> activeGems)
    {
        if (gemData.instancedGem != null)
        {
            activeGems.Remove(gemData.instancedGem);
            UnregisterOccupiedPosition(gemData.arrayPosition);
            Destroy(gemData.instancedGem);
            gemData.instancedGem = null;

            if (enableDebug)
                Debug.Log($"GemSpawner LOD: Distrutta gemma a {gemData.worldPosition}");
        }
    }

    int GetBFSDistance(Vector2Int arrayPos)
    {
        if (mapManager == null || !mapManager.IsValidArrayCoordinate(arrayPos))
            return -1;

        // NUOVO: Controllo aggiuntivo per evitare IndexOutOfRangeException
        if (arrayPos.x < 0 || arrayPos.x >= mapManager.Distances.GetLength(0) ||
            arrayPos.y < 0 || arrayPos.y >= mapManager.Distances.GetLength(1))
            return -1;

        return mapManager.Distances[arrayPos.x, arrayPos.y];
    }

    //Registra una posizione come occupata
    public static void RegisterOccupiedPosition(Vector2Int position, GameObject obj)
    {
        if (occupiedPositions.ContainsKey(position))
        {
            // Se c'è già qualcosa, rimuovi il vecchio oggetto
            if (occupiedPositions[position] != null && occupiedPositions[position] != obj)
            {
                Debug.LogWarning($"Posizione {position} già occupata! Rimuovendo oggetto esistente.");
                Destroy(occupiedPositions[position]);
            }
        }
        occupiedPositions[position] = obj;
    }

    // Libera una posizione occupata
    public static void UnregisterOccupiedPosition(Vector2Int position)
    {
        occupiedPositions.Remove(position);
    }

    void StartSpawnCoroutine()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnCheckRoutine());
    }

    public static bool IsPositionFree(Vector2Int position)
    {
        // Pulisci reference a oggetti distrutti
        if (occupiedPositions.ContainsKey(position) && occupiedPositions[position] == null)
        {
            occupiedPositions.Remove(position);
        }

        return !occupiedPositions.ContainsKey(position);
    }

    public static void ClearAllOccupiedPositions()
    {
        occupiedPositions.Clear();
    }

    IEnumerator SpawnCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnCheckInterval);

            // Pulisci reference a gemme distrutte
            CleanupDestroyedGems();

            // MODIFICATO: Controlla solo se generare posizioni per gemme grigie
            if (fogVisibilityUnlocked && !grayGemCollected && grayGemPositions.Count < maxGrayGemsOnMap)
            {
                GenerateGrayGemPositions();
            }
        }
    }

    void CleanupDestroyedGems()
    {
        activeYellowGems.RemoveAll(gem => gem == null);
        activeBlueGems.RemoveAll(gem => gem == null);
        activeGrayGems.RemoveAll(gem => gem == null);
        activeGreenGems.RemoveAll(gem => gem == null);

        // NUOVO: Pulisci anche i riferimenti nelle liste posizioni
        CleanupGemPositionsList(yellowGemPositions, activeYellowGems);
        CleanupGemPositionsList(blueGemPositions, activeBlueGems);
        CleanupGemPositionsList(grayGemPositions, activeGrayGems);
    }

    // NUOVO: Pulisci riferimenti nelle liste posizioni
    void CleanupGemPositionsList(List<GemSpawnData> positions, List<GameObject> activeGems)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            if (positions[i].IsInstanced && positions[i].instancedGem == null)
            {
                positions[i].instancedGem = null;
            }
        }
    }

    #region Day/Night Event Handlers

    void OnDayStart()
    {
        if (enableDebug)
            Debug.Log("GemSpawner: Giorno iniziato - rimuovendo gemme blu e verdi, gestendo posizioni gemme gialle");

        // Rimuovi tutte le gemme blu quando inizia il giorno
        DestroyAllGemsOfType(blueGemPositions, activeBlueGems);
        // AGGIUNTO: Pulisci anche le posizioni blu per rigenerarle alla prossima notte
        blueGemPositions.Clear();

        // NUOVO: Rimuovi tutte le gemme verdi attive quando inizia il giorno
        DestroyAllActiveGreenGems();

        // Genera posizioni gemme gialle se non sono state raccolte
        if (!yellowGemCollected)
        {
            // MODIFICATO: Se Ã¨ il primissimo avvio (startFromDay), genera immediatamente
            if (dayNightManager != null && dayNightManager.startFromDay)
            {
                GenerateYellowGemPositions(); // Generazione immediata
            }
            else
            {
                StartCoroutine(GenerateYellowGemPositionsWithDelay()); // Generazione con delay normale
            }
        }
    }

    void OnNightStart()
    {
        if (enableDebug)
            Debug.Log("GemSpawner: Notte iniziata - rimuovendo gemme gialle e generando posizioni gemme blu");

        // Rimuovi tutte le gemme gialle quando inizia la notte
        DestroyAllGemsOfType(yellowGemPositions, activeYellowGems);
        // AGGIUNTO: Pulisci anche le posizioni gialle per rigenerarle al prossimo giorno
        yellowGemPositions.Clear();

        // Genera posizioni gemme blu se non sono state raccolte
        if (!blueGemCollected)
            StartCoroutine(GenerateBlueGemPositionsWithDelay());
    }

    #endregion

    #region Yellow Gem Generation (Sun)

    IEnumerator GenerateYellowGemPositionsWithDelay()
    {
        yield return new WaitForSeconds(yellowGemSpawnDelay);
        GenerateYellowGemPositions();
    }

    void GenerateYellowGemPositions()
    {
        if (yellowGemPrefab == null || playerController == null || mapManager == null || yellowGemCollected)
        {
            if (enableDebug && yellowGemCollected)
                Debug.Log("GemSpawner: Gemma gialla già raccolta - skip generazione");
            return;
        }

        // Genera posizioni se non già fatto
        if (yellowGemPositions.Count == 0)
        {
            GenerateGemPositionsInRadius(yellowGemPositions, yellowGemMinDistance, yellowGemMaxDistance, maxYellowGemsOnMap);

            if (enableDebug)
                Debug.Log($"GemSpawner: Generate {yellowGemPositions.Count} posizioni per gemme gialle");
        }
    }

    #endregion

    #region Blue Gem Generation (Night)

    IEnumerator GenerateBlueGemPositionsWithDelay()
    {
        yield return new WaitForSeconds(blueGemSpawnDelay);
        GenerateBlueGemPositions();
    }

    void GenerateBlueGemPositions()
    {
        if (blueGemPrefab == null || playerController == null || mapManager == null || blueGemCollected)
        {
            if (enableDebug && blueGemCollected)
                Debug.Log("GemSpawner: Gemma blu già raccolta - skip generazione");
            return;
        }

        // Genera posizioni se non già fatto
        if (blueGemPositions.Count == 0)
        {
            GenerateGemPositionsInRadius(blueGemPositions, blueGemMinDistance, blueGemMaxDistance, maxBlueGemsOnMap);

            if (enableDebug)
                Debug.Log($"GemSpawner: Generate {blueGemPositions.Count} posizioni per gemme blu");
        }
    }

    #endregion

    #region Green Gem Spawning (Zombie Drop)

    /// <summary>
    /// Chiamato dai nemici quando muoiono per tentare il drop di una gemma verde
    /// </summary>
    /// <param name="enemyPosition">Posizione del nemico morto</param>
    public void TryDropGreenGem(Vector3 enemyPosition)
    {
        if (greenGemPrefab == null || greenGemCollected)
        {
            if (enableDebug && greenGemCollected)
                Debug.Log("GemSpawner: Gemma verde giÃ  raccolta - skip drop");
            return;
        }

        if (Random.Range(0f, 1f) <= greenGemDropChance)
        {
            Vector3 dropPosition = FindClosestValidGemPosition(enemyPosition);

            if (dropPosition != Vector3.zero)
            {
                Vector2Int arrayPos = mapManager.WorldToArrayCoordinates(dropPosition);

                // Controlla se c'Ã¨ una moneta da rimuovere
                RemoveCoinAtPosition(arrayPos);

                // Spawna la gemma verde
                GameObject gem = Instantiate(greenGemPrefab, dropPosition, Quaternion.identity);

                // NUOVO: Aggiungi alla lista delle gemme verdi attive
                activeGreenGems.Add(gem);

                // Registra la posizione come occupata
                RegisterOccupiedPosition(arrayPos, gem);

                if (enableDebug)
                    Debug.Log($"GemSpawner: Gemma verde droppata a {dropPosition} (snappato) dal nemico in {enemyPosition}");
            }
            else
            {
                if (enableDebug)
                    Debug.Log($"GemSpawner: Nessuna posizione valida trovata vicino a {enemyPosition} per gemma verde");
            }
        }
    }

    Vector3 FindClosestValidGemPosition(Vector3 centerPosition)
    {
        if (mapManager == null || !mapManager.wallCalculated)
            return Vector3.zero;

        Vector2Int centerArrayPos = mapManager.WorldToArrayCoordinates(centerPosition);

        // Se la posizione centrale è già valida, usala
        if (IsValidGemSpawnPosition(centerArrayPos))
        {
            return SnapToTileCenter(centerArrayPos);
        }

        // Cerca in cerchi concentrici crescenti (algoritmo BFS-like)
        int maxSearchRadius = 5; // Massimo 5 tile di raggio

        for (int radius = 1; radius <= maxSearchRadius; radius++)
        {
            // Controlla tutti i tile nel perimetro del cerchio corrente
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    // Salta posizioni non sul perimetro del cerchio corrente
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                        continue;

                    Vector2Int testPos = centerArrayPos + new Vector2Int(dx, dy);

                    if (IsValidGemSpawnPosition(testPos))
                    {
                        return SnapToTileCenter(testPos);
                    }
                }
            }
        }

        // Se non trova nessuna posizione valida nel raggio di ricerca
        return Vector3.zero;
    }

    Vector3 SnapToTileCenter(Vector2Int arrayPos)
    {
        Vector3Int cellPos = new Vector3Int(
            arrayPos.x + mapManager.MapOffset.x,
            arrayPos.y + mapManager.MapOffset.y,
            0
        );

        Vector3 worldPos = mapManager.tilemap.CellToWorld(cellPos);
        // Snappa al centro del tile
        return new Vector3(
            Mathf.Floor(worldPos.x) + 0.5f,
            Mathf.Floor(worldPos.y) + 0.5f,
            0
        );
    }

    bool IsValidGemSpawnPosition(Vector2Int arrayPos)
    {
        // Deve essere dentro i bounds
        if (!mapManager.IsValidArrayCoordinate(arrayPos))
            return false;

        // Deve essere un corridoio camminabile per AI
        if (!mapManager.IsWalkableForAI(arrayPos))
            return false;

        // Deve essere valido per spawn (stesso controllo delle monete)
        if (!mapManager.IsValidForCoinSpawn(arrayPos))
            return false;

        return true;
    }

    void RemoveCoinAtPosition(Vector2Int arrayPos)
    {
        if (coinGenerator != null)
        {
            // Informa il coin generator che deve rimuovere la moneta
            coinGenerator.OnCoinCollected(arrayPos);

            if (enableDebug)
                Debug.Log($"GemSpawner: Rimossa moneta alla posizione {arrayPos} per spawn gemma verde");
        }
    }

    #endregion

    #region Red Gem Spawning (Blood)

    /// <summary>
    /// Chiamato quando il player muore
    /// </summary>
    public void OnPlayerDeath()
    {
        currentPlayerDeaths++;

        if (enableDebug)
            Debug.Log($"GemSpawner: Player morto {currentPlayerDeaths}/{deathsRequiredForRedGem} volte");

        if (currentPlayerDeaths >= deathsRequiredForRedGem && !redGemSpawned && !redGemCollected)
        {
            SpawnRedGem();
        }
    }

    void SpawnRedGem()
    {
        if (redGemPrefab == null || redGemCollected)
        {
            if (enableDebug && redGemCollected)
                Debug.Log("GemSpawner: Gemma rossa già raccolta - skip spawn");
            return;
        }

        // Usa la posizione fissa specificata nell'inspector
        redGemInstance = Instantiate(redGemPrefab, redGemSpawnPosition, Quaternion.identity);
        redGemSpawned = true;

        if (enableDebug)
            Debug.Log($"GemSpawner: Gemma rossa spawnata alla posizione fissa {redGemSpawnPosition} dopo {currentPlayerDeaths} morti");
    }

    #endregion

    #region Gray Gem Generation (Fog)

    /// <summary>
    /// Chiamato quando si sblocca la visibilità della nebbia
    /// </summary>
    public void UnlockFogVisibility()
    {
        fogVisibilityUnlocked = true;

        if (enableDebug)
            Debug.Log("GemSpawner: Visibilità nebbia sbloccata");
    }

    void GenerateGrayGemPositions()
    {
        if (grayGemPrefab == null || playerController == null || mapManager == null || grayGemCollected)
        {
            if (enableDebug && grayGemCollected)
                Debug.Log("GemSpawner: Gemma grigia già raccolta - skip generazione");
            return;
        }

        int positionsToGenerate = maxGrayGemsOnMap - grayGemPositions.Count;

        if (positionsToGenerate > 0)
        {
            GenerateGemPositionsInRadius(grayGemPositions, grayGemMinDistance, grayGemMaxDistance, positionsToGenerate);

            if (enableDebug)
                Debug.Log($"GemSpawner: Generate {positionsToGenerate} nuove posizioni per gemme grigie");
        }
    }

    #endregion

    #region Position Generation

    /// <summary>
    /// NUOVO: Genera posizioni per gemme in un raggio dal centro del labirinto
    /// </summary>
    void GenerateGemPositionsInRadius(List<GemSpawnData> targetList, int minDistance, int maxDistance, int maxPositions)
    {
        if (mapManager == null || !mapManager.wallCalculated)
            return;

        int positionsGenerated = 0;

        for (int attempt = 0; attempt < maxSpawnAttempts * maxPositions && positionsGenerated < maxPositions; attempt++)
        {
            // Genera angolo casuale
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // Genera distanza casuale nel range
            float distance = Random.Range(minDistance, maxDistance);

            // Calcola posizione target dal centro del labirinto
            Vector3 targetWorldPos = labyrinthCenter + new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                0
            );

            // Snappa al centro del tile (x.5, y.5)
            Vector3 snappedPos = new Vector3(
                Mathf.Floor(targetWorldPos.x) + 0.5f,
                Mathf.Floor(targetWorldPos.y) + 0.5f,
                0
            );

            // Verifica se la posizione è valida per spawn di gemme
            Vector2Int arrayPos = mapManager.WorldToArrayCoordinates(snappedPos);

            if (mapManager.IsValidArrayCoordinate(arrayPos) &&
                IsValidCorridorTile(arrayPos) &&
                IsPositionFree(arrayPos) &&
                !IsPositionAlreadyInList(arrayPos, targetList))
            {
                // Verifica distanza effettiva dal centro del labirinto
                float actualDistance = Vector3.Distance(labyrinthCenter, snappedPos);
                if (actualDistance >= minDistance && actualDistance <= maxDistance)
                {
                    targetList.Add(new GemSpawnData(snappedPos, arrayPos));
                    RegisterOccupiedPosition(arrayPos, null); // Riserva la posizione
                    positionsGenerated++;
                }
            }
        }

        if (enableDebug)
            Debug.Log($"GemSpawner: Generate {positionsGenerated}/{maxPositions} posizioni valide");
    }

    /// <summary>
    /// NUOVO: Controlla se una posizione è già nella lista
    /// </summary>
    bool IsPositionAlreadyInList(Vector2Int arrayPos, List<GemSpawnData> list)
    {
        foreach (var data in list)
        {
            if (data.arrayPosition == arrayPos)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Trova una posizione valida vicino a un punto specificato (solo su corridoi)
    /// </summary>
    Vector3 FindNearbyValidGemPosition(Vector3 centerPosition, int maxRadius)
    {
        if (mapManager == null || !mapManager.wallCalculated)
            return Vector3.zero;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            // Genera posizione casuale nel raggio
            Vector3 offset = new Vector3(
                Random.Range(-maxRadius, maxRadius + 1),
                Random.Range(-maxRadius, maxRadius + 1),
                0
            );

            Vector3 targetPos = centerPosition + offset;

            // Snappa al centro del tile (x.5, y.5)
            Vector3 snappedPos = new Vector3(
                Mathf.Floor(targetPos.x) + 0.5f,
                Mathf.Floor(targetPos.y) + 0.5f,
                0
            );

            Vector2Int arrayPos = mapManager.WorldToArrayCoordinates(snappedPos);

            if (mapManager.IsValidArrayCoordinate(arrayPos) &&
                IsValidCorridorTile(arrayPos) &&
                IsPositionFree(arrayPos))
            {
                return snappedPos;
            }
        }

        // Se non trova una posizione valida, prova a usare la posizione del centro snappata
        Vector3 fallbackPos = new Vector3(
            Mathf.Floor(centerPosition.x) + 0.5f,
            Mathf.Floor(centerPosition.y) + 0.5f,
            0
        );

        Vector2Int fallbackArrayPos = mapManager.WorldToArrayCoordinates(fallbackPos);
        if (mapManager.IsValidArrayCoordinate(fallbackArrayPos) &&
            IsValidCorridorTile(fallbackArrayPos) &&
            IsPositionFree(fallbackArrayPos))
        {
            return fallbackPos;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Controlla se una posizione è un tile corridoio valido per spawn gemme
    /// </summary>
    bool IsValidCorridorTile(Vector2Int arrayPos)
    {
        // Usa la stessa logica delle monete ma assicurati che sia un corridoio
        if (!mapManager.IsValidForCoinSpawn(arrayPos))
            return false;

        // Verifica che non sia un muro (assumendo che i corridoi siano walkable)
        return mapManager.IsWalkableForAI(arrayPos);
    }

    #endregion

    #region Gem Collection Methods

    /// <summary>
    /// Chiamato quando il player raccoglie una gemma gialla
    /// </summary>
    public void OnYellowGemCollected()
    {
        yellowGemCollected = true;
        inventoryManager.AddItem(yellowGem);
        DestroyAllGemsOfType(yellowGemPositions, activeYellowGems);
        StartCoroutine(ShowGemCollected(LightGemText));

        if (enableDebug)
            Debug.Log("GemSpawner: Gemma gialla raccolta - non spawneranno più gemme gialle");
    }

    /// <summary>
    /// Chiamato quando il player raccoglie una gemma blu
    /// </summary>
    public void OnBlueGemCollected()
    {
        blueGemCollected = true;
        inventoryManager.AddItem(blueGem);
        DestroyAllGemsOfType(blueGemPositions, activeBlueGems);
        StartCoroutine(ShowGemCollected(NightGemText));

        if (enableDebug)
            Debug.Log("GemSpawner: Gemma blu raccolta - non spawneranno più gemme blu");
    }

    /// <summary>
    /// Chiamato quando il player raccoglie una gemma verde
    /// </summary>
    public void OnGreenGemCollected()
    {
        greenGemCollected = true;
        inventoryManager.AddItem(greenGem);
        DestroyAllActiveGreenGems();
        StartCoroutine(ShowGemCollected(ZombieGemText));

        if (enableDebug)
            Debug.Log("GemSpawner: Gemma verde raccolta - tutte le altre gemme verdi rimosse");
    }

    /// <summary>
    /// Chiamato quando il player raccoglie una gemma grigia
    /// </summary>
    public void OnGrayGemCollected()
    {
        grayGemCollected = true;
        inventoryManager.AddItem(grayGem);
        DestroyAllGemsOfType(grayGemPositions, activeGrayGems);
        StartCoroutine(ShowGemCollected(FogGemText));

        if (enableDebug)
            Debug.Log("GemSpawner: Gemma grigia raccolta - non spawneranno più gemme grigie");
    }

    /// <summary>
    /// Chiamato quando il player raccoglie una gemma rossa
    /// </summary>
    public void OnRedGemCollected()
    {
        redGemCollected = true;
        inventoryManager.AddItem(redGem);

        Destroy(redGemInstance);
        StartCoroutine(ShowGemCollected(BloodGemText));

        if (enableDebug)
            Debug.Log("GemSpawner: Gemma rossa raccolta - non spawneranno più gemme rosse");
    }

    #endregion

    #region Utility Methods

    // MODIFICATO: Distrugge tutte le gemme di un tipo specifico
    void DestroyAllGemsOfType(List<GemSpawnData> gemPositions, List<GameObject> activeGems)
    {
        // Distruggi tutte le gemme istanziate
            for (int i = activeGems.Count - 1; i >= 0; i--)
            {
                if (activeGems[i] != null)
                {
                    // Libera la posizione prima di distruggere
                    Vector2Int arrayPos = mapManager.WorldToArrayCoordinates(activeGems[i].transform.position);
                    UnregisterOccupiedPosition(arrayPos);

                    Destroy(activeGems[i]);
                }
            }
        activeGems.Clear();

        // Reset dei riferimenti nelle posizioni
        for (int i = 0; i < gemPositions.Count; i++)
        {
            if (gemPositions[i].instancedGem != null)
            {
                // AGGIUNTO: Libera anche le posizioni occupate dalle posizioni non istanziate
                UnregisterOccupiedPosition(gemPositions[i].arrayPosition);
            }
            gemPositions[i].instancedGem = null;
        }
    }

    void DestroyAllActiveGreenGems()
    {
        for (int i = activeGreenGems.Count - 1; i >= 0; i--)
        {
            if (activeGreenGems[i] != null)
            {
                // Libera la posizione occupata
                Vector2Int arrayPos = mapManager.WorldToArrayCoordinates(activeGreenGems[i].transform.position);
                UnregisterOccupiedPosition(arrayPos);

                Destroy(activeGreenGems[i]);
            }
        }
        activeGreenGems.Clear();

        if (enableDebug && activeGreenGems.Count > 0)
            Debug.Log("GemSpawner: Tutte le gemme verdi attive sono state distrutte");
    }

    IEnumerator ShowGemCollected(TextMeshProUGUI gemText)
    {
        gemCollectedPanel.SetActive(true);

        if (gemText != null)
        {
            gemText.gameObject.SetActive(true);

            yield return new WaitForSeconds(2f);

            gemText.gameObject.SetActive(false);
        }

        gemCollectedPanel.SetActive(false);
    }

    /// <summary>
    /// Reset dello spawner per nuovo gioco
    /// </summary>
    public void ResetSpawner()
    {
        currentPlayerDeaths = 0;
        redGemSpawned = false;
        fogVisibilityUnlocked = false;

        // Reset collection flags
        yellowGemCollected = false;
        blueGemCollected = false;
        greenGemCollected = false;
        grayGemCollected = false;
        redGemCollected = false;

        // Rimuovi tutte le gemme attive e pulisci le posizioni
        DestroyAllGemsOfType(yellowGemPositions, activeYellowGems);
        DestroyAllGemsOfType(blueGemPositions, activeBlueGems);
        DestroyAllGemsOfType(grayGemPositions, activeGrayGems);

        // NUOVO: Rimuovi anche tutte le gemme verdi
        DestroyAllActiveGreenGems();

        // Pulisci completamente le liste posizioni
        yellowGemPositions.Clear();
        blueGemPositions.Clear();
        grayGemPositions.Clear();

        ClearAllOccupiedPositions();

        if (enableDebug)
            Debug.Log("GemSpawner: Reset completato con ottimizzazione LOD e gestione gemme verdi");
    }

    /// <summary>
    /// NUOVO: Force update del LOD quando il player si teletrasporta
    /// </summary>
    public void ForceUpdateLOD()
    {
        if (playerController != null && mapManager != null && mapManager.wallCalculated)
        {
            UpdateGemLOD();

            if (enableDebug)
                Debug.Log("GemSpawner: LOD forzato dopo teletrasporto player");
        }
    }

    /// <summary>
    /// Ottieni statistiche correnti dello spawner
    /// </summary>
    public string GetSpawnerStats()
    {
        int yellowInstanced = 0, blueInstanced = 0, grayInstanced = 0;

        foreach (var pos in yellowGemPositions)
            if (pos.IsInstanced) yellowInstanced++;

        foreach (var pos in blueGemPositions)
            if (pos.IsInstanced) blueInstanced++;

        foreach (var pos in grayGemPositions)
            if (pos.IsInstanced) grayInstanced++;

        return $"Posizioni Generate - Gialle: {yellowGemPositions.Count}, Blu: {blueGemPositions.Count}, Grigie: {grayGemPositions.Count}\n" +
               $"Gemme Istanziate - Gialle: {yellowInstanced}, Blu: {blueInstanced}, Grigie: {grayInstanced}, Verdi: {activeGreenGems.Count}\n" +
               $"Morti Player: {currentPlayerDeaths}/{deathsRequiredForRedGem}, " +
               $"Gemma Rossa: {(redGemSpawned ? "Spawnata" : "Non Spawnata")}, " +
               $"VisibilitÃ  Nebbia: {(fogVisibilityUnlocked ? "Sbloccata" : "Bloccata")}\n" +
               $"Gemme Raccolte - G:{yellowGemCollected}, B:{blueGemCollected}, V:{greenGemCollected}, Gr:{grayGemCollected}, R:{redGemCollected}";
    }

    /// <summary>
    /// NUOVO: Ottieni informazioni LOD per debug
    /// </summary>
    public string GetLODInfo()
    {
        if (playerController == null || mapManager == null)
            return "Player o MapManager non disponibile";

        Vector2Int playerPos = mapManager.WorldToArrayCoordinates(playerController.transform.position);

        return $"Player Position: {playerPos}\n" +
               $"Spawn Distance: {gemSpawnDistance}, Despawn Distance: {gemDespawnDistance}\n" +
               $"LOD Update Interval: {lodUpdateInterval}s";
    }

    #endregion

    #region Gizmos and Debug Visualization

    void OnDrawGizmosSelected()
    {
        // Disegna il centro del labirinto
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(labyrinthCenter, 2f);

        // Disegna gli anelli di spawn delle gemme gialle
        Gizmos.color = Color.yellow;
        DrawWireCircle(labyrinthCenter, yellowGemMinDistance);
        DrawWireCircle(labyrinthCenter, yellowGemMaxDistance);

        // Disegna gli anelli di spawn delle gemme blu
        Gizmos.color = Color.blue;
        DrawWireCircle(labyrinthCenter, blueGemMinDistance);
        DrawWireCircle(labyrinthCenter, blueGemMaxDistance);

        // Disegna gli anelli di spawn delle gemme grigie
        Gizmos.color = Color.gray;
        DrawWireCircle(labyrinthCenter, grayGemMinDistance);
        DrawWireCircle(labyrinthCenter, grayGemMaxDistance);

        // Disegna la posizione fissa della gemma rossa
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(redGemSpawnPosition, Vector3.one);

        // NUOVO: Visualizza posizioni generate
        if (Application.isPlaying)
        {
            // Posizioni gemme gialle (cerchi gialli)
            Gizmos.color = Color.yellow;
            foreach (var pos in yellowGemPositions)
            {
                if (pos.IsInstanced)
                    Gizmos.DrawSphere(pos.worldPosition, 0.3f); // Istanziata
                else
                    Gizmos.DrawWireSphere(pos.worldPosition, 0.3f); // Solo posizione
            }

            // Posizioni gemme blu (cerchi blu)
            Gizmos.color = Color.blue;
            foreach (var pos in blueGemPositions)
            {
                if (pos.IsInstanced)
                    Gizmos.DrawSphere(pos.worldPosition, 0.3f);
                else
                    Gizmos.DrawWireSphere(pos.worldPosition, 0.3f);
            }

            // Posizioni gemme grigie (cerchi grigi)
            Gizmos.color = Color.gray;
            foreach (var pos in grayGemPositions)
            {
                if (pos.IsInstanced)
                    Gizmos.DrawSphere(pos.worldPosition, 0.3f);
                else
                    Gizmos.DrawWireSphere(pos.worldPosition, 0.3f);
            }

            // NUOVO: Visualizza range LOD del player
            if (playerController != null && mapManager != null)
            {
                Vector3 playerWorldPos = playerController.transform.position;

                // Range spawn (verde)
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(playerWorldPos, gemSpawnDistance);

                // Range despawn (rosso)
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(playerWorldPos, gemDespawnDistance);
            }
        }

#if UNITY_EDITOR
        // Etichette per i cerchi
        GUIStyle labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = 10;
        labelStyle.fontStyle = FontStyle.Bold;

        // Etichette gemme gialle
        Vector3 yellowMinLabel = labyrinthCenter + Vector3.right * yellowGemMinDistance + Vector3.up * 5f;
        Vector3 yellowMaxLabel = labyrinthCenter + Vector3.right * yellowGemMaxDistance + Vector3.up * 5f;
        UnityEditor.Handles.Label(yellowMinLabel, $"Yellow Min: {yellowGemMinDistance}", labelStyle);
        UnityEditor.Handles.Label(yellowMaxLabel, $"Yellow Max: {yellowGemMaxDistance}", labelStyle);

        // Etichette gemme blu
        Vector3 blueMinLabel = labyrinthCenter + Vector3.right * blueGemMinDistance + Vector3.down * 5f;
        Vector3 blueMaxLabel = labyrinthCenter + Vector3.right * blueGemMaxDistance + Vector3.down * 5f;
        UnityEditor.Handles.Label(blueMinLabel, $"Blue Min: {blueGemMinDistance}", labelStyle);
        UnityEditor.Handles.Label(blueMaxLabel, $"Blue Max: {blueGemMaxDistance}", labelStyle);

        // Etichette gemme grigie
        Vector3 grayMinLabel = labyrinthCenter + Vector3.left * grayGemMinDistance + Vector3.up * 5f;
        Vector3 grayMaxLabel = labyrinthCenter + Vector3.left * grayGemMaxDistance + Vector3.up * 5f;
        UnityEditor.Handles.Label(grayMinLabel, $"Gray Min: {grayGemMinDistance}", labelStyle);
        UnityEditor.Handles.Label(grayMaxLabel, $"Gray Max: {grayGemMaxDistance}", labelStyle);

        // Etichetta gemma rossa
        UnityEditor.Handles.Label(redGemSpawnPosition + Vector3.up + Vector3.left * 0.5f, "Red Gem Position", labelStyle);

        // NUOVO: Etichette LOD
        if (Application.isPlaying && playerController != null)
        {
            Vector3 playerPos = playerController.transform.position;
            UnityEditor.Handles.Label(playerPos + Vector3.up * 3f, $"LOD Spawn: {gemSpawnDistance}", labelStyle);
            UnityEditor.Handles.Label(playerPos + Vector3.up * 4f, $"LOD Despawn: {gemDespawnDistance}", labelStyle);
        }
#endif
    }

    void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 64;
        float angle = 0f;
        Vector3 lastPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

        for (int i = 1; i <= segments; i++)
        {
            angle = (float)i / segments * 2f * Mathf.PI;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }
    }

    #endregion

    void OnDestroy()
    {
        // Unsubscribe dagli eventi
        if (dayNightManager != null)
        {
            dayNightManager.events.OnDayStart.RemoveListener(OnDayStart);
            dayNightManager.events.OnNightStart.RemoveListener(OnNightStart);
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        // NUOVO: Ferma anche la coroutine LOD
        if (lodCoroutine != null)
        {
            StopCoroutine(lodCoroutine);
        }
    }

    /// <summary>
    /// METODO DEBUG: Testa il sistema di spawn gemme verdi
    /// </summary>
    [ContextMenu("Test Green Gem Drop")]
    void TestGreenGemDrop()
    {
        if (playerController != null)
        {
            TryDropGreenGem(playerController.transform.position);
        }
    }

    #region SAVE AND LOAD

    public void Save(ref GemData data)
    {
        data.hasYellowGem = yellowGemCollected;
        data.hasBlueGem = blueGemCollected;
        data.hasGreenGem = greenGemCollected;
        data.hasGrayGem = grayGemCollected;
        data.hasRedGem = redGemCollected;
    }

    public void Load(GemData data)
    {
        yellowGemCollected = data.hasYellowGem;
        blueGemCollected = data.hasBlueGem;
        greenGemCollected = data.hasGreenGem;
        grayGemCollected = data.hasGrayGem;
        redGemCollected = data.hasRedGem;
        playerController.hasLightGem = data.hasYellowGem;
        playerController.hasNightGem = data.hasBlueGem;
        playerController.hasZombieGem = data.hasGreenGem;
        playerController.hasFogGem = data.hasGrayGem;
        playerController.hasBloodGem = data.hasRedGem;
    }

    #endregion

}

//SAVE AND LOAD
[System.Serializable]
public struct GemData
{
    public bool hasYellowGem;
    public bool hasBlueGem;
    public bool hasGreenGem;
    public bool hasGrayGem;
    public bool hasRedGem;
}