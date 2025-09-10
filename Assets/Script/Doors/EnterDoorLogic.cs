using UnityEngine;

public class EnterDoorLogic : MonoBehaviour
{
    [Header("Door Reference")]
    public DoorController door;

    private void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log("PLAYER IN FRON OF THE DOOR");
        if (other.CompareTag("Player"))
        {
            door.OnPlayerEnterArea();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            door.OnPlayerExitArea();
        }
    }
}
