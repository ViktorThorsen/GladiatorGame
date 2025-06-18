using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

using UnityEngine.Networking;


public class GladProfilePopup : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text popupNameText;
    [SerializeField] private TMP_Text popupLevelText;
    [SerializeField] private Button sendChallengeButton;
    [SerializeField] private ProfileImageDataBase profileImageDataBase;
    [SerializeField] private Image popupHairImg;
    [SerializeField] private Image popupEyesImg;
    [SerializeField] private Image popupChestImg;
    [SerializeField] private FeedbackPopup feedbackPopupHandler;

    [SerializeField] private Transform popupSkillPanel;
    [SerializeField] private GameObject skillPrefab;
    [SerializeField] private SkillDataBase skillDataBase;

    public void ShowGladiatorPopup(int id, string name, int level, string hair, string eyes, string chest)
    {
        StartCoroutine(FetchGladiatorSkills(id));
        popupPanel.SetActive(true);
        popupNameText.text = name;
        popupLevelText.text = "Level: " + level;
        popupHairImg.sprite = profileImageDataBase.GetProfileImageByName(hair).profileImage;
        popupEyesImg.sprite = profileImageDataBase.GetProfileImageByName(eyes).profileImage;
        popupChestImg.sprite = profileImageDataBase.GetProfileImageByName(chest).profileImage;

        sendChallengeButton.onClick.RemoveAllListeners();
        sendChallengeButton.onClick.AddListener(() =>
        {
            TrySendChallenge(id);
        });
    }
    private void TrySendChallenge(int opponentId)
    {
        int cost = 20;
        if (CharacterData.Instance.coins >= cost)
        {
            CharacterData.Instance.coins -= cost;
            StartCoroutine(SendChallenge(opponentId));
        }
        else
        {
            feedbackPopupHandler.ShowFeedback("Failed to sent challenge", "you don't have enough coins");
        }
    }
    private IEnumerator FetchGladiatorSkills(int id)
    {
        string url = $"http://localhost:5000/api/characters/skills?id={id}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Kunde inte hämta skills: " + request.error);
            yield break;
        }

        // Rensa tidigare skills från popupen
        foreach (Transform child in popupSkillPanel)
            Destroy(child.gameObject);

        // ✅ Använd din befintliga SkillDataSerializable
        SkillDataSerializable skillData = JsonUtility.FromJson<SkillDataSerializable>(request.downloadHandler.text);

        foreach (SkillEntrySerializable skillEntry in skillData.skills)
        {
            Skill skill = skillDataBase.GetSkillByName(skillEntry.skillName);
            if (skill == null) continue;

            GameObject newSlot = Instantiate(skillPrefab, popupSkillPanel);

            Image skillImage = newSlot.transform.Find("skillImage").GetComponent<Image>();
            newSlot.transform.Find("Image1").gameObject.SetActive(skillEntry.level == 1);
            newSlot.transform.Find("Image2").gameObject.SetActive(skillEntry.level == 2);
            newSlot.transform.Find("Image3").gameObject.SetActive(skillEntry.level == 3);

            skillImage.sprite = skill.skillIcon;

            SkillUI skillUI = newSlot.GetComponent<SkillUI>();
            if (skillUI != null)
            {
                skillUI.Skill = skill;
                skillUI.Level = skillEntry.level;
            }
        }
    }
    public void CloseGladiatorPopup()
    {
        popupPanel.SetActive(false);

        foreach (Transform child in popupSkillPanel)
            Destroy(child.gameObject);
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