using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FootprintManager : MonoBehaviour
{
    [Header("Footprint Settings")]
    public bool footprintEnabled = false;
    public TileBase footprintTile;
    public int footprintFrequency = 1; // Ogni quante celle piazzare un'impronta (1 = ogni cella)
    
    [Header("Tilemap References")]
    public Tilemap footprintTilemap;
    public PlayerController playerController;
    public MazeManager mazeManager;
    
    [Header("Debug")]
    public bool enableDebug = false;
    
    // Stato interno
    private Vector3Int lastPlayerCell;
    private bool hasInitialPosition = false;
    private int movementCounter = 0;
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeManager();
    }
    
    void InitializeManager()
    {
        // Trova automaticamente i riferimenti se non assegnati
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("FootprintManager: PlayerController non trovato!");
                return;
            }
        }
        
        // Trova o crea la tilemap per le impronte
        if (footprintTilemap == null)
        {
            GameObject footprintObj = GameObject.Find("FootprintTilemap");
            if (footprintObj == null)
            {
                // Crea automaticamente la tilemap per le impronte
                footprintObj = CreateFootprintTilemap();
            }
            footprintTilemap = footprintObj.GetComponent<Tilemap>();
        }

        // Trova il mazeManager
        if (mazeManager == null)
        {
            mazeManager = FindFirstObjectByType<MazeManager>();
            if (mazeManager == null)
            {
                Debug.LogError("FootprintManager: MazeManager non trovato!");
                return;
            }
        }
        
        if (footprintTilemap == null)
        {
            Debug.LogError("FootprintManager: FootprintTilemap non trovata e non può essere creata!");
            return;
        }
        
        if (footprintTile == null)
        {
            Debug.LogWarning("FootprintManager: FootprintTile non assegnata! Assegna una tile nell'Inspector.");
            return;
        }
        
        isInitialized = true;
        
        // Inizializza la posizione del player
        if (playerController != null)
        {
            UpdatePlayerPosition();
        }
        
        if (enableDebug)
        {
            Debug.Log("FootprintManager inizializzato correttamente");
        }
    }
    
    GameObject CreateFootprintTilemap()
    {
        // Crea un nuovo GameObject per la tilemap delle impronte
        GameObject footprintObj = new GameObject("FootprintTilemap");
        
        // Aggiungi i componenti necessari
        TilemapRenderer renderer = footprintObj.AddComponent<TilemapRenderer>();
        Tilemap tilemap = footprintObj.AddComponent<Tilemap>();
        
        // Configura il renderer per essere sopra il terreno ma sotto il player
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 1; // Sopra il terreno (0) ma sotto il player (2+)
        
        // Trova il parent appropriato (tipicamente il GameObject Labirinto)
        GameObject labirinto = GameObject.Find("Labirinto");
        if (labirinto != null)
        {
            footprintObj.transform.SetParent(labirinto.transform);
        }
        
        Debug.Log("FootprintTilemap creata automaticamente");
        return footprintObj;
    }
    
    void Update()
    {
        if (!isInitialized || !footprintEnabled || playerController == null) return;
        
        UpdatePlayerPosition();
    }
    
    void UpdatePlayerPosition()
    {
        if (footprintTilemap == null) return;
        
        Vector3Int currentCell = footprintTilemap.WorldToCell(playerController.transform.position);
        
        if (!hasInitialPosition)
        {
            lastPlayerCell = currentCell;
            hasInitialPosition = true;
            if (enableDebug)
            {
                Debug.Log($"Posizione iniziale player: {currentCell}");
            }
            return;
        }
        
        // Controlla se il player è cambiato di cella
        if (currentCell != lastPlayerCell)
        {
            // Il player si è mosso in una nuova cella
            Vector2 moveDirection = GetMoveDirection(lastPlayerCell, currentCell);
            
            // Incrementa il contatore di movimento
            movementCounter++;
            
            // Piazza l'impronta solo se il contatore raggiunge la frequenza impostata
            if (movementCounter >= footprintFrequency)
            {
                PlaceFootprint(lastPlayerCell, moveDirection);
                movementCounter = 0; // Reset del contatore
            }
            
            lastPlayerCell = currentCell;
            
            if (enableDebug)
            {
                Debug.Log($"Player mosso a: {currentCell}, direzione: {moveDirection}");
            }
        }
    }
    
    Vector2 GetMoveDirection(Vector3Int fromCell, Vector3Int toCell)
    {
        Vector3Int direction = toCell - fromCell;
        
        // Normalizza la direzione a uno dei quattro movimenti cardinali
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Movimento orizzontale
            return direction.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            // Movimento verticale
            return direction.y > 0 ? Vector2.up : Vector2.down;
        }
    }
    
    void PlaceFootprint(Vector3Int position, Vector2 direction)
    {
        if (footprintTilemap == null || footprintTile == null) return;
        
        // Se il player è nell'hub o c'è già un'impronta
        if (IsPlayerInHub() || HasFootprintAtPosition(position)) return;
        
        // Calcola l'angolo di rotazione basato sulla direzione
        float rotationAngle = GetRotationAngle(direction);
        
        // Crea la matrice di trasformazione con la rotazione
        Matrix4x4 transformMatrix = Matrix4x4.TRS(
            Vector3.zero, 
            Quaternion.Euler(0, 0, rotationAngle), 
            Vector3.one
        );
        
        // Piazza la tile con la trasformazione
        footprintTilemap.SetTile(position, footprintTile);
        footprintTilemap.SetTransformMatrix(position, transformMatrix);
        
        if (enableDebug)
        {
            Debug.Log($"Impronta piazzata in {position} con rotazione {rotationAngle}°");
        }
    }
    
    float GetRotationAngle(Vector2 direction)
    {
        // Converte la direzione in angoli di rotazione
        if (direction == Vector2.up)
            return 0f;    // Su
        else if (direction == Vector2.right)
            return 270f;  // Destra (o -90)
        else if (direction == Vector2.down)
            return 180f;  // Giù
        else if (direction == Vector2.left)
            return 90f;   // Sinistra
        
        return 0f; // Default
    }
    
    bool IsPlayerInHub()
    {
        if (mazeManager == null)
        {
            // Fallback: se non abbiamo MazeManager, assumiamo che sia nel labirinto
            // per evitare di bloccare completamente il sistema
            if (enableDebug)
            {
                Debug.LogWarning("MazeManager non disponibile - assumendo player nel labirinto");
            }
            return false;
        }
        
        // Usa le proprietà pubbliche del MazeManager per determinare la posizione
        bool inHub = mazeManager.IsPlayerInOuterHub || mazeManager.IsPlayerInInnerHub;
        
        if (enableDebug && inHub)
        {
            string hubType = mazeManager.IsPlayerInInnerHub ? "Inner Hub" : "Outer Hub";
            Debug.Log($"Player rilevato in {hubType} - impronte disabilitate");
        }
        
        return inHub;
    }
    
    bool HasFootprintAtPosition(Vector3Int position)
    {
        return footprintTilemap != null && footprintTilemap.HasTile(position);
    }

    /// <summary>
    /// Abilita il sistema di impronte (attivato dal power-up binocular)
    /// </summary>
    public void EnableFootprints()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("FootprintManager non inizializzato, impossibile abilitare le impronte");
            return;
        }

        footprintEnabled = true;

        // Reset della posizione per evitare impronte spurie
        if (playerController != null)
        {
            lastPlayerCell = footprintTilemap.WorldToCell(playerController.transform.position);
            hasInitialPosition = true;
        }

        movementCounter = 0;

        if (enableDebug)
        {
            Debug.Log("Sistema di impronte abilitato");
        }
    }
    
    /// <summary>
    /// Disabilita il sistema di impronte
    /// </summary>
    public void DisableFootprints()
    {
        footprintEnabled = false;
        
        if (enableDebug)
        {
            Debug.Log("Sistema di impronte disabilitato");
        }
    }
    
    /// <summary>
    /// Pulisce tutte le impronte dalla tilemap (da chiamare all'alba)
    /// </summary>
    public void ResetFootprints()
    {
        if (footprintTilemap != null)
        {
            footprintTilemap.SetTilesBlock(footprintTilemap.cellBounds, new TileBase[footprintTilemap.cellBounds.size.x * footprintTilemap.cellBounds.size.y]);
            
            if (enableDebug)
            {
                Debug.Log("Tutte le impronte sono state rimosse");
            }
        }
        
        // Reset dello stato
        movementCounter = 0;
        hasInitialPosition = false;
        
        // Reinizializza la posizione del player se il sistema è attivo
        if (footprintEnabled && playerController != null)
        {
            StartCoroutine(ReinitializePositionNextFrame());
        }
    }
    
    IEnumerator ReinitializePositionNextFrame()
    {
        yield return null; // Aspetta un frame
        
        if (playerController != null && footprintTilemap != null)
        {
            lastPlayerCell = footprintTilemap.WorldToCell(playerController.transform.position);
            hasInitialPosition = true;
            
            if (enableDebug)
            {
                Debug.Log($"Posizione player reinizializzata dopo reset: {lastPlayerCell}");
            }
        }
    }
    
    /// <summary>
    /// Aggiorna la tilemap di riferimento (utile quando cambia il labirinto)
    /// </summary>
    public void UpdateTilemapReference(Tilemap newTilemap)
    {
        if (newTilemap == null) return;
        
        // Reset dello stato quando cambia la tilemap
        hasInitialPosition = false;
        movementCounter = 0;
        
        // Reinizializza la posizione del player sulla nuova tilemap
        if (playerController != null)
        {
            lastPlayerCell = newTilemap.WorldToCell(playerController.transform.position);
            hasInitialPosition = true;
        }
        
        if (enableDebug)
        {
            Debug.Log($"Riferimento tilemap aggiornato: {newTilemap.name}");
        }
    }
    
    /// <summary>
    /// Metodo per testare il sistema (da usare nell'Editor)
    /// </summary>
    [ContextMenu("Test - Abilita Impronte")]
    public void TestEnableFootprints()
    {
        EnableFootprints();
    }
    
    [ContextMenu("Test - Disabilita Impronte")]
    public void TestDisableFootprints()
    {
        DisableFootprints();
    }
    
    [ContextMenu("Test - Reset Impronte")]
    public void TestResetFootprints()
    {
        ResetFootprints();
    }
    
    // Proprietà pubbliche per accesso esterno
    public bool IsEnabled => footprintEnabled;
    public int FootprintCount => footprintTilemap != null ? footprintTilemap.GetUsedTilesCount() : 0;
}