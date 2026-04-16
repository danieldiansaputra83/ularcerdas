using System;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Collections.Generic;



public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    public static event Action OnFirebaseReady; // Event yang bisa didengarkan script lain
    public static bool IsFirebaseReady { get; private set; } = false;
    // Properti untuk mengakses Auth dan Firestore dari script lain
    public FirebaseAuth auth;
    public FirebaseFirestore db;
    public FirebaseUser user;

    void Awake()
    {
        // Pastikan hanya ada satu instance FirebaseManager
        if (Instance == null)
        {
            // Jika ini adalah instance pertama, jadikan ini sebagai Instance
            Instance = this;
            // Jangan hancurkan objek ini saat pindah scene
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Jika instance sudah ada, hancurkan duplikat ini
            Destroy(gameObject);
            return;
        }

        // Pindahkan logika inisialisasi ke Awake agar siap sebelum Start
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        // Dapatkan akses ke layanan Authentication dan Firestore
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            if (task.IsCompleted)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                Debug.Log("Firebase berhasil diinisialisasi.");

                IsFirebaseReady = true;
                OnFirebaseReady?.Invoke(); 
            }
            else
            {
                Debug.LogError("Gagal inisialisasi Firebase: " + task.Exception);
            }
        });
    }

    // Fungsi untuk mendaftarkan user baru
    public async Task<bool> RegisterUser(string email, string username, string password)
    {
        if (!IsFirebaseReady)
        {
            Debug.LogError("Firebase Auth belum siap.");
            return false;
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
        {
            Debug.LogError("Email, Password, atau Username tidak boleh kosong.");
            return false;
        }

        Debug.Log("--- Memulai proses registrasi ke Firebase ---"); // Pesan #1
        try
        {
            Debug.Log("Mencoba membuat user di Firebase Auth..."); // Pesan #2
            AuthResult authResult = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            Debug.Log("BERHASIL membuat user di Firebase Auth."); // Pesan #3

            FirebaseUser newUser = authResult.User;
            Debug.Log($"User ID Baru: {newUser.UserId}. Mencoba menyimpan ke Firestore..."); // Pesan #4

            DocumentReference docRef = db.Collection("users").Document(newUser.UserId);
            Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "username", username },
            { "email", email },
            { "level", 1 },
            { "xp", 0 },
            { "avatarId", 0 },
            { "pawnId", 0 }
        };
            await docRef.SetAsync(userData);

            Debug.Log("BERHASIL menyimpan data ke Firestore."); // Pesan #5
            return true;
        }
        catch (System.Exception ex) // Kita tangkap SEMUA jenis error
        {
            Debug.LogError($"!!! TERJADI ERROR SAAT REGISTRASI: {ex.Message}"); // Pesan #6
            Debug.LogError($"DETAIL ERROR LENGKAP: {ex}"); // Ini akan menampilkan detail error yang lebih lengkap
            return false;
        }
    }

    public async Task<bool> LoginUser(string email, string password)
    {
        if (auth == null)
        {
            Debug.LogError("Firebase Auth belum siap.");
            return false;
        }

        Debug.Log($"Mencoba login dengan email: {email}...");
        try
        {
            // Coba masuk dengan email dan password yang diberikan
            AuthResult loginResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
            FirebaseUser user = loginResult.User;

            Debug.Log($"Login BERHASIL! User ID: {user.UserId}");
            return true; // Login berhasil
        }
        catch (FirebaseException ex)
        {
            // Tangani jika ada error (misal: password salah, user tidak ditemukan)
            Debug.LogError($"Gagal login: {ex.Message}");
            return false; // Login gagal
        }
    }

    // Pastikan Anda juga memiliki fungsi SaveUserData ini di dalam script yang sama
    private async Task SaveUserData(string userId, string username, string email)
    {
        Debug.Log($"--- Memulai SaveUserData untuk user: {username} ---"); // LOG BARU
        try
        {
            DocumentReference docRef = db.Collection("users").Document(userId);
            Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "username", username },
            { "email", email },
            { "level", 1 },
            { "xp", 0 }
        };
            await docRef.SetAsync(userData);
            Debug.Log($"--- BERHASIL menyimpan data ke Firestore untuk user: {username} ---"); // LOG BARU
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"!!! ERROR DI DALAM FUNGSI SaveUserData: {ex.Message}"); // LOG BARU
        }
    }
void Start()
{
    FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
        if (task.IsCompleted)
        {
            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance; // Baris ini menginisialisasi Firestore

            // TAMBAHKAN LOG INI UNTUK MEMASTIKAN
            if (db != null)
            {
                Debug.Log("Firebase Firestore berhasil diinisialisasi.");
                IsFirebaseReady = true;
                OnFirebaseReady?.Invoke(); // Kirim sinyal bahwa Firebase sudah siap
            }
            else
            {
                Debug.LogError("PENTING: Firebase Firestore GAGAL diinisialisasi.");
            }
        }
        else
        {
            Debug.LogError("Gagal inisialisasi Firebase: " + task.Exception);
        }
    });
}
}