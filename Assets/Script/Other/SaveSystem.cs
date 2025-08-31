using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEditor.Build.Content;
using UnityEngine.InputSystem;
using System.Collections;

public class SaveSystem
{
    public static SaveData _saveData = new SaveData();

    [System.Serializable]
    public struct SaveData
    {
        public PlayerSaveData playerData;
        public CurrencyData currencyData;
        public InventoryData inventoryData;
        public int sceneIndex;
    }

    public static string SaveFileName()
    {
        string saveFile = Application.persistentDataPath + "/save" + ".save";
        return saveFile;
    }

    //Metodo per verificare se esiste un salvataggio
    public static bool SaveExists()
    {
        return File.Exists(SaveFileName());
    }

    public static void Save(PlayerController player, CoinUIManager coin, InventoryManager inventory)
    {
        try
        {
            player.Save(ref _saveData.playerData);
            coin.Save(ref _saveData.currencyData);
            _saveData.inventoryData = inventory.SaveInventory();
            _saveData.sceneIndex = SceneManager.GetActiveScene().buildIndex;

            string json = JsonUtility.ToJson(_saveData, true);
            File.WriteAllText(SaveFileName(), json);

            Debug.Log("=== SALVATAGGIO COMPLETATO ===");
            Debug.Log("Scena: " + _saveData.sceneIndex);
            Debug.Log("Monete salvate: " + _saveData.currencyData.CurrencyAmount);
            Debug.Log("File salvato in: " + SaveFileName());
        }
        catch(System.Exception e)
        {
            Debug.LogError("Errore durante il salvataggio: " + e.Message);
        }
    }

    public static void Load()
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
        catch(System.Exception e)
        {
            Debug.LogError("Errore durante il caricamento: " + e.Message);
        }
    }

    /**
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        CoinUIManager coin = GameObject.FindFirstObjectByType<CoinUIManager>();
        PlayerController player = GameObject.FindFirstObjectByType<PlayerController>();
        //InventoryManager inventory = GameObject.FindFirstObjectByType<InventoryManager>();
        if (player != null && coin != null)
        {
            player.Load(_saveData.playerData);
            coin.Load(_saveData.currencyData);
            //inventory.LoadInventory(_saveData.inventoryData);
            Debug.Log("Player e monete caricate con successo!" + _saveData.currencyData.CurrencyAmount);
        }
        else
        {
            Debug.LogWarning("SaveSystem: PlayerController non trovato nella scena");
        }
    }
    **/
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

        Debug.Log("=== TENTATIVO CARICAMENTO COMPONENTI ===");
        Debug.Log("CoinUIManager trovato: " + (coin != null));
        Debug.Log("PlayerController trovato: " + (player != null));

        if (player != null && coin != null)
        {
            player.Load(_saveData.playerData);
            coin.Load(_saveData.currencyData);
            inventory.LoadInventory(_saveData.inventoryData);

            Debug.Log("=== CARICAMENTO COMPLETATO ===");
            Debug.Log("Monete caricate: " + _saveData.currencyData.CurrencyAmount);
        }
        else
        {
            Debug.LogError("SaveSystem: Componenti non trovati nella scena!");
            if (coin == null) Debug.LogError("CoinUIManager non trovato!");
            if (player == null) Debug.LogError("PlayerController non trovato!");
            if (inventory == null) Debug.LogError("InventoryManager non trovato!");
        }
    }
}

//IT'S MISSING TIME OF THE DAY TO SAVE


