using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;

public class DynamicCoinGenerator : MonoBehaviour
{
    [Header("Configurazione Monete")]
    public GameObject coinPrefab;
    public float coinDensity = 0.3f; // Probabilità che una tile valida abbia una moneta (0-1)
    public int maxTotalCoins = 200; // Numero massimo di monete attive contemporaneamente
    
    [Header("Tilemap Settings")]
    public Tilemap tilemap;
    public TileBase targetTile;
    public Vector3 spawnOffset = new Vector3(0.5f, 0.5f, 0);
    
    [Header("Area Dinamica")]
    public float generationRadius = 25f; // Raggio di generazione intorno al player
    public float destructionRadius = 35f; // Raggio oltre il quale le monete vengono eliminate
    public float visibilityBuffer = 5f; // Buffer extra per spawn fuori dal campo visivo
    public float updateInterval = 1.5f; // Ogni quanto aggiornare (secondi)
    public int maxCoinsPerUpdate = 8; // Limite monete generate per update
    
    [Header("Chunk System")]
    public float chunkSize = 8f; // Dimensione di ogni chunk
    
    [Header("Camera Settings")]
    public Camera playerCamera; // Camera del player per calcolare il campo visivo
    public float cameraBufferMultiplier = 1.5f; // Moltiplicatore per il buffer della camera
    
    private Transform playerTransform;
    private Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
    private HashSet<Vector3Int> occupiedPositions = new HashSet<Vector3Int>();
    private float nextUpdateTime;
    private int currentCoinCount = 0;
    
    // Per il controllo del campo visivo
    private float cameraSize;
    private float cameraAspect;
    
    [System.Serializable]
    public class ChunkData
    {
        public List<GameObject> coins = new List<GameObject>();
        public HashSet<Vector3Int> generatedPositions = new HashSet<Vector3Int>();
        public bool isFullyGenerated = false;
        
        public void AddCoin(GameObject coin, Vector3Int position)
        {
            coins.Add(coin);
            generatedPositions.Add(position);
        }
        
        public bool RemoveCoin(GameObject coin, Vector3Int position)
        {
            generatedPositions.Remove(position);
            return coins.Remove(coin);
        }
        
        public void Clear()
        {
            foreach (GameObject coin in coins)
            {
                if (coin != null) DestroyImmediate(coin);
            }
            coins.Clear();
            generatedPositions.Clear();
            isFullyGenerated = false;
        }
    }
    
    void Start()
    {
        // Trova il player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Player non trovato! Assicurati che abbia il tag 'Player'");
            return;
        }
        
        // Trova la camera se non assegnata
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }
        
        SetupCamera();
        
        // Prima generazione con coroutine per evitare lag
        StartCoroutine(InitialGeneration());
        nextUpdateTime = Time.time + updateInterval;
    }
    
    void SetupCamera()
    {
        if (playerCamera != null)
        {
            if (playerCamera.orthographic)
            {
                cameraSize = playerCamera.orthographicSize;
                cameraAspect = playerCamera.aspect;
            }
            else
            {
                // Per camere perspective, approssima la size basandosi sulla distanza
                float distance = Vector3.Distance(playerCamera.transform.position, playerTransform.position);
                cameraSize = distance * Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                cameraAspect = playerCamera.aspect;
            }
        }
        else
        {
            // Valori di default
            cameraSize = 10f;
            cameraAspect = 16f/9f;
        }
    }
    
    IEnumerator InitialGeneration()
    {
        yield return new WaitForEndOfFrame();
        UpdateCoinGeneration();
    }
    
    void Update()
    {
        if (playerTransform == null) return;
        
        if (Time.time >= nextUpdateTime)
        {
            UpdateCoinGeneration();
            nextUpdateTime = Time.time + updateInterval;
        }
    }
    
    void UpdateCoinGeneration()
    {
        Vector3 playerPos = playerTransform.position;
        
        // 1. Rimuovi monete troppo lontane
        RemoveDistantCoins(playerPos);
        
        // 2. Genera monete nell'area vicina (ma fuori dal campo visivo)
        GenerateNearbyCoins(playerPos);
        
        // 3. Aggiorna conteggio
        UpdateCoinCount();
        
        // Debug info
        Debug.Log($"Monete attive: {currentCoinCount}/{maxTotalCoins}, Chunks attivi: {chunks.Count}");
    }
    
    void RemoveDistantCoins(Vector3 playerPos)
    {
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        
        foreach (var kvp in chunks)
        {
            Vector2Int chunkCoord = kvp.Key;
            ChunkData chunkData = kvp.Value;
            
            // Calcola centro del chunk
            Vector3 chunkCenter = new Vector3(
                chunkCoord.x * chunkSize + chunkSize * 0.5f,
                chunkCoord.y * chunkSize + chunkSize * 0.5f,
                playerPos.z
            );
            
            float distanceToChunk = Vector3.Distance(new Vector3(playerPos.x, playerPos.y, chunkCenter.z), chunkCenter);
            
            if (distanceToChunk > destructionRadius)
            {
                // Rimuovi tutte le monete in questo chunk
                foreach (GameObject coin in chunkData.coins)
                {
                    if (coin != null)
                    {
                        Vector3Int tilePos = tilemap.WorldToCell(coin.transform.position);
                        occupiedPositions.Remove(tilePos);
                        Destroy(coin);
                        currentCoinCount--;
                    }
                }
                
                chunksToRemove.Add(chunkCoord);
            }
        }
        
        // Rimuovi chunk vuoti
        foreach (Vector2Int chunkToRemove in chunksToRemove)
        {
            chunks.Remove(chunkToRemove);
        }
    }
    
    void GenerateNearbyCoins(Vector3 playerPos)
    {
        // Controlla se abbiamo già troppe monete
        if (currentCoinCount >= maxTotalCoins)
        {
            return;
        }
        
        // Calcola range di chunk da controllare
        int minChunkX = Mathf.FloorToInt((playerPos.x - generationRadius) / chunkSize);
        int maxChunkX = Mathf.FloorToInt((playerPos.x + generationRadius) / chunkSize);
        int minChunkY = Mathf.FloorToInt((playerPos.y - generationRadius) / chunkSize);
        int maxChunkY = Mathf.FloorToInt((playerPos.y + generationRadius) / chunkSize);
        
        int coinsGeneratedThisUpdate = 0;
        
        // Lista di chunk da processare, ordinata per distanza
        List<Vector2Int> chunksToProcess = new List<Vector2Int>();
        
        for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
        {
            for (int chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
            {
                Vector2Int chunkCoord = new Vector2Int(chunkX, chunkY);
                
                // Se il chunk non esiste ancora o non è completamente generato
                if (!chunks.ContainsKey(chunkCoord) || !chunks[chunkCoord].isFullyGenerated)
                {
                    chunksToProcess.Add(chunkCoord);
                }
            }
        }
        
        // Ordina per distanza dal player
        chunksToProcess.Sort((a, b) => {
            Vector3 posA = new Vector3(a.x * chunkSize, a.y * chunkSize, 0);
            Vector3 posB = new Vector3(b.x * chunkSize, b.y * chunkSize, 0);
            float distA = Vector3.Distance(playerPos, posA);
            float distB = Vector3.Distance(playerPos, posB);
            return distA.CompareTo(distB);
        });
        
        // Processa chunk in ordine di distanza
        foreach (Vector2Int chunkCoord in chunksToProcess)
        {
            coinsGeneratedThisUpdate += GenerateCoinsInChunk(chunkCoord, playerPos);
            
            // Limita generazione per update
            if (coinsGeneratedThisUpdate >= maxCoinsPerUpdate || currentCoinCount >= maxTotalCoins)
            {
                break;
            }
        }
    }
    
    int GenerateCoinsInChunk(Vector2Int chunkCoord, Vector3 playerPos)
    {
        // Calcola bounds del chunk
        float chunkWorldX = chunkCoord.x * chunkSize;
        float chunkWorldY = chunkCoord.y * chunkSize;
        
        Vector3Int minCell = tilemap.WorldToCell(new Vector3(chunkWorldX, chunkWorldY, 0));
        Vector3Int maxCell = tilemap.WorldToCell(new Vector3(chunkWorldX + chunkSize, chunkWorldY + chunkSize, 0));
        
        // Ottieni o crea chunk data
        if (!chunks.ContainsKey(chunkCoord))
        {
            chunks[chunkCoord] = new ChunkData();
        }
        
        ChunkData chunkData = chunks[chunkCoord];
        List<Vector3Int> validPositions = GetValidPositionsInArea(minCell, maxCell);
        int coinsGenerated = 0;
        
        foreach (Vector3Int position in validPositions)
        {
            // Controlla se abbiamo raggiunto il limite
            if (currentCoinCount >= maxTotalCoins)
            {
                break;
            }
            
            // Controlla se la posizione è già stata processata in questo chunk
            if (chunkData.generatedPositions.Contains(position))
                continue;
                
            // Marca come processata anche se non genera una moneta
            chunkData.generatedPositions.Add(position);
            
            // Controlla se la posizione è già occupata globalmente
            if (occupiedPositions.Contains(position))
                continue;
            
            Vector3 worldPos = tilemap.CellToWorld(position) + spawnOffset;
            
            // Controlla se è nel raggio di generazione
            if (Vector3.Distance(worldPos, playerPos) > generationRadius)
                continue;
            
            // IMPORTANTE: Controlla se è nel campo visivo del player
            if (IsInCameraView(worldPos, playerPos))
                continue; // Skip se è visibile
            
            // Probabilità di spawn
            if (Random.value <= coinDensity)
            {
                GameObject coin = Instantiate(coinPrefab, worldPos, Quaternion.identity);
                coin.transform.SetParent(this.transform);
                
                SetupCoinOptimization(coin);
                
                chunkData.AddCoin(coin, position);
                occupiedPositions.Add(position);
                currentCoinCount++;
                coinsGenerated++;
            }
        }
        
        // Marca il chunk come completamente generato
        chunkData.isFullyGenerated = true;
        
        return coinsGenerated;
    }
    
    bool IsInCameraView(Vector3 worldPos, Vector3 playerPos)
    {
        if (playerCamera == null) return false;
        
        // Calcola i bounds della camera con buffer
        float bufferedCameraHeight = cameraSize * cameraBufferMultiplier + visibilityBuffer;
        float bufferedCameraWidth = bufferedCameraHeight * cameraAspect;
        
        Vector3 cameraPos = playerCamera.transform.position;
        
        // Controlla se il punto è dentro i bounds della camera (con buffer)
        bool inHorizontalBounds = Mathf.Abs(worldPos.x - cameraPos.x) <= bufferedCameraWidth;
        bool inVerticalBounds = Mathf.Abs(worldPos.y - cameraPos.y) <= bufferedCameraHeight;
        
        return inHorizontalBounds && inVerticalBounds;
    }
    
    List<Vector3Int> GetValidPositionsInArea(Vector3Int minCell, Vector3Int maxCell)
    {
        List<Vector3Int> validPositions = new List<Vector3Int>();
        
        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase tileAtPosition = tilemap.GetTile(position);
                
                if (tileAtPosition == targetTile)
                {
                    validPositions.Add(position);
                }
            }
        }
        
        return validPositions;
    }
    
    void UpdateCoinCount()
    {
        // Riconteggia le monete attive (per sicurezza)
        int actualCount = 0;
        foreach (var chunk in chunks.Values)
        {
            // Rimuovi monete null dalla lista
            chunk.coins.RemoveAll(coin => coin == null);
            actualCount += chunk.coins.Count;
        }
        currentCoinCount = actualCount;
    }
    
    void SetupCoinOptimization(GameObject coin)
    {
        coin.tag = "Coin";
        
        // Animazione semplice
        SimpleCoinAnimation animScript = coin.GetComponent<SimpleCoinAnimation>();
        if (animScript == null)
        {
            animScript = coin.AddComponent<SimpleCoinAnimation>();
        }
        
        // Collider
        if (coin.GetComponent<Collider>() == null)
        {
            SphereCollider collider = coin.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
        }
        
        // Collector
        if (coin.GetComponent<DynamicCoinCollector>() == null)
        {
            coin.AddComponent<DynamicCoinCollector>();
        }
    }
    
    public void RemoveCoin(GameObject coin, Vector3Int tilePosition)
    {
        occupiedPositions.Remove(tilePosition);
        currentCoinCount--;
        
        // Trova e rimuovi dai chunk
        foreach (var chunk in chunks.Values)
        {
            if (chunk.RemoveCoin(coin, tilePosition))
            {
                break;
            }
        }
    }
    
    [ContextMenu("Force Update")]
    public void ForceUpdate()
    {
        if (playerTransform != null)
        {
            UpdateCoinGeneration();
        }
    }
    
    [ContextMenu("Clear All Coins")]
    public void ClearAllCoins()
    {
        foreach (var chunk in chunks.Values)
        {
            chunk.Clear();
        }
        
        chunks.Clear();
        occupiedPositions.Clear();
        currentCoinCount = 0;
    }
    
    [ContextMenu("Debug Info")]
    public void DebugInfo()
    {
        Debug.Log($"Chunks attivi: {chunks.Count}");
        Debug.Log($"Posizioni occupate: {occupiedPositions.Count}");
        Debug.Log($"Monete contate: {currentCoinCount}");
        
        int actualCoins = 0;
        foreach (var chunk in chunks.Values)
        {
            actualCoins += chunk.coins.Count;
        }
        Debug.Log($"Monete reali: {actualCoins}");
    }
    
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        
        Vector3 playerPos = playerTransform.position;
        
        // Area di generazione
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerPos, generationRadius);
        
        // Area di distruzione
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerPos, destructionRadius);
        
        // Campo visivo camera (con buffer)
        if (playerCamera != null)
        {
            Gizmos.color = Color.blue;
            float bufferedHeight = cameraSize * cameraBufferMultiplier + visibilityBuffer;
            float bufferedWidth = bufferedHeight * cameraAspect;
            Vector3 cameraPos = playerCamera.transform.position;
            Gizmos.DrawWireCube(cameraPos, new Vector3(bufferedWidth * 2, bufferedHeight * 2, 1));
        }
        
        // Griglia chunk
        Gizmos.color = Color.yellow;
        int range = Mathf.CeilToInt(generationRadius / chunkSize) + 1;
        
        int playerChunkX = Mathf.FloorToInt(playerPos.x / chunkSize);
        int playerChunkY = Mathf.FloorToInt(playerPos.y / chunkSize);
        
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                Vector3 chunkCenter = new Vector3(
                    (playerChunkX + x) * chunkSize + chunkSize * 0.5f,
                    (playerChunkY + y) * chunkSize + chunkSize * 0.5f,
                    playerPos.z
                );
                
                // Colora diversamente i chunk generati
                Vector2Int chunkCoord = new Vector2Int(playerChunkX + x, playerChunkY + y);
                if (chunks.ContainsKey(chunkCoord))
                {
                    Gizmos.color = chunks[chunkCoord].isFullyGenerated ? Color.green : Color.yellow;
                }
                else
                {
                    Gizmos.color = Color.gray;
                }
                
                Gizmos.DrawWireCube(chunkCenter, Vector3.one * chunkSize);
            }
        }
    }
}

// Script di animazione invariato
public class SimpleCoinAnimation : MonoBehaviour
{
    [Header("Animation")]
    public float rotationSpeed = 180f;
    public float bounceHeight = 0.2f;
    public float bounceSpeed = 2f;
    
    private Vector3 startPosition;
    private float randomOffset;
    
    void Start()
    {
        startPosition = transform.position;
        randomOffset = Random.Range(0f, Mathf.PI * 2f);
    }
    
    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        
        float bounce = Mathf.Sin(Time.time * bounceSpeed + randomOffset) * bounceHeight;
        transform.position = startPosition + Vector3.up * bounce;
    }
}

// Collector invariato
public class DynamicCoinCollector : MonoBehaviour
{
    public int coinValue = 1;
    public AudioClip collectSound;
    public GameObject collectEffect;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CollectCoin();
        }
    }
    
    void CollectCoin()
    {
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
        
        DynamicCoinGenerator generator = FindObjectOfType<DynamicCoinGenerator>();
        if (generator != null)
        {
            Vector3Int tilePos = generator.tilemap.WorldToCell(transform.position);
            generator.RemoveCoin(gameObject, tilePos);
        }
        
        Destroy(gameObject);
    }
}