using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DoorController : MonoBehaviour
{
    public int doorID;
    [SerializeField] private bool isOpen = false; // Campo privato serializzabile per debug
    private Animator animator;
    
    // Riferimento al DayNightCycleManager per ottenere il day count
    private DayNightCycleManager dayNightManager;

    [Header("Game Win UI")]
    public Canvas victoryCanvas;
    public float durationMessage = 6f;
    public string sceneToLoad = "MainMenu";

    void Awake()
    {
        animator = GetComponent<Animator>();
        dayNightManager = UnityEngine.Object.FindFirstObjectByType<DayNightCycleManager>();

        /**
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if(collider != null && IsOuterDoor())
        {
            collider.isTrigger = true;
        }
        **/
        if(victoryCanvas != null)
        {
            victoryCanvas.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        Debug.Log($"Trigger rilevato su porta {doorID}");

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if(doorID == GetDoorOfTheDay() && isOpen)
            {
                StartCoroutine(HandleGameWin(player));
                return;
            }
            TryOpen(player);
        }
    }

    IEnumerator HandleGameWin(PlayerController player)
    {
        Debug.Log("Player ha attraversato l'uscita! Gioco completato!");

        //Disabilita il movimento del player
        if(player != null)
        {
            player.enabled = false;
        }

        victoryCanvas.gameObject.SetActive(true);

        yield return new WaitForSeconds(durationMessage);

        SceneManager.LoadScene(sceneToLoad);
    }

    public void TryOpen(PlayerController player)
    {
        Debug.Log($"TryOpen chiamato su porta {doorID}");

        // Le inner doors (ID -1) non possono essere aperte dal player
        if (doorID == -1)
        {
            Debug.Log("Le porte interne si aprono automaticamente!");
            return;
        }
        
        if (isOpen) return;

        // Calcola quale porta è "quella giusta" oggi (solo per outer doors)
        int correctDoor = GetDoorOfTheDay();

        //if (player.hasKey && doorID == correctDoor)
        if (player.CheckForKey() && doorID == correctDoor)
        {
            // MODIFICA: Apri tutte le porte con lo stesso doorID
            OpenAllDoorsWithSameID();
            Debug.Log("Porte " + doorID + " aperte con successo!");
        }
        //else if (!player.hasKey)
        else if (!player.CheckForKey()) 
        {
            Debug.Log("Hai bisogno di una chiave per aprire questa porta!");
        }
        else
        {
            Debug.Log("La porta " + doorID + " non è quella giusta per oggi! Oggi si apre la porta " + correctDoor);
        }
    }

    void OpenDoor()
    {
        isOpen = true;
        animator.SetBool("isOpen", true);
        Debug.Log("Porta " + doorID + " aperta!");
    }
    
    // NUOVO METODO: Apre tutte le porte con lo stesso doorID
    void OpenAllDoorsWithSameID()
    {
        // Trova tutte le porte nella scena
        DoorController[] allDoors = GameObject.FindObjectsByType<DoorController>(FindObjectsSortMode.None);
        
        int doorsOpened = 0;
        foreach (DoorController door in allDoors)
        {
            // Apri tutte le porte con lo stesso doorID che non sono già aperte
            if (door.doorID == this.doorID && !door.isOpen)
            {
                door.OpenDoor();
                doorsOpened++;
            }
        }
        
        Debug.Log($"Aperte {doorsOpened} porte con ID {doorID}");
    }
    
    // Metodo per chiudere la porta (usato dal MazeManager per le inner doors)
    public void CloseDoor()
    {
        isOpen = false;
        animator.SetBool("isOpen", false);
        Debug.Log("Porta " + doorID + " chiusa!");
    }
    
    // Metodo per aprire la porta senza controlli (usato dal MazeManager per le inner doors)
    public void ForceOpen()
    {
        isOpen = true;
        animator.SetBool("isOpen", true);
        Debug.Log("Porta " + doorID + " forzata aperta!");
    }

    public bool IsOpen()
    {
        return isOpen;
    }
    
    public bool IsInnerDoor()
    {
        return doorID == -1;
    }
    
    public bool IsOuterDoor()
    {
        return doorID >= 1 && doorID <= 8;
    }
    
    // Logica per scegliere la porta del giorno basata sui giorni di gioco
    int GetDoorOfTheDay()
    {
        if (dayNightManager == null)
        {
            Debug.LogWarning("DayNightCycleManager non trovato! Usando giorno 1 come fallback.");
            return 1;
        }
        
        int gameDay = dayNightManager.GetDayCount();
        
        // Cicla tra le porte 1-8 basandosi sui giorni di gioco
        int doorOfTheDay = ((gameDay - 1) % 8) + 1;
        
        Debug.Log($"Giorno di gioco: {gameDay}, Porta del giorno: {doorOfTheDay}");
        
        return doorOfTheDay;
    }
}