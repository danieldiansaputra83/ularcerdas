using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerCard : MonoBehaviour
{
    public Image avatarImage;
    public TMP_Text usernameText;
    public TMP_Text scoreText;

    // HAPUS variabel Slider dan logika isMe
    // Kita kembalikan ke fungsi update yang simpel
    public void UpdateDisplay(Sprite avatarSprite, string username, int score)
    {
        // --- DEBUGGING ---
        Debug.Log($"[CARD UPDATE] Nama: {username}, Skor Diterima: {score}");

        if (avatarImage != null) avatarImage.sprite = avatarSprite;
        if (usernameText != null) usernameText.text = username;
        
        // Cek Slot Score Text
        if (scoreText != null) 
        {
            scoreText.text = "Poin: " + score;
            // Paksa refresh layout (kadang teks panjang terpotong)
            scoreText.ForceMeshUpdate(); 
        }
        else
        {
            Debug.LogError($"[CARD ERROR] Slot 'Score Text' di Prefab {gameObject.name} masih KOSONG!");
        }
    }
}