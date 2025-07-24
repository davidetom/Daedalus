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
    
    private void SetupOptimizedCollider(Tilemap tilemap, int[,] mazeData, int mazeSize)
    {
        // Approccio 1: Usa TilemapCollider2D nativo
        TilemapCollider2D tilemapCollider = tilemap.gameObject.AddComponent<TilemapCollider2D>();
        
        // Aggiungi CompositeCollider2D per un singolo collider ottimizzato
        Rigidbody2D rb = tilemap.gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        
        CompositeCollider2D compositeCollider = tilemap.gameObject.AddComponent<CompositeCollider2D>();
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Outlines;
        
        // Configura per combinare in un singolo collider
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        
        // TRUCCO EFFICIENTE: Imposta ColliderType per ogni tile
        for (int i = 0; i < mazeSize; i++)
        {
            for (int j = 0; j < mazeSize; j++)
            {
                Vector3Int posizione = new Vector3Int(j, mazeSize - 1 - i, 0);
                
                if (mazeData[i, j] == 0) // Muro
                {
                    tilemap.SetColliderType(posizione, Tile.ColliderType.Sprite);
                }
                else // Corridoio o prato
                {
                    tilemap.SetColliderType(posizione, Tile.ColliderType.None);
                }
            }
        }
        
        Debug.Log("Collider nativo ottimizzato configurato - UN SOLO collider composito creato!");
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
            // Controlli di sicurezza iniziali
            Debug.Log($"Generando labirinto {numeroLabirinto}...");
            
            // Verifica che i tile siano assegnati
            if (muraTile == null)
            {
                Debug.LogError("muraTile non è assegnato nell'inspector!");
                return null;
            }
            if (corridoioTile == null)
            {
                Debug.LogError("corridoioTile non è assegnato nell'inspector!");
                return null;
            }
            if (pratoTile == null)
            {
                Debug.LogError("pratoTile non è assegnato nell'inspector!");
                return null;
            }
            
            // Leggi i dati del labirinto
            string[] righe = File.ReadAllLines(percorsoFile);
            if (righe == null || righe.Length == 0)
            {
                Debug.LogError($"File vuoto o non leggibile: {percorsoFile}");
                return null;
            }
            
            int mazeSize = int.Parse(righe[0]);
            Debug.Log($"Dimensione labirinto: {mazeSize}x{mazeSize}");
            
            int[,] mazeData = new int[mazeSize, mazeSize];
            
            for (int i = 0; i < mazeSize; i++)
            {
                if (i + 1 >= righe.Length)
                {
                    Debug.LogError($"File del labirinto incompleto alla riga {i + 1}");
                    return null;
                }
                
                string riga = righe[i + 1];
                if (riga.Length < mazeSize)
                {
                    Debug.LogError($"Riga {i + 1} troppo corta: {riga.Length} caratteri invece di {mazeSize}");
                    return null;
                }
                
                for (int j = 0; j < mazeSize; j++)
                {
                    if (!int.TryParse(riga[j].ToString(), out mazeData[i, j]))
                    {
                        Debug.LogError($"Carattere non valido alla posizione [{i},{j}]: '{riga[j]}'");
                        return null;
                    }
                }
            }
            
            // Crea il GameObject tilemap con controlli migliorati
            GameObject tilemapObj = null;
            Tilemap tilemap = null;
            
            if (tilemapPrefab != null)
            {
                Debug.Log("Usando tilemapPrefab...");
                tilemapObj = Instantiate(tilemapPrefab);
                
                // Cerca la Tilemap in modo più robusto
                tilemap = tilemapObj.GetComponent<Tilemap>();
                if (tilemap == null)
                {
                    tilemap = tilemapObj.GetComponentInChildren<Tilemap>();
                }
                
                // Se ancora non trova la tilemap, verifica la struttura del prefab
                if (tilemap == null)
                {
                    Debug.LogError($"Il prefab {tilemapPrefab.name} non contiene un componente Tilemap!");
                    Debug.LogError("Struttura del prefab:");
                    LogGameObjectStructure(tilemapObj, 0);
                    
                    // Fallback: crea manualmente la struttura
                    Debug.Log("Creando manualmente la struttura Tilemap...");
                    CreateTilemapStructure(tilemapObj, out tilemap);
                }
            }
            else
            {
                Debug.Log("Creando nuovo GameObject tilemap...");
                tilemapObj = new GameObject($"Labirinto_{numeroLabirinto:D2}");
                CreateTilemapStructure(tilemapObj, out tilemap);
            }
            
            if (tilemap == null)
            {
                Debug.LogError("Impossibile creare o trovare il componente Tilemap!");
                return null;
            }
            
            Debug.Log($"Tilemap trovata: {tilemap.name} su GameObject: {tilemap.gameObject.name}");
            
            // Popola la tilemap con tutti i tile
            Debug.Log("Popolando la tilemap...");
            int muriCount = 0, corridoiCount = 0, pratoCount = 0;
            
            for (int i = 0; i < mazeSize; i++)
            {
                for (int j = 0; j < mazeSize; j++)
                {
                    Vector3Int posizione = new Vector3Int(j, mazeSize - 1 - i, 0);
                    TileBase tileSelezionata = null;
                    
                    switch (mazeData[i, j])
                    {
                        case 0: 
                            tileSelezionata = muraTile; 
                            muriCount++;
                            break;
                        case 1: 
                            tileSelezionata = corridoioTile; 
                            corridoiCount++;
                            break;
                        case 2: 
                            tileSelezionata = pratoTile; 
                            pratoCount++;
                            break;
                    }
                    
                    if (tileSelezionata != null)
                    {
                        tilemap.SetTile(posizione, tileSelezionata);
                    }
                }
            }
            
            Debug.Log($"Tilemap popolata - Muri: {muriCount}, Corridoi: {corridoiCount}, Prato: {pratoCount}");
            
            // Aggiungi il collider nativo ottimizzato
            GameObject tilemapGameObject = tilemap.gameObject;
            
            // Rimuovi eventuali collider esistenti
            TilemapCollider2D existingCollider = tilemapGameObject.GetComponent<TilemapCollider2D>();
            if (existingCollider != null)
            {
                DestroyImmediate(existingCollider);
            }
            
            // Usa l'approccio nativo più efficiente
            SetupOptimizedCollider(tilemap, mazeData, mazeSize);
            
            return tilemapObj;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Errore nella generazione del labirinto {numeroLabirinto}: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return null;
        }
    }
    
    // Nuova funzione per creare la struttura Tilemap manualmente
    private void CreateTilemapStructure(GameObject parent, out Tilemap tilemap)
    {
        // Assicurati che il parent abbia un Grid component
        Grid grid = parent.GetComponent<Grid>();
        if (grid == null)
        {
            grid = parent.AddComponent<Grid>();
        }
        
        // Crea il child GameObject per la Tilemap
        GameObject tilemapChild = new GameObject("Tilemap");
        tilemapChild.transform.SetParent(parent.transform);
        
        // Aggiungi i componenti necessari
        tilemap = tilemapChild.AddComponent<Tilemap>();
        TilemapRenderer renderer = tilemapChild.AddComponent<TilemapRenderer>();
        
        Debug.Log($"Struttura Tilemap creata manualmente: {tilemap.name}");
    }
    
    // Funzione di debug per loggare la struttura del GameObject
    private void LogGameObjectStructure(GameObject obj, int depth)
    {
        string indent = new string(' ', depth * 2);
        Component[] components = obj.GetComponents<Component>();
        string componentList = "";
        foreach (Component comp in components)
        {
            componentList += comp.GetType().Name + " ";
        }
        
        Debug.Log($"{indent}{obj.name} - Components: {componentList}");
        
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            LogGameObjectStructure(obj.transform.GetChild(i).gameObject, depth + 1);
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