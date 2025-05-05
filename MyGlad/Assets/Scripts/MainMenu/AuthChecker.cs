using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AuthChecker : MonoBehaviour
{
    public static AuthChecker instance; // Valfritt: gör det lätt att kalla på från andra scripts

    private void Awake()
    {
        instance = this;
    }

    public void StartCheck(GameObject loadingPanel)
    {
        StartCoroutine(CheckIfHasGladiator(loadingPanel));
    }

    private IEnumerator CheckIfHasGladiator(GameObject loadingPanel)
    {
        string jwt = PlayerPrefs.GetString("jwt", null);
        if (string.IsNullOrEmpty(jwt))
        {
            Debug.LogWarning("Ingen JWT hittades, visa login");
            loadingPanel.SetActive(false);
            yield break;
        }

        UnityWebRequest request = UnityWebRequest.Get("http://localhost:5000/api/users/me");
        request.SetRequestHeader("Authorization", $"Bearer {jwt}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var json = request.downloadHandler.text;
            var response = JsonUtility.FromJson<HasGladiatorResponse>(json);
            Debug.Log("✅ hasGladiator: " + response.hasGladiator);

            if (response.hasGladiator)
                SceneController.instance.LoadScene("ChooseCharacter");
            else
                SceneController.instance.LoadScene("CreateCharacter");
        }
        else
        {
            Debug.LogError("❌ Fel vid kontroll av gladiator: " + request.error);
            loadingPanel.SetActive(false); // Göm loading om API-koll misslyckas
        }
    }

    [System.Serializable]
    public class HasGladiatorResponse
    {
        public bool hasGladiator;
    }
}
