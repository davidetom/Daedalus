using System.Diagnostics;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public GameObject player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = player.transform.position;
        pos.z = -10f;
        transform.position = pos;
    }
}
