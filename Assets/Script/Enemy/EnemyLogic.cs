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
    [Range(1, 5)]
    public int stopDistance = 2; // Distanza minima dal player (in tile BFS) - 2 significa 1 tile fisico di separazione
    public bool enableDebug = false; // Debug abilitato/disabilitato
    
    [Header("Random Patrol")]
    public float directionChangeChance = 0.3f; // Probabilità di cambiare direzione durante il patrol
    public float playerBias = 0.6f; // Bias verso il player durante il patrol (0-1)
    private Vector2 currentPatrolDirection = Vector2.zero;
    private int patrolStepsInDirection = 0;
    private int maxPatrolStepsInDirection = 5; // Max passi nella stessa direzione durante patrol

    [Header("Health Settings")]
    public float maxHealthPoints = 10f;
    [SerializeField] private float currentHealthPoints;
    public bool isDead = false;
    
    [Header("Knockback Settings")]
    public float knockbackForce = 2f;
    public float knockbackDuration = 0.5f;
    private bool isKnockedBack = false;
    private Vector2 knockbackDirection = Vector2.zero;
    
    [Header("Death Effects")]
    public GameObject deathEffect; // Particelle o effetti alla morte (opzionale)
    public AudioClip deathSound; // Suono alla morte (opzionale)
    public float deathDelay = 0.1f; // Delay prima di distruggere il GameObject

    [Header("Damage Feedback")]
    public float damageFeedbackDuration = 0.2f;
    public Color damageColor = Color.red;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool takingDamage = false;
    private Coroutine currentDamageFeedbackCoroutine = null;

    [Header("Attack Settings")]
    public bool closeToPlayer = false;
    public float attackDamage = 2f;
    public float attackDuration = 0.3f;
    public float attackCooldown = 1f;
    public bool isAttacking = false;
    private bool canAttack = true;

    private Animator animator;
    private Transform playerTransform;
    private Vector3 lastPlayerPosition = Vector3.zero; // NUOVO: per tracciare la posizione del player
    private float playerPositionCheckInterval = 0.1f; // Controlla ogni 0.1 secondi
    private float lastPlayerPositionCheckTime = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Trova il player nella scena
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    void Start()
    {
        // Inizializza la vita
        currentHealthPoints = maxHealthPoints;
        
        // Salva il colore originale per il feedback del danno
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        if (isDead) return; // Non fare nulla se è morto

        // NUOVO: Monitora la posizione del player per rilevare respawn o teletrasporti
        CheckPlayerPositionChange();

        HandleKnockback();
        HandleMovement();
        HandleAttack();
    }

    void CheckPlayerPositionChange()
    {
        if (playerTransform == null || Time.time < lastPlayerPositionCheckTime + playerPositionCheckInterval) 
            return;
            
        lastPlayerPositionCheckTime = Time.time;
        
        // Se è la prima volta, inizializza
        if (lastPlayerPosition == Vector3.zero)
        {
            lastPlayerPosition = playerTransform.position;
            return;
        }
        
        // Calcola la distanza dal controllo precedente
        float distanceMoved = Vector3.Distance(playerTransform.position, lastPlayerPosition);
        
        // Controlla anche se il player è morto/respawnato
        PlayerController pc = playerTransform.GetComponent<PlayerController>();
        bool playerJustRespawned = pc != null && !pc.IsAlive();
        
        // NUOVO: Controlla se la matrice BFS è in uno stato inconsistente
        bool bfsInconsistent = IsBFSInconsistent();
        
        // Condizioni per forzare ricalcolo BFS:
        bool shouldRecalculate = false;
        string reason = "";
        
        // 1. Movimento drastico (teletrasporto/respawn)
        if (distanceMoved > 2f)
        {
            shouldRecalculate = true;
            reason = $"movimento drastico ({distanceMoved:F1} unità)";
        }
        
        // 2. Player respawnato
        if (playerJustRespawned)
        {
            shouldRecalculate = true;
            reason = "player respawnato";
        }
        
        // 3. BFS inconsistente
        if (bfsInconsistent)
        {
            shouldRecalculate = true;
            reason = "BFS inconsistente";
        }
        
        // 4. NUOVO: Controllo se siamo "bloccati" in modalità closeToPlayer ma il player è lontano
        if (closeToPlayer && mapManager != null && mapManager.wallCalculated)
        {
            Vector2Int playerArrayPos = mapManager.WorldToArrayCoordinates(playerTransform.position);
            Vector2Int enemyArrayPos = mapManager.WorldToArrayCoordinates(transform.position);
            
            if (mapManager.IsValidArrayCoordinate(playerArrayPos) && mapManager.IsValidArrayCoordinate(enemyArrayPos))
            {
                // Calcola distanza fisica effettiva
                float physicalDistance = Vector3.Distance(transform.position, playerTransform.position);
                
                // Se siamo in modalità closeToPlayer ma fisicamente siamo lontani, c'è un problema
                if (physicalDistance > stopDistance * 1.5f) // Margine di tolleranza
                {
                    shouldRecalculate = true;
                    reason = $"nemico bloccato in closeToPlayer ma distanza fisica è {physicalDistance:F1}";
                    
                    // Reset immediato dello stato
                    closeToPlayer = false;
                    isAttacking = false;
                }
            }
        }
        
        if (shouldRecalculate)
        {
            if (enableDebug)
            {
                Debug.Log($"{gameObject.name}: Ricalcolo BFS per: {reason}");
            }
            
            ForcePlayerBFSRecalculation();
            
            // Reset dello stato del nemico per sicurezza
            closeToPlayer = false;
            currentPatrolDirection = Vector2.zero;
            patrolStepsInDirection = 0;
        }
        
        lastPlayerPosition = playerTransform.position;
    }

    // NUOVO METODO: Verifica se la BFS è in uno stato inconsistente
    bool IsBFSInconsistent()
    {
        if (mapManager == null || !mapManager.wallCalculated) return true;
        
        Vector2Int playerArrayPos = mapManager.WorldToArrayCoordinates(playerTransform.position);
        Vector2Int enemyArrayPos = mapManager.WorldToArrayCoordinates(transform.position);
        
        if (!mapManager.IsValidArrayCoordinate(playerArrayPos) || !mapManager.IsValidArrayCoordinate(enemyArrayPos))
            return true;
        
        // Il player deve sempre essere a distanza 0 da se stesso
        int playerDistance = mapManager.Distances[playerArrayPos.x, playerArrayPos.y];
        if (playerDistance != 0)
        {
            if (enableDebug)
            {
                Debug.Log($"BFS inconsistente: player a distanza {playerDistance} da se stesso");
            }
            return true;
        }
        
        // Se siamo in closeToPlayer, dovremmo avere una distanza BFS valida e piccola
        if (closeToPlayer)
        {
            int enemyDistance = mapManager.Distances[enemyArrayPos.x, enemyArrayPos.y];
            if (enemyDistance < 0 || enemyDistance > intelligentChaseDistance)
            {
                if (enableDebug)
                {
                    Debug.Log($"BFS inconsistente: nemico closeToPlayer ma distanza BFS = {enemyDistance}");
                }
                return true;
            }
        }
        
        return false;
    }

    // NUOVO: Forza il ricalcolo delle distanze BFS tramite il PlayerController
    void ForcePlayerBFSRecalculation()
    {
        if (playerTransform != null)
        {
            PlayerController pc = playerTransform.GetComponent<PlayerController>();
            if (pc != null)
            {
                // Usa reflection per chiamare il metodo privato CalcoloDistanze
                var calcoloDistanzeMethod = typeof(PlayerController).GetMethod("CalcoloDistanze", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (calcoloDistanzeMethod != null)
                {
                    calcoloDistanzeMethod.Invoke(pc, null);
                    
                    if (enableDebug)
                    {
                        Debug.Log($"{gameObject.name}: BFS ricalcolata con successo.");
                    }
                }
            }
        }
    }

    void HandleKnockback()
    {
        // Il rinculo ora viene gestito dalla coroutine KnockbackMovement()
        // Questo metodo serve solo per debug/stato
        if (enableDebug && isKnockedBack)
        {
            Debug.Log($"{gameObject.name}: In stato di rinculo verso {knockbackDirection}");
        }
    }

    // FIXED: Metodo pubblico per ricevere danni - VERSIONE SEMPLIFICATA
    public void TakeDamage(float damage, Vector2 attackDirection = default)
    {
        if (isDead) return; // Protezione base
        
        currentHealthPoints -= damage;
        currentHealthPoints = Mathf.Max(0, currentHealthPoints);
        
        if (enableDebug)
        {
            Debug.Log($"{gameObject.name} ha ricevuto {damage} danni. Vita rimanente: {currentHealthPoints}");
        }
        
        // Feedback visivo del danno
        StartDamageFeedback();
        
        // Applica rinculo se possibile e se non è già in rinculo
        if (attackDirection != Vector2.zero && !isKnockedBack)
        {
            ApplyKnockback(attackDirection);
        }
        
        // Controlla se è morto
        if (currentHealthPoints <= 0)
        {
            Die();
        }
    }
    
    // FIXED: Gestione corretta del feedback del danno
    void StartDamageFeedback()
    {
        // Ferma il feedback precedente se esiste
        if (currentDamageFeedbackCoroutine != null)
        {
            StopCoroutine(currentDamageFeedbackCoroutine);
            currentDamageFeedbackCoroutine = null;
        }
        
        // Avvia il nuovo feedback solo se non è morto
        if (!isDead)
        {
            currentDamageFeedbackCoroutine = StartCoroutine(DamageFeedbackCoroutine());
        }
    }
    
    // FIXED: Versione corretta della coroutine di feedback
    IEnumerator DamageFeedbackCoroutine()
    {
        if (spriteRenderer == null || isDead) 
        {
            currentDamageFeedbackCoroutine = null;
            yield break;
        }
        
        takingDamage = true;
        spriteRenderer.color = damageColor;
        
        yield return new WaitForSeconds(damageFeedbackDuration);
        
        // Ripristina il colore solo se non è morto
        if (!isDead && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        takingDamage = false;
        currentDamageFeedbackCoroutine = null;
    }
    
    // FIXED: Applica il rinculo - VERSIONE SEMPLIFICATA
    void ApplyKnockback(Vector2 direction)
    {
        if (isDead || isKnockedBack) return;
        
        // Converte la direzione in movimento ortogonale tile-based
        Vector2 orthogonalDirection = GetOrthogonalDirection(direction);
        
        // Calcola la posizione target (esattamente 1 tile di distanza)
        Vector3 targetPosition = transform.position + (Vector3)orthogonalDirection;
        
        // Verifica che la posizione target sia camminabile
        if (!IsWalkable(targetPosition))
        {
            if (enableDebug)
            {
                Debug.Log($"{gameObject.name}: Posizione di rinculo non camminabile, rinculo annullato");
            }
            return;
        }
        
        // Imposta lo stato di rinculo
        isKnockedBack = true;
        
        // Ferma il movimento corrente
        if (isMoving)
        {
            StopAllCoroutines();
            // Riavvia il feedback del danno se era attivo
            if (takingDamage)
            {
                StartDamageFeedback();
            }
            isMoving = false;
        }
        
        // Reset della direzione patrol
        currentPatrolDirection = Vector2.zero;
        patrolStepsInDirection = 0;
        
        // Avvia la coroutine del movimento di rinculo
        StartCoroutine(KnockbackMovement(targetPosition));
        
        if (enableDebug)
        {
            Debug.Log($"{gameObject.name}: Avviato rinculo fluido verso {targetPosition}");
        }
    }
    
    // Coroutine per movimento di rinculo fluido
    IEnumerator KnockbackMovement(Vector3 targetPosition)
    {
        // Movimento fluido come quello del player
        while ((targetPosition - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, knockbackForce * Time.deltaTime);
            yield return null;
        }
        
        // Assicura posizione finale esatta
        transform.position = targetPosition;
        
        // Fine del rinculo
        isKnockedBack = false;
        
        if (enableDebug)
        {
            Debug.Log($"{gameObject.name}: Rinculo completato a posizione {transform.position}");
        }
    }
    
    // Converte una direzione qualsiasi in direzione ortogonale (su/giù/sinistra/destra)
    Vector2 GetOrthogonalDirection(Vector2 inputDirection)
    {
        // Normalizza la direzione
        Vector2 normalizedDir = inputDirection.normalized;
        
        // Trova la componente più forte
        if (Mathf.Abs(normalizedDir.x) > Mathf.Abs(normalizedDir.y))
        {
            // Movimento orizzontale
            return normalizedDir.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            // Movimento verticale
            return normalizedDir.y > 0 ? Vector2.up : Vector2.down;
        }
    }
    
    // FIXED: Gestisce la morte del nemico
    void Die()
    {
        if (isDead) return; // Previeni chiamate multiple
        
        isDead = true;
        
        // Ferma correttamente il feedback del danno
        if (currentDamageFeedbackCoroutine != null)
        {
            StopCoroutine(currentDamageFeedbackCoroutine);
            currentDamageFeedbackCoroutine = null;
        }
        takingDamage = false;
        
        if (enableDebug)
        {
            Debug.Log($"{gameObject.name} è morto!");
        }
        
        // Ferma tutti i movimenti
        StopAllCoroutines();
        isMoving = false;
        isKnockedBack = false;
        isAttacking = false;
        closeToPlayer = false;
        
        // Disabilita il collider per evitare ulteriori interazioni
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        // Effetti di morte
        StartCoroutine(DeathSequence());
    }
    
    IEnumerator DeathSequence()
    {
        // Effetti visivi/audio (opzionali)
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        if (deathSound != null && GetComponent<AudioSource>() != null)
        {
            GetComponent<AudioSource>().PlayOneShot(deathSound);
        }
        
        // Fade out del colore
        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0);
            
            float fadeTime = deathDelay * 0.8f; // Usa l'80% del tempo per il fade
            float timer = 0;
            
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                float progress = timer / fadeTime;
                spriteRenderer.color = Color.Lerp(startColor, targetColor, progress);
                yield return null;
            }
        }
        
        yield return new WaitForSeconds(deathDelay);
        
        // Distruggi il GameObject
        Destroy(gameObject);
    }

    // Proprietà pubbliche per accesso esterno
    public float GetCurrentHealth() => currentHealthPoints;
    public float GetMaxHealth() => maxHealthPoints;
    public float GetHealthPercentage() => currentHealthPoints / maxHealthPoints;
    public bool IsAlive() => !isDead;
    public bool IsKnockedBack() => isKnockedBack;

    Vector2 FindPlayer()
    {
        // Se è in rinculo, non muoversi attivamente
        if (isKnockedBack) return Vector2.zero;
        
        if (playerTransform == null) 
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
        Vector2Int playerArrayPos = mapManager.WorldToArrayCoordinates(playerTransform.position);

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

        // FIXED: Controlla se può attaccare (stessa tile o adiacente)
        if (currentDistance <= stopDistance)
        {
            closeToPlayer = true;
            if (enableDebug) Debug.Log($"Nemico a distanza di attacco dal player (distanza: {currentDistance}).");
            
            // Se è sulla stessa tile (distanza 0), non muoversi
            if (currentDistance == 0)
            {
                return Vector2.zero;
            }
            // Se è a distanza 1, può scegliere di muoversi o fermarsi per attaccare
            else if (currentDistance == 1)
            {
                // 50% possibilità di fermarsi per attaccare
                if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
                {
                    return Vector2.zero;
                }
            }
        }
        else
        {
            closeToPlayer = false;
        }

        List<DirectionInfo> validDirections = new List<DirectionInfo>();

        // Analizza tutte le 4 direzioni
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int nextArrayPos = enemyArrayPos + directionOffsets[i];
            
            // Verifica bounds
            if (!mapManager.IsValidArrayCoordinate(nextArrayPos))
                continue;

            // IMPORTANTE: Verifica se la cella è camminabile per AI (solo corridoi)
            if (!mapManager.IsWalkableForAI(nextArrayPos))
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
        
        // CONTROLLO AGGIUNTIVO: Non muoversi se la prossima mossa ci porterebbe alla distanza di stop o meno
        if (bestDirection.distance < stopDistance)
        {
            if (enableDebug) Debug.Log($"Prossima mossa troppo vicina al player (distanza: {bestDirection.distance}, stop: {stopDistance}). Stop movimento.");
            return Vector2.zero;
        }
        
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
                                   !IsDirectionWalkableForAI(enemyArrayPos, currentPatrolDirection) ||
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
            if (IsDirectionWalkableForAI(enemyArrayPos, directions[i]))
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

    // IMPORTANTE: Nuovo metodo che usa il MapManager per controllare se l'AI può camminare
    bool IsDirectionWalkableForAI(Vector2Int fromArrayPos, Vector2 direction)
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

        // IMPORTANTE: Usa il nuovo metodo specifico per AI (solo corridoi)
        return mapManager.IsWalkableForAI(targetArrayPos);
    }

    void HandleMovement()
    {
        // Non muoversi se è in rinculo o morto
        if (isKnockedBack || isDead) 
        {
            animator.SetBool("isMoving", false);
            return;
        }

        if (!isMoving && !isAttacking)
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
            // Se viene applicato un rinculo durante il movimento, ferma il movimento
            if (isKnockedBack)
            {
                isMoving = false;
                yield break;
            }
            
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;

        isMoving = false;
    }

    // IMPORTANTE: Aggiornato per usare il nuovo sistema MapManager
    public bool IsWalkable(Vector3 targetPos)
    {
        if (mapManager != null && mapManager.wallCalculated)
        {
            // Usa il nuovo sistema del MapManager specifico per AI
            Vector2Int arrayPos = mapManager.WorldToArrayCoordinates(targetPos);
            if (!mapManager.IsValidArrayCoordinate(arrayPos))
                return false;
                
            // I nemici possono camminare solo sui corridoi
            return mapManager.IsWalkableForAI(arrayPos);
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

    void HandleAttack()
    {
        if (isKnockedBack || isDead)
        {
            return;
        }

        if (closeToPlayer && canAttack && !isAttacking)
        {
            StartCoroutine(DamagePlayer());
        }
    }

    // FIXED: Corretto il bug del cooldown dell'attacco
    IEnumerator DamagePlayer()
    {
        isAttacking = true;
        canAttack = false;
        isMoving = false;

        // Trova il player e applica danno
        PlayerController pc = playerTransform.gameObject.GetComponent<PlayerController>();
        if (pc != null && pc.IsAlive())
        {
            pc.TakeDamage(attackDamage);
            
            if (enableDebug)
            {
                Debug.Log($"{gameObject.name} ha attaccato il player per {attackDamage} danni!");
            }
        }

        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true; // FIXED: Era impostato su false
    }

    // Visualizza informazioni nell'Inspector durante il gioco
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !enableDebug) return;

        // Disegna un cerchio colorato per indicare la modalità 
        if (isDead)
        {
            Gizmos.color = Color.black; // Morto
        }
        else if (isKnockedBack)
        {
            Gizmos.color = Color.magenta; // In rinculo
        }
        else if (IsInIntelligentMode())
        {
            Gizmos.color = Color.red; // Modalità inseguimento intelligente
        }
        else
        {
            Gizmos.color = Color.yellow; // Modalità patrol
        }
        
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        // Mostra la direzione del movimento
        if (targetDirection != Vector2.zero && !isDead)
        {
            Gizmos.color = Color.blue;
            Vector3 targetPos = transform.position + (Vector3)targetDirection;
            Gizmos.DrawLine(transform.position, targetPos);
        }
        
        // Mostra la direzione del rinculo
        if (isKnockedBack)
        {
            Gizmos.color = Color.cyan;
            Vector3 knockbackPos = transform.position + (Vector3)knockbackDirection;
            Gizmos.DrawLine(transform.position, knockbackPos);
        }
        
        // Mostra se può attaccare
        if (closeToPlayer && canAttack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
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