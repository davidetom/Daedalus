using UnityEngine;
using UnityEngine.Tilemaps;

public class HubCameraMovement : MonoBehaviour
{
    [Header("Player Reference")]
    public GameObject player;

    [Header("Camera Settings")]
    public float cameraOffset = 10f; // Distanza Z dalla telecamera

    [Header("Hub Tilemap Bounds")]
    public Tilemap hubTilemap; // Riferimento alla tilemap dell'hub
    [SerializeField] private float tilemapTopBound = 169;
    [SerializeField] private float tilemapBottomBound = 153;
    [SerializeField] private float topBound;
    [SerializeField] private float bottomBound;

    [Header("Fixed Position")]
    public float fixedXPosition = 405f; // Posizione X fissa della telecamera

    [Header("Camera Smoothing")]
    public bool enableSmoothing = true;
    public float smoothSpeed = 5f; // Velocità di smoothing del movimento verticale

    [Header("Debug")]
    public bool enableDebug = false;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        float cameraHeight = cam.orthographicSize;
        bottomBound = tilemapBottomBound + cameraHeight;
        topBound = tilemapTopBound - cameraHeight;
        
        InitializeReferences();
    }

    void InitializeReferences()
    {
        // Trova automaticamente il player se non assegnato
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj;
                if (enableDebug)
                {
                    Debug.Log("Player trovato automaticamente per HubCamera");
                }
            }
            else
            {
                Debug.LogError("Player non trovato! Assegna il player manualmente o usa il tag 'Player'.");
            }
        }

        // Trova automaticamente la tilemap dell'hub se non assegnata
        if (hubTilemap == null)
        {
            // Cerca una tilemap chiamata "HubTilemap" o simile
            GameObject hubTilemapObj = GameObject.Find("HubTilemap");
            if (hubTilemapObj == null)
            {
                hubTilemapObj = GameObject.Find("Hub_Tilemap");
            }
            if (hubTilemapObj == null)
            {
                // Cerca qualsiasi tilemap con "Hub" nel nome
                Tilemap[] allTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
                foreach (var tilemap in allTilemaps)
                {
                    if (tilemap.name.ToLower().Contains("hub"))
                    {
                        hubTilemap = tilemap;
                        break;
                    }
                }
            }
            else
            {
                hubTilemap = hubTilemapObj.GetComponent<Tilemap>();
            }

            if (hubTilemap != null && enableDebug)
            {
                Debug.Log($"HubTilemap trovata automaticamente: {hubTilemap.name}");
            }
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        HubController hubController = FindFirstObjectByType<HubController>();
        if (hubController == null || !hubController.IsPlayerInHub())
        {
            return; // Non aggiornare la camera se il player non è nell'hub
        }

        // Posizione attuale della telecamera
        Vector3 currentPos = transform.position;

        // Posizione target
        Vector3 targetPos = new Vector3(
            fixedXPosition,                    // X fisso
            GetYPosition(),             // Y vincolato ai bounds
            player.transform.position.z - cameraOffset  // Z con offset
        );

        // Applica il movimento (con o senza smoothing)
        if (enableSmoothing)
        {
            transform.position = Vector3.Lerp(currentPos, targetPos, smoothSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPos;
        }

        if (enableDebug && Time.frameCount % 60 == 0) // Debug ogni secondo circa
        {
            Debug.Log($"HubCamera - Player Y: {player.transform.position.y:F2}, Camera Y: {transform.position.y:F2}");
        }
    }

    float GetYPosition()
    {
        // Ottieni la posizione Y del player
        float playerY = player.transform.position.y;

        // Vincola la Y ai bounds della tilemap
        float clampedY = Mathf.Clamp(playerY, bottomBound, topBound);

        return clampedY;
    }

    // Metodi pubblici per configurazione runtime
    public void SetFixedXPosition(float newX)
    {
        fixedXPosition = newX;
        if (enableDebug)
        {
            Debug.Log($"HubCamera X position aggiornata a: {newX}");
        }
    }

    // Metodi per debug
    [ContextMenu("Print Current Bounds")]
    public void PrintCurrentBounds()
    {
        Debug.Log($"HubCamera Bounds - Top: {topBound}, Bottom: {bottomBound}, X fisso: {fixedXPosition}");
    }

    // Proprietà pubbliche per accesso esterno
    public float TopBound => topBound;
    public float BottomBound => bottomBound;

    // Metodo per verificare se il player è nei bounds dell'hub
    public bool IsPlayerInHubBounds()
    {
        if (player == null) return false;

        float playerY = player.transform.position.y;
        return playerY >= bottomBound && playerY <= topBound;
    }

    void OnValidate()
    {
        // Validazione nell'editor
        if (smoothSpeed <= 0)
            smoothSpeed = 5f;

        if (cameraOffset <= 0)
            cameraOffset = 10f;
    }

    void OnDrawGizmos()
    {
        if (!enableDebug) return;

        // Disegna i bounds della telecamera
        Gizmos.color = Color.yellow;

        // Linea superiore
        Vector3 topLeft = new Vector3(fixedXPosition - 5f, topBound, 0);
        Vector3 topRight = new Vector3(fixedXPosition + 5f, topBound, 0);
        Gizmos.DrawLine(topLeft, topRight);

        // Linea inferiore
        Vector3 bottomLeft = new Vector3(fixedXPosition - 5f, bottomBound, 0);
        Vector3 bottomRight = new Vector3(fixedXPosition + 5f, bottomBound, 0);
        Gizmos.DrawLine(bottomLeft, bottomRight);

        // Linee verticali
        Gizmos.DrawLine(topLeft, bottomLeft);
        Gizmos.DrawLine(topRight, bottomRight);

        // Posizione X fissa
        Gizmos.color = Color.red;
        Vector3 fixedXTop = new Vector3(fixedXPosition, topBound + 2f, 0);
        Vector3 fixedXBottom = new Vector3(fixedXPosition, bottomBound - 2f, 0);
        Gizmos.DrawLine(fixedXTop, fixedXBottom);
    }
}