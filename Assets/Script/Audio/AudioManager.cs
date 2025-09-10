using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Effetti Sonori - Giocatore")]
    public AudioClip swordHit;
    public AudioClip coinPickup;
    public AudioClip gemPickup;
    public AudioClip itemPurchase;
    public AudioClip errorPurchase;
    public AudioClip death;


    public AudioClip damageTaken;

    
    public AudioClip labyrinthDayMusic;
    public AudioClip labyrinthNightMusic;

    [Header("Effetti Sonori - Ambiente")]
    public AudioClip doorOpen;
    public AudioClip doorClose;

    [Header("Effetti Sonori - UI")]
    public AudioClip menuButton;
    public AudioClip buttonClick;

    [Header("Effetti Sonori - Nemici")]
    public AudioClip enemyShout;

    [Header("Altro")]
    public AudioClip victory;

    [Header("Musica")]
    public AudioClip mainMenuMusic;
    public AudioClip gameMusic;

    [Header("Controlli Volume")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;      // Volume generale
    [Range(0f, 1f)]
    public float sfxVolume = 1f;         // Volume effetti sonori
    [Range(0f, 1f)]
    public float musicVolume = 1f;       // Volume musica

    private AudioSource sfxSource;   // Per effetti sonori
    private AudioSource musicSource; // Per musica di sottofondo

    void Awake()
    {
        // Singleton (un solo AudioManager in tutta l'app)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Non distruggere quando cambi scena
            
            // Carica le impostazioni del volume salvate
            LoadVolumeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Due AudioSource: uno per musica, uno per effetti
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length < 2)
        {
            // Se non ci sono abbastanza AudioSource, li aggiungiamo
            sfxSource = gameObject.AddComponent<AudioSource>();
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            sfxSource = sources[0];
            musicSource = sources[1];
        }

        musicSource.loop = true; // La musica gira in loop
        
        // Applica i volumi iniziali
        UpdateVolumes();
    }

    // --- Controllo Volume ---
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
        SaveVolumeSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
        SaveVolumeSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
        SaveVolumeSettings();
    }

    private void UpdateVolumes()
    {
        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;
            
        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;
    }

    // Salva le impostazioni del volume
    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    // Carica le impostazioni del volume
    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
    }

    // Metodi pubblici per ottenere i valori del volume (utili per inizializzare gli slider)
    public float GetMasterVolume() => masterVolume;
    public float GetSFXVolume() => sfxVolume;
    public float GetMusicVolume() => musicVolume;

    // --- Effetti sonori ---
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            // Usa PlayOneShot per gli effetti, mantenendo il volume impostato
            sfxSource.PlayOneShot(clip, masterVolume * sfxVolume);
        }
    }

    public void StopSound()
    {
        if (sfxSource != null)
        {
            // Usa PlayOneShot per gli effetti, mantenendo il volume impostato
            sfxSource.Stop();
        }
    }

    // Giocatore
    public void PlaySwordHit() => PlaySound(swordHit);
    public void PlayCoinPickup() => PlaySound(coinPickup);
    public void PlayGemPickup() => PlaySound(gemPickup);
    public void PlayItemPurchase() => PlaySound(itemPurchase);

    public void PlayErrorPurchase() => PlaySound(errorPurchase);
    // Ambiente
    public void PlayDoorOpen() => PlaySound(doorOpen);
    public void PlayDoorClose() => PlaySound(doorClose);

    // UI
    public void PlayMenuButton() => PlaySound(menuButton);
    public void PlayButtonClick() => PlaySound(buttonClick);

    // Nemici
    public void PlayEnemyShout() => PlaySound(enemyShout);

    // Altro
    public void PlayVictory() => PlaySound(victory);
    public void StopVictory() => StopSound();

     public void PlayDamageTaken() => PlaySound(damageTaken);    // NUOVO
    public void PlayDeath() => PlaySound(death);                // NUOVO

    // --- Musica ---
    public void PlayMusic(AudioClip clip)
    {
        if (clip != null && musicSource != null)
        {
            if (musicSource.clip == clip && musicSource.isPlaying) return; // evita restart
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    // Musica specifica per labirinto
    public void PlayLabyrinthDayMusic() => PlayMusic(labyrinthDayMusic);
    public void PlayLabyrinthNightMusic() => PlayMusic(labyrinthNightMusic);

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }
}