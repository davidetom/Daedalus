using UnityEngine;

public class Coin : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            if (player.coinsPicked < player.maxCoinNumber)
            {
                player.coinsPicked++;
                Destroy(gameObject);
            }
        }
    }
}
