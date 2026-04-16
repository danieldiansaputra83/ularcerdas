using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Firebase.Firestore;

public class LeaderboardManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform container;
    public GameObject itemTemplate;
    public Button nextButton;
    public Sprite[] allAvatars;
    
    [Header("My Stats Header")]
    public Slider xpSlider;
    public TMP_Text levelText;
    public TMP_Text scoreGainText;

    void Start()
    {
        
        List<FinalResultData> results = new List<FinalResultData>(GameplayManager.finalResultsList);
        // Urutkan dari skor tertinggi
        results = results.OrderByDescending(x => x.score).ToList();

        itemTemplate.SetActive(false);
        string myUsername = PlayerPrefs.GetString("MyUsername", "");

        foreach (FinalResultData data in results)
        {
            GameObject newItem = Instantiate(itemTemplate, container);
            newItem.SetActive(true);

            // Gunakan LeaderboardItem.cs yang sudah disederhanakan
            LeaderboardItem itemScript = newItem.GetComponent<LeaderboardItem>();
            
            // 1. Cari Sprite Avatar
            Sprite avatarSprite = null;
            if (allAvatars != null && allAvatars.Length > 0)
            {
                avatarSprite = allAvatars[0]; // Default
                if (data.avatarId >= 0 && data.avatarId < allAvatars.Length)
                    avatarSprite = allAvatars[data.avatarId];
            }

            // 2. Tampilkan Data ke Item
            // (Pastikan script LeaderboardItem.cs Anda juga sudah yang versi 3 parameter)
            itemScript.SetupDisplay(avatarSprite, data.username, data.score);

            // 3. Logika Slider Header (Hanya untuk Saya)
            if (data.username == myUsername)
            {
                if (scoreGainText != null)
                {
                    scoreGainText.text = "+" + data.score;
                    
                    // Opsional: Hanya muncul jika skor > 0
                    scoreGainText.gameObject.SetActive(data.score > 0); 
                }
                
                if (xpSlider != null && levelText != null)
                {
                    // --- PERBAIKAN DI SINI (Harus 5 Argumen) ---
                    StartCoroutine(AnimateProgress(xpSlider, levelText, data.initialXP, data.initialLevel, data.score));
                }
                SaveProgressToFirebase(data.initialXP, data.initialLevel, data.score);
            }
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => SceneManager.LoadScene("PembahasanSoal"));
        }
    }
    
    // Fungsi Animasi (Meminta 5 parameter)
    IEnumerator AnimateProgress(Slider slider, TMP_Text levelText, int startXP, int startLevel, int scoreToAdd)
    {
        if (slider != null) slider.gameObject.SetActive(true);
        if (levelText != null) levelText.gameObject.SetActive(true);

        float duration = 2.0f; 
        float elapsed = 0f;
        
        // Hitung total XP
        int totalStartXP = (startLevel * 1000) + startXP;
        int totalTargetXP = totalStartXP + scoreToAdd;

        // Kecepatan animasi dinamis
        float pointsPerSecond = Mathf.Max(scoreToAdd, 200) / 1.5f; 
        int remainingScoreToAdd = scoreToAdd;
        int currentXP = startXP;
        int currentLevel = startLevel;
        int maxXPPerLevel = 1000;
        slider.maxValue = maxXPPerLevel;

        while (remainingScoreToAdd > 0)
        {
            int spaceInCurrentLevel = maxXPPerLevel - currentXP;
            int pointsToAddNow = Mathf.Min(remainingScoreToAdd, spaceInCurrentLevel);
            int targetXP = currentXP + pointsToAddNow;

            float t = 0;
            float startVal = currentXP;
            float endVal = targetXP;

            while (t < 1.0f)
            {
                t += Time.deltaTime * (pointsPerSecond / (endVal - startVal + 1));
                float val = Mathf.Lerp(startVal, endVal, t);
                
                slider.value = val;
                levelText.text = $"{Mathf.FloorToInt(val)}/1000 (Lv. {currentLevel})";
                yield return null;
            }

            currentXP = targetXP;
            remainingScoreToAdd -= pointsToAddNow;

            if (currentXP >= maxXPPerLevel)
            {
                slider.value = maxXPPerLevel;
                yield return new WaitForSeconds(0.1f);

                currentLevel++;
                currentXP = 0;
                slider.value = 0;
                levelText.text = $"0/1000 (Lv. {currentLevel})";
            }
        }

        slider.value = currentXP;
        levelText.text = $"{currentXP}/1000 (Lv. {currentLevel})";
    }

    private void SaveProgressToFirebase(int startXP, int startLevel, int scoreToAdd)
    {
        int totalXP = (startLevel * 1000) + startXP + scoreToAdd;
        int newLevel = totalXP / 1000;
        int newXP = totalXP % 1000;

        if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            DocumentReference docRef = FirebaseFirestore.DefaultInstance.Collection("users").Document(userId);

            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "level", newLevel },
                { "xp", newXP }
            };
            docRef.UpdateAsync(updates);
        }
    }
}