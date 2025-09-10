using System.Collections;
using TMPro;
using UnityEngine;

public class InnerHubController : MonoBehaviour
{
    [Header("Riferimenti Oggetti Figli")]
    [SerializeField] private Collider2D exitPoint;
    [SerializeField] private GameObject doorIndicator;
    [SerializeField] public GameObject bedIndicator;
    [SerializeField] private GameObject altarIndicator;

    [Header("Animazione Freccia")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;
    private bool isPlayerInExitPoint = false;
    private bool isPlayerInBedPoint = false;
    private bool isPlayerInAltarPoint = false;
    private bool isAnimatingDoor = false;
    private bool isAnimatingBed = false;
    private bool isAnimatingAltar = false;

    [Header("Riferimenti Sistema")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private OuterHubController outerHubController;
    [SerializeField] private BedLogic bed;
    [SerializeField] private GemSpawner gemSpawner;
    [SerializeField] private GameObject bloodOffersPanel;
    [SerializeField] private TextMeshProUGUI bloodOffersText;
    private string altarPrefix = "THE ALTAR AWAITS ";
    private string altarSuffix1 = " MORE BLOOD OFFERINGS...";
    private string altarSuffix2 = " MORE BLOOD OFFERING...";

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
                    //Debug.LogError("ExitPoint trovato ma non ha un Collider2D!");
                }
            }
            else
            {
                //Debug.LogError("Oggetto figlio 'ExitPoint' non trovato!");
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
                //Debug.LogError("Oggetto figlio 'DoorIndicator' non trovato!");
            }
        }

        if (bedIndicator == null)
        {
            Transform bedIndicatorTransform = transform.Find("BedIndicator");
            if (bedIndicatorTransform != null)
            {
                bedIndicator = bedIndicatorTransform.gameObject;
            }
            else
            {
                //Debug.LogError("Oggetto figlio 'BedIndicator' non trovato!");
            }
        }

        if (altarIndicator == null)
        {
            Transform altarIndicatorTransform = transform.Find("AltarIndicator");
            if (altarIndicatorTransform != null)
            {
                altarIndicator = altarIndicatorTransform.gameObject;
            }
            else
            {
                //Debug.LogError("Oggetto figlio 'AltarIndicator' non trovato!");
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
                //Debug.Log("OuterHubController trovato automaticamente");
            }
        }

        if (bed == null)
        {
            bed = FindFirstObjectByType<BedLogic>();
            if (bed != null && enableDebug)
            {
                //Debug.Log("Letto trovato automaticamente");
            }
        }

        if (gemSpawner == null)
        {
            gemSpawner = FindFirstObjectByType<GemSpawner>();
            if (gemSpawner != null && enableDebug)
            {
                //Debug.Log("GemSpawner trovato automaticamente");
            }
        }

        // Verifica che l'ExitPoint sia configurato come trigger
            if (exitPoint != null && !exitPoint.isTrigger)
            {
                //Debug.LogWarning("ExitPoint Collider2D dovrebbe essere configurato come Trigger!");
            }
    }

    void SetupInitialState()
    {
        if (bloodOffersPanel != null)
            bloodOffersPanel.SetActive(false);

        if (bloodOffersText != null)
            bloodOffersText.gameObject.SetActive(false);
            
        // Salva la posizione originale dell'indicatore della porta
        if (doorIndicator != null)
        {
            doorIndicator.SetActive(false); // Inizialmente disattivo
        }

        // Setup per il bed indicator
        if (bedIndicator != null)
        {
            bedIndicator.SetActive(false); // Inizialmente disattivo
        }

        // Setup per il bed indicator
        if (altarIndicator != null)
        {
            altarIndicator.SetActive(false); // Inizialmente disattivo
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
            //Debug.Log("Premi E per uscire dall'hub");
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
            //Debug.Log("Premi E per dormire");
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

    public void OnPlayerEnterAltarArea()
    {
        isPlayerInAltarPoint = true;

        // Attiva il bed indicator
        if (altarIndicator != null)
        {
            altarIndicator.SetActive(true);

            // Avvia l'animazione di movimento su e giù
            if (!isAnimatingAltar)
            {
                StartCoroutine(AnimateAltarIndicator());
            }
        }

        // Notifica al player che può interagire (opzionale)
        if (playerController != null && enableDebug)
        {
            //Debug.Log("Premi E per interagire");
        }
    }

    public void OnPlayerExitAltarArea()
    {
        isPlayerInAltarPoint = false;

        // Disattiva il bed indicator
        if (altarIndicator != null)
        {
            altarIndicator.SetActive(false);
        }

        // Ferma l'animazione
        isAnimatingAltar = false;
    }

    private IEnumerator AnimateDoorIndicator()
    {
        isAnimatingDoor = true;

        Vector3 originalDoorPosition = doorIndicator.transform.localPosition;

        while (isPlayerInExitPoint && doorIndicator != null && doorIndicator.activeInHierarchy)
        {
            // Calcola il movimento su e giù usando sin
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

            // Applica il movimento alla posizione originale
            Vector3 newPosition = originalDoorPosition;
            newPosition.y += yOffset;

            doorIndicator.transform.localPosition = newPosition;

            yield return null;
        }

        // Ripristina la posizione originale quando finisce l'animazione
        if (doorIndicator != null)
        {
            doorIndicator.transform.localPosition = originalDoorPosition;
        }

        isAnimatingDoor = false;
    }

    public IEnumerator AnimateBedIndicator()
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

    public IEnumerator AnimateAltarIndicator()
    {
        isAnimatingAltar = true;

        // Salva la posizione originale del bed indicator se non già fatto
        Vector3 originalAltarPosition = altarIndicator.transform.localPosition;

        while (isPlayerInAltarPoint && altarIndicator != null && altarIndicator.activeInHierarchy)
        {
            // Calcola il movimento su e giù usando sin
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

            // Applica il movimento alla posizione originale
            Vector3 newPosition = originalAltarPosition;
            newPosition.y += yOffset;

            altarIndicator.transform.localPosition = newPosition;

            yield return null;
        }

        // Ripristina la posizione originale quando finisce l'animazione
        if (altarIndicator != null)
        {
            altarIndicator.transform.localPosition = originalAltarPosition;
        }

        isAnimatingAltar = false;
    }

    public void ExitHub()
    {
        if (enableDebug)
        {
            //Debug.Log("Player sta uscendo dall'hub");
        }

        // Chiama il metodo TeleportOutOfHub dell'OuterHubController
        if (outerHubController != null)
        {
            outerHubController.TeleportOutOfHub();

            // Nascondi l'indicatore dopo il teletrasporto
            OnPlayerExitDoorArea();
        }
        else
        {
            //Debug.LogError("OuterHubController non trovato! Impossibile uscire dall'hub.");
        }
    }

    public void BedSleep()
    {
        if (enableDebug)
        {
            //Debug.Log("Player sta provando a dormire");
        }
        
        if (bed != null)
            bed.TrySleep();
    }

    public void InteractWithAltar()
    {
        if (enableDebug)
        {
            //Debug.Log("Player sta provando ad interagire con l'altare");
        }

        if (gemSpawner.currentPlayerDeaths < gemSpawner.deathsRequiredForRedGem)
            StartCoroutine(AltarInteraction());
        else
        {
            gemSpawner.OnRedGemCollected();
            playerController.hasBloodGem = true;

            // AGGIUNGI QUI IL SUONO DELLA GEMMA
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGemPickup();
                if (enableDebug)
                {
                    //Debug.Log("Suono gemma riprodotto!");
                }
            }
            else
            {
                //Debug.LogWarning("AudioManager.Instance non trovato!");
            }
        }
    }

    IEnumerator AltarInteraction()
    {
        if (bloodOffersPanel != null)
        {
            bloodOffersPanel.SetActive(true);

            if (bloodOffersText != null)
            {
                int deathsRemaining = gemSpawner.deathsRequiredForRedGem -
                                        gemSpawner.currentPlayerDeaths;
                string altarSuffix;
                
                if (deathsRemaining > 1)
                    altarSuffix = altarSuffix1;
                else
                    altarSuffix = altarSuffix2;

                bloodOffersText.text = altarPrefix + deathsRemaining.ToString() + altarSuffix;

                bloodOffersText.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(3f);

            bloodOffersPanel.SetActive(false);
            bloodOffersText.gameObject.SetActive(false);
        }
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
    public bool IsPlayerInAltarPoint => isPlayerInAltarPoint;
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