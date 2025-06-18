using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class ChallangesManager : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreen;

    [Header("Challenge UI")]
    [SerializeField] private GameObject challengePanel;
    [SerializeField] private GameObject pendingChallengePrefab;
    [SerializeField] private GameObject receivedChallengePrefab;
    [SerializeField] private Transform pendingContainer;
    [SerializeField] private Transform receivedContainer;

    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private PetDataBase petDataBase;
    [SerializeField] private SkillDataBase skillDataBase;

    [SerializeField] private GameObject noPendingText;
    [SerializeField] private GameObject noReceivedText;
    [SerializeField] private GameObject alertBubble;

    [SerializeField] private GameObject insufficientCoinsPopup;

    [SerializeField] private FeedbackPopup feedbackPopupHandler;
    [SerializeField] private GladProfilePopup gladProfilePopupHandler;




    void Start()
    {
        StartCoroutine(LoadChallenges());
    }

    public void TrySpendCoinsAndStartMatch(int coinsToSpend)
    {
        StartCoroutine(SpendCoinsAndContinue(coinsToSpend));
    }

    private IEnumerator SpendCoinsAndContinue(int coinsToSpend)
    {
        int characterId = PlayerPrefs.GetInt("characterId");
        string url = $"http://localhost:5000/api/characters/spendcoins?characterId={characterId}&coinsToSpend={coinsToSpend}";

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "");
        string jwt = PlayerPrefs.GetString("jwt");

        if (!string.IsNullOrEmpty(jwt))
            request.SetRequestHeader("Authorization", $"Bearer {jwt}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Error spending coins: " + request.error);
            yield break;
        }

        if (request.responseCode == 200)
        {
            CharacterData.Instance.coins -= coinsToSpend;
            Debug.Log("✅ Coins spenderade – laddar scen.");
            SceneController.instance.LoadScene("ArenaBattle");
        }
    }

    public void OpenChallengesPopup()
    {
        challengePanel.SetActive(true);
        StartCoroutine(LoadChallenges());
    }
    public void CloseChallengesPopup()
    {
        challengePanel.SetActive(false);
        StartCoroutine(LoadChallenges());
    }

    private IEnumerator LoadChallenges()
    {
        int charId = PlayerPrefs.GetInt("characterId");

        // Rensa gamla objekt
        foreach (Transform child in pendingContainer) Destroy(child.gameObject);
        foreach (Transform child in receivedContainer) Destroy(child.gameObject);

        string url = $"http://localhost:5000/api/challenges?characterId={charId}";
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Misslyckades att ladda utmaningar: " + req.error);
            yield break;
        }

        ChallengeListWrapper wrapper = JsonUtility.FromJson<ChallengeListWrapper>(req.downloadHandler.text);

        foreach (var challenge in wrapper.pending)
        {
            GameObject go = Instantiate(pendingChallengePrefab, pendingContainer);
            go.transform.Find("NameText").GetComponent<TMP_Text>().text = challenge.opponentName;

            go.transform.Find("delete").GetComponent<Button>().onClick.AddListener(() =>
            {
                OnDeclineChallengeButtonClicked(challenge.id, go);
                Destroy(go);
            });
        }

        foreach (var challenge in wrapper.received)
        {
            GameObject go = Instantiate(receivedChallengePrefab, receivedContainer);
            go.transform.Find("NameText").GetComponent<TMP_Text>().text = challenge.challengerName + " Lv " + challenge.challengerLevel;

            // Klick på hela kortet (men inte knappar)
            Button cardButton = go.GetComponent<Button>();
            if (cardButton != null)
            {
                int challengerId = challenge.challengerId;

                cardButton.onClick.AddListener(() =>
                {
                    Debug.Log($"knappen reggad");
                    StartCoroutine(FetchAndShowOpponentPopup(challengerId));
                });
            }

            go.transform.Find("accept").GetComponent<Button>().onClick.AddListener(() =>
            {
                AcceptChallenge(challenge.id, challenge.challengerId);
            });

            go.transform.Find("decline").GetComponent<Button>().onClick.AddListener(() =>
            {
                OnDeclineChallengeButtonClicked(challenge.id, go);
                Destroy(go);
            });
        }

        // Pending text
        noPendingText.SetActive(wrapper.pending == null || wrapper.pending.Count == 0);
        noReceivedText.SetActive(wrapper.received == null || wrapper.received.Count == 0);
        alertBubble.SetActive(wrapper.received != null && wrapper.received.Count > 0);
    }

    private IEnumerator FetchAndShowOpponentPopup(int opponentId)
    {
        string url = $"http://localhost:5000/api/characters/visuals?id={opponentId}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Kunde inte hämta gladiatorvisuals: " + request.error);
            yield break;
        }

        CharacterVisualDTO visual = JsonUtility.FromJson<CharacterVisualDTO>(request.downloadHandler.text);

        string hairKey = ProfileImageMapper.MapHair(visual.hair);
        string eyesKey = ProfileImageMapper.MapEyes(visual.eyes);
        string chestKey = ProfileImageMapper.MapChest(visual.chest);

        gladProfilePopupHandler.ShowGladiatorPopup(opponentId, visual.name, visual.level, hairKey, eyesKey, chestKey);
    }

    [System.Serializable]
    public class CharacterVisualDTO
    {
        public string name;
        public int level;
        public string hair;
        public string eyes;
        public string chest;
    }


    [System.Serializable]
    public class ChallengeListWrapper
    {
        public List<ChallengeDTO> pending;
        public List<ChallengeDTO> received;
    }

    [System.Serializable]
    public class ChallengeDTO
    {
        public int id;
        public int challengerId;
        public string challengerName;
        public int challengerLevel;
        public string opponentName;
    }
    public void OnDeclineChallengeButtonClicked(int challengeId, GameObject cardToDestroy)
    {
        StartCoroutine(DeleteChallengeRequest(challengeId, cardToDestroy));
    }

    private IEnumerator DeleteChallengeRequest(int challengeId, GameObject cardToDestroy)
    {
        string url = $"http://localhost:5000/api/challenges?id={challengeId}";
        UnityWebRequest request = UnityWebRequest.Delete(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Misslyckades med att ta bort utmaning: " + request.error);
        }
        else
        {
            Debug.Log("✅ Utmaning borttagen");
            Destroy(cardToDestroy); // Radera bara det kortet
        }
    }

    public void AcceptChallenge(int opponentId, int challengeId)
    {
        StartCoroutine(HandleAcceptedChallenge(opponentId, challengeId));
    }

    private IEnumerator HandleAcceptedChallenge(int challengeId, int opponentId)
    {
        loadingScreen.SetActive(true);

        // Ta bort utmaningen först
        string deleteUrl = $"http://localhost:5000/api/challenges?id={challengeId}";
        UnityWebRequest deleteReq = UnityWebRequest.Delete(deleteUrl);
        yield return deleteReq.SendWebRequest();

        if (deleteReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Kunde inte radera challenge vid accept: " + deleteReq.error);
            loadingScreen.SetActive(false);
            yield break;
        }

        // Ladda fiendens karaktär
        yield return StartCoroutine(EnemyGladiatorData.Instance.LoadCharacterFromBackend(
            itemDataBase,
            petDataBase,
            skillDataBase,
            opponentId
        ));

        loadingScreen.SetActive(false);
        SceneController.instance.LoadScene("ArenaBattle");
    }

    private IEnumerator SendChallenge(int opponentId)
    {
        int challengerId = PlayerPrefs.GetInt("characterId");
        string jwt = PlayerPrefs.GetString("jwt");

        string url = "http://localhost:5000/api/challenges";
        ChallengePayload payload = new ChallengePayload
        {
            challengerId = challengerId,
            opponentId = opponentId
        };

        string json = JsonUtility.ToJson(payload);
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {jwt}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            feedbackPopupHandler.ShowFeedback("Error", "Failed to send challenge");
        }
        else
        {
            feedbackPopupHandler.ShowFeedback("Success", "Challenge sent");
        }
    }

    [System.Serializable]
    public class ChallengePayload
    {
        public int challengerId;
        public int opponentId;
    }
}