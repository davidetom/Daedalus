using UnityEngine;

public abstract class Collectible : MonoBehaviour
{
    // Ogni oggetto raccolto può fare qualcosa di specifico sul player
    public abstract void OnCollect(PlayerController player);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                OnCollect(player); // comportamento specifico
                Destroy(gameObject);
            }
        }
    }
}
