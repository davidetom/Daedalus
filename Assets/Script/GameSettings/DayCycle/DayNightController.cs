using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightController : MonoBehaviour
{
    public Transform LightsHolder;

    public Light2D DayLight;
    public Gradient DayLightGradient;

    public Light2D NightLight;
    public Gradient NightLightGradient;

    public Transform player;
    
    [Header("Hub Settings")]
    public Vector3 defaultHubPosition = new Vector3(155f, 155f, 0f);
    private HubController hubController;

    void Start()
    {
        UpdateLight(0);
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        player = playerObj.transform;
        
        // Trova il HubController
        hubController = FindFirstObjectByType<HubController>();
    }

    void Update()
    {
        // Se il player è nell'hub, usa la posizione di default
        if (hubController != null && hubController.IsPlayerInHub())
        {
            transform.position = defaultHubPosition;
        }
        else
        {
            // Altrimenti segui il player normalmente
            transform.position = player.position;
        }
    }

    public void UpdateLight(float ratio)
    {
        DayLight.color = DayLightGradient.Evaluate(ratio);
        NightLight.color = NightLightGradient.Evaluate(ratio);

        LightsHolder.rotation = Quaternion.Euler(0, 0, 360.0f * ratio);
    }
}