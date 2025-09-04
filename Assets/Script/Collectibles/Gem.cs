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
    }
}