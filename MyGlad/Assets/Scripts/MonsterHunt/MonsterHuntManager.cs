using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MonsterHuntManager : MonoBehaviour
{
    public static MonsterHuntManager Instance { get; private set; }
    [SerializeField] private List<UnityEngine.UI.Button> stageButtons;
    [SerializeField] private UnityEngine.UI.ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRect;

    // Use a List instead of an array to store multiple monster names
    private List<string> selectedMonsterNames = new List<string>();


    // Property to get the selected monster names
    public List<string> SelectedMonsterNames
    {
        get { return selectedMonsterNames; }
        set { selectedMonsterNames = value; }
    }
    public string sceneState;

    public int currentStage;
    public int selectedStage;

    [SerializeField] private SpriteRenderer map;
    [SerializeField] private BackgroundImageDataBase backgroundImageDataBase;

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
    public void Start()
    {
        switch (ChooseLandsManager.Instance.ChoosedLand)
        {
            case "Forest":
                StartCoroutine(LoadStageAndLog("Forest"));
                break;
            case "Desert":
                StartCoroutine(LoadStageAndLog("Desert"));
                break;
            case "Swamp":
                StartCoroutine(LoadStageAndLog("Swamp"));
                break;
            case "Jungle":
                StartCoroutine(LoadStageAndLog("Jungle"));
                break;
            case "Farm":
                StartCoroutine(LoadStageAndLog("Farm"));
                break;
            case "Frostlands":
                StartCoroutine(LoadStageAndLog("Frostlands"));
                break;
            case "Savannah":
                StartCoroutine(LoadStageAndLog("Savannah"));
                break;
        }
    }

    private IEnumerator LoadStageAndLog(string map)
    {
        yield return StartCoroutine(FetchMonsterHuntStage(CharacterData.Instance.Id, map));
        Debug.Log($"✅ är på stage: {currentStage}");
    }

    // Method to select a monster and add its name to the list
    public void SelectMonster(string name)
    {

        selectedMonsterNames.Clear();
        // Add the selected monster's name to the list (allow duplicates)
        selectedMonsterNames.Add(name);
    }

    public void SetState(string state)
    {
        sceneState = state;
    }
    public void SetSelectedStage(int stage)
    {
        selectedStage = stage;
    }

    // Method to clear all selected monsters
    public void ClearSelectedMonsters()
    {
        selectedMonsterNames.Clear();
    }

    public void Cleanup()
    {
        if (Instance == this)
        {
            Instance = null;
            Destroy(gameObject);
        }
    }

    public void OnStartMatchButtonClicked()
    {
        TryStartMatch();
    }

    public void TryStartMatch()
    {
        if (CharacterData.Instance.Energy > 0)
        {
            Debug.Log("✅ Tillräcklig energi, startar match...");
            StartCoroutine(StartMatchSequence());
        }
        else
        {
            Debug.LogWarning("❌ Inte tillräcklig energi för att starta match!");
            ShowNoEnergyPopup();
        }
    }

    private IEnumerator StartMatchSequence()
    {
        // Använd energi först
        yield return StartCoroutine(CharacterData.Instance.UseEnergyForMatch());

        // Sen starta själva matchen här
        SceneController.instance.LoadScene("Battle");
    }

    public void OnBackButtonClick()
    {
        SceneController.instance.LoadScene("Base"); // Load the character selection scene
    }

    private void ShowNoEnergyPopup()
    {
        // Här kan du öppna ett UI-fönster eller visa ett meddelande
        Debug.LogWarning("⚡ Du behöver mer energi för att spela!");
    }

    public IEnumerator FetchMonsterHuntStage(int characterId, string map)
    {
        string url = $"http://localhost:5000/api/monsterhunt?characterId={characterId}&map={UnityWebRequest.EscapeURL(map)}";
        UnityWebRequest request = UnityWebRequest.Get(url);

        string token = PlayerPrefs.GetString("jwt");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            if (int.TryParse(request.downloadHandler.text, out int stage))
            {
                currentStage = stage;
                UpdateStageButtons();
                ScrollToStageButton(currentStage);
            }
            else
            {
                Debug.LogWarning("⚠️ Fick inget giltigt stage-värde tillbaka.");
            }
        }
        else if (request.responseCode == 404)
        {
            Debug.Log("⚠️ Monster hunt hittades inte.");
        }
        else
        {
            Debug.LogError("❌ Fel vid GET: " + request.error);
        }
    }
    private void UpdateStageButtons()
    {
        for (int i = 0; i < stageButtons.Count; i++)
        {
            bool isUnlocked = (i + 1) <= currentStage;
            stageButtons[i].interactable = isUnlocked;
        }
    }

    private void ScrollToStageButton(int stage)
    {
        if (stage < 1 || stage > stageButtons.Count)
            return;

        RectTransform target = stageButtons[stage - 1].GetComponent<RectTransform>();

        float contentHeight = contentRect.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;

        // Hämta position i lokal y-led, justerat för mitten av knappen
        float targetY = -target.localPosition.y + (target.rect.height / 2f);

        // Justera scroll så att den positionen hamnar i mitten av viewport
        float offset = targetY - (viewportHeight / 2f);
        float scrollValue = Mathf.Clamp01(offset / (contentHeight - viewportHeight));

        scrollRect.verticalNormalizedPosition = 1f - scrollValue;
    }
}
