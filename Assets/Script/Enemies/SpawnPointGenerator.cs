using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class SpawnPoint
{
    public Vector3 position;
    
    public SpawnPoint(Vector3 pos)
    {
        position = pos;
    }
}

public class SpawnPointGenerator : MonoBehaviour
{
    [Header("Configurazione")]
    [SerializeField] private int numeroSpawnPoints;
    [SerializeField] private Tilemap labirintoTilemap;
    [SerializeField] private TileBase corridoioTile; // Opzionale - se null, accetta qualsiasi tile

    [Header("Configurazione Distanza")]
    [SerializeField] private Vector2 puntoRiferimento = new Vector2(155f, 155f);
    [SerializeField] private float distanzaMinima = 60f;
    [SerializeField] private float distanzaMassima = 130f;

    [Header("Spawn Points Generati")]
    [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    [Header("Visualizzazione")]
    [SerializeField] private bool mostraDebugInfo = true;
    [SerializeField] private bool mostraSpawnPointsInScena = true;

    [Header("Integrazione Automatica")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] public bool autoUpdateOnMazeChange = true;

    void Start()
    {
        // Trova automaticamente l'EnemySpawner se non assegnato
        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
    }

    public void UpdateTilemapAndRegenerateSpawns(Tilemap newTilemap)
    {
        //Debug.Log($"UpdateTilemapAndRegenerateSpawns chiamato. AutoUpdate: {autoUpdateOnMazeChange}");

        if (!autoUpdateOnMazeChange)
        {
            //Debug.Log("AutoUpdateOnMazeChange è disattivato, skip aggiornamento");
            return;
        }

        if (newTilemap == null)
        {
            //Debug.LogError("Tilemap è NULL in UpdateTilemapAndRegenerateSpawns!");
            return;
        }

        // Debug più dettagliato
        //Debug.Log($"Tilemap corrente: {(labirintoTilemap != null ? labirintoTilemap.name : "NULL")}");
        //Debug.Log($"Nuova tilemap: {newTilemap.name}");
        //Debug.Log($"Spawn points attuali: {spawnPoints.Count}");

        // Controlla se è la stessa tilemap (evita rigenerazione inutile)
        if (labirintoTilemap == newTilemap && spawnPoints.Count > 0)
        {
            //Debug.Log("Tilemap non cambiata e spawn points già presenti, skip rigenerazione spawn points");
            return;
        }

        //Debug.Log("Tilemap cambiata o spawn points vuoti, procedendo con rigenerazione...");

        // Aggiorna la tilemap
        labirintoTilemap = newTilemap;

        // Rigenera gli spawn points
        GeneraSpawnPoints();

        // Passa automaticamente i nuovi spawn points all'EnemySpawner
        if (enemySpawner != null)
        {
            PassSpawnPointsToEnemySpawner();
        }
        else
        {
            //Debug.LogWarning("EnemySpawner non trovato per passaggio automatico spawn points!");
            // Cerca di nuovo
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
            if (enemySpawner != null)
            {
                PassSpawnPointsToEnemySpawner();
                //Debug.Log("EnemySpawner trovato al secondo tentativo");
            }
        }
    }

    public void ForceRegenerateSpawns(Tilemap newTilemap)
    {
        //Debug.Log($"ForceRegenerateSpawns chiamato con tilemap: {(newTilemap != null ? newTilemap.name : "NULL")}");

        if (newTilemap == null)
        {
            //Debug.LogError("Tilemap è NULL in ForceRegenerateSpawns!");
            return;
        }

        // Aggiorna sempre la tilemap
        labirintoTilemap = newTilemap;

        // Forza la rigenerazione senza controlli preventivi
        //Debug.Log("Avviando rigenerazione forzata spawn points...");
        GeneraSpawnPoints();

        // Passa automaticamente i nuovi spawn points all'EnemySpawner
        if (enemySpawner != null)
        {
            PassSpawnPointsToEnemySpawner();
        }
        else
        {
            // Cerca di nuovo l'EnemySpawner se non è stato trovato
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
            if (enemySpawner != null)
            {
                PassSpawnPointsToEnemySpawner();
                //Debug.Log("EnemySpawner trovato al secondo tentativo");
            }
            else
            {
                //Debug.LogWarning("EnemySpawner non trovato per passaggio automatico spawn points!");
            }
        }
    }

    private void PassSpawnPointsToEnemySpawner()
    {
        if (enemySpawner == null)
        {
            //Debug.LogWarning("EnemySpawner non assegnato, tentativo di ricerca...");
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
        
        if (enemySpawner == null)
        {
            //Debug.LogError("EnemySpawner non trovato nella scena!");
            return;
        }
        
        if (spawnPoints.Count == 0)
        {
            //Debug.LogWarning("Nessun spawn point da passare all'EnemySpawner");
            return;
        }
        
        // Converti SpawnPoint in Vector3 array
        Vector3[] positions = new Vector3[spawnPoints.Count];
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            positions[i] = spawnPoints[i].position;
        }
        
        // Passa i dati all'EnemySpawner
        enemySpawner.UpdateGeneratedSpawnPoints(positions);
        
        //Debug.Log($"Passati {positions.Length} spawn points all'EnemySpawner");
        
        // VERIFICA: Controlla che l'EnemySpawner stia effettivamente usando i generated spawn points
        if (!enemySpawner.useGeneratedSpawnPoints)
        {
            //Debug.LogWarning("ATTENZIONE: EnemySpawner non sta usando i generated spawn points!");
            //Debug.LogWarning("Forzando l'utilizzo dei generated spawn points...");
            
            // Forza l'utilizzo attraverso il metodo pubblico dell'inspector
            enemySpawner.CopiaPosizioniDaGenerator();
        }
        else
        {
            //Debug.Log("EnemySpawner configurato correttamente per usare generated spawn points");
        }
    }

    // Metodo pubblico per forzare l'aggiornamento manuale
    [ContextMenu("Aggiorna e Passa Spawn Points")]
    public void ForceUpdateAndPass()
    {
        GeneraSpawnPoints();
        PassSpawnPointsToEnemySpawner();
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(SpawnPointGenerator))]
    public class SpawnPointGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SpawnPointGenerator generator = (SpawnPointGenerator)target;

            GUILayout.Space(10);
            GUILayout.Label("Controlli", UnityEditor.EditorStyles.boldLabel);

            if (GUILayout.Button("Genera Spawn Points", GUILayout.Height(30)))
            {
                generator.GeneraSpawnPoints();
            }

            if (GUILayout.Button("Pulisci Spawn Points"))
            {
                generator.PulisciSpawnPoints();
            }

            GUILayout.Space(5);
            GUILayout.Label($"Spawn Points Disponibili: {generator.spawnPoints.Count}", UnityEditor.EditorStyles.helpBox);
        }
    }
#endif

    public void GeneraSpawnPoints()
    {
        // Pulisci i vecchi spawn points
        spawnPoints.Clear();
        
        // Trova automaticamente la tilemap se non è assegnata
        if (labirintoTilemap == null)
        {
            labirintoTilemap = FindFirstObjectByType<Tilemap>();
        }
        
        if (labirintoTilemap == null)
        {
            //Debug.LogError("Nessuna Tilemap trovata! Assegna la tilemap del labirinto.");
            return;
        }
        
        // Trova tutte le posizioni valide
        List<Vector3> posizioniValide = TrovaPosizioniValide();
        
        if (posizioniValide.Count == 0)
        {
            //Debug.LogWarning("Nessuna posizione valida trovata per gli spawn points!");
            return;
        }
        
        // Genera gli spawn points in posizioni random
        int spawnPointsDaGenerare = Mathf.Min(numeroSpawnPoints, posizioniValide.Count);
        
        for (int i = 0; i < spawnPointsDaGenerare; i++)
        {
            // Scegli una posizione random dalla lista
            int indiceRandom = Random.Range(0, posizioniValide.Count);
            Vector3 posizione = posizioniValide[indiceRandom];
            
            // Rimuovi la posizione dalla lista per evitare duplicati
            posizioniValide.RemoveAt(indiceRandom);
            
            // Crea lo spawn point
            spawnPoints.Add(new SpawnPoint(posizione));
        }
        
        if (mostraDebugInfo)
        {
            //Debug.Log($"Generati {spawnPointsDaGenerare} spawn points.");
        }
        
        // RIMUOVI le chiamate agli EditorUtility durante runtime
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Marca la scena come modificata per salvare i cambiamenti SOLO in editor mode
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
        #endif
    }

    public void PulisciSpawnPoints()
    {
        spawnPoints.Clear();

        if (mostraDebugInfo)
        {
            //Debug.Log("Spawn points puliti.");
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    

    private List<Vector3> TrovaPosizioniValide()
    {
        List<Vector3> posizioniValide = new List<Vector3>();

        // Ottieni i bounds della tilemap
        BoundsInt bounds = labirintoTilemap.cellBounds;

        if (mostraDebugInfo)
        {
            //Debug.Log($"Scansionando tilemap con bounds: {bounds}");
        }

        int posizioniScansionate = 0;
        int posizioniCorridoio = 0;

        // Scansiona ogni cella della tilemap
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                posizioniScansionate++;
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                TileBase tile = labirintoTilemap.GetTile(cellPosition);

                // Controlla se è un tile corridoio (o se corridoioTile non è assegnato, accetta qualsiasi tile non nullo)
                if (tile != null && (corridoioTile == null || tile == corridoioTile))
                {
                    posizioniCorridoio++;

                    // Converte la posizione della cella in coordinate world
                    Vector3 worldPos = labirintoTilemap.CellToWorld(cellPosition);

                    // Aggiunge gli offset specificati (.5 per x, .7 per y)
                    Vector3 posizioneFinale = new Vector3(worldPos.x + 0.5f, worldPos.y + 0.7f, worldPos.z);

                    // Controlla se la distanza è nel range specificato
                    float distanza = Vector2.Distance(new Vector2(posizioneFinale.x, posizioneFinale.y), puntoRiferimento);

                    if (distanza >= distanzaMinima && distanza <= distanzaMassima)
                    {
                        posizioniValide.Add(posizioneFinale);
                    }
                }
            }
        }

        if (mostraDebugInfo)
        {
            //Debug.Log($"Scansione completata:");
            //Debug.Log($"- Posizioni totali scansionate: {posizioniScansionate}");
            //Debug.Log($"- Posizioni con tile corridoio: {posizioniCorridoio}");
            //Debug.Log($"- Posizioni valide (nel range di distanza): {posizioniValide.Count}");
        }

        return posizioniValide;
    }

    // Metodi pubblici per accedere agli spawn points
    public List<SpawnPoint> GetSpawnPoints()
    {
        return new List<SpawnPoint>(spawnPoints);
    }

    public int GetNumeroSpawnPoints()
    {
        return spawnPoints.Count;
    }

    // Visualizzazione debug nella Scene View
    private void OnDrawGizmosSelected()
    {
        if (!mostraDebugInfo) return;

        // Disegna il punto di riferimento
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(puntoRiferimento.x, puntoRiferimento.y, 0), 2f);

        // Disegna i cerchi di distanza minima e massima
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(puntoRiferimento.x, puntoRiferimento.y, 0), distanzaMinima);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(new Vector3(puntoRiferimento.x, puntoRiferimento.y, 0), distanzaMassima);

        // Disegna gli spawn points
        if (spawnPoints != null && spawnPoints.Count > 0 && mostraSpawnPointsInScena)
        {
            Gizmos.color = Color.cyan;

            for (int i = 0; i < spawnPoints.Count; i++)
            {
                Vector3 pos = spawnPoints[i].position;
                Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);

                // Disegna il numero dello spawn point
#if UNITY_EDITOR
                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.textColor = Color.black;
                labelStyle.fontSize = 12;
                labelStyle.fontStyle = FontStyle.Bold;
                UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, (i + 1).ToString(), labelStyle);
#endif
            }
        }
    }
}