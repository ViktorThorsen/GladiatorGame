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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void NextLevel()
    {
        // Load the next scene and switch music if necessary
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadSceneAsync(nextSceneIndex);
    }
    public void LoadScene(string sceneName)
    {
        if (sceneName == "Battle")
        {
            if (CharacterData.Instance.Energy > 1)
            {
                CharacterData.Instance.Energy--;
                SceneManager.LoadSceneAsync(sceneName);
                currentSceneName = sceneName;
            }
        }
        else
        {
            SceneManager.LoadSceneAsync(sceneName);
            currentSceneName = sceneName;
        }
    }
    public void Logout()
    {
        // Ta bort JWT och annan sparad info
        PlayerPrefs.DeleteKey("jwt");
        PlayerPrefs.DeleteKey("id");
        PlayerPrefs.DeleteKey("characterId");
        PlayerPrefs.Save();

        // Rensa ev. singleton-instansobjekt
        if (CharacterData.Instance != null)
        {
            Destroy(CharacterData.Instance.gameObject);
        }
        if (FightData.Instance != null)
        {
            Destroy(FightData.Instance.gameObject);
        }

        // Ladda huvudmenyn
        SceneManager.LoadScene("MainMenu"); // Ersätt med rätt scen-namn
    }
}