// EmergencyTileCreator.cs
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EmergencyTileCreator : MonoBehaviour
{
    [Header("Assegna i tuoi sprite qui")]
    public Sprite spriteMuro;
    public Sprite spriteCorridoio;
    public Sprite spriteMaschera;
    
    [Header("Tiles generati (si riempiono automaticamente)")]
    public Tile tileMuro;
    public Tile tileCorridoio;
    public Tile tileMaschera;
    
    #if UNITY_EDITOR
    [ContextMenu("CREA TILES ORA!")]
    public void CreaTilesSubito()
    {
        string cartella = "Assets/Tiles/Emergency/";
        
        // Crea la cartella
        if (!System.IO.Directory.Exists(cartella))
        {
            System.IO.Directory.CreateDirectory(cartella);
        }
        
        // Crea i tiles
        tileMuro = CreaSingoloTile(spriteMuro, cartella + "MuroTile.asset", true);
        tileCorridoio = CreaSingoloTile(spriteCorridoio, cartella + "CorridoioTile.asset", false);
        tileMaschera = CreaSingoloTile(spriteMaschera, cartella + "MascheraTile.asset", false);
        
        // Aggiorna l'Inspector
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("✅ TILES CREATI CON SUCCESSO!");
        Debug.Log($"Muro: {tileMuro != null}");
        Debug.Log($"Corridoio: {tileCorridoio != null}");
        Debug.Log($"Maschera: {tileMaschera != null}");
    }
    
    private Tile CreaSingoloTile(Sprite sprite, string percorso, bool conCollider)
    {
        if (sprite == null)
        {
            Debug.LogWarning($"Sprite nullo per: {percorso}");
            return null;
        }
        
        // Crea il tile
        Tile nuovoTile = ScriptableObject.CreateInstance<Tile>();
        nuovoTile.sprite = sprite;
        
        // Imposta il collider
        if (conCollider)
            nuovoTile.colliderType = Tile.ColliderType.Sprite;
        else
            nuovoTile.colliderType = Tile.ColliderType.None;
        
        // Salva come asset
        AssetDatabase.CreateAsset(nuovoTile, percorso);
        
        return nuovoTile;
    }
    
    [ContextMenu("Pulisci Tiles")]
    public void PulisciTiles()
    {
        tileMuro = null;
        tileCorridoio = null;
        tileMaschera = null;
        EditorUtility.SetDirty(this);
    }
    #endif
}