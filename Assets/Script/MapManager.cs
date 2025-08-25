using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    [Header("Tilemap Settings")]
    public Tilemap tilemap;
    public TileBase corridoioTile; // Solo questo tile è camminabile
    
    private bool[,] walls;
    private int[,] distances;
    private int mapWidth;
    private int mapHeight;
    private Vector2Int mapOffset; // Offset basato sui bounds della tilemap
    
    public bool[,] Walls => walls;
    public int[,] Distances => distances;
    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public Vector2Int MapOffset => mapOffset;

    public bool wallCalculated = false;

    void Start()
    {
        CalculateMapDimensions();
        InitializeArrays();
        CalculateWallMatrix();
        wallCalculated = true;
    }
    
    void CalculateMapDimensions()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap non assegnata in MapManager!");
            return;
        }
        
        // Ottieni i bounds effettivi della tilemap
        BoundsInt bounds = tilemap.cellBounds;
        
        // Imposta le dimensioni basate sui bounds
        mapWidth = bounds.size.x;
        mapHeight = bounds.size.y;
        mapOffset = new Vector2Int(bounds.xMin, bounds.yMin);
        
        Debug.Log($"Dimensioni mappa calcolate automaticamente: {mapWidth}x{mapHeight}");
        Debug.Log($"Bounds tilemap: min({bounds.xMin}, {bounds.yMin}) max({bounds.xMax}, {bounds.yMax})");
        Debug.Log($"Offset mappa: ({mapOffset.x}, {mapOffset.y})");
    }
    
    void InitializeArrays()
    {
        walls = new bool[mapWidth, mapHeight];
        distances = new int[mapWidth, mapHeight];
    }
    
    public void CalculateWallMatrix()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap non assegnata in MapManager!");
            return;
        }
        
        if (corridoioTile == null)
        {
            Debug.LogError("CorridoioTile non assegnato in MapManager! Tutti i tile saranno considerati muri.");
        }
        
        // Resetta la matrice walls
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                walls[x, y] = true; // Default: tutto è muro
            }
        }
        
        int walkableTiles = 0;
        
        // Scansiona tutta la tilemap usando le dimensioni calcolate
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Converti coordinate array in coordinate tilemap
                Vector3Int cellPosition = new Vector3Int(
                    mapOffset.x + x, 
                    mapOffset.y + y, 
                    0
                );
                
                // Controlla se c'è un tile in questa posizione
                TileBase tileAtPosition = tilemap.GetTile(cellPosition);
                
                // Solo il corridoioTile è camminabile, tutto il resto è muro
                bool isWalkable = (corridoioTile != null && tileAtPosition == corridoioTile);
                
                walls[x, y] = !isWalkable;
                
                if (isWalkable) walkableTiles++;
            }
        }
        
        Debug.Log($"Matrice walls calcolata: {mapWidth}x{mapHeight}");
        Debug.Log($"Tile camminabili trovati: {walkableTiles}");
        Debug.Log($"Tile muro: {(mapWidth * mapHeight) - walkableTiles}");
    }
    
    // Metodo per ottenere coordinate array da posizione world
    public Vector2Int WorldToArrayCoordinates(Vector3 worldPos)
    {
        Vector3Int cellPos = tilemap.WorldToCell(worldPos);
        
        int arrayX = cellPos.x - mapOffset.x;
        int arrayY = cellPos.y - mapOffset.y;
        
        return new Vector2Int(arrayX, arrayY);
    }
    
    // Metodo per verificare se una coordinata array è valida
    public bool IsValidArrayCoordinate(Vector2Int arrayPos)
    {
        return arrayPos.x >= 0 && arrayPos.x < mapWidth && 
               arrayPos.y >= 0 && arrayPos.y < mapHeight;
    }
    
    // Metodo per verificare se una posizione world è un muro
    public bool IsWallAtWorldPosition(Vector3 worldPos)
    {
        Vector2Int arrayPos = WorldToArrayCoordinates(worldPos);
        if (!IsValidArrayCoordinate(arrayPos))
            return true; // Fuori bounds = muro
            
        return walls[arrayPos.x, arrayPos.y];
    }
    
    // Metodo per ottenere la distanza da una posizione world
    public int GetDistanceAtWorldPosition(Vector3 worldPos)
    {
        Vector2Int arrayPos = WorldToArrayCoordinates(worldPos);
        if (!IsValidArrayCoordinate(arrayPos))
            return -1; // Fuori bounds
            
        return distances[arrayPos.x, arrayPos.y];
    }
    
    // Metodo per verificare se una posizione world è camminabile
    public bool IsWalkableAtWorldPosition(Vector3 worldPos)
    {
        return !IsWallAtWorldPosition(worldPos);
    }
}