using UnityEngine;

public class Coin : Collectible
{
    public override void OnCollect(PlayerController player)
    {
        if (player.coinsPicked < player.maxCoinNumber)
        {   

            player.coinsPicked++;
            if (AudioManager.Instance != null)
                {
                AudioManager.Instance.PlayCoinPickup();
                }
            Destroy(gameObject);

        }
    }
    
    public override void NotifyOnPick(GemSpawner gemSpawner)
    {
        // Notifica il coin generator
        DynamicCoinGenerator coinGen = FindFirstObjectByType<DynamicCoinGenerator>();
        if (coinGen != null)
            coinGen.OnCoinCollected(position);
        
        // Libera anche nel sistema globale
        GemSpawner.UnregisterOccupiedPosition(position);
        
    }
}
