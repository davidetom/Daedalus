using UnityEngine;

public class PlayerColliderFix : MonoBehaviour
{
    [Header("Player Settings")]
    public float playerSpeed = 5f;
    public bool usePhysicsMovement = true;
    
    [Header("Collider Settings")]
    public bool autoFixColliders = true;
    
    private Rigidbody2D rb;
    private Collider2D playerCollider;
    
    void Start()
    {
        if (autoFixColliders)
        {
            FixPlayerColliders();
        }
        
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
    }
    
    [ContextMenu("Fix Player Colliders")]
    public void FixPlayerColliders()
    {
        Debug.Log("Fixing player colliders...");
        
        // 1. Assicurati che ci sia un Rigidbody2D configurato correttamente
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            Debug.Log("Aggiunto Rigidbody2D al player");
        }
        
        // Configurazione corretta del Rigidbody2D
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f; // Per un gioco 2D top-down
        rb.linearDamping = 5f; // Per fermare il player quando non si muove
        rb.angularDamping = 5f;
        rb.freezeRotation = true; // Impedisce al player di ruotare
        
        Debug.Log("Rigidbody2D configurato");
        
        // 2. Assicurati che ci sia un collider appropriato
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            // Aggiungi un CircleCollider2D (spesso migliore per il movimento)
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = 0.4f; // Regola in base alla dimensione del player
            collider = circleCollider;
            Debug.Log("Aggiunto CircleCollider2D al player");
        }
        
        // Assicurati che il collider NON sia un trigger
        collider.isTrigger = false;
        
        Debug.Log($"Player collider: {collider.GetType().Name}, isTrigger: {collider.isTrigger}");
        
        // 3. Rimuovi eventuali CompositeCollider2D dal player (non dovrebbe averlo)
        CompositeCollider2D composite = GetComponent<CompositeCollider2D>();
        if (composite != null)
        {
            Debug.LogWarning("Rimosso CompositeCollider2D dal player - non dovrebbe averlo!");
            DestroyImmediate(composite);
        }
        
        // 4. Controlla il layer
        if (gameObject.layer == 0)
        {
            gameObject.layer = LayerMask.NameToLayer("Default");
        }
        
        Debug.Log($"Player layer: {LayerMask.LayerToName(gameObject.layer)}");
        Debug.Log("Player colliders fixed!");
    }
    
    void Update()
    {
        if (usePhysicsMovement && rb != null)
        {
            HandlePhysicsMovement();
        }
        else
        {
            HandleTransformMovement();
        }
    }
    
    private void HandlePhysicsMovement()
    {
        // Movimento basato su fisica (raccomandato per collisioni)
        Vector2 movement = Vector2.zero;
        
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            movement.y = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            movement.y = -1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            movement.x = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            movement.x = 1f;
        
        // Normalizza il movimento diagonale
        movement = movement.normalized;
        
        // Applica la velocità
        rb.linearVelocity = movement * playerSpeed;
    }
    
    private void HandleTransformMovement()
    {
        // Movimento basato su transform (meno raccomandato per collisioni)
        Vector3 movement = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            movement.y = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            movement.y = -1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            movement.x = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            movement.x = 1f;
        
        // Applica il movimento
        transform.Translate(movement.normalized * playerSpeed * Time.deltaTime);
    }
    
    // Test di debug
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Player collision with: {collision.gameObject.name}");
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Player trigger with: {other.name}");
    }
    
    [ContextMenu("Test Movement")]
    public void TestMovement()
    {
        Debug.Log("Testing movement - press WASD to move");
        Debug.Log($"Physics movement: {usePhysicsMovement}");
        Debug.Log($"Player speed: {playerSpeed}");
    }
    
    // Funzione per testare se il player può muoversi in una direzione
    private bool CanMoveTo(Vector2 direction)
    {
        if (playerCollider == null) return true;
        
        float distance = 0.1f; // Piccola distanza per il test
        RaycastHit2D hit = Physics2D.BoxCast(
            playerCollider.bounds.center,
            playerCollider.bounds.size,
            0f,
            direction,
            distance
        );
        
        return hit.collider == null;
    }
}