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

    [Header("Tilemap Reference")]
    public Tilemap tilemap;
    public TileBase muraTile;
    public TileBase corridoioTile; // Il tile su cui il nemico può camminare

    [Header("Pathfinding")]
    public int maxPathDistance = 50;
    public bool enableDebug = false; // Spento di default per performance
    private List<Vector3Int> currentPath;
    private int currentPathIndex;
    private float pathRecalculateTimer = 0f;
    private const float PATH_RECALCULATE_INTERVAL = 2f; // Calcola percorso ogni 2 secondi
    private Vector3Int lastKnownPlayerPosition;
    private Vector3Int generalTargetDirection; // Direzione generale verso il player

    [Header("Movement Behavior")]
    public float directionChangeChance = 0.1f; // 10% chance di cambiare direzione casualmente
    public int maxMovesWithoutRecalculation = 8; // Max mosse prima di ricalcolare forzatamente
    public float maxChaseDistance = 15f; // Distanza massima per inseguire il player
    private int movesSinceLastCalculation = 0;
    
    [Header("Random Patrol")]
    private Vector2 currentPatrolDirection = Vector2.zero;
    private bool isPatrolling = false;

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
        pathRecalculateTimer += Time.deltaTime;
        HandleMovement();
    }

    Vector2 FindPlayer()
    {
        if (player == null) 
        {
            if (enableDebug) Debug.Log("Player non trovato!");
            return GetPatrolDirection();
        }

        Vector3Int enemyPos = tilemap.WorldToCell(transform.position);
        Vector3Int playerPos = tilemap.WorldToCell(player.position);
        float distanceToPlayer = GetDistance(enemyPos, playerPos);
        
        // Se il player è troppo lontano, entra in modalità pattugliamento
        if (distanceToPlayer > maxChaseDistance)
        {
            if (!isPatrolling)
            {
                if (enableDebug) Debug.Log($"Player troppo lontano ({distanceToPlayer}), iniziando pattugliamento");
                isPatrolling = true;
                currentPath = null; // Cancella il percorso esistente
            }
            return GetPatrolDirection();
        }
        
        // Se era in pattugliamento e ora il player è vicino, riprendi l'inseguimento
        if (isPatrolling)
        {
            if (enableDebug) Debug.Log("Player vicino, riprendendo inseguimento");
            isPatrolling = false;
            currentPatrolDirection = Vector2.zero;
        }
        
        // FASE 1: Ricalcola il percorso solo quando necessario
        bool shouldRecalculate = pathRecalculateTimer >= PATH_RECALCULATE_INTERVAL || 
                                currentPath == null || 
                                movesSinceLastCalculation >= maxMovesWithoutRecalculation ||
                                Vector3Int.Distance(playerPos, lastKnownPlayerPosition) > 3; // Player si è mosso molto

        if (shouldRecalculate)
        {
            pathRecalculateTimer = 0f;
            movesSinceLastCalculation = 0;
            lastKnownPlayerPosition = playerPos;
            
            List<Vector3Int> path = FindPath(enemyPos, playerPos);
            
            if (path != null && path.Count > 1)
            {
                currentPath = path;
                currentPathIndex = 1;
                
                // Calcola direzione generale per quando il pathfinding non è disponibile
                generalTargetDirection = GetGeneralDirection(enemyPos, playerPos);
                
                if (enableDebug) Debug.Log($"Nuovo percorso calcolato: {path.Count} nodi");
                
                // Segui il percorso preciso
                return GetDirectionFromPath(enemyPos);
            }
            else
            {
                // Nessun percorso trovato, usa direzione generale
                generalTargetDirection = GetGeneralDirection(enemyPos, playerPos);
                if (enableDebug) Debug.Log("Nessun percorso - uso direzione generale");
            }
        }

        // FASE 2: Tra i calcoli, usa strategie intelligenti
        movesSinceLastCalculation++;
        
        // Se hai un percorso valido, seguilo
        if (currentPath != null && currentPathIndex < currentPath.Count)
        {
            Vector2 pathDirection = GetDirectionFromPath(enemyPos);
            if (pathDirection != Vector2.zero)
            {
                return pathDirection;
            }
        }

        // FASE 3: Movimento euristico intelligente
        return GetSmartHeuristicDirection(enemyPos, playerPos);
    }

    Vector2 GetPatrolDirection()
    {
        Vector3 currentPos = transform.position;
        Vector3Int enemyPos = tilemap.WorldToCell(currentPos);
        
        // Se non hai una direzione di pattugliamento o la strada è bloccata
        if (currentPatrolDirection == Vector2.zero || !IsWalkable(currentPos + (Vector3)currentPatrolDirection))
        {
            // Scegli una nuova direzione, dando priorità a quelle che avvicinano al player
            Vector2[] possibleDirections = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            List<Vector2> validDirections = new List<Vector2>();
            List<Vector2> playerDirections = new List<Vector2>(); // Direzioni che avvicinano al player
            
            // Se il player esiste, calcola le direzioni che ci avvicinano
            if (player != null)
            {
                Vector3Int playerPos = tilemap.WorldToCell(player.position);
                Vector3Int diff = playerPos - enemyPos;
                
                // Direzioni che riducono la distanza dal player
                if (diff.x > 0) playerDirections.Add(Vector2.right);
                if (diff.x < 0) playerDirections.Add(Vector2.left);
                if (diff.y > 0) playerDirections.Add(Vector2.up);
                if (diff.y < 0) playerDirections.Add(Vector2.down);
            }
            
            // Trova tutte le direzioni valide (non bloccate)
            foreach (Vector2 dir in possibleDirections)
            {
                if (IsWalkable(currentPos + (Vector3)dir))
                {
                    validDirections.Add(dir);
                }
            }
            
            if (validDirections.Count > 0)
            {
                Vector2 chosenDirection = Vector2.zero;
                
                // PRIORITÀ 1: Direzioni che avvicinano al player E sono percorribili
                List<Vector2> goodDirections = validDirections.Where(dir => playerDirections.Contains(dir)).ToList();
                
                if (goodDirections.Count > 0)
                {
                    // Scegli casualmente tra le direzioni "buone"
                    chosenDirection = goodDirections[UnityEngine.Random.Range(0, goodDirections.Count)];
                    if (enableDebug) Debug.Log($"Pattugliamento verso player: {chosenDirection}");
                }
                else
                {
                    // PRIORITÀ 2: Nessuna direzione "buona", scegli casualmente tra quelle valide
                    chosenDirection = validDirections[UnityEngine.Random.Range(0, validDirections.Count)];
                    if (enableDebug) Debug.Log($"Pattugliamento casuale: {chosenDirection}");
                }
                
                currentPatrolDirection = chosenDirection;
            }
            else
            {
                // Se non ci sono direzioni valide, ferma il pattugliamento
                currentPatrolDirection = Vector2.zero;
                if (enableDebug) Debug.Log("Nessuna direzione valida per pattugliamento");
            }
        }
        
        return currentPatrolDirection;
    }

    Vector2 GetDirectionFromPath(Vector3Int enemyPos)
    {
        if (currentPath == null || currentPathIndex >= currentPath.Count) 
            return Vector2.zero;

        Vector3Int nextNode = currentPath[currentPathIndex];
        
        // Se abbiamo raggiunto il nodo corrente, passa al successivo
        if (enemyPos == nextNode)
        {
            currentPathIndex++;
            if (currentPathIndex >= currentPath.Count) return Vector2.zero;
            nextNode = currentPath[currentPathIndex];
        }
        
        return new Vector2(nextNode.x - enemyPos.x, nextNode.y - enemyPos.y);
    }

    Vector3Int GetGeneralDirection(Vector3Int from, Vector3Int to)
    {
        Vector3Int diff = to - from;
        return new Vector3Int(
            diff.x > 0 ? 1 : (diff.x < 0 ? -1 : 0),
            diff.y > 0 ? 1 : (diff.y < 0 ? -1 : 0),
            0
        );
    }

    Vector2 GetSmartHeuristicDirection(Vector3Int enemyPos, Vector3Int playerPos)
    {
        // Lista delle direzioni possibili in ordine di preferenza
        List<Vector2> preferredDirections = new List<Vector2>();
        
        // Direzione diretta verso il player (Manhattan)
        Vector3Int diff = playerPos - enemyPos;
        
        // Aggiungi le direzioni in ordine di distanza
        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
        {
            // Movimento orizzontale prioritario
            if (diff.x > 0) preferredDirections.Add(Vector2.right);
            else if (diff.x < 0) preferredDirections.Add(Vector2.left);
            
            if (diff.y > 0) preferredDirections.Add(Vector2.up);
            else if (diff.y < 0) preferredDirections.Add(Vector2.down);
        }
        else
        {
            // Movimento verticale prioritario
            if (diff.y > 0) preferredDirections.Add(Vector2.up);
            else if (diff.y < 0) preferredDirections.Add(Vector2.down);
            
            if (diff.x > 0) preferredDirections.Add(Vector2.right);
            else if (diff.x < 0) preferredDirections.Add(Vector2.left);
        }
        
        // Aggiungi le direzioni rimanenti per evitare di rimanere bloccati
        Vector2[] allDirections = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (Vector2 dir in allDirections)
        {
            if (!preferredDirections.Contains(dir))
                preferredDirections.Add(dir);
        }
        
        // Piccola probabilità di esplorare casualmente (anti-loop)
        if (UnityEngine.Random.Range(0f, 1f) < directionChangeChance)
        {
            preferredDirections = preferredDirections.OrderBy(x => UnityEngine.Random.Range(0f, 1f)).ToList();
        }
        
        // Prova ogni direzione fino a trovarne una valida
        foreach (Vector2 direction in preferredDirections)
        {
            Vector3 testPos = transform.position + (Vector3)direction;
            if (IsWalkable(testPos))
            {
                if (enableDebug) Debug.Log($"Direzione euristica: {direction}");
                return direction;
            }
        }
        
        if (enableDebug) Debug.Log("Nessuna direzione valida trovata");
        return Vector2.zero;
    }

    List<Vector3Int> FindPath(Vector3Int start, Vector3Int target)
    {
        // Lista dei nodi da esplorare
        List<Node> openList = new List<Node>();
        // Lista dei nodi già esplorati
        HashSet<Vector3Int> closedList = new HashSet<Vector3Int>();
        
        // Nodo iniziale
        Node startNode = new Node(start, null, 0, GetDistance(start, target));
        openList.Add(startNode);
        
        while (openList.Count > 0)
        {
            // Trova il nodo con il costo F più basso
            Node currentNode = openList.OrderBy(n => n.FCost).First();
            openList.Remove(currentNode);
            closedList.Add(currentNode.position);
            
            // Se abbiamo raggiunto il target
            if (currentNode.position == target)
            {
                return ReconstructPath(currentNode);
            }
            
            // Esplora i nodi vicini (solo 4 direzioni: su, giù, sinistra, destra)
            Vector3Int[] neighbors = new Vector3Int[]
            {
                currentNode.position + Vector3Int.up,
                currentNode.position + Vector3Int.down,
                currentNode.position + Vector3Int.left,
                currentNode.position + Vector3Int.right
            };
            
            foreach (Vector3Int neighborPos in neighbors)
            {
                // Salta se già esplorato
                if (closedList.Contains(neighborPos))
                    continue;
                
                // Salta se non è camminabile
                if (!IsWalkableForPathfinding(neighborPos))
                    continue;
                
                // Salta se troppo lontano (per evitare calcoli infiniti)
                if (GetDistance(start, neighborPos) > maxPathDistance)
                    continue;
                
                float newGCost = currentNode.gCost + 1;
                
                // Controlla se questo percorso verso il vicino è migliore
                Node existingNeighbor = openList.FirstOrDefault(n => n.position == neighborPos);
                
                if (existingNeighbor == null)
                {
                    // Nuovo nodo
                    Node newNeighbor = new Node(neighborPos, currentNode, newGCost, GetDistance(neighborPos, target));
                    openList.Add(newNeighbor);
                }
                else if (newGCost < existingNeighbor.gCost)
                {
                    // Percorso migliore trovato
                    existingNeighbor.parent = currentNode;
                    existingNeighbor.gCost = newGCost;
                }
            }
        }
        
        // Nessun percorso trovato
        return null;
    }
    
    List<Vector3Int> ReconstructPath(Node endNode)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Node currentNode = endNode;
        
        while (currentNode != null)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }
        
        path.Reverse();
        return path;
    }
    
    float GetDistance(Vector3Int a, Vector3Int b)
    {
        // Distanza Manhattan (solo movimenti orizzontali/verticali)
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    
    bool IsWalkableForPathfinding(Vector3Int cellPosition)
    {
        if (tilemap == null) 
        {
            if (enableDebug) Debug.LogWarning("Tilemap non assegnata!");
            return false;
        }
        
        TileBase tileAtPosition = tilemap.GetTile(cellPosition);
        
        // PRIORITA 1: Se hai definito corridoioTile, usa solo quello
        if (corridoioTile != null)
        {
            bool isWalkable = tileAtPosition == corridoioTile;
            if (enableDebug && !isWalkable) 
                Debug.Log($"Posizione {cellPosition} non è corridoio. Tile: {tileAtPosition?.name ?? "null"}");
            return isWalkable;
        }
        
        // PRIORITA 2: Se non hai corridoioTile, evita solo i muri
        if (tileAtPosition == null) 
        {
            if (enableDebug) Debug.Log($"Nessun tile alla posizione {cellPosition}");
            return false; // Cambiato da true - probabilmente spazio vuoto non camminabile
        }
        
        // Controlla se è un muro
        bool isWall = muraTile != null && tileAtPosition == muraTile;
        
        if (!isWall)
        {
            var colliderType = tilemap.GetColliderType(cellPosition);
            isWall = colliderType != Tile.ColliderType.None;
        }
        
        if (enableDebug && isWall)
            Debug.Log($"Posizione {cellPosition} è un muro. Tile: {tileAtPosition?.name}");
        
        return !isWall;
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
        if (tilemap == null)
        {
            Debug.LogWarning("Tilemap non assegnata!");
            return true;
        }

        Vector3Int cellPosition = tilemap.WorldToCell(targetPos);
        return IsWalkableForPathfinding(cellPosition);
    }
}

// Classe per rappresentare un nodo nell'algoritmo A*
public class Node
{
    public Vector3Int position;
    public Node parent;
    public float gCost; // Distanza dal nodo iniziale
    public float hCost; // Distanza euristica dal target
    public float FCost => gCost + hCost; // Costo totale
    
    public Node(Vector3Int pos, Node parentNode, float g, float h)
    {
        position = pos;
        parent = parentNode;
        gCost = g;
        hCost = h;
    }
}