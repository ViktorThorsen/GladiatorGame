using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using System.Collections;



public class LoginManager : MonoBehaviour
{

    [System.Serializable]
    public class LoginRequest
    {
        public string idToken;
    }

    [System.Serializable]
    public class TokenResponse
    {
        public string token;
        public int id;
    }


    public void LoginWithGoogle()
    {
        StartCoroutine(LoginCoroutine());
    }

    private IEnumerator LoginCoroutine()
    {
        string idToken = "mock-id";
        var loginData = new LoginRequest { idToken = idToken };
        string json = JsonUtility.ToJson(loginData);
        using UnityWebRequest www = new UnityWebRequest("http://localhost:5000/api/auth/google", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();
        Debug.Log("📦 Response raw JSON: " + www.downloadHandler.text);

        if (www.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<TokenResponse>(www.downloadHandler.text);
            Debug.Log("✅ JWT mottagen: " + response.token);
            PlayerPrefs.SetString("jwt", response.token);
            PlayerPrefs.SetInt("id", response.id);
            PlayerPrefs.Save();

            // 💤 LÄGG TILL LITEN DELAY
            yield return new WaitForSeconds(0.2f);

            AuthChecker.instance.StartCheck();
        }
        else
        {
            Debug.LogError("❌ Login misslyckades: " + www.error);
        }
    }

}
