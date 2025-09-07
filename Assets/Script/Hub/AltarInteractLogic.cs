using UnityEngine;

public class AltarInteractLogic : MonoBehaviour
{
    [Header("Hub Controller")]
    public InnerHubController hub;

    public bool enableDebug = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (enableDebug)
                Debug.Log("Player entrato nell'area dell'altare");
            
            hub.OnPlayerEnterAltarArea();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (enableDebug)
                Debug.Log("Player uscito dall'area del letto");
            
            hub.OnPlayerExitAltarArea();
        }
    }
}
