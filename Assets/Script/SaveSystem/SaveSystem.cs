using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.InputSystem;
using System.Collections;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
public class SaveSystem
{
    //Iniziata nuova partita o caricamento salvataggio?
    public static bool isNewGame = false;

    private static Dictionary<string, SaveData> _userSaveData = new Dictionary<string, SaveData>();

    [System.Serializable]
    public struct SaveData
    {
        public PlayerSaveData playerData;
        public InventoryData inventoryData;
        public DayNightSaveData dayNightSaveData;
        public ShopData shopData;
        public HubData hubData;
        public DifficultyData difficultyData;
        public GemData gemData;
        public FogData fogData;
        public int sceneIndex;
        public MazeData mazeData;
    }

    private static string GetCurrentUserId()
    {
        return FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "guest_" + SystemInfo.deviceUniqueIdentifier;
    }

    private static SaveData GetCurrentUserSaveData()
    {
        string userId = GetCurrentUserId();
        if (!_userSaveData.ContainsKey(userId))
        {
            _userSaveData[userId] = new SaveData();
        }
        return _userSaveData[userId];
    }

    // Imposta i dati di salvataggio per l'utente corrente
    private static void SetCurrentUserSaveData(SaveData data)
    {
        string userId = GetCurrentUserId();
        _userSaveData[userId] = data;
    }

    // Nome file locale personalizzato per utente
    public static string SaveFileName()
    {
        string userId = GetCurrentUserId();
        return Application.persistentDataPath + "/save_" + userId + ".save";
    }

    // Controlla se esiste un salvataggio locale per l'utente corrente
    public static bool SaveExists()
    {
        return File.Exists(SaveFileName());
    }

    public static void Save(PlayerController player, InventoryManager inventory,
                       DayNightCycleManager dayNight, ShopManager shop, OuterHubController hub, GemSpawner gem)
    {
        try
        {
            SaveData currentSaveData = GetCurrentUserSaveData();

            player.Save(ref currentSaveData.playerData);
            currentSaveData.inventoryData = inventory.SaveInventory();
            dayNight.Save(ref currentSaveData.dayNightSaveData);
            hub.Save(ref currentSaveData.hubData);

            MazeManager mazeManager = GameObject.FindFirstObjectByType<MazeManager>();
            if (mazeManager != null)
            {
                mazeManager.Save(ref currentSaveData.mazeData);
            }

            if (shop != null)
                {
                    shop.Save(ref currentSaveData.shopData);
                    //Debug.Log("Dati shop salvati");
                }
                else
                {
                    ShopManager foundShop = GameObject.FindFirstObjectByType<ShopManager>();
                    if (foundShop != null)
                    {
                        foundShop.Save(ref currentSaveData.shopData);
                        //Debug.Log("ShopManager trovato automaticamente e salvato");
                    }
                }

            DifficultyManager difficultyManager = GameObject.FindFirstObjectByType<DifficultyManager>();
            if(difficultyManager != null)
            {
                difficultyManager.Save(ref currentSaveData.difficultyData);
                //Debug.Log("Difficolt� salvata!");
            }
            else
            {
                //DifficultyManager non trovato, salva a normal di default
                currentSaveData.difficultyData.difficultyLevel = (int)DifficultyLevel.Normal;
            }

            gem.Save(ref currentSaveData.gemData);

            FogManager fogManager = GameObject.FindFirstObjectByType<FogManager>();
            if(fogManager != null)
            {
                fogManager.Save(ref currentSaveData.fogData);
                //Debug.Log("Dati nebbia salvati");
            }

            currentSaveData.sceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Aggiorna i dati in memoria per l'utente corrente
            SetCurrentUserSaveData(currentSaveData);

            string json = JsonUtility.ToJson(currentSaveData, true);

            // Salvataggio locale
            File.WriteAllText(SaveFileName(), json);
            Debug.Log($"Salvataggio locale completato per utente {GetCurrentUserId()}: {SaveFileName()}");

            // Salvataggio su Firebase
            SaveToFirebase(json);

            //Torna al men� principale
            SceneManager.LoadScene("MainMenu");
            Time.timeScale = 1f;
        }
        catch //(System.Exception e)
        {
            //Debug.LogError("Errore durante il salvataggio: " + e.Message);
        }
    }

    // Carica prima dal cloud, poi dal locale se necessario
    public static void Load()
    {
        LoadFromFirebase(success =>
        {
            if (!success)
            {
                //Debug.LogWarning($"Caricamento dal cloud fallito per {GetCurrentUserId()}, provo dal locale...");
                LoadLocal();
            }
        });
    }

    public static void LoadLocal()
    {
        if (!SaveExists())
        {
            //Debug.LogWarning($"Nessun file di salvataggio trovato per {GetCurrentUserId()}!");
            return;
        }

        try
        {
            isNewGame = false;

            string saveContent = File.ReadAllText(SaveFileName());
            SaveData loadedData = JsonUtility.FromJson<SaveData>(saveContent);

            // Memorizza i dati per l'utente corrente
            SetCurrentUserSaveData(loadedData);

            //Debug.Log($"=== CARICAMENTO LOCALE INIZIATO PER {GetCurrentUserId()} ===");
            //Debug.Log("Dati caricati - Monete: " + loadedData.currencyData.CurrencyAmount);
            //Debug.Log("Scena da caricare: " + loadedData.sceneIndex);

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(loadedData.sceneIndex);
        }
        catch //(System.Exception e)
        {
            //Debug.LogError("Errore durante il caricamento locale: " + e.Message);
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        MonoBehaviour anyMonoBehaviour = GameObject.FindFirstObjectByType<MonoBehaviour>();
        if (anyMonoBehaviour != null)
        {
            anyMonoBehaviour.StartCoroutine(LoadDataAfterDelay());
        }
    }

    private static IEnumerator LoadDataAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Ottieni i dati per l'utente corrente
        SaveData currentUserData = GetCurrentUserSaveData();
    
        PlayerController player = GameObject.FindFirstObjectByType<PlayerController>();
        InventoryManager inventory = GameObject.FindFirstObjectByType<InventoryManager>();
        DayNightCycleManager dayNight = GameObject.FindFirstObjectByType<DayNightCycleManager>();
        ShopManager shop = GameObject.FindFirstObjectByType<ShopManager>();
        OuterHubController hub = GameObject.FindFirstObjectByType<OuterHubController>();
        DifficultyManager difficultyManager = GameObject.FindFirstObjectByType<DifficultyManager>();
        GameElementsManager gameElementsManager = GameObject.FindFirstObjectByType<GameElementsManager>();
        GemSpawner gemSpawner = GameObject.FindFirstObjectByType<GemSpawner>();
        FogManager fogManager = GameObject.FindFirstObjectByType<FogManager>();
        MazeManager mazeManager = GameObject.FindFirstObjectByType<MazeManager>();

        //Debug.Log($"=== CARICAMENTO COMPONENTI PER {GetCurrentUserId()} ===");
        //Debug.Log("CoinUIManager trovato: " + (coin != null));
        //Debug.Log("PlayerController trovato: " + (player != null));
        //Debug.Log("DayNightCycleManager trovato: " + (dayNight != null));
        //Debug.Log("FogManager trovato: " + (fogManager != null));

        if (player != null && dayNight != null && shop != null)
        {
            player.Load(currentUserData.playerData);
            inventory.LoadInventory(currentUserData.inventoryData);
            if (player.HasPotion())
                player.UpdateHealth();
            dayNight.Load(currentUserData.dayNightSaveData);
            shop.Load(currentUserData.shopData);
            hub.Load(currentUserData.hubData);

            if (difficultyManager != null)
            {
                difficultyManager.Load(currentUserData.difficultyData);
                //Debug.Log("Difficolt� caricata!");

                yield return new WaitForEndOfFrame();
            }

            if (fogManager != null)
            {
                fogManager.Load(currentUserData.fogData);
                //Debug.Log("Dati nebbia caricati!");
            }

            if (mazeManager != null)
            {
                mazeManager.Load(currentUserData.mazeData);
            }


            gameElementsManager.ConfigureGameElements();
            gemSpawner.Load(currentUserData.gemData);

            //Debug.Log($"Dati caricati in scena per utente {GetCurrentUserId()}!");
        }
        else
        {
            //Debug.LogError("SaveSystem: Componenti non trovati nella scena!");
        }
    }

    // Salva su Firebase
    private static void SaveToFirebase(string json)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            //Debug.LogWarning("Utente non autenticato, salvataggio cloud ignorato");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("users").Document(user.UserId);

        var updateData = new Dictionary<string, object>
        {
            {"gameSave", json},
            {"lastSaveTime", Timestamp.GetCurrentTimestamp()} // Aggiungi timestamp
        };

        docRef.UpdateAsync(updateData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log($"Salvataggio su Firebase completato per {user.UserId}");
            }
            else
            {
                Debug.LogError($"Errore salvataggio su Firebase per {user.UserId}: " + task.Exception);
            }
        });
    }

    // Carica da Firebase
    private static void LoadFromFirebase(Action<bool> onComplete)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            //Debug.LogWarning("Utente non autenticato, caricamento cloud ignorato");
            onComplete(false);
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("users").Document(user.UserId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.ContainsField("gameSave"))
                {
                    string json = snapshot.GetValue<string>("gameSave");
                    SaveData loadedData = JsonUtility.FromJson<SaveData>(json);

                    // Memorizza i dati per l'utente corrente
                    SetCurrentUserSaveData(loadedData);

                    Debug.Log($"Dati caricati da Firebase per {user.UserId}");
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    SceneManager.LoadScene(loadedData.sceneIndex);
                    onComplete(true);
                }
                else
                {
                    Debug.Log($"Nessun salvataggio cloud trovato per {user.UserId}");
                    onComplete(false);
                }
            }
            else
            {
                Debug.LogError($"Errore caricamento da Firebase per {user.UserId}: " + task.Exception);
                onComplete(false);
            }
        });
    }

    // Metodo per pulire i dati quando l'utente fa logout
    public static void ClearCurrentUserData()
    {
        string userId = GetCurrentUserId();
        if (_userSaveData.ContainsKey(userId))
        {
            _userSaveData.Remove(userId);
            //Debug.Log($"SaveSystem: dati rimossi dalla memoria per {userId}");
        }

        // Rimuovi anche il file locale se esiste
        if (SaveExists())
        {
            try
            {
                File.Delete(SaveFileName());
                //Debug.Log($"File locale rimosso per {userId}");
            }
            catch //(System.Exception e)
            {
                //Debug.LogError($"Errore rimozione file locale: {e.Message}");
            }
        }
    }

    // Metodo per pulire TUTTI i dati (per debug o reset completo)
    public static void ClearAllData()
    {
        _userSaveData.Clear();
        //Debug.Log("SaveSystem: tutti i dati in memoria azzerati");
    }

    //Se nuova partita
    public static void NewGame()
    {
        isNewGame = true;

        //Carica scena di gioco
        SceneManager.LoadScene("Labirinto");
    }

    public static bool HasFogSaveData()
    {
        if (!SaveExists()) return false;

        SaveData currentData = GetCurrentUserSaveData();
        return currentData.fogData.isInitialized && !string.IsNullOrEmpty(currentData.fogData.fogBitArrayData);
    }
}