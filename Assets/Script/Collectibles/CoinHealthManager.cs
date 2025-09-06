using UnityEngine;
using TMPro; // Se usi TextMeshPro

public class CoinUIManager : MonoBehaviour
{

    [Header("UI References")]
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI healthText;
    
    [Header("Settings")]
    public string coinPrefix = "x";
    public string healthSuffix = "/100"; // da modificare se si compra il powerup vita
    public PlayerController player;

    void Start()
    {
        // Aggiorna subito il testo
        UpdateCoinDisplay();
    }
    
    void Update()
    {
        UpdateCoinDisplay();
    }
    
    public void UpdateCoinDisplay()
    {
        if (coinText != null && player != null)
        {
            coinText.text = coinPrefix + player.coinsPicked.ToString();
        }
    }

    public void UpdateHealthDisplay()
    {
        if (healthText != null && player != null)
        {
            if (healthText.gameObject.activeInHierarchy)
            {
                healthText.text = player.GetCurrentHealth().ToString() + healthSuffix;
            }
        }
    }
    //SAVE AND LOAD
    #region Save and Load
    public void Save(ref CurrencyData data)
    {
        data.CurrencyAmount = player.coinsPicked;
    }

    public void Load(CurrencyData data)
    {
        player.coinsPicked = data.CurrencyAmount;
        UpdateCoinDisplay();
    }

    #endregion
}

[System.Serializable]
public struct CurrencyData
{
    public int CurrencyAmount;
}
