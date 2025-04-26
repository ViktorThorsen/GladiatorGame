using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Networking;
using TMPro;

public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    [SerializeField] ItemDataBase itemDataBase;
    [SerializeField] PetDataBase petDataBase;
    [SerializeField] SkillDataBase skillDataBase;
    [SerializeField] Transform enemyCharPos;
    [SerializeField] TMP_Text nameText;
    [SerializeField] GameObject characterPrefab;
    [SerializeField] Transform parentObj;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(GetRandomNameAndLoadCharacter());
    }

    private IEnumerator GetRandomNameAndLoadCharacter()
    {
        int currentId = PlayerPrefs.GetInt("characterId");
        int randomId = currentId;

        string currentName = PlayerPrefs.GetString("characterName");
        string randomName = currentName;

        int tries = 0;
        while ((randomId == currentId || randomName == currentName) && tries < 50)
        {
            UnityWebRequest request = UnityWebRequest.Get("http://localhost:5000/api/characters/random");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ Failed to fetch random character: " + request.error);
                yield break;
            }

            string json = request.downloadHandler.text;
            EnemyCharacterResponse response = JsonUtility.FromJson<EnemyCharacterResponse>(json);

            randomId = response.id;
            randomName = response.name;

            tries++;
        }

        if (randomName == currentName)
        {
            Debug.LogWarning("⚠️ Fick samma namn efter 50 försök – laddar ändå.");
        }

        Debug.Log("✅ Ny karaktär: " + randomName + " (ID: " + randomId + ")");

        // Rensa gammal karaktär
        foreach (Transform child in parentObj)
        {
            if (child.CompareTag("Player"))
            {
                Destroy(child.gameObject);
            }
        }

        // Ladda ny karaktär
        yield return StartCoroutine(LoadAndCreateGladiator(randomId));
    }

    public void OnChangeCharacterButtonClicked()
    {
        StartCoroutine(GetRandomNameAndLoadCharacter());
    }


    private IEnumerator LoadAndCreateGladiator(int id)
    {
        yield return StartCoroutine(EnemyGladiatorData.Instance.LoadCharacterFromBackend(itemDataBase, petDataBase, skillDataBase, id));

        GameObject characterObject = CharacterManager.InstantiateEnemyGladiator(
        EnemyGladiatorData.Instance,
            characterPrefab,
            parentObj,
            enemyCharPos,
            new Vector3(0.7f, 0.7f, 1f)
        ); characterObject.tag = "Player";
        nameText.text = EnemyGladiatorData.Instance.CharName;
    }

    public void Cleanup()
    {
        if (Instance == this)
        {
            Instance = null;
            Destroy(gameObject);
        }
        ;
    }
}
