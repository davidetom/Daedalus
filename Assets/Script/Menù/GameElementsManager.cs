using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class GameElementsManager : MonoBehaviour
{
    [System.Serializable]
    public class DifficultyElement
    {
        public string elementName;
        public GameObject gameObjectToToggle;
        public bool enableOnEasy = true;
        public bool enableOnNormal = true;
        public bool enableOnHard = true;
    }

    [Header("Elements to Manage for Difficulty")]
    public List<DifficultyElement> elementsToManage = new List<DifficultyElement>();

    [Header("Specific Elements")]
    public Canvas minimap;

    private void Start()
    {
        Invoke(nameof(ConfigureGameElements), 0.1f);   
    }

    public void ConfigureGameElements()
    {
        if(DifficultyManager.Instance == null)
        {
            Debug.Log("DifficultyManager non trovato! Usando difficoltà Normal di default.");
            return;
        }

        DifficultyLevel currentDifficulty = DifficultyManager.Instance.GetCurrentDifficulty();
        Debug.Log($"Configurando elementi per difficolt�: {currentDifficulty}");

        //Gestisci elementi specifici
        ConfigureSpecificElements(currentDifficulty);

        foreach(var element in elementsToManage)
        {
            if(element.gameObjectToToggle != null)
            {
                bool shouldEnable = ShouldElementBeEnabled(element, currentDifficulty);
                element.gameObjectToToggle.SetActive(shouldEnable);
                Debug.Log($"Elemento '{element.elementName}' {(shouldEnable ? "abilitato" : "disabilitato")}");
            }
        }
    }

    private void ConfigureSpecificElements(DifficultyLevel difficulty)
    {
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                SetMinimapActive(true);
                break;

            case DifficultyLevel.Normal:
                SetMinimapActive(true);
                break;

            case DifficultyLevel.Hard:
                SetMinimapActive(false);
                break;
        }
    }

    private bool ShouldElementBeEnabled(DifficultyElement element, DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => element.enableOnEasy,
            DifficultyLevel.Normal => element.enableOnNormal,
            DifficultyLevel.Hard => element.enableOnHard,
            _ => true
        };
    }

    // Metodi per gestire elementi specifici
    private void SetMinimapActive(bool active)
    {
        if (minimap != null)
        {
            minimap.gameObject.SetActive(active);
            Debug.Log($"Minimappa {(active ? "abilitata" : "disabilitata")}");
        }
    }
}
