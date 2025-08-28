using UnityEngine;
using System;

public class DoorController : MonoBehaviour
{

    public int doorID;
    private bool isOpen = false;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    public void TryOpen(PlayerController player)
    {
        if (isOpen) return;

        // Calcola quale porta è "quella giusta" oggi
        int correctDoor = GetDoorOfTheDay();

        if (player.hasKey && doorID == correctDoor)
        {
            OpenDoor();
        }
        else
        {
            Debug.Log("La porta " + doorID + " non si apre oggi!");
        }
    }

    void OpenDoor()
    {
        isOpen = true;
        spriteRenderer.enabled = false;   // sparisce graficamente
        boxCollider.enabled = false;      // disabilita il muro
        gameObject.SetActive(false);
        Debug.Log("Porta " + doorID + " aperta!");
    }

    public bool IsOpen()
    {
        return isOpen;
    }
    
    // Logica semplificata per scegliere la porta del giorno
    int GetDoorOfTheDay()
    {
        int day = DateTime.Now.Day; // giorno del mese
        return (day % 8) + 1;       // esempio: cicla tra 1 e 8
    }

}

