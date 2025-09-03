// MazeCameraMovement.cs - MODIFICHE CONSIGLIATE

using System.Diagnostics;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public GameObject player;
    public float cameraOffset = 10f; // Cambiato nome variabile per consistenza

    // NUOVO: Riferimenti opzionali per controllo automatico
    [Header("Auto Management")]
    public bool autoFindPlayer = true;
    
    void Start()
    {
        // NUOVO: Trova automaticamente il player se necessario
        if (autoFindPlayer && player == null)
            player = GameObject.FindGameObjectWithTag("Player");
    }

    void LateUpdate() // CAMBIATO: Usa LateUpdate invece di Update per le telecamere
    {
        if (player == null) return;
        
        Vector3 pos = player.transform.position;
        pos.z = -cameraOffset; // CORRETTO: era - cameraOffSet
        transform.position = pos;
    }
    
    // NUOVO: Metodo pubblico per aggiornare il target
    public void SetTarget(GameObject newTarget)
    {
        player = newTarget;
    }
}