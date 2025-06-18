using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // För Button
using UnityEngine.Networking;
using Unity.VisualScripting; // För UnityWebRequest

public class searchManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private GameObject searchResultPrefab;
    [SerializeField] private Transform searchResultContainer;
    [SerializeField] private ProfileImageDataBase profileImageDataBase;
    [SerializeField] private GameObject skillPrefab;
    [SerializeField] private GladProfilePopup gladProfilePopup;

    void Start()
    {
        if (searchResultContainer == null)
        {
            return;
        }

        int playerLevel = CharacterData.Instance.Level;
        StartCoroutine(FetchInitialGladiators(playerLevel));
    }

    private IEnumerator FetchInitialGladiators(int level)
    {
        int playerId = CharacterData.Instance.Id;
        string url = $"http://localhost:5000/api/characters/levelsearch?level={level}&excludeId={playerId}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Kunde inte hämta initiala gladiatorer: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;
        GladiatorSearchResultList resultList = JsonUtility.FromJson<GladiatorSearchResultList>(json);

        Debug.Log("🔢 Antal resultat: " + resultList.results.Count);

        ClearSearchResults();
        PopulateResults(resultList.results);
    }

    private void PopulateResults(List<GladiatorSearchResult> results)
    {
        foreach (var result in results)
        {
            GameObject obj = Instantiate(searchResultPrefab, searchResultContainer);

            TMP_Text nameText = obj.transform.Find("NameText").GetComponent<TMP_Text>();
            TMP_Text levelText = obj.transform.Find("LevelText").GetComponent<TMP_Text>();

            Image hairImg = obj.transform.Find("Image/CharacterProfileImage/Hair").GetComponent<Image>();
            Image eyesImg = obj.transform.Find("Image/CharacterProfileImage/Eyes").GetComponent<Image>();
            Image chestImg = obj.transform.Find("Image/CharacterProfileImage/Chest").GetComponent<Image>();

            nameText.text = result.name;
            levelText.text = "Lv " + result.level;

            string hairKey = ProfileImageMapper.MapHair(result.hair);
            string eyesKey = ProfileImageMapper.MapEyes(result.eyes);
            string chestKey = ProfileImageMapper.MapChest(result.chest);

            hairImg.sprite = profileImageDataBase.GetProfileImageByName(hairKey).profileImage;
            eyesImg.sprite = profileImageDataBase.GetProfileImageByName(eyesKey).profileImage;
            chestImg.sprite = profileImageDataBase.GetProfileImageByName(chestKey).profileImage;

            obj.GetComponent<Button>().onClick.AddListener(() =>
            {
                gladProfilePopup.ShowGladiatorPopup(result.id, result.name, result.level, hairKey, eyesKey, chestKey);
            });
        }
    }

    public void OnSearchInputChanged()
    {
        string query = searchInput.text;
        if (query.Length >= 2)
        {
            StartCoroutine(SearchGladiators(query));
        }
        else
        {
            ClearSearchResults();
        }
    }

    private void ClearSearchResults()
    {
        if (searchResultContainer == null)
        {
            Debug.LogWarning("⚠️ searchResultContainer är inte assignad.");
            return;
        }

        foreach (Transform child in searchResultContainer)
            Destroy(child.gameObject);
    }

    [System.Serializable]
    public class GladiatorSearchResultList
    {
        public List<GladiatorSearchResult> results;
    }
    [System.Serializable]
    public class GladiatorSearchResult
    {
        public int id;
        public string name;
        public int level;
        public string hair;
        public string eyes;
        public string chest;
    }

    private IEnumerator SearchGladiators(string query)
    {
        int playerId = CharacterData.Instance.Id;
        string url = $"http://localhost:5000/api/characters/search?query={query}&excludeId={playerId}";

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Sökning misslyckades: " + request.error);
            yield break;
        }

        // Wrappa json-arrayen för JsonUtility
        string wrappedJson = "{\"results\":" + request.downloadHandler.text + "}";
        GladiatorSearchResultList resultList = JsonUtility.FromJson<GladiatorSearchResultList>(request.downloadHandler.text);

        ClearSearchResults();

        PopulateResults(resultList.results);
    }
}
