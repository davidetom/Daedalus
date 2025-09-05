// Modifica completa della classe BedLogic.cs
using UnityEngine;

public class BedLogic : MonoBehaviour
{
    [Header("Riferimenti Sistema")]
    public PlayerController playerController;
    public DayNightCycleManager dayNightManager;
    public MazeManager mazeManager;
    
    [Header("Feedback Visivi/Sonori")]
    public AudioClip sleepSound;
    public AudioClip healSound;
    public GameObject healEffect; // Effetto particelle per la cura
    
    [Header("Debug")]
    public bool enableDebug = false;
    
    private AudioSource audioSource;
    
    void Start()
    {
        InitializeReferences();
    }
    
    void InitializeReferences()
    {
        // Trova automaticamente i riferimenti se non assegnati
        if (playerController == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerController = playerObj.GetComponent<PlayerController>();
            }
        }
        
        if (dayNightManager == null)
            dayNightManager = FindFirstObjectByType<DayNightCycleManager>();

        if (mazeManager == null)
            mazeManager = FindFirstObjectByType<MazeManager>();
        
        // Setup audio source per suoni
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void TrySleep()
    {
        if (playerController == null)
        {
            if (enableDebug)
                Debug.LogWarning("PlayerController non trovato! Impossibile dormire.");
            return;
        }
        
        if (playerController.IsDead())
        {
            if (enableDebug)
                Debug.Log("Il player morto non può dormire!");
            return;
        }
        
        // Verifica se è notte
        bool isNightTime = IsCurrentlyNight();
        
        if (isNightTime)
        {
            // È notte: dormi e salta alla mattina
            SleepThroughNight();
        }
        else
        {
            // Non è notte: solo cura
            RestWithoutSleep();
        }
    }
    
    private bool IsCurrentlyNight()
    {
        if (dayNightManager == null)
        {
            if (enableDebug)
                Debug.LogWarning("DayNightManager non trovato! Assumendo che non sia notte.");
            return false;
        }
        
        return dayNightManager.IsNight;
    }
    
    private void SleepThroughNight()
    {
        if (enableDebug)
            Debug.Log("Player sta dormendo - cura completa e salta alla mattina");
            
        // 1. Cura completa del player
        HealPlayer();
        
        // 2. Riproduci suono del sonno
        PlaySleepSound();
        
        // 3. Usa il metodo SleepToNextDay del MazeManager per saltare la notte
        if (mazeManager != null)
        {
            mazeManager.SleepToNextDay();
            
            if (enableDebug)
                Debug.Log("Chiamato MazeManager.SleepToNextDay() - notte saltata");
        }
        else if (dayNightManager != null)
        {
            // Fallback: se MazeManager non è disponibile, usa il metodo diretto
            dayNightManager.ForceToDawn();
            
            if (enableDebug)
                Debug.Log("Fallback: notte saltata usando DayNightManager direttamente");
        }
        
        // 4. Mostra effetto di cura se disponibile
        ShowHealEffect();
    }
    
    private void RestWithoutSleep()
    {
        if (enableDebug)
            Debug.Log("Player si sta riposando - solo cura, senza dormire");
            
        // 1. Cura completa del player
        HealPlayer();
        
        // 2. Riproduci suono di cura
        PlayHealSound();
        
        // 3. Mostra effetto di cura se disponibile
        ShowHealEffect();
    }
    
    private void HealPlayer()
    {
        if (playerController != null)
        {
            float previousHealth = playerController.GetCurrentHealth();
            playerController.FullHeal();
            float newHealth = playerController.GetCurrentHealth();
            
            if (enableDebug)
                Debug.Log($"Player curato: {previousHealth} -> {newHealth} HP");
        }
    }
    
    private void PlaySleepSound()
    {
        if (audioSource != null && sleepSound != null)
        {
            audioSource.PlayOneShot(sleepSound);
        }
    }
    
    private void PlayHealSound()
    {
        if (audioSource != null && healSound != null)
        {
            audioSource.PlayOneShot(healSound);
        }
    }
    
    private void ShowHealEffect()
    {
        if (healEffect != null && playerController != null)
        {
            GameObject effect = Instantiate(healEffect, playerController.transform.position, Quaternion.identity);
            
            // Distruggi l'effetto dopo un po' se non ha autodistruzione
            if (effect.GetComponent<ParticleSystem>() == null)
            {
                Destroy(effect, 3f);
            }
        }
    }
    
    // Metodi di utilità pubblica per testing
    [ContextMenu("Test Sleep (Force Night)")]
    public void TestSleepAtNight()
    {
        if (dayNightManager != null)
        {
            dayNightManager.ForceToNight();
        }
        TrySleep();
    }
    
    [ContextMenu("Test Rest (Force Day)")]
    public void TestRestAtDay()
    {
        if (dayNightManager != null)
        {
            dayNightManager.ForceToDay();
        }
        TrySleep();
    }
    
    [ContextMenu("Damage Player for Testing")]
    public void DamagePlayerForTesting()
    {
        if (playerController != null)
        {
            playerController.TakeDamage(50f);
            Debug.Log($"Player danneggiato per test. Vita attuale: {playerController.GetCurrentHealth()}");
        }
    }
}