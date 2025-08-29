using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform player;   // Il Player da seguire
    public float zoom = 10f;   // Valore iniziale dello zoom
    public float zoomSpeed = 2f; // Velocità zoom
    public float minZoom = 5f;   // Zoom minimo
    public float maxZoom = 30f;  // Zoom massimo

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = zoom;
    }

    void LateUpdate()
    {
        if (player != null)
        {
            // Centra la minimappa sul Player
            Vector3 newPos = player.position;
            newPos.z = -10f; // fisso per la camera 2D
            transform.position = newPos;
        }

        // Gestione zoom con tasti (PC)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            zoom -= scroll * zoomSpeed;
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            cam.orthographicSize = zoom;
        }

        // Gestione zoom mobile (pinch)
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 prevTouch0 = touch0.position - touch0.deltaPosition;
            Vector2 prevTouch1 = touch1.position - touch1.deltaPosition;

            float prevMag = (prevTouch0 - prevTouch1).magnitude;
            float currentMag = (touch0.position - touch1.position).magnitude;

            float diff = currentMag - prevMag;

            zoom -= diff * 0.01f; // sensibilità pinch
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            cam.orthographicSize = zoom;
        }
    }
}
