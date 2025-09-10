using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using System.Threading.Tasks;

public class ProfileButtonManager : MonoBehaviour
{
    [Header("UI References")]
    public Button profileButton;        // L'icona login/profilo
    public TMP_Text buttonText;             // Testo sotto l'icona (Login / Profilo)
    public GameObject profilePanel;     // Pannello che mostra i dati utente
    public TMP_Text usernameText;           // Campo testo Username
    public TMP_Text emailText;              // Campo testo Email
    public Button logoutButton;         // Bottone Logout

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void OnEnable()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        auth.StateChanged += AuthStateChanged;

        profileButton.onClick.AddListener(OnProfileButtonClick);
        logoutButton.onClick.AddListener(OnLogoutClick);

        UpdateButton();
    }

    void OnDisable()
    {
        auth.StateChanged -= AuthStateChanged;
    }

    private void AuthStateChanged(object sender, System.EventArgs e)
    {
        UpdateButton();
    }

    void UpdateButton()
    {
        if (auth.CurrentUser == null)
        {
            buttonText.text = "Login";
            profilePanel.SetActive(false);
            logoutButton.gameObject.SetActive(false);
        }
        else
        {
            buttonText.text = "Profile";
            logoutButton.gameObject.SetActive(true);
        }
    }

    void OnProfileButtonClick()
    {
        if (auth.CurrentUser == null)
        {
            // Utente non loggato: vai alla scena Login
            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
            return;
        }

        // Utente loggato: mostra/nascondi pannello profilo
        profilePanel.SetActive(!profilePanel.activeSelf);

        if (profilePanel.activeSelf)
        {
            LoadUserProfile(auth.CurrentUser.UserId);
        }
    }

    async void LoadUserProfile(string userId)
    {
        try
        {
            DocumentReference docRef = db.Collection("users").Document(userId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                string username = snapshot.ContainsField("username") ? snapshot.GetValue<string>("username") : "N/A";
                string email = snapshot.ContainsField("email") ? snapshot.GetValue<string>("email") : "N/A";

                usernameText.text = $"Username: {username}";
                emailText.text = $"Email: {email}";
            }
            else
            {
                usernameText.text = "Errore: dati utente non trovati";
                emailText.text = "";
            }
        }
        catch (System.Exception e)
        {
            usernameText.text = "Errore caricamento dati";
            emailText.text = e.Message;
        }
    }

    void OnLogoutClick()
    {
        auth.SignOut();
        SaveSystem.ClearCurrentUserData();
        profilePanel.SetActive(false);
        UpdateButton();
    }
}