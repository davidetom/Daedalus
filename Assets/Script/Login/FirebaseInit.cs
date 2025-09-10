using UnityEngine;
using Firebase;
using Firebase.Extensions;

public class FirebaseInit : MonoBehaviour
{
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                //Debug.Log("✅ Firebase inizializzato correttamente!");
            }
            else
            {
                //Debug.LogError("❌ Errore inizializzazione Firebase: " + task.Result.ToString());
            }
        });
    }
}