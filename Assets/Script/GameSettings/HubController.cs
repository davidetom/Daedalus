using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HubController : MonoBehaviour
{
    [Header("Riferimenti Oggetti Figli")]
    [SerializeField] private Collider2D enterPoint;
    [SerializeField] private GameObject doorIndicator;

    [Header("Animazione Freccia")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;
    private Vector3 originalIndicatorPosition;
    private bool isPlayerInEnterPoint = false;
    private bool isAnimating = false;

    [Header("Scene Management")]
    [SerializeField] private string hubInteriorSceneName = "HubInterior"; // Nome della scena interna dell'hub
    
    [Header("Riferimenti Sistema")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private DayNightCycleManager dayNightManager;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = false;

    void Start()
    {
        InitializeReferences();
        SetupInitialState();
    }

    void InitializeReferences()
    {
        // Trova automaticamente gli oggetti figli se non assegnati
        if (enterPoint == null)
        {
            Transform enterPointTransform = transform.Find("EnterPoint");
            if (enterPointTransform != null)
            {
                enterPoint = enterPointTransform.GetComponent<Collider2D>();
                if (enterPoint == null)
                {
                    Debug.LogError("EnterPoint trovato ma non ha un Collider2D!");
                }
            }
            else
            {
                Debug.LogError("Oggetto figlio 'EnterPoint' non trovato!");
            }
        }

        if (doorIndicator == null)
        {
            Transform doorIndicatorTransform = transform.Find("DoorIndicator");
            if (doorIndicatorTransform != null)
            {
                doorIndicator = doorIndicatorTransform.gameObject;
            }
            else
            {
                Debug.LogError("Oggetto figlio 'DoorIndicator' non trovato!");
            }
        }

        // Trova automaticamente i componenti del sistema se non assegnati
        if (playerController == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerController = playerObj.GetComponent<PlayerController>();
            }
        }

        if (dayNightManager == null)
        {
            dayNightManager = Object.FindFirstObjectByType<DayNightCycleManager>();
        }

        // Verifica che l'EnterPoint sia configurato come trigger
        if (enterPoint != null && !enterPoint.isTrigger)
        {
            Debug.LogWarning("EnterPoint Collider2D dovrebbe essere configurato come Trigger!");
        }
    }

    void SetupInitialState()
    {
        // Salva la posizione originale dell'indicatore
        if (doorIndicator != null)
        {
            originalIndicatorPosition = doorIndicator.transform.localPosition;
            doorIndicator.SetActive(false); // Inizialmente disattivo
        }
    }

    void Update()
    {
        // Controlla input per entrare nell'hub
        if (isPlayerInEnterPoint && Input.GetKeyDown(KeyCode.E))
        {
            EnterHub();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Nota: questo script deve essere sull'oggetto EnterPoint, non sull'Hub principale
        // Se è sull'Hub principale, devi controllare se il collider che ha fatto trigger è l'EnterPoint
        
        if (other.CompareTag("Player"))
        {
            if (enableDebug)
                Debug.Log("Player entrato nell'area dell'hub");
            
            OnPlayerEnterArea();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (enableDebug)
                Debug.Log("Player uscito dall'area dell'hub");
            
            OnPlayerExitArea();
        }
    }

    private void OnPlayerEnterArea()
    {
        isPlayerInEnterPoint = true;
        
        // Attiva il door indicator
        if (doorIndicator != null)
        {
            doorIndicator.SetActive(true);
            
            // Avvia l'animazione di movimento su e giù
            if (!isAnimating)
            {
                StartCoroutine(AnimateDoorIndicator());
            }
        }

        // Notifica al player che può interagire (opzionale)
        if (playerController != null && enableDebug)
        {
            Debug.Log("Premi E per entrare nell'hub");
        }
    }

    private void OnPlayerExitArea()
    {
        isPlayerInEnterPoint = false;
        
        // Disattiva il door indicator
        if (doorIndicator != null)
        {
            doorIndicator.SetActive(false);
        }

        // Ferma l'animazione
        isAnimating = false;
    }

    private IEnumerator AnimateDoorIndicator()
    {
        isAnimating = true;
        
        while (isPlayerInEnterPoint && doorIndicator != null && doorIndicator.activeInHierarchy)
        {
            // Calcola il movimento su e giù usando sin
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            
            // Applica il movimento alla posizione originale
            Vector3 newPosition = originalIndicatorPosition;
            newPosition.y += yOffset;
            
            doorIndicator.transform.localPosition = newPosition;
            
            yield return null;
        }
        
        // Ripristina la posizione originale quando finisce l'animazione
        if (doorIndicator != null)
        {
            doorIndicator.transform.localPosition = originalIndicatorPosition;
        }
        
        isAnimating = false;
    }

    private void EnterHub()
    {
        if (enableDebug)
            Debug.Log("Player sta entrando nell'hub");

        // Pausa il ciclo giorno/notte
        PauseDayNightCycle();
        
        // Carica la scena dell'interno dell'hub
        LoadHubInteriorScene();
    }

    private void PauseDayNightCycle()
    {
        if (dayNightManager != null)
        {
            dayNightManager.PauseSystem();
            
            if (enableDebug)
                Debug.Log("Ciclo giorno/notte messo in pausa");
        }
        else
        {
            Debug.LogWarning("DayNightCycleManager non trovato! Impossibile mettere in pausa il ciclo.");
        }
    }

    private void LoadHubInteriorScene()
    {
        // Salva la posizione attuale del player e altri dati necessari
        SavePlayerDataBeforeSceneChange();
        
        // Carica la scena dell'interno dell'hub
        if (!string.IsNullOrEmpty(hubInteriorSceneName))
        {
            if (enableDebug)
                Debug.Log($"Caricando scena: {hubInteriorSceneName}");
                
            SceneManager.LoadScene(hubInteriorSceneName);
        }
        else
        {
            Debug.LogError("Nome della scena dell'hub interno non specificato!");
        }
    }

    private void SavePlayerDataBeforeSceneChange()
    {
        // Qui puoi salvare i dati del player che devono persistere tra le scene
        // Ad esempio usando PlayerPrefs o un sistema di salvataggio più complesso
        
        if (playerController != null)
        {
            // Salva posizione del player
            Vector3 playerPos = playerController.transform.position;
            PlayerPrefs.SetFloat("PlayerPosX", playerPos.x);
            PlayerPrefs.SetFloat("PlayerPosY", playerPos.y);
            PlayerPrefs.SetFloat("PlayerPosZ", playerPos.z);
            
            // Salva vita attuale
            PlayerPrefs.SetFloat("PlayerHealth", playerController.GetCurrentHealth());
            
            // Salva stato del ciclo giorno/notte
            if (dayNightManager != null)
            {
                PlayerPrefs.SetFloat("DayTime", dayNightManager.dayTime);
                PlayerPrefs.SetInt("CurrentPhase", (int)dayNightManager.currentPhase);
            }
            
            if (enableDebug)
                Debug.Log("Dati del player salvati prima del cambio scena");
        }
    }

    // Metodo pubblico per essere chiamato dal pulsante mobile
    public void TryEnterHub()
    {
        if (isPlayerInEnterPoint)
        {
            EnterHub();
        }
        else if (enableDebug)
        {
            Debug.Log("Player non si trova nell'area di ingresso dell'hub");
        }
    }

    // Metodi pubblici per debugging
    [ContextMenu("Test Enter Hub")]
    public void TestEnterHub()
    {
        EnterHub();
    }

    [ContextMenu("Force Show Indicator")]
    public void ForceShowIndicator()
    {
        OnPlayerEnterArea();
    }

    [ContextMenu("Force Hide Indicator")]
    public void ForceHideIndicator()
    {
        OnPlayerExitArea();
    }

    // Proprietà pubbliche per accesso esterno
    public bool IsPlayerInEnterPoint => isPlayerInEnterPoint;
    public bool IsIndicatorActive => doorIndicator != null && doorIndicator.activeInHierarchy;

    void OnValidate()
    {
        // Validazione nell'editor
        if (bobSpeed <= 0)
            bobSpeed = 2f;
        
        if (bobHeight <= 0)
            bobHeight = 0.3f;
    }
}
