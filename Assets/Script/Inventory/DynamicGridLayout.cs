using UnityEngine;
using UnityEngine.UI;

public class DynamicGridLayout : MonoBehaviour
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
            //Debug.LogError("DynamicGridLayout: Nessun GridLayoutGroup trovato su " + gameObject.name);
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

        // Debug per verificare i calcoli
        //Debug.Log($"Container Size: {containerSize}, Cell Size: {cellSize}");
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
}