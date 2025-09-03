using UnityEngine;
using UnityEngine.UI;

public class MinimapFollow : MonoBehaviour
{
    [Header("Player Follow")]
    public Transform player;

    [Header("Camera Settings")]
    public float cameraHeight = 5f;

    [Header("UI References")]
    public RawImage minimapDisplay;
    public Image borderFrame;

    [Header("Border Customization")]
    public Color borderColor = Color.black;
    public Vector2 minimapSize = new Vector2(200, 200);
    public float borderThickness = 10f;
    
    [Header("Hub Settings")]
    private HubController hubController;
    private bool wasActiveBeforeHub = true;

    private Camera cam;
    private RenderTexture renderTexture;

    private void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = cameraHeight;
        cam.rect = new Rect(0, 0, 1, 1);

        // Trova il HubController
        hubController = FindFirstObjectByType<HubController>();

        StartCoroutine(SetupMinimapDelayed());
    }

    System.Collections.IEnumerator SetupMinimapDelayed()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        SetupMinimapRenderTexture();
        SetupBorder();
    }

    void SetupMinimapRenderTexture()
    {
        Vector2 actualSize = minimapDisplay.GetComponent<RectTransform>().sizeDelta;
        renderTexture = new RenderTexture((int)actualSize.x, (int)actualSize.y, 16);
        renderTexture.Create();

        cam.targetTexture = renderTexture;

        if (minimapDisplay != null)
        {
            minimapDisplay.texture = renderTexture;
            minimapDisplay.uvRect = new Rect(0, 0, 1, 1);

            Debug.Log($"Render Texture creata: {actualSize.x} x {actualSize.y}");
            Debug.Log($"RawImage size: {minimapDisplay.rectTransform.sizeDelta}");
        }
    }

    void SetupBorder()
    {
        if (borderFrame != null)
        {
            borderFrame.color = borderColor;
            Vector2 actualMinimapSize = minimapDisplay.GetComponent<RectTransform>().sizeDelta;
            RectTransform borderRect = borderFrame.GetComponent<RectTransform>();
            borderRect.sizeDelta = actualMinimapSize + Vector2.one * borderThickness * 2;

            borderFrame.transform.SetSiblingIndex(minimapDisplay.transform.GetSiblingIndex() - 1);
        }
    }

    void Update()
    {
        // Controlla se il player è nell'hub e gestisci la visibilità della minimappa
        if (hubController != null)
        {
            bool playerInHub = hubController.IsPlayerInHub();
            
            if (playerInHub && minimapDisplay.gameObject.activeInHierarchy)
            {
                // Player appena entrato nell'hub - nascondi minimappa
                wasActiveBeforeHub = minimapDisplay.gameObject.activeInHierarchy;
                SetMinimapVisibility(false);
            }
            else if (!playerInHub && !minimapDisplay.gameObject.activeInHierarchy && wasActiveBeforeHub)
            {
                // Player appena uscito dall'hub - mostra minimappa
                SetMinimapVisibility(true);
            }
        }
    }

    void SetMinimapVisibility(bool visible)
    {
        if (minimapDisplay != null)
            minimapDisplay.gameObject.SetActive(visible);
        
        if (borderFrame != null)
            borderFrame.gameObject.SetActive(visible);
        
        // Attiva/disattiva anche la camera per risparmiare performance
        cam.enabled = visible;
    }

    void LateUpdate()
    {
        if (player != null && cam.enabled)
        {
            Vector3 newPos = player.position;
            newPos.z = -10f;
            transform.position = newPos;
        }
    }

    public void ChangeBorderColor(Color newColor)
    {
        borderColor = newColor;
        if (borderFrame != null)
            borderFrame.color = borderColor;
    }

    public void SetCameraHeight(float height)
    {
        cameraHeight = height;
        cam.orthographicSize = cameraHeight;
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}