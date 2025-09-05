using System.Collections;
using UnityEngine;

public class InnerHubController : MonoBehaviour
{
    [Header("Riferimenti Oggetti Figli")]
    [SerializeField] private Collider2D exitPoint;
    [SerializeField] private GameObject doorIndicator;
    [SerializeField] private GameObject bedIndicator;

    [Header("Animazione Freccia")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;
    private Vector3 originalIndicatorPosition;
    private bool isPlayerInExitPoint = false;
    private bool isPlayerInBedPoint = false;
    private bool isAnimatingDoor = false;
    private bool isAnimatingBed = false;

    [Header("Riferimenti Sistema")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private OuterHubController outerHubController;
    [SerializeField] private BedLogic bed;

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
        if (exitPoint == null)
        {
            Transform exitPointTransform = transform.Find("ExitPoint");
            if (exitPointTransform != null)
            {
                exitPoint = exitPointTransform.GetComponent<Collider2D>();
                if (exitPoint == null)
                {
                    Debug.LogError("ExitPoint trovato ma non ha un Collider2D!");
                }
            }
            else
            {
                Debug.LogError("Oggetto figlio 'ExitPoint' non trovato!");
            }
        }

        if (doorIndicator == null)
        {
            Transform doorIndicatorTransform = transform.Find("InnerDoorIndicator");
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

        // Trova automaticamente l'OuterHubController se non assegnato
        if (outerHubController == null)
        {
            outerHubController = FindFirstObjectByType<OuterHubController>();
            if (outerHubController != null && enableDebug)
            {
                Debug.Log("OuterHubController trovato automaticamente");
            }
        }

        if (bed == null)
        {
            bed = FindFirstObjectByType<BedLogic>();
            if (bed != null && enableDebug)
            {
                Debug.Log("Letto trovato automaticamente");
            }
        }

        // Verifica che l'ExitPoint sia configurato come trigger
            if (exitPoint != null && !exitPoint.isTrigger)
            {
                Debug.LogWarning("ExitPoint Collider2D dovrebbe essere configurato come Trigger!");
            }
    }

    void SetupInitialState()
    {
        // Salva la posizione originale dell'indicatore della porta
        if (doorIndicator != null)
        {
            originalIndicatorPosition = doorIndicator.transform.localPosition;
            doorIndicator.SetActive(false); // Inizialmente disattivo
        }

        // NUOVO: Setup per il bed indicator
        if (bedIndicator != null)
        {
            bedIndicator.SetActive(false); // Inizialmente disattivo
        }
    }

    public void OnPlayerEnterDoorArea()
    {
        isPlayerInExitPoint = true;
        
        // Attiva il door indicator
        if (doorIndicator != null)
        {
            doorIndicator.SetActive(true);
            
            // Avvia l'animazione di movimento su e giù
            if (!isAnimatingDoor)
            {
                StartCoroutine(AnimateDoorIndicator());
            }
        }

        // Notifica al player che può interagire (opzionale)
        if (playerController != null && enableDebug)
        {
            Debug.Log("Premi E per uscire dall'hub");
        }
    }

    public void OnPlayerExitDoorArea()
    {
        isPlayerInExitPoint = false;
        
        // Disattiva il door indicator
        if (doorIndicator != null)
        {
            doorIndicator.SetActive(false);
        }

        // Ferma l'animazione
        isAnimatingDoor = false;
    }

    public void OnPlayerEnterBedArea()
    {
        isPlayerInBedPoint = true;

        // Attiva il bed indicator
        if (bedIndicator != null)
        {
            bedIndicator.SetActive(true);

            // Avvia l'animazione di movimento su e giù
            if (!isAnimatingBed)
            {
                StartCoroutine(AnimateBedIndicator());
            }
        }

        // Notifica al player che può interagire (opzionale)
        if (playerController != null && enableDebug)
        {
            Debug.Log("Premi E per dormire");
        }
    }

    public void OnPlayerExitBedArea()
    {
        isPlayerInBedPoint = false;

        // Disattiva il bed indicator
        if (bedIndicator != null)
        {
            bedIndicator.SetActive(false);
        }

        // Ferma l'animazione
        isAnimatingBed = false;
    }

    private IEnumerator AnimateDoorIndicator()
    {
        isAnimatingDoor = true;

        while (isPlayerInExitPoint && doorIndicator != null && doorIndicator.activeInHierarchy)
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

        isAnimatingDoor = false;
    }

    private IEnumerator AnimateBedIndicator()
    {
        isAnimatingBed = true;

        // Salva la posizione originale del bed indicator se non già fatto
        Vector3 originalBedPosition = bedIndicator.transform.localPosition;

        while (isPlayerInBedPoint && bedIndicator != null && bedIndicator.activeInHierarchy)
        {
            // Calcola il movimento su e giù usando sin
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

            // Applica il movimento alla posizione originale
            Vector3 newPosition = originalBedPosition;
            newPosition.y += yOffset;

            bedIndicator.transform.localPosition = newPosition;

            yield return null;
        }

        // Ripristina la posizione originale quando finisce l'animazione
        if (bedIndicator != null)
        {
            bedIndicator.transform.localPosition = originalBedPosition;
        }

        isAnimatingBed = false;
    }

    public void ExitHub()
    {
        if (enableDebug)
            Debug.Log("Player sta uscendo dall'hub");

        // Chiama il metodo TeleportOutOfHub dell'OuterHubController
        if (outerHubController != null)
        {
            outerHubController.TeleportOutOfHub();

            // Nascondi l'indicatore dopo il teletrasporto
            OnPlayerExitDoorArea();
        }
        else
        {
            Debug.LogError("OuterHubController non trovato! Impossibile uscire dall'hub.");
        }
    }

    public void BedSleep()
    {
        if (enableDebug)
            Debug.Log("Player sta provando a dormire");
        
        if (bed != null)
            bed.TrySleep();
    }

    // Metodi pubblici per debugging
    [ContextMenu("Test Exit Hub")]
    public void TestExitHub()
    {
        ExitHub();
    }

    [ContextMenu("Force Show Indicator")]
    public void ForceShowIndicator()
    {
        OnPlayerEnterDoorArea();
    }

    [ContextMenu("Force Hide Indicator")]
    public void ForceHideIndicator()
    {
        OnPlayerExitDoorArea();
    }

    // Proprietà pubbliche per accesso esterno
    public bool IsPlayerInExitPoint => isPlayerInExitPoint;
    public bool IsPlayerInBedPoint => isPlayerInBedPoint;
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