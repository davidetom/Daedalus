using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Difficulty Settings")]
    public DifficultyLevel currentDifficulty = DifficultyLevel.Normal;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetDifficulty(DifficultyLevel difficulty)
    {
        currentDifficulty = difficulty;
        Debug.Log($"Difficoltà impostata a: {difficulty}");
    }

    public void SetDifficulty(int difficultyIndex)
    {
        currentDifficulty = (DifficultyLevel)difficultyIndex;
        Debug.Log($"Difficoltà impostata a: {currentDifficulty}");
    }

    public DifficultyLevel GetCurrentDifficulty()
    {
        return currentDifficulty;
    }

    //Metodi per controllare la difficoltà
    public bool IsEasy() => currentDifficulty == DifficultyLevel.Easy;
    public bool IsNormal() => currentDifficulty == DifficultyLevel.Normal;
    public bool IsHard() => currentDifficulty == DifficultyLevel.Hard;

    //PROVA
    #region SAVE AND LOAD

    public void Save(ref DifficultyData data)
    {
        data.difficultyLevel = (int)currentDifficulty;
    }

    public void Load(DifficultyData data)
    {
        currentDifficulty = (DifficultyLevel)data.difficultyLevel;
    }

    #endregion
}

//ENUM per le difficoltà
public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}

//PROVA
[System.Serializable]
public struct DifficultyData
{
    public int difficultyLevel;
}
