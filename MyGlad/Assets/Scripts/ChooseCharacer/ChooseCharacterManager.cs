using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ChooseCharacterManager : MonoBehaviour
{

    public GameObject[] buttonColumns; // Drag in tre knappar i Unity-editorn
    public GameObject[] characterButtons; // Drag in tre knappar i Unity-editorn
    public TMP_Text[] nameTexts;
    public TMP_Text[] levelTexts;

    [System.Serializable]
    public class CharacterSummary
    {
        public int id;
        public string name;
        public int level;
    }

    [System.Serializable]
    public class CharacterListWrapper
    {
        public List<CharacterSummary> characters;
    }

    void Start()
    {
        StartCoroutine(LoadCharacters());
    }

    IEnumerator LoadCharacters()
    {
        string jwt = PlayerPrefs.GetString("jwt", null);
        UnityWebRequest request = UnityWebRequest.Get("http://localhost:5000/api/user/characters");
        request.SetRequestHeader("Authorization", $"Bearer {jwt}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var json = request.downloadHandler.text;
            var characters = JsonUtility.FromJson<CharacterListWrapper>("{\"characters\":" + json + "}");

            for (int i = 0; i < characterButtons.Length; i++)
            {
                if (i < characters.characters.Count)
                {
                    buttonColumns[i].SetActive(true);
                    characterButtons[i].SetActive(true);
                    nameTexts[i].text = characters.characters[i].name;
                    levelTexts[i].text = "Level: " + characters.characters[i].level;
                    int charId = characters.characters[i].id;
                    characterButtons[i].GetComponent<Button>().onClick.AddListener(() => SelectCharacter(charId));
                }
                else
                {
                    characterButtons[i].SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogError("❌ Kunde inte hämta karaktärer: " + request.error);
        }
    }

    void SelectCharacter(int characterId)
    {
        PlayerPrefs.SetInt("characterId", characterId);
        SceneController.instance.LoadScene("Base"); // Eller vad scenen heter
    }
}
