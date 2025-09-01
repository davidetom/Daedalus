using UnityEngine;
using UnityEngine.UI;

public class MinimapFollow : MonoBehaviour
{
    [Header("Player Follow")]
    public Transform player; //Player da seguire

    [Header("Camera Settings")]
    public float cameraHeight = 5f; //Altezza camera orthographic

    [Header("UI References")]
    public RawImage minimapDisplay; //Rawimage che mostra la minimappa
    public Image borderFrame; //immagine del bordo

    [Header("Border Customization")]
    public Color borderColor = Color.black;
    public Vector2 minimapSize = new Vector2(200, 200);
    public float borderThickness = 10f;

    private Camera cam;
    private RenderTexture renderTexture;

    private void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = cameraHeight;

        cam.rect = new Rect(0, 0, 1, 1);

        // Aspetta un frame per assicurarsi che l'UI sia completamente inizializzata
        StartCoroutine(SetupMinimapDelayed());
    }

    System.Collections.IEnumerator SetupMinimapDelayed()
    {
        // Aspetta più frame per permettere al Canvas Scaler di fare il suo lavoro
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f); // Piccolo delay aggiuntivo

        SetupMinimapRenderTexture();
        SetupBorder();
    }

    void SetupMinimapRenderTexture()
    {
        // Usa le stesse dimensioni del RawImage per evitare scaling issues
        Vector2 actualSize = minimapDisplay.GetComponent<RectTransform>().sizeDelta;

        // Crea la Render Texture con le dimensioni corrette
        renderTexture = new RenderTexture((int)actualSize.x, (int)actualSize.y, 16);
        renderTexture.Create();

        // Assegna la Render Texture alla camera
        cam.targetTexture = renderTexture;

        // Assegna la texture al RawImage UI
        if (minimapDisplay != null)
        {
            minimapDisplay.texture = renderTexture;
            minimapDisplay.uvRect = new Rect(0, 0, 1, 1); // <-- Aggiungi questa riga

            Debug.Log($"Render Texture creata: {actualSize.x} x {actualSize.y}");
            Debug.Log($"RawImage size: {minimapDisplay.rectTransform.sizeDelta}");
        }
    }

    void SetupBorder()
    {
        if (borderFrame != null)
        {
            // Imposta il colore del bordo
            borderFrame.color = borderColor;

            // Il bordo ha le stesse dimensioni del RawImage + spessore
            Vector2 actualMinimapSize = minimapDisplay.GetComponent<RectTransform>().sizeDelta;
            RectTransform borderRect = borderFrame.GetComponent<RectTransform>();
            borderRect.sizeDelta = actualMinimapSize + Vector2.one * borderThickness * 2;

            // Assicurati che il bordo sia dietro la minimappa nell'ordine di rendering
            borderFrame.transform.SetSiblingIndex(minimapDisplay.transform.GetSiblingIndex() - 1);
        }
    }

    void LateUpdate()
    {
        if (player != null)
        {
            // Centra la minimappa sul Player
            Vector3 newPos = player.position;
            newPos.z = -10f; // fisso per la camera 2D
            transform.position = newPos;
        }
    }

    // Metodo per cambiare il colore del bordo a runtime
    public void ChangeBorderColor(Color newColor)
    {
        borderColor = newColor;
        if (borderFrame != null)
            borderFrame.color = borderColor;
    }

    // Metodo per cambiare l'altezza della camera
    public void SetCameraHeight(float height)
    {
        cameraHeight = height;
        cam.orthographicSize = cameraHeight;
    }

    void OnDestroy()
    {
        // Pulisci la Render Texture quando l'oggetto viene distrutto
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}
