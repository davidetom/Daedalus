using UnityEngine;

public class LabyrinthMusicManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public DayNightCycleManager dayNightManager;
    
    [Header("Controlli")]
    public bool playMusicOnStart = true;
    public float fadeTime = 2f; // Tempo di dissolvenza tra le tracce (futuro)
    
    private bool isInitialized = false;

    public bool musicPaused = false;
    void Start()
    {
        // Trova automaticamente il DayNightCycleManager se non assegnato
        if (dayNightManager == null)
            dayNightManager = FindFirstObjectByType<DayNightCycleManager>();

        if (dayNightManager == null)
        {
            Debug.LogError("LabyrinthMusicManager: DayNightCycleManager non trovato!");
            return;
        }

        // Collega gli eventi
        ConnectToEvents();

        // Avvia la musica appropriata se richiesto
        if (playMusicOnStart)
        {
            // Attendi un frame per assicurarsi che tutto sia inizializzato
            Invoke(nameof(StartInitialMusic), 0.1f);
        }

        isInitialized = true;
    }

    void ConnectToEvents()
    {
        // Collega gli eventi del day/night cycle
        if (dayNightManager.events != null)
        {
            dayNightManager.events.OnDayStart.AddListener(OnDayStart);
            dayNightManager.events.OnNightStart.AddListener(OnNightStart);
            dayNightManager.events.OnNewDay.AddListener(OnNewDay);
        }
        else
        {
            Debug.LogError("LabyrinthMusicManager: Eventi del DayNightCycleManager non configurati!");
        }
    }

    void StartInitialMusic()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("LabyrinthMusicManager: AudioManager.Instance non trovato!");
            return;
        }

        // Avvia la musica corretta basandosi sulla fase attuale
        if (dayNightManager.IsDay || dayNightManager.IsDawn || dayNightManager.IsSunset)
        {
            PlayDayMusic();
        }
        else if (dayNightManager.IsNight)
        {
            PlayNightMusic();
        }

        Debug.Log($"Musica labirinto avviata - Fase corrente: {dayNightManager.currentPhase}");
    }

    // Chiamato quando inizia il giorno
    void OnDayStart()
    {
        if (!isInitialized) return;
        
        Debug.Log("LabyrinthMusicManager: Passaggio alla musica del giorno");
        PlayDayMusic();
    }

    // Chiamato quando inizia la notte
    void OnNightStart()
    {
        if (!isInitialized) return;
        
        Debug.Log("LabyrinthMusicManager: Passaggio alla musica della notte");
        PlayNightMusic();
    }

    // Chiamato quando inizia un nuovo giorno
    void OnNewDay()
    {
        if (!isInitialized) return;
        
        Debug.Log($"LabyrinthMusicManager: Nuovo giorno iniziato (Giorno {dayNightManager.GetDayCount()})");
        // La musica del giorno verrà gestita da OnDayStart
    }

    void PlayDayMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLabyrinthDayMusic();
            Debug.Log("Riproduzione musica labirinto - GIORNO");
        }
    }

    void PlayNightMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLabyrinthNightMusic();
            Debug.Log("Riproduzione musica labirinto - NOTTE");
        }
    }

    // Metodi pubblici per controllo manuale
    public void ForcePlayDayMusic()
    {
        PlayDayMusic();
    }

    public void ForcePlayNightMusic()
    {
        PlayNightMusic();
    }

    public void StopLabyrinthMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            Debug.Log("Musica labirinto fermata");
        }
    }

    // Metodi per gestire la pausa durante la morte
    public void PauseMusicForDeath()
    {
        musicPaused = true;
        StopLabyrinthMusic();
        Debug.Log("Musica labirinto messa in pausa per morte");
    }

    public void ResumeMusicAfterRevive()
    {
        musicPaused = false;
        
        // Riprendi la musica appropriata basandosi sulla fase corrente
        if (dayNightManager != null)
        {
            if (dayNightManager.IsDay || dayNightManager.IsDawn || dayNightManager.IsSunset)
            {
                PlayDayMusic();
            }
            else if (dayNightManager.IsNight)
            {
                PlayNightMusic();
            }
        }
        
        Debug.Log("Musica labirinto ripresa dopo revive");
    }

    // Disconnetti gli eventi quando viene distrutto l'oggetto
    void OnDestroy()
    {
        if (dayNightManager != null && dayNightManager.events != null)
        {
            dayNightManager.events.OnDayStart.RemoveListener(OnDayStart);
            dayNightManager.events.OnNightStart.RemoveListener(OnNightStart);
            dayNightManager.events.OnNewDay.RemoveListener(OnNewDay);
        }
    }

    // Metodo per debugging - mostra lo stato corrente
    [ContextMenu("Debug Current State")]
    void DebugCurrentState()
    {
        if (dayNightManager != null)
        {
            Debug.Log($"Fase corrente: {dayNightManager.currentPhase}");
            Debug.Log($"Tempo del giorno: {dayNightManager.dayTime}");
            Debug.Log($"Giorno corrente: {dayNightManager.GetDayCount()}");
        }
    }
}