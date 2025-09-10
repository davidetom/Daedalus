using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WrongDoorWarningUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject wrongDoorPanel;           // Il panel da attivare/disattivare
    public TextMeshProUGUI wrongDoorText;       // Il testo di warning

    [Header("Animation Settings")]
    public bool useAnimation = true;
    public float animationSpeed = 2f;
    
    [Header("Timer Settings")]
    public float warningDuration = 3f;          // Durata del warning in secondi
    
    private bool isWarningActive = false;
    private Coroutine blinkCoroutine;
    private Coroutine timerCoroutine;
    private float lastAttemptTime = 0f;
    
    void OnEnable()
    {
        // Sottoscrivi agli eventi del DoorController
        DoorController.OnWrongDoorAttempt += ShowWarning;
    }
    
    void OnDisable()
    {
        // Rimuovi la sottoscrizione agli eventi
        DoorController.OnWrongDoorAttempt -= ShowWarning;
    }
    
    void Start()
    {
        // Assicurati che il warning sia nascosto all'inizio
        if (wrongDoorPanel != null)
            wrongDoorPanel.SetActive(false);
    }
    
    void ShowWarning()
    {
        // Aggiorna il tempo dell'ultimo tentativo
        lastAttemptTime = Time.time;
        
        // Se il warning è già attivo, resetta solo il timer
        if (isWarningActive)
        {
            ResetTimer();
            return;
        }
        
        // Attiva il warning per la prima volta
        isWarningActive = true;
        
        if (wrongDoorPanel != null)
        {
            wrongDoorPanel.SetActive(true);
        }
        
        // Avvia animazione lampeggiante se abilitata
        if (useAnimation && wrongDoorText != null)
        {
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkText());
        }
        
        // Avvia il timer per nascondere il warning
        StartTimer();
        
        //Debug.Log("Wrong door warning shown!");
    }
    
    void StartTimer()
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(WarningTimer());
    }
    
    void ResetTimer()
    {
        // Ferma il timer corrente e ne avvia uno nuovo
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(WarningTimer());
        
        //Debug.Log("Wrong door warning timer reset!");
    }
    
    System.Collections.IEnumerator WarningTimer()
    {
        float startTime = Time.time;
        
        while (Time.time - startTime < warningDuration)
        {
            // Se c'è stato un nuovo tentativo durante l'attesa, resetta il timer
            if (Time.time - lastAttemptTime < warningDuration)
            {
                startTime = lastAttemptTime;
            }
            
            yield return null;
        }
        
        // Timer scaduto, nascondi il warning
        HideWarning();
    }
    
    void HideWarning()
    {
        if (!isWarningActive) return;
        
        isWarningActive = false;
        
        if (wrongDoorPanel != null)
        {
            wrongDoorPanel.SetActive(false);
        }
        
        // Ferma l'animazione
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        // Ferma il timer
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        // Ripristina il colore del testo
        if (wrongDoorText != null)
        {
            Color textColor = wrongDoorText.color;
            textColor.a = 1f;
            wrongDoorText.color = textColor;
        }
        
        //Debug.Log("Wrong door warning hidden!");
    }
    
    System.Collections.IEnumerator BlinkText()
    {
        if (wrongDoorText == null) yield break;
        
        Color originalColor = wrongDoorText.color;
        
        while (isWarningActive)
        {
            // Fade out
            float alpha = 1f;
            while (alpha > 0.3f && isWarningActive)
            {
                alpha -= Time.deltaTime * animationSpeed;
                Color newColor = originalColor;
                newColor.a = alpha;
                wrongDoorText.color = newColor;
                yield return null;
            }

            // Fade in
            while (alpha < 1f && isWarningActive)
            {
                alpha += Time.deltaTime * animationSpeed;
                Color newColor = originalColor;
                newColor.a = alpha;
                wrongDoorText.color = newColor;
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
    
    // Metodo per cambiare la durata del warning da Inspector o codice
    public void SetWarningDuration(float duration)
    {
        warningDuration = duration;
    }
}