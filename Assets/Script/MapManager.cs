using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public enum TileType
{
    Wall = 0,        // Muro - completamente bloccato
    Corridor = 1,    // Corridoio - camminabile per tutti
    Grass = 2,       // Prato - visibile ma non camminabile
    Door = 3,        // Porta - logica speciale
    Empty = 4        // Tile vuoto - trattato come muro
}

public class MapManager : MonoBehaviour
{
    [Header("Tilemap Settings")]
    public Tilemap tilemap;
    
    [Header("Tile Configuration")]
    [SerializeField] private TileBase[] corridorTiles;    // Tile corridoio (camminabili)
    [SerializeField] private TileBase[] grassTiles;       // Tile prato (visibili ma non camminabili)
    [SerializeField] private TileBase[] wallTiles;        // Tile muro
    [SerializeField] private TileBase[] doorTiles;        // Tile porta (opzionale)
    
    // Backward compatibility
    [Header("Legacy (Deprecated)")]
    public TileBase corridoioTile; // Mantenuto per compatibilità
    
    private TileType[,] tileTypes;   // Nuova matrice con tipi di tile
    private int[,] distances;
    private int mapWidth;
    private int mapHeight;
    private Vector2Int mapOffset;
    
    // HashSets per ricerca veloce
    private HashSet<TileBase> corridorTileSet;
    private HashSet<TileBase> grassTileSet;
    private HashSet<TileBase> wallTileSet;
    private HashSet<TileBase> doorTileSet;
    
    // Proprietà pubbliche
    public TileType[,] TileTypes => tileTypes;
    public int[,] Distances => distances;
    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public Vector2Int MapOffset => mapOffset;

    // Backward compatibility - mantengo walls ma ora è calcolata dai TileTypes
    public bool[,] Walls 
    { 
        get 
        {
            bool[,] walls = new bool[mapWidth, mapHeight];
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    walls[x, y] = !IsWalkableForAI(x, y);
                }
            }
            return walls;
        }
    }

    public bool wallCalculated = false;

    void Start()
    {
        InitializeTileSets();
        CalculateMapDimensions();
        InitializeArrays();
        CalculateTileTypeMatrix();
        wallCalculated = true;
    }
    
    void InitializeTileSets()
    {
        corridorTileSet = new HashSet<TileBase>();
        grassTileSet = new HashSet<TileBase>();
        wallTileSet = new HashSet<TileBase>();
        doorTileSet = new HashSet<TileBase>();
        
        // Aggiungi tile corridoio
        if (corridorTiles != null)
        {
            foreach (var tile in corridorTiles)
            {
                if (tile != null)
                    corridorTileSet.Add(tile);
            }
        }
        
        // Backward compatibility
        if (corridoioTile != null && !corridorTileSet.Contains(corridoioTile))
        {
            corridorTileSet.Add(corridoioTile);
        }
        
        // Aggiungi tile prato
        if (grassTiles != null)
        {
            foreach (var tile in grassTiles)
            {
                if (tile != null)
                    grassTileSet.Add(tile);
            }
        }
        
        // Aggiungi tile muro
        if (wallTiles != null)
        {
            foreach (var tile in wallTiles)
            {
                if (tile != null)
                    wallTileSet.Add(tile);
            }
        }
        
        // Aggiungi tile porta
        if (doorTiles != null)
        {
            foreach (var tile in doorTiles)
            {
                if (tile != null)
                    doorTileSet.Add(tile);
            }
        }
        
        Debug.Log($"Tile configurati - Corridoi: {corridorTileSet.Count}, Prato: {grassTileSet.Count}, Muri: {wallTileSet.Count}, Porte: {doorTileSet.Count}");
    }
    
    void CalculateMapDimensions()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap non assegnata in MapManager!");
            return;
        }
        
        BoundsInt bounds = tilemap.cellBounds;
        mapWidth = bounds.size.x;
        mapHeight = bounds.size.y;
        mapOffset = new Vector2Int(bounds.xMin, bounds.yMin);
        
        Debug.Log($"Dimensioni mappa: {mapWidth}x{mapHeight}");
        Debug.Log($"Offset mappa: ({mapOffset.x}, {mapOffset.y})");
    }
    
    void InitializeArrays()
    {
        tileTypes = new TileType[mapWidth, mapHeight];
        distances = new int[mapWidth, mapHeight];
    }
    
    public void CalculateTileTypeMatrix()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap non assegnata in MapManager!");
            return;
        }
        
        // Resetta la matrice
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                tileTypes[x, y] = TileType.Wall; // Default: muro
            }
        }
        
        int corridorCount = 0, grassCount = 0, wallCount = 0, doorCount = 0, emptyCount = 0;
        
        // Scansiona tutta la tilemap
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int cellPosition = new Vector3Int(
                    mapOffset.x + x, 
                    mapOffset.y + y, 
                    0
                );
                
                TileBase tileAtPosition = tilemap.GetTile(cellPosition);
                
                TileType tileType = TileType.Wall; // Default
                
                if (tileAtPosition == null)
                {
                    tileType = TileType.Empty;
                    emptyCount++;
                }
                else if (corridorTileSet.Contains(tileAtPosition))
                {
                    tileType = TileType.Corridor;
                    corridorCount++;
                }
                else if (grassTileSet.Contains(tileAtPosition))
                {
                    tileType = TileType.Grass;
                    grassCount++;
                }
                else if (wallTileSet.Contains(tileAtPosition))
                {
                    tileType = TileType.Wall;
                    wallCount++;
                }
                else if (doorTileSet.Contains(tileAtPosition))
                {
                    tileType = TileType.Door;
                    doorCount++;
                }
                else
                {
                    // Tile non configurato - default a muro
                    tileType = TileType.Wall;
                    wallCount++;
                }
                
                tileTypes[x, y] = tileType;
            }
        }
        
        Debug.Log($"Matrice TileTypes calcolata: {mapWidth}x{mapHeight}");
        Debug.Log($"Corridoi: {corridorCount}, Prato: {grassCount}, Muri: {wallCount}, Porte: {doorCount}, Vuoti: {emptyCount}");
    }
    
    // Metodi per determinare se una posizione è camminabile per diversi scopi
    
    /// <summary>
    /// Determina se una posizione è camminabile per l'AI (nemici)
    /// Solo i corridoi sono camminabili per l'AI
    /// </summary>
    public bool IsWalkableForAI(int arrayX, int arrayY)
    {
        if (!IsValidArrayCoordinate(arrayX, arrayY))
            return false;
            
        TileType type = tileTypes[arrayX, arrayY];
        return type == TileType.Corridor;
        // Potresti aggiungere TileType.Door qui se i nemici possono attraversare porte aperte
    }
    
    public bool IsWalkableForAI(Vector2Int arrayPos)
    {
        return IsWalkableForAI(arrayPos.x, arrayPos.y);
    }
    
    /// <summary>
    /// Determina se una posizione è camminabile per il player
    /// Corridoi, prato e porte sono camminabili per il player
    /// </summary>
    public bool IsWalkableForPlayer(int arrayX, int arrayY)
    {
        if (!IsValidArrayCoordinate(arrayX, arrayY))
            return false;
            
        TileType type = tileTypes[arrayX, arrayY];
        return type == TileType.Corridor || type == TileType.Grass || type == TileType.Door;
        // La logica delle porte aperte/chiuse sarà gestita nel PlayerController
    }
    
    public bool IsWalkableForPlayer(Vector2Int arrayPos)
    {
        return IsWalkableForPlayer(arrayPos.x, arrayPos.y);
    }
    
    /// <summary>
    /// Determina se una posizione è visibile (non un muro solido)
    /// Corridoi, prato e porte sono visibili
    /// </summary>
    public bool IsVisible(int arrayX, int arrayY)
    {
        if (!IsValidArrayCoordinate(arrayX, arrayY))
            return false;
            
        TileType type = tileTypes[arrayX, arrayY];
        return type == TileType.Corridor || type == TileType.Grass || type == TileType.Door;
    }
    
    public bool IsVisible(Vector2Int arrayPos)
    {
        return IsVisible(arrayPos.x, arrayPos.y);
    }
    
    /// <summary>
    /// Determina se una posizione è valida per spawn di monete
    /// Solo i corridoi sono validi per le monete
    /// </summary>
    public bool IsValidForCoinSpawn(int arrayX, int arrayY)
    {
        if (!IsValidArrayCoordinate(arrayX, arrayY))
            return false;
            
        TileType type = tileTypes[arrayX, arrayY];
        return type == TileType.Corridor;
    }
    
    public bool IsValidForCoinSpawn(Vector2Int arrayPos)
    {
        return IsValidForCoinSpawn(arrayPos.x, arrayPos.y);
    }
    
    // Metodi di utilità per ottenere il tipo di tile
    public TileType GetTileTypeAtArrayPos(int arrayX, int arrayY)
    {
        if (!IsValidArrayCoordinate(arrayX, arrayY))
            return TileType.Wall;
            
        return tileTypes[arrayX, arrayY];
    }
    
    public TileType GetTileTypeAtArrayPos(Vector2Int arrayPos)
    {
        return GetTileTypeAtArrayPos(arrayPos.x, arrayPos.y);
    }
    
    public TileType GetTileTypeAtWorldPos(Vector3 worldPos)
    {
        Vector2Int arrayPos = WorldToArrayCoordinates(worldPos);
        return GetTileTypeAtArrayPos(arrayPos);
    }
    
    // Metodi esistenti adattati al nuovo sistema
    public Vector2Int WorldToArrayCoordinates(Vector3 worldPos)
    {
        Vector3Int cellPos = tilemap.WorldToCell(worldPos);
        int arrayX = cellPos.x - mapOffset.x;
        int arrayY = cellPos.y - mapOffset.y;
        return new Vector2Int(arrayX, arrayY);
    }
    
    public bool IsValidArrayCoordinate(Vector2Int arrayPos)
    {
        return IsValidArrayCoordinate(arrayPos.x, arrayPos.y);
    }
    
    public bool IsValidArrayCoordinate(int arrayX, int arrayY)
    {
        return arrayX >= 0 && arrayX < mapWidth && 
               arrayY >= 0 && arrayY < mapHeight;
    }
    
    // Metodi backward compatibility
    public bool IsWallAtWorldPosition(Vector3 worldPos)
    {
        Vector2Int arrayPos = WorldToArrayCoordinates(worldPos);
        if (!IsValidArrayCoordinate(arrayPos))
            return true;
            
        // Per backward compatibility, considera "muro" tutto ciò che non è camminabile per il player
        return !IsWalkableForPlayer(arrayPos);
    }
    
    public int GetDistanceAtWorldPosition(Vector3 worldPos)
    {
        Vector2Int arrayPos = WorldToArrayCoordinates(worldPos);
        if (!IsValidArrayCoordinate(arrayPos))
            return -1;
            
        return distances[arrayPos.x, arrayPos.y];
    }
    
    public bool IsWalkableAtWorldPosition(Vector3 worldPos)
    {
        return !IsWallAtWorldPosition(worldPos);
    }
    
    // Metodi per aggiungere tile a runtime
    public void AddCorridorTile(TileBase tile)
    {
        if (tile != null && !corridorTileSet.Contains(tile))
        {
            corridorTileSet.Add(tile);
        }
    }
    
    public void AddGrassTile(TileBase tile)
    {
        if (tile != null && !grassTileSet.Contains(tile))
        {
            grassTileSet.Add(tile);
        }
    }
    
    public void AddWallTile(TileBase tile)
    {
        if (tile != null && !wallTileSet.Contains(tile))
        {
            wallTileSet.Add(tile);
        }
    }
}