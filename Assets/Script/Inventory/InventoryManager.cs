using UnityEngine;
using UnityEngine.Rendering;
using Unity.VisualScripting;

public class InventoryManager : MonoBehaviour
{
    public int maxStackedItems = 999;
    public InventorySlot[] inventorySlots;
    public GameObject InventoryItemPrefab;

    //FOR SAVING AND LOADING
    [Header("Item Database")]
    public Item[] allItems;

    public bool AddItem(Item item)
    {
        //Check if any slot has same item lower than max
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            InventorySlot slot = inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
            if (itemInSlot != null &&
                itemInSlot.item == item &&
                itemInSlot.count < maxStackedItems &&
                itemInSlot.item.stackable == true)
            {
                itemInSlot.count++;
                itemInSlot.RefreshCount();
                return true;
            }
        }

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            InventorySlot slot = inventorySlots[i];
            InventoryItem ItemInSlot = slot.GetComponentInChildren<InventoryItem>();
            if(ItemInSlot == null)
            {
                SpawnNewItem(item, slot);
                return true;
            }
        }
        return false;
        //Variable used to display with UI if an Object has been addded
        //To the inventory or not
    }

    void SpawnNewItem(Item item, InventorySlot slot)
    {
        GameObject newItemGo = Instantiate(InventoryItemPrefab, slot.transform);
        InventoryItem inventoryItem = newItemGo.GetComponent<InventoryItem>();
        inventoryItem.InitializeItem(item);
    }

    //Finds the item on the Database using the name
    public Item FindItemByName(string name)
    {
        foreach (var item in allItems)
        {
            if (item.name == name)
                return item;
        }
        Debug.LogWarning($"Item con Nome '{name}' not found");
        return null;
    }

    public bool HasItem(Item item)
    {

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            InventorySlot slot = inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot == null || itemInSlot.item == null)
            {
                continue;
            }
            if (item == itemInSlot.item)
            {
                return true;
            }
        }
        return false;
    }

    public void RemoveItem(Item item)
    {
        if (item == null) return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            InventorySlot slot = inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot == null || itemInSlot.item == null)
                continue;

            if (item == itemInSlot.item)
            {
                if (itemInSlot.count > 1)
                {
                    itemInSlot.count--;
                    itemInSlot.RefreshCount();
                }
                else
                {
                    Destroy(itemInSlot.gameObject);
                }

                return;
            }
        }
    }

    #region SAVE AND LOAD

    public InventoryData SaveInventory()
    {
        InventoryData data = new InventoryData();
        data.slots = new InventorySlotData[inventorySlots.Length];

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            InventorySlot slot = inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot != null)
            {
                data.slots[i].ItemName = itemInSlot.item.name;
                data.slots[i].count = itemInSlot.count;
            }
            else
            {
                data.slots[i].ItemName = "";
                data.slots[i].count = 0;
            }

        }
        return data;
    }

    public void LoadInventory(InventoryData data)
    {
        foreach (var slot in inventorySlots)
        {
            foreach (Transform child in slot.transform)
            {
                Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < data.slots.Length; i++)
        {
            if (!string.IsNullOrEmpty(data.slots[i].ItemName))
            {
                Item itemToAdd = FindItemByName(data.slots[i].ItemName);
                if (itemToAdd != null)
                {
                    GameObject newItemGo = Instantiate(InventoryItemPrefab, inventorySlots[i].transform);
                    InventoryItem inventoryItem = newItemGo.GetComponent<InventoryItem>();
                    inventoryItem.InitializeItem(itemToAdd);
                    inventoryItem.count = data.slots[i].count;
                    inventoryItem.RefreshCount();
                }
            }
        }
    }


    #endregion

}

[System.Serializable]
public struct InventorySlotData
{
    public string ItemName;
    public int count;
}

[System.Serializable]
public struct InventoryData
{
    public InventorySlotData[] slots;
}