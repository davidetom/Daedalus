using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemySpawner : MonoBehaviour
{
    [Header("Configurazione Spawn")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int maxEnemies = 15;
    public float spawnInterval = 2f;
    public bool spawnRandomly = true;
    
    [Header("Integrazione SpawnPoint Generator")]
    [SerializeField] private bool useGeneratedSpawnPoints = true;
    [SerializeField] private Vector3[] generatedSpawnPositions;
    
    [Header("Parametri Nemici")]
    public float enemySpeed = 3f;
    public float enemyDetectionRadius = 8f;
    public float enemyDamage = 10f;
    
    [Header("Target")]
    public Transform playerTarget;
    
    [Header("Distance-Based Spawning")]
    [Tooltip("Distanza minima dal player per lo spawn iniziale (in tile BFS)")]
    public int initialSpawnMinDistance = 20;
    [Tooltip("Distanza massima dal player per lo spawn iniziale (in tile BFS)")]
    public int initialSpawnMaxDistance = 100;
    [Tooltip("Distanza minima dal player per lo spawn continuo (in tile BFS)")]
    public int continuousSpawnMinDistance = 10;
    [Tooltip("Distanza massima dal player per lo spawn continuo (in tile BFS)")]
    public int continuousSpawnMaxDistance = 30;
    [Tooltip("Riferimento al MapManager per accedere alle distanze BFS")]
    public MapManager mapManager;
    [Tooltip("Abilita debug per visualizzare informazioni di spawning")]
    public bool enableSpawnDebug = false;
    
    [Header("Respawn Settings")]
    [Tooltip("Abilita il respawn automatico quando i nemici vengono uccisi")]
    public bool enableRespawn = true;
    [Tooltip("Tempo di attesa prima di respawnare un nemico dopo la sua morte")]
    public float respawnDelay = 3f;
    [Tooltip("Numero minimo di nemici da mantenere attivi (se possibile)")]
    public int minActiveEnemies = 3;
    
    // Lista nemici attivi
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isSpawning = false;
    
    // Cache per ottimizzazione
    private List<SpawnPointInfo> validSpawnPoints = new List<SpawnPointInfo>();
    private float lastDistanceUpdateTime = 0f;
    private float distanceUpdateInterval = 1f; // Aggiorna ogni secondo
    
    // Proprietà pubbliche
    public int GetActiveEnemyCount() => activeEnemies.Count;
    public bool IsSpawning => isSpawning;
    
    void Start()
    {
        // Trova il player se non assegnato
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) 
                playerTarget = playerObj.transform;
        }
        
        // Trova il MapManager se non assegnato
        if (mapManager == null)
        {
            mapManager = FindFirstObjectByType<MapManager>();
        }
        
        // Setup punti di spawn
        SetupSpawnPoints();
    }
    
    void SetupSpawnPoints()
    {
        if (useGeneratedSpawnPoints && generatedSpawnPositions != null && generatedSpawnPositions.Length > 0)
        {
            // Usa le posizioni generate
            CreateTransformsFromPositions();
            Debug.Log($"🎯 Utilizzando {generatedSpawnPositions.Length} spawn points generati");
        }
        else
        {
            // Usa il metodo originale
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("EnemySpawn");
                spawnPoints = new Transform[spawnObjects.Length];
                for (int i = 0; i < spawnObjects.Length; i++)
                {
                    spawnPoints[i] = spawnObjects[i].transform;
                }
            }
            Debug.Log($"🎯 Utilizzando {spawnPoints.Length} spawn points tradizionali");
        }
    }
    
    void CreateTransformsFromPositions()
    {
        // Crea Transform temporanei dalle posizioni generate
        spawnPoints = new Transform[generatedSpawnPositions.Length];
        
        for (int i = 0; i < generatedSpawnPositions.Length; i++)
        {
            // Crea un GameObject temporaneo per ogni posizione
            GameObject spawnObj = new GameObject($"GeneratedSpawn_{i + 1}");
            spawnObj.transform.position = generatedSpawnPositions[i];
            spawnObj.transform.SetParent(this.transform); // Organizza sotto questo GameObject
            spawnPoints[i] = spawnObj.transform;
        }
    }
    
    [ContextMenu("Copia Posizioni da SpawnPointGenerator")]
    public void CopiaPosizioniDaGenerator()
    {
        SpawnPointGenerator generator = FindFirstObjectByType<SpawnPointGenerator>();
        
        if (generator == null)
        {
            Debug.LogError("❌ Nessun SpawnPointGenerator trovato nella scena!");
            return;
        }
        
        List<SpawnPoint> spawnPointsList = generator.GetSpawnPoints();
        
        if (spawnPointsList.Count == 0)
        {
            Debug.LogWarning("⚠️ Nessun spawn point disponibile nel generator! Genera prima gli spawn points.");
            return;
        }
        
        // Copia le posizioni nell'array
        generatedSpawnPositions = new Vector3[spawnPointsList.Count];
        
        for (int i = 0; i < spawnPointsList.Count; i++)
        {
            generatedSpawnPositions[i] = spawnPointsList[i].position;
        }
        
        // Attiva l'uso delle posizioni generate
        useGeneratedSpawnPoints = true;
        
        Debug.Log($"✅ Copiate {generatedSpawnPositions.Length} posizioni dal SpawnPointGenerator");
        
        // Se siamo in play mode, ricrea i punti di spawn
        if (Application.isPlaying)
        {
            SetupSpawnPoints();
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    [ContextMenu("Pulisci Posizioni Generate")]
    public void PulisciPosizioniGenerate()
    {
        generatedSpawnPositions = new Vector3[0];
        useGeneratedSpawnPoints = false;
        
        // Rimuovi i GameObject temporanei se esistono
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child != this.transform && child.name.StartsWith("GeneratedSpawn_"))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
        
        Debug.Log("🧹 Posizioni generate pulite");
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    void Update()
    {
        // Rimuovi riferimenti a nemici distrutti e gestisci respawn
        HandleDeadEnemies();
        
        // Aggiorna periodicamente la cache dei punti di spawn validi
        if (Time.time - lastDistanceUpdateTime > distanceUpdateInterval)
        {
            UpdateValidSpawnPointsCache();
            lastDistanceUpdateTime = Time.time;
        }
        
        // Controlla se serve respawnare nemici per mantenere il numero minimo
        if (enableRespawn && isSpawning)
        {
            CheckAndTriggerRespawn();
        }
    }
    
    /// <summary>
    /// Gestisce la rimozione dei nemici morti e avvia il processo di respawn
    /// </summary>
    void HandleDeadEnemies()
    {
        int enemiesBeforeCleanup = activeEnemies.Count;
        
        // Rimuovi riferimenti a nemici distrutti
        activeEnemies.RemoveAll(enemy => enemy == null);
        
        int enemiesKilled = enemiesBeforeCleanup - activeEnemies.Count;
        
        // Se alcuni nemici sono stati uccisi e il respawn è abilitato
        if (enemiesKilled > 0 && enableRespawn && isSpawning)
        {
            if (enableSpawnDebug)
                Debug.Log($"💀 {enemiesKilled} nemici uccisi. Avvio respawn con delay di {respawnDelay}s");
            
            // Avvia il respawn per ogni nemico ucciso
            for (int i = 0; i < enemiesKilled; i++)
            {
                StartCoroutine(RespawnEnemyAfterDelay());
            }
        }
    }
    
    /// <summary>
    /// Controlla se il numero di nemici attivi è sotto il minimo e triggera il respawn se necessario
    /// </summary>
    void CheckAndTriggerRespawn()
    {
        if (activeEnemies.Count < minActiveEnemies && activeEnemies.Count < maxEnemies)
        {
            int enemiesToSpawn = Mathf.Min(minActiveEnemies - activeEnemies.Count, maxEnemies - activeEnemies.Count);
            
            if (enableSpawnDebug)
                Debug.Log($"📈 Sotto il minimo di nemici ({activeEnemies.Count}/{minActiveEnemies}). Spawning {enemiesToSpawn} nemici immediati.");
            
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                // Usa spawn continuo per mantenere il numero minimo
                SpawnEnemyAtDistanceRange(continuousSpawnMinDistance, continuousSpawnMaxDistance, false);
            }
        }
    }
    
    /// <summary>
    /// Coroutine per respawnare un nemico dopo un delay
    /// </summary>
    IEnumerator RespawnEnemyAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        
        // Controlla se siamo ancora in modalità spawning e sotto il limite massimo
        if (isSpawning && activeEnemies.Count < maxEnemies)
        {
            bool spawned = SpawnEnemyAtDistanceRange(continuousSpawnMinDistance, continuousSpawnMaxDistance, false);
            
            if (enableSpawnDebug)
            {
                if (spawned)
                    Debug.Log("🔄 Nemico respawnato con successo");
                else
                    Debug.LogWarning("⚠️ Impossibile respawnare nemico - nessun punto valido");
            }
        }
    }
    void UpdateValidSpawnPointsCache()
    {
        if (mapManager == null || !mapManager.wallCalculated || playerTarget == null)
        {
            if (enableSpawnDebug)
                Debug.LogWarning("MapManager non disponibile o player non trovato per aggiornamento cache spawn");
            return;
        }
        
        validSpawnPoints.Clear();
        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;
            
            Vector3 spawnPos = spawnPoints[i].position;
            Vector2Int arrayPos = mapManager.WorldToArrayCoordinates(spawnPos);
            
            // Verifica che la posizione sia valida
            if (!mapManager.IsValidArrayCoordinate(arrayPos))
                continue;
            
            // Verifica che sia camminabile per l'AI (solo corridoi)
            if (!mapManager.IsWalkableForAI(arrayPos))
                continue;
            
            // Ottieni la distanza BFS
            int distance = mapManager.Distances[arrayPos.x, arrayPos.y];
            
            // Aggiungi alla cache se la distanza è valida
            if (distance >= 0)
            {
                validSpawnPoints.Add(new SpawnPointInfo
                {
                    index = i,
                    position = spawnPos,
                    distance = distance
                });
            }
        }
        
        if (enableSpawnDebug)
        {
            Debug.Log($"Cache spawn aggiornata: {validSpawnPoints.Count}/{spawnPoints.Length} punti validi");
        }
    }
    
    /// <summary>
    /// Ottiene una lista di punti di spawn validi entro un range di distanza specificato
    /// </summary>
    List<SpawnPointInfo> GetValidSpawnPointsInRange(int minDistance, int maxDistance)
    {
        return validSpawnPoints.Where(sp => sp.distance >= minDistance && sp.distance <= maxDistance).ToList();
    }
    
    public void SpawnNightEnemies()
    {
        if (isSpawning) return;
        
        Debug.Log("🔥 Inizio spawn nemici notturni");
        isSpawning = true;
        
        // Aggiorna immediatamente la cache
        UpdateValidSpawnPointsCache();
        
        // Spawna immediatamente alcuni nemici a distanza iniziale
        int initialSpawn = Mathf.Min(maxEnemies / 2, spawnPoints.Length);
        
        for (int i = 0; i < initialSpawn; i++)
        {
            if (!SpawnEnemyAtDistanceRange(initialSpawnMinDistance, initialSpawnMaxDistance, true))
            {
                if (enableSpawnDebug)
                    Debug.LogWarning($"Impossibile spawnare nemico iniziale {i+1}/{initialSpawn}");
            }
        }
        
        // Avvia lo spawn continuo
        if (spawnRandomly)
        {
            StartCoroutine(SpawnEnemiesOverTime());
        }
    }
    
    IEnumerator SpawnEnemiesOverTime()
    {
        while (isSpawning && activeEnemies.Count < maxEnemies)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if (activeEnemies.Count < maxEnemies)
            {
                // Usa i parametri di spawn continuo
                if (!SpawnEnemyAtDistanceRange(continuousSpawnMinDistance, continuousSpawnMaxDistance, false))
                {
                    if (enableSpawnDebug)
                        Debug.LogWarning("Impossibile spawnare nemico continuo - nessun punto valido trovato");
                }
            }
        }
    }
    
    /// <summary>
    /// Spawna un nemico in un punto casuale entro il range di distanza specificato
    /// </summary>
    bool SpawnEnemyAtDistanceRange(int minDistance, int maxDistance, bool isInitialSpawn)
    {
        if (spawnPoints.Length == 0 || enemyPrefab == null)
        {
            Debug.LogWarning("Nessun punto di spawn o prefab nemico configurato!");
            return false;
        }
        
        // Ottieni punti di spawn validi nel range di distanza
        List<SpawnPointInfo> availableSpawns = GetValidSpawnPointsInRange(minDistance, maxDistance);
        
        if (availableSpawns.Count == 0)
        {
            if (enableSpawnDebug)
            {
                string spawnType = isInitialSpawn ? "iniziale" : "continuo";
                Debug.LogWarning($"Nessun punto di spawn {spawnType} trovato nel range {minDistance}-{maxDistance} tile dal player");
                
                // Debug: mostra le distanze disponibili
                var allDistances = validSpawnPoints.Select(sp => sp.distance).Distinct().OrderBy(d => d);
                Debug.Log($"Distanze disponibili: [{string.Join(", ", allDistances)}]");
            }
            return false;
        }
        
        // Scegli un punto casuale tra quelli disponibili
        SpawnPointInfo chosenSpawn = availableSpawns[UnityEngine.Random.Range(0, availableSpawns.Count)];
        Transform spawnPoint = spawnPoints[chosenSpawn.index];
        
        // Spawna il nemico
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Assicurati che il nemico sia attivo
        if (!newEnemy.activeInHierarchy)
        {
            newEnemy.SetActive(true);
            if (enableSpawnDebug)
                Debug.Log($"Nemico era disattivato, ora attivato: {newEnemy.name}");
        }
        
        activeEnemies.Add(newEnemy);
        
        if (enableSpawnDebug)
        {
            string spawnType = isInitialSpawn ? "iniziale" : "continuo";
            Debug.Log($"👹 Nemico {spawnType} spawnato a distanza {chosenSpawn.distance} tile dal player (posizione {spawnPoint.position})");
        }
        
        return true;
    }
    
    void SpawnEnemyAtRandomPoint()
    {
        // Metodo backward compatible - usa spawn continuo come default
        SpawnEnemyAtDistanceRange(continuousSpawnMinDistance, continuousSpawnMaxDistance, false);
    }
    
    public void ClearAllEnemies()
    {
        Debug.Log($"🧹 Rimozione di {activeEnemies.Count} nemici");
        
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        
        activeEnemies.Clear();
        isSpawning = false;
        StopAllCoroutines();
    }
    
    public void SpawnEnemyAtSpecificPoint(int spawnIndex)
    {
        if (spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
        {
            Debug.LogWarning($"Indice spawn {spawnIndex} non valido!");
            return;
        }
        
        Transform spawnPoint = spawnPoints[spawnIndex];
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        activeEnemies.Add(newEnemy);
        
        if (enableSpawnDebug)
        {
            Debug.Log($"👹 Nemico spawnato manualmente al punto {spawnIndex} (posizione {spawnPoint.position})");
        }
    }
    
    public void SetMaxEnemies(int newMax)
    {
        maxEnemies = newMax;
        
        // Se abbiamo troppi nemici, rimuovi alcuni
        while (activeEnemies.Count > maxEnemies)
        {
            GameObject enemyToRemove = activeEnemies[activeEnemies.Count - 1];
            activeEnemies.RemoveAt(activeEnemies.Count - 1);
            if (enemyToRemove != null)
                Destroy(enemyToRemove);
        }
    }
    
    public void PauseSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }
    
    public void ResumeSpawning()
    {
        if (activeEnemies.Count < maxEnemies)
        {
            isSpawning = true;
            if (spawnRandomly)
                StartCoroutine(SpawnEnemiesOverTime());
        }
    }
    
    // Metodo per ottenere informazioni sui nemici e spawn
    public string GetEnemyStatusInfo()
    {
        string spawnType = useGeneratedSpawnPoints ? "Generated" : "Manual";
        int validSpawnsCount = validSpawnPoints != null ? validSpawnPoints.Count : 0;
        string respawnStatus = enableRespawn ? "ON" : "OFF";
        
        return $"Nemici: {activeEnemies.Count}/{maxEnemies} (min:{minActiveEnemies}) | Spawning: {(isSpawning ? "ATTIVO" : "FERMATO")} | Respawn: {respawnStatus} | Tipo: {spawnType} | Spawn validi: {validSpawnsCount}";
    }
    
    /// <summary>
    /// Metodo di debug per visualizzare le distanze dei punti di spawn
    /// </summary>
    [ContextMenu("Debug Spawn Distances")]
    public void DebugSpawnDistances()
    {
        UpdateValidSpawnPointsCache();
        
        Debug.Log("=== DEBUG SPAWN DISTANCES ===");
        Debug.Log($"Punti di spawn totali: {spawnPoints.Length}");
        Debug.Log($"Punti di spawn validi: {validSpawnPoints.Count}");
        
        // Raggruppa per distanza
        var groupedByDistance = validSpawnPoints.GroupBy(sp => sp.distance).OrderBy(g => g.Key);
        
        foreach (var group in groupedByDistance)
        {
            Debug.Log($"Distanza {group.Key}: {group.Count()} punti");
        }
        
        // Mostra range disponibili
        var initialRange = validSpawnPoints.Where(sp => sp.distance >= initialSpawnMinDistance && sp.distance <= initialSpawnMaxDistance);
        var continuousRange = validSpawnPoints.Where(sp => sp.distance >= continuousSpawnMinDistance && sp.distance <= continuousSpawnMaxDistance);
        
        Debug.Log($"Spawn iniziali ({initialSpawnMinDistance}-{initialSpawnMaxDistance}): {initialRange.Count()} punti disponibili");
        Debug.Log($"Spawn continui ({continuousSpawnMinDistance}-{continuousSpawnMaxDistance}): {continuousRange.Count()} punti disponibili");
    }
    
    // Gizmos per visualizzare i punti di spawn nell'editor
    void OnDrawGizmosSelected()
    {
        // Visualizza spawn points tradizionali
        if (!useGeneratedSpawnPoints && spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform spawnPoint = spawnPoints[i];
                if (spawnPoint == null) continue;
                
                // Colore basato sulla distanza se disponibile
                Color gizmoColor = Color.red;
                if (Application.isPlaying && validSpawnPoints != null)
                {
                    var spawnInfo = validSpawnPoints.FirstOrDefault(sp => sp.index == i);
                    if (spawnInfo != null)
                    {
                        // Verde per spawn iniziali, giallo per continui, rosso per fuori range
                        if (spawnInfo.distance >= initialSpawnMinDistance && spawnInfo.distance <= initialSpawnMaxDistance)
                            gizmoColor = Color.green;
                        else if (spawnInfo.distance >= continuousSpawnMinDistance && spawnInfo.distance <= continuousSpawnMaxDistance)
                            gizmoColor = Color.yellow;
                        else
                            gizmoColor = Color.gray;
                    }
                }
                
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(spawnPoint.position, 1f);
                Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.up * 2f);
                
                #if UNITY_EDITOR
                if (Application.isPlaying && validSpawnPoints != null)
                {
                    var spawnInfo = validSpawnPoints.FirstOrDefault(sp => sp.index == i);
                    if (spawnInfo != null)
                    {
                        UnityEditor.Handles.Label(spawnPoint.position + Vector3.up * 1.5f, 
                            $"D: {spawnInfo.distance}");
                    }
                }
                #endif
            }
        }
        
        // Visualizza posizioni generate
        if (useGeneratedSpawnPoints && generatedSpawnPositions != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < generatedSpawnPositions.Length; i++)
            {
                Vector3 pos = generatedSpawnPositions[i];
                
                // Colore basato sulla distanza se disponibile
                Color gizmoColor = Color.green;
                if (Application.isPlaying && validSpawnPoints != null)
                {
                    var spawnInfo = validSpawnPoints.FirstOrDefault(sp => sp.index == i);
                    if (spawnInfo != null)
                    {
                        if (spawnInfo.distance >= initialSpawnMinDistance && spawnInfo.distance <= initialSpawnMaxDistance)
                            gizmoColor = Color.green;
                        else if (spawnInfo.distance >= continuousSpawnMinDistance && spawnInfo.distance <= continuousSpawnMaxDistance)
                            gizmoColor = Color.yellow;
                        else
                            gizmoColor = Color.gray;
                    }
                }
                
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireCube(pos, Vector3.one);
                
                #if UNITY_EDITOR
                if (Application.isPlaying && validSpawnPoints != null)
                {
                    var spawnInfo = validSpawnPoints.FirstOrDefault(sp => sp.index == i);
                    if (spawnInfo != null)
                    {
                        UnityEditor.Handles.Label(pos + Vector3.up * 0.7f, 
                            $"{i + 1} (D:{spawnInfo.distance})");
                    }
                    else
                    {
                        UnityEditor.Handles.Label(pos + Vector3.up * 0.7f, (i + 1).ToString());
                    }
                }
                else
                {
                    UnityEditor.Handles.Label(pos + Vector3.up * 0.7f, (i + 1).ToString());
                }
                #endif
            }
        }
        
        // Visualizza il range del player se disponibile
        if (Application.isPlaying && playerTarget != null && mapManager != null)
        {
            Vector3 playerPos = playerTarget.position;
            
            // Cerchio per range spawn iniziale (verde)
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawSphere(playerPos, initialSpawnMaxDistance);
            
            // Cerchio per range spawn continuo (giallo)
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Gizmos.DrawSphere(playerPos, continuousSpawnMaxDistance);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(EnemySpawner))]
    public class EnemySpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EnemySpawner spawner = (EnemySpawner)target;
            
            GUILayout.Space(10);
            GUILayout.Label("Controlli SpawnPoint Generator", UnityEditor.EditorStyles.boldLabel);
            
            if (GUILayout.Button("Copia Posizioni da SpawnPointGenerator", GUILayout.Height(25)))
            {
                spawner.CopiaPosizioniDaGenerator();
            }
            
            if (spawner.generatedSpawnPositions != null && spawner.generatedSpawnPositions.Length > 0)
            {
                if (GUILayout.Button("Pulisci Posizioni Generate"))
                {
                    spawner.PulisciPosizioniGenerate();
                }
                
                GUILayout.Space(5);
                GUILayout.Label($"Posizioni Generate: {spawner.generatedSpawnPositions.Length}", UnityEditor.EditorStyles.helpBox);
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Controlli Spawn", UnityEditor.EditorStyles.boldLabel);
            
            if (Application.isPlaying)
            {
                if (GUILayout.Button("Spawn Nemici Notturni"))
                {
                    spawner.SpawnNightEnemies();
                }
                
                if (GUILayout.Button("Pulisci Tutti i Nemici"))
                {
                    spawner.ClearAllEnemies();
                }
                
                if (GUILayout.Button("Debug Distanze Spawn"))
                {
                    spawner.DebugSpawnDistances();
                }
                
                GUILayout.Space(5);
                GUILayout.Label(spawner.GetEnemyStatusInfo(), UnityEditor.EditorStyles.helpBox);
                
                // Informazioni sulla cache
                if (spawner.validSpawnPoints != null && spawner.validSpawnPoints.Count > 0)
                {
                    var initialCount = spawner.validSpawnPoints.Count(sp => 
                        sp.distance >= spawner.initialSpawnMinDistance && 
                        sp.distance <= spawner.initialSpawnMaxDistance);
                    var continuousCount = spawner.validSpawnPoints.Count(sp => 
                        sp.distance >= spawner.continuousSpawnMinDistance && 
                        sp.distance <= spawner.continuousSpawnMaxDistance);
                        
                    GUILayout.Label($"Spawn Iniziali Disponibili: {initialCount}", UnityEditor.EditorStyles.helpBox);
                    GUILayout.Label($"Spawn Continui Disponibili: {continuousCount}", UnityEditor.EditorStyles.helpBox);
                }
                
                // Informazioni sul respawn
                GUILayout.Space(5);
                string respawnInfo = spawner.enableRespawn ? 
                    $"Respawn: ATTIVO (delay: {spawner.respawnDelay}s, min: {spawner.minActiveEnemies})" : 
                    "Respawn: DISATTIVATO";
                GUILayout.Label(respawnInfo, UnityEditor.EditorStyles.helpBox);
            }
        }
    }
#endif
}

/// <summary>
/// Struttura per contenere informazioni sui punti di spawn con distanza
/// </summary>
[System.Serializable]
public class SpawnPointInfo
{
    public int index;
    public Vector3 position;
    public int distance;
}