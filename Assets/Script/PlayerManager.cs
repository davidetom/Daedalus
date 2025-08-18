using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int coinsPicked = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Currency"))
        {
            coinsPicked++;
            Destroy(other);
        }
    }
}
