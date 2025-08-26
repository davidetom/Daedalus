using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale; // salva la scala originale
    }

    // Quando premi il pulsante
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = originalScale * 0.9f; // rimpicciolisce il pulsante
    }

    // Quando rilasci il pulsante
    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale; // torna alla scala originale
    }
}
