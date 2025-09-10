using UnityEngine;
using UnityEngine.UI;

public class DynamicShopLayout : MonoBehaviour
{
    [Header("Grid Configuration")]
    [Tooltip("Numero di colonne nel grid")]
    public int columns = 5;

    [Tooltip("Numero di righe nel grid")]
    public int rows = 4;

    [Tooltip("Spazio tra le celle")]
    public Vector2 spacing = new Vector2(5, 5);

    [Header("Padding Settings")]
    [Tooltip("Margine sinistro")]
    public int paddingLeft = 10;
    [Tooltip("Margine destro")]
    public int paddingRight = 10;
    [Tooltip("Margine superiore")]
    public int paddingTop = 10;
    [Tooltip("Margine inferiore")]
    public int paddingBottom = 10;

    [Header("Options")]
    [Tooltip("Mantiene le celle quadrate")]
    public bool keepSquare = true;

    [Tooltip("Aggiorna automaticamente quando cambia la dimensione")]
    public bool autoUpdate = true;

    [Header("Text Scaling")]
    [Tooltip("Abilita il ridimensionamento automatico del testo")]
    public bool enableTextScaling = true;

    [Tooltip("Dimensione base del testo")]
    public float baseTextSize = 14f;

    [Tooltip("Dimensione minima del testo")]
    public float minTextSize = 8f;

    [Tooltip("Dimensione massima del testo")]
    public float maxTextSize = 32f;

    [Tooltip("Fattore di scala per il testo")]
    [Range(0.5f, 2f)]
    public float textScaleFactor = 1f;

    [Header("Image & Button Scaling")]
    [Tooltip("Abilita il ridimensionamento automatico di immagini e bottoni")]
    public bool enableImageButtonScaling = true;

    [Tooltip("Dimensione base larghezza bottoni")]
    public float baseButtonWidth = 150f;

    [Tooltip("Dimensione base altezza bottoni")]
    public float baseButtonHeight = 80f;

    [Tooltip("Dimensione base larghezza immagini (per cella di riferimento)")]
    public float baseImageWidth = 190f;

    [Tooltip("Dimensione base altezza immagini (per cella di riferimento)")]
    public float baseImageHeight = 160f;

    [Tooltip("Dimensione minima larghezza immagini")]
    public float minImageWidth = 50f;

    [Tooltip("Dimensione massima larghezza immagini")]
    public float maxImageWidth = 300f;

    [Tooltip("Dimensione minima altezza immagini")]
    public float minImageHeight = 40f;

    [Tooltip("Dimensione massima altezza immagini")]
    public float maxImageHeight = 250f;

    [Tooltip("Fattore di scala per immagini e bottoni")]
    [Range(0.5f, 2f)]
    public float imageButtonScaleFactor = 1f;

    private GridLayoutGroup gridLayout;
    private RectTransform rectTransform;
    private Vector2 lastSize;

    void Awake()
    {
        Initialize();
    }

    void Start()
    {
        UpdateCellSize();
    }

    void Update()
    {
        if (autoUpdate)
        {
            CheckForSizeChange();
        }
    }

    void Initialize()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();

        if (gridLayout == null)
        {
            Debug.LogError("DynamicGridLayout: Nessun GridLayoutGroup trovato su " + gameObject.name);
            return;
        }

        // Configura il GridLayoutGroup
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;
    }

    void CheckForSizeChange()
    {
        if (rectTransform == null) return;

        Vector2 currentSize = rectTransform.rect.size;
        if (currentSize != lastSize)
        {
            UpdateCellSize();
            lastSize = currentSize;
        }
    }

    [ContextMenu("Update Cell Size")]
    public void UpdateCellSize()
    {
        if (gridLayout == null || rectTransform == null) return;

        Vector2 containerSize = rectTransform.rect.size;

        RectOffset padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);

        // Calcola lo spazio disponibile
        float availableWidth = containerSize.x - padding.left - padding.right - (spacing.x * (columns - 1));
        float availableHeight = containerSize.y - padding.top - padding.bottom - (spacing.y * (rows - 1));

        // Calcola le dimensioni delle celle
        float cellWidth = availableWidth / columns;
        float cellHeight = availableHeight / rows;

        Vector2 cellSize;

        if (keepSquare)
        {
            // Mantieni le celle quadrate
            float size = Mathf.Min(cellWidth, cellHeight);
            cellSize = new Vector2(size, size);
        }
        else
        {
            cellSize = new Vector2(cellWidth, cellHeight);
        }

        gridLayout.cellSize = cellSize;
        gridLayout.spacing = spacing;
        gridLayout.padding = padding;

        // Ridimensiona i testi
        if (enableTextScaling)
        {
            ScaleTextsInCells(cellSize);
        }

        //Ridimensionamento Bottoni e Immagini
        if (enableImageButtonScaling)
        {
            ScaleImagesAndButtonsInCells(cellSize);
        }
    }

    void ScaleTextsInCells(Vector2 cellSize)
    {
        // Calcola la nuova dimensione del testo basata sulla dimensione della cella
        float cellArea = cellSize.x * cellSize.y;
        float baseArea = 350f * 220f;
        float areaRatio = cellArea / baseArea;

        float newTextSize = baseTextSize * Mathf.Sqrt(areaRatio) * textScaleFactor;
        newTextSize = Mathf.Clamp(newTextSize, minTextSize, maxTextSize);

        // Applica la nuova dimensione
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform cell = transform.GetChild(i);
            ScaleTextsInCell(cell, newTextSize);
        }
    }

    void ScaleTextsInCell(Transform cell, float textSize)
    {
        Text[] texts = cell.GetComponentsInChildren<Text>();
        foreach (Text text in texts)
        {
            LayoutElement layoutElement = text.GetComponent<LayoutElement>();
            if (layoutElement != null && layoutElement.ignoreLayout) continue;
            text.fontSize = Mathf.RoundToInt(textSize);
        }

        TMPro.TextMeshProUGUI[] tmpTexts = cell.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (TMPro.TextMeshProUGUI tmpText in tmpTexts)
        {
            tmpText.fontSize = textSize;
        }

    }

    void ScaleImagesAndButtonsInCells(Vector2 cellSize)
    {
        float cellArea = cellSize.x * cellSize.y;
        float baseArea = 350f * 220f; // O dovrebbe essere 290f * 248f per le tue celle?
        float areaRatio = cellArea / baseArea;

        float scaleFactor = Mathf.Sqrt(areaRatio); // Solo il fattore di scala

        // Applica a tutte le celle passando il fattore invece della dimensione
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform cell = transform.GetChild(i);
            ScaleImagesAndButtonsInCell(cell, scaleFactor, cellSize);
        }
    }

    void ScaleImagesAndButtonsInCell(Transform cell, float scaleFactor, Vector2 cellSize)
    {
        float newImageWidth = baseImageWidth * scaleFactor * imageButtonScaleFactor;
        float newImageHeight = baseImageHeight * scaleFactor * imageButtonScaleFactor;

        newImageWidth = Mathf.Clamp(newImageWidth, minImageWidth, maxImageWidth);
        newImageHeight = Mathf.Clamp(newImageHeight, minImageHeight, maxImageHeight);

        Image[] images = cell.GetComponentsInChildren<Image>();
        foreach (Image image in images)
        {
            if (image.transform == cell) continue;

            LayoutElement layoutElement = image.GetComponent<LayoutElement>();
            if (layoutElement != null && layoutElement.ignoreLayout) continue;

            RectTransform imageRect = image.GetComponent<RectTransform>();
            if (imageRect != null)
            {
                imageRect.sizeDelta = new Vector2(newImageWidth, newImageHeight);
            }
        }

        // Ridimensiona bottoni
        Button[] buttons = cell.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            LayoutElement layoutElement = button.GetComponent<LayoutElement>();
            if (layoutElement != null && layoutElement.ignoreLayout) continue;

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                float buttonWidth = baseButtonWidth * scaleFactor * imageButtonScaleFactor;
                float buttonHeight = baseButtonHeight * scaleFactor * imageButtonScaleFactor;
                buttonRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            }
        }
    }

    public void SetColumns(int newColumns)
    {
        columns = newColumns;
        if (gridLayout != null)
        {
            gridLayout.constraintCount = columns;
            UpdateCellSize();
        }
    }

    public void SetPadding(int left, int right, int top, int bottom)
    {
        paddingLeft = left;
        paddingRight = right;
        paddingTop = top;
        paddingBottom = bottom;
        UpdateCellSize();
    }

    public void SetTextScaling(bool enabled, float baseSize = 14f, float scaleFactor = 1f)
    {
        enableTextScaling = enabled;
        baseTextSize = baseSize;
        textScaleFactor = scaleFactor;
        if (enabled) UpdateCellSize();
    }

    public void SetTextSizeLimits(float minSize, float maxSize)
    {
        minTextSize = minSize;
        maxTextSize = maxSize;
        if (enableTextScaling) UpdateCellSize();
    }

    // Funzioni per configurare image/button scaling
    public void SetImageButtonScaling(bool enabled, float baseWidth = 190f, float baseHeight = 160f, float scaleFactor = 1f)
    {
        enableImageButtonScaling = enabled;
        baseImageWidth = baseWidth;
        baseImageHeight = baseHeight;
        imageButtonScaleFactor = scaleFactor;
        if (enabled) UpdateCellSize();
    }

    public void SetImageSizeLimits(float minWidth, float maxWidth, float minHeight, float maxHeight)
    {
        minImageWidth = minWidth;
        maxImageWidth = maxWidth;
        minImageHeight = minHeight;
        maxImageHeight = maxHeight;
        if (enableImageButtonScaling) UpdateCellSize();
    }
}
