using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;

public class FogManager : MonoBehaviour
{
    [Header("Tilemap Settings")]
    public Tilemap mainTilemap;
    public Tilemap fogTilemap;
    public TileBase fogTile;
    public GameObject fogPrefab;

    [Header("Fog Settings")]
    public Vector3Int centerCell;
    public float minDist = 3f;
    public float maxDist = 8f;
    public Color fogColor = Color.gray;

    [Header("Warning Zone Settings")]
    public Vector3Int warningCenterCell = new Vector3Int(155, 155, 0);
    public float warningMinDist = 117f;
    public float warningMaxDist = 119f;
    public bool canPlayerPassFog = false;

    // Eventi per la warning zone
    public static System.Action OnPlayerEnteredWarningZone;
    public static System.Action OnPlayerExitedWarningZone;

    // Stato interno
    private bool isPlayerInWarningZone = false;
    private bool[,] matriceNebbia = new bool[310, 310];
    private const int MAZE_SIZE = 310;

    // Cache ottimizzazioni
    private BoundsInt cachedBounds;
    private bool boundsInitialized = false;
    private bool isInitialized = false;
    
    // Cache per warning zone
    private Vector3Int lastPlayerCellPos = Vector3Int.zero;
    private float lastWarningCheck = 0f;
    private const float WARNING_CHECK_INTERVAL = 0.1f; // Controlla ogni 100ms invece che ogni frame

    void Start()
    {
        SetupRenderOrder();
        CacheBounds();
        
        // Inizializzazione in background completamente asincrona
        StartCoroutine(InitializeFogBackground());
    }

    private void SetupRenderOrder()
    {
        if (fogTilemap != null && mainTilemap != null)
        {
            var fogRenderer = fogTilemap.GetComponent<TilemapRenderer>();
            var mainRenderer = mainTilemap.GetComponent<TilemapRenderer>();

            if (fogRenderer != null && mainRenderer != null)
            {
                fogRenderer.sortingOrder = mainRenderer.sortingOrder + 2;
            }
        }
    }

    private void CacheBounds()
    {
        if (!boundsInitialized)
        {
            cachedBounds = mainTilemap != null ? mainTilemap.cellBounds : fogTilemap.cellBounds;
            boundsInitialized = true;
        }
    }

    // Inizializzazione completamente in background
    private IEnumerator InitializeFogBackground()
    {
        // Aspetta che altri sistemi si inizializzino
        yield return new WaitForSeconds(0.5f);
        
        // Inizializza la matrice immediatamente (operazione veloce)
        InitializeMatrix();
        
        // Disabilita temporaneamente il rendering per evitare lag visivi
        var fogRenderer = fogTilemap.GetComponent<TilemapRenderer>();
        bool wasEnabled = fogRenderer.enabled;
        fogRenderer.enabled = false;

        try
        {
            if (ShouldLoadFromSave())
            {
                yield return StartCoroutine(LoadFogSilently());
            }
            else
            {
                yield return StartCoroutine(LoadPrefabSilently());
            }
        }
        finally
        {
            // Riabilita il rendering solo alla fine
            fogRenderer.enabled = wasEnabled;
            isInitialized = true;
            
            //Debug.Log("FogManager: Inizializzazione completata in background");
        }
    }

    // Caricamento silenzioso senza impatto sul gameplay
    private IEnumerator LoadFogSilently()
    {
        // Carica dati dal save system se disponibili
        if (!SaveSystem.HasFogSaveData())
        {
            yield return StartCoroutine(LoadPrefabSilently());
            yield break;
        }

        yield return StartCoroutine(LoadPrefabSilently());
    }

    // Caricamento prefab ottimizzato
    private IEnumerator LoadPrefabSilently()
    {
        if (fogPrefab == null || fogTilemap == null)
        {
            //Debug.LogError("FogManager: Componenti mancanti");
            yield break;
        }

        // Pulisci senza operazioni costose
        ClearFogTilemapFast();
        yield return null;

        GameObject prefabInstance = Instantiate(fogPrefab);
        Tilemap prefabTilemap = prefabInstance.GetComponent<Tilemap>();

        if (prefabTilemap == null)
        {
            Destroy(prefabInstance);
            yield break;
        }

        // Copia SOLO la matrice, non la tilemap (molto più veloce)
        yield return StartCoroutine(CopyPrefabToMatrix(prefabTilemap));
        
        Destroy(prefabInstance);
        
        // Applica alla tilemap in micro-batch per non causare lag
        yield return StartCoroutine(ApplyMatrixToTilemapSilently());
    }

    // Copia solo nella matrice (operazione veloce)
    private IEnumerator CopyPrefabToMatrix(Tilemap sourceTilemap)
    {
        BoundsInt bounds = sourceTilemap.cellBounds;
        TileBase[] allTiles = sourceTilemap.GetTilesBlock(bounds);

        int index = 0;
        int processed = 0;
        const int PROCESS_PER_FRAME = 1000; // Processa 1000 posizioni per frame

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (allTiles[index] != null)
            {
                UpdateMatrixPositionFast(pos, true);
            }

            index++;
            processed++;

            // Pausa solo ogni 1000 operazioni
            if (processed >= PROCESS_PER_FRAME)
            {
                processed = 0;
                yield return null;
            }
        }
    }

    // Applica matrice alla tilemap in micro-batch
    private IEnumerator ApplyMatrixToTilemapSilently()
    {
        // Pre-calcola tutte le posizioni (operazione veloce)
        List<Vector3Int> fogPositions = new List<Vector3Int>();
        
        for (int x = 0; x < MAZE_SIZE; x++)
        {
            for (int y = 0; y < MAZE_SIZE; y++)
            {
                if (matriceNebbia[x, y])
                {
                    fogPositions.Add(MatrixToTileCoordinatesFast(x, y));
                }
            }
        }

        // Applica in micro-batch da 10 tile per frame
        const int MICRO_BATCH = 10;
        
        for (int i = 0; i < fogPositions.Count; i += MICRO_BATCH)
        {
            int end = Mathf.Min(i + MICRO_BATCH, fogPositions.Count);
            
            for (int j = i; j < end; j++)
            {
                fogTilemap.SetTile(fogPositions[j], fogTile);
                fogTilemap.SetColor(fogPositions[j], fogColor);
            }
            
            // Pausa ogni micro-batch
            yield return null;
        }
    }

    // Pulizia ottimizzata per build
    private void ClearFogTilemapFast()
    {
        if (fogTilemap == null) return;
        
        // In build, usa il metodo più performante
        if (!Application.isEditor)
        {
            // Metodo ottimizzato per build: disabilita il collider temporaneamente
            var collider = fogTilemap.GetComponent<TilemapCollider2D>();
            bool hadCollider = collider != null && collider.enabled;
            
            if (hadCollider)
                collider.enabled = false;
                
            try
            {
                fogTilemap.CompressBounds();
                BoundsInt bounds = fogTilemap.cellBounds;
                if (bounds.size.x > 0 && bounds.size.y > 0)
                {
                    fogTilemap.SetTilesBlock(bounds, new TileBase[bounds.size.x * bounds.size.y]);
                }
            }
            finally
            {
                if (hadCollider)
                    collider.enabled = true;
            }
        }
        else
        {
            // Metodo standard per editor
            fogTilemap.CompressBounds();
            BoundsInt bounds = fogTilemap.cellBounds;
            if (bounds.size.x > 0 && bounds.size.y > 0)
            {
                fogTilemap.SetTilesBlock(bounds, new TileBase[bounds.size.x * bounds.size.y]);
            }
        }
    }

    private bool ShouldLoadFromSave()
    {
        return SaveSystem.HasFogSaveData() && !SaveSystem.isNewGame;
    }

    // OTTIMIZZATO: Update matrice più veloce
    private void UpdateMatrixPositionFast(Vector3Int tilePosition, bool hasFog)
    {
        int matrixX = tilePosition.x - cachedBounds.xMin;
        int matrixY = tilePosition.y - cachedBounds.yMin;

        if (matrixX >= 0 && matrixX < MAZE_SIZE && matrixY >= 0 && matrixY < MAZE_SIZE)
        {
            matriceNebbia[matrixX, matrixY] = hasFog;
        }
    }

    private Vector3Int MatrixToTileCoordinatesFast(int matrixX, int matrixY)
    {
        return new Vector3Int(
            matrixX + cachedBounds.xMin,
            matrixY + cachedBounds.yMin,
            0
        );
    }

    // OTTIMIZZATO: Warning zone con cache e intervalli
    public void CheckPlayerWarningZone(Vector3 playerWorldPosition)
    {
        // Controlla solo ogni WARNING_CHECK_INTERVAL secondi
        if (Time.time - lastWarningCheck < WARNING_CHECK_INTERVAL)
            return;
            
        lastWarningCheck = Time.time;

        if (canPlayerPassFog)
        {
            if (isPlayerInWarningZone)
            {
                isPlayerInWarningZone = false;
                OnPlayerExitedWarningZone?.Invoke();
            }
            return;
        }

        if (mainTilemap == null) return;

        Tilemap tilemapToUse = fogTilemap != null ? fogTilemap : mainTilemap;
        Vector3Int playerCellPos = tilemapToUse.WorldToCell(playerWorldPosition);

        // Cache: controlla solo se il player si è davvero mosso
        if (playerCellPos == lastPlayerCellPos)
            return;
            
        lastPlayerCellPos = playerCellPos;

        float distanceFromWarningCenter = Vector3Int.Distance(playerCellPos, warningCenterCell);
        bool playerShouldBeInWarningZone = (distanceFromWarningCenter >= warningMinDist &&
                                           distanceFromWarningCenter <= warningMaxDist);

        if (playerShouldBeInWarningZone && !isPlayerInWarningZone)
        {
            isPlayerInWarningZone = true;
            OnPlayerEnteredWarningZone?.Invoke();
        }
        else if (!playerShouldBeInWarningZone && isPlayerInWarningZone)
        {
            isPlayerInWarningZone = false;
            OnPlayerExitedWarningZone?.Invoke();
        }
    }

    // OTTIMIZZATO: Reveal fog più efficiente
    public void RevealFogAtPosition(Vector3Int position)
    {
        if (!isInitialized) return; // Non fare nulla se non inizializzato
        
        if (fogTilemap != null && fogTilemap.HasTile(position))
        {
            fogTilemap.SetTile(position, null);
            UpdateMatrixPositionFast(position, false);
        }
    }

    public void RevealFogInRadius(Vector3Int centerPosition, float radius)
    {
        if (!isInitialized) return;
        
        // Cache dell'area per evitare ricalcoli
        int radiusInt = Mathf.CeilToInt(radius);
        BoundsInt area = new BoundsInt(
            centerPosition.x - radiusInt,
            centerPosition.y - radiusInt,
            0,
            radiusInt * 2 + 1,
            radiusInt * 2 + 1,
            1
        );

        // Pre-calcola il raggio al quadrato per evitare sqrt
        float radiusSquared = radius * radius;

        foreach (var pos in area.allPositionsWithin)
        {
            // Usa distanza al quadrato (più veloce)
            float distSquared = (pos - centerPosition).sqrMagnitude;
            if (distSquared <= radiusSquared && fogTilemap.HasTile(pos))
            {
                fogTilemap.SetTile(pos, null);
                UpdateMatrixPositionFast(pos, false);
            }
        }
    }

    // Metodi pubblici semplificati
    public void SetPlayerCanPassFog(bool canPass)
    {
        bool previousValue = canPlayerPassFog;
        canPlayerPassFog = canPass;

        if (canPass && !previousValue && isPlayerInWarningZone)
        {
            isPlayerInWarningZone = false;
            OnPlayerExitedWarningZone?.Invoke();
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

    public bool HasFogAtPosition(Vector3Int position)
    {
        return fogTilemap != null && fogTilemap.HasTile(position);
    }

    // Metodi di utilità per matrice
    private void InitializeMatrix()
    {
        // Inizializzazione veloce con Array.Clear (più efficiente del doppio loop)
        System.Array.Clear(matriceNebbia, 0, matriceNebbia.Length);
    }

    private Vector2Int TileToMatrixCoordinates(Vector3Int tilePosition)
    {
        if (!boundsInitialized)
        {
            CacheBounds();
        }

        int matrixX = Mathf.Clamp(tilePosition.x - cachedBounds.xMin, 0, MAZE_SIZE - 1);
        int matrixY = Mathf.Clamp(tilePosition.y - cachedBounds.yMin, 0, MAZE_SIZE - 1);

        return new Vector2Int(matrixX, matrixY);
    }

    // Metodo per sapere se l'inizializzazione è completata
    public bool IsInitialized()
    {
        return isInitialized;
    }

    // Save/Load methods (semplificati)
    public BitArray GetFogBitArray()
    {
        BitArray bitArray = new BitArray(MAZE_SIZE * MAZE_SIZE);
        
        for (int x = 0; x < MAZE_SIZE; x++)
        {
            for (int y = 0; y < MAZE_SIZE; y++)
            {
                bitArray[x * MAZE_SIZE + y] = matriceNebbia[x, y];
            }
        }
        
        return bitArray;
    }

    public void SetFogBitArray(BitArray bitArray)
    {
        if (bitArray == null || bitArray.Length != MAZE_SIZE * MAZE_SIZE)
        {
            //Debug.LogError($"BitArray invalido! Dimensione attesa: {MAZE_SIZE * MAZE_SIZE}");
            return;
        }

        for (int x = 0; x < MAZE_SIZE; x++)
        {
            for (int y = 0; y < MAZE_SIZE; y++)
            {
                matriceNebbia[x, y] = bitArray[x * MAZE_SIZE + y];
            }
        }
    }

    public void Save(ref FogData data)
    {
        BitArray bitArray = GetFogBitArray();
        data.fogBitArrayData = SerializeFogBitArray(bitArray);
        data.isInitialized = true;
        data.canPlayerPass = canPlayerPassFog;
    }

    public void Load(FogData data)
    {
        if (!data.isInitialized || string.IsNullOrEmpty(data.fogBitArrayData))
        {
            // Non fare nulla qui, l'inizializzazione avverrà in background
            return;
        }

        BitArray bitArray = DeserializeFogBitArray(data.fogBitArrayData);
        if (bitArray != null)
        {
            SetFogBitArray(bitArray);
        }
        
        canPlayerPassFog = data.canPlayerPass;
    }

    // Metodi di serializzazione
    public string SerializeFogBitArray(BitArray bitArray)
    {
        if (bitArray == null) return "";
        byte[] bytes = new byte[(bitArray.Length + 7) / 8];
        bitArray.CopyTo(bytes, 0);
        return System.Convert.ToBase64String(bytes);
    }

    public BitArray DeserializeFogBitArray(string serializedData)
    {
        if (string.IsNullOrEmpty(serializedData)) return null;

        try
        {
            byte[] bytes = System.Convert.FromBase64String(serializedData);
            BitArray bitArray = new BitArray(bytes);
            BitArray correctSizeBitArray = new BitArray(MAZE_SIZE * MAZE_SIZE);

            int copyLength = Mathf.Min(bitArray.Length, correctSizeBitArray.Length);
            for (int i = 0; i < copyLength; i++)
            {
                correctSizeBitArray[i] = bitArray[i];
            }

            return correctSizeBitArray;
        }
        catch (System.Exception e)
        {
            //Debug.LogError($"Errore deserializzazione BitArray: {e.Message}");
            return null;
        }
    }

    // Context menu per debug (mantieni solo quelli essenziali)
    [ContextMenu("Debug Matrix State")]
    public void DebugMatrixState()
    {
        int fogCount = 0;
        for (int x = 0; x < MAZE_SIZE; x++)
        {
            for (int y = 0; y < MAZE_SIZE; y++)
            {
                if (matriceNebbia[x, y]) fogCount++;
            }
        }
        
        Debug.Log($"Matrice: {fogCount}/{MAZE_SIZE * MAZE_SIZE} posizioni con nebbia");
        Debug.Log($"Inizializzato: {isInitialized}");
    }
}

[System.Serializable]
public struct FogData
{
    public string fogBitArrayData;
    public bool isInitialized;
    public bool canPlayerPass;
}