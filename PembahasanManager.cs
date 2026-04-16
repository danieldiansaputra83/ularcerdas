using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PembahasanManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform contentContainer; // Slot untuk objek 'Content' dari ScrollView
    public GameObject textTemplate;    // Slot untuk 'PembahasanItemTemplate'
    public Button backButton;          // Slot untuk tombol kembali

    void Start()
    {
        // 1. Ambil data riwayat dari QuizManager
        List<QuestionData> history = QuizManager.playedQuestionsHistory;

        // Pastikan template asli mati
        if (textTemplate != null) textTemplate.SetActive(false);

        // Cek jika tidak ada data
        if (history == null || history.Count == 0)
        {
            Debug.Log("Tidak ada soal yang dimainkan.");
            return;
        }

        // 2. Loop untuk menampilkan setiap soal
        int nomor = 1;
        foreach (QuestionData q in history)
        {
            // Duplikat template
            GameObject newItem = Instantiate(textTemplate, contentContainer);
            newItem.SetActive(true); // Hidupkan duplikatnya

            // Ambil komponen teks
            TMP_Text itemText = newItem.GetComponent<TMP_Text>();

            // Cari teks jawaban yang benar (A/B/C/D) berdasarkan index
            string jawabanBenarText = "Tidak ada data";
            if (q.pilihanJawaban.Length > q.indexJawabanBenar)
            {
                jawabanBenarText = q.pilihanJawaban[q.indexJawabanBenar];
            }

            // 3. Format Teks Pembahasan
            // Menggunakan Rich Text untuk warna (Soal Putih, Jawaban Hijau, Penjelasan Kuning)
            itemText.text = 
                $"<b>{nomor}. {q.teksSoal}</b>\n" +
                $"<color=#00FF00>Jawaban: {jawabanBenarText}</color>\n" +
                $"--------------------------------------------------";

            nomor++;
        }

        // 4. Fungsi Tombol Kembali
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                // Pastikan koneksi network mati sebelum kembali ke menu
                if (Unity.Netcode.NetworkManager.Singleton != null)
                {
                    Unity.Netcode.NetworkManager.Singleton.Shutdown();
                }
                SceneManager.LoadScene("Main Menu");
            });
        }
    }
}