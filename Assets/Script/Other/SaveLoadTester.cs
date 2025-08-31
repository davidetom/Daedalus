using UnityEngine;

public class SaveLoadTester : MonoBehaviour
{
    //FOR NOW THE LOADING IS DONE BY PRESSING THE KEY "1"

    public PlayerController player;
    public CoinUIManager coin;
    public InventoryManager inventory;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SaveSystem.Load();
        }
    }

    public void SaveGame()
    {
        SaveSystem.Save(player, coin, inventory);
    }
}
