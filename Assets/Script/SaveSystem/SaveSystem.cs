using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEditor.Build.Content;
using UnityEngine.InputSystem;
using System.Collections;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using NUnit.Framework;
public class SaveSystem
{
    public static SaveData _saveData = new SaveData();

    [System.Serializable]
    public struct SaveData
    {
        public PlayerSaveData playerData;
        public CurrencyData currencyData;
        public InventoryData inventoryData;
        public DayNightSaveData dayNightSaveData;
        public ShopData shopData;
        //PROVA SAVE SYSTEM PER HUB
        public HubData hubData;
        public int sceneIndex;
    }

    //Nome file locale personalizzato per utente
    public static string SaveFileName()
    {
        //string saveFile = Application.persistentDataPath + "/save" + ".save";
        //return saveFile;
        //NEW FOR FIREBASE
        string userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "guest";
        return Application.persistentDataPath + "/save_" + userId + ".save";
    }

    //Controlla se esiste un salvataggio in locale
    public static bool SaveExists()
    {
        return File.Exists(SaveFileName());
    }

    public static void Save(PlayerController player, CoinUIManager coin, InventoryManager inventory, DayNightCycleManager dayNight, ShopManager shop, OuterHubController hub)
    {
        try
        {
            player.Save(ref _saveData.playerData);
            coin.Save(ref _saveData.currencyData);
            _saveData.inventoryData = inventory.SaveInventory();
            dayNight.Save(ref _saveData.dayNightSaveData);
            hub.Save(ref _saveData.hubData);

            if (shop != null)
            {
                shop.Save(ref _saveData.shopData);
                Debug.Log("Dati shop salvati");
            }
            else
            {
                ShopManager foundShop = GameObject.FindFirstObjectByType<ShopManager>();
                if (foundShop != null)
                {
                    foundShop.Save(ref _saveData.shopData);
                    Debug.Log("ShopManager trovato automaticamente e salvato");
                }
            }
            _saveData.sceneIndex = SceneManager.GetActiveScene().buildIndex;

            string json = JsonUtility.ToJson(_saveData, true);

            //Salvataggio in locale
            File.WriteAllText(SaveFileName(), json);
            Debug.Log("Salvataggio locale completato in: " + SaveFileName());

            //Salvataggio su Firebase
            SaveToFirebase(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Errore durante il salvataggio: " + e.Message);
        }
    }

    //Prima prova Load dal cloud
    public static void Load()
    {
        LoadFromFirebase(success =>
        {
            if (!success)
            {
                Debug.LogWarning("Caricamento dal cloud fallito, provo dal locale...");
                LoadLocal();
            }
        });
    }


    public static void LoadLocal()
    {
        if (!SaveExists())
        {
            Debug.LogWarning("Nessun file di salvataggio trovato!");
            return;
        }
        try
        {
            string saveContent = File.ReadAllText(SaveFileName());
            _saveData = JsonUtility.FromJson<SaveData>(saveContent);

            Debug.Log("=== CARICAMENTO INIZIATO ===");
            Debug.Log("Dati caricati - Monete: " + _saveData.currencyData.CurrencyAmount);
            Debug.Log("Scena da caricare: " + _saveData.sceneIndex);

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(_saveData.sceneIndex);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Errore durante il caricamento: " + e.Message);
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Usa una coroutine per aspettare che gli oggetti siano pronti
        MonoBehaviour anyMonoBehaviour = GameObject.FindFirstObjectByType<MonoBehaviour>();
        if (anyMonoBehaviour != null)
        {
            anyMonoBehaviour.StartCoroutine(LoadDataAfterDelay());
        }
    }

    private static IEnumerator LoadDataAfterDelay()
    {
        // Aspetta qualche frame per essere sicuri che tutto sia inizializzato
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        CoinUIManager coin = GameObject.FindFirstObjectByType<CoinUIManager>();
        PlayerController player = GameObject.FindFirstObjectByType<PlayerController>();
        InventoryManager inventory = GameObject.FindFirstObjectByType<InventoryManager>();
        DayNightCycleManager dayNight = GameObject.FindFirstObjectByType<DayNightCycleManager>();
        ShopManager shop = GameObject.FindFirstObjectByType<ShopManager>();
        OuterHubController hub = GameObject.FindFirstObjectByType<OuterHubController>();

        Debug.Log("=== TENTATIVO CARICAMENTO COMPONENTI ===");
        Debug.Log("CoinUIManager trovato: " + (coin != null));
        Debug.Log("PlayerController trovato: " + (player != null));
        Debug.Log("DayNightCycleManager trovato: " + (dayNight != null));

        if (player != null && coin != null && dayNight != null && shop != null)
        {
            player.Load(_saveData.playerData);
            coin.Load(_saveData.currencyData);
            inventory.LoadInventory(_saveData.inventoryData);
            dayNight.Load(_saveData.dayNightSaveData);
            shop.Load(_saveData.shopData);
            hub.Load(_saveData.hubData);
            Debug.Log("Dati caricati in scena!");
        }
        else
        {
            Debug.LogError("SaveSystem: Componenti non trovati nella scena!");
        }
    }

    //SALVA SU FIREBASE
    private static void SaveToFirebase(string json)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogWarning("Utente non autenticato, salvataggio cloud ignorato");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("users").Document(user.UserId);

        docRef.UpdateAsync(new Dictionary<string, object>
        {
            {"gameSave", json }
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
                Debug.Log("Salvataggio su Firebase completato");
            else
                Debug.LogError("Errore salvataggio su Firebase: " + task.Exception);
        });
    }

    //LOAD FROM FIREBASE
    private static void LoadFromFirebase(Action<bool> onComplete)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogWarning("Utente non autenticato, caricamento cloud ignorato");
            onComplete(false);
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("users").Document(user.UserId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.ContainsField("gameSave"))
                {
                    string json = snapshot.GetValue<string>("gameSave");
                    _saveData = JsonUtility.FromJson<SaveData>(json);

                    Debug.Log("Dati caricati da Firebase");
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    SceneManager.LoadScene(_saveData.sceneIndex);
                    onComplete(true);
                }
                else
                {
                    Debug.Log("Nessun salvataggio cloud trovato");
                    onComplete(false);
                }
            }
            else
            {
                Debug.LogError("Errore caricamento da Firebase: " + task.Exception);
                onComplete(false);
            }
        });
    }

    //Metodo per resettare i dati quando l'utente non è loggato
    public static void ClearData()
    {
        _saveData = new SaveData();
        Debug.Log("SaveSystem: dati in memoria azzerati");
    }
}


