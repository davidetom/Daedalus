using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class AttackJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("UI Components")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public Image handleImage; // Per cambiare il colore durante drag
    public Image backgroundImage; // Riferimento al background
    
    [Header("Settings")]
    public float maxDistance = 80f;
    public float deadZone = 0.4f; // Soglia per distinguere tap da drag
    public float tapTime = 0.3f; // Tempo massimo per considerarlo un tap
    
    [Header("Visual Feedback")]
    public Color normalColor = new Color(0.8f, 0.8f, 0.9f, 0.7f); // Grigio-blu traslucido
    public Color dragColor = new Color(1f, 0.8f, 0.2f, 0.9f); // Arancione dorato
    public Color attackColor = new Color(1f, 0.3f, 0.3f, 1f); // Rosso intenso
    public Color disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f); // Grigio scuro
    
    [Header("Background Visual")]
    public Color backgroundNormalColor = new Color(0.2f, 0.2f, 0.3f, 0.6f); // Scuro traslucido
    public Color backgroundActiveColor = new Color(0.3f, 0.1f, 0.1f, 0.8f); // Rosso scuro per attacco
    
    [Header("Animation Settings")]
    public float scaleOnPress = 0.9f;
    public float scaleAnimationSpeed = 10f;
    public bool enablePulseAnimation = true;
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.1f;
    
    [Header("Player Reference")]
    public PlayerController playerController;
    
    [Header("Preview Settings")]
    public LineRenderer attackPreview;
    public bool showPreview = true;
    
    private Vector2 startPosition;
    private Vector2 currentPosition;
    private bool isDragging = false;
    private float pressTime;
    private Vector2 joystickCenter;
    private Vector2 attackDirection = Vector2.zero;
    private Vector3 originalHandleScale;
    private Vector3 originalBackgroundScale;
    private Coroutine pulseCoroutine;
    
    void Start()
    {
        joystickCenter = joystickBackground.position;
        
        // Salva le scale originali per le animazioni
        if (joystickHandle != null)
        {
            originalHandleScale = joystickHandle.localScale;
        }
        if (joystickBackground != null)
        {
            originalBackgroundScale = joystickBackground.localScale;
        }
        
        // Setup iniziale del preview
        if (attackPreview != null)
        {
            attackPreview.enabled = false;
            attackPreview.positionCount = 2;
            attackPreview.startWidth = 0.1f;
            attackPreview.endWidth = 0.05f;
            attackPreview.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        // Setup colori iniziali
        if (handleImage != null)
        {
            handleImage.color = normalColor;
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundNormalColor;
        }
        
        // Setup stile del joystick
        SetupJoystickStyle();
        
        // Avvia animazione di pulse se abilitata
        if (enablePulseAnimation)
        {
            pulseCoroutine = StartCoroutine(PulseAnimation());
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = false;
        pressTime = Time.time;
        startPosition = eventData.position;
        currentPosition = eventData.position;
        attackDirection = Vector2.zero;
        
        // Animazione di press
        if (joystickHandle != null)
        {
            StartCoroutine(ScaleAnimation(joystickHandle, originalHandleScale * scaleOnPress, scaleAnimationSpeed));
        }
        
        // Ferma animazione di pulse
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // Feedback visivo immediato
        if (handleImage != null)
        {
            handleImage.color = dragColor;
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundActiveColor;
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        currentPosition = eventData.position;
        
        Vector2 direction = currentPosition - startPosition;
        float distance = direction.magnitude;
        
        // Se superiamo la dead zone, iniziamo il dragging
        if (distance > deadZone * maxDistance)
        {
            isDragging = true;
            
            // Aggiorna la posizione del joystick handle
            Vector2 clampedDirection = Vector2.ClampMagnitude(direction, maxDistance);
            joystickHandle.position = joystickCenter + clampedDirection;
            
            // Calcola la direzione dell'attacco (convertita per il gioco)
            Vector2 normalizedDirection = clampedDirection.normalized;
            attackDirection = ConvertScreenToGameDirection(normalizedDirection);
            
            // Mostra preview dell'attacco se il player può attaccare
            if (showPreview && CanPlayerAttack())
            {
                ShowAttackPreview(attackDirection);
            }
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        float releaseTime = Time.time - pressTime;
        Vector2 direction = currentPosition - startPosition;
        
        // Ripristina la scala originale del handle
        if (joystickHandle != null)
        {
            StartCoroutine(ScaleAnimation(joystickHandle, originalHandleScale, scaleAnimationSpeed));
        }
        
        // Feedback visivo di attacco
        if (handleImage != null)
        {
            StartCoroutine(AttackFeedback());
        }
        
        // Reset visual del joystick
        joystickHandle.position = joystickCenter;
        HideAttackPreview();
        
        if (isDragging && direction.magnitude > deadZone * maxDistance)
        {
            // È stato un drag - esegui attacco direzionale
            ExecuteDirectionalAttack(attackDirection);
        }
        else if (releaseTime < tapTime)
        {
            // È stato un tap - esegui la logica del PulsanteAzione esistente
            ExecuteAction();
        }
        
        // Reset background
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundNormalColor;
        }
        
        // Riavvia animazione di pulse
        if (enablePulseAnimation && pulseCoroutine == null)
        {
            pulseCoroutine = StartCoroutine(PulseAnimation());
        }
        
        isDragging = false;
        attackDirection = Vector2.zero;
    }
    
    /// <summary>
    /// Converte la direzione dello schermo in direzione di gioco (solo cardinali)
    /// </summary>
    private Vector2 ConvertScreenToGameDirection(Vector2 screenDirection)
    {
        // Determina la direzione cardinale più vicina
        float absX = Mathf.Abs(screenDirection.x);
        float absY = Mathf.Abs(screenDirection.y);
        
        if (absX > absY)
        {
            // Movimento orizzontale
            return screenDirection.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            // Movimento verticale (CORRETTO: Y positivo nel drag = su nel gioco)
            return screenDirection.y > 0 ? Vector2.up : Vector2.down;
        }
    }
    
    private void ExecuteDirectionalAttack(Vector2 direction)
    {
        if (playerController != null && CanPlayerAttack())
        {
            // Crea un metodo pubblico nel PlayerController per impostare la direzione
            playerController.SetAttackDirection(direction);
            
            // Esegue l'attacco usando il metodo esistente
            playerController.HandleAttack();
        }
    }
    
    private void ExecuteAction()
    {
        if (playerController != null)
        {
            // Usa il metodo esistente del PlayerController per le interazioni
            playerController.PulsanteAzione();
        }
    }
    
    private bool CanPlayerAttack()
    {
        if (playerController == null) return false;
        
        // IMPORTANTE: Usa la stessa logica del PlayerController.PulsanteAzione()
        // Non può attaccare se è nell'inner hub (dentro casa)
        if (playerController.InInnerHub) // Se è nell'inner hub
        {
            return false;
        }
        
        // Non può attaccare se è nell'outer hub
        if (playerController.mazeManager != null && playerController.mazeManager.IsPlayerInOuterHub)
        {
            return false;
        }
        
        // Può attaccare solo nel labirinto, di notte, con le altre condizioni
        return playerController.isNightTime && 
               playerController.canAttack && 
               !playerController.isDead &&
               (!playerController.isMoving || playerController.canAttackWhileMoving) &&
               !playerController.isAttacking;
    }
    
    private void ShowAttackPreview(Vector2 direction)
    {
        if (attackPreview == null || playerController == null) return;
        
        // Calcola posizione di inizio e fine dell'attacco
        Vector3 startPos = playerController.transform.position;
        Vector3 endPos = startPos + new Vector3(direction.x, direction.y, 0) * playerController.attackRange;
        
        // Imposta le posizioni del LineRenderer
        attackPreview.SetPosition(0, startPos);
        attackPreview.SetPosition(1, endPos);
        attackPreview.enabled = true;
        
        // Cambia colore in base al fatto che possa attaccare o meno
        Color previewColor = CanPlayerAttack() ? Color.red : Color.gray;
        previewColor.a = 0.7f;
        attackPreview.startColor = previewColor;
        attackPreview.endColor = previewColor;
    }
    
    private void HideAttackPreview()
    {
        if (attackPreview != null)
        {
            attackPreview.enabled = false;
        }
    }
    
    private IEnumerator AttackFeedback()
    {
        if (handleImage == null) yield break;
        
        // Flash rosso per feedback dell'attacco
        handleImage.color = attackColor;
        yield return new WaitForSeconds(0.1f);
        
        // Torna al colore normale
        handleImage.color = normalColor;
    }
    
    void Update()
    {
        // Aggiorna il colore del handle in base allo stato del player
        if (handleImage != null && !isDragging)
        {
            if (CanPlayerAttack())
            {
                handleImage.color = normalColor;
            }
            else
            {
                // Grigio se non può attaccare
                handleImage.color = disabledColor;
            }
        }
        
        // Aggiorna il preview durante il drag
        if (isDragging && showPreview && attackDirection != Vector2.zero)
        {
            ShowAttackPreview(attackDirection);
        }
    }
    
    /// <summary>
    /// Setup dello stile del joystick con bordi arrotondati e ombre
    /// </summary>
    private void SetupJoystickStyle()
    {
        // Setup background
        if (backgroundImage != null)
        {
            // Crea uno sprite circolare se non esiste
            if (backgroundImage.sprite == null)
            {
                backgroundImage.sprite = CreateCircleSprite(128, new Color(1, 1, 1, 1));
            }
            backgroundImage.type = Image.Type.Sliced;
        }
        
        // Setup handle
        if (handleImage != null)
        {
            // Crea uno sprite circolare se non esiste
            if (handleImage.sprite == null)
            {
                handleImage.sprite = CreateCircleSprite(64, new Color(1, 1, 1, 1));
            }
            handleImage.type = Image.Type.Sliced;
        }
    }
    
    /// <summary>
    /// Crea uno sprite circolare procedurale
    /// </summary>
    private Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.4f;
        float borderRadius = size * 0.45f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    // Centro del cerchio
                    texture.SetPixel(x, y, color);
                }
                else if (distance <= borderRadius)
                {
                    // Bordo con anti-aliasing
                    float alpha = 1f - (distance - radius) / (borderRadius - radius);
                    Color borderColor = color;
                    borderColor.a = alpha * 0.8f;
                    texture.SetPixel(x, y, borderColor);
                }
                else
                {
                    // Trasparente
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// Animazione di scala fluida
    /// </summary>
    private IEnumerator ScaleAnimation(RectTransform target, Vector3 targetScale, float speed)
    {
        Vector3 startScale = target.localScale;
        float t = 0;
        
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * speed;
            target.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        target.localScale = targetScale;
    }
    
    /// <summary>
    /// Animazione di pulse continua quando il joystick è idle
    /// </summary>
    private IEnumerator PulseAnimation()
    {
        while (true)
        {
            if (!isDragging && CanPlayerAttack())
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
                Vector3 pulseScale = originalBackgroundScale * (1f + pulse);
                
                if (joystickBackground != null)
                {
                    joystickBackground.localScale = pulseScale;
                }
                
                // Pulse del colore
                if (backgroundImage != null)
                {
                    Color pulseColor = backgroundActiveColor;
                    pulseColor.a = backgroundNormalColor.a + (pulse * 0.2f);
                    backgroundImage.color = Color.Lerp(backgroundNormalColor, pulseColor, Mathf.Abs(pulse));
                }
            }
            
            yield return null;
        }
    }
}