using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;

public class LoginManager : MonoBehaviour
{
    [Header("UI Login")]
    public GameObject loginUIPanel; // Panel yang membungkus input email/pass (agar bisa disembunyikan)
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    
    [Header("UI Register")]
    public Button openRegisterButton; // Tombol "Daftar Disini"
    public GameObject registerPanel;  // <--- SLOT INI KEMBALI ADA
    public Button closeRegisterButton; // Tombol "Kembali/Tutup" di dalam panel register

    [Header("UI Error Popup")]
    public GameObject errorPopupPanel;
    public TMP_Text errorText;
    public Button closePopupButton;

    private FirebaseAuth auth;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        // --- LISTENER LOGIN ---
        if(loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
        
        // --- LISTENER REGISTER (LOGIKA PANEL) ---
        if(openRegisterButton != null) 
        {
            openRegisterButton.onClick.AddListener(() => {
                // Buka Panel Register, Sembunyikan Login
                if(registerPanel != null) registerPanel.SetActive(true);
                if(loginUIPanel != null) loginUIPanel.SetActive(false);
            });
        }

        if(closeRegisterButton != null)
        {
            closeRegisterButton.onClick.AddListener(() => {
                // Tutup Panel Register, Munculkan Login Kembali
                if(registerPanel != null) registerPanel.SetActive(false);
                if(loginUIPanel != null) loginUIPanel.SetActive(true);
            });
        }

        // --- LISTENER POPUP ERROR ---
        if(closePopupButton != null) closePopupButton.onClick.AddListener(CloseErrorPopup);

        // Pastikan Panel Register & Error mati di awal
        if(registerPanel != null) registerPanel.SetActive(false);
        if(errorPopupPanel != null) errorPopupPanel.SetActive(false);
        // Pastikan Login Panel nyala
        if(loginUIPanel != null) loginUIPanel.SetActive(true);
    }

    private async void OnLoginClicked()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("Email dan Password tidak boleh kosong!");
            return;
        }

        loginButton.interactable = false;

        try
        {
            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            Debug.Log($"Login Berhasil: {result.User.DisplayName}");
            SceneManager.LoadScene("MainMenuScene");
        }
        catch (System.Exception ex)
        {
            HandleFirebaseError(ex);
        }
        finally
        {
            if(loginButton != null) loginButton.interactable = true;
        }
    }

    private void HandleFirebaseError(System.Exception ex)
    {
        string message = "Terjadi kesalahan.";
        FirebaseException firebaseEx = ex as FirebaseException;
        
        if (firebaseEx == null && ex.InnerException is FirebaseException)
            firebaseEx = ex.InnerException as FirebaseException;

        if (firebaseEx != null)
        {
            var errorCode = (AuthError)firebaseEx.ErrorCode;
            switch (errorCode)
            {
                case AuthError.WrongPassword: message = "Password salah."; break;
                case AuthError.UserNotFound: message = "Akun tidak ditemukan."; break;
                case AuthError.InvalidEmail: message = "Email tidak valid."; break;
                case AuthError.NetworkRequestFailed: message = "Cek koneksi internet."; break;
                default: message = ex.Message; break;
            }
        }
        else
        {
            message = ex.Message;
        }
        ShowError(message);
    }

    void ShowError(string message)
    {
        if (errorPopupPanel != null)
        {
            errorText.text = message;
            errorPopupPanel.SetActive(true);
        }
        else
        {
            Debug.LogError(message);
        }
    }

    void CloseErrorPopup()
    {
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
    }
}