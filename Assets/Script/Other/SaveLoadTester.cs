using UnityEngine;

public class SaveLoadTester : MonoBehaviour
{
    public PlayerController player;
    public CoinUIManager coin;
    public InventoryManager inventory;
    public DayNightCycleManager dayNight;
    
    public void Load()
    {
        SaveSystem.Load();
    }

    public void SaveGame()
    {
        SaveSystem.Save(player, coin, inventory, dayNight);
    }
}
