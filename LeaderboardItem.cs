using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardItem : MonoBehaviour
{
    [Header("Komponen UI")]
    public Image avatarImage;
    public TMP_Text usernameText;
    public TMP_Text scoreText;

    // Fungsi Setup yang BARU dan SEDERHANA (Hanya 3 Parameter)
    public void SetupDisplay(Sprite avatar, string username, int score)
    {
        // 1. Isi Data
        if (avatarImage != null) avatarImage.sprite = avatar;
        if (usernameText != null) usernameText.text = username;
        if (scoreText != null) scoreText.text = score + " Poin";
        
        // Pastikan background putih bersih (netral)
        if (GetComponent<Image>() != null)
            GetComponent<Image>().color = Color.white; 
    }
}