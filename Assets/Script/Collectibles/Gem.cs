using UnityEngine;

public class Gem : Collectible
{
    public enum GemColor { Light, Night, Zombie, Blood, Fog }
    public GemColor gemColor;

    public override void OnCollect(PlayerController player)
    {
        switch (gemColor)
        {
            case GemColor.Light: player.hasLightGem = true; break;
            case GemColor.Night: player.hasNightGem = true; break;
            case GemColor.Zombie: player.hasZombieGem = true; break;
            case GemColor.Blood: player.hasBloodGem = true; break;
            case GemColor.Fog: player.hasFogGem = true; break;
        }

        Destroy(gameObject);
    }

    public override void NotifyOnPick(GemSpawner gemSpawner)
    {
        // MODIFICATO: Usa le coordinate array corrette
        Vector2Int arrayPos = mapManager != null ?
            mapManager.WorldToArrayCoordinates(transform.position) :
            new Vector2Int((int)transform.position.x, (int)transform.position.y);

        GemSpawner.UnregisterOccupiedPosition(arrayPos);

        if (gemSpawner != null)
        {
            switch (gemColor)
            {
                case GemColor.Light: gemSpawner.OnYellowGemCollected(); break;
                case GemColor.Night: gemSpawner.OnBlueGemCollected(); break;
                case GemColor.Zombie: gemSpawner.OnGreenGemCollected(); break;
                case GemColor.Blood: gemSpawner.OnRedGemCollected(); break;
                case GemColor.Fog: gemSpawner.OnGrayGemCollected(); break;
            }
        }
    }
}