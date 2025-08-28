using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections; // <-- necessario per IEnumerator

public class FirebaseAuthManager : MonoBehaviour
{
    public InputField usernameInput;
    public InputField emailInput;
    public InputField passwordInput;
    public Text messageText;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
    }

    // --- LOGIN ---
    public void Login()
    {
        string username = usernameInput.text.Trim();
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
        {
            ShowMessage("⚠️ Enter username, email, and password", Color.red);
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    ShowMessage("❌ Invalid credentials", Color.red);
                    return;
                }

                FirebaseUser user = task.Result.User;

                DocumentReference docRef = db.Collection("users").Document(user.UserId);
                docRef.GetSnapshotAsync().ContinueWithOnMainThread(snapshotTask =>
                {
                    if (snapshotTask.IsFaulted || snapshotTask.IsCanceled)
                    {
                        ShowMessage("❌ Failed to retrieve user data", Color.red);
                        auth.SignOut();
                        return;
                    }

                    DocumentSnapshot snapshot = snapshotTask.Result;

                    if (snapshot.Exists && snapshot.ContainsField("username"))
                    {
                        string savedUsername = snapshot.GetValue<string>("username");

                        if (savedUsername.Equals(username, StringComparison.OrdinalIgnoreCase))
                        {
                            ShowMessage("✅ Login successful! Welcome " + savedUsername, Color.green);

                            docRef.UpdateAsync("lastLogin", Timestamp.GetCurrentTimestamp());

                            // Attendi 2 secondi, poi vai al MainMenu
                            StartCoroutine(LoadSceneWithDelay("MainMenu", 2f));
                        }
                        else
                        {
                            ShowMessage("❌ Invalid username for this email address", Color.red);
                            auth.SignOut();
                        }
                    }
                    else
                    {
                        ShowMessage("❌ Failed to retrieve user data", Color.red);
                        auth.SignOut();
                    }
                });
            });
    }

    // --- REGISTRAZIONE ---
    public void Register()
    {
        string username = usernameInput.text.Trim();
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowMessage("⚠️ Enter username, email, and password", Color.red);
            return;
        }

        db.Collection("users").WhereEqualTo("username", username)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(usernameTask =>
            {
                if (usernameTask.IsFaulted || usernameTask.IsCanceled)
                {
                    ShowMessage("❌ Username verification error", Color.red);
                    return;
                }

                if (usernameTask.Result.Count > 0)
                {
                    ShowMessage("❌ Username is already taken", Color.red);
                    return;
                }

                auth.CreateUserWithEmailAndPasswordAsync(email, password)
                    .ContinueWithOnMainThread(task =>
                    {
                        if (task.IsFaulted || task.IsCanceled)
                        {
                            FirebaseException fbEx = task.Exception?.Flatten().InnerExceptions[0] as FirebaseException;
                            AuthError errorCode = (AuthError)fbEx.ErrorCode;

                            switch (errorCode)
                            {
                                case AuthError.EmailAlreadyInUse:
                                    ShowMessage("❌ Email is already taken", Color.red);
                                    break;
                                case AuthError.InvalidEmail:
                                    ShowMessage("❌ Invalid email format", Color.red);
                                    break;
                                case AuthError.WeakPassword:
                                    ShowMessage("❌ Password too weak (min 6 characters)", Color.red);
                                    break;
                                default:
                                    ShowMessage("❌ Registration failed: " + errorCode, Color.red);
                                    break;
                            }
                            return;
                        }

                        FirebaseUser user = task.Result.User;

                        var userData = new
                        {
                            username = username,
                            email = email,
                            victories = 0,
                            lastLogin = Timestamp.GetCurrentTimestamp()
                        };

                        db.Collection("users").Document(user.UserId).SetAsync(userData)
                            .ContinueWithOnMainThread(saveTask =>
                            {
                                if (saveTask.IsCompleted)
                                {
                                    ShowMessage("✅ Registration successful!", Color.green);
                                    StartCoroutine(LoadSceneWithDelay("MainMenu", 2f)); // attesa 2 secondi
                                }
                                else
                                {
                                    ShowMessage("⚠️ Registered but error saving data", Color.yellow);
                                }
                            });
                    });
            });
    }

    // --- GIOCA COME OSPITE ---
    public void PlayAsGuest()
    {
        ShowMessage("✅ Playing as guest", Color.green);
        StartCoroutine(LoadSceneWithDelay("MainMenu", 1.5f));
    }

    // --- Coroutine per ritardare il cambio scena ---
    private IEnumerator LoadSceneWithDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    // --- MOSTRA MESSAGGIO ---
    private void ShowMessage(string msg, Color color)
    {
        if (messageText != null)
        {
            messageText.text = msg;
            messageText.color = color;
        }
        else
        {
            Debug.Log(msg);
        }
    }
}
