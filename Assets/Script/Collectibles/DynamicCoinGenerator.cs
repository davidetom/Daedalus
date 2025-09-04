using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DynamicCoinGenerator : MonoBehaviour
{
    [Header("Coin Settings")]
    public GameObject coinPrefab;
    public int maxDistance = 10; // Distanza massima per generare monete
    public float coinSpawnChance = 0.05f; // Probabilità di spawn (5%)
    public float updateInterval = 1f; // Intervallo di aggiornamento in secondi
    
    [Header("Visibility Settings")]
    public Camera playerCamera; // Camera del player
    public float visibilityBuffer = 2f; // Buffer extra per evitare pop-in ai bordi
    public bool spawnInVisibleCorridors = true; // Spawna monete anche nei corridoi visibili ma non raggiungibili
    
    [Header("References")]
    public MapManager mapManager;
    public GameObject player;
    private Transform playerTransform;

    [Header("Collision Detection")]
    public GemSpawner gemSpawner; // Reference al gem spawner
    
    // Dictionary per tracciare le monete attive
    private Dictionary<Vector2Int, GameObject> activeCoins = new Dictionary<Vector2Int, GameObject>();
    
    // HashSet per evitare di ricalcolare posizioni già controllate
    private HashSet<Vector2Int> checkedPositions = new HashSet<Vector2Int>();
    
    void Start()
    {
        if (mapManager == null)
        {
            mapManager = Object.FindFirstObjectByType<MapManager>();
        }
        
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (gemSpawner == null)
            gemSpawner = FindFirstObjectByType<GemSpawner>();

        playerTransform = player.transform;
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null && playerTransform != null)
            {
                playerCamera = playerTransform.GetComponentInChildren<Camera>();
            }
        }
        
        // Avvia la coroutine per l'aggiornamento periodico
        StartCoroutine(UpdateCoinsRoutine());
    }
    
    IEnumerator UpdateCoinsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            
            if (mapManager != null && mapManager.wallCalculated && playerTransform != null)
            {
                UpdateCoins();
            }
        }
    }
    
    void UpdateCoins()
    {
        // Ottieni la posizione del player in coordinate array
        Vector2Int playerArrayPos = mapManager.WorldToArrayCoordinates(playerTransform.position);
        
        if (!mapManager.IsValidArrayCoordinate(playerArrayPos))
            return;
        
        // Lista delle posizioni da rimuovere
        List<Vector2Int> coinsToRemove = new List<Vector2Int>();
        
        // 1. Controlla le monete esistenti e rimuovi quelle troppo distanti
        foreach (var coinPair in activeCoins)
        {
            Vector2Int coinPos = coinPair.Key;
            int distance = mapManager.Distances[coinPos.x, coinPos.y];
            
            if (distance > maxDistance || distance <= 0) // distance <= 0 significa inaccessibile
            {
                coinsToRemove.Add(coinPos);
            }
        }
        
        // Rimuovi le monete troppo distanti
        foreach (var posToRemove in coinsToRemove)
        {
            if (activeCoins.TryGetValue(posToRemove, out GameObject coinToDestroy))
            {
                // NUOVO: Libera la posizione prima di distruggere
                GemSpawner.UnregisterOccupiedPosition(posToRemove);
                
                Destroy(coinToDestroy);
                activeCoins.Remove(posToRemove);
            }
        }
        
        // 2. Genera nuove monete nelle posizioni valide
        GenerateCoinsInRange(playerArrayPos);
        
        // 3. Se abilitato, genera monete anche nei corridoi visibili
        if (spawnInVisibleCorridors)
        {
            GenerateCoinsInVisibleCorridors();
        }
    }
    
    void GenerateCoinsInRange(Vector2Int playerPos)
    {
        checkedPositions.Clear();
        
        // Controlla solo una regione quadrata attorno al player per ottimizzare
        int searchRadius = maxDistance + 2; // Piccolo buffer per evitare pop-in
        
        int startX = Mathf.Max(0, playerPos.x - searchRadius);
        int endX = Mathf.Min(mapManager.MapWidth - 1, playerPos.x + searchRadius);
        int startY = Mathf.Max(0, playerPos.y - searchRadius);
        int endY = Mathf.Min(mapManager.MapHeight - 1, playerPos.y + searchRadius);
        
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                
                // Salta se già c'è una moneta qui
                if (activeCoins.ContainsKey(currentPos))
                    continue;
                
                // Controlla se è una posizione valida per una moneta
                if (IsValidCoinPosition(currentPos))
                {
                    // Genera moneta con probabilità
                    if (Random.value < coinSpawnChance)
                    {
                        SpawnCoin(currentPos);
                    }
                }
            }
        }
    }
    
    void GenerateCoinsInVisibleCorridors()
    {
        if (playerCamera == null)
            return;
        
        // Calcola i bounds del campo visivo in coordinate world
        Bounds cameraBounds = GetCameraWorldBounds();
        
        // Converti i bounds in coordinate array della tilemap
        Vector2Int minArrayPos = mapManager.WorldToArrayCoordinates(cameraBounds.min);
        Vector2Int maxArrayPos = mapManager.WorldToArrayCoordinates(cameraBounds.max);
        
        // Assicurati che siano dentro i bounds della mappa
        minArrayPos.x = Mathf.Max(0, minArrayPos.x);
        minArrayPos.y = Mathf.Max(0, minArrayPos.y);
        maxArrayPos.x = Mathf.Min(mapManager.MapWidth - 1, maxArrayPos.x);
        maxArrayPos.y = Mathf.Min(mapManager.MapHeight - 1, maxArrayPos.y);
        
        // Scansiona tutti i tile visibili
        for (int x = minArrayPos.x; x <= maxArrayPos.x; x++)
        {
            for (int y = minArrayPos.y; y <= maxArrayPos.y; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                
                // Salta se già c'è una moneta qui
                if (activeCoins.ContainsKey(currentPos))
                    continue;
                
                // Controlla se è un corridoio visibile (anche se non raggiungibile)
                if (IsVisibleCorridor(currentPos))
                {
                    // Genera moneta con probabilità
                    if (Random.value < coinSpawnChance)
                    {
                        SpawnCoin(currentPos);
                    }
                }
            }
        }
    }
    
    bool IsValidCoinPosition(Vector2Int arrayPos)
    {
        // Deve essere dentro i bounds
        if (!mapManager.IsValidArrayCoordinate(arrayPos))
            return false;
        
        // IMPORTANTE: Deve essere un tile valido per spawn monete (solo corridoi)
        if (!mapManager.IsValidForCoinSpawn(arrayPos))
            return false;
        
        // Deve essere a distanza valida dal player (raggiungibile tramite corridoi)
        int distance = mapManager.Distances[arrayPos.x, arrayPos.y];
        if (distance <= 0 || distance > maxDistance)
            return false;
        
        // Non deve essere visibile dalla camera del player
        if (IsPositionVisibleToPlayer(arrayPos))
            return false;
        
        // NUOVO: La posizione deve essere libera (no collision con gemme)
        if (!GemSpawner.IsPositionFree(arrayPos))
            return false;
        
        return true;
    }
    
    bool IsVisibleCorridor(Vector2Int arrayPos)
    {
        // Deve essere dentro i bounds
        if (!mapManager.IsValidArrayCoordinate(arrayPos))
            return false;
        
        // IMPORTANTE: Deve essere un corridoio (non prato o muro)
        if (!mapManager.IsValidForCoinSpawn(arrayPos))
            return false;
        
        // Deve essere vicino all'area visibile ma NON direttamente visibile
        // (per evitare spawn davanti agli occhi del player)
        if (IsPositionVisibleToPlayer(arrayPos))
            return false;
        
        // Controlla se è abbastanza vicino all'area visibile
        if (!IsNearVisibleArea(arrayPos))
            return false;
        
        // NUOVO: La posizione deve essere libera (no collision con gemme)
        if (!GemSpawner.IsPositionFree(arrayPos))
            return false;
        
        return true;
    }
    
    bool IsPositionVisibleToPlayer(Vector2Int arrayPos)
    {
        if (playerCamera == null)
            return false;
        
        // Converti coordinate array in posizione world
        Vector3Int cellPos = new Vector3Int(
            arrayPos.x + mapManager.MapOffset.x,
            arrayPos.y + mapManager.MapOffset.y,
            0
        );
        
        Vector3 worldPos = mapManager.tilemap.CellToWorld(cellPos);
        worldPos += new Vector3(0.5f, 0.5f, 0); // Centra nel tile
        
        // Ottieni i bounds della viewport con buffer
        Vector3 viewportPos = playerCamera.WorldToViewportPoint(worldPos);
        
        // Aggiungi un buffer per evitare pop-in ai bordi dello schermo
        float bufferNormalized = visibilityBuffer / Mathf.Min(Screen.width, Screen.height);
        
        // Controlla se è dentro i bounds della viewport (con buffer)
        bool isVisible = viewportPos.x >= -bufferNormalized && 
                        viewportPos.x <= 1f + bufferNormalized && 
                        viewportPos.y >= -bufferNormalized && 
                        viewportPos.y <= 1f + bufferNormalized &&
                        viewportPos.z > 0; // Davanti alla camera
        
        return isVisible;
    }
    
    bool IsNearVisibleArea(Vector2Int arrayPos)
    {
        if (playerCamera == null)
            return false;
        
        // Converti coordinate array in posizione world
        Vector3Int cellPos = new Vector3Int(
            arrayPos.x + mapManager.MapOffset.x,
            arrayPos.y + mapManager.MapOffset.y,
            0
        );
        
        Vector3 worldPos = mapManager.tilemap.CellToWorld(cellPos);
        worldPos += new Vector3(0.5f, 0.5f, 0);
        
        // Calcola la distanza dal centro della camera
        Vector3 cameraCenter = playerCamera.transform.position;
        cameraCenter.z = 0;
        
        float distance = Vector3.Distance(worldPos, cameraCenter);
        
        // Considera "vicino" se è entro una certa distanza dall'area visibile
        float maxVisibleDistance;
        if (playerCamera.orthographic)
        {
            maxVisibleDistance = Mathf.Max(playerCamera.orthographicSize * 2f, 
                                         playerCamera.orthographicSize * 2f * playerCamera.aspect);
        }
        else
        {
            float cameraDistance = Mathf.Abs(playerCamera.transform.position.z);
            float height = 2.0f * cameraDistance * Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            maxVisibleDistance = Mathf.Max(height, height * playerCamera.aspect);
        }
        
        // Aggiungi un range extra per includere corridoi appena fuori dall'inquadratura
        return distance <= maxVisibleDistance * 1.5f;
    }
    
    Bounds GetCameraWorldBounds()
    {
        if (playerCamera.orthographic)
        {
            float height = playerCamera.orthographicSize * 2f;
            float width = height * playerCamera.aspect;
            
            // Aggiungi il buffer
            width += visibilityBuffer;
            height += visibilityBuffer;
            
            Vector3 center = playerCamera.transform.position;
            center.z = 0; // Proietta sul piano della tilemap
            
            return new Bounds(center, new Vector3(width, height, 0));
        }
        else
        {
            // Per camera prospettica, calcola i bounds al livello z=0
            float distance = Mathf.Abs(playerCamera.transform.position.z);
            float height = 2.0f * distance * Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * playerCamera.aspect;
            
            // Aggiungi il buffer
            width += visibilityBuffer;
            height += visibilityBuffer;
            
            Vector3 center = playerCamera.transform.position;
            center.z = 0;
            
            return new Bounds(center, new Vector3(width, height, 0));
        }
    }
    
    void SpawnCoin(Vector2Int arrayPos)
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning("Coin prefab non assegnato!");
            return;
        }
        
        // NUOVO: Controllo finale per essere sicuri che la posizione sia libera
        if (!GemSpawner.IsPositionFree(arrayPos))
        {
            Debug.LogWarning($"Tentativo di spawnare moneta in posizione occupata: {arrayPos}");
            return;
        }
        
        // Converti coordinate array in posizione world
        Vector3Int cellPos = new Vector3Int(
            arrayPos.x + mapManager.MapOffset.x,
            arrayPos.y + mapManager.MapOffset.y,
            0
        );
        
        Vector3 worldPos = mapManager.tilemap.CellToWorld(cellPos);
        worldPos += new Vector3(0.5f, 0.5f, 0); // Centra nel tile
        
        // Instanzia la moneta
        GameObject newCoin = Instantiate(coinPrefab, worldPos, Quaternion.identity);
        newCoin.transform.parent = transform; // Organizza nell'hierarchy
        
        // Aggiungi alla dictionary
        activeCoins[arrayPos] = newCoin;
        
        // NUOVO: Registra la posizione come occupata
        GemSpawner.RegisterOccupiedPosition(arrayPos, newCoin);
    }
    
    // Metodo pubblico per forzare un aggiornamento (utile per debug)
    [ContextMenu("Force Update Coins")]
    public void ForceUpdateCoins()
    {
        if (mapManager != null && mapManager.wallCalculated && playerTransform != null)
        {
            UpdateCoins();
        }
    }
    
    // Metodo per pulire tutte le monete (utile per reset)
    public void ClearAllCoins()
    {
        foreach (var coinPair in activeCoins)
        {
            if (coinPair.Value != null)
            {
                // NUOVO: Libera la posizione prima di distruggere
                GemSpawner.UnregisterOccupiedPosition(coinPair.Key);
                
                Destroy(coinPair.Value);
            }
        }
        activeCoins.Clear();
    }

    public void SyncWithGemSystem()
    {
        // Registra tutte le monete esistenti nel sistema di collision detection
        foreach (var coinPair in activeCoins)
        {
            if (coinPair.Value != null)
            {
                GemSpawner.RegisterOccupiedPosition(coinPair.Key, coinPair.Value);
            }
        }
    }

    public void OnCoinCollected(Vector2Int position)
    {
        // Libera la posizione quando una moneta viene raccolta
        if (activeCoins.ContainsKey(position))
        {
            GemSpawner.UnregisterOccupiedPosition(position);
            activeCoins.Remove(position);
        }
    }

    // Metodi di debug/statistiche
    public int GetActiveCoinCount()
    {
        return activeCoins.Count;
    }
    
    public int GetCoinsInCorridors()
    {
        int count = 0;
        foreach (var coinPos in activeCoins.Keys)
        {
            if (mapManager.GetTileTypeAtArrayPos(coinPos) == TileType.Corridor)
            {
                count++;
            }
        }
        return count;
    }

    void OnDrawGizmosSelected()
    {
        // Visualizza le monete attive nell'editor
        if (activeCoins != null && mapManager != null)
        {
            foreach (var coinPair in activeCoins)
            {
                Vector2Int coinPos = coinPair.Key;
                TileType tileType = mapManager.GetTileTypeAtArrayPos(coinPos);

                // Colore diverso in base al tipo di tile e alle collision
                bool isBlocked = !GemSpawner.IsPositionFree(coinPos);

                if (isBlocked)
                {
                    Gizmos.color = Color.magenta; // Posizione bloccata da gemma
                }
                else
                {
                    switch (tileType)
                    {
                        case TileType.Corridor:
                            Gizmos.color = Color.yellow; // Monete su corridoi - OK
                            break;
                        case TileType.Grass:
                            Gizmos.color = Color.red; // Monete su prato - ERRORE!
                            break;
                        default:
                            Gizmos.color = Color.white; // Altri tipi - da verificare
                            break;
                    }
                }

                Vector3Int cellPos = new Vector3Int(
                    coinPos.x + mapManager.MapOffset.x,
                    coinPos.y + mapManager.MapOffset.y,
                    0
                );

                if (mapManager.tilemap != null)
                {
                    Vector3 worldPos = mapManager.tilemap.CellToWorld(cellPos);
                    worldPos += new Vector3(0.5f, 0.5f, 0);

                    if (isBlocked)
                    {
                        // Disegna un cubo solido per posizioni bloccate
                        Gizmos.DrawCube(worldPos, Vector3.one * 0.8f);
                    }
                    else
                    {
                        Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
                    }

                    // Mostra anche la distanza dal player
                    int distance = mapManager.Distances[coinPos.x, coinPos.y];
                    if (distance > 0)
                    {
                        // Disegna una linea che indica la distanza
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireSphere(worldPos, distance * 0.1f);
                    }
                }
            }
        }
    }
}