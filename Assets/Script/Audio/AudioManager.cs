using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Effetti Sonori - Giocatore")]
    public AudioClip swordHit;
    public AudioClip coinPickup;
    public AudioClip gemPickup;
    public AudioClip itemPurchase;
    public AudioClip walking; // per ora non usato
    public AudioClip healing;          // NUOVO: cura
    public AudioClip damageTaken;      // NUOVO: danni subiti
    public AudioClip death;            // NUOVO: morte

    [Header("Effetti Sonori - Ambiente")]
    public AudioClip doorOpen;
    public AudioClip doorClose;
    public AudioClip nightIsComing;    // NUOVO: notte sta arrivando

    [Header("Effetti Sonori - UI")]
    public AudioClip buttonClick;

    [Header("Effetti Sonori - Nemici")]
    public AudioClip enemyShout;

    [Header("Altro")]
    public AudioClip victory;

    [Header("Musica")]
    public AudioClip mainMenuMusic;
    public AudioClip gameMusic;

    private AudioSource sfxSource;   // Per effetti sonori
    private AudioSource musicSource; // Per musica di sottofondo

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length < 2)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            sfxSource = sources[0];
            musicSource = sources[1];
        }

        musicSource.loop = true;
    }

    // --- Effetti sonori generici ---
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // Giocatore
    public void PlaySwordHit() => PlaySound(swordHit);
    public void PlayCoinPickup() => PlaySound(coinPickup);
    public void PlayGemPickup() => PlaySound(gemPickup);
    public void PlayItemPurchase() => PlaySound(itemPurchase);
    public void PlayWalking() => PlaySound(walking);
    public void PlayHealing() => PlaySound(healing);            // NUOVO
    public void PlayDamageTaken() => PlaySound(damageTaken);    // NUOVO
    public void PlayDeath() => PlaySound(death);                // NUOVO

    // Ambiente
    public void PlayDoorOpen() => PlaySound(doorOpen);
    public void PlayDoorClose() => PlaySound(doorClose);
    public void PlayNightIsComing() => PlaySound(nightIsComing); // NUOVO

    // UI
    public void PlayButtonClick() => PlaySound(buttonClick);

    // Nemici
    public void PlayEnemyShout() => PlaySound(enemyShout);

    // Altro
    public void PlayVictory() => PlaySound(victory);

    // --- Musica ---
    public void PlayMusic(AudioClip clip)
    {
        if (clip != null && musicSource != null)
        {
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }
}
