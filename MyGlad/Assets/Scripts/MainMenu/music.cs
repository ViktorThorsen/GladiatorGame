using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance { get; private set; }

    // Ljudklipp för olika scener
    public AudioClip menuMusic;  // Ljudklipp för huvudmenyn
    public AudioClip battleMusic;  // Ljudklipp för spelet
    public AudioClip battle1Music;
    public AudioClip battle2Music;
    public AudioClip battle3Music;
    // You can add more clips for additional scenes if necessary
    public AudioClip levelUpMusic;
    public AudioClip arenaManagerMusic;

    public AudioClip storyMusic; // Ljudklipp för arenan
    public AudioClip story2Music;

    public AudioClip ThemeViol;
    public AudioClip ThemeViol2;
    public AudioClip ThemeViol3;
    public AudioClip scars;
    public AudioClip taken;
    public AudioClip eyes;
    public AudioClip rome;
    public AudioClip forest;

    private AudioSource audioSource;
    private string sceneToResumeMusicFor = null;
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
        // ❗ Förhindra att vi avbryter belöningsmusiken i förtid
        if (audioSource.clip == levelUpMusic && audioSource.isPlaying)
        {
            // Vänta istället in att låten spelas klart
            sceneToResumeMusicFor = scene.name;
            return;
        }

        audioSource.volume = normalVolume;
        AudioClip[] menuClips = new AudioClip[] { storyMusic, story2Music, ThemeViol, ThemeViol2, ThemeViol3, scars, menuMusic, taken, eyes, rome, forest };
        AudioClip[] arenaClips = new AudioClip[] { battleMusic, battle1Music, battle2Music, battle3Music };
        switch (scene.name)
        {

            case "Battle":
            case "ArenaBattle":
                // Slumpa ett index och spela det valet
                int index = Random.Range(0, arenaClips.Length);
                PlayMusic(arenaClips[index]);
                break;

            case "LevelUp":
                PlayMusic(levelUpMusic);
                break;

            case "Arena":
                if (audioSource.clip == storyMusic || audioSource.clip == story2Music
                || audioSource.clip == ThemeViol || audioSource.clip == ThemeViol2 ||
                audioSource.clip == ThemeViol3 || audioSource.clip == scars ||
                audioSource.clip == menuMusic || audioSource.clip == taken || audioSource.clip == eyes || audioSource.clip == rome || audioSource.clip == forest)
                {
                    // Om vi redan spelar en av dessa låtar, fortsätt spela den
                    return;
                }
                // Slumpa ett index och spela det valet
                int index3 = Random.Range(0, menuClips.Length);
                PlayMusic(menuClips[index3]);
                break;

            case "RewardScene":
                if (audioSource.clip == storyMusic || audioSource.clip == story2Music ||
                 audioSource.clip == ThemeViol || audioSource.clip == ThemeViol2 ||
                  audioSource.clip == ThemeViol3 || audioSource.clip == scars ||
                  audioSource.clip == menuMusic || audioSource.clip == taken || audioSource.clip == eyes || audioSource.clip == rome || audioSource.clip == forest)
                {
                    // Om vi redan spelar en av dessa låtar, fortsätt spela den
                    return;
                }
                // Slumpa ett index och spela det valet
                int index1 = Random.Range(0, menuClips.Length);
                PlayMusic(menuClips[index1]);
                break;

            default:
                if (audioSource.clip == storyMusic || audioSource.clip == story2Music ||
                 audioSource.clip == ThemeViol || audioSource.clip == ThemeViol2 ||
                 audioSource.clip == ThemeViol3 || audioSource.clip == scars ||
                 audioSource.clip == menuMusic || audioSource.clip == taken || audioSource.clip == eyes || audioSource.clip == rome || audioSource.clip == forest)
                {
                    // Om vi redan spelar en av dessa låtar, fortsätt spela den
                    return;
                }
                // Slumpa ett index och spela det valet
                int index2 = Random.Range(0, menuClips.Length);
                PlayMusic(menuClips[index2]);
                break;
        }
    }

    void Update()
    {
        if (audioSource.clip == levelUpMusic && !audioSource.isPlaying && sceneToResumeMusicFor != null)
        {
            // LevelUpMusiken är klar, byt till nästa musik för den scenen
            ResumeMusicForScene(sceneToResumeMusicFor);
            sceneToResumeMusicFor = null;
        }
    }

    public void SetSceneToResume(string sceneName)
    {
        sceneToResumeMusicFor = sceneName;
    }

    private void ResumeMusicForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "MainMenu":
                PlayMusic(story2Music);
                break;

            case "Battle":
            case "ArenaBattle":
                PlayMusic(battleMusic);
                break;

            case "Arena":
                PlayMusic(arenaManagerMusic);
                break;

            default:
                PlayMusic(story2Music);
                break;
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        // Endast returnera om exakt samma clip redan spelas
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        audioSource.clip = clip;

        if (clip == levelUpMusic)
        {
            audioSource.loop = false;
        }
        else
        {
            audioSource.loop = true;
        }

        audioSource.Play();
    }

    public AudioClip GetCurrentClip()
    {
        return audioSource.clip;
    }

    public bool IsPlaying()
    {
        return audioSource.isPlaying;
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
