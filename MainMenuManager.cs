using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text usernameText;
    public Button levelButton; 
    public TMP_Text levelText; 
    public TMP_Text xpText;    
    public Slider xpSlider;    

    [Header("Avatar Selection")]
    public Image profilePictureImage;
    public Button changePictureButton;
    public GameObject avatarSelectionPanel;
    public GameObject avatarButtonTemplate;
    public Transform avatarGridContainer;
    private Sprite[] availableAvatars;

    [Header("Pawn Selection")]
    public Image selectedPawnImage;
    public Button changePawnButton;
    public GameObject pawnSelectionPanel;
    public GameObject pawnButtonTemplate;
    public Transform pawnGridContainer;
    private Sprite[] availablePawns;

    [Header("Change Name UI")]
    public GameObject changeNamePanel;
    public TMP_InputField newNameInput;
    public Button saveNameButton;
    public Button usernameButton;
    public Button cancelNameButton;

    [Header("Level Selection")]
    public GameObject levelSelectionPanel;

    // --- Variabel Class ---
    private FirebaseUser user;
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Start()
    {
        // 1. Muat aset lokal
        LoadAvatarsFromResources();
        LoadPawnsFromResources();
        PopulateAvatarSelectionGrid();
        PopulatePawnSelectionGrid();

        // 2. Hubungkan UI statis
        changePictureButton.onClick.AddListener(OpenAvatarSelectionPanel);
        changePawnButton.onClick.AddListener(OpenPawnSelectionPanel);
        
        if (usernameButton != null) usernameButton.onClick.AddListener(OpenChangeNamePanel);
        if (saveNameButton != null) saveNameButton.onClick.AddListener(SaveNewUsername);
        if (cancelNameButton != null) cancelNameButton.onClick.AddListener(CloseChangeNamePanel);
        
        // Tambahan: Logika tombol level untuk membuka panel level (jika ada)
        if (levelButton != null) levelButton.onClick.AddListener(() => {
            if (levelSelectionPanel != null) levelSelectionPanel.SetActive(true);
        });

        // Sembunyikan panel di awal
        if (avatarSelectionPanel != null) avatarSelectionPanel.SetActive(false);
        if (pawnSelectionPanel != null) pawnSelectionPanel.SetActive(false);
        if (changeNamePanel != null) changeNamePanel.SetActive(false);
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);

        // 3. Inisialisasi Firebase
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);

        // --- LOGIKA LEVEL DEFAULT (Offline/Belum Login) ---
        int currentLevel = PlayerPrefs.GetInt("SelectedLevel", 0);
        if (currentLevel == 0)
        {
            currentLevel = 1;
            PlayerPrefs.SetInt("SelectedLevel", 1);
            PlayerPrefs.Save();
        }
        if (levelText != null) levelText.text = "Level " + currentLevel;
    }

    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = (auth.CurrentUser != null);
            if (!signedIn && user != null)
            {
                SceneManager.LoadScene("Login");
            }
            
            user = auth.CurrentUser;
            if (signedIn)
            {
                LoadUserData(); // Panggil fungsi load data
            }
        }
    }

    // --- FUNGSI LOAD DATA UTAMA ---
    private async void LoadUserData()
    {
        if (user == null) return;

        DocumentReference docRef = db.Collection("users").Document(user.UserId);
        
        try
        {
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                // 1. Username
                if (snapshot.TryGetValue("username", out object usernameObject))
                {
                    usernameText.text = usernameObject.ToString();
                    PlayerPrefs.SetString("MyUsername", usernameObject.ToString());
                }

                // 2. Level & XP
                // Kita gunakan nama variabel yang unik agar tidak bentrok
                long finalLevel = 1; 
                long finalXP = 0;

                // Ambil data dari database
                if (snapshot.TryGetValue("level", out object levelObj))
                {
                    finalLevel = System.Convert.ToInt64(levelObj);
                }
                if (snapshot.TryGetValue("xp", out object xpObj))
                {
                    finalXP = System.Convert.ToInt64(xpObj);
                }

                // Hitung jika XP berlebih (Naik Level)
                while (finalXP >= 1000)
                {
                    finalLevel++;
                    finalXP -= 1000;
                    // (Opsional: Di sini bisa tambah kode untuk update balik ke Firebase)
                }

                // UPDATE UI
                if (levelText != null) levelText.text = "Level " + finalLevel;
                // Jika tombol level menampilkan teks juga
                if (levelButton != null) 
                {
                    TMP_Text btnTxt = levelButton.GetComponentInChildren<TMP_Text>();
                    if(btnTxt != null) btnTxt.text = "Level " + finalLevel;
                }
                
                if (xpSlider != null)
                {
                    xpSlider.maxValue = 1000;
                    xpSlider.value = finalXP;
                }
                if (xpText != null) xpText.text = $"{finalXP} / 1000";

                // SIMPAN KE PLAYERPREFS (Agar GameplayScene tahu)
                PlayerPrefs.SetInt("MyLevel", (int)finalLevel);
                PlayerPrefs.SetInt("MyXP", (int)finalXP);
                
                // --- INI PERBAIKAN AGAR LEVEL OTOMATIS TERPILIH ---
                // Paksa SelectedLevel mengikuti level asli pemain
                PlayerPrefs.SetInt("SelectedLevel", (int)finalLevel);
                PlayerPrefs.Save();
                // --------------------------------------------------

                Debug.Log($"Data Loaded: Level {finalLevel}, XP {finalXP}");

                // 3. Avatar
                if (snapshot.TryGetValue("avatarId", out object avatarIdObject))
                {
                    int avatarId = System.Convert.ToInt32(avatarIdObject);
                    if (avatarId >= 0 && avatarId < availableAvatars.Length)
                    {
                        profilePictureImage.sprite = availableAvatars[avatarId];
                        PlayerPrefs.SetInt("MyAvatarId", avatarId);
                    }
                }
                
                // 4. Pion
                if (snapshot.TryGetValue("pawnId", out object pawnIdObject))
                {
                    int pawnId = System.Convert.ToInt32(pawnIdObject);
                    if (pawnId >= 0 && pawnId < availablePawns.Length)
                    {
                        selectedPawnImage.sprite = availablePawns[pawnId];
                        PlayerPrefs.SetInt("MyPawnId", pawnId);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Gagal memuat data: {ex.Message}");
        }
    }

    // --- FUNGSI TOMBOL & HELPER ---
    
    public void OpenChangeNamePanel() { if (changeNamePanel != null) { changeNamePanel.SetActive(true); if (newNameInput != null) newNameInput.text = usernameText.text; } }
    public void CloseChangeNamePanel() { if (changeNamePanel != null) changeNamePanel.SetActive(false); }
    
    public async void SaveNewUsername()
    {
        string newName = newNameInput.text.Trim();
        if (string.IsNullOrEmpty(newName)) return;
        usernameText.text = newName;
        PlayerPrefs.SetString("MyUsername", newName);
        if (user != null) {
            DocumentReference docRef = db.Collection("users").Document(user.UserId);
            await docRef.UpdateAsync("username", newName);
        }
        if (changeNamePanel != null) changeNamePanel.SetActive(false);
    }

    public void OnMulaiBermainClicked() { SceneManager.LoadScene("Lobby"); }
    public void Logout() { auth.SignOut(); SceneManager.LoadScene("Login Page"); }

    // Fungsi Pilihan Level Manual (Jika pemain ingin main level bawah)
    public void SelectLevel(int level)
    {
        Debug.Log($"Level {level} dipilih manual.");
        PlayerPrefs.SetInt("SelectedLevel", level);
        PlayerPrefs.Save();
        if (levelText != null) 
        {
            levelText.text = "Level " + level; 
        }

        // Update Teks di Tombol (jika tombolnya menampilkan level juga)
        if (levelButton != null)
        {
            TMP_Text btnText = levelButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = "Level " + level;
            }
        }
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
    }

    void LoadAvatarsFromResources() { availableAvatars = Resources.LoadAll<Sprite>("Avatars"); }
    void LoadPawnsFromResources() { availablePawns = Resources.LoadAll<Sprite>("Pawns"); }

    void PopulateAvatarSelectionGrid() { foreach (Transform child in avatarGridContainer) Destroy(child.gameObject); for (int i = 0; i < availableAvatars.Length; i++) { int index = i; GameObject btn = Instantiate(avatarButtonTemplate, avatarGridContainer); btn.GetComponent<Image>().sprite = availableAvatars[i]; btn.GetComponent<Button>().onClick.AddListener(() => SelectAvatar(index)); } }
    void PopulatePawnSelectionGrid() { foreach (Transform child in pawnGridContainer) Destroy(child.gameObject); for (int i = 0; i < availablePawns.Length; i++) { int index = i; GameObject btn = Instantiate(pawnButtonTemplate, pawnGridContainer); btn.GetComponent<Image>().sprite = availablePawns[i]; btn.GetComponent<Button>().onClick.AddListener(() => SelectPawn(index)); } }
    
    public void OpenAvatarSelectionPanel() { avatarSelectionPanel.SetActive(true); }
    public async void SelectAvatar(int index) { 
        profilePictureImage.sprite = availableAvatars[index]; 
        avatarSelectionPanel.SetActive(false); 
        PlayerPrefs.SetInt("MyAvatarId", index);
        if(user!=null) await db.Collection("users").Document(user.UserId).UpdateAsync("avatarId", index);
    }
    
    public void OpenPawnSelectionPanel() { pawnSelectionPanel.SetActive(true); }
    public async void SelectPawn(int index) { 
        selectedPawnImage.sprite = availablePawns[index]; 
        pawnSelectionPanel.SetActive(false);
        PlayerPrefs.SetInt("MyPawnId", index);
        if(user!=null) await db.Collection("users").Document(user.UserId).UpdateAsync("pawnId", index);
    }
}