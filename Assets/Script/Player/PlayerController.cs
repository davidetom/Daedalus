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
    public Vector3 startPos;

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
    public bool canAttackWhileMoving = true; // Permette di attaccare mentre si muove

    [Header("Attack Settings")]
    public float enemyKnockbackForce = 2f; // Forza del rinculo applicato ai nemici
    public bool enableAttackEffects = true; // Abilita effetti visivi/sonori
    public AudioClip attackSound; // Suono dell'attacco (opzionale)
    public GameObject attackEffect; // Effetto visivo dell'attacco (opzionale)

    [Header("Enemy Detection")]
    public LayerMask enemyLayerMask; // Layer dei nemici (opzionale, per ottimizzazione)

    [Header("Damage Feedback")]
    public float damageFeedbackDuration = 0.2f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool takingDamage = false;
    private Coroutine currentDamageFeedbackCoroutine = null; // NUOVO: per gestire correttamente il feedback
    
    [Header("Health Settings")]
    public float maxHealthPoints = 10f;
    [SerializeField] private float currentHealthPoints;
    public bool isDead = false;

    [Header("Coins and Gems")]
    public int coinsPicked = 0;

    // INPUT SETTINGS
    private Vector2 input;
    private Vector2 mobileInput = Vector2.zero; // Nuova variabile per mobile input

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // AGGIUNTO: inizializzazione
    }

    void Start()
    {
        currentHealthPoints = maxHealthPoints;
        
        // AGGIUNTO: Salva il colore originale per il feedback del danno
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

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
        // MODIFICA: Non può muoversi durante il danno o se è morto
        if (!isMoving && (!isAttacking || canAttackWhileMoving) && !takingDamage && !isDead)
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

                // 🔧 Aggiorna sempre i parametri di movimento per permettere la transizione corretta
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);

                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;
                
                if (IsWalkable(targetPos))
                    StartCoroutine(Move(targetPos));
            }
        }

        // Aggiorna l'animazione di movimento - durante l'attacco sarà sovrascritta dall'animazione di attacco
        animator.SetBool("isMoving", isMoving);
    }

    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;

        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            // MODIFICA: Ferma il movimento se il player prende danno o muore
            if (takingDamage || isDead)
            {
                SnapToNearestGridPosition();
                isMoving = false;
                yield break;
            }
            
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;

        // Ricalcola le distanze dopo ogni movimento
        CalcoloDistanze();

        isMoving = false;
    }
    
    void SnapToNearestGridPosition()
    {
        Vector3 currentPos = transform.position;
        
        // Calcola la posizione della griglia più vicina (assumendo griglia 1x1 con centro a 0.5, 0.5)
        float snappedX = Mathf.Round(currentPos.x - 0.5f) + 0.5f;
        float snappedY = Mathf.Round(currentPos.y - 0.7f) + 0.7f;
        
        Vector3 snappedPosition = new Vector3(snappedX, snappedY, currentPos.z);
        
        // Applica la correzione
        transform.position = snappedPosition;
        
        Debug.Log($"Player disallineato corretto da {currentPos} a {snappedPosition}");
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
        // MODIFICA: Non può attaccare se prende danno o è morto
        if ((!isMoving || canAttackWhileMoving) && !isAttacking && canAttack && !takingDamage && !isDead)
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

        // 🔧 Durante l'attacco, forza la transizione all'animazione di attacco
        // sovrascrivendo temporaneamente l'animazione di movimento
        animator.SetBool("isMoving", false);

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

        // 🔧 Dopo l'attacco, ripristina immediatamente l'animazione di movimento se necessario
        // L'HandleMovement() si occuperà di mantenere aggiornati i parametri
        animator.SetBool("isMoving", isMoving);

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
        
        // PRIMA: Controlla nemici sulla stessa tile del player (distanza 0)
        Collider2D[] collidersOnPlayer = Physics2D.OverlapPointAll(startPos, enemyLayerMask);
        
        foreach (var collider in collidersOnPlayer)
        {
            if (collider.CompareTag("Enemy"))
            {
                if (!enemiesHit.Contains(collider.gameObject))
                {
                    enemiesHit.Add(collider.gameObject);
                    Debug.Log($"Nemico trovato sulla stessa tile del player: {collider.gameObject.name}");
                }
            }
        }
        
        // POI: Per ogni tile nel range (dalla distanza 1 in poi)
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

    // METODO CORRETTO: Gestione del danno ricevuto dal player
    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            Debug.Log("Player già morto, danno ignorato");
            return;
        }

        Debug.Log($"Player riceve {damage} danni. Vita prima: {currentHealthPoints}");
        
        currentHealthPoints -= damage;
        currentHealthPoints = Mathf.Max(0, currentHealthPoints);

        Debug.Log($"Vita dopo danno: {currentHealthPoints}");

        // AGGIUNTO: Se il player si stava muovendo, fermalo e correggi la posizione
        if (isMoving)
        {
            StopAllCoroutines(); // Ferma il movimento
            SnapToNearestGridPosition(); // Correggi la posizione
            isMoving = false; // Reset dello stato
            
            // Ricalcola le distanze dalla nuova posizione corretta
            CalcoloDistanze();
        }

        // Avvia feedback visivo del danno
        StartDamageFeedback();

        if (currentHealthPoints <= 0)
        {
            Die();
        }
    }
    
    // NUOVO METODO: Gestione del feedback visivo del danno
    void StartDamageFeedback()
    {
        // Ferma il feedback precedente se esiste
        if (currentDamageFeedbackCoroutine != null)
        {
            StopCoroutine(currentDamageFeedbackCoroutine);
            currentDamageFeedbackCoroutine = null;
        }
        
        // Avvia il nuovo feedback
        currentDamageFeedbackCoroutine = StartCoroutine(DamageFeedbackCoroutine());
    }
    
    // NUOVO METODO: Coroutine per il lampeggiamento del player
    IEnumerator DamageFeedbackCoroutine()
    {
        if (spriteRenderer == null || isDead) 
        {
            currentDamageFeedbackCoroutine = null;
            yield break;
        }
        
        takingDamage = true;
        
        // Lampeggiamento con modifica dell'alpha
        float flashDuration = damageFeedbackDuration / 6f; // 6 flash totali
        
        for (int i = 0; i < 3; i++) // 3 cicli di lampeggiamento
        {
            if (isDead) break; // Interrompi se il player muore durante il feedback
            
            // Imposta alpha a 0.3 (quasi trasparente)
            Color flashColor = originalColor;
            flashColor.a = 0.3f;
            spriteRenderer.color = flashColor;
            
            yield return new WaitForSeconds(flashDuration);
            
            if (isDead) break;
            
            // Ripristina alpha originale
            spriteRenderer.color = originalColor;
            
            yield return new WaitForSeconds(flashDuration);
        }
        
        // Assicura che il colore sia completamente ripristinato
        if (!isDead && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        takingDamage = false;
        currentDamageFeedbackCoroutine = null;
    }

    void Die()
    {
        if (isDead) return;

        Debug.Log("Player morto!");
        isDead = true;
        
        // Ferma il feedback del danno se attivo
        if (currentDamageFeedbackCoroutine != null)
        {
            StopCoroutine(currentDamageFeedbackCoroutine);
            currentDamageFeedbackCoroutine = null;
        }
        takingDamage = false;
        
        StopAllCoroutines();
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Ferma tutti i movimenti e azioni
        isMoving = false;
        isAttacking = false;
        canAttack = false;
        animator.SetBool("isMoving", false);
        animator.SetBool("isAttacking", false);

        yield return new WaitForSeconds(1f);

        // Logica per un death screen

        yield return new WaitForSeconds(1.5f);

        // Logica per rimuovere il death screen

        yield return new WaitForSeconds(0.2f);
        
        // Reinizializza il player
        InizializeSettings();
    }

    void InizializeSettings()
    {
        Debug.Log("Reinizializzazione player...");

        isDead = false;
        currentHealthPoints = maxHealthPoints;
        isMoving = false;
        isAttacking = false;
        canAttack = true;
        takingDamage = false;

        // Ripristina il colore originale
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // Reset delle coroutine
        if (currentDamageFeedbackCoroutine != null)
        {
            StopCoroutine(currentDamageFeedbackCoroutine);
            currentDamageFeedbackCoroutine = null;
        }

        animator.SetBool("isAttacking", false);
        animator.SetBool("isMoving", false);
        transform.position = startPos;

        // coinsPicked = 0; oppure coinsPicked = coinsPicked / 2; DA DECIDERE

        ResetAllEnemiesState();

        InvalidateDistances();
        StartCoroutine(RecalculateDistancesNextFrame());

    }

    void ResetAllEnemiesState()
    {
        EnemyLogic[] allEnemies = GameObject.FindObjectsByType<EnemyLogic>(FindObjectsSortMode.None);
        
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && enemy.IsAlive())
            {
                // Reset dello stato di attacco e vicinanza
                enemy.closeToPlayer = false;
                enemy.isAttacking = false;
                
                // Forza il reset della direzione patrol
                if (enemy.enableDebug)
                {
                    Debug.Log($"Reset stato nemico: {enemy.name}");
                }
                
                // Usa reflection per resettare le variabili private se necessario
                var patrolDirectionField = typeof(EnemyLogic).GetField("currentPatrolDirection", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (patrolDirectionField != null)
                {
                    patrolDirectionField.SetValue(enemy, Vector2.zero);
                }
                
                var patrolStepsField = typeof(EnemyLogic).GetField("patrolStepsInDirection", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (patrolStepsField != null)
                {
                    patrolStepsField.SetValue(enemy, 0);
                }
            }
        }
        
        Debug.Log($"Reset effettuato su {allEnemies.Length} nemici");
    }

    void InvalidateDistances()
    {
        if (mapManager == null || !mapManager.wallCalculated) return;
        
        int width = mapManager.Distances.GetLength(0);
        int height = mapManager.Distances.GetLength(1);
        
        // Imposta tutte le distanze a -1 (non raggiungibile)
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                mapManager.Distances[x, y] = -1;
        
        Debug.Log("Distanze BFS invalidate - i nemici dovranno attendere il ricalcolo");
    }

    IEnumerator RecalculateDistancesNextFrame()
    {
        yield return null; // Aspetta un frame
        
        // Ricalcola le distanze dalla nuova posizione
        CalcoloDistanze();
        
        Debug.Log($"Distanze BFS ricalcolate dopo respawn a posizione: {transform.position}");
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
    
    // Metodi pubblici per accesso esterno allo stato del player
    public float GetCurrentHealth() => currentHealthPoints;
    public float GetMaxHealth() => maxHealthPoints;
    public float GetHealthPercentage() => currentHealthPoints / maxHealthPoints;
    public bool IsAlive() => !isDead;
    public bool IsTakingDamage() => takingDamage;

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