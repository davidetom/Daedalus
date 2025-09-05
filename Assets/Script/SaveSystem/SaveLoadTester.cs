using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveLoadTester : MonoBehaviour
{
    public PlayerController player;
    public CoinUIManager coin;
    public InventoryManager inventory;
    public DayNightCycleManager dayNight;
    public ShopManager shop;
    public OuterHubController hub;
    public void Load()
    {
        SaveSystem.Load();
    }

    public void SaveGame()
    {
        SaveSystem.Save(player, coin, inventory, dayNight, shop, hub);

    }
}
