using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems; // serve per i pulsanti mobile

public class PlayerController : MonoBehaviour
{
    public float moveSpeed;
    public bool isMoving;
    private Vector2 input;

    [Header("Tilemap Reference")]
    public Tilemap tilemap;
    public TileBase muraTile;

    [Header("Map Reference")]
    public MapManager mapManager; // Riferimento al MapManager

    [Header("Door Interaction")]
    public float interactRange = 1f; // distanza massima per interagire con la porta
    public KeyCode interactKey = KeyCode.E;
    public bool hasKey = false; //all'inizio non ha la chiave

    public int coinCount;

    private Animator animator;

    // 🔹 Nuova variabile per mobile input
    private Vector2 mobileInput = Vector2.zero;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        // Aspetta che il MapManager sia inizializzato, poi calcola le distanze iniziali
        if (mapManager != null && mapManager.wallCalculated)
        {
            CalcoloDistanze();
        }
    }

    void Update()
    {
        // Calcola le distanze al primo frame utile se non è stato ancora fatto
        if (mapManager != null && mapManager.wallCalculated && mapManager.Distances[0,0] == 0 && !HasCalculatedDistances())
        {
            CalcoloDistanze();
        }

        HandleMovement();

        // Controlla input per aprire la porta
        if (Input.GetKeyDown(interactKey))
        {
            TryOpenNearbyDoor();
        }
    }

    // Metodo helper per verificare se le distanze sono state calcolate
    private bool HasCalculatedDistances()
    {
        Vector2Int playerArrayPos = mapManager.WorldToArrayCoordinates(transform.position);
        if (mapManager.IsValidArrayCoordinate(playerArrayPos))
        {
            return mapManager.Distances[playerArrayPos.x, playerArrayPos.y] == 0;
        }
        return false;
    }

      void HandleMovement()
    {
        if (!isMoving)
        {
            // 🔹 Se sto usando pulsanti mobile → uso mobileInput
            // altrimenti uso Input da tastiera
            if (mobileInput != Vector2.zero)
            {
                input = mobileInput;
            }
            else
            {
                input.x = Input.GetAxisRaw("Horizontal");
                input.y = Input.GetAxisRaw("Vertical");
            }

            if (input.x != 0) input.y = 0;

            if (input != Vector2.zero)
            {
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);
                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;
                
                if (IsWalkable(targetPos))
                    StartCoroutine(Move(targetPos));
            }
        }

        animator.SetBool("isMoving", isMoving);
    }

    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;

        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;

        // Ricalcola le distanze dopo ogni movimento
        CalcoloDistanze();

        isMoving = false;
    }

    public bool IsWalkable(Vector3 targetPos)
    {
        if (tilemap == null)
        {
            Debug.LogWarning("Tilemap non assegnata!");
            return true;
        }

        // Prima verifica usando il MapManager se disponibile
        if (mapManager != null && mapManager.wallCalculated)
        {
            if (mapManager.IsWallAtWorldPosition(targetPos))
            {
                return false;
            }
        }
        else
        {
            // Fallback al sistema originale se MapManager non è disponibile
            Vector3Int cellPosition = tilemap.WorldToCell(targetPos);
            TileBase tileAtPosition = tilemap.GetTile(cellPosition);

            bool isWall = (muraTile != null && tileAtPosition == muraTile);

            if (!isWall && tileAtPosition != null)
            {
                var colliderType = tilemap.GetColliderType(cellPosition);
                isWall = (colliderType != Tile.ColliderType.None);
            }

            if (isWall) return false;
        }

        // Verifica se c'è una porta chiusa in quella cella
        Collider2D doorCollider = Physics2D.OverlapPoint(targetPos);
        if (doorCollider != null)
        {
            DoorController door = doorCollider.GetComponent<DoorController>();
            if (door != null && !door.IsOpen())
            {
                return false; // Porta chiusa → non camminabile
            }
        }

        return true;
    }

    void TryOpenNearbyDoor()
    {
        DoorController[] doors = GameObject.FindObjectsByType<DoorController>(FindObjectsSortMode.None);

        foreach (var door in doors)
        {
            float distance = Vector3.Distance(transform.position, door.transform.position);
            if (distance <= interactRange)
            {
                door.TryOpen(this);
                break;
            }
        }
    }

    void CalcoloDistanze()
    {
        if (mapManager == null || !mapManager.wallCalculated)
        {
            Debug.LogWarning("MapManager non disponibile o non inizializzato!");
            return;
        }

        Vector2Int playerArrayPos = mapManager.WorldToArrayCoordinates(transform.position);
        
        if (!mapManager.IsValidArrayCoordinate(playerArrayPos))
        {
            Debug.LogWarning($"Posizione player fuori dai bounds della mappa: {playerArrayPos}");
            return;
        }

        BFS(playerArrayPos, mapManager.Distances, mapManager.Walls);
    }
    
    void BFS(Vector2Int start, int[,] dist, bool[,] walls)
    {
        int width = dist.GetLength(0);
        int height = dist.GetLength(1);

        // Reset distanze
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dist[x, y] = -1;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        dist[start.x, start.y] = 0;

        // Movimenti ortogonali
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int d = dist[current.x, current.y];

            foreach (var dir in dirs)
            {
                Vector2Int next = current + dir;

                // Controllo limiti
                if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height)
                    continue;
                
                // Controllo muri
                if (walls[next.x, next.y])
                    continue;

                // Se non visitato, aggiorna distanza e aggiungi alla coda
                if (dist[next.x, next.y] == -1)
                {
                    dist[next.x, next.y] = d + 1;
                    queue.Enqueue(next);
                }
            }
        }
    }

    // Metodi di utilità per accedere alle distanze
    public int GetDistanceAtCurrentPosition()
    {
        if (mapManager == null) return -1;
        return mapManager.GetDistanceAtWorldPosition(transform.position);
    }

    public int GetDistanceAtPosition(Vector3 worldPos)
    {
        if (mapManager == null) return -1;
        return mapManager.GetDistanceAtWorldPosition(worldPos);
    }


    // ----------------- Metodi per pulsanti mobile -----------------
    public void MuoviSu() => mobileInput = Vector2.up;
    public void MuoviGiu() => mobileInput = Vector2.down;
    public void MuoviDestra() => mobileInput = Vector2.right;
    public void MuoviSinistra() => mobileInput = Vector2.left;
    public void StopMovimento() => mobileInput = Vector2.zero;

    public void PulsanteAzione()
    {
        // Al momento solo porta
        TryOpenNearbyDoor();

        // FUTURO: attacco nemico
        // if (nemicoVicino) AttaccaNemico();
    }
}