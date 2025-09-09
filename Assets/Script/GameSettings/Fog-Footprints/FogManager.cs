using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;

public class FogManager : MonoBehaviour
{
    [Header("Tilemap Settings")]
    public Tilemap mainTilemap;      // La tilemap che cambia ogni giorno
    public Tilemap fogTilemap;       // La tilemap della nebbia (separata)
    public TileBase fogTile;         // Tile da usare per la nebbia
    public GameObject fogPrefab;     // Prefab della tilemap nebbia preconfigurata

    [Header("Fog Settings")]
    public Vector3Int centerCell;    // Il centro in coordinate cella
    public float minDist = 3f;       // Distanza minima (dentro = nessuna nebbia)
    public float maxDist = 8f;       // Distanza massima (fuori = nessuna nebbia)
    public Color fogColor = Color.gray; // Colore nebbia uniforme

    [Header("Warning Zone Settings")]
    public Vector3Int warningCenterCell = new Vector3Int(155, 155, 0); // Centro della warning zone
    public float warningMinDist = 117f;  // Distanza minima warning zone
    public float warningMaxDist = 119f;  // Distanza massima warning zone
    public bool canPlayerPassFog = false; // Il player può passare attraverso la nebbia?

   // [Header("Save/Load Settings")]
   // public bool loadFromSave = false; // Impostare true se si vuole caricare da salvataggio

    // Eventi per la warning zone
    public static System.Action OnPlayerEnteredWarningZone;
    public static System.Action OnPlayerExitedWarningZone;

    // Stato interno per tracking del player
    private bool isPlayerInWarningZone = false;

    // Matrice per il salvataggio dello stato della nebbia (310x310)
    private bool[,] matriceNebbia = new bool[310, 310];
    private const int MAZE_SIZE = 310;

    void Start()
    {
        // Assicurati che la fog tilemap sia sopra la main tilemap
        if (fogTilemap != null && mainTilemap != null)
        {
            var fogRenderer = fogTilemap.GetComponent<TilemapRenderer>();
            var mainRenderer = mainTilemap.GetComponent<TilemapRenderer>();

            if (fogRenderer != null && mainRenderer != null)
            {
                fogRenderer.sortingOrder = mainRenderer.sortingOrder + 2;
            }
        }

        // Decide se caricare da prefab o da salvataggio
        if (SaveSystem.HasFogSaveData() && !SaveSystem.isNewGame)
        {
            // Qui dovresti caricare i dati di salvataggio e poi chiamare ApplyFogFromSave()
            // Per ora usiamo il prefab come fallback
            /**
            if (HasSaveData())
            {
                LoadFogDataFromSave();
                ApplyFogFromSave();
            }
            else
            {
                ApplyFogFromPrefab();
            }
            **/
            Debug.Log("Caricamento nebbia da dati salvati");
            ApplyFogFromSave();
        }
        else
        {
            Debug.Log("Nuova partita - caricamento nebbia da prefab");
            // Nuova partita - usa il prefab
            ApplyFogFromPrefab();
        }
    }

    [ContextMenu("Applica Nebbia")]
    public void ApplyFog()
    {
        if (fogTilemap == null || fogTile == null || mainTilemap == null)
        {
            Debug.LogError("Tilemap o FogTile non assegnati!");
            return;
        }

        // Pulisci la fog tilemap
        ClearFogTilemap();

        //NEW: resetta la matrice
        InitializeMatrix();

        // Applica nebbia solo nell'anello tra minDist e maxDist
        foreach (var pos in mainTilemap.cellBounds.allPositionsWithin)
        {
            if (!mainTilemap.HasTile(pos)) continue;

            float dist = Vector3Int.Distance(pos, centerCell);

            // Nebbia SOLO nell'anello tra minDist e maxDist
            if (dist >= minDist && dist <= maxDist)
            {
                fogTilemap.SetTile(pos, fogTile);
                fogTilemap.SetColor(pos, fogColor);

                //NEW: aggiorna la matrice
                UpdateMatrixPosition(pos, true);
            }
        }
    }

    /// <summary>
    /// Applica la nebbia usando il prefab preconfigurato e aggiorna la matrice
    /// </summary>
    public void ApplyFogFromPrefab()
    {
        if (fogPrefab == null)
        {
            Debug.LogError("Prefab della nebbia non assegnato!");
            return;
        }

        if (fogTilemap == null || mainTilemap == null)
        {
            Debug.LogError("Tilemap non assegnate!");
            return;
        }

        // Pulisci la fog tilemap attuale
        ClearFogTilemap();
        
        // Resetta la matrice
        InitializeMatrix();

        // Ottieni il prefab Tilemap
        GameObject prefabInstance = Instantiate(fogPrefab);
        Tilemap prefabTilemap = prefabInstance.GetComponent<Tilemap>();
        
        if (prefabTilemap == null)
        {
            Debug.LogError("Il prefab non contiene una Tilemap!");
            Destroy(prefabInstance);
            return;
        }

        // Copia tutti i tile dal prefab alla tilemap attiva
        BoundsInt bounds = prefabTilemap.cellBounds;
        TileBase[] allTiles = prefabTilemap.GetTilesBlock(bounds);
        Color[] allColors = new Color[allTiles.Length];

        int index = 0;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (allTiles[index] != null)
            {
                // Copia il tile
                fogTilemap.SetTile(pos, allTiles[index]);
                fogTilemap.SetColor(pos, fogColor);
                
                // Aggiorna la matrice
                UpdateMatrixPosition(pos, true);
            }
            index++;
        }
        // Distruggi l'istanza temporanea del prefab
        Destroy(prefabInstance);

        Debug.Log($"Nebbia applicata da prefab. Tile copiati: {GetFogTileCount()}");
    }

    /// <summary>
    /// Applica la nebbia usando i dati di salvataggio
    /// </summary>
    public void ApplyFogFromSave()
    {
        if (fogTilemap == null || fogTile == null)
        {
            Debug.LogError("Tilemap o FogTile non assegnati!");
            return;
        }

        // Pulisci la fog tilemap
        ClearFogTilemap();

        // Crea una lista delle posizioni dove applicare la nebbia
        List<Vector3Int> fogPositions = new List<Vector3Int>();

        // Scansiona la matrice per trovare le posizioni con nebbia
        for (int x = 0; x < MAZE_SIZE; x++)
        {
            for (int y = 0; y < MAZE_SIZE; y++)
            {
                if (matriceNebbia[x, y])
                {
                    // Converti le coordinate della matrice in coordinate tilemap
                    Vector3Int tilePos = MatrixToTileCoordinates(x, y);
                    fogPositions.Add(tilePos);
                }
            }
        }

        // Applica i tile nebbia nelle posizioni salvate
        foreach (Vector3Int pos in fogPositions)
        {
            fogTilemap.SetTile(pos, fogTile);
            fogTilemap.SetColor(pos, fogColor);
        }

        Debug.Log($"Nebbia applicata da salvataggio. Tile ripristinati: {fogPositions.Count}");
    }

    [ContextMenu("Rimuovi Nebbia")]
    public void RemoveFog()
    {
        ClearFogTilemap();
        InitializeMatrix(); // Resetta anche la matrice
    }

    // Rimuove nebbia in una posizione specifica
    public void RevealFogAtPosition(Vector3Int position)
    {
        if (fogTilemap != null && fogTilemap.HasTile(position))
        {
            fogTilemap.SetTile(position, null);
            // Aggiorna la matrice
            UpdateMatrixPosition(position, false);
        }
    }

    // Rimuove nebbia in un raggio (completamente)
    public void RevealFogInRadius(Vector3Int centerPosition, float radius)
    {
        BoundsInt area = new BoundsInt(
            centerPosition.x - Mathf.CeilToInt(radius),
            centerPosition.y - Mathf.CeilToInt(radius),
            0,
            Mathf.CeilToInt(radius * 2) + 1,
            Mathf.CeilToInt(radius * 2) + 1,
            1
        );

        foreach (var pos in area.allPositionsWithin)
        {
            float dist = Vector3Int.Distance(pos, centerPosition);
            if (dist <= radius && fogTilemap.HasTile(pos))
            {
                fogTilemap.SetTile(pos, null);
                // Aggiorna la matrice
                UpdateMatrixPosition(pos, false);
            }
        }
    }

    // Effetto "torcia" - rimuove completamente nel centro, gradualmente sui bordi
    public void RevealFogGradually(Vector3Int centerPosition, float innerRadius, float outerRadius)
    {
        BoundsInt area = new BoundsInt(
            centerPosition.x - Mathf.CeilToInt(outerRadius),
            centerPosition.y - Mathf.CeilToInt(outerRadius),
            0,
            Mathf.CeilToInt(outerRadius * 2) + 1,
            Mathf.CeilToInt(outerRadius * 2) + 1,
            1
        );

        foreach (var pos in area.allPositionsWithin)
        {
            if (!fogTilemap.HasTile(pos)) continue;

            float dist = Vector3Int.Distance(pos, centerPosition);

            if (dist <= innerRadius)
            {
                // Rimuovi completamente la nebbia nel centro
                fogTilemap.SetTile(pos, null);
                // Aggiorna la matrice
                UpdateMatrixPosition(pos, false);
            }
            else if (dist <= outerRadius)
            {
                // Riduci gradualmente l'intensità della nebbia sui bordi
                float t = Mathf.InverseLerp(innerRadius, outerRadius, dist);
                Color newColor = new Color(fogColor.r, fogColor.g, fogColor.b, t);
                fogTilemap.SetColor(pos, newColor);
                // Nota: in questo caso non rimuoviamo il tile, solo cambiamo il colore
                // quindi la matrice rimane true
            }
        }
    }

    private void ClearFogTilemap()
    {
        if (fogTilemap == null) return;

        BoundsInt bounds = fogTilemap.cellBounds;
        TileBase[] emptyTiles = new TileBase[bounds.size.x * bounds.size.y * bounds.size.z];
        fogTilemap.SetTilesBlock(bounds, emptyTiles);
    }

    // Metodi di utilità 
    public void SetFogCenter(Vector3Int newCenter)
    {
        centerCell = newCenter;
        ApplyFog();
    }

    public void UpdateFogDistances(float newMinDist, float newMaxDist)
    {
        minDist = newMinDist;
        maxDist = newMaxDist;
        ApplyFog();
    }

    // Verifica se c'è nebbia in una posizione
    public bool HasFogAtPosition(Vector3Int position)
    {
        return fogTilemap != null && fogTilemap.HasTile(position);
    }

    #region Warning Zone Methods
    public void CheckPlayerWarningZone(Vector3 playerWorldPosition)
    {
        // Debug dettagliato delle coordinate
        if (Debug.isDebugBuild)
        {
            Debug.Log($"=== DIAGNOSI COORDINATE ===");
            Debug.Log($"Player World Position: {playerWorldPosition}");

            if (mainTilemap != null)
            {
                Vector3Int playerCell = mainTilemap.WorldToCell(playerWorldPosition);
                Vector3 reconvertedWorld = mainTilemap.CellToWorld(playerCell);

                Debug.Log($"MainTilemap conversion: World {playerWorldPosition} -> Cell {playerCell} -> World {reconvertedWorld}");
            }

            if (fogTilemap != null)
            {
                Vector3Int playerCellFog = fogTilemap.WorldToCell(playerWorldPosition);
                Vector3 reconvertedWorldFog = fogTilemap.CellToWorld(playerCellFog);

                Debug.Log($"FogTilemap conversion: World {playerWorldPosition} -> Cell {playerCellFog} -> World {reconvertedWorldFog}");
            }

            Debug.Log($"Warning Center Cell: {warningCenterCell}");

            if (mainTilemap != null)
            {
                Vector3 warningCenterWorld = mainTilemap.CellToWorld(warningCenterCell);
                Debug.Log($"Warning Center World: {warningCenterWorld}");
            }

            Debug.Log($"=== FINE DIAGNOSI ===");
        }

        // Se il player può passare attraverso la nebbia, non mostrare warning
        if (canPlayerPassFog)
        {
            if (isPlayerInWarningZone)
            {
                isPlayerInWarningZone = false;
                OnPlayerExitedWarningZone?.Invoke();
                Debug.Log("Warning nascosto: player ora può attraversare la nebbia!");
            }
            return;
        }

        // Verifica che mainTilemap sia assegnata
        if (mainTilemap == null)
        {
            Debug.LogError("MainTilemap non assegnata in FogManager!");
            return;
        }

        // USA LA TILEMAP CORRETTA per la conversione
        // Se mainTilemap e fogTilemap sono diverse, usa fogTilemap per la nebbia
        Tilemap tilemapToUse = fogTilemap != null ? fogTilemap : mainTilemap;

        // Converti la posizione world in coordinate cella
        Vector3Int playerCellPos = tilemapToUse.WorldToCell(playerWorldPosition);

        // Calcola la distanza dal centro della warning zone
        float distanceFromWarningCenter = Vector3Int.Distance(playerCellPos, warningCenterCell);

        Debug.Log($"Player cell pos (using {tilemapToUse.name}): {playerCellPos}");
        Debug.Log($"Warning center cell: {warningCenterCell}");
        Debug.Log($"Distance from warning center: {distanceFromWarningCenter:F2}");
        Debug.Log($"Warning range: {warningMinDist:F1} - {warningMaxDist:F1}");

        // Determina se il player è nell'anello di warning
        bool playerShouldBeInWarningZone = (distanceFromWarningCenter >= warningMinDist &&
                                           distanceFromWarningCenter <= warningMaxDist);

        Debug.Log($"Should be in warning zone: {playerShouldBeInWarningZone}");
        Debug.Log($"Currently in warning zone: {isPlayerInWarningZone}");

        // Gestisci i cambiamenti di stato
        if (playerShouldBeInWarningZone && !isPlayerInWarningZone)
        {
            isPlayerInWarningZone = true;
            OnPlayerEnteredWarningZone?.Invoke();
            Debug.Log($"✓ PLAYER ENTRATO IN WARNING ZONE! Distanza: {distanceFromWarningCenter:F1}");

            if (OnPlayerEnteredWarningZone == null)
            {
                Debug.LogError("NESSUN LISTENER per OnPlayerEnteredWarningZone!");
            }
        }
        else if (!playerShouldBeInWarningZone && isPlayerInWarningZone)
        {
            isPlayerInWarningZone = false;
            OnPlayerExitedWarningZone?.Invoke();
            Debug.Log($"✓ PLAYER USCITO DA WARNING ZONE! Distanza: {distanceFromWarningCenter:F1}");
        }
    }

    public void SetPlayerCanPassFog(bool canPass)
    {
        bool previousValue = canPlayerPassFog;
        canPlayerPassFog = canPass;

        // Se il player ha appena acquisito la capacità di passare attraverso la nebbia
        // e era nella warning zone, nascondi immediatamente il warning
        if (canPass && !previousValue && isPlayerInWarningZone)
        {
            isPlayerInWarningZone = false;
            OnPlayerExitedWarningZone?.Invoke();

            if (Debug.isDebugBuild)
            {
                Debug.Log("Warning nascosto: player ora può passare attraverso la nebbia!");
            }
        }
    }

    public bool IsPlayerInWarningZone()
    {
        return isPlayerInWarningZone && !canPlayerPassFog;
    }

    public bool CanPlayerPassFog()
    {
        return canPlayerPassFog;
    }

    /// <summary>
    /// NUOVO METODO: Reset del sistema di warning (utile per debug o restart)
    /// </summary>
    [ContextMenu("Reset Warning Zone")]
    public void ResetWarningZone()
    {
        if (isPlayerInWarningZone)
        {
            isPlayerInWarningZone = false;
            OnPlayerExitedWarningZone?.Invoke();
        }
        canPlayerPassFog = false;

        if (Debug.isDebugBuild)
        {
            Debug.Log("Warning zone resettata!");
        }
    }
    #endregion

    #region Matrix and Save/Load Methods
    /// <summary>
    /// Inizializza la matrice nebbia (tutti false)
    /// </summary>
    private void InitializeMatrix()
    {
        for (int x = 0; x < MAZE_SIZE; x++)
        {
            for (int y = 0; y < MAZE_SIZE; y++)
            {
                matriceNebbia[x, y] = false;
            }
        }
    }

    /// <summary>
    /// Aggiorna una posizione specifica nella matrice
    /// </summary>
    private void UpdateMatrixPosition(Vector3Int tilePosition, bool hasFog)
    {
        Vector2Int matrixCoords = TileToMatrixCoordinates(tilePosition);
        
        if (IsValidMatrixCoordinate(matrixCoords))
        {
            matriceNebbia[matrixCoords.x, matrixCoords.y] = hasFog;
        }
    }

    /// <summary>
    /// Converte coordinate tilemap in coordinate matrice
    /// </summary>
    private Vector2Int TileToMatrixCoordinates(Vector3Int tilePosition)
    {

        if (Debug.isDebugBuild)
        {
            Debug.Log($"Converting tile {tilePosition} to matrix coordinates");
        }
        // Assumendo che le coordinate tilemap partano da (0,0) e la matrice pure
        // Adatta questo metodo alle tue coordinate specifiche
        //ESEMPIO:se le coordinate tilmap vanno da -155 a 154 aggiungi 155 per avere 0-309

        //TROVA I BOUNDS PER CAPIRE LE COORDINATE REALI
        BoundsInt realBounds = mainTilemap != null ? mainTilemap.cellBounds : fogTilemap.cellBounds;
        //int matrixX = tilePosition.x + 155;
        //int matrixY = tilePosition.y + 155;
        int matrixX = tilePosition.x - realBounds.xMin;
        int matrixY = tilePosition.y - realBounds.yMin;

        //Clamp per sicurezza
        matrixX = Mathf.Clamp(matrixX, 0, MAZE_SIZE - 1);
        matrixY = Mathf.Clamp(matrixY, 0, MAZE_SIZE - 1);

        if (Debug.isDebugBuild)
        {
            Debug.Log($"Real bounds: {realBounds}, Matrix coords: ({matrixX}, {matrixY})");
        }

        return new Vector2Int(matrixX, matrixY);

        //return new Vector2Int(tilePosition.x, tilePosition.y);
    }

    /// <summary>
    /// Converte coordinate matrice in coordinate tilemap
    /// </summary>
    private Vector3Int MatrixToTileCoordinates(int matrixX, int matrixY)
    {
        BoundsInt realBounds = mainTilemap != null ? mainTilemap.cellBounds : fogTilemap.cellBounds;

        // Assumendo che le coordinate tilemap partano da (0,0) e la matrice pure
        // Adatta questo metodo alle tue coordinate specifiche
        //int tileX = matrixX - 155;
        //int tileY = matrixY - 155;
        int tileX = matrixX + realBounds.xMin;
        int tileY = matrixY + realBounds.yMin;
         
        return new Vector3Int(tileX, tileY, 0);

        //return new Vector3Int(matrixX, matrixY, 0);
    }

    /// <summary>
    /// Verifica se le coordinate della matrice sono valide
    /// </summary>
    private bool IsValidMatrixCoordinate(Vector2Int coords)
    {
        return coords.x >= 0 && coords.x < MAZE_SIZE && coords.y >= 0 && coords.y < MAZE_SIZE;
    }

    /// <summary>
    /// Converte la matrice nebbia in un BitArray per il salvataggio
    /// </summary>
    public BitArray GetFogBitArray()
    {
        BitArray bitArray = new BitArray(MAZE_SIZE * MAZE_SIZE);
        
        for (int x = 0; x < MAZE_SIZE; x++)
        {
            for (int y = 0; y < MAZE_SIZE; y++)
            {
                int index = x * MAZE_SIZE + y;
                bitArray[index] = matriceNebbia[x, y];
            }
        }
        
        Debug.Log($"Matrice nebbia convertita in BitArray. Dimensione: {bitArray.Length} bit");
        return bitArray;
    }

    /// <summary>
    /// Carica la matrice nebbia da un BitArray
    /// </summary>
    public void SetFogBitArray(BitArray bitArray)
    {
        if (bitArray == null || bitArray.Length != MAZE_SIZE * MAZE_SIZE)
        {
            Debug.LogError($"BitArray invalido! Dimensione attesa: {MAZE_SIZE * MAZE_SIZE}, ricevuta: {bitArray?.Length ?? 0}");
            return;
        }

        for (int x = 0; x < MAZE_SIZE; x++)
        {
            for (int y = 0; y < MAZE_SIZE; y++)
            {
                int index = x * MAZE_SIZE + y;
                matriceNebbia[x, y] = bitArray[index];
            }
        }
        
        Debug.Log($"Matrice nebbia caricata da BitArray. Tile nebbia: {GetMatrixFogCount()}");
    }

    /// <summary>
    /// Conta quanti tile nebbia ci sono nella matrice
    /// </summary>
    private int GetMatrixFogCount()
    {
        int count = 0;
        for (int x = 0; x < MAZE_SIZE; x++)
        {
            for (int y = 0; y < MAZE_SIZE; y++)
            {
                if (matriceNebbia[x, y]) count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Conta quanti tile nebbia ci sono attualmente nella tilemap
    /// </summary>
    private int GetFogTileCount()
    {
        if (fogTilemap == null) return 0;
        
        int count = 0;
        BoundsInt bounds = fogTilemap.cellBounds;
        
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (fogTilemap.HasTile(pos)) count++;
        }
        
        return count;
    }

    private void LoadFogDataFromSave()
    {
        // Implementa qui la logica per caricare i dati dal sistema di salvataggio
        // e chiamare SetFogBitArray() con i dati caricati
        
        // Esempio:
        // string savedData = PlayerPrefs.GetString("FogData", "");
        // if (!string.IsNullOrEmpty(savedData))
        // {
        //     BitArray bitArray = DeserializeBitArray(savedData);
        //     SetFogBitArray(bitArray);
        // }
    }

    /// <summary>
    /// Metodo di debug per visualizzare lo stato della matrice
    /// </summary>
    [ContextMenu("Debug Matrix State")]
    public void DebugMatrixState()
    {
        Debug.Log($"Stato matrice nebbia: {GetMatrixFogCount()}/{MAZE_SIZE * MAZE_SIZE} posizioni con nebbia");
        Debug.Log($"Stato tilemap: {GetFogTileCount()} tile nebbia attivi");
    }
    #endregion


    #region SAVE AND LOAD

    //METODO PER CONVERTIRE BITARRAY IN STRINGA PER JSON
    public string SerializeFogBitArray(BitArray bitArray)
    {
        if (bitArray == null) return "";

        byte[] bytes = new byte[(bitArray.Length + 7) / 8];
        bitArray.CopyTo(bytes, 0);
        return System.Convert.ToBase64String(bytes);
    }

    //METODO PER CONVERTIRE STRINGA SERIALIZZATA IN BITARRAY
    public BitArray DeserializeFogBitArray(string serializedData)
    {
        if (string.IsNullOrEmpty(serializedData)) return null;

        try
        {
            byte[] bytes = System.Convert.FromBase64String(serializedData);
            BitArray bitArray = new BitArray(bytes);

            /**
            //Tronca o espandi alla dimensione corretta
            if (bitArray.Length != MAZE_SIZE * MAZE_SIZE)
            {
                BitArray correctSizeBitArray = new BitArray(MAZE_SIZE * MAZE_SIZE);
                int copyLength = Mathf.Min(bitArray.Length, correctSizeBitArray.Length);

                for (int i = 0; i < copyLength; i++)
                {
                    correctSizeBitArray[i] = bitArray[i];
                }

                return correctSizeBitArray;
            }

            return bitArray;
            **/
            // CREA SEMPRE un BitArray della dimensione corretta
            BitArray correctSizeBitArray = new BitArray(MAZE_SIZE * MAZE_SIZE);

            // Copia solo i bit validi
            int copyLength = Mathf.Min(bitArray.Length, correctSizeBitArray.Length);

            for (int i = 0; i < copyLength; i++)
            {
                correctSizeBitArray[i] = bitArray[i];
            }

            Debug.Log($"BitArray deserializzato: {copyLength}/{correctSizeBitArray.Length} bit copiati");

            return correctSizeBitArray;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Errore deserializzazione BitArray: {e.Message}");
            return null;
        }
    }

    public void Save(ref FogData data)
    {
        BitArray bitArray = GetFogBitArray();
        data.fogBitArrayData = SerializeFogBitArray(bitArray);
        data.isInitialized = true;
        data.canPlayerPass = canPlayerPassFog;

        Debug.Log($"FogManager: Dati nebbia salvati. Matrice con {GetMatrixFogCount()} tile nebbia");
    }

    public void Load(FogData data)
    {
        if(!data.isInitialized || string.IsNullOrEmpty(data.fogBitArrayData))
        {
            Debug.Log("FogManager: Nessun dato nebbia da caricare, uso prefab");
            ApplyFogFromPrefab();
            return;
        }

        BitArray bitArray = DeserializeFogBitArray(data.fogBitArrayData);
        if(bitArray != null)
        {
            SetFogBitArray(bitArray);
            ApplyFogFromSave();
            Debug.Log($"FogManager: Dati nebbia caricati con successo. {GetMatrixFogCount()} tile nebbia");
        }
        else
        {
            Debug.LogError("FogManager: Errore nel caricamento dati nebbia, uso prefab");
            ApplyFogFromPrefab();
        }
        canPlayerPassFog = data.canPlayerPass;
    }

    #endregion

    [ContextMenu("Debug Bounds and Coordinates")]
    public void DebugBoundsAndCoordinates()
    {
        if (mainTilemap != null)
        {
            BoundsInt bounds = mainTilemap.cellBounds;
            Debug.Log($"MainTilemap bounds: {bounds}");
            Debug.Log($"MainTilemap size: {bounds.size}");
        }

        if (fogTilemap != null)
        {
            BoundsInt fogBounds = fogTilemap.cellBounds;
            Debug.Log($"FogTilemap bounds: {fogBounds}");
            Debug.Log($"FogTilemap size: {fogBounds.size}");
        }

        if (fogPrefab != null)
        {
            GameObject temp = Instantiate(fogPrefab);
            Tilemap prefabTilemap = temp.GetComponent<Tilemap>();
            if (prefabTilemap != null)
            {
                Debug.Log($"Prefab tilemap bounds: {prefabTilemap.cellBounds}");
            }
            Destroy(temp);
        }
    }
}

//FOR SAVE AND LOAD
[System.Serializable]
public struct FogData
{
    public string fogBitArrayData;
    public bool isInitialized;
    public bool canPlayerPass;
}