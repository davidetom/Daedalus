using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OuterHubController : MonoBehaviour
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

    [Header("Riferimenti Sistema")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private DayNightCycleManager dayNightManager;
    public GameObject healthBar;
    public Button shopButton;
    public GameObject minimap;

    [Header("Teleportation")]
    [SerializeField] private Vector3 hubSpawnPosition = new Vector3(406.5f, 153.7f, 0f);
    [SerializeField] private Vector3 exitSpawnPosition; // Posizione di ritorno nel labirinto

    [Header("Camera Management")]
    [SerializeField] private Camera mazeCamera;
    [SerializeField] private Camera hubCamera;
    private bool playerInHub = false;

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
            Transform doorIndicatorTransform = transform.Find("OuterDoorIndicator");
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

        // NUOVO: Trova le telecamere automaticamente se non assegnate
        if (mazeCamera == null)
        {
            GameObject mazeCamObj = GameObject.Find("MazeCamera");
            if (mazeCamObj != null)
            {
                mazeCamera = mazeCamObj.GetComponent<Camera>();
            }
            else
            {
                Debug.LogWarning("MazeCamera non trovata! Assegnala manualmente nell'inspector.");
            }
        }

        if (hubCamera == null)
        {
            GameObject hubCamObj = GameObject.Find("HubCamera");
            if (hubCamObj != null)
            {
                hubCamera = hubCamObj.GetComponent<Camera>();
            }
            else
            {
                Debug.LogWarning("HubCamera non trovata! Assegnala manualmente nell'inspector.");
            }
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

        // NUOVO: Salva la posizione di uscita (posizione attuale dell'hub)
        exitSpawnPosition = new Vector3(155.5f, 151.7f, 0f);

        // NUOVO: Setup iniziale delle telecamere
        SetupCameras();
    }

    private void SetupCameras()
    {
        if (mazeCamera != null && hubCamera != null)
        {
            // All'inizio il player è nel labirinto
            mazeCamera.gameObject.SetActive(true);
            hubCamera.gameObject.SetActive(false);
            playerInHub = false;
            
            if (enableDebug)
            {
                Debug.Log("Setup telecamere completato - MazeCamera attiva");
            }
        }
        else
        {
            Debug.LogError("Una o entrambe le telecamere non sono assegnate!");
        }
    }

    public void OnPlayerEnterArea()
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

    public void OnPlayerExitArea()
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

    public void EnterHub()
    {
        if (enableDebug)
            Debug.Log("Player sta entrando nell'hub");

        // Trasporta il player all'interno dell'hub
        TeleportToHub();
    }

    /*
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
    */
    
    private void TeleportToHub()
    {
        if (playerController == null)
        {
            Debug.LogError("PlayerController non trovato!");
            return;
        }

        if (enableDebug)
            Debug.Log($"Teletrasportando il player all'hub: {hubSpawnPosition}");

        // Usa il metodo SafeTransportTo del PlayerController
        playerController.SafeTransportTo(hubSpawnPosition);

        // NUOVO: Cambia telecamere
        SwitchToHubCamera();

        healthBar.SetActive(false);

        // Segna che il player è nell'hub
        playerInHub = true;

        // Nascondi l'indicatore dopo il teletrasporto
        OnPlayerExitArea();

        //Accendi il bottone dello shop
        shopButton.gameObject.SetActive(true);

        //Disattiva la minimappa
        minimap.SetActive(false);
    }
    
    public void TeleportOutOfHub()
    {
        if (playerController == null)
        {
            Debug.LogError("PlayerController non trovato!");
            return;
        }

        if (!playerInHub)
        {
            Debug.LogWarning("Il player non è nell'hub!");
            return;
        }

        if (enableDebug)
            Debug.Log($"Teletrasportando il player fuori dall'hub: {exitSpawnPosition}");

        // Teletrasporta alla posizione di uscita
        playerController.SafeTransportTo(exitSpawnPosition);

        // Cambia alla telecamera del labirinto
        SwitchToMazeCamera();

        healthBar.SetActive(true);

        // Segna che il player non è più nell'hub
        playerInHub = false;

        //Disattiva lo shop fuori da casa
        shopButton.gameObject.SetActive(false);

        //Riattiva la minimappa
        minimap.SetActive(true);
    }

    private void SwitchToHubCamera()
    {
        if (mazeCamera != null && hubCamera != null)
        {
            mazeCamera.gameObject.SetActive(false);
            hubCamera.gameObject.SetActive(true);

            if (enableDebug)
            {
                Debug.Log("Switched to HubCamera");
            }
        }
        else
        {
            Debug.LogWarning("Non è possibile cambiare telecamera - riferimenti mancanti!");
        }
    }

    private void SwitchToMazeCamera()
    {
        if (mazeCamera != null && hubCamera != null)
        {
            hubCamera.gameObject.SetActive(false);
            mazeCamera.gameObject.SetActive(true);
            
            if (enableDebug)
            {
                Debug.Log("Switched to MazeCamera");
            }
        }
        else
        {
            Debug.LogWarning("Non è possibile cambiare telecamera - riferimenti mancanti!");
        }
    }

    public bool IsPlayerInHub()
    {
        return playerInHub;
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

    #region SAVE AND LOAD

    public void Save(ref HubData data)
    {
        data.playerInHubData = playerInHub;
    }

    public void Load(HubData data)
    {
        playerInHub = data.playerInHubData;
    }


    #endregion
}

[System.Serializable]
public struct HubData
{
    public bool playerInHubData;
}
