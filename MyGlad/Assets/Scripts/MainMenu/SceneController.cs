using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;
    public string currentSceneName;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded; // Registrera scenlyssnare
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Avregistrera för säkerhets skull
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        Debug.Log("🎮 Scen laddad: " + currentSceneName);
    }

    public void NextLevel()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadSceneAsync(nextSceneIndex);
    }

    public void LoadScene(string sceneName)
    {
        if (sceneName == "Arena")
        {
            if (ReplayManager.Instance != null)
            {
                Destroy(ReplayManager.Instance.gameObject);
            }
        }
        SceneManager.LoadSceneAsync(sceneName);

        // currentSceneName sätts nu i OnSceneLoaded istället
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey("jwt");
        PlayerPrefs.DeleteKey("id");
        PlayerPrefs.DeleteKey("characterId");
        PlayerPrefs.Save();

        if (CharacterData.Instance != null)
        {
            Destroy(CharacterData.Instance.gameObject);
        }
        if (FightData.Instance != null)
        {
            Destroy(FightData.Instance.gameObject);
        }

        SceneManager.LoadScene("MainMenu");
    }
}