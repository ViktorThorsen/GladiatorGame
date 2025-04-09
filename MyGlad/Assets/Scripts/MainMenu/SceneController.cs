using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;

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
        SceneManager.LoadSceneAsync(nextSceneIndex).completed += (AsyncOperation op) =>
        {
            SwitchMusicBasedOnScene(SceneManager.GetSceneByBuildIndex(nextSceneIndex).name);
        };
    }

    public void LoadScene(string sceneName)
    {
        if (sceneName == "Battle")
        {
            if (CharacterData.Instance.Energy > 1)
            {
                CharacterData.Instance.Energy--;
                SceneManager.LoadSceneAsync(sceneName).completed += (AsyncOperation op) =>
            {
                SwitchMusicBasedOnScene(sceneName);
            };
            }
            else
            {

            }
        }
        else
        {
            // Load the specified scene and switch music if necessary
            SceneManager.LoadSceneAsync(sceneName).completed += (AsyncOperation op) =>
            {
                SwitchMusicBasedOnScene(sceneName);
            };
        }

    }

    // Method to switch music after loading a new scene
    private void SwitchMusicBasedOnScene(string sceneName)
    {
        if (BackgroundMusicManager.Instance != null)
        {
            // Check the scene name and switch music accordingly
            switch (sceneName)
            {
                case "MainMenu":
                    BackgroundMusicManager.Instance.PlayMusic(BackgroundMusicManager.Instance.menuMusic);
                    break;

                case "Battle":
                    BackgroundMusicManager.Instance.PlayMusic(BackgroundMusicManager.Instance.battleMusic);
                    break;
                case "ArenaBattle":
                    BackgroundMusicManager.Instance.PlayMusic(BackgroundMusicManager.Instance.battleMusic);
                    break;

                case "LevelUp":
                    BackgroundMusicManager.Instance.PlayMusic(BackgroundMusicManager.Instance.levelUpMusic);
                    break;

                case "Arena":
                    BackgroundMusicManager.Instance.PlayMusic(BackgroundMusicManager.Instance.arenaManagerMusic);
                    break;

                // Add more scenes and their respective music here
                default:
                    BackgroundMusicManager.Instance.PlayMusic(BackgroundMusicManager.Instance.menuMusic); // Default music
                    break;
            }
        }
    }
}