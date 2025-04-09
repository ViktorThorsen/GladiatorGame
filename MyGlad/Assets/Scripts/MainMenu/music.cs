using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance { get; private set; }

    // Ljudklipp för olika scener
    public AudioClip menuMusic;  // Ljudklipp för huvudmenyn
    public AudioClip battleMusic;  // Ljudklipp för spelet
    // You can add more clips for additional scenes if necessary
    public AudioClip levelUpMusic;
    public AudioClip arenaManagerMusic;

    private AudioSource audioSource;
    private float normalVolume = 1.0f;
    void Awake()
    {
        // Ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Don't destroy between scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Get the AudioSource component
        audioSource = GetComponent<AudioSource>();

        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Event callback when a new scene is loaded
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        audioSource.volume = normalVolume;
        // Check the scene name and switch music accordingly
        switch (scene.name)
        {
            case "MainMenu":
                PlayMusic(menuMusic); // Play menu music in the MainMenu scene
                break;

            case "Battle":
                PlayMusic(battleMusic); // Play game music in the GameScene
                break;
            case "ArenaBattle":
                PlayMusic(battleMusic); // Play game music in the GameScene
                break;

            case "RewardScene":
                PlayMusic(levelUpMusic); // Play game music in the GameScene
                break;

            case "Arena":
                PlayMusic(arenaManagerMusic); // Play game music in the GameScene
                break;

            // You can add more cases for different scenes and their corresponding music
            default:
                PlayMusic(menuMusic); // Default music if no specific case is found
                break;
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (audioSource.clip == clip) return; // Don't restart the music if it's already playing

        audioSource.clip = clip;

        // Disable looping for levelUpMusic, but loop for other scenes
        if (clip == levelUpMusic)
        {
            audioSource.loop = false; // Disable looping
        }
        else
        {
            audioSource.loop = true;  // Enable looping for other tracks
        }

        audioSource.Play();
    }

    public void FadeOutMusic(float fadeDuration)
    {
        StartCoroutine(FadeOutCoroutine(fadeDuration));
    }

    private IEnumerator FadeOutCoroutine(float fadeDuration)
    {
        float startVolume = audioSource.volume;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
            yield return null; // Wait for the next frame
        }

        audioSource.volume = 0;
        audioSource.Stop();
    }

    private void OnDestroy()
    {
        // Unsubscribe from the sceneLoaded event to avoid memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
