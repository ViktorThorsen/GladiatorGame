using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;

public class BaseManager : MonoBehaviour
{
    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private PetDataBase petDataBase;
    [SerializeField] private SkillDataBase skillDataBase;
    [SerializeField] private TMP_Text lvlText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text strText;
    [SerializeField] private TMP_Text agiText;
    [SerializeField] private TMP_Text intText;
    [SerializeField] private TMP_Text PreText;
    [SerializeField] private TMP_Text DefText;
    [SerializeField] private TMP_Text FortText;
    [SerializeField] private TMP_Text HelText;
    [SerializeField] private Slider xpBar;


    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text valorText;

    // Array to hold references to the UI item slots (assign in the Inspector)
    [SerializeField] private GameObject inventoryPopupPanel;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject skillPrefab;
    [SerializeField] private GameObject statsPopupPanel;
    [SerializeField] private Image[] weaponSlots = new Image[12];
    [SerializeField] private Image[] consumableSlots = new Image[3];
    [SerializeField] private Image[] petSlots = new Image[3];
    [SerializeField] private Image[] shortcutSlots = new Image[3];

    [SerializeField] private Image hair;
    [SerializeField] private Image eyes;
    [SerializeField] private Image chest;
    [SerializeField] private TMP_Text nameText;

    [SerializeField] private ProfileImageDataBase profileImageDataBase;


    [SerializeField] private GameObject skillImagePrefab;
    [SerializeField] private Transform skillPanel;

    [SerializeField] private GameObject loadingScreen;




    public GameObject characterPrefab;   // Reference to the character prefab
    public Transform parentObj;          // Reference to the Canvas (assign in Inspector)
    public Transform charPos;            // Reference to the charPos Transform (assign in Inspector)

    [System.Serializable]
    public class CharacterResponse
    {
        public int characterId;
        public CharacterWrapper wrapper;
    }

    void Awake()
    {
        if (!SkillDataBase.Instance)
            skillDataBase.InitializeInstance();
    }

    void Start()
    {
        StartCoroutine(InitCharacterAndUI());
    }

    private IEnumerator InitCharacterAndUI()
    {
        loadingScreen.SetActive(true);
        if (MonsterHuntManager.Instance != null)
        {
            Destroy(MonsterHuntManager.Instance.gameObject);
        }
        if (ReplayCharacterData.Instance != null)
        {
            Destroy(ReplayCharacterData.Instance.gameObject);
        }
        if (ReplayEnemyGladData.Instance != null)
        {
            Destroy(ReplayEnemyGladData.Instance.gameObject);
        }

        if (ChooseLandsManager.Instance != null)
        {
            Destroy(ChooseLandsManager.Instance.gameObject);
        }
        if (ReplayData.Instance != null)
        {
            Destroy(ReplayData.Instance.gameObject);
        }
        if (ReplayGameManager.Instance != null)
        {
            Destroy(ReplayGameManager.Instance.gameObject);
        }
        string token = PlayerPrefs.GetString("jwt", null);
        int userid = PlayerPrefs.GetInt("id");
        if (!string.IsNullOrEmpty(token))
        {
            Debug.Log("✅ Användaren är inloggad, JWT: " + token);
            Debug.Log("✅ Användarens id : " + userid);
            Debug.Log("karaktärens id : " + PlayerPrefs.GetInt("characterId"));


            if (CharacterData.Instance == null)
            {
                GameObject characterObj = new GameObject("CharacterData");
                characterObj.AddComponent<CharacterData>();
                DontDestroyOnLoad(characterObj);

                // 🔁 Vänta en frame så att Awake() hinner köras
                yield return null;

                bool loadSuccess = false;
                yield return StartCoroutine(CharacterData.Instance.LoadCharacterFromBackend(
                    itemDataBase,
                    petDataBase,
                    skillDataBase,
                    success => loadSuccess = success
                ));

                if (!loadSuccess)
                {
                    Debug.LogWarning("❌ Kunde inte ladda karaktärsdata – loggar ut.");
                    SceneController.instance.Logout();
                    yield break;
                }
                else
                {
                    CharacterData.Instance.Id = PlayerPrefs.GetInt("characterId");
                }
            }
            if (FightData.Instance == null)
            {
                GameObject fightDataObj = new GameObject("FightData");
                fightDataObj.AddComponent<FightData>();
                DontDestroyOnLoad(fightDataObj);

                // 🔁 Vänta en frame så att Awake() hinner köras
                yield return null;
            }

            // 👤 Instansiera karaktären
            GameObject characterObject = CharacterManager.InstantiateCharacter(
                CharacterData.Instance,
                characterPrefab,
                parentObj,
                charPos,
                new Vector3(0.7f, 0.7f, 1f)
            );

            if (characterObject != null)
            {
                lvlText.text = CharacterData.Instance.Level.ToString();
                strText.text = CharacterData.Instance.Strength.ToString();
                agiText.text = CharacterData.Instance.Agility.ToString();
                intText.text = CharacterData.Instance.Intellect.ToString();
                HelText.text = CharacterData.Instance.Health.ToString();
                PreText.text = CharacterData.Instance.precision.ToString();
                DefText.text = CharacterData.Instance.Defense.ToString();
                coinsText.text = CharacterData.Instance.coins.ToString();
                valorText.text = CharacterData.Instance.valor.ToString();
            }
            else
            {
                SceneController.instance.Logout();
            }

            UpdateXpBar();
            UpdateInventorySlots();
            UpdateProfilePic();


            UpdatePetSlots();

            if (!CharacterData.Instance.CreatedNow)
            {
                bool energySuccess = false;
                yield return StartCoroutine(CharacterData.Instance.FetchCharacterEnergy(success => energySuccess = success));
                if (!energySuccess)
                {
                    Debug.LogWarning("❌ Energin kunde inte hämtas – loggar ut.");
                    SceneController.instance.Logout();
                    yield break;
                }
                else
                {
                    energyText.text = $"{CharacterData.Instance.Energy} / 10";
                    if (CharacterData.Instance.needUpdate)
                    {
                        yield return StartCoroutine(SaveCharacter());
                    }
                }

            }
            else if (CharacterData.Instance.CreatedNow)
            {
                energyText.text = $"{CharacterData.Instance.Energy} / 10";
                Debug.Log("sparar karaktär");
                yield return StartCoroutine(SaveCharacter());
            }



        }
        else
        {
            Debug.LogWarning("❌ Ingen JWT hittades, visa login");
            SceneController.instance.Logout();
        }
        loadingScreen.SetActive(false);
    }

    public void ToggleInventoryPopup()
    {
        bool isActive = inventoryPopupPanel.activeSelf;

        if (isActive)
        {
            // Om panelen ska stängas, kör save-koll
            OnCloseInventory();
        }
        else
        {
            inventoryPopupPanel.SetActive(true);
        }
    }

    public void OnCloseInventory()
    {
        inventoryPopupPanel.SetActive(false);

        if (Inventory.Instance.HasChanges())
        {
            StartCoroutine(SaveCharacter());
            Inventory.Instance.ClearDirtyFlag();
        }
    }

    public void ToggleStatsPopup()
    {
        bool isActive = statsPopupPanel.activeSelf;
        statsPopupPanel.SetActive(!isActive);
    }




    public IEnumerator SaveCharacter()
    {
        if (CharacterData.Instance != null)
        {
            Debug.Log("Saving character data...");
            CharacterData.Instance.needUpdate = false;
            yield return StartCoroutine(SaveAndLinkCharacter());
        }
        else
        {
            Debug.LogWarning("CharacterData instance not found!");
        }
    }

    private IEnumerator SaveAndLinkCharacter()
    {
        yield return StartCoroutine(CharacterData.Instance.SaveCharacterToBackend());

        Debug.Log("Character saved, now linking...");
        yield return StartCoroutine(CharacterData.Instance.LinkCharacterToUser(
            PlayerPrefs.GetInt("id"),
            PlayerPrefs.GetInt("characterId")
        ));

        Debug.Log("Character linked to user!");
        yield return StartCoroutine(AddCharacterMonsterHuntInfo(PlayerPrefs.GetInt("characterId")));
    }

    public IEnumerator AddCharacterMonsterHuntInfo(int characterId)
    {
        CharacterIdDTO data = new CharacterIdDTO { characterId = characterId };
        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = UnityWebRequest.PostWwwForm("http://localhost:5000/api/monsterhunt", "");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        string token = PlayerPrefs.GetString("jwt");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Monster Hunt skapad: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("❌ Misslyckades: " + request.error);
        }
    }

    public void OnSwitchCharacterButtonClick()
    {
        SceneController.instance.LoadScene("ChooseCharacter"); // Load the character selection scene
    }

    // Method to update the inventory slots in the UI
    private void UpdateInventorySlots()
    {
        nameText.text = CharacterData.Instance.CharName;
        // Make sure the inventory is not null
        if (Inventory.Instance != null)
        {
            // Get the inventory items
            List<Item> weaponItems = Inventory.Instance.GetWeapons();
            List<Item> consumableItems = Inventory.Instance.GetConsumables();
            // Loop through the inventory items and assign to slots
            if (weaponSlots != null)
            {
                for (int i = 0; i < weaponSlots.Length; i++)
                {
                    Transform slot = weaponSlots[i].transform;

                    // 🧹 Rensa tidigare items i slotten
                    foreach (Transform child in slot)
                    {
                        Destroy(child.gameObject);
                    }

                    // 🪄 Lägg till nytt item om det finns
                    if (i < weaponItems.Count)
                    {
                        Item item = weaponItems[i];

                        GameObject itemObj = Instantiate(itemPrefab, slot);
                        itemObj.transform.localPosition = Vector3.zero;

                        Image img = itemObj.GetComponent<Image>();
                        img.sprite = item.itemIcon;

                        ItemUI itemUI = itemObj.GetComponent<ItemUI>();
                        if (itemUI != null)
                        {
                            itemUI.Item = item;
                        }
                    }
                }
            }
            if (consumableSlots != null)
            {
                for (int i = 0; i < consumableSlots.Length; i++)
                {
                    if (consumableSlots[i] == null) continue;

                    Transform slot = consumableSlots[i].transform;

                    foreach (Transform child in slot)
                    {
                        Destroy(child.gameObject);
                    }

                    if (i < consumableItems.Count)
                    {
                        Item item = consumableItems[i];
                        GameObject itemObj = Instantiate(itemPrefab, slot);
                        itemObj.transform.localPosition = Vector3.zero;

                        Image img = itemObj.GetComponent<Image>();
                        if (img != null) img.sprite = item.itemIcon;

                        ItemUI itemUI = itemObj.GetComponent<ItemUI>();
                        if (itemUI != null)
                        {
                            itemUI.Item = item;
                        }
                    }
                }
            }
            // Clear any existing slots if necessary
            foreach (Transform child in skillPanel)
            {
                Destroy(child.gameObject);
            }
            // Iterate over each item in the combat inventory
            foreach (SkillInstance skillInstance in Inventory.Instance.GetSkills())
            {
                Skill skill = skillDataBase.GetSkillByName(skillInstance.skillName);
                if (skill == null) continue;

                GameObject newSlot = Instantiate(skillPrefab, skillPanel);

                Image skillImage = newSlot.transform.Find("skillImage").GetComponent<Image>();
                newSlot.transform.Find("Image1").gameObject.SetActive(skillInstance.level == 1);
                newSlot.transform.Find("Image2").gameObject.SetActive(skillInstance.level == 2);
                newSlot.transform.Find("Image3").gameObject.SetActive(skillInstance.level == 3);

                skillImage.sprite = skill.skillIcon;

                SkillUI skillUI = newSlot.GetComponent<SkillUI>();
                if (skillUI != null)
                {
                    skillUI.Skill = skill;
                    skillUI.Level = skillInstance.level; // Lägg till detta om du vill visa nivå t.ex.
                }
            }
            if (Inventory.Instance.shortcutWeaponIndexes.Count == Inventory.Instance.GetWeapons().Count)
            {
                for (int weaponIndex = 0; weaponIndex < Inventory.Instance.shortcutWeaponIndexes.Count; weaponIndex++)
                {
                    int shortcutSlot = Inventory.Instance.shortcutWeaponIndexes[weaponIndex];

                    if (shortcutSlot >= 0 && shortcutSlot < shortcutSlots.Length)
                    {
                        foreach (Transform child in shortcutSlots[shortcutSlot].transform)
                        {
                            Destroy(child.gameObject);
                        }

                        Item item = Inventory.Instance.GetWeapons()[weaponIndex];
                        GameObject itemObj = Instantiate(itemPrefab, shortcutSlots[shortcutSlot].transform);
                        itemObj.transform.localPosition = Vector3.zero;

                        Image img = itemObj.GetComponent<Image>();
                        img.sprite = item.itemIcon;

                        ItemUI itemUI = itemObj.GetComponent<ItemUI>();
                        if (itemUI != null)
                        {
                            itemUI.Item = item;
                        }

                        Destroy(itemObj.GetComponent<ItemDragHandler>());
                        var cg = itemObj.GetComponent<CanvasGroup>();
                        if (cg != null) Destroy(cg);
                    }
                }
            }
        }
    }
    public void UpdatePetSlots()
    {
        // Hämta BeastMaster-skills och nivå
        var beastMasterSkill = Inventory.Instance.GetSkills()
            .FirstOrDefault(s => s.skillName == "BeastMaster");

        int maxPetSlots = beastMasterSkill?.level ?? 0;  // 0 om inte finns
        List<GameObject> pets = Inventory.Instance.GetPets();

        for (int i = 0; i < petSlots.Length; i++)
        {
            if (i < maxPetSlots)
            {
                // Aktivera slot
                petSlots[i].gameObject.SetActive(true);

                if (i < pets.Count && pets[i] != null)
                {
                    Sprite petsIcon = pets[i].GetComponent<MonsterStats>().icon;
                    petSlots[i].sprite = petsIcon;
                    petSlots[i].color = new Color(1, 1, 1, 1); // synlig
                }
                else
                {
                    petSlots[i].sprite = null;
                    petSlots[i].color = new Color(1, 1, 1, 0); // tom slot
                }
            }
            else
            {
                // Inaktivera slot utanför tillåten nivå
                petSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void UpdateXpBar()
    {
        int baseXpRequiredToLevelUp = 100;  // Base XP for level 1
        float xpMultiplier = 2f;  // Exponential multiplier

        // Calculate the XP required for the current level to level up
        int xpRequiredToLevelUp = Mathf.FloorToInt(baseXpRequiredToLevelUp * Mathf.Pow(xpMultiplier, CharacterData.Instance.Level - 1));

        // Calculate the player's XP progress within the current level
        float xpProgress = (float)CharacterData.Instance.Xp / xpRequiredToLevelUp;

        // Update the XP bar slider to reflect the percentage progress
        xpBar.value = Mathf.Clamp01(xpProgress);  // Clamp to ensure the value stays between 0 and 1

        // Update the color of the XP bar (optional)
        Image xpFillImage = xpBar.fillRect.GetComponent<Image>();
        xpFillImage.color = new Color(0f, 1f, 0.043f, 1f);  // Opaque green
    }

    public void UpdateProfilePic()
    {
        string hairLabel = CharacterData.Instance.BodyPartLabels[0];
        string eyesLabel = CharacterData.Instance.BodyPartLabels[1];
        string chestLabel = CharacterData.Instance.BodyPartLabels[2];

        string hairKey = ProfileImageMapper.MapHair(hairLabel);
        string eyesKey = ProfileImageMapper.MapEyes(eyesLabel);
        string chestKey = ProfileImageMapper.MapChest(chestLabel);

        hair.sprite = profileImageDataBase.GetProfileImageByName(hairKey).profileImage;
        eyes.sprite = profileImageDataBase.GetProfileImageByName(eyesKey).profileImage;
        chest.sprite = profileImageDataBase.GetProfileImageByName(chestKey).profileImage;
    }
}