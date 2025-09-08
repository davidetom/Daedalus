using UnityEngine;
using UnityEngine.Tilemaps;

public class FogManager : MonoBehaviour
{
    [Header("Tilemap Settings")]
    public Tilemap mainTilemap;      // La tilemap che cambia ogni giorno
    public Tilemap fogTilemap;       // La tilemap della nebbia (separata)
    public TileBase fogTile;         // Tile da usare per la nebbia

    [Header("Fog Settings")]
    public Vector3Int centerCell;    // Il centro in coordinate cella
    public float minDist = 3f;       // Distanza minima (dentro = nessuna nebbia)
    public float maxDist = 8f;       // Distanza massima (fuori = nessuna nebbia)
    public Color fogColor = Color.gray; // Colore nebbia uniforme

    [Header("Warning Zone Settings")]
    public Vector3Int warningCenterCell = new Vector3Int(155, 155, 0); // Centro della warning zone
    public float warningMinDist = 117f;  // Distanza minima warning zone
    public float warningMaxDist = 119f;  // Distanza massima warning zone
    public bool canPlayerPassFog = false; // Il player può passare attraverso la nebbia?

    // Eventi per la warning zone
    public static System.Action OnPlayerEnteredWarningZone;
    public static System.Action OnPlayerExitedWarningZone;

    // Stato interno per tracking del player
    private bool isPlayerInWarningZone = false;

    void Start()
    {
        // Assicurati che la fog tilemap sia sopra la main tilemap
        if (fogTilemap != null && mainTilemap != null)
        {
            var fogRenderer = fogTilemap.GetComponent<TilemapRenderer>();
            var mainRenderer = mainTilemap.GetComponent<TilemapRenderer>();

            if (fogRenderer != null && mainRenderer != null)
            {
                fogRenderer.sortingOrder = mainRenderer.sortingOrder + 2;
            }
        }

        ApplyFog();
    }

    [ContextMenu("Applica Nebbia")]
    public void ApplyFog()
    {
        if (fogTilemap == null || fogTile == null || mainTilemap == null)
        {
            Debug.LogError("Tilemap o FogTile non assegnati!");
            return;
        }

        // Pulisci la fog tilemap
        ClearFogTilemap();

        // Applica nebbia solo nell'anello tra minDist e maxDist
        foreach (var pos in mainTilemap.cellBounds.allPositionsWithin)
        {
            if (!mainTilemap.HasTile(pos)) continue;

            float dist = Vector3Int.Distance(pos, centerCell);

            // Nebbia SOLO nell'anello tra minDist e maxDist
            if (dist >= minDist && dist <= maxDist)
            {
                fogTilemap.SetTile(pos, fogTile);
                fogTilemap.SetColor(pos, fogColor);
            }
        }
    }

    [ContextMenu("Rimuovi Nebbia")]
    public void RemoveFog()
    {
        ClearFogTilemap();
    }

    // Rimuove nebbia in una posizione specifica
    public void RevealFogAtPosition(Vector3Int position)
    {
        if (fogTilemap != null && fogTilemap.HasTile(position))
        {
            fogTilemap.SetTile(position, null);
        }
    }

    // Rimuove nebbia in un raggio (completamente)
    public void RevealFogInRadius(Vector3Int centerPosition, float radius)
    {
        BoundsInt area = new BoundsInt(
            centerPosition.x - Mathf.CeilToInt(radius),
            centerPosition.y - Mathf.CeilToInt(radius),
            0,
            Mathf.CeilToInt(radius * 2) + 1,
            Mathf.CeilToInt(radius * 2) + 1,
            1
        );

        foreach (var pos in area.allPositionsWithin)
        {
            float dist = Vector3Int.Distance(pos, centerPosition);
            if (dist <= radius && fogTilemap.HasTile(pos))
            {
                fogTilemap.SetTile(pos, null);
            }
        }
    }

    // Effetto "torcia" - rimuove completamente nel centro, gradualmente sui bordi
    public void RevealFogGradually(Vector3Int centerPosition, float innerRadius, float outerRadius)
    {
        BoundsInt area = new BoundsInt(
            centerPosition.x - Mathf.CeilToInt(outerRadius),
            centerPosition.y - Mathf.CeilToInt(outerRadius),
            0,
            Mathf.CeilToInt(outerRadius * 2) + 1,
            Mathf.CeilToInt(outerRadius * 2) + 1,
            1
        );

        foreach (var pos in area.allPositionsWithin)
        {
            if (!fogTilemap.HasTile(pos)) continue;

            float dist = Vector3Int.Distance(pos, centerPosition);

            if (dist <= innerRadius)
            {
                // Rimuovi completamente la nebbia nel centro
                fogTilemap.SetTile(pos, null);
            }
            else if (dist <= outerRadius)
            {
                // Riduci gradualmente l'intensità della nebbia sui bordi
                float t = Mathf.InverseLerp(innerRadius, outerRadius, dist);
                Color newColor = new Color(fogColor.r, fogColor.g, fogColor.b, t);
                fogTilemap.SetColor(pos, newColor);
            }
        }
    }

    private void ClearFogTilemap()
    {
        if (fogTilemap == null) return;

        BoundsInt bounds = fogTilemap.cellBounds;
        TileBase[] emptyTiles = new TileBase[bounds.size.x * bounds.size.y * bounds.size.z];
        fogTilemap.SetTilesBlock(bounds, emptyTiles);
    }

    // Metodi di utilità 
    public void SetFogCenter(Vector3Int newCenter)
    {
        centerCell = newCenter;
        ApplyFog();
    }

    public void UpdateFogDistances(float newMinDist, float newMaxDist)
    {
        minDist = newMinDist;
        maxDist = newMaxDist;
        ApplyFog();
    }

    // Verifica se c'è nebbia in una posizione
    public bool HasFogAtPosition(Vector3Int position)
    {
        return fogTilemap != null && fogTilemap.HasTile(position);
    }

    public void CheckPlayerWarningZone(Vector3 playerWorldPosition)
    {
        // Debug dettagliato delle coordinate
        if (Debug.isDebugBuild)
        {
            Debug.Log($"=== DIAGNOSI COORDINATE ===");
            Debug.Log($"Player World Position: {playerWorldPosition}");

            if (mainTilemap != null)
            {
                Vector3Int playerCell = mainTilemap.WorldToCell(playerWorldPosition);
                Vector3 reconvertedWorld = mainTilemap.CellToWorld(playerCell);

                Debug.Log($"MainTilemap conversion: World {playerWorldPosition} -> Cell {playerCell} -> World {reconvertedWorld}");
            }

            if (fogTilemap != null)
            {
                Vector3Int playerCellFog = fogTilemap.WorldToCell(playerWorldPosition);
                Vector3 reconvertedWorldFog = fogTilemap.CellToWorld(playerCellFog);

                Debug.Log($"FogTilemap conversion: World {playerWorldPosition} -> Cell {playerCellFog} -> World {reconvertedWorldFog}");
            }

            Debug.Log($"Warning Center Cell: {warningCenterCell}");

            if (mainTilemap != null)
            {
                Vector3 warningCenterWorld = mainTilemap.CellToWorld(warningCenterCell);
                Debug.Log($"Warning Center World: {warningCenterWorld}");
            }

            Debug.Log($"=== FINE DIAGNOSI ===");
        }

        // Se il player può passare attraverso la nebbia, non mostrare warning
        if (canPlayerPassFog)
        {
            if (isPlayerInWarningZone)
            {
                isPlayerInWarningZone = false;
                OnPlayerExitedWarningZone?.Invoke();
                Debug.Log("Warning nascosto: player ora può attraversare la nebbia!");
            }
            return;
        }

        // Verifica che mainTilemap sia assegnata
        if (mainTilemap == null)
        {
            Debug.LogError("MainTilemap non assegnata in FogManager!");
            return;
        }

        // USA LA TILEMAP CORRETTA per la conversione
        // Se mainTilemap e fogTilemap sono diverse, usa fogTilemap per la nebbia
        Tilemap tilemapToUse = fogTilemap != null ? fogTilemap : mainTilemap;

        // Converti la posizione world in coordinate cella
        Vector3Int playerCellPos = tilemapToUse.WorldToCell(playerWorldPosition);

        // Calcola la distanza dal centro della warning zone
        float distanceFromWarningCenter = Vector3Int.Distance(playerCellPos, warningCenterCell);

        Debug.Log($"Player cell pos (using {tilemapToUse.name}): {playerCellPos}");
        Debug.Log($"Warning center cell: {warningCenterCell}");
        Debug.Log($"Distance from warning center: {distanceFromWarningCenter:F2}");
        Debug.Log($"Warning range: {warningMinDist:F1} - {warningMaxDist:F1}");

        // Determina se il player è nell'anello di warning
        bool playerShouldBeInWarningZone = (distanceFromWarningCenter >= warningMinDist &&
                                           distanceFromWarningCenter <= warningMaxDist);

        Debug.Log($"Should be in warning zone: {playerShouldBeInWarningZone}");
        Debug.Log($"Currently in warning zone: {isPlayerInWarningZone}");

        // Gestisci i cambiamenti di stato
        if (playerShouldBeInWarningZone && !isPlayerInWarningZone)
        {
            isPlayerInWarningZone = true;
            OnPlayerEnteredWarningZone?.Invoke();
            Debug.Log($"✓ PLAYER ENTRATO IN WARNING ZONE! Distanza: {distanceFromWarningCenter:F1}");

            if (OnPlayerEnteredWarningZone == null)
            {
                Debug.LogError("NESSUN LISTENER per OnPlayerEnteredWarningZone!");
            }
        }
        else if (!playerShouldBeInWarningZone && isPlayerInWarningZone)
        {
            isPlayerInWarningZone = false;
            OnPlayerExitedWarningZone?.Invoke();
            Debug.Log($"✓ PLAYER USCITO DA WARNING ZONE! Distanza: {distanceFromWarningCenter:F1}");
        }
    }

    public void SetPlayerCanPassFog(bool canPass)
    {
        bool previousValue = canPlayerPassFog;
        canPlayerPassFog = canPass;

        // Se il player ha appena acquisito la capacità di passare attraverso la nebbia
        // e era nella warning zone, nascondi immediatamente il warning
        if (canPass && !previousValue && isPlayerInWarningZone)
        {
            isPlayerInWarningZone = false;
            OnPlayerExitedWarningZone?.Invoke();

            if (Debug.isDebugBuild)
            {
                Debug.Log("Warning nascosto: player ora può passare attraverso la nebbia!");
            }
        }
    }

    public bool IsPlayerInWarningZone()
    {
        return isPlayerInWarningZone && !canPlayerPassFog;
    }

    public bool CanPlayerPassFog()
    {
        return canPlayerPassFog;
    }

    /// <summary>
    /// NUOVO METODO: Reset del sistema di warning (utile per debug o restart)
    /// </summary>
    [ContextMenu("Reset Warning Zone")]
    public void ResetWarningZone()
    {
        if (isPlayerInWarningZone)
        {
            isPlayerInWarningZone = false;
            OnPlayerExitedWarningZone?.Invoke();
        }
        canPlayerPassFog = false;

        if (Debug.isDebugBuild)
        {
            Debug.Log("Warning zone resettata!");
        }
    }
}