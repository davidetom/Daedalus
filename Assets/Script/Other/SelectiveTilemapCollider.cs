using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "WallOnlyTile", menuName = "2D/Tiles/WallOnlyTile")]
public class WallOnlyTile : TileBase
{
    public Sprite sprite;
    public Tile.ColliderType colliderType = Tile.ColliderType.Sprite;
    
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = sprite;
        tileData.colliderType = colliderType;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockTransform;
    }
}

// Script alternativo per ignorare tile specifici nei collider
public class SelectiveTilemapCollider : MonoBehaviour
{
    [Header("Tilemap Settings")]
    public Tilemap tilemap;
    public TileBase muraTile;
    public TileBase corridoioTile;
    public TileBase pratoTile;
    
    private TilemapCollider2D tilemapCollider;
    private CompositeCollider2D compositeCollider;
    
    public void GenerateColliders()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap non assegnata!");
            return;
        }
        
        // Rimuovi collider esistenti
        ClearColliders();
        
        // Approach 1: Usa TilemapCollider2D nativo con tile personalizzati
        SetupNativeTilemapCollider();
        
        Debug.Log("Collider nativo configurato - solo i tile muro avranno collider");
    }
    
    private void SetupNativeTilemapCollider()
    {
        // Aggiungi TilemapCollider2D
        tilemapCollider = gameObject.AddComponent<TilemapCollider2D>();
        
        // Aggiungi CompositeCollider2D per ottimizzazione
        Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        
        compositeCollider = gameObject.AddComponent<CompositeCollider2D>();
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Outlines;
        
        // Configura il TilemapCollider2D per usare il composite
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        
        // TRUCCO: Modifica i tile per avere collider solo sui muri
        ModifyTileColliders();
    }
    
    private void ModifyTileColliders()
    {
        if (muraTile == null || corridoioTile == null || pratoTile == null)
        {
            Debug.LogWarning("Alcuni tile non sono assegnati - usando approccio alternativo");
            return;
        }
        
        // Ottieni bounds della tilemap
        BoundsInt bounds = tilemap.cellBounds;
        
        // Crea una copia dei tile con collider solo per i muri
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase currentTile = tilemap.GetTile(position);
                
                if (currentTile == corridoioTile || currentTile == pratoTile)
                {
                    // Per corridoi e prato, rimuovi il tile e rimettilo senza collider
                    SetTileWithoutCollider(position, currentTile);
                }
                // I tile muro mantengono il loro collider naturale
            }
        }
    }
    
    private void SetTileWithoutCollider(Vector3Int position, TileBase originalTile)
    {
        // Crea un tile temporaneo senza collider
        // Questo è un workaround - idealmente useresti tile personalizzati
        tilemap.SetTile(position, originalTile);
        
        // Usa SetColliderType per rimuovere il collider da questa posizione specifica
        tilemap.SetColliderType(position, Tile.ColliderType.None);
    }
    
    private void ClearColliders()
    {
        // Rimuovi tutti i componenti collider esistenti
        TilemapCollider2D existingTilemapCollider = GetComponent<TilemapCollider2D>();
        if (existingTilemapCollider != null)
            DestroyImmediate(existingTilemapCollider);
            
        CompositeCollider2D existingComposite = GetComponent<CompositeCollider2D>();
        if (existingComposite != null)
            DestroyImmediate(existingComposite);
            
        Rigidbody2D existingRb = GetComponent<Rigidbody2D>();
        if (existingRb != null)
            DestroyImmediate(existingRb);
    }
    
    [ContextMenu("Rigenera Collider")]
    public void RegenerateColliders()
    {
        GenerateColliders();
    }
    
    [ContextMenu("Reset Collider Types")]
    public void ResetColliderTypes()
    {
        if (tilemap == null) return;
        
        BoundsInt bounds = tilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase currentTile = tilemap.GetTile(position);
                
                if (currentTile == muraTile)
                {
                    tilemap.SetColliderType(position, Tile.ColliderType.Sprite);
                }
                else if (currentTile == corridoioTile || currentTile == pratoTile)
                {
                    tilemap.SetColliderType(position, Tile.ColliderType.None);
                }
            }
        }
        
        Debug.Log("Collider types resettati per tutti i tile");
    }
}