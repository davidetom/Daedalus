using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FogWarningUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject fogWarningPanel;           // Il panel da attivare/disattivare
    public TextMeshProUGUI fogWarningText;       // Il testo di warning
    public TextMeshProUGUI visorNeededText;

    [Header("Animation (Optional)")]
    public bool useAnimation = true;
    public float animationSpeed = 2f;
    
    private bool isWarningActive = false;
    private Coroutine blinkCoroutine;
    
    void OnEnable()
    {
        // Sottoscrivi agli eventi del FogManager
        FogManager.OnPlayerEnteredWarningZone += ShowWarning;
        FogManager.OnPlayerExitedWarningZone += HideWarning;
    }
    
    void OnDisable()
    {
        // Rimuovi la sottoscrizione agli eventi
        FogManager.OnPlayerEnteredWarningZone -= ShowWarning;
        FogManager.OnPlayerExitedWarningZone -= HideWarning;
    }
    
    void Start()
    {
        // Assicurati che il warning sia nascosto all'inizio
        if (fogWarningPanel != null)
            fogWarningPanel.SetActive(false);
    }
    
    void ShowWarning()
    {
        if (isWarningActive) return;
        
        isWarningActive = true;
        
        if (fogWarningPanel != null)
        {
            fogWarningPanel.SetActive(true);
        }
        
        // Avvia animazione lampeggiante se abilitata
        if (useAnimation && fogWarningText != null && visorNeededText != null)
        {
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkText());
        }
        
        Debug.Log("Player entered warning zone!");
    }
    
    void HideWarning()
    {
        if (!isWarningActive) return;
        
        isWarningActive = false;
        
        if (fogWarningPanel != null)
        {
            fogWarningPanel.SetActive(false);
        }
        
        // Ferma l'animazione
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        // Ripristina il colore del testo
        if (fogWarningText != null)
        {
            Color textColor = fogWarningText.color;
            textColor.a = 1f;
            fogWarningText.color = textColor;
        }

        // Ripristina il colore del testo
        if (visorNeededText != null)
        {
            Color textColor = visorNeededText.color;
            textColor.a = 1f;
            visorNeededText.color = textColor;
        }
        
        Debug.Log("Player exited warning zone!");
    }
    
    System.Collections.IEnumerator BlinkText()
    {
        if (fogWarningText == null || visorNeededText == null) yield break;
        
        Color originalFogWarningColor = fogWarningText.color;
        Color originalVisorNeededColor = visorNeededText.color;
        
        while (isWarningActive)
        {
            // Fade out
            float alpha = 1f;
            while (alpha > 0.3f && isWarningActive)
            {
                alpha -= Time.deltaTime * animationSpeed;
                Color newFogWarningColor = originalFogWarningColor;
                Color newVisorNeededColor = originalVisorNeededColor;
                newFogWarningColor.a = alpha;
                newVisorNeededColor.a = alpha;
                fogWarningText.color = newFogWarningColor;
                visorNeededText.color = newVisorNeededColor;
                yield return null;
            }

            // Fade in
            while (alpha < 1f && isWarningActive)
            {
                alpha += Time.deltaTime * animationSpeed;
                Color newFogWarningColor = originalFogWarningColor;
                Color newVisorNeededColor = originalVisorNeededColor;
                newFogWarningColor.a = alpha;
                newVisorNeededColor.a = alpha;
                fogWarningText.color = newFogWarningColor;
                visorNeededText.color = newVisorNeededColor;
                yield return null;
            }
        }
    }
    
    // Metodi pubblici per controllo manuale (opzionali)
    public void ForceShowWarning()
    {
        ShowWarning();
    }
    
    public void ForceHideWarning()
    {
        HideWarning();
    }
    
    public bool IsWarningActive()
    {
        return isWarningActive;
    }
}