using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Settings")]
    public ShopItem[] shopItems;
    public TextMeshProUGUI playerMoneyText;

    [Header("References")]
    public InventoryManager inventoryManager;
    public CoinUIManager coinUIManager;
    public PlayerController playerController;

    [Header("UI Feedback")]
    public GameObject insufficientFundsPanel;
    public TextMeshProUGUI insufficientFundsMessage;
    public float messageDuration = 2f;

    //Per il pannello di fondi insufficienti
    private Coroutine hideMessageCoroutine;

    void Start()
    {
        InitializeShop();
        coinUIManager.UpdateCoinDisplay();
    }

    void InitializeShop()
    {
        for (int i = 0; i < shopItems.Length; i++)
        {
            int index = i;
            shopItems[i].purchaseButton.onClick.AddListener(() => PurchaseItem(index));
           
            if (shopItems[i].isPurchased)
            {
                HandlePurchasedItem(shopItems[i]);
            }
        }
    }

    public void PurchaseItem(int itemIndex)
    {
        ShopItem item = shopItems[itemIndex];

        if (CanPurchase(item))
        {
            if (item.requiresGems)
            {
                ConsumeRequiredGems(item);
                Debug.Log($"Purchased: {item.itemData.name} with gems");
            }
            else
            {
                playerController.coinsPicked -= item.price;
                Debug.Log($"Purchased: {item.itemData.name} for {item.price}$ - Remaining coins: {playerController.coinsPicked}");
            }

            item.isPurchased = true;
            //SUONO OGGETTO COMPRATO
            AudioManager.Instance.PlayItemPurchase();

            //Aggiungi all'inventario
            if (inventoryManager != null)
            {
                inventoryManager.AddItem(item.itemData);
                playerController.PowerUpEnabled();
            }

            //Aggiorna UI monete
            coinUIManager.UpdateCoinDisplay();

            //Disabilita il bottone e cambia aspetto
            HandlePurchasedItem(item);

            Debug.Log($"Purchased: {item.itemData.name} for {item.price}$ - Remaining coins: {playerController.coinsPicked}");
        }
        else
        {
            ShowInsufficientFundsMessage();
        }
    }

    private bool CanPurchase(ShopItem item)
    {
        if (playerController == null)
        {
            Debug.LogWarning("PlayerController reference is missing!");
            return false;
        }

        if (item.isPurchased)
            return false;

        if (item.requiresGems)
        {
            return HasRequiredGems(item);
        }

        return playerController.coinsPicked >= item.price && !item.isPurchased;
    }

    private bool HasRequiredGems(ShopItem item)
    {
        if (inventoryManager == null || item.requiredGems == null)
            return false;

        for (int i = 0; i < item.requiredGems.Length; i++)
        {
            Item requiredGem = item.requiredGems[i];
            
            if (!inventoryManager.HasItem(requiredGem))
            {
                Debug.Log("Inventory doesn't have the " + requiredGem.name);
                return false;
            }
        }
        return true;
    }

    private void ConsumeRequiredGems(ShopItem item)
    {
        if (inventoryManager == null || item.requiredGems == null)
            return;
        for (int i = 0; i < item.requiredGems.Length; i++)
        {
            Item requiredGem = item.requiredGems[i];

            inventoryManager.RemoveItem(requiredGem);
        }
    }

    private void HandlePurchasedItem(ShopItem item)
    {
        item.purchaseButton.gameObject.SetActive(false);
        item.soldButton.gameObject.SetActive(true);
        item.ItemOnTable.gameObject.SetActive(true);
    }

    private void ShowInsufficientFundsMessage()
    {
        if (insufficientFundsPanel != null)
        {
            insufficientFundsPanel.SetActive(true);
            if (insufficientFundsMessage != null)
            {
                insufficientFundsMessage.text = $"You don't have the required resources!";
            }

            if (hideMessageCoroutine != null)
            {
                StopCoroutine(hideMessageCoroutine);
            }

            hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay());
        }
        else
        {
            Debug.Log($"Insufficient funds for purchase! Current coins: {playerController.coinsPicked}");
        }
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        if (insufficientFundsPanel != null)
        {
            insufficientFundsPanel.SetActive(false);
        }
        hideMessageCoroutine = null;
    }

    //METODI UTILI
    public int GetPlayerCoins()
    {
        return playerController != null ? playerController.coinsPicked : 0;
    }

    public bool IsItemPurchased(int itemIndex)
    {
        return itemIndex >= 0 && itemIndex < shopItems.Length && shopItems[itemIndex].isPurchased;
    }

    #region SAVE AND LOAD

    public void Save(ref ShopData data)
    {
        data.purchasedItems = new bool[shopItems.Length];
        for (int i = 0; i < shopItems.Length; i++)
        {
            data.purchasedItems[i] = shopItems[i].isPurchased;
        }
    }

    public void Load(ShopData data)
    {
        if (data.purchasedItems != null)
        {
            for (int i = 0; i < shopItems.Length && i < data.purchasedItems.Length; i++)
            {
                shopItems[i].isPurchased = data.purchasedItems[i];
                if (shopItems[i].isPurchased)
                {
                    HandlePurchasedItem(shopItems[i]);
                }
                else
                {
                    if (shopItems[i].ItemOnTable != null)
                    {
                        shopItems[i].ItemOnTable.SetActive(false);
                    }
                }
            }
            coinUIManager.UpdateCoinDisplay();
            Debug.Log("Shop data loaded - Items: " + data.purchasedItems.Length);
        }
    }

    #endregion
}

[System.Serializable]
public class ShopItem
{
    public Item itemData;
    public int price;
    public Button purchaseButton;
    public Button soldButton;
    public TextMeshProUGUI priceText;
    public GameObject ItemOnTable;
    public bool isPurchased = false;

    [Header("Special Purchase Type")]
    public bool requiresGems = false;
    public Item[] requiredGems;
}

//FOR SAVE AND LOAD
[System.Serializable]
public struct ShopData
{
    public bool[] purchasedItems;
}
