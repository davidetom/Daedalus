using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI Riferimenti")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;
    public Image fillImage; // immagine di riempimento della barra
    public RectTransform hpBarTransform; // il RectTransform della barra

    [Header("Valori Vita")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Colori")]
    public Color fullHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public Color damageFlashColor = Color.red;
    private Color originalColor;

    [Header("Effetti")]
    public float flashDuration = 0.2f;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 5f;

    void Start()
    {
        currentHealth = maxHealth;
        hpSlider.maxValue = maxHealth;
        hpSlider.value = currentHealth;
        originalColor = fillImage.color;
        UpdateUI();
    }

    //TEST DANNO E CURA
    void Update()
    {
    // Premi H per subire 10 danni
    if (Input.GetKeyDown(KeyCode.H))
    {
        TakeDamage(10);
    }

    // Premi J per curarti di 10
    if (Input.GetKeyDown(KeyCode.J))
    {
        Heal(10);
    }
}


    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        UpdateUI();

        // Attiva effetti
        StartCoroutine(FlashDamage());
        StartCoroutine(ShakeBar());
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateUI();
    }

    private void UpdateUI()
    {
        hpSlider.value = currentHealth;
        hpText.text = currentHealth + " / " + maxHealth;

        float healthPercent = (float)currentHealth / maxHealth;

        // Colore dinamico in base alla percentuale
        if (healthPercent > 0.5f)
        {
            fillImage.color = Color.Lerp(midHealthColor, fullHealthColor, (healthPercent - 0.5f) * 2f);
        }
        else
        {
            fillImage.color = Color.Lerp(lowHealthColor, midHealthColor, healthPercent * 2f);
        }
    }

    private IEnumerator FlashDamage()
    {
        fillImage.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);
        UpdateUI(); // ripristina il colore corretto in base alla vita
    }

    private IEnumerator ShakeBar()
    {
        Vector3 originalPos = hpBarTransform.localPosition;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            hpBarTransform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        hpBarTransform.localPosition = originalPos; // resetta posizione
    }
}
