// MazeCameraMovement.cs - SCRIPT COMPLETO CON GAMEOBJECT BOUNDS

using System.Collections;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public GameObject player;
    public float cameraOffset = 10f;

    [Header("Auto Management")]
    public bool autoFindPlayer = true;
    
    [Header("Maze Bounds - GameObject References")]
    public bool useConstraints = false;
    public Transform topLeftBound;
    public Transform topRightBound;
    public Transform bottomLeftBound;
    public Transform bottomRightBound;
    
    [Header("Smooth Transition")]
    public float smoothTransitionSpeed = 4f;
    
    // Bounds calcolati dai GameObject
    private float leftBound;
    private float rightBound;
    private float topBound;
    private float bottomBound;
    private bool boundsCalculated = false;
    private float transitionStartTime;
    private bool isTransitioning = false;
    private bool isTransitioningAtNight = false;
    private Vector3 targetPosition;

    // Bounds esterni del labirinto
    private float outerLeftBound = 0f;
    private float outerRightBound = 310f;
    private float outerTopBound = 310f;
    private float outerBottomBound = 0f;

    public MazeManager mazeManager;

    void Start()
    {
        if (autoFindPlayer && player == null)
            player = GameObject.FindGameObjectWithTag("Player");
            
        // Trova il MazeManager per controllare lo stato delle porte
        mazeManager = FindFirstObjectByType<MazeManager>();
        
        if (mazeManager == null)
            Debug.LogWarning("MazeManager non trovato! I controlli della telecamera potrebbero non funzionare correttamente.");
            
        // Calcola i bounds iniziali dai GameObject
        CalculateBoundsFromGameObjects();
    }

    void LateUpdate()
    {
        if (player == null) return;

        if (isTransitioning)
        {
            HandleSmoothTransition();
        }
        else if (isTransitioningAtNight)
        {
            HandleNightTransition();
        }
        else
        {
            HandleNormalFollow();
        }
    }
    
    void HandleNormalFollow()
    {
        Vector3 pos = player.transform.position;
        pos.z = -cameraOffset;

        // Applica i vincoli solo se le porte sono chiuse
        if (useConstraints && boundsCalculated)
        {
            pos = ApplyBounds(pos);
        }
        else
        {
            pos = ApplyOuterBounds(pos);
        }
        
        transform.position = pos;
    }

    void HandleSmoothTransition()
    {
        // Calcola la posizione target aggiornata basata sulla posizione corrente del player
        Vector3 updatedTarget = player.transform.position;
        updatedTarget.z = -cameraOffset;

        // Applica i vincoli se necessario
        if (useConstraints && boundsCalculated)
        {
            updatedTarget = ApplyBounds(updatedTarget);
        }

        // Aggiorna il target per seguire il player
        targetPosition = updatedTarget;

        // Controlla il timeout della transizione (5 secondi)
        if (Time.time - transitionStartTime > 5f)
        {
            // Timeout raggiunto - forza il completamento della transizione
            transform.position = targetPosition;
            isTransitioning = false;
            Debug.LogWarning("Transizione telecamera forzata per timeout (5 secondi) - ritorno al follow normale");
            return;
        }

        // Muovi dolcemente verso la posizione target aggiornata
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothTransitionSpeed * Time.deltaTime);

        // Controlla se abbiamo raggiunto la destinazione
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance < 0.1f)
        {
            // Transizione completata
            transform.position = targetPosition;
            isTransitioning = false;
            Debug.Log("Transizione telecamera completata - ritorno al follow normale");
        }
    }

    void HandleNightTransition()
    {
        // Durante la transizione notturna, la telecamera si sposta verso una posizione con bounds applicati
        Vector3 nightTarget = player.transform.position;
        nightTarget.z = -cameraOffset;

        // Applica sempre i bounds per la posizione notturna
        if (boundsCalculated)
        {
            nightTarget = ApplyBounds(nightTarget);
        }

        // Aggiorna il target per seguire il player con bounds
        targetPosition = nightTarget;

        // Controlla il timeout della transizione (5 secondi)
        if (Time.time - transitionStartTime > 5f)
        {
            // Timeout raggiunto - forza il completamento della transizione
            transform.position = targetPosition;
            isTransitioningAtNight = false;
            Debug.LogWarning("Transizione notturna forzata per timeout (5 secondi) - ritorno al follow normale");
            return;
        }

        // Muovi dolcemente verso la posizione target con bounds
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothTransitionSpeed * Time.deltaTime);

        // Controlla se abbiamo raggiunto la destinazione
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance < 0.1f)
        {
            // Transizione completata
            transform.position = targetPosition;
            isTransitioning = false;
            Debug.Log("Transizione notturna completata - ritorno al follow normale con bounds");
        }
    }
    
    Vector3 ApplyBounds(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, leftBound, rightBound);
        position.y = Mathf.Clamp(position.y, bottomBound, topBound);
        return position;
    }

    Vector3 ApplyOuterBounds(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, outerLeftBound, outerRightBound);
        position.y = Mathf.Clamp(position.y, outerBottomBound, outerTopBound);
        return position;
    }

    // Calcola i bounds dai 4 GameObject di riferimento
    void CalculateBoundsFromGameObjects()
    {
        Camera cam = GetComponent<Camera>();
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        if (topLeftBound == null || topRightBound == null || bottomLeftBound == null || bottomRightBound == null)
        {
            Debug.LogWarning("Non tutti i GameObject bounds sono assegnati! Assegna topLeftBound, topRightBound, bottomLeftBound, bottomRightBound nell'Inspector.");
            boundsCalculated = false;
            return;
        }

        // Trova i valori min e max dalle posizioni dei 4 GameObject
        float minX = Mathf.Min(topLeftBound.position.x, bottomLeftBound.position.x);
        float maxX = Mathf.Max(topRightBound.position.x, bottomRightBound.position.x);
        float minY = Mathf.Min(bottomLeftBound.position.y, bottomRightBound.position.y);
        float maxY = Mathf.Max(topLeftBound.position.y, topRightBound.position.y);

        bottomBound = minY + halfHeight;
        topBound = maxY - halfHeight;
        leftBound = minX + halfWidth;
        rightBound = maxX - halfWidth;

        boundsCalculated = true;
    }
    
    // Metodo chiamato dal MazeManager quando le porte si aprono
    public void OnMazeDoorsOpened()
    {
        if (player == null || mazeManager.playerInInnerHub) return;
        
        // Calcola dove dovrebbe essere la telecamera se seguisse normalmente il player
        Vector3 idealPosition = player.transform.position;
        idealPosition.z = -cameraOffset;
        
        // Controlla se la telecamera è già centrata sul player
        float distance = Vector3.Distance(transform.position, idealPosition);
        if (distance > 1f) // Se la distanza è significativa
        {
            Debug.Log("Porte aperte - iniziando transizione dolce verso il player");
            StartSmoothTransition(idealPosition);
        }
        else
        {
            Debug.Log("Porte aperte - telecamera già centrata sul player");
        }
    }

    void StartSmoothTransition(Vector3 target)
    {
        targetPosition = target;
        isTransitioning = true;
        transitionStartTime = Time.time;
    }

    // Metodo chiamato all'inizio della notte
    public void OnNightStart()
    {
        if (player == null) return;

        // Calcola dove dovrebbe essere la telecamera con i bounds applicati
        Vector3 nightPosition = player.transform.position;
        nightPosition.z = -cameraOffset;

        if (boundsCalculated)
        {
            nightPosition = ApplyBounds(nightPosition);
        }

        // Controlla se la telecamera è già nella posizione corretta
        float distance = Vector3.Distance(transform.position, nightPosition);
        if (distance > 1f) // Se la distanza è significativa
        {
            Debug.Log("Notte iniziata - iniziando transizione dolce verso posizione con bounds");
            StartSmoothTransition(nightPosition);
            isTransitioningAtNight = true;
        }
        else
        {
            Debug.Log("Notte iniziata - telecamera già nella posizione corretta");
        }
    }
    
    // Metodo per attivare/disattivare i vincoli
    public void SetConstraintsActive(bool active)
    {
        useConstraints = active;
        Debug.Log($"Vincoli telecamera: {(active ? "ATTIVATI" : "DISATTIVATI")}");
    }
    
    // Metodo per debug - mostra i bounds e i GameObject nel Scene View
    void OnDrawGizmosSelected()
    {
        // Mostra i bounds calcolati
        if (useConstraints && boundsCalculated)
        {
            Gizmos.color = Color.red;
            Vector3 center = new Vector3((leftBound + rightBound) / 2f, (bottomBound + topBound) / 2f, 0);
            Vector3 size = new Vector3(rightBound - leftBound, topBound - bottomBound, 1f);
            Gizmos.DrawWireCube(center, size);
        }
        
        // Mostra i GameObject di riferimento
        Gizmos.color = Color.yellow;
        float sphereSize = 2f;
        
        if (topLeftBound != null)
        {
            Gizmos.DrawWireSphere(topLeftBound.position, sphereSize);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, topLeftBound.position);
            
            // Etichetta per il debug
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(topLeftBound.position + Vector3.up * 3, "TOP LEFT");
            #endif
        }
        
        Gizmos.color = Color.yellow;
        if (topRightBound != null)
        {
            Gizmos.DrawWireSphere(topRightBound.position, sphereSize);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, topRightBound.position);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(topRightBound.position + Vector3.up * 3, "TOP RIGHT");
            #endif
        }
        
        Gizmos.color = Color.yellow;
        if (bottomLeftBound != null)
        {
            Gizmos.DrawWireSphere(bottomLeftBound.position, sphereSize);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, bottomLeftBound.position);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(bottomLeftBound.position + Vector3.down * 3, "BOTTOM LEFT");
            #endif
        }
        
        Gizmos.color = Color.yellow;
        if (bottomRightBound != null)
        {
            Gizmos.DrawWireSphere(bottomRightBound.position, sphereSize);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, bottomRightBound.position);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(bottomRightBound.position + Vector3.down * 3, "BOTTOM RIGHT");
            #endif
        }
        
        // Mostra informazioni sui bounds nell'Inspector
        if (boundsCalculated)
        {
            #if UNITY_EDITOR
            Vector3 centerPos = new Vector3((leftBound + rightBound) / 2f, (bottomBound + topBound) / 2f, 0);
            UnityEditor.Handles.Label(centerPos, $"Bounds: ({leftBound:F1}, {bottomBound:F1}) to ({rightBound:F1}, {topBound:F1})");
            #endif
        }
    }
}