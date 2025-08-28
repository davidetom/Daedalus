using UnityEngine;
using TMPro; // Se usi TextMeshPro

public class CoinUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI coinText;
    
    [Header("Settings")]
    public string coinPrefix = "x";
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
}
