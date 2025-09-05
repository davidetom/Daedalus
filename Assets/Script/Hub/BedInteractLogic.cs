using UnityEngine;

public class BedInteractLogic : MonoBehaviour
{
    [Header("Hub Controller")]
    public InnerHubController hub;

    public bool enableDebug = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (enableDebug)
                Debug.Log("Player entrato nell'area del letto");
            
            hub.OnPlayerEnterBedArea();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (enableDebug)
                Debug.Log("Player uscito dall'area del letto");
            
            hub.OnPlayerExitBedArea();
        }
    }
}
