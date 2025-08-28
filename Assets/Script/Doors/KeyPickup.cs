using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Controlla se il player entra nel trigger
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            player.hasKey = true; // assegna la chiave
            Debug.Log("Hai raccolto una chiave!");
            Destroy(gameObject); // rimuove la chiave dalla scena
        }
    }
}
