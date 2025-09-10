using UnityEngine;
using UnityEngine.UI;

public class DIfficultyMenuLayout : MonoBehaviour
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

    [Tooltip("Dimensione base del testo (per cella 100x100)")]
    public float baseTextSize = 14f;

    [Tooltip("Dimensione minima del testo")]
    public float minTextSize = 8f;

    [Tooltip("Dimensione massima del testo")]
    public float maxTextSize = 32f;

    [Tooltip("Fattore di scala per il testo (più alto = testo più grande)")]
    [Range(0.5f, 2f)]
    public float textScaleFactor = 1f;

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

        // Crea il RectOffset qui, non nell'inizializzazione del campo
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
            // Mantieni le celle quadrate usando la dimensione minore
            float size = Mathf.Min(cellWidth, cellHeight);
            cellSize = new Vector2(size, size);
        }
        else
        {
            cellSize = new Vector2(cellWidth, cellHeight);
        }

        // Applica le impostazioni
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = spacing;
        gridLayout.padding = padding;

        // Ridimensiona i testi nelle celle se abilitato
        if (enableTextScaling)
        {
            ScaleTextsInCells(cellSize);
        }

        // Debug per verificare i calcoli
        Debug.Log($"Container Size: {containerSize}, Cell Size: {cellSize}");
    }

    void ScaleTextsInCells(Vector2 cellSize)
    {
        // Calcola la nuova dimensione del testo basata sulla dimensione della cella
        float cellArea = cellSize.x * cellSize.y;
        float baseArea = 350f * 220f; // Area di riferimento (100x100)
        float areaRatio = cellArea / baseArea;

        // Calcola la nuova dimensione del testo
        float newTextSize = baseTextSize * Mathf.Sqrt(areaRatio) * textScaleFactor;
        newTextSize = Mathf.Clamp(newTextSize, minTextSize, maxTextSize);

        // Applica la nuova dimensione a tutti i testi nelle celle
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform cell = transform.GetChild(i);
            ScaleTextsInCell(cell, newTextSize);
        }

        Debug.Log($"Text Size Updated: {newTextSize} (Cell Size: {cellSize})");
    }

    void ScaleTextsInCell(Transform cell, float textSize)
    {
        // Trova tutti i componenti Text (UI classica) nella cella
        Text[] texts = cell.GetComponentsInChildren<Text>();
        foreach (Text text in texts)
        {
            text.fontSize = Mathf.RoundToInt(textSize);
        }

        // Trova tutti i componenti TextMeshPro nella cella
        TMPro.TextMeshProUGUI[] tmpTexts = cell.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (TMPro.TextMeshProUGUI tmpText in tmpTexts)
        {
            tmpText.fontSize = textSize;
        }

        // Se hai altri tipi di testo, aggiungili qui
    }

    // Funzione per cambiare il numero di colonne runtime
    public void SetColumns(int newColumns)
    {
        columns = newColumns;
        if (gridLayout != null)
        {
            gridLayout.constraintCount = columns;
            UpdateCellSize();
        }
    }

    // Funzioni utility per impostare il padding da codice
    public void SetPadding(int left, int right, int top, int bottom)
    {
        paddingLeft = left;
        paddingRight = right;
        paddingTop = top;
        paddingBottom = bottom;
        UpdateCellSize();
    }

    // Funzioni per configurare il text scaling
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
}