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
}