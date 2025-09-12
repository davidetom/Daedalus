using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveLoadTester : MonoBehaviour
{
    public PlayerController player;
    public InventoryManager inventory;
    public DayNightCycleManager dayNight;
    public ShopManager shop;
    public OuterHubController hub;
    public GemSpawner gem;
    public void Load()
    {
        SaveSystem.Load();
    }

    public void SaveGame()
    {
        SaveSystem.Save(player, inventory, dayNight, shop, hub, gem);
    }
}