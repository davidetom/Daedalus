using Unity.VisualScripting;
using UnityEngine;
using System.Collections;

public class StartMessageUI : MonoBehaviour
{

    [Header("UI Reference")]
    public GameObject startMessageCanvas;
    public float messageDuration = 4f;
    [SerializeField] private GameObject gameUICanvas;
    [SerializeField] private GameObject gameButtons;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private GameObject saveUI;

    void Start()
    {
        if (SaveSystem.isNewGame)
        {
            HideStart();
            startMessageCanvas.SetActive(true);
            Debug.Log("START GAME MESSAGE ON");
            //Resetta il flag, così non resta attivo
            StartCoroutine(HideMessageAfterDelay());
            SaveSystem.isNewGame = false;
        }
        else
        {
            startMessageCanvas.SetActive(false);
            Debug.Log("START MESSAGE OFF");
        }
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        if (startMessageCanvas != null)
        {
            startMessageCanvas.SetActive(false);
            ShowStart();
        }
    }

    public void HideStart()
    {
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
        }

        if (gameButtons != null)
        {
            gameButtons.SetActive(false);
        }

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }

        if (saveUI != null)
        {
            saveUI.SetActive(false);
        }
    }

    public void ShowStart()
    {
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
        }

        if (gameButtons != null)
        {
            gameButtons.SetActive(true);
        }

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(true);
        }

        if (saveUI != null)
        {
            saveUI.SetActive(true);
        }
    }


}
