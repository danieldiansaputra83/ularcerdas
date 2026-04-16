using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource bgmSource; // Referensi ke Audio Source

    void Start()
    {
        // Jika lupa mengisi di Inspector, script akan mencari sendiri
        if (bgmSource == null) 
            bgmSource = GetComponent<AudioSource>();
            
        bgmSource.Play(); // Mulai mainkan
    }

    // Fungsi untuk mematikan/menyalakan suara (Mute)
    public void ToggleMusic()
    {
        bgmSource.mute = !bgmSource.mute;
    }
}