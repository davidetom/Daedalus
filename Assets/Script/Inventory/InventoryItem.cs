using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{

    [Header("UI")]
    public Image image;
    public Text countText;

    [HideInInspector] public Item item;
    //For the max stack
    [HideInInspector] public int count = 1;

    public void InitializeItem(Item newItem)
    {
        item = newItem;
        image.sprite = newItem.image;
        RefreshCount();
    }

    public void RefreshCount()
    {
        countText.text = count.ToString();
        bool textActive = count > 1;
        countText.gameObject.SetActive(textActive);
    }
}
