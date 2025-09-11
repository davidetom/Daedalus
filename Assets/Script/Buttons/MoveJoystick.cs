using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class MovementJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("UI Components")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public Image handleImage;
    public Image backgroundImage;
    
    [Header("Settings")]
    [Tooltip("Distanza massima come percentuale della larghezza dello schermo (0.1 = 10% della larghezza)")]
    public float maxDistancePercentage = 0.08f; // 8% della larghezza schermo
    public float deadZone = 0.2f; // Soglia per iniziare il movimento
    
    [Header("Alternative Settings")]
    [Tooltip("Se true, usa la percentuale. Se false, usa i pixel scalati per DPI")]
    public bool usePercentageMode = true;
    [Tooltip("Distanza in pixel per risoluzione di riferimento 1920x1080")]
    public float referenceMaxDistance = 80f;
    
    [Header("Visual Feedback")]
    public Color normalColor = new Color(0.8f, 0.8f, 0.9f, 0.7f);
    public Color dragColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
    public Color disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
    
    [Header("Background Visual")]
    public Color backgroundNormalColor = new Color(0.2f, 0.2f, 0.3f, 0.6f);
    public Color backgroundActiveColor = new Color(0.1f, 0.3f, 0.1f, 0.8f);
    
    [Header("Animation Settings")]
    public float scaleOnPress = 0.9f;
    public float scaleAnimationSpeed = 10f;
    public bool enablePulseAnimation = true;
    public float pulseSpeed = 1.5f;
    public float pulseIntensity = 0.05f;
    
    [Header("Player Reference")]
    public PlayerController playerController;
    
    private Vector2 startPosition;
    private Vector2 currentPosition;
    private bool isDragging = false;
    private Vector2 joystickCenter;
    private Vector2 movementDirection = Vector2.zero;
    private Vector3 originalHandleScale;
    private Vector3 originalBackgroundScale;
    private Coroutine pulseCoroutine;
    private Coroutine movementCoroutine;
    
    // Variabile calcolata per la distanza massima effettiva
    private float calculatedMaxDistance;
    
    void Start()
    {
        // Calcola la distanza massima in base al tipo di scaling scelto
        CalculateMaxDistance();
        
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
        
        // Debug info
        Debug.Log($"Joystick Max Distance: {calculatedMaxDistance}px (Screen: {Screen.width}x{Screen.height}, DPI: {Screen.dpi})");
    }
    
    private void CalculateMaxDistance()
    {
        if (usePercentageMode)
        {
            // Modalità percentuale: usa una percentuale della larghezza dello schermo
            calculatedMaxDistance = Screen.width * maxDistancePercentage;
        }
        else
        {
            // Modalità DPI scaling: scala la distanza di riferimento in base ai DPI
            float referenceDPI = 96f; // DPI standard per 1920x1080
            float currentDPI = Screen.dpi > 0 ? Screen.dpi : referenceDPI;
            float dpiScale = currentDPI / referenceDPI;
            
            calculatedMaxDistance = referenceMaxDistance * dpiScale;
            
            // Clamp per evitare valori troppo estremi
            calculatedMaxDistance = Mathf.Clamp(calculatedMaxDistance, 40f, Screen.width * 0.15f);
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        startPosition = eventData.position;
        currentPosition = eventData.position;
        movementDirection = Vector2.zero;
        
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
        if (!isDragging) return;
        
        currentPosition = eventData.position;
        
        Vector2 direction = currentPosition - startPosition;
        float distance = direction.magnitude;
        
        // Aggiorna la posizione del joystick handle usando la distanza calcolata
        Vector2 clampedDirection = Vector2.ClampMagnitude(direction, calculatedMaxDistance);
        joystickHandle.position = joystickCenter + clampedDirection;
        
        // Se superiamo la dead zone, iniziamo il movimento
        if (distance > deadZone * calculatedMaxDistance)
        {
            Vector2 normalizedDirection = clampedDirection.normalized;
            Vector2 cardinalDirection = ConvertToCardinalDirection(normalizedDirection);
            
            // Solo se la direzione è cambiata, aggiorna il movimento
            if (cardinalDirection != movementDirection)
            {
                movementDirection = cardinalDirection;
                UpdatePlayerMovement(cardinalDirection);
            }
        }
        else
        {
            // Dentro la dead zone - ferma il movimento
            if (movementDirection != Vector2.zero)
            {
                movementDirection = Vector2.zero;
                StopPlayerMovement();
            }
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        
        // Ripristina la scala originale del handle
        if (joystickHandle != null)
        {
            StartCoroutine(ScaleAnimation(joystickHandle, originalHandleScale, scaleAnimationSpeed));
        }
        
        // Reset visual del joystick
        joystickHandle.position = joystickCenter;
        
        // Ferma il movimento del player
        StopPlayerMovement();
        movementDirection = Vector2.zero;
        
        // Reset colori
        if (handleImage != null)
        {
            handleImage.color = CanPlayerMove() ? normalColor : disabledColor;
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundNormalColor;
        }
        
        // Riavvia animazione di pulse
        if (enablePulseAnimation && pulseCoroutine == null)
        {
            pulseCoroutine = StartCoroutine(PulseAnimation());
        }
    }
    
    // Converte la direzione del joystick in direzione cardinale
    private Vector2 ConvertToCardinalDirection(Vector2 direction)
    {
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);
        
        if (absX > absY)
        {
            // Movimento orizzontale
            return direction.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            // Movimento verticale
            return direction.y > 0 ? Vector2.up : Vector2.down;
        }
    }
    
    private void UpdatePlayerMovement(Vector2 direction)
    {
        if (playerController != null && CanPlayerMove())
        {
            // Ferma la coroutine precedente se esiste
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
            }
            
            // Avvia il nuovo movimento
            movementCoroutine = StartCoroutine(ContinuousMovement(direction));
        }
    }
    
    private void StopPlayerMovement()
    {
        if (playerController != null)
        {
            playerController.StopMovimento();
            
            // Ferma la coroutine di movimento continuo
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
        }
    }
    
    private IEnumerator ContinuousMovement(Vector2 direction)
    {
        while (isDragging && movementDirection == direction && CanPlayerMove())
        {
            // Invia il comando di movimento al player
            if (direction == Vector2.up)
                playerController.MuoviSu();
            else if (direction == Vector2.down)
                playerController.MuoviGiu();
            else if (direction == Vector2.right)
                playerController.MuoviDestra();
            else if (direction == Vector2.left)
                playerController.MuoviSinistra();
            
            yield return null; // Aspetta un frame
        }
        
        // Ferma il movimento quando esce dal loop
        playerController.StopMovimento();
        movementCoroutine = null;
    }
    
    private bool CanPlayerMove()
    {
        if (playerController == null) return false;
        
        // Il player può muoversi se:
        // - Non è morto
        // - Non sta attaccando (a meno che non possa attaccare mentre si muove)
        // - Non è già in movimento (per evitare conflitti) o può attaccare mentre si muove
        return !playerController.isDead && 
               (!playerController.isAttacking || playerController.canAttackWhileMoving) &&
               (!playerController.isMoving || playerController.canAttackWhileMoving);
    }
    
    void Update()
    {
        // Aggiorna il colore del handle in base allo stato del player
        if (handleImage != null && !isDragging)
        {
            if (CanPlayerMove())
            {
                handleImage.color = normalColor;
            }
            else
            {
                handleImage.color = disabledColor;
            }
        }
    }
    
    // Setup dello stile del joystick con sprite circolari
    private void SetupJoystickStyle()
    {
        // Setup background
        if (backgroundImage != null)
        {
            if (backgroundImage.sprite == null)
            {
                backgroundImage.sprite = CreateCircleSprite(128, new Color(1, 1, 1, 1));
            }
            backgroundImage.type = Image.Type.Sliced;
        }

        // Setup handle
        if (handleImage != null)
        {
            if (handleImage.sprite == null)
            {
                handleImage.sprite = CreateCircleSprite(64, new Color(1, 1, 1, 1));
            }
            handleImage.type = Image.Type.Sliced;
        }
    }
    
    // Crea uno sprite circolare procedurale
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

    // Animazione di scala fluida
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
    
    // Animazione di pulse continua quando il joystick è idle
    private IEnumerator PulseAnimation()
    {
        while (true)
        {
            if (!isDragging && CanPlayerMove())
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
                    pulseColor.a = backgroundNormalColor.a + (pulse * 0.1f);
                    backgroundImage.color = Color.Lerp(backgroundNormalColor, pulseColor, Mathf.Abs(pulse));
                }
            }
            else
            {
                // Ripristina la scala normale quando è in uso
                if (joystickBackground != null)
                {
                    joystickBackground.localScale = originalBackgroundScale;
                }
            }
            
            yield return null;
        }
    }
    
    // Metodo pubblico per forzare lo stop del movimento (utile per altri sistemi)
    public void ForceStopMovement()
    {
        isDragging = false;
        movementDirection = Vector2.zero;
        
        if (joystickHandle != null)
        {
            joystickHandle.position = joystickCenter;
        }
        
        StopPlayerMovement();
        
        // Reset colori
        if (handleImage != null)
        {
            handleImage.color = CanPlayerMove() ? normalColor : disabledColor;
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundNormalColor;
        }
    }
    
    // Metodi pubblici per debug e regolazione runtime
    [ContextMenu("Recalculate Max Distance")]
    public void RecalculateMaxDistance()
    {
        CalculateMaxDistance();
        Debug.Log($"New Max Distance: {calculatedMaxDistance}px");
    }
    
    // Proprietà per accedere alla distanza calcolata
    public float GetCalculatedMaxDistance()
    {
        return calculatedMaxDistance;
    }
}