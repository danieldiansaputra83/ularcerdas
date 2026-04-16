using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    // Variabel statis untuk menyimpan referensi ke dirinya sendiri
    private static BackgroundMusic instance;

    void Awake()
    {
        // Cek apakah sudah ada objek BackgroundMusic lain di game?
        if (instance == null)
        {
            // Jika belum ada, jadikan ini sebagai yang utama
            instance = this;
            
            // MANTRA AJAIB: Jangan hancurkan objek ini saat pindah scene!
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            // Jika SUDAH ADA (misal saat kembali ke Main Menu),
            // Hancurkan objek baru ini agar musik tidak dobel
            Destroy(gameObject);
        }
    }
}