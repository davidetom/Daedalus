using UnityEngine;

public class RefillCoins : MonoBehaviour
{
    public PlayerController playerController;
    void Start()
    {
        if (!SaveSystem.isNewGame && playerController.CoinsRefilled)
            Destroy(gameObject);
    }
    
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (playerController != null)
        {
            playerController.coinsPicked += 5000;
        }
        Destroy(gameObject);
    }
}
