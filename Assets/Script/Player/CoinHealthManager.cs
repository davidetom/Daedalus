using UnityEngine;
using TMPro;

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
        UpdateHealthDisplay();
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
            healthText.text = player.GetCurrentHealth().ToString() + healthSuffix;
        }
    }
}