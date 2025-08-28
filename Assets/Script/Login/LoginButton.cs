using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginButton : MonoBehaviour
{
    public void GoToLogin()
    {
        SceneManager.LoadScene("LoginScene"); // scrivi il nome esatto della tua scena
    }
}
