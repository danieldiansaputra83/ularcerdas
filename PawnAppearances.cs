using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PawnAppearance : NetworkBehaviour
{
    public Sprite[] allPawnSprites; 

    // GANTI INI: Kita butuh referensi spesifik ke Image anak
    public Image pawnImageDisplay; 

    private NetworkVariable<int> pawnId = new NetworkVariable<int>(0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

// Ubah 'void' menjadi 'IEnumerator' agar bisa menunggu
    public override void OnNetworkSpawn()
    {
        // Mulai proses setup sebagai Coroutine
        StartCoroutine(SetupPawnRoutine());
    }

    private System.Collections.IEnumerator SetupPawnRoutine()
    {
        // --- TUNGGU SAMPAI MANAGER SIAP ---
        // Kita menunggu sampai 'GameplayManager.Instance' tidak null
        // dan 'boardTransform'-nya juga sudah terisi.
        while (GameplayManager.Instance == null || GameplayManager.Instance.boardTransform == null)
        {
            // Tunggu sebentar (satu frame) lalu cek lagi
            yield return null; 
        }

        // --- SETELAH MANAGER SIAP, BARU JALANKAN LOGIKA ---
        GameplayManager gm = GameplayManager.Instance;

        // 1. Jadikan anak dari Papan
        transform.SetParent(gm.boardTransform, false);
        transform.localScale = Vector3.one;

        // 2. Pindah ke Waypoint 0
        if (gm.waypoints.Count > 0)
        {
            transform.position = gm.waypoints[0].position;
        }
        
        Debug.Log("Pion berhasil terhubung dan mendarat di Papan!");

        // ... (Sisa kode event listener dan IsOwner tetap sama) ...
        pawnId.OnValueChanged += OnPawnIdChanged;
        OnPawnIdChanged(0, pawnId.Value);

        if (IsOwner)
        {
            int myPawn = PlayerPrefs.GetInt("MyPawnId", 0);
            TellHostMyPawnServerRpc(myPawn);
        }
    }
    
    [ServerRpc]
    private void TellHostMyPawnServerRpc(int selectedPawnId)
    {
        pawnId.Value = selectedPawnId;
    }

    private void OnPawnIdChanged(int oldId, int newId)
    {
        // Pastikan pawnImageDisplay sudah dihubungkan
        if (pawnImageDisplay != null && newId >= 0 && newId < allPawnSprites.Length)
        {
            pawnImageDisplay.sprite = allPawnSprites[newId];
        }
    }

    public override void OnNetworkDespawn()
    {
        pawnId.OnValueChanged -= OnPawnIdChanged;
    }
}