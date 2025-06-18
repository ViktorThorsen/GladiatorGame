using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.UI;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager Instance { get; private set; }
    public GameObject replayItemPrefab;
    public Transform replayListParent;
    [SerializeField] private GameObject historyPanel;

    private bool isHistoryVisible = false;

    public ReplayPayload selectedReplay;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Så den inte försvinner mellan scener
    }

    void Start()
    {
        if (SceneController.instance.currentSceneName == "Arena")
        {
            LoadReplays();
        }
    }

    public void ToggleHistoryPanel()
    {
        isHistoryVisible = !isHistoryVisible;
        historyPanel.SetActive(isHistoryVisible);
    }

    public void LoadReplays()
    {
        StartCoroutine(FetchReplays());
    }

    IEnumerator FetchReplays()
    {
        int characterId = PlayerPrefs.GetInt("characterId");
        UnityWebRequest request = UnityWebRequest.Get($"http://localhost:5000/api/replays/character?characterId={characterId}");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Failed to fetch replays: " + request.error);
            yield break;
        }

        var json = request.downloadHandler.text;
        List<ReplayPayload> replays = JsonConvert.DeserializeObject<List<ReplayPayload>>(json);

        foreach (var replay in replays)
        {
            GameObject item = Instantiate(replayItemPrefab, replayListParent);
            TMP_Text text = item.GetComponentInChildren<TMP_Text>();
            Button button = item.GetComponentInChildren<Button>();

            string currentCharName = CharacterData.Instance.CharName;
            bool isPlayer = replay.player.character.charName == currentCharName;

            // Vem är motståndaren?
            string opponentName = isPlayer ? replay.enemy.character.charName : replay.player.character.charName;

            // Vann spelaren?
            bool isWin = replay.winner == currentCharName;
            string result = isWin ? "Won" : "Lost";

            // Visa: Kartnamn | Resultat | Motståndare
            text.text = $"{replay.mapName} | {result} vs {opponentName}";

            button.onClick.AddListener(() =>
            {
                Debug.Log("▶ Showing replay...");
                selectedReplay = replay;
                PlayReplay();
            });
        }
    }
    public void PlayReplay()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Replay");
    }
}


