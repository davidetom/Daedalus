using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemSpawner : MonoBehaviour
{
    [Header("Debug")]
    public bool enableDebug = false;

    [Header("References")]
    public MapManager mapManager;
    public DayNightCycleManager dayNightManager;
    public PlayerController playerController;

    [Header("Gem Prefabs")]
    public GameObject yellowGemPrefab; // Gemma del Sole
    public GameObject blueGemPrefab;   // Gemma della Notte
    public GameObject greenGemPrefab;  // Gemma degli Zombie (droppata dai nemici)
    public GameObject redGemPrefab;    // Gemma del Sangue (dopo 5 morti)
    public GameObject grayGemPrefab;   // Gemma della Nebbia (visibile solo con powerup)

    [Header("Center of Labyrinth")]
    public Vector3 labyrinthCenter = new Vector3(155f, 155f, 0f);

    [Header("Yellow Gem Settings (Sun)")]
    [Range(10, 200)]
    public int yellowGemMinDistance = 30;
    [Range(10, 200)]
    public int yellowGemMaxDistance = 100;
    public int maxYellowGemsOnMap = 3;
    public float yellowGemSpawnDelay = 5f; // Delay dall'inizio del giorno

    [Header("Blue Gem Settings (Night)")]
    [Range(10, 200)]
    public int blueGemMinDistance = 80;
    [Range(10, 200)]
    public int blueGemMaxDistance = 130;
    public int maxBlueGemsOnMap = 2;
    public float blueGemSpawnDelay = 3f; // Delay dall'inizio della notte

    [Header("Green Gem Settings (Zombie)")]
    [Range(0f, 1f)]
    public float greenGemDropChance = 0.15f; // 15% di probabilità

    [Header("Red Gem Settings (Blood)")]
    public int deathsRequiredForRedGem = 5;
    public Vector3 redGemSpawnPosition = new Vector3(200f, 200f, 0f); // Posizione fissa
    private int currentPlayerDeaths = 0;
    private bool redGemSpawned = false;

    [Header("Gray Gem Settings (Fog)")]
    [Range(10, 200)]
    public int grayGemMinDistance = 130;
    [Range(10, 200)]
    public int grayGemMaxDistance = 150;
    public bool fogVisibilityUnlocked = false; // Settato da un powerup
    private bool grayGemSpawned = false;

    [Header("Spawn Settings")]
    public float spawnCheckInterval = 1f; // Intervallo controllo spawn
    public int maxSpawnAttempts = 50; // Max tentativi per trovare posizione valida

    [Header("Gem Collection Tracking")]
    private bool yellowGemCollected = false;
    private bool blueGemCollected = false;
    private bool greenGemCollected = false;
    private bool grayGemCollected = false;
    private bool redGemCollected = false;

    // State tracking
    private List<GameObject> activeYellowGems = new List<GameObject>();
    private List<GameObject> activeBlueGems = new List<GameObject>();
    private List<GameObject> activeGrayGems = new List<GameObject>();
    private Coroutine spawnCoroutine;

    void Start()
    {
        InitializeReferences();
        StartSpawnCoroutine();
        
        // Sottoscrivi agli eventi del ciclo giorno/notte
        if (dayNightManager != null)
        {
            dayNightManager.events.OnDayStart.AddListener(OnDayStart);
            dayNightManager.events.OnNightStart.AddListener(OnNightStart);
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
    }

    void StartSpawnCoroutine()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        
        spawnCoroutine = StartCoroutine(SpawnCheckRoutine());
    }

    IEnumerator SpawnCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnCheckInterval);
            
            // Pulisci reference a gemme distrutte
            CleanupDestroyedGems();
            
            // Controlla spawn gemma grigia se visibilità sbloccata
            if (fogVisibilityUnlocked && !grayGemSpawned && !grayGemCollected)
            {
                SpawnGrayGem();
            }
        }
    }

    void CleanupDestroyedGems()
    {
        activeYellowGems.RemoveAll(gem => gem == null);
        activeBlueGems.RemoveAll(gem => gem == null);
        activeGrayGems.RemoveAll(gem => gem == null);
    }

    #region Day/Night Event Handlers

    void OnDayStart()
    {
        if (enableDebug)
            Debug.Log("GemSpawner: Giorno iniziato - rimuovendo gemme blu e spawnando gemme gialle");
        
        // Rimuovi tutte le gemme blu quando inizia il giorno
        DestroyAllGems(activeBlueGems);
        
        // Spawna gemme gialle se non sono state raccolte
        if (!yellowGemCollected)
            StartCoroutine(SpawnYellowGemsWithDelay());
    }

    void OnNightStart()
    {
        if (enableDebug)
            Debug.Log("GemSpawner: Notte iniziata - rimuovendo gemme gialle e spawnando gemme blu");
        
        // Rimuovi tutte le gemme gialle quando inizia la notte
        DestroyAllGems(activeYellowGems);
        
        // Spawna gemme blu se non sono state raccolte
        if (!blueGemCollected)
            StartCoroutine(SpawnBlueGemsWithDelay());
    }

    #endregion

    #region Yellow Gem Spawning (Sun)

    IEnumerator SpawnYellowGemsWithDelay()
    {
        yield return new WaitForSeconds(yellowGemSpawnDelay);
        SpawnYellowGems();
    }

    void SpawnYellowGems()
    {
        if (yellowGemPrefab == null || playerController == null || mapManager == null || yellowGemCollected)
        {
            if (enableDebug && yellowGemCollected)
                Debug.Log("GemSpawner: Gemma gialla già raccolta - skip spawn");
            return;
        }

        // Spawna fino al massimo consentito
        int gemsToSpawn = maxYellowGemsOnMap - activeYellowGems.Count;
        
        for (int i = 0; i < gemsToSpawn; i++)
        {
            Vector3 spawnPos = FindValidGemSpawnPositionInRadius(yellowGemMinDistance, yellowGemMaxDistance);
            
            if (spawnPos != Vector3.zero)
            {
                GameObject gem = Instantiate(yellowGemPrefab, spawnPos, Quaternion.identity);
                activeYellowGems.Add(gem);
                
                if (enableDebug)
                    Debug.Log($"GemSpawner: Gemma gialla spawnata a {spawnPos}");
            }
            else
            {
                if (enableDebug)
                    Debug.LogWarning("GemSpawner: Impossibile trovare posizione valida per gemma gialla");
            }
        }
    }

    #endregion

    #region Blue Gem Spawning (Night)

    IEnumerator SpawnBlueGemsWithDelay()
    {
        yield return new WaitForSeconds(blueGemSpawnDelay);
        SpawnBlueGems();
    }

    void SpawnBlueGems()
    {
        if (blueGemPrefab == null || playerController == null || mapManager == null || blueGemCollected)
        {
            if (enableDebug && blueGemCollected)
                Debug.Log("GemSpawner: Gemma blu già raccolta - skip spawn");
            return;
        }

        // Spawna fino al massimo consentito
        int gemsToSpawn = maxBlueGemsOnMap - activeBlueGems.Count;
        
        for (int i = 0; i < gemsToSpawn; i++)
        {
            Vector3 spawnPos = FindValidGemSpawnPositionInRadius(blueGemMinDistance, blueGemMaxDistance);
            
            if (spawnPos != Vector3.zero)
            {
                GameObject gem = Instantiate(blueGemPrefab, spawnPos, Quaternion.identity);
                activeBlueGems.Add(gem);
                
                if (enableDebug)
                    Debug.Log($"GemSpawner: Gemma blu spawnata a {spawnPos}");
            }
            else
            {
                if (enableDebug)
                    Debug.LogWarning("GemSpawner: Impossibile trovare posizione valida per gemma blu");
            }
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
                Debug.Log("GemSpawner: Gemma verde già raccolta - skip drop");
            return;
        }

        if (Random.Range(0f, 1f) <= greenGemDropChance)
        {
            // Trova una posizione valida vicino al nemico (solo su tile corridoio)
            Vector3 dropPosition = FindNearbyValidGemPosition(enemyPosition, 3); // Entro 3 tile
            
            if (dropPosition != Vector3.zero)
            {
                GameObject gem = Instantiate(greenGemPrefab, dropPosition, Quaternion.identity);
                
                if (enableDebug)
                    Debug.Log($"GemSpawner: Gemma verde droppata a {dropPosition} dal nemico in {enemyPosition}");
            }
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
        GameObject gem = Instantiate(redGemPrefab, redGemSpawnPosition, Quaternion.identity);
        redGemSpawned = true;
        
        if (enableDebug)
            Debug.Log($"GemSpawner: Gemma rossa spawnata alla posizione fissa {redGemSpawnPosition} dopo {currentPlayerDeaths} morti");
    }

    #endregion

    #region Gray Gem Spawning (Fog)

    /// <summary>
    /// Chiamato quando si sblocca la visibilità della nebbia
    /// </summary>
    public void UnlockFogVisibility()
    {
        fogVisibilityUnlocked = true;
        
        if (enableDebug)
            Debug.Log("GemSpawner: Visibilità nebbia sbloccata");
    }

    void SpawnGrayGem()
    {
        if (grayGemPrefab == null || playerController == null || mapManager == null || grayGemCollected)
        {
            if (enableDebug && grayGemCollected)
                Debug.Log("GemSpawner: Gemma grigia già raccolta - skip spawn");
            return;
        }

        Vector3 spawnPos = FindValidGemSpawnPositionInRadius(grayGemMinDistance, grayGemMaxDistance);
        
        if (spawnPos != Vector3.zero)
        {
            GameObject gem = Instantiate(grayGemPrefab, spawnPos, Quaternion.identity);
            activeGrayGems.Add(gem);
            grayGemSpawned = true;
            
            if (enableDebug)
                Debug.Log($"GemSpawner: Gemma grigia spawnata a {spawnPos}");
        }
        else
        {
            if (enableDebug)
                Debug.LogWarning("GemSpawner: Impossibile trovare posizione per gemma grigia");
        }
    }

    #endregion

    #region Position Finding

    /// <summary>
    /// Trova una posizione valida per spawning gemme in un raggio dal centro del labirinto
    /// </summary>
    Vector3 FindValidGemSpawnPositionInRadius(int minDistance, int maxDistance)
    {
        if (mapManager == null || !mapManager.wallCalculated)
            return Vector3.zero;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
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
                IsValidCorridorTile(arrayPos)) // Solo su tile corridoio
            {
                // Verifica distanza effettiva dal centro del labirinto
                float actualDistance = Vector3.Distance(labyrinthCenter, snappedPos);
                if (actualDistance >= minDistance && actualDistance <= maxDistance)
                {
                    return snappedPos;
                }
            }
        }

        return Vector3.zero;
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
                IsValidCorridorTile(arrayPos))
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
            IsValidCorridorTile(fallbackArrayPos))
        {
            return fallbackPos;
        }

        return Vector3.zero; // Nessuna posizione valida trovata
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
        DestroyAllGems(activeYellowGems);
        
        if (enableDebug)
            Debug.Log("GemSpawner: Gemma gialla raccolta - non spawneranno più gemme gialle");
    }

    /// <summary>
    /// Chiamato quando il player raccoglie una gemma blu
    /// </summary>
    public void OnBlueGemCollected()
    {
        blueGemCollected = true;
        DestroyAllGems(activeBlueGems);
        
        if (enableDebug)
            Debug.Log("GemSpawner: Gemma blu raccolta - non spawneranno più gemme blu");
    }

    /// <summary>
    /// Chiamato quando il player raccoglie una gemma verde
    /// </summary>
    public void OnGreenGemCollected()
    {
        greenGemCollected = true;
        
        if (enableDebug)
            Debug.Log("GemSpawner: Gemma verde raccolta - non spawneranno più gemme verdi");
    }

    /// <summary>
    /// Chiamato quando il player raccoglie una gemma grigia
    /// </summary>
    public void OnGrayGemCollected()
    {
        grayGemCollected = true;
        DestroyAllGems(activeGrayGems);
        
        if (enableDebug)
            Debug.Log("GemSpawner: Gemma grigia raccolta - non spawneranno più gemme grigie");
    }

    /// <summary>
    /// Chiamato quando il player raccoglie una gemma rossa
    /// </summary>
    public void OnRedGemCollected()
    {
        redGemCollected = true;
        
        if (enableDebug)
            Debug.Log("GemSpawner: Gemma rossa raccolta - non spawneranno più gemme rosse");
    }

    #endregion

    #region Utility Methods

    void DestroyAllGems(List<GameObject> gemList)
    {
        for (int i = gemList.Count - 1; i >= 0; i--)
        {
            if (gemList[i] != null)
            {
                Destroy(gemList[i]);
            }
        }
        gemList.Clear();
    }

    /// <summary>
    /// Reset dello spawner per nuovo gioco
    /// </summary>
    public void ResetSpawner()
    {
        currentPlayerDeaths = 0;
        redGemSpawned = false;
        grayGemSpawned = false;
        fogVisibilityUnlocked = false;

        // Reset collection flags
        yellowGemCollected = false;
        blueGemCollected = false;
        greenGemCollected = false;
        grayGemCollected = false;
        redGemCollected = false;

        // Rimuovi tutte le gemme attive
        DestroyAllGems(activeYellowGems);
        DestroyAllGems(activeBlueGems);
        DestroyAllGems(activeGrayGems);

        if (enableDebug)
            Debug.Log("GemSpawner: Reset completato");
    }

    /// <summary>
    /// Ottieni statistiche correnti dello spawner
    /// </summary>
    public string GetSpawnerStats()
    {
        return $"Gemme Attive - Gialle: {activeYellowGems.Count}/{maxYellowGemsOnMap}, " +
               $"Blu: {activeBlueGems.Count}/{maxBlueGemsOnMap}, " +
               $"Grigie: {activeGrayGems.Count}, " +
               $"Morti Player: {currentPlayerDeaths}/{deathsRequiredForRedGem}, " +
               $"Gemma Rossa: {(redGemSpawned ? "Spawnata" : "Non Spawnata")}, " +
               $"Visibilità Nebbia: {(fogVisibilityUnlocked ? "Sbloccata" : "Bloccata")}, " +
               $"Gemme Raccolte - G:{yellowGemCollected}, B:{blueGemCollected}, V:{greenGemCollected}, Gr:{grayGemCollected}, R:{redGemCollected}";
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
        Gizmos.DrawWireCube(redGemSpawnPosition, Vector3.one * 2f);

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
        UnityEditor.Handles.Label(redGemSpawnPosition + Vector3.up * 2f, "Red Gem Position", labelStyle);
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
    }
}