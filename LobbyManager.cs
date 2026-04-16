using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks; // Penting untuk async
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Tombol Utama")]
    public Button createRoomButton;
    public Button joinRoomButton;

    [Header("UI Panel Gabung")]
    public GameObject joinRoomPanel;
    public TMP_InputField joinCodeInput;
    public Button confirmJoinButton;
    public Button backButton;
    public static string CurrentJoinCode { get; private set; }

    async void Start()
    {
        // Inisialisasi Unity Services dan login secara anonim
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Hubungkan fungsi ke tombol-tombol
        createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
        confirmJoinButton.onClick.AddListener(OnConfirmJoinClicked);
        backButton.onClick.AddListener(OnBackClicked);

        joinRoomPanel.SetActive(false); // Pastikan panel tersembunyi
    }

    private async void OnCreateRoomClicked()
    {
        Debug.Log("Membuat Ruang...");
        try
        {
            // 1. Buat Alokasi (Host) di Relay
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4); // 4 = maks pemain

            // 2. Dapatkan Kode Unik (Join Code)
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Ruang dibuat! Kode Unik: {joinCode}");

            // --- TAMBAHKAN BARIS INI UNTUK MENYIMPANNYA ---
            CurrentJoinCode = joinCode;

            // 3. Konfigurasi NetworkManager
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // 4. Mulai sebagai Host
            NetworkManager.Singleton.StartHost();

            // 5. Pindah ke Scene Game
            // Ganti "GameplayScene" dengan nama scene game Anda
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Gagal membuat ruang: {e.Message}");
        }
    }

    private void OnJoinRoomClicked()
    {
        // Tampilkan panel untuk memasukkan kode
        joinRoomPanel.SetActive(true);
        createRoomButton.gameObject.SetActive(false);
        joinRoomButton.gameObject.SetActive(false);
    }

    private async void OnConfirmJoinClicked()
    {
        string joinCode = joinCodeInput.text;
        Debug.Log($"Mencoba bergabung dengan kode: {joinCode}");

        try
        {
            // 1. Gabung Alokasi (Client) menggunakan kode
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // 2. Konfigurasi NetworkManager
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // 3. Mulai sebagai Client
            NetworkManager.Singleton.StartClient();

            // 4. Pindah ke Scene Game (akan di-handle oleh Host)
            // Kita tidak perlu LoadScene di sini, Netcode akan menyinkronkan scene secara otomatis
            Debug.Log("Berhasil terhubung ke host!");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Gagal bergabung ke ruang: {e.Message}");
        }
    }

    private void OnBackClicked()
    {
        // Sembunyikan panel gabung dan tampilkan tombol utama lagi
        joinRoomPanel.SetActive(false);
        createRoomButton.gameObject.SetActive(true);
        joinRoomButton.gameObject.SetActive(true);
    }
    public void GoToMainMenu()
    {
        // Perintah ini akan memuat MainMenuScene
        // Pastikan nama "MainMenuScene" sama persis dengan nama file scene Anda
        SceneManager.LoadScene("Main Menu");
    }
    
}