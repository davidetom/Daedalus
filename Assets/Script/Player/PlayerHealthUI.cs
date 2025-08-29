using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI Riferimenti")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;
    public Image fillImage; // immagine di riempimento della barra
    public RectTransform hpBarTransform; // il RectTransform della barra

    [Header("Player Reference")]
    public PlayerController playerController; // Riferimento al PlayerController

    [Header("Colori")]
    public Color fullHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public Color damageFlashColor = Color.red;
    private Color originalColor;

    [Header("Effetti")]
    public float flashDuration = 0.2f;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 5f;

    // Variabili per tracciare i cambiamenti
    private float lastKnownHealth;
    private bool wasAlive = true;

    void Start()
    {
        // Trova automaticamente il PlayerController se non assegnato
        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("PlayerController non trovato! Assegna il riferimento manualmente.");
                return;
            }
        }

        // IMPORTANTE: Memorizza la posizione originale della barra
        if (hpBarTransform != null)
        {
            // Assicurati che la barra sia nella posizione corretta all'inizio
            hpBarTransform.localPosition = Vector3.zero;
            hpBarTransform.anchoredPosition = Vector2.zero;
        }

        // NUOVO: Disabilita l'interazione con lo slider per evitare input accidentali
        if (hpSlider != null)
        {
            hpSlider.interactable = false;
        }

        InitializeUI();
    }

    void InitializeUI()
    {
        if (playerController == null) return;

        float maxHealth = playerController.GetMaxHealth();
        float currentHealth = playerController.GetCurrentHealth();

        hpSlider.maxValue = maxHealth;
        hpSlider.value = currentHealth;
        lastKnownHealth = currentHealth;
        originalColor = fillImage.color;
        
        UpdateUI();
    }

    void Update()
    {
        if (playerController == null) return;

        // Controlla se la vita è cambiata
        float currentHealth = playerController.GetCurrentHealth();
        bool isAlive = playerController.IsAlive();

        // Se la vita è diminuita, attiva gli effetti di danno
        if (currentHealth < lastKnownHealth)
        {
            float damageTaken = lastKnownHealth - currentHealth;
            OnDamageTaken(damageTaken);
        }

        // Se il player è morto ma era vivo prima
        if (!isAlive && wasAlive)
        {
            OnPlayerDied();
        }
        // Se il player è tornato in vita (respawn)
        else if (isAlive && !wasAlive)
        {
            OnPlayerRespawned();
        }

        // Aggiorna sempre l'UI se c'è stato un cambiamento
        if (currentHealth != lastKnownHealth || isAlive != wasAlive)
        {
            UpdateUI();
        }

        lastKnownHealth = currentHealth;
        wasAlive = isAlive;

        // TEST - Controlli di test solo se il player è vivo e non sta prendendo danno
        if (isAlive && !playerController.IsTakingDamage())
        {
            TestControls();
        }
    }

    // Chiamato quando il player subisce danno
    private void OnDamageTaken(float damage)
    {
        Debug.Log($"UI: Player ha subito {damage} danni");
        
        // Attiva effetti solo se il player è vivo
        if (playerController.IsAlive())
        {
            StartCoroutine(FlashDamage());
            StartCoroutine(ShakeBar());
        }
    }

    // Chiamato quando il player muore
    private void OnPlayerDied()
    {
        Debug.Log("UI: Player è morto");
        
        // NUOVO: Ferma tutti gli effetti quando il player muore
        StopAllCoroutines();
        
        // Reset immediato della posizione della barra se è stata spostata
        if (hpBarTransform != null)
        {
            hpBarTransform.localPosition = Vector3.zero;
            hpBarTransform.anchoredPosition = Vector2.zero;
        }
        
        // Qui potresti aggiungere effetti speciali per la morte
        // Ad esempio, far lampeggiare la barra o cambiarle colore
    }

    // Chiamato quando il player respawna
    private void OnPlayerRespawned()
    {
        Debug.Log("UI: Player è respawnato");
        // Ferma eventuali effetti in corso e resetta l'UI
        StopAllCoroutines();
        
        // IMPORTANTE: Resetta la posizione della barra della vita
        if (hpBarTransform != null)
        {
            hpBarTransform.localPosition = Vector3.zero;
            hpBarTransform.anchoredPosition = Vector2.zero;
        }
        
        // Reinizializza l'UI completamente
        StartCoroutine(ResetUINextFrame());
    }
    
    // Coroutine per resettare l'UI nel frame successivo
    private IEnumerator ResetUINextFrame()
    {
        yield return null; // Aspetta un frame
        
        // Assicurati che la posizione sia davvero resettata
        if (hpBarTransform != null)
        {
            hpBarTransform.localPosition = Vector3.zero;
            hpBarTransform.anchoredPosition = Vector2.zero;
        }
        
        // Reinizializza tutti i valori UI
        InitializeUI();
        
        Debug.Log("UI completamente resettata dopo respawn");
    }

    private void UpdateUI()
    {
        if (playerController == null) return;

        float currentHealth = playerController.GetCurrentHealth();
        float maxHealth = playerController.GetMaxHealth();
        float healthPercent = playerController.GetHealthPercentage();

        // Aggiorna slider e testo
        hpSlider.value = currentHealth;
        hpText.text = Mathf.Ceil(currentHealth) + " / " + Mathf.Ceil(maxHealth);

        // Colore dinamico in base alla percentuale
        if (healthPercent > 0.5f)
        {
            fillImage.color = Color.Lerp(midHealthColor, fullHealthColor, (healthPercent - 0.5f) * 2f);
        }
        else
        {
            fillImage.color = Color.Lerp(lowHealthColor, midHealthColor, healthPercent * 2f);
        }

        // Se il player è morto, potresti voler modificare l'aspetto dell'UI
        if (!playerController.IsAlive())
        {
            // Opzionale: rendi la barra più scura o trasparente quando morto
            Color currentColor = fillImage.color;
            currentColor.a = 0.5f; // Semi-trasparente
            fillImage.color = currentColor;
        }
    }

    // Metodi pubblici che ora delegano al PlayerController
    public void TakeDamage(int damage)
    {
        if (playerController != null)
        {
            playerController.TakeDamage(damage);
        }
    }

    public void Heal(int amount)
    {
        if (playerController != null)
        {
            // Ora usa il metodo Heal del PlayerController
            float actualHeal = playerController.Heal(amount);
            Debug.Log($"Player curato di {actualHeal} HP tramite UI");
        }
    }

    private IEnumerator FlashDamage()
    {
        if (fillImage == null) yield break;
        
        Color originalFillColor = fillImage.color;
        fillImage.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);
        
        // Ripristina il colore corretto tramite UpdateUI
        UpdateUI();
    }

    private IEnumerator ShakeBar()
    {
        if (hpBarTransform == null) yield break;
        
        Vector3 originalPos = hpBarTransform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            // SICUREZZA: Interrompi lo shake se il player è morto
            if (playerController != null && !playerController.IsAlive())
            {
                hpBarTransform.localPosition = originalPos;
                yield break;
            }
            
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            hpBarTransform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // IMPORTANTE: Ripristina sempre la posizione originale alla fine
        hpBarTransform.localPosition = originalPos;
        
        // SICUREZZA EXTRA: Se non è alla posizione corretta, forza il reset
        if (hpBarTransform.localPosition != Vector3.zero)
        {
            hpBarTransform.localPosition = Vector3.zero;
            hpBarTransform.anchoredPosition = Vector2.zero;
            Debug.LogWarning("Posizione barra della vita forzatamente resettata dopo shake");
        }
    }

    // MODIFICATO: Controlli di test - ora controlla lo stato del player prima di agire
    private void TestControls()
    {
        // Non eseguire test se il player è morto o sta prendendo danno
        if (!playerController.IsAlive() || playerController.IsTakingDamage())
        {
            return;
        }

        // Premi H per subire 10 danni
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10);
        }

        // Premi J per test cura
        if (Input.GetKeyDown(KeyCode.J))
        {
            Heal(10);
        }
    }

    // Metodi di utilità pubblici
    public float GetCurrentHealth()
    {
        return playerController != null ? playerController.GetCurrentHealth() : 0f;
    }

    public float GetMaxHealth()
    {
        return playerController != null ? playerController.GetMaxHealth() : 0f;
    }

    public float GetHealthPercentage()
    {
        return playerController != null ? playerController.GetHealthPercentage() : 0f;
    }

    public bool IsPlayerAlive()
    {
        return playerController != null ? playerController.IsAlive() : false;
    }
}