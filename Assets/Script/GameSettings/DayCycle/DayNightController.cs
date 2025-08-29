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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateLight(0);
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        player = playerObj.transform;
    }

    void Update()
    {
        transform.position = player.position;
    }

    public void UpdateLight(float ratio)
    {
        DayLight.color = DayLightGradient.Evaluate(ratio);
        NightLight.color = NightLightGradient.Evaluate(ratio);

        LightsHolder.rotation = Quaternion.Euler(0, 0, 360.0f * ratio);
    }
}
