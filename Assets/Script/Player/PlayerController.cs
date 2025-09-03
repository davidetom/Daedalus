using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems; // serve per i pulsanti mobile

public class PlayerController : MonoBehaviour
{
    private Animator animator;

    [Header("Enable Debug")]
    public bool enableDebug = false;

    [Header("Tilemap Reference")]
    public Tilemap tilemap;
    public TileBase muraTile;

    [Header("Hub Tilemap References")]
    public Tilemap hubBackgroundTilemap;
    public Tilemap hubSolidObjectsBaseTilemap;
    public Tilemap hubSolidObjectsTilemap;

    [Header("Hub Controller Reference")]
    public OuterHubController outerHubController; // Per verificare se siamo nell'hub
    public InnerHubController innerHubController;

    [Header("Map Reference")]
    public MapManager mapManager; // Riferimento al MapManager
    public Vector3 startPos;

    [Header("Cycle Management")]
    public DayNightCycleManager dayNightCycleManager;
    public MazeManager mazeManager;

    [Header("Move Settings")]
    public float moveSpeed;
    public bool isMoving = false;

    [Header("Door Interaction")]
    public float interactRange = 1f; // distanza massima per interagire con la porta
    public KeyCode interactKey = KeyCode.E;
    public bool hasKey = false; //all'inizio non ha la chiave

    [Header("Attack Settings")]
    public float attackDamage = 25f;
    public float attackRange = 2f;
    public float recoil = 1f;
    public float attackCooldown = 0.3f;
    private bool attackAnimationFinished = false;
    private Vector2 lastDirection = Vector2.down;
    public bool isAttacking = false;
    public bool canAttack = true;
    public bool isNightTime = false; // il player può attaccare solo di notte
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
    public float invincibilityDuration = 0.5f; // NUOVO: Durata dell'invincibilità
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool takingDamage = false;
    private bool isInvincible = false; // NUOVO: Stato di invincibilità
    private Coroutine currentDamageFeedbackCoroutine = null;
    private Coroutine currentInvincibilityCoroutine = null; // NUOVO: Coroutine per invincibilità

    [Header("Health Settings")]
    public float maxHealthPoints = 100f;
    [SerializeField] private float currentHealthPoints;
    public bool isDead = false;

    [Header("Coins and Gems")]
    public int maxCoinNumber = 9999;
    public int coinsPicked = 0;

    //GAMEOVER UI
    [Header("Defeat UI")]
    [SerializeField] private GameObject gameUICanvas;
    [SerializeField] private GameObject gameButtons;
    [SerializeField] private GameObject gameOverCanvas;

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

        InitializeHubTilemapReferences();
        
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
        if (mapManager != null && mapManager.wallCalculated && mapManager.Distances[0, 0] == 0 && !HasCalculatedDistances())
        {
            CalcoloDistanze();
        }

        HandleMovement();

        // Controlla input per aprire la porta
        if (Input.GetKeyDown(interactKey))
            KeyBoardKeyAzione();
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
        // MODIFICA: Rimosso il controllo !takingDamage - ora può muoversi anche durante il danno
        if (!isMoving && (!isAttacking || canAttackWhileMoving) && !isDead)
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
            // MODIFICA: Rimosso il controllo takingDamage - ferma solo se muore
            if (isDead)
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
    
    public void SnapToNearestGridPosition()
    {
        Vector3 currentPos = transform.position;
        
        // Calcola la posizione della griglia più vicina (assumendo griglia 1x1 con centro a 0.5, 0.5)
        float snappedX = Mathf.Round(currentPos.x - 0.5f) + 0.5f;
        float snappedY = Mathf.Round(currentPos.y - 0.7f) + 0.7f;
        
        Vector3 snappedPosition = new Vector3(snappedX, snappedY, currentPos.z);
        
        // Applica la correzione
        transform.position = snappedPosition;
        
        //Debug.Log($"Player disallineato corretto da {currentPos} a {snappedPosition}");
    }

    public bool IsWalkable(Vector3 targetPos)
    {
        if (IsPlayerInHub())
        {
            return IsWalkableInHub(targetPos);
        }

        if (mapManager != null && mapManager.wallCalculated)
        {
            Vector2Int arrayPos = mapManager.WorldToArrayCoordinates(targetPos);
            if (!mapManager.IsValidArrayCoordinate(arrayPos))
                return false;

            if (!mapManager.IsWalkableForPlayer(arrayPos))
                return false;
        }
        else
        {
            // Fallback al sistema originale se MapManager non è disponibile
            if (tilemap == null)
            {
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

        // NUOVO: Verifica se c'è una porta chiusa in quella cella
        DoorController[] doors = GameObject.FindObjectsByType<DoorController>(FindObjectsSortMode.None);
        foreach (DoorController door in doors)
        {
            // Controlla se la porta è nella stessa posizione della tile target
            Vector3 doorPos = door.transform.position;
            float distance = Vector3.Distance(doorPos, targetPos);
            
            // Se la porta è molto vicina alla posizione target (stessa tile)
            if (distance < 0.5f)
            {
                // Se la porta è chiusa, la tile non è percorribile
                if (!door.IsOpen())
                {
                    return false;
                }
            }
        }

        // NUOVO: Verifica se c'è un edificio (Building) in quella posizione
        Collider2D[] buildingColliders = Physics2D.OverlapPointAll(targetPos);
        foreach (Collider2D collider in buildingColliders)
        {
            if (collider.CompareTag("Building"))
            {
                if (enableDebug)
                {
                    Debug.Log($"Movimento bloccato: edificio rilevato in {targetPos}");
                }
                return false;
            }
        }

        return true;
    }
    
    private bool IsWalkableInHub(Vector3 targetPos)
    {
        // USA IL METODO CORRETTO DELLA TILEMAP PER LA CONVERSIONE
        Vector3Int cellPosition = hubBackgroundTilemap.WorldToCell(targetPos);
        
        // Controllo 1: Deve esistere un tile sulla tilemap Background per essere camminabile
        bool hasBackgroundTile = false;
        if (hubBackgroundTilemap != null)
        {
            TileBase backgroundTile = hubBackgroundTilemap.GetTile(cellPosition);
            hasBackgroundTile = backgroundTile != null;
            
            if (enableDebug)
            {
                Debug.Log($"Hub: Pos world {targetPos} -> Pos cella {cellPosition} -> Tile: {(backgroundTile != null ? backgroundTile.name : "NULL")}");
            }
        }
        
        if (!hasBackgroundTile)
        {
            if (enableDebug)
            {
                Debug.Log($"Hub: Nessun tile background alla cella {cellPosition} (world: {targetPos})");
            }
            return false;
        }
        
        // Controllo 2: NON deve esserci un tile su SolidObjectsBase
        if (hubSolidObjectsBaseTilemap != null)
        {
            TileBase solidBaseTile = hubSolidObjectsBaseTilemap.GetTile(cellPosition);
            if (solidBaseTile != null)
            {
                if (enableDebug)
                {
                    Debug.Log($"Hub: Tile solido base trovato alla cella {cellPosition}");
                }
                return false;
            }
        }
        
        // Controllo 3: NON deve esserci un tile su SolidObjects
        if (hubSolidObjectsTilemap != null)
        {
            TileBase solidTile = hubSolidObjectsTilemap.GetTile(cellPosition);
            if (solidTile != null)
            {
                if (enableDebug)
                {
                    Debug.Log($"Hub: Tile solido trovato alla cella {cellPosition}");
                }
                return false;
            }
        }
        
        if (enableDebug)
        {
            Debug.Log($"Hub: Posizione {targetPos} (cella {cellPosition}) è camminabile");
        }
        
        return true;
    }

    private bool IsPlayerInHub()
    {
        // Metodo 1: Usa HubController se disponibile
        if (outerHubController != null)
        {
            return outerHubController.IsPlayerInHub();
        }
        
        // Metodo 2: Fallback - controlla coordinate
        Vector3 hubCenter = new Vector3(405f, 160f, 0f);
        float distanceFromHub = Vector3.Distance(transform.position, hubCenter);
        return distanceFromHub < 20f; // Raggio arbitrario
    }

    void InitializeHubTilemapReferences()
    {
        // Trova automaticamente le tilemap dell'hub se non assegnate
        if (hubBackgroundTilemap == null)
        {
            GameObject backgroundObj = GameObject.Find("BackGround");
            if (backgroundObj != null)
            {
                hubBackgroundTilemap = backgroundObj.GetComponent<Tilemap>();
                if (enableDebug && hubBackgroundTilemap != null)
                {
                    Debug.Log("Hub BackGround tilemap trovata automaticamente");
                }
            }
        }

        if (hubSolidObjectsBaseTilemap == null)
        {
            GameObject solidBaseObj = GameObject.Find("SolidObjectsBase");
            if (solidBaseObj != null)
            {
                hubSolidObjectsBaseTilemap = solidBaseObj.GetComponent<Tilemap>();
                if (enableDebug && hubSolidObjectsBaseTilemap != null)
                {
                    Debug.Log("Hub SolidObjectsBase tilemap trovata automaticamente");
                }
            }
        }

        if (hubSolidObjectsTilemap == null)
        {
            GameObject solidObj = GameObject.Find("SolidObjects");
            if (solidObj != null)
            {
                hubSolidObjectsTilemap = solidObj.GetComponent<Tilemap>();
                if (enableDebug && hubSolidObjectsTilemap != null)
                {
                    Debug.Log("Hub SolidObjects tilemap trovata automaticamente");
                }
            }
        }

        // Trova HubController se non assegnati
        if (outerHubController == null)
        {
            outerHubController = FindFirstObjectByType<OuterHubController>();
            if (enableDebug && outerHubController != null)
            {
                Debug.Log("OuterHubController trovato automaticamente");
            }
        }
        
        if (innerHubController == null)
        {
            innerHubController = FindFirstObjectByType<InnerHubController>();
            if (enableDebug && innerHubController != null)
            {
                Debug.Log("InnerHubController trovato automaticamente");
            }
        }
    }

    public void HandleAttack()
    {
        // MODIFICA: Rimosso il controllo !takingDamage - può attaccare anche durante il danno
        if ((!isMoving || canAttackWhileMoving) && !isAttacking && canAttack && !isDead)
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
                    //Debug.Log($"Nemico trovato sulla stessa tile del player: {collider.gameObject.name}");
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
            
            //Debug.Log($"Attaccato {enemy.name} per {attackDamage} danni! Vita rimanente: {enemyLogic.GetCurrentHealth()}");
            
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
            //Debug.LogWarning($"Il nemico {enemy.name} non ha il componente EnemyLogic o è già morto!");
        }
    }

    // In PlayerController.cs, modifica il metodo TakeDamage

    public void TakeDamage(float damage)
    {
        // Se è invincibile o morto, ignora il danno
        if (isDead || isInvincible)
        {
            if (isInvincible && enableDebug)
            {
                Debug.Log("Danno bloccato: player invincibile");
            }
            return;
        }

        Debug.Log($"Player riceve {damage} danni. Vita prima: {currentHealthPoints}");
        
        currentHealthPoints -= damage;
        currentHealthPoints = Mathf.Max(0, currentHealthPoints);

        Debug.Log($"Vita dopo danno: {currentHealthPoints}");

        // MODIFICA: Rimossa la gestione del movimento durante il danno
        // Il player può continuare a muoversi liberamente

        // Avvia l'invincibilità prima del feedback visivo
        StartInvincibility();

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
        
        // Avvia il nuovo feedback solo se non è morto
        if (!isDead)
        {
            currentDamageFeedbackCoroutine = StartCoroutine(DamageFeedbackCoroutine());
        }
    }
    
    // NUOVO METODO: Coroutine per il lampeggiamento del player
    IEnumerator DamageFeedbackCoroutine()
    {
        if (spriteRenderer == null || isDead) 
        {
            currentDamageFeedbackCoroutine = null;
            yield break;
        }
        
        takingDamage = true; // Mantieni la flag per il feedback visivo
        
        // Lampeggiamento durante l'invincibilità
        float totalDuration = Mathf.Max(damageFeedbackDuration, invincibilityDuration);
        float flashInterval = 0.1f; // Lampeggia ogni 0.1 secondi
        float elapsed = 0f;
        
        bool isVisible = true;
        
        while (elapsed < totalDuration && !isDead)
        {
            // Cambia visibilità 
            if (isInvincible)
            {
                // Durante l'invincibilità: lampeggiamento alpha
                Color currentColor = originalColor;
                currentColor.a = isVisible ? 0.3f : 1f;
                spriteRenderer.color = currentColor;
            }
            else
            {
                // Feedback normale del danno (primo momento)
                spriteRenderer.color = Color.red;
            }
            
            isVisible = !isVisible;
            
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }
        
        // Ripristina il colore originale
        if (!isDead && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        takingDamage = false;
        currentDamageFeedbackCoroutine = null;
    }

    void StartInvincibility()
    {
        // Ferma l'invincibilità precedente se esiste
        if (currentInvincibilityCoroutine != null)
        {
            StopCoroutine(currentInvincibilityCoroutine);
            currentInvincibilityCoroutine = null;
        }
        
        // Avvia la nuova invincibilità
        currentInvincibilityCoroutine = StartCoroutine(InvincibilityCoroutine());
    }

    IEnumerator InvincibilityCoroutine()
    {
        if (isDead)
        {
            currentInvincibilityCoroutine = null;
            yield break;
        }
        
        isInvincible = true;
        
        if (enableDebug)
        {
            Debug.Log($"Player invincibile per {invincibilityDuration} secondi");
        }
        
        yield return new WaitForSeconds(invincibilityDuration);
        
        // Fine dell'invincibilità
        if (!isDead)
        {
            isInvincible = false;
            
            if (enableDebug)
            {
                Debug.Log("Invincibilità terminata");
            }
        }
        
        currentInvincibilityCoroutine = null;
    }

    void Die()
    {
        if (isDead) return;

        Debug.Log("Player morto!");
        isDead = true;
        
        // NUOVO: Ferma anche l'invincibilità
        if (currentInvincibilityCoroutine != null)
        {
            StopCoroutine(currentInvincibilityCoroutine);
            currentInvincibilityCoroutine = null;
        }
        isInvincible = false;
        
        // Ferma il feedback del danno se attivo
        if (currentDamageFeedbackCoroutine != null)
        {
            StopCoroutine(currentDamageFeedbackCoroutine);
            currentDamageFeedbackCoroutine = null;
        }
        takingDamage = false;
        
        // NUOVO: Ferma anche il ciclo giorno/notte
        if (dayNightCycleManager != null)
        {
            dayNightCycleManager.PauseSystem();
        }
        
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

        // Logica il death screen
        yield return new WaitForSeconds(0.2f);
        ShowGameOver();
    }

    //UI per il processo di Respawn
    public void ShowGameOver()
    {
        //FERMA IL GIOCO
        Time.timeScale = 0f;

        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
        }

        if (gameButtons != null)
        {
            gameButtons.SetActive(false);
        }

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }
    }

    //Dopo respawn riattiva tutta la UI
    public void ShowRespawn()
    {
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
        }

        if (gameButtons != null)
        {
            gameButtons.SetActive(true);
        }

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(false);
        }

        //Riprendi gioco
        Time.timeScale = 1f;
    }

    public void InizializeSettings()
    {
        Debug.Log("Reinizializzazione player...");

        isDead = false;
        currentHealthPoints = maxHealthPoints;
        isMoving = false;
        isAttacking = false;
        canAttack = true;
        takingDamage = false;
        isInvincible = false; // NUOVO: Reset invincibilità

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
        
        // NUOVO: Reset coroutine invincibilità
        if (currentInvincibilityCoroutine != null)
        {
            StopCoroutine(currentInvincibilityCoroutine);
            currentInvincibilityCoroutine = null;
        }

        animator.SetBool("isAttacking", false);
        animator.SetBool("isMoving", false);
        transform.position = startPos;

        // NUOVO: Cambia labirinto prima di riavviare il giorno
        if (mazeManager != null)
        {
            mazeManager.ChangeToNextMaze();
        }

        // NUOVO: Riavvia il ciclo giorno/notte dall'inizio del giorno
        if (dayNightCycleManager != null)
        {
            dayNightCycleManager.ResetToDay();
        }

        ResetAllEnemiesState();

        InvalidateDistances();
        StartCoroutine(RecalculateDistancesNextFrame());
        ShowRespawn();
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
        
        //Debug.Log($"Reset effettuato su {allEnemies.Length} nemici");
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

    public float Heal(float healAmount)
    {
        if (isDead)
        {
            //Debug.Log("Non si può curare un player morto");
            return 0f;
        }

        float previousHealth = currentHealthPoints;
        currentHealthPoints += healAmount;
        currentHealthPoints = Mathf.Min(currentHealthPoints, maxHealthPoints);
        
        float actualHealAmount = currentHealthPoints - previousHealth;
        
        //Debug.Log($"Player curato di {actualHealAmount} HP. Vita attuale: {currentHealthPoints}/{maxHealthPoints}");
        
        return actualHealAmount;
    }

    public void FullHeal()
    {
        if (isDead)
        {
            //Debug.Log("Non si può curare un player morto");
            return;
        }

        currentHealthPoints = maxHealthPoints;
        //Debug.Log("Player completamente curato!");
    }

    void TryOpenNearbyDoor()
    {
        DoorController[] doors = GameObject.FindObjectsByType<DoorController>(FindObjectsSortMode.None);

        foreach (var door in doors)
        {
            float distance = Vector3.Distance(transform.position, door.transform.position);
            if (distance <= interactRange)
            {
                // NUOVO: Solo le outer doors possono essere aperte dal player
                if (door.IsOuterDoor())
                {
                    door.TryOpen(this);
                }
                else if (door.IsInnerDoor())
                {
                    Debug.Log("Le porte interne si aprono automaticamente!");
                }
                break;
            }
        }
    }

    // CALCOLO DISTANZE PER I NEMICI - MODIFICATO PER USARE SOLO CORRIDOI
    void CalcoloDistanze()
    {
        if (mapManager == null || !mapManager.wallCalculated)
        {
            //Debug.LogWarning("MapManager non disponibile o non inizializzato!");
            return;
        }

        Vector2Int playerArrayPos = mapManager.WorldToArrayCoordinates(transform.position);
        
        if (!mapManager.IsValidArrayCoordinate(playerArrayPos))
        {
            //Debug.LogWarning($"Posizione player fuori dai bounds della mappa: {playerArrayPos}");
            return;
        }

        // IMPORTANTE: Usa il nuovo BFS che considera solo i corridoi
        BFS_CorridorsOnly(playerArrayPos, mapManager.Distances, mapManager.TileTypes);
    }

    /// <summary>
    /// Metodo pubblico per ricalcolare le distanze BFS.
    /// Chiamato dal MazeManager quando cambia la tilemap.
    /// </summary>
    public void RecalculateDistances()
    {
        CalcoloDistanze();
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

    // Aggiungi questo metodo pubblico alla fine della classe PlayerController

    /// <summary>
    /// Trasporta il player in modo sicuro alla posizione specificata, 
    /// fermando tutti i movimenti e correggendo la posizione sulla griglia.
    /// </summary>
    public void SafeTransportTo(Vector3 targetPosition)
    {
        // Ferma tutte le coroutine attive (inclusa Move())
        StopAllCoroutines();
        
        // Reset completo dello stato di movimento
        isMoving = false;
        isAttacking = false;
        
        // Reset dell'input mobile
        StopMovimento();
        
        // Reset delle animazioni
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetBool("isAttacking", false);
            animator.SetFloat("moveX", 0);
            animator.SetFloat("moveY", 0);
        }
        
        // Imposta la nuova posizione
        transform.position = targetPosition;
        
        // Correggi la posizione sulla griglia
        SnapToNearestGridPosition();
        
        // Ricalcola le distanze BFS
        CalcoloDistanze();
        
        Debug.Log($"Player trasportato in modo sicuro a: {transform.position}");
    }
    
    // Metodi pubblici per accesso esterno allo stato del player
    public float GetCurrentHealth() => currentHealthPoints;
    public float GetMaxHealth() => maxHealthPoints;
    public float GetHealthPercentage() => currentHealthPoints / maxHealthPoints;
    public bool IsAlive() => !isDead;
    public bool IsTakingDamage() => takingDamage;
    public bool IsInvincible() => isInvincible;
    public bool InHub => IsPlayerInHub();

    // ----------------- Metodi per pulsanti mobile -----------------
    public void MuoviSu() => mobileInput = Vector2.up;
    public void MuoviGiu() => mobileInput = Vector2.down;
    public void MuoviDestra() => mobileInput = Vector2.right;
    public void MuoviSinistra() => mobileInput = Vector2.left;
    public void StopMovimento() => mobileInput = Vector2.zero;

    public void PulsanteAzione()
    {
        // Prima controlla se siamo nell'inner hub (la priorità più alta)
        if (IsPlayerInHub())                                        // se il player è nell'inner hub
        {
            if (innerHubController != null && innerHubController.IsPlayerInExitPoint)         
            {
                innerHubController.ExitHub();
            }
            // Nell'inner hub NON si può attaccare, quindi non aggiungiamo altre azioni
            return;
        }
        
        // Se siamo nell'outer hub (ma non nell'inner hub)
        if (mazeManager.IsPlayerInHub)                              // se il player è nell'outer hub
        {
            if (outerHubController != null && outerHubController.IsPlayerInEnterPoint)        
            {
                outerHubController.EnterHub();
            }
            // Nell'outer hub NON si può attaccare, quindi non aggiungiamo altre azioni
            return;
        }
        
        // Se siamo nel labirinto (fuori da entrambi gli hub)
        if (isNightTime)                                            // se è notte
        {
            HandleAttack();
        }
        else                                                        // se è giorno
        {
            TryOpenNearbyDoor();
        }
    }

    public void KeyBoardKeyAzione()
    {
        // Prima controlla se siamo nell'inner hub (la priorità più alta)
        if (IsPlayerInHub())                                        // se il player è nell'inner hub
        {
            if (innerHubController != null && innerHubController.IsPlayerInExitPoint)         
            {
                innerHubController.ExitHub();
            }
            // Nell'inner hub NON si può attaccare, quindi non aggiungiamo altre azioni
            return;
        }
        
        // Se siamo nell'outer hub (ma non nell'inner hub)
        if (mazeManager.IsPlayerInHub)                              // se il player è nell'outer hub
        {
            if (outerHubController != null && outerHubController.IsPlayerInEnterPoint)        
            {
                outerHubController.EnterHub();
            }
            // Nell'outer hub NON si può attaccare, quindi non aggiungiamo altre azioni
            return;
        }
        
        // Se siamo nel labirinto (fuori da entrambi gli hub)
        if (isNightTime)                                            // se è notte
        {
            HandleAttack();
        }
        else                                                        // se è giorno
        {
            TryOpenNearbyDoor();
        }
    }

    //FOR SAVE AND LOAD DATA
    #region Save and Load

    public void Save(ref PlayerSaveData data)
    {
        data.Position = transform.position;
        data.HealthPoints = currentHealthPoints;
    }

    public void Load(PlayerSaveData data)
    {
        transform.position = data.Position;
        this.currentHealthPoints = data.HealthPoints;
    }

    #endregion
}

//Struct per i dati da salvare (per ora solo la posizione)
[System.Serializable]
public struct PlayerSaveData
{
    public Vector3 Position;
    public float HealthPoints;
}