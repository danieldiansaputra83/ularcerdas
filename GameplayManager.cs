using UnityEngine;
using UnityEngine.UI; 
using TMPro; 
using Unity.Netcode;
using Unity.Collections; 
using System.Collections.Generic; 
using System.Collections; 
using UnityEngine.SceneManagement;
using System.Linq; 

// Struktur Data Pemain
[System.Serializable]
public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
{
    public ulong ClientId;
    public FixedString64Bytes Username;
    public int AvatarId;
    public int Score;
    public int InitialLevel;
    public int InitialXP;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Username);
        serializer.SerializeValue(ref AvatarId);
        serializer.SerializeValue(ref Score);
        serializer.SerializeValue(ref InitialLevel);
        serializer.SerializeValue(ref InitialXP);
    }

    public bool Equals(PlayerData other)
    {
        return ClientId == other.ClientId && 
               Username == other.Username && 
               AvatarId == other.AvatarId &&
               Score == other.Score && 
               InitialLevel == other.InitialLevel && 
               InitialXP == other.InitialXP;
    }
}

public struct FinalResultData
{
    public string username;
    public int score;
    public int avatarId;
    public int initialLevel;
    public int initialXP;
}

public class GameplayManager : NetworkBehaviour
{
    public static GameplayManager Instance; 
    public static List<FinalResultData> finalResultsList = new List<FinalResultData>();

    [Header("UI Elemen")]
    public Transform boardTransform;
    public TMP_Text kodeRuangText;
    public TMP_Text timerText;
    public Button kocokDaduButton;
    public TMP_Text turnIndicatorText; 

    [Header("UI Dadu")]
    public Image diceDisplayImage; 
    public Sprite[] diceSprites;   

    [Header("UI Daftar Pemain")]
    public GameObject playerCardPrefab;
    public Transform playerCardContainer;
    public Sprite[] allAvatars;

    [Header("UI Game Over")]
    public GameObject gameOverPanel;
    public TMP_Text winnerNameText;
    public Button nextSceneButton;

    [Header("Sistem Papan")]
    public Transform waypointParent;
    public List<Transform> waypoints = new List<Transform>();

    [Header("Aturan Kuis")]
    public QuizManager quizManager; 
    public List<int> petakKuis; 
    public List<int> petakBonus; 
    
    [Header("UI Bonus")]
    public GameObject bonusPanel;
    public Image bonusStarImage;

    private const int MAX_INDEX = 63; 

    private Dictionary<int, int> snakesAndLadders = new Dictionary<int, int>()
    {
        {14, 29}, {23, 40}, {27, 45}, {43, 59}, 
        {62, 51}, {53, 25}, {34, 16}, {22, 5}
    };

    // --- VARIABEL JARINGAN (SUDAH DIPERBAIKI) ---
    private NetworkVariable<int> gameLevel = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<FixedString32Bytes> networkKodeRuang = new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // 1. Timer Global (20 Menit)
    public NetworkVariable<float> totalGameTimer = new NetworkVariable<float>(1200f);
    
    // 2. Timer Giliran (1 Menit) - Dipindahkan ke atas agar rapi
    public NetworkVariable<float> turnTimer = new NetworkVariable<float>(60f); 
    public float turnDuration = 60f; 

    private NetworkVariable<bool> isTimerStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> currentTurnIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkList<PlayerData> allPlayers = new NetworkList<PlayerData>();
    private Dictionary<ulong, int> playerPositions = new Dictionary<ulong, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (waypointParent != null && waypoints.Count == 0)
        {
            foreach (Transform child in waypointParent) waypoints.Add(child);
        }
    }

    public override void OnNetworkSpawn()
    {
        gameLevel.OnValueChanged += (oldLvl, newLvl) => 
        {
            if (quizManager != null) quizManager.InitializeQuestions(newLvl);
        };
        
        networkKodeRuang.OnValueChanged += (a, b) => { kodeRuangText.text = "Kode Ruang: " + b.ToString(); };
        
        // REVISI: Timer UI mengikuti Total Game Timer (20 Menit)
        totalGameTimer.OnValueChanged += (a, b) => { UpdateTimerUI(b); };
        
        allPlayers.OnListChanged += HandlePlayerListChanged;
        currentTurnIndex.OnValueChanged += (a, b) => { UpdateDiceButtonState(); };

        kodeRuangText.text = "Kode Ruang: " + networkKodeRuang.Value.ToString();
        
        // REVISI: Update UI awal pakai totalGameTimer
        UpdateTimerUI(totalGameTimer.Value);
        
        RedrawPlayerList();
        
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
        if(diceDisplayImage != null) diceDisplayImage.gameObject.SetActive(false);
        if(bonusPanel != null) bonusPanel.SetActive(false);

        if (IsHost)
        {
            networkKodeRuang.Value = LobbyManager.CurrentJoinCode;
            int hostSelectedLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
            gameLevel.Value = hostSelectedLevel;

            // REVISI: Reset kedua timer saat spawn
            totalGameTimer.Value = 1200f;
            turnTimer.Value = turnDuration;
        }

        if (quizManager != null) quizManager.InitializeQuestions(gameLevel.Value);
        
        string myUsername = PlayerPrefs.GetString("MyUsername", "Pemain");
        int myAvatarId = PlayerPrefs.GetInt("MyAvatarId", 0);
        int myLevel = PlayerPrefs.GetInt("MyLevel", 1);
        int myXP = PlayerPrefs.GetInt("MyXP", 0);

        TellHostMyInfoServerRpc(myUsername, myAvatarId, myLevel, myXP);
        
        kocokDaduButton.onClick.AddListener(OnKocokDaduClicked);
        if (nextSceneButton != null) 
        {
            nextSceneButton.onClick.RemoveAllListeners();
            nextSceneButton.onClick.AddListener(OnNextSceneClicked);
        }

        StartCoroutine(DelayInitialButtonCheck());
    }

    private void HandlePlayerListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        Debug.Log("Data Pemain Berubah! Mengupdate UI...");
        RedrawPlayerList();
        UpdateDiceButtonState();
    }
    
    IEnumerator DelayInitialButtonCheck()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateDiceButtonState();
    }

    public override void OnNetworkDespawn()
    {
        if (allPlayers != null)
        {
            allPlayers.OnListChanged -= HandlePlayerListChanged;
        }
        base.OnNetworkDespawn();
    }

    void Update()
    {
        if (IsServer && isGameActive.Value)
        {
            // 1. Kurangi Waktu Global (20 Menit)
            if (totalGameTimer.Value > 0)
            {
                totalGameTimer.Value -= Time.deltaTime;
            }
            else
            {
                EndGameServerRpc(); 
            }

            // 2. Kurangi Waktu Giliran (1 Menit)
            if (turnTimer.Value > 0)
            {
                turnTimer.Value -= Time.deltaTime;
            }
            else
            {
                HandleTurnTimeUp();
            }
        }
    }    
    
    private void HandleTurnTimeUp()
    {
        Debug.Log("Waktu Giliran Habis!");
        ForceCloseQuizClientRpc();
        if(IsServer) ChangeTurn(); // Fungsi ChangeTurn digabung di logika FinishTurn
    }

    // Fungsi ganti giliran manual (dipanggil saat waktu habis)
    private void ChangeTurn()
    {
         if (allPlayers.Count > 0)
        {
            int nextTurn = (currentTurnIndex.Value + 1) % allPlayers.Count;
            // REVISI: Reset Timer Giliran
            turnTimer.Value = turnDuration; 
            currentTurnIndex.Value = nextTurn;
        }
    }

    [ClientRpc]
    private void ForceCloseQuizClientRpc()
    {
        if (quizManager != null) 
        {
            quizManager.CloseAllPanels();
            Debug.Log("Kuis ditutup paksa oleh server karena waktu habis.");
        }
    }

    private void UpdateDiceButtonState()
    {
        if (allPlayers.Count == 0 || !isGameActive.Value) 
        {
            if (kocokDaduButton != null) kocokDaduButton.interactable = false;
            return;
        }

        ulong myId = NetworkManager.Singleton.LocalClientId;
        int myIndex = -1;
        
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].ClientId == myId)
            {
                myIndex = i;
                break;
            }
        }

        bool isMyTurn = (myIndex == currentTurnIndex.Value);

        if (kocokDaduButton != null)
        {
            kocokDaduButton.interactable = isMyTurn;
        }

        if (turnIndicatorText != null)
        {
            int turnNow = currentTurnIndex.Value;
            if (turnNow < allPlayers.Count)
                turnIndicatorText.text = $"Giliran: {allPlayers[turnNow].Username}";
        }
    }

    // --- FUNGSI OPER GILIRAN (SAAT PEMAIN SELESAI JALAN) ---
    [ServerRpc(RequireOwnership = false)]
    public void FinishTurnServerRpc()
    {
        if (allPlayers.Count > 0)
        {
            int nextTurn = (currentTurnIndex.Value + 1) % allPlayers.Count;
            
            // REVISI: Reset timer GILIRAN (turnTimer), bukan game timer
            turnTimer.Value = turnDuration; 
            
            currentTurnIndex.Value = nextTurn;
            Debug.Log($"Giliran Berpindah ke Index: {nextTurn}");
        }
    }

    // --- LOGIKA TOMBOL & DADU ---

    private void OnKocokDaduClicked() 
    { 
        if (kocokDaduButton != null) kocokDaduButton.interactable = false;
        RequestKocokDaduServerRpc(); 
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestKocokDaduServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int senderIndex = -1;
        for (int i = 0; i < allPlayers.Count; i++) { if (allPlayers[i].ClientId == clientId) senderIndex = i; }
        
        if (senderIndex != currentTurnIndex.Value) 
        {
            Debug.LogWarning("Bukan giliranmu!");
            return; 
        }

        if (!isGameActive.Value) return;
        if (!isTimerStarted.Value) isTimerStarted.Value = true;

        int hasilDadu = Random.Range(1, 7);
        Debug.Log($"Pemain {clientId} dapat angka: {hasilDadu}");

        if (!playerPositions.ContainsKey(clientId)) playerPositions[clientId] = 0;
        int posisiAwal = playerPositions[clientId];
        int posisiMentah = posisiAwal + hasilDadu;
        int posisiAkhir = posisiMentah;
        bool isBounce = false;

        if (posisiMentah > MAX_INDEX)
        {
            isBounce = true;
            int kelebihan = posisiMentah - MAX_INDEX;
            posisiAkhir = MAX_INDEX - kelebihan;
        }

        if (posisiMentah == MAX_INDEX)
        {
            playerPositions[clientId] = MAX_INDEX;
            isGameActive.Value = false;
            AddScoreServerRpc(150); 
            PlayTurnClientRpc(clientId, hasilDadu, posisiAwal, MAX_INDEX, false, true, false, 0);
            StartCoroutine(ServerHandleWin(clientId, posisiAwal, MAX_INDEX));
            return; 
        }

        playerPositions[clientId] = posisiAkhir;

        bool isTrap = false;
        int posisiTrap = posisiAkhir;
        if (snakesAndLadders.ContainsKey(posisiAkhir))
        {
            isTrap = true;
            posisiTrap = snakesAndLadders[posisiAkhir];
            playerPositions[clientId] = posisiTrap; 
        }

        PlayTurnClientRpc(clientId, hasilDadu, posisiAwal, posisiMentah, isBounce, false, isTrap, posisiTrap);
    }

    private IEnumerator ServerHandleWin(ulong winnerId, int startPos, int endPos)
    {
        int jarak = Mathf.Abs(endPos - startPos);
        float totalWaktu = 1.5f + (jarak * 0.3f) + 1.0f;
        yield return new WaitForSeconds(totalWaktu);
        TriggerGameOverClientRpc(winnerId);
    }

    [ClientRpc]
    private void PlayTurnClientRpc(ulong playerId, int diceResult, int startPos, int rawNextPos, bool isBounce, bool isWin, bool isTrap, int trapDest)
    {
        StartCoroutine(TurnRoutine(playerId, diceResult, startPos, rawNextPos, isBounce, isWin, isTrap, trapDest));
    }

    private IEnumerator TurnRoutine(ulong playerId, int diceResult, int startPos, int rawNextPos, bool isBounce, bool isWin, bool isTrap, int trapDest)
    {
        if (kocokDaduButton != null) kocokDaduButton.interactable = false;

        if (diceDisplayImage != null)
        {
            diceDisplayImage.gameObject.SetActive(true);
            float duration = 1.0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                int randomFace = Random.Range(0, 6);
                diceDisplayImage.sprite = diceSprites[randomFace];
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            diceDisplayImage.sprite = diceSprites[diceResult - 1];
            yield return new WaitForSeconds(0.5f);
            diceDisplayImage.gameObject.SetActive(false);
        }

        Transform pawn = GetPlayerPawn(playerId);
        if (pawn != null)
        {
            if (isBounce)
            {
                int bounceEndPos = MAX_INDEX - (rawNextPos - MAX_INDEX);
                yield return StartCoroutine(BouncePawnRoutine(playerId, pawn, startPos, bounceEndPos)); 
            }
            else
            {
                yield return StartCoroutine(MovePawnRoutine(playerId, pawn, startPos, rawNextPos));
            }

            if (isTrap)
            {
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(MovePawnRoutine(playerId, pawn, -1, trapDest));
            }
        }

        if (!isWin)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                FinishTurnServerRpc();
            }
            yield return new WaitForSeconds(0.2f); 
            UpdateDiceButtonState();
        }
    }

    [ClientRpc] private void MovePawnClientRpc(ulong playerId, int startPos, int endPos) { Transform p = GetPlayerPawn(playerId); if(p) StartCoroutine(MovePawnRoutine(playerId, p, startPos, endPos)); }
    
    private IEnumerator MovePawnRoutine(ulong playerId, Transform pawn, int startIndex, int targetIndex) {
        if (startIndex == -1) { Vector3 s = pawn.position; Vector3 e = waypoints[targetIndex].position; float el = 0; while(el<1f){pawn.position=Vector3.Lerp(s,e,el); el+=Time.deltaTime; yield return null;} pawn.position=e; }
        else { for(int i=startIndex+1; i<=targetIndex; i++) { if(i>=waypoints.Count) break; Vector3 s=pawn.position; Vector3 n=waypoints[i].position; float el=0; while(el<0.3f){pawn.position=Vector3.Lerp(s,n,el/0.3f); el+=Time.deltaTime; yield return null;} pawn.position=n; } }

        if (NetworkManager.Singleton.LocalClientId == playerId) {
            int pt = targetIndex + 1;
            if (petakKuis.Contains(pt)) {
                yield return new WaitForSeconds(0.5f);
                if (quizManager != null && quizManager.allQuestions.Count > 0)
                {
                    int randomIndex = Random.Range(0, quizManager.allQuestions.Count);
                    TriggerQuizServerRpc(randomIndex, playerId);
                }
            } else if (petakBonus!=null && petakBonus.Contains(pt)) {
                AddScoreServerRpc(10);
                if (bonusPanel!=null) StartCoroutine(ShowBonusEffect());
                yield return new WaitForSeconds(3.0f);
            }
        }
    }
    
    private IEnumerator BouncePawnRoutine(ulong playerId, Transform pawn, int startIndex, int finalIndex) {
         for(int i=startIndex+1; i<=MAX_INDEX; i++) { Vector3 s=pawn.position; Vector3 n=waypoints[i].position; float el=0; while(el<0.3f){pawn.position=Vector3.Lerp(s,n,el/0.3f); el+=Time.deltaTime; yield return null;} pawn.position=n; }
         yield return new WaitForSeconds(0.1f);
         for(int i=MAX_INDEX-1; i>=finalIndex; i--) { Vector3 s=pawn.position; Vector3 n=waypoints[i].position; float el=0; while(el<0.3f){pawn.position=Vector3.Lerp(s,n,el/0.3f); el+=Time.deltaTime; yield return null;} pawn.position=n; }
         
         if (NetworkManager.Singleton.LocalClientId == playerId) {
            int pt = finalIndex + 1;
            if (petakKuis.Contains(pt)) {
                yield return new WaitForSeconds(0.5f);
                if (quizManager!=null && quizManager.allQuestions.Count>0) {
                    int r = Random.Range(0, quizManager.allQuestions.Count);
                    TriggerQuizServerRpc(r, playerId);
                }
            } else if (petakBonus!=null && petakBonus.Contains(pt)) {
                AddScoreServerRpc(10);
                if (bonusPanel!=null) StartCoroutine(ShowBonusEffect());
                yield return new WaitForSeconds(3.0f);
            }
        }
    }

    private Transform GetPlayerPawn(ulong playerId) {
        PawnAppearance[] pawns = FindObjectsOfType<PawnAppearance>();
        foreach(var p in pawns) if(p.OwnerClientId == playerId) return p.transform;
        return null;
    }
    [ServerRpc(RequireOwnership = false)] private void RequestSpawnPositionServerRpc(ServerRpcParams rpcParams = default) { ResetPlayerPosition(rpcParams.Receive.SenderClientId); }
    private void ResetPlayerPosition(ulong clientId) { playerPositions[clientId] = 0; TeleportPawnClientRpc(clientId, 0); }
    [ClientRpc] private void TeleportPawnClientRpc(ulong playerId, int targetIndex) { if (waypoints.Count == 0) return; Transform pawn = GetPlayerPawn(playerId); if (pawn != null) pawn.position = waypoints[targetIndex].position; }
    
    [ServerRpc(RequireOwnership = false)]
    private void EndGameServerRpc() { TriggerGameOverClientRpc(9999); } // 9999 kode waktu habis

    [ClientRpc] private void TriggerGameOverClientRpc(ulong winnerId) {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (kocokDaduButton != null) kocokDaduButton.interactable = false;
        if (winnerNameText != null) {
            if (winnerId == 9999) { winnerNameText.text = "WAKTU HABIS!"; winnerNameText.color = Color.yellow; }
            else if (NetworkManager.Singleton.LocalClientId == winnerId) { winnerNameText.text = "KAMU MENANG!"; winnerNameText.color = Color.green; }
            else { winnerNameText.text = "KAMU KALAH!"; winnerNameText.color = Color.red; }
        }
    }
    private void OnNextSceneClicked() {
        finalResultsList.Clear();
        foreach (PlayerData p in allPlayers) {
            FinalResultData data = new FinalResultData { username = p.Username.ToString(), score = p.Score, avatarId = p.AvatarId, initialLevel = p.InitialLevel, initialXP = p.InitialXP };
            finalResultsList.Add(data);
        }
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("Leaderboard"); 
    }
    [ServerRpc(RequireOwnership = false)] private void TellHostMyInfoServerRpc(string username, int avatarId, int level, int xp, ServerRpcParams rpcParams = default) {
        PlayerData data = new PlayerData { ClientId = rpcParams.Receive.SenderClientId, Username = username, AvatarId = avatarId, Score = 0, InitialLevel = level, InitialXP = xp };
        allPlayers.Add(data);
    }
    private void HandleClientDisconnect(ulong clientId)
    {
        if (!isGameActive.Value) return; 
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].ClientId == clientId)
            {
                allPlayers.RemoveAt(i);
                if (playerPositions.ContainsKey(clientId)) playerPositions.Remove(clientId);
                break;
            }
        }
    }
    
    private void RedrawPlayerList()
    {
        int existingCardCount = playerCardContainer.childCount;
        int playerCount = allPlayers.Count;

        for (int i = 0; i < playerCount; i++)
        {
            GameObject cardObj;
            PlayerData player = allPlayers[i];

            if (i < existingCardCount) cardObj = playerCardContainer.GetChild(i).gameObject;
            else cardObj = Instantiate(playerCardPrefab, playerCardContainer);

            PlayerCard cardScript = cardObj.GetComponent<PlayerCard>();
            Sprite avatarSprite = allAvatars[0];
            if (player.AvatarId >= 0 && player.AvatarId < allAvatars.Length) 
                avatarSprite = allAvatars[player.AvatarId];

            cardScript.UpdateDisplay(avatarSprite, player.Username.ToString(), player.Score);
        }

        if (existingCardCount > playerCount)
        {
            for (int i = playerCount; i < existingCardCount; i++) Destroy(playerCardContainer.GetChild(i).gameObject);
        }
        
        Debug.Log($"UI Player List Diperbarui. Total: {playerCount} pemain.");
    }
    private void UpdateTimerUI(float detik) { if (detik < 0) detik = 0; float menit = Mathf.FloorToInt(detik / 60); float detikSisa = Mathf.FloorToInt(detik % 60); timerText.text = string.Format("{0:00}:{1:00}", menit, detikSisa); }
    [ServerRpc(RequireOwnership = false)] 
    public void AddScoreServerRpc(int pointsToAdd, ServerRpcParams rpcParams = default) 
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        for (int i = 0; i < allPlayers.Count; i++) 
        {
            if (allPlayers[i].ClientId == clientId) 
            {
                PlayerData dataPemain = allPlayers[i];
                dataPemain.Score += pointsToAdd; 
                allPlayers[i] = dataPemain; 
                break;
            }
        }
    }
    [ServerRpc(RequireOwnership = false)] public void TriggerQuizServerRpc(int questionIndex, ulong activePlayerId) { OpenQuizClientRpc(questionIndex, activePlayerId); }
    [ClientRpc]
    private void OpenQuizClientRpc(int questionIndex, ulong activePlayerId)
    {
        bool isMe = (NetworkManager.Singleton.LocalClientId == activePlayerId);
        if (quizManager != null) quizManager.ShowSpecificQuestion(questionIndex, isMe);
    }
    [ServerRpc(RequireOwnership = false)] public void QuizFinishedServerRpc() { CloseQuizPanelsClientRpc(); }
    [ClientRpc] private void CloseQuizPanelsClientRpc() { if (quizManager != null) quizManager.CloseAllPanels(); }
    private IEnumerator ShowBonusEffect() { if (bonusPanel != null) bonusPanel.SetActive(true); float duration = 3.0f; float elapsed = 0f; while (elapsed < duration) { if (bonusStarImage != null) bonusStarImage.transform.Rotate(0, 0, -180 * Time.deltaTime); elapsed += Time.deltaTime; yield return null; } if (bonusPanel != null) bonusPanel.SetActive(false); if (bonusStarImage != null) bonusStarImage.transform.rotation = Quaternion.identity; }
}