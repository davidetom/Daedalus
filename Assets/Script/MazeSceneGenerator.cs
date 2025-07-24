using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class MazeSceneGenerator : MonoBehaviour
{
    [Header("Prefab Settings")]
    public GameObject tilemapPrefab; // Prefab base con Tilemap e TilemapRenderer
    
    [Header("Tiles")]
    public TileBase muraTile;
    public TileBase corridoioTile;
    public TileBase pratoTile;
    
    [Header("Generation Settings")]
    public bool generaAllAvvio = false;
    public string cartellaScene = "Assets/Scenes/Labirinti/";
    public string cartellaPrefab = "Assets/Prefabs/Labirinti/";
    
    [Header("Scene Template")]
    public GameObject playerPrefab;
    public GameObject lightPrefab;
    public Camera cameraPrefab;
    
    void Start()
    {
        if (generaAllAvvio)
        {
            GeneraTuttiILabirinti();
        }
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Genera Tutti i Labirinti")]
    public void GeneraTuttiILabirinti()
    {
        // Crea le cartelle se non esistono
        if (!Directory.Exists(cartellaScene))
            Directory.CreateDirectory(cartellaScene);
        if (!Directory.Exists(cartellaPrefab))
            Directory.CreateDirectory(cartellaPrefab);
        
        for (int i = 1; i <= 8; i++)
        {
            GeneraScenaLabirinto(i);
        }
        
        AssetDatabase.Refresh();
        Debug.Log("Tutte le 8 scene dei labirinti sono state generate!");
    }
    
    private void GeneraScenaLabirinto(int numeroLabirinto)
    {
        string nomeFile = $"labirinto_{numeroLabirinto:D2}.txt";
        string percorsoCompleto = Path.Combine(Application.streamingAssetsPath, nomeFile);
        
        if (!File.Exists(percorsoCompleto))
        {
            Debug.LogError($"File non trovato: {percorsoCompleto}");
            return;
        }
        
        // Crea nuova scena
        Scene nuovaScena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(nuovaScena);
        
        // Genera la tilemap
        GameObject tilemapObj = GeneraTilemapDaFile(percorsoCompleto, numeroLabirinto);
        
        if (tilemapObj != null)
        {
            // Aggiungi elementi base alla scena
            AggiungiElementiScena(tilemapObj, numeroLabirinto);
            
            // Salva come prefab
            string percorsoPrefab = $"{cartellaPrefab}Labirinto_{numeroLabirinto:D2}.prefab";
            PrefabUtility.SaveAsPrefabAsset(tilemapObj, percorsoPrefab);
            
            // Salva la scena
            string percorsoScena = $"{cartellaScene}Labirinto_{numeroLabirinto:D2}.unity";
            EditorSceneManager.SaveScene(nuovaScena, percorsoScena);
            
            Debug.Log($"Scena e prefab labirinto {numeroLabirinto} salvati!");
        }
        
        // Chiudi la scena
        EditorSceneManager.CloseScene(nuovaScena, true);
    }
    
    private GameObject GeneraTilemapDaFile(string percorsoFile, int numeroLabirinto)
    {
        try
        {
            // Leggi i dati del labirinto
            string[] righe = File.ReadAllLines(percorsoFile);
            int mazeSize = int.Parse(righe[0]);
            int[,] mazeData = new int[mazeSize, mazeSize];
            
            for (int i = 0; i < mazeSize; i++)
            {
                string riga = righe[i + 1];
                for (int j = 0; j < mazeSize; j++)
                {
                    mazeData[i, j] = int.Parse(riga[j].ToString());
                }
            }
            
            // Crea il GameObject tilemap
            GameObject tilemapObj;
            if (tilemapPrefab != null)
            {
                tilemapObj = Instantiate(tilemapPrefab);
            }
            else
            {
                tilemapObj = new GameObject($"Labirinto_{numeroLabirinto:D2}");
                tilemapObj.AddComponent<Grid>();
                
                GameObject tilemapChild = new GameObject("Tilemap");
                tilemapChild.transform.SetParent(tilemapObj.transform);
                tilemapChild.AddComponent<TilemapRenderer>();
                tilemapChild.AddComponent<Tilemap>();
            }
            
            // Ottieni la tilemap
            Tilemap tilemap = tilemapObj.GetComponentInChildren<Tilemap>();
            if (tilemap == null)
            {
                Debug.LogError("Tilemap non trovata nel prefab!");
                return null;
            }
            
            // Popola la tilemap
            for (int i = 0; i < mazeSize; i++)
            {
                for (int j = 0; j < mazeSize; j++)
                {
                    Vector3Int posizione = new Vector3Int(j, mazeSize - 1 - i, 0);
                    TileBase tileSelezionata = null;
                    
                    switch (mazeData[i, j])
                    {
                        case 0: tileSelezionata = muraTile; break;
                        case 1: tileSelezionata = corridoioTile; break;
                        case 2: tileSelezionata = pratoTile; break;
                    }
                    
                    if (tileSelezionata != null)
                    {
                        tilemap.SetTile(posizione, tileSelezionata);
                    }
                }
            }
            
            // Aggiungi collider se necessario
            TilemapCollider2D collider = tilemapObj.GetComponentInChildren<TilemapCollider2D>();
            if (collider == null)
            {
                tilemap.gameObject.AddComponent<TilemapCollider2D>();
            }
            
            return tilemapObj;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Errore nella generazione del labirinto {numeroLabirinto}: {e.Message}");
            return null;
        }
    }
    
    private void AggiungiElementiScena(GameObject labirintoObj, int numeroLabirinto)
    {
        Vector2Int mazeSize = new Vector2Int(155, 155); // La tua dimensione fissa
        Vector3 centro = new Vector3(mazeSize.x / 2f, mazeSize.y / 2f, 0);
        
        // Aggiungi player se esiste il prefab
        if (playerPrefab != null)
        {
            GameObject player = Instantiate(playerPrefab);
            player.transform.position = new Vector3(mazeSize.x / 2f, 1, 0); // Posizione di partenza
            player.name = "Player";
        }
        
        // Aggiungi camera
        GameObject cameraObj;
        if (cameraPrefab != null)
        {
            cameraObj = Instantiate(cameraPrefab.gameObject);
        }
        else
        {
            cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = mazeSize.x * 0.6f;
        }
        
        cameraObj.transform.position = new Vector3(centro.x, centro.y, -10);
        cameraObj.tag = "MainCamera";
        
        // Aggiungi luce se esiste il prefab
        if (lightPrefab != null)
        {
            GameObject light = Instantiate(lightPrefab);
            light.transform.position = centro;
            light.name = "Directional Light";
        }
        else
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light lightComp = lightObj.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        }
    }
    
    [ContextMenu("Genera Solo Prefab")]
    public void GeneraSoloPrefab()
    {
        if (!Directory.Exists(cartellaPrefab))
            Directory.CreateDirectory(cartellaPrefab);
        
        for (int i = 1; i <= 8; i++)
        {
            string nomeFile = $"labirinto_{i:D2}.txt";
            string percorsoCompleto = Path.Combine(Application.streamingAssetsPath, nomeFile);
            
            if (File.Exists(percorsoCompleto))
            {
                GameObject tilemapObj = GeneraTilemapDaFile(percorsoCompleto, i);
                if (tilemapObj != null)
                {
                    string percorsoPrefab = $"{cartellaPrefab}Labirinto_{i:D2}.prefab";
                    PrefabUtility.SaveAsPrefabAsset(tilemapObj, percorsoPrefab);
                    DestroyImmediate(tilemapObj);
                    Debug.Log($"Prefab labirinto {i} salvato!");
                }
            }
        }
        
        AssetDatabase.Refresh();
    }
    #endif
}