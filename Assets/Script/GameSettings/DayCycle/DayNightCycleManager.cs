using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DayNightCycleManager : MonoBehaviour
{
    [Header("Durata delle Fasi (in secondi)")]
    public float dawnDuration = 30f;      // Alba: da 1/6 (0.167) a 0.25
    public float dayDuration = 180f;      // Giorno: da 0.25 a 0.75  
    public float sunsetDuration = 30f;    // Tramonto: da 0.75 a 5/6 (0.833)
    public float nightDuration = 120f;    // Notte: da 5/6 (0.833) a 1/6 (0.167)

    [Header("Sistema Luci")]
    public DayNightController lightController;

    [Header("Conteggio Giorni")]
    [SerializeField] private int dayCount = 1; // Inizia dal giorno 1

    [Header("Parametri Sistema")]
    [Range(0f, 1f)]
    public float dayTime = 0.25f; // Inizia dal giorno (0.25)
    public bool isRunning = true;
    [System.NonSerialized] // AGGIUNTO: Evita serializzazione ma mantiene pubblico
    public bool startFromDay = true; // Inizia dal giorno al primo avvio

    // Enumerazione delle fasi del giorno basata sui tuoi valori
    public enum DayPhase
    {
        Dawn,    // 1/6 (0.167) → 0.25
        Day,     // 0.25 → 0.75
        Sunset,  // 0.75 → 5/6 (0.833)
        Night    // 5/6 (0.833) → 1 → 1/6 (0.167)
    }

    [Header("Stato Attuale")]
    public DayPhase currentPhase = DayPhase.Day;
    public float phaseTimer = 0f;
    public float totalCycleDuration;

    // Valori temporali del ciclo (da 0 a 1)
    private const float DAWN_START = 1f / 6f;      // 0.167
    private const float DAY_START = 0.25f;       // 0.25
    private const float SUNSET_START = 0.75f;    // 0.75  
    private const float NIGHT_START = 5f / 6f;     // 0.833

    //PER CAMBIARE COLORE BORDO MINIMAPPA E MONETE DURANTE LA NOTTE PER MAGGIORE VISIBILITA'
    [Header("UI Changer")]
    public MinimapFollow minimapBorder;
    public TextMeshProUGUI coinText;

    // Eventi per le varie fasi
    [System.Serializable]
    public class DayNightEvents
    {
        public UnityEvent OnDawnStart;
        public UnityEvent OnDayStart;
        public UnityEvent OnSunsetStart;
        public UnityEvent OnNightStart;
        public UnityEvent OnCycleComplete;
        public UnityEvent OnNewDay; // Nuovo evento per l'inizio di un nuovo giorno
    }

    public DayNightEvents events;

    // Proprietà pubbliche
    public bool IsDawn => currentPhase == DayPhase.Dawn;
    public bool IsDay => currentPhase == DayPhase.Day;
    public bool IsSunset => currentPhase == DayPhase.Sunset;
    public bool IsNight => currentPhase == DayPhase.Night;

    // Getter per il day count
    public int GetDayCount() => dayCount;

    void Start()
    {
        totalCycleDuration = dawnDuration + dayDuration + sunsetDuration + nightDuration;

        // Trova il controller delle luci se non assegnato
        if (lightController == null)
            lightController = Object.FindFirstObjectByType<DayNightController>();

        // Inizializza il tempo del giorno
        if (startFromDay)
        {
            dayTime = DAY_START; // Inizia dal giorno (0.25)
            currentPhase = DayPhase.Day;
            phaseTimer = 0f;
        }

        Debug.Log($"Gioco iniziato al giorno {dayCount}");

        // Aggiorna immediatamente le luci
        UpdateLighting();

        // Avvia il ciclo
        StartCoroutine(DayNightCycle());
    }

    void Update()
    {
        if (!isRunning) return;

        // Aggiorna le luci tramite il controller esistente
        UpdateLighting();
    }

    void UpdateLighting()
    {
        if (lightController != null)
        {
            lightController.UpdateLight(dayTime);
        }
    }

    IEnumerator DayNightCycle()
    {
        // Se iniziamo dal giorno, salta alba ma invoca comunque l'evento di nuovo giorno
        if (startFromDay && currentPhase == DayPhase.Day)
        {
            events.OnNewDay?.Invoke(); // Invoca evento nuovo giorno
            events.OnDayStart?.Invoke(); // AGGIUNTO: Invoca anche OnDayStart per far spawnare le gemme gialle
            yield return StartCoroutine(RunPhase(DayPhase.Day, dayDuration, DAY_START, SUNSET_START));
            startFromDay = false; // Evita di saltare l'alba nei cicli successivi
        }

        while (isRunning)
        {
            // FASE TRAMONTO
            currentPhase = DayPhase.Sunset;
            events.OnSunsetStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Sunset, sunsetDuration, SUNSET_START, NIGHT_START));

            // FASE NOTTE
            minimapBorder.ChangeBorderColor(Color.white);
            coinText.color = Color.white;
            currentPhase = DayPhase.Night;
            events.OnNightStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Night, nightDuration, NIGHT_START, DAWN_START));

            // FASE ALBA
            currentPhase = DayPhase.Dawn;
            events.OnDawnStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Dawn, dawnDuration, DAWN_START, DAY_START));

            // NUOVO GIORNO INIZIA
            IncrementDay();

            // FASE GIORNO
            minimapBorder.ChangeBorderColor(Color.black);
            coinText.color = Color.black;
            currentPhase = DayPhase.Day;
            events.OnNewDay?.Invoke(); // Invoca evento nuovo giorno
            events.OnDayStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Day, dayDuration, DAY_START, SUNSET_START));

            // Ciclo completato
            events.OnCycleComplete?.Invoke();
        }
    }

    private void IncrementDay()
    {
        dayCount++;
        Debug.Log($"Iniziato nuovo giorno: Giorno {dayCount}");

        // Calcola quale porta sarà quella del giorno oggi
        int doorOfTheDay = ((dayCount - 1) % 8) + 1;
        Debug.Log($"Porta del giorno {dayCount}: Porta {doorOfTheDay}");
    }

    IEnumerator RunPhase(DayPhase phase, float duration, float startTime, float endTime)
    {
        phaseTimer = 0f;

        while (phaseTimer < duration && isRunning)
        {
            phaseTimer += Time.deltaTime;
            float progress = phaseTimer / duration;

            // Calcola il dayTime basandosi sul progresso della fase
            dayTime = CalculateDayTime(startTime, endTime, progress);

            // Gestisci il wraparound per la notte (da 5/6 a 1/6)
            if (phase == DayPhase.Night)
            {
                if (progress <= 0.5f)
                {
                    // Prima metà della notte: da 5/6 a 1 (mezzanotte)
                    dayTime = Mathf.Lerp(NIGHT_START, 1f, progress * 2f);
                }
                else
                {
                    // Seconda metà della notte: da 0 a 1/6
                    dayTime = Mathf.Lerp(0f, DAWN_START, (progress - 0.5f) * 2f);
                }
            }

            yield return null;
        }
    }

    float CalculateDayTime(float start, float end, float progress)
    {
        return Mathf.Lerp(start, end, progress);
    }

    // Metodi pubblici per il controllo
    public void PauseSystem()
    {
        isRunning = false;
    }

    public void ResumeSystem()
    {
        isRunning = true;
    }

    public void SetDayTime(float newTime)
    {
        dayTime = Mathf.Repeat(newTime, 1f); // Assicura che sia tra 0 e 1

        // Determina la fase corrente basandosi sul dayTime
        currentPhase = GetPhaseFromDayTime(dayTime);

        // Aggiorna le luci immediatamente
        UpdateLighting();
    }

    DayPhase GetPhaseFromDayTime(float time)
    {
        if (time >= DAWN_START && time < DAY_START)
            return DayPhase.Dawn;
        else if (time >= DAY_START && time < SUNSET_START)
            return DayPhase.Day;
        else if (time >= SUNSET_START && time < NIGHT_START)
            return DayPhase.Sunset;
        else
            return DayPhase.Night;
    }

    public float GetPhaseProgress()
    {
        float phaseDuration = GetCurrentPhaseDuration();
        return phaseDuration > 0 ? phaseTimer / phaseDuration : 0f;
    }

    float GetCurrentPhaseDuration()
    {
        switch (currentPhase)
        {
            case DayPhase.Dawn: return dawnDuration;
            case DayPhase.Day: return dayDuration;
            case DayPhase.Sunset: return sunsetDuration;
            case DayPhase.Night: return nightDuration;
            default: return 0f;
        }
    }

    public string GetTimeString()
    {
        int hours = Mathf.FloorToInt(dayTime * 24);
        int minutes = Mathf.FloorToInt((dayTime * 24 * 60) % 60);
        return $"{hours:00}:{minutes:00}";
    }

    public void ResetToDay()
    {
        Debug.Log("Reset del ciclo giorno/notte all'inizio del giorno (da sonno)");

        // Ferma il ciclo corrente
        StopAllCoroutines();

        // Reset al giorno con i valori corretti
        dayTime = DAY_START; // 0.25
        currentPhase = DayPhase.Day;
        phaseTimer = 0f;

        // Incrementa il day count quando resettiamo al giorno
        IncrementDay();

        // Aggiorna colori UI per il giorno
        if (minimapBorder != null)
            minimapBorder.ChangeBorderColor(Color.black);
        if (coinText != null)
            coinText.color = Color.black;

        // Notifica il MazeManager del reset
        MazeManager mazeManager = FindFirstObjectByType<MazeManager>();
        if (mazeManager != null)
        {
            mazeManager.HandleSleepReset();
        }

        // Aggiorna immediatamente le luci con il nuovo dayTime
        UpdateLighting();

        // Riavvia il sistema
        isRunning = true;

        // Usa una versione speciale del ciclo per il sonno
        StartCoroutine(DayNightCycleAfterSleep());
    }

    private IEnumerator DayNightCycleAfterSleep()
    {
        // Invoca immediatamente gli eventi del nuovo giorno
        events.OnNewDay?.Invoke();
        events.OnDayStart?.Invoke();

        // NUOVO: Attendi un momento per assicurarsi che tutto sia sincronizzato
        yield return new WaitForSeconds(1f);

        // Ora apri le porte (il labirinto è già cambiato)
        MazeManager mazeManager = FindFirstObjectByType<MazeManager>();
        if (mazeManager != null)
        {
            mazeManager.OpenMazeDoors();
            Debug.Log("Porte aperte dopo il sonno");
        }

        // Attendi un altro secondo prima del maze open warning
        yield return new WaitForSeconds(1f);

        // Mostra il maze open warning
        if (mazeManager != null)
        {
            mazeManager.ShowMazeOpenWarningAfterSleep();
        }

        // Continua con il ciclo normale dalla fase giorno
        yield return StartCoroutine(RunPhase(DayPhase.Day, dayDuration, DAY_START, SUNSET_START));

        // Poi continua con il ciclo normale
        while (isRunning)
        {
            // FASE TRAMONTO
            minimapBorder.ChangeBorderColor(Color.black);
            coinText.color = Color.black;
            currentPhase = DayPhase.Sunset;
            events.OnSunsetStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Sunset, sunsetDuration, SUNSET_START, NIGHT_START));

            // FASE NOTTE
            minimapBorder.ChangeBorderColor(Color.white);
            coinText.color = Color.white;
            currentPhase = DayPhase.Night;
            events.OnNightStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Night, nightDuration, NIGHT_START, DAWN_START));

            // FASE ALBA
            currentPhase = DayPhase.Dawn;
            events.OnDawnStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Dawn, dawnDuration, DAWN_START, DAY_START));

            // NUOVO GIORNO INIZIA
            IncrementDay();

            // FASE GIORNO
            minimapBorder.ChangeBorderColor(Color.black);
            coinText.color = Color.black;
            currentPhase = DayPhase.Day;
            events.OnNewDay?.Invoke();
            events.OnDayStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Day, dayDuration, DAY_START, SUNSET_START));

            // Ciclo completato
            events.OnCycleComplete?.Invoke();
        }
    }

    private IEnumerator DayNightCycleFromDay()
    {
        // Inizia direttamente dalla fase giorno
        yield return StartCoroutine(RunPhase(DayPhase.Day, dayDuration, DAY_START, SUNSET_START));

        // Poi continua con il ciclo normale
        while (isRunning)
        {
            // FASE TRAMONTO
            currentPhase = DayPhase.Sunset;
            events.OnSunsetStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Sunset, sunsetDuration, SUNSET_START, NIGHT_START));

            // FASE NOTTE
            currentPhase = DayPhase.Night;
            events.OnNightStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Night, nightDuration, NIGHT_START, DAWN_START));

            // FASE ALBA
            currentPhase = DayPhase.Dawn;
            events.OnDawnStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Dawn, dawnDuration, DAWN_START, DAY_START));

            // NUOVO GIORNO INIZIA
            IncrementDay();

            // FASE GIORNO
            currentPhase = DayPhase.Day;
            events.OnNewDay?.Invoke(); // Invoca evento nuovo giorno
            events.OnDayStart?.Invoke();
            yield return StartCoroutine(RunPhase(DayPhase.Day, dayDuration, DAY_START, SUNSET_START));

            // Ciclo completato
            events.OnCycleComplete?.Invoke();
        }
    }

    // Metodi per forzare fasi specifiche (utili per debugging)
    public void ForceToNight()
    {
        SetDayTime(NIGHT_START);
    }

    public void ForceToDay()
    {
        SetDayTime(DAY_START);
    }

    public void ForceToSunset()
    {
        SetDayTime(SUNSET_START);
    }

    public void ForceToDawn()
    {
        SetDayTime(DAWN_START);
    }

    // Metodi per il debug e testing
    public void SetDayCount(int newDayCount)
    {
        dayCount = Mathf.Max(1, newDayCount);
        Debug.Log($"Day count impostato a: {dayCount}");
    }

    public void AddDays(int daysToAdd)
    {
        dayCount += daysToAdd;
        Debug.Log($"Aggiunti {daysToAdd} giorni. Day count attuale: {dayCount}");
    }
    
    public bool IsInitialized()
    {
        // Il sistema è considerato inizializzato se:
        // 1. Il sistema è in esecuzione
        // 2. Non siamo più nella fase di setup iniziale (startFromDay è false dopo il primo ciclo)
        return isRunning && !startFromDay;
    }

    #region SAVE AND LOAD

    public void Save(ref DayNightSaveData data)
    {
        data.dayTimeValue = dayTime;
        data.currentPhaseIndex = (int)currentPhase;
        data.phaseTimer = phaseTimer;
        data.wasRunning = isRunning;

        Debug.Log("DayNight salvato - Tempo: " + dayTime + " Fase: " + currentPhase);
    }

    public void Load(DayNightSaveData data)
    {
        // Ferma tutto
        StopAllCoroutines();

        dayTime = data.dayTimeValue;
        currentPhase = (DayPhase)data.currentPhaseIndex;
        phaseTimer = data.phaseTimer;
        isRunning = data.wasRunning;

        // Aggiorna immediatamente le luci
        UpdateLighting();

        // Forza l'aggiornamento del colore della minimappa se necessario
        if (minimapBorder != null)
        {
            if (IsNight)
                minimapBorder.ChangeBorderColor(Color.white);
            else
                minimapBorder.ChangeBorderColor(Color.black);
        }

        // Riavvia il sistema
        if (isRunning)
        {
            StartCoroutine(DayNightCycle());
        }

        Debug.Log("DayNight caricato - Tempo: " + dayTime + " Fase: " + currentPhase);
    }

    #endregion
}

//For Save and Load
[System.Serializable]
public struct DayNightSaveData
{
    public float dayTimeValue;
    public int currentPhaseIndex;
    public float phaseTimer;
    public bool wasRunning;
}