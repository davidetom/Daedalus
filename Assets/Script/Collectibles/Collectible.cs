using UnityEngine;

public abstract class Collectible : MonoBehaviour
{
    public MapManager mapManager;
    public GemSpawner gemSpawner;

    public Vector2Int position;

    public void Start()
    {
        if (mapManager == null)
            mapManager = FindFirstObjectByType<MapManager>();

        if (gemSpawner == null)
            gemSpawner = FindFirstObjectByType<GemSpawner>();

        // Usa le coordinate array corrette invece di cast diretto
        Vector2Int arrayPos = mapManager != null ? 
            mapManager.WorldToArrayCoordinates(transform.position) : 
            new Vector2Int((int)transform.position.x, (int)transform.position.y);
        
        position = arrayPos;
    }

    // Ogni oggetto raccolto può fare qualcosa di specifico sul player
    public abstract void OnCollect(PlayerController player);
    
    public abstract void NotifyOnPick(GemSpawner gemSpawner);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Usa le coordinate corrette della gemma
                NotifyOnPick(gemSpawner);
                OnCollect(player); // comportamento specifico
            }
        }
    }
}