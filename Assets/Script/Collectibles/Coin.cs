using UnityEngine;

public class Coin : Collectible
{
    public override void OnCollect(PlayerController player)
    {
        if (player.coinsPicked < player.maxCoinNumber)
            {
                player.coinsPicked++;
                Destroy(gameObject);
            }
    }
}
