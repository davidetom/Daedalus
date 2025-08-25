using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class EnemyLogic : MonoBehaviour
{
    public float moveSpeed;
    public bool isMoving;
    public Vector2 targetDirection;
    private Vector2 input;

    [Header("Map Reference")]
    public MapManager mapManager; // Riferimento al MapManager per accedere alle distanze BFS

    [Header("Tilemap Reference")]
    public Tilemap tilemap;
    public TileBase muraTile;
    public TileBase corridoioTile; // Il tile su cui il nemico può camminare

    [Header("AI Behavior")]
    [Range(10, 350)]
    public int intelligentChaseDistance = 100; // Distanza in tile per comportamento intelligente
    public bool enableDebug = false; // Debug abilitato/disabilitato
    
    [Header("Random Patrol")]
    public float directionChangeChance = 0.3f; // Probabilità di cambiare direzione durante il patrol
    public float playerBias = 0.6f; // Bias verso il player durante il patrol (0-1)
    private Vector2 currentPatrolDirection = Vector2.zero;
    private int patrolStepsInDirection = 0;
    private int maxPatrolStepsInDirection = 5; // Max passi nella stessa direzione durante patrol

    private Animator animator;
    private Transform player;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        // Trova il player nella scena
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        HandleMovement();
    }

    Vector2 FindPlayer()
    {
        if (player == null) 
        {
            if (enableDebug) Debug.Log("Player non trovato!");
            return GetRandomPatrolDirection();
        }

        if (mapManager == null || !mapManager.wallCalculated)
        {
            if (enableDebug) Debug.Log("MapManager non disponibile, usando movimento casuale");
            return GetRandomPatrolDirection();
        }

        // Ottieni posizioni in coordinate array
        Vector2Int enemyArrayPos = mapManager.WorldToArrayCoordinates(transform.position);
        Vector2Int playerArrayPos = mapManager.WorldToArrayCoordinates(player.position);

        // Verifica che entrambe le posizioni siano valide
        if (!mapManager.IsValidArrayCoordinate(enemyArrayPos) || !mapManager.IsValidArrayCoordinate(playerArrayPos))
        {
            return GetRandomPatrolDirection();
        }

        // Ottieni la distanza BFS dalla matrice calcolata dal player
        int distanceToPlayer = mapManager.Distances[enemyArrayPos.x, enemyArrayPos.y];
        
        // Decide il comportamento in base alla distanza
        if (distanceToPlayer >= 0 && distanceToPlayer <= intelligentChaseDistance)
        {
            // MODALITÀ INTELLIGENTE: Usa la matrice BFS per trovare il percorso ottimale
            return GetIntelligentDirection(enemyArrayPos);
        }
        else
        {
            // MODALITÀ PATROL: Movimento pseudo-casuale con bias verso il player
            return GetPatrolDirection(enemyArrayPos, playerArrayPos);
        }
    }

    Vector2 GetIntelligentDirection(Vector2Int enemyArrayPos)
    {
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2Int[] directionOffsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        int currentDistance = mapManager.Distances[enemyArrayPos.x, enemyArrayPos.y];
        
        if (currentDistance <= 0)
        {
            if (enableDebug) Debug.Log("Già raggiunto il player o distanza non valida");
            return Vector2.zero;
        }

        List<DirectionInfo> validDirections = new List<DirectionInfo>();

        // Analizza tutte le 4 direzioni
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int nextArrayPos = enemyArrayPos + directionOffsets[i];
            
            // Verifica bounds
            if (!mapManager.IsValidArrayCoordinate(nextArrayPos))
                continue;

            // Verifica se la cella è camminabile (non è un muro)
            if (mapManager.Walls[nextArrayPos.x, nextArrayPos.y])
                continue;

            // Ottieni la distanza BFS della cella adiacente
            int nextDistance = mapManager.Distances[nextArrayPos.x, nextArrayPos.y];
            
            // Se la distanza è valida (>= 0), significa che c'è un percorso verso il player
            if (nextDistance >= 0)
            {
                validDirections.Add(new DirectionInfo
                {
                    direction = directions[i],
                    distance = nextDistance
                });

                if (enableDebug)
                {
                    Debug.Log($"Direzione {directions[i]}: distanza {nextDistance}");
                }
            }
        }

        if (validDirections.Count == 0)
        {
            if (enableDebug) Debug.Log("Nessuna direzione valida trovata nell'inseguimento intelligente");
            return Vector2.zero;
        }

        // Trova la direzione con la distanza minore (più vicina al player)
        DirectionInfo bestDirection = validDirections.OrderBy(d => d.distance).First();
        
        if (enableDebug)
        {
            Debug.Log($"Direzione scelta: {bestDirection.direction} (distanza: {bestDirection.distance})");
        }

        return bestDirection.direction;
    }

    Vector2 GetPatrolDirection(Vector2Int enemyArrayPos, Vector2Int playerArrayPos)
    {
        // Incrementa il contatore dei passi nella direzione corrente
        if (currentPatrolDirection != Vector2.zero)
        {
            patrolStepsInDirection++;
        }

        // Cambia direzione se:
        // 1. Non hai una direzione corrente
        // 2. La direzione corrente è bloccata
        // 3. Hai fatto troppi passi nella stessa direzione
        // 4. Probabilità casuale di cambiare direzione
        bool shouldChangeDirection = currentPatrolDirection == Vector2.zero ||
                                   !IsDirectionWalkable(enemyArrayPos, currentPatrolDirection) ||
                                   patrolStepsInDirection >= maxPatrolStepsInDirection ||
                                   UnityEngine.Random.Range(0f, 1f) < directionChangeChance;

        if (shouldChangeDirection)
        {
            Vector2 newDirection = ChooseNewPatrolDirection(enemyArrayPos, playerArrayPos);
            if (newDirection != Vector2.zero)
            {
                currentPatrolDirection = newDirection;
                patrolStepsInDirection = 0;
                
                if (enableDebug)
                {
                    Debug.Log($"Nuova direzione patrol: {currentPatrolDirection}");
                }
            }
        }

        return currentPatrolDirection;
    }

    Vector2 ChooseNewPatrolDirection(Vector2Int enemyArrayPos, Vector2Int playerArrayPos)
    {
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2Int[] directionOffsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        List<DirectionInfo> validDirections = new List<DirectionInfo>();
        List<DirectionInfo> playerDirections = new List<DirectionInfo>(); // Direzioni verso il player

        // Calcola la differenza per il bias verso il player
        Vector2Int playerDiff = playerArrayPos - enemyArrayPos;

        for (int i = 0; i < directions.Length; i++)
        {
            if (IsDirectionWalkable(enemyArrayPos, directions[i]))
            {
                DirectionInfo dirInfo = new DirectionInfo
                {
                    direction = directions[i],
                    distance = 0 // Non importante per il patrol
                };

                validDirections.Add(dirInfo);

                // Verifica se questa direzione ci avvicina al player
                Vector2Int dirOffset = directionOffsets[i];
                if ((playerDiff.x > 0 && dirOffset.x > 0) || 
                    (playerDiff.x < 0 && dirOffset.x < 0) ||
                    (playerDiff.y > 0 && dirOffset.y > 0) || 
                    (playerDiff.y < 0 && dirOffset.y < 0))
                {
                    playerDirections.Add(dirInfo);
                }
            }
        }

        if (validDirections.Count == 0)
        {
            if (enableDebug) Debug.Log("Nessuna direzione valida per patrol");
            return Vector2.zero;
        }

        // Applica bias verso il player
        if (playerDirections.Count > 0 && UnityEngine.Random.Range(0f, 1f) < playerBias)
        {
            // Scegli una direzione che si avvicina al player
            DirectionInfo chosenDir = playerDirections[UnityEngine.Random.Range(0, playerDirections.Count)];
            if (enableDebug) Debug.Log($"Patrol con bias verso player: {chosenDir.direction}");
            return chosenDir.direction;
        }
        else
        {
            // Scegli una direzione casuale tra quelle valide
            DirectionInfo chosenDir = validDirections[UnityEngine.Random.Range(0, validDirections.Count)];
            if (enableDebug) Debug.Log($"Patrol casuale: {chosenDir.direction}");
            return chosenDir.direction;
        }
    }

    Vector2 GetRandomPatrolDirection()
    {
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        List<Vector2> validDirections = new List<Vector2>();

        Vector3 currentPos = transform.position;
        
        foreach (Vector2 dir in directions)
        {
            if (IsWalkable(currentPos + (Vector3)dir))
            {
                validDirections.Add(dir);
            }
        }

        if (validDirections.Count > 0)
        {
            return validDirections[UnityEngine.Random.Range(0, validDirections.Count)];
        }

        return Vector2.zero;
    }

    bool IsDirectionWalkable(Vector2Int fromArrayPos, Vector2 direction)
    {
        Vector2Int directionOffset = Vector2Int.zero;
        
        if (direction == Vector2.up) directionOffset = Vector2Int.up;
        else if (direction == Vector2.down) directionOffset = Vector2Int.down;
        else if (direction == Vector2.left) directionOffset = Vector2Int.left;
        else if (direction == Vector2.right) directionOffset = Vector2Int.right;
        else return false;

        Vector2Int targetArrayPos = fromArrayPos + directionOffset;

        // Verifica bounds
        if (!mapManager.IsValidArrayCoordinate(targetArrayPos))
            return false;

        // Verifica se non è un muro
        return !mapManager.Walls[targetArrayPos.x, targetArrayPos.y];
    }

    void HandleMovement()
    {
        if (!isMoving)
        {
            targetDirection = FindPlayer();
            input.x = targetDirection.x;
            input.y = targetDirection.y;

            // Assicurati di muoverti solo in una direzione alla volta
            if (input.x != 0) input.y = 0;

            if (enableDebug && input != Vector2.zero)
                Debug.Log($"Input movimento: {input}");

            if (input != Vector2.zero)
            {
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);
                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;

                if (enableDebug)
                    Debug.Log($"Tentativo movimento da {transform.position} a {targetPos}");

                if (IsWalkable(targetPos))
                {
                    if (enableDebug) Debug.Log("Movimento iniziato!");
                    StartCoroutine(Move(targetPos));
                }
                else
                {
                    if (enableDebug) Debug.Log("Movimento bloccato - posizione non camminabile");
                    // Reset della direzione patrol se è bloccata
                    if (currentPatrolDirection == input)
                    {
                        currentPatrolDirection = Vector2.zero;
                        patrolStepsInDirection = 0;
                    }
                }
            }
            else
            {
                if (enableDebug) Debug.Log("Nessun input movimento");
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

        isMoving = false;
    }

    public bool IsWalkable(Vector3 targetPos)
    {
        if (mapManager != null && mapManager.wallCalculated)
        {
            // Usa il MapManager se disponibile (più efficiente e coerente)
            return mapManager.IsWalkableAtWorldPosition(targetPos);
        }
        else
        {
            // Fallback al sistema originale
            if (tilemap == null)
            {
                Debug.LogWarning("Tilemap non assegnata!");
                return true;
            }

            Vector3Int cellPosition = tilemap.WorldToCell(targetPos);
            TileBase tileAtPosition = tilemap.GetTile(cellPosition);

            // Se hai definito corridoioTile, usa solo quello
            if (corridoioTile != null)
            {
                return tileAtPosition == corridoioTile;
            }

            // Altrimenti evita solo i muri
            if (tileAtPosition == null) return false;
            
            bool isWall = muraTile != null && tileAtPosition == muraTile;
            
            if (!isWall)
            {
                var colliderType = tilemap.GetColliderType(cellPosition);
                isWall = colliderType != Tile.ColliderType.None;
            }
            
            return !isWall;
        }
    }

    // Metodi di debug per visualizzare informazioni
    public int GetCurrentDistanceFromPlayer()
    {
        if (mapManager == null || !mapManager.wallCalculated) return -1;
        
        Vector2Int enemyArrayPos = mapManager.WorldToArrayCoordinates(transform.position);
        if (!mapManager.IsValidArrayCoordinate(enemyArrayPos)) return -1;
        
        return mapManager.Distances[enemyArrayPos.x, enemyArrayPos.y];
    }

    public bool IsInIntelligentMode()
    {
        int distance = GetCurrentDistanceFromPlayer();
        return distance >= 0 && distance <= intelligentChaseDistance;
    }

    // Visualizza informazioni nell'Inspector durante il gioco
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !enableDebug) return;

        // Disegna un cerchio colorato per indicare la modalità
        if (IsInIntelligentMode())
        {
            Gizmos.color = Color.red; // Modalità inseguimento intelligente
        }
        else
        {
            Gizmos.color = Color.yellow; // Modalità patrol
        }
        
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        // Mostra la direzione del movimento
        if (targetDirection != Vector2.zero)
        {
            Gizmos.color = Color.blue;
            Vector3 targetPos = transform.position + (Vector3)targetDirection;
            Gizmos.DrawLine(transform.position, targetPos);
        }
    }
}

// Classe helper per informazioni sulle direzioni
[System.Serializable]
public class DirectionInfo
{
    public Vector2 direction;
    public int distance;
}