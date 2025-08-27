using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems; // serve per i pulsanti mobile

public class PlayerController : MonoBehaviour
{
    private Animator animator;

    [Header("Tilemap Reference")]
    public Tilemap tilemap;
    public TileBase muraTile;

    [Header("Map Reference")]
    public MapManager mapManager; // Riferimento al MapManager

    [Header("Move Settings")]
    public float moveSpeed;
    public bool isMoving = false;

    [Header("Door Interaction")]
    public float interactRange = 1f; // distanza massima per interagire con la porta
    public KeyCode interactKey = KeyCode.E;
    public bool hasKey = false; //all'inizio non ha la chiave

    [Header("Attack Settings")]
    public float attackDamage = 5f;
    public float attackRange = 2f;
    public float recoil = 1f;
    public float attackCooldown = 0.3f;
    private bool attackAnimationFinished = false;
    private Vector2 lastDirection = Vector2.down;
    public bool isAttacking = false;
    public bool canAttack = true;
    public bool isNightTime = true; // il player può attaccare solo di notte

    [Header("Enemy Attack Settings")]
    public float enemyKnockbackForce = 2f; // Forza del rinculo applicato ai nemici
    public bool enableAttackEffects = true; // Abilita effetti visivi/sonori
    public AudioClip attackSound; // Suono dell'attacco (opzionale)
    public GameObject attackEffect; // Effetto visivo dell'attacco (opzionale)

    [Header("Enemy Detection")]
    public LayerMask enemyLayerMask; // Layer dei nemici (opzionale, per ottimizzazione)

    [Header("Coins and Gems")]
    public int coinsPicked = 0;

    // INPUT SETTINGS
    private Vector2 input;
    private Vector2 mobileInput = Vector2.zero; // Nuova variabile per mobile input

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
            if (isNightTime) HandleAttack();
            else TryOpenNearbyDoor();
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
        if (!isMoving && !isAttacking)
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
                lastDirection = input;

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
        if (mapManager != null && mapManager.wallCalculated)
        {
            // Usa il nuovo sistema del MapManager
            Vector2Int arrayPos = mapManager.WorldToArrayCoordinates(targetPos);
            if (!mapManager.IsValidArrayCoordinate(arrayPos))
                return false;
                
            // Il player può camminare su corridoi e porte
            if (!mapManager.IsWalkableForPlayer(arrayPos))
                return false;
        }
        else
        {
            // Fallback al sistema originale se MapManager non è disponibile
            if (tilemap == null)
            {
                Debug.LogWarning("Tilemap non assegnata!");
                return true;
            }

            Vector3Int cellPosition = tilemap.WorldToCell(targetPos);
            TileBase tileAtPosition = tilemap.GetTile(cellPosition);

            bool isWall = muraTile != null && tileAtPosition == muraTile;

            if (!isWall && tileAtPosition != null)
            {
                var colliderType = tilemap.GetColliderType(cellPosition);
                isWall = colliderType != Tile.ColliderType.None;
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

    public void HandleAttack()
    {
        if (!isMoving && !isAttacking && canAttack)
        {
            animator.SetFloat("attackX", lastDirection.x);
            animator.SetFloat("attackY", lastDirection.y);

            // Rileva nemici prima di iniziare l'attacco
            List<GameObject> enemiesInRange = DetectEnemiesTileByTile(lastDirection);
            
            StartCoroutine(Attack(enemiesInRange));
        }

        animator.SetBool("isAttacking", isAttacking);
    }

    IEnumerator Attack(List<GameObject> enemiesToHit)
    {
        isAttacking = true;
        canAttack = false;
        attackAnimationFinished = false;

        // Effetti sonori dell'attacco
        if (enableAttackEffects && attackSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
        }

        yield return null;
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"));
        
        // Aspetta che l'Animation Event segnali la fine
        while (!attackAnimationFinished)
        {
            yield return null;
        }

        // Applica danno ai nemici quando l'animazione finisce
        foreach (var enemy in enemiesToHit)
        {
            if (enemy != null) // Controlla se il nemico esiste ancora
            {
                ApplyDamageToEnemy(enemy);
            }
        }

        isAttacking = false;
        animator.SetBool("isAttacking", false);

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    public void OnAttackAnimationEnd()
    {
        attackAnimationFinished = true;
    }

    private List<GameObject> DetectEnemiesTileByTile(Vector2 attackDirection)
    {
        List<GameObject> enemiesHit = new List<GameObject>();
        Vector3 startPos = transform.position;
        
        // Per ogni tile nel range
        for (int distance = 1; distance <= attackRange; distance++)
        {
            Vector3 tilePos = startPos + new Vector3(
                attackDirection.x * distance, 
                attackDirection.y * distance, 
                0
            );
            
            // Controlla nemici in questa tile
            Collider2D[] colliders = Physics2D.OverlapPointAll(tilePos, enemyLayerMask);
            
            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Enemy"))
                {
                    if (!enemiesHit.Contains(collider.gameObject))
                    {
                        enemiesHit.Add(collider.gameObject);
                    }
                }
            }
            
            // OPZIONALE: Fermati se incontri un muro
            if (mapManager != null && mapManager.GetTileTypeAtWorldPos(tilePos) == TileType.Wall)
            {
                break; // L'attacco non passa attraverso i muri
            }
        }
        
        return enemiesHit;
    }

    // Metodo per applicare danno al nemico - IMPLEMENTATO COMPLETAMENTE
    private void ApplyDamageToEnemy(GameObject enemy)
    {
        // Cerca il componente EnemyLogic
        EnemyLogic enemyLogic = enemy.GetComponent<EnemyLogic>();
        if (enemyLogic != null && enemyLogic.IsAlive())
        {
            // Calcola la direzione del rinculo (dal player verso il nemico)
            Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
            
            // Applica il danno con la direzione del rinculo
            enemyLogic.TakeDamage(attackDamage, knockbackDirection);
            
            Debug.Log($"Attaccato {enemy.name} per {attackDamage} danni! Vita rimanente: {enemyLogic.GetCurrentHealth()}");
            
            // Effetti visivi dell'attacco
            if (enableAttackEffects && attackEffect != null)
            {
                // Spawna l'effetto a metà strada tra player e nemico
                Vector3 effectPosition = Vector3.Lerp(transform.position, enemy.transform.position, 0.5f);
                GameObject effect = Instantiate(attackEffect, effectPosition, Quaternion.identity);
                
                // Distruggi l'effetto dopo un po' (se non ha un sistema di autodistruzione)
                if (effect.GetComponent<ParticleSystem>() == null)
                {
                    Destroy(effect, 2f);
                }
            }
        }
        else
        {
            Debug.LogWarning($"Il nemico {enemy.name} non ha il componente EnemyLogic o è già morto!");
        }
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

    // CALCOLO DISTANZE PER I NEMICI - MODIFICATO PER USARE SOLO CORRIDOI
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

        // IMPORTANTE: Usa il nuovo BFS che considera solo i corridoi
        BFS_CorridorsOnly(playerArrayPos, mapManager.Distances, mapManager.TileTypes);
    }
    
    // Nuovo metodo BFS che considera solo i corridoi per l'AI
    void BFS_CorridorsOnly(Vector2Int start, int[,] dist, TileType[,] tileTypes)
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
                
                // IMPORTANTE: Considera solo i corridoi per l'AI pathfinding
                // I nemici possono raggiungere solo tile corridoio
                if (tileTypes[next.x, next.y] != TileType.Corridor)
                    continue;

                // Se non visitato, aggiorna distanza e aggiungi alla coda
                if (dist[next.x, next.y] == -1)
                {
                    dist[next.x, next.y] = d + 1;
                    queue.Enqueue(next);
                }
            }
        }
        
        //Debug.Log($"BFS completata da posizione player: {start}");
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
        if (isNightTime) HandleAttack();
        else TryOpenNearbyDoor();

        // FUTURO: attacco nemico
        // if (nemicoVicino) AttaccaNemico();
    }
}