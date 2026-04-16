using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class QuizManager : MonoBehaviour
{
    [Header("UI Elements (Pemain Aktif)")]
    public GameObject quizPanel;
    public TMP_Text questionText;
    public TMP_Text quizTimerText;

    [Header("UI Elements (Penonton)")]
    public GameObject quizPanelOther;
    public TMP_Text quizTimerTextOther;
    public TMP_Text questionTextOther;
    
    [Header("Dynamic Answers")]
    public Transform answerContainer; 
    public GameObject answerButtonPrefab; 
    
    [Header("Settings")]
    public Color defaultColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    // Time limit diambil dari GameplayManager, variabel ini hanya visual setting jika perlu
    public int timeLimitSeconds = 60; 

    // Data Internal
    public List<QuestionData> allQuestions;
    private QuestionData currentQuestion;
    private List<GameObject> activeButtons = new List<GameObject>();
    public static List<QuestionData> playedQuestionsHistory = new List<QuestionData>();

    void Start()
    {
        playedQuestionsHistory.Clear();
        CloseAllPanels();
    }

    public void InitializeQuestions(int level)
    {
        string path = "Soal/Level" + level;
        List<QuestionData> loadedQuestions = Resources.LoadAll<QuestionData>(path).ToList();
        allQuestions = loadedQuestions.OrderBy(q => q.name).ToList();

        if (allQuestions.Count == 0) 
        {
            Debug.LogError($"GAWAT! Tidak ada file soal di: Resources/{path}");
        }
        else
        {
            Debug.Log($"Sukses memuat {allQuestions.Count} soal untuk Level {level}.");
        }
    }

    // --- FUNGSI UNTUK PENONTON ---
    public void ShowWaitingPanel()
    {
        if (quizPanelOther != null) 
        {
            quizPanelOther.SetActive(true);
            // REVISI: Hapus StartCoroutine(QuizTimerRoutine) karena timer sekarang jalan di Update()
        }
    }

    // --- FUNGSI UNTUK PEMAIN AKTIF ---
    public void ShowSpecificQuestion(int index, bool isMe)
    {
        if (allQuestions == null || index >= allQuestions.Count) return;

        currentQuestion = allQuestions[index];
        if (isMe) playedQuestionsHistory.Add(currentQuestion);

        CloseAllPanels();
        
        // REVISI: Hapus StopAllCoroutines karena kita tidak pakai Coroutine timer lagi
        // StopAllCoroutines(); 

        if (isMe)
        {
            quizPanel.SetActive(true);
            if (questionText != null) questionText.text = currentQuestion.teksSoal;
            GenerateButtons(); 
        }
        else
        {
            quizPanelOther.SetActive(true);
            if (questionTextOther != null) 
            {
                questionTextOther.text = currentQuestion.teksSoal; 
            }
        }
        
        // REVISI: Hapus StartCoroutine(QuizTimerRoutine) di sini juga
    }

    void GenerateButtons()
    {
        ClearAnswerButtons();
        for (int i = 0; i < currentQuestion.pilihanJawaban.Length; i++)
        {
            GameObject newButtonObj = Instantiate(answerButtonPrefab, answerContainer);
            activeButtons.Add(newButtonObj);
            
            Button btnComponent = newButtonObj.GetComponent<Button>();
            TMP_Text btnText = newButtonObj.GetComponentInChildren<TMP_Text>();

            btnText.text = currentQuestion.pilihanJawaban[i];
            btnComponent.image.color = defaultColor;

            int indexPilihan = i; 
            btnComponent.onClick.AddListener(() => OnAnswerSelected(indexPilihan));
        }
    }

    // --- LOGIKA TIMER (DI UPDATE) ---
    void Update()
    {
        // Hanya update jika salah satu panel kuis aktif
        if (quizPanel.activeSelf || quizPanelOther.activeSelf)
        {
            float timeLeft = 0;
            
            // REVISI: Mengambil waktu dari 'turnTimer' milik GameplayManager
            if (GameplayManager.Instance != null)
            {
                // PERBAIKAN UTAMA DI SINI (Ganti networkTimer jadi turnTimer)
                timeLeft = GameplayManager.Instance.turnTimer.Value; 
            }

            string timeString = $"00:{Mathf.CeilToInt(timeLeft).ToString("00")}";

            if (quizTimerText != null) quizTimerText.text = timeString;
            if (quizTimerTextOther != null) quizTimerTextOther.text = timeString;

            if (timeLeft <= 5f)
            {
                if (quizTimerText != null) quizTimerText.color = Color.red;
            }
            else
            {
                if (quizTimerText != null) quizTimerText.color = Color.white;
            }
        }
    }

    // --- LOGIKA JAWABAN ---
    
    void ClearAnswerButtons()
    {
        foreach (GameObject btn in activeButtons) Destroy(btn);
        activeButtons.Clear();
    }

    void OnAnswerSelected(int indexPilihan)
    {
        // Kunci semua tombol
        foreach (GameObject btnObj in activeButtons)
        {
            btnObj.GetComponent<Button>().interactable = false;
        }

        bool isCorrect = (indexPilihan == currentQuestion.indexJawabanBenar);
        Image selectedBtnImage = activeButtons[indexPilihan].GetComponent<Image>();

        if (isCorrect)
        {
            selectedBtnImage.color = correctColor;
            
            // Tambah Skor ke Server
            if (GameplayManager.Instance != null) 
                GameplayManager.Instance.AddScoreServerRpc(currentQuestion.bobotNilai);
        }
        else
        {
            selectedBtnImage.color = wrongColor;
            
            if (currentQuestion.indexJawabanBenar < activeButtons.Count)
            {
                activeButtons[currentQuestion.indexJawabanBenar].GetComponent<Image>().color = correctColor;
            }
        }

        StartCoroutine(FinishQuizSequence());
    }

    IEnumerator FinishQuizSequence()
    {
        yield return new WaitForSeconds(2.0f);
        
        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.QuizFinishedServerRpc();
        }
        CloseAllPanels();
    }

    public void CloseAllPanels()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        if (quizPanelOther != null) quizPanelOther.SetActive(false);
    }
}