using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class LeaderBoardManager : MonoBehaviour
{
    [SerializeField] private GameObject leaderboardItemPrefab;
    [SerializeField] private Transform leaderboardListParent;

    private void Start()
    {
        StartCoroutine(FetchLeaderboard());
    }

    private IEnumerator FetchLeaderboard()
    {
        UnityWebRequest request = UnityWebRequest.Get("http://localhost:5000/api/leaderboard");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Failed to fetch leaderboard: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;
        List<LeaderboardCharacter> leaderboard = JsonConvert.DeserializeObject<List<LeaderboardCharacter>>(json);

        for (int i = 0; i < leaderboard.Count; i++)
        {
            var character = leaderboard[i];
            GameObject item = Instantiate(leaderboardItemPrefab, leaderboardListParent);

            TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>();
            TMP_Text rankText = texts[0];
            TMP_Text levelText = texts[1];
            TMP_Text nameText = texts[2];
            TMP_Text valorText = texts[3];

            rankText.text = $"#{i + 1}";
            levelText.text = $"Level {character.level}";
            nameText.text = character.charName;
            valorText.text = character.valor.ToString();
        }
    }

    [System.Serializable]
    private class LeaderboardCharacter
    {
        public int characterId;
        public string charName;
        public int level;
        public int valor;
    }
}

