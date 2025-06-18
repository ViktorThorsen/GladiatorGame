using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using Unity.VisualScripting;


public class RewardSystem : MonoBehaviour
{
    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private MonsterDataBase monsterDataBase;
    [SerializeField] private PetDataBase petDataBase;
    [SerializeField] private Image[] weaponSlots = new Image[20];
    [SerializeField] private Image[] consumableSlots = new Image[3];
    [SerializeField] private Image[] petSlots = new Image[3];

    [SerializeField] private Sprite coinImage;

    [SerializeField] private TMP_Text xpRewardText;

    [SerializeField] private Transform rewardSlotParent;
    [SerializeField] private GameObject rewardSlotPrefab;
    [SerializeField] private GameObject swapPopup;

    [SerializeField] private GameObject swapItemPrefab;
    [SerializeField] private Image newItemSlot;
    private SwapItemUI currentlyHighlightedSlot;
    [SerializeField] private GameObject weaponSwapPanel;
    [SerializeField] private GameObject consumableSwapPanel;
    [SerializeField] private GameObject petSwapPanel;

    [SerializeField] private TMP_Text swapInfoText;
    [SerializeField] private TMP_Text swapInfoTitle;
    private SwapType pendingSwapType;
    private Item pendingItemReward;
    private ItemType pendingItemType;
    private GameObject pendingPetReward;
    private bool initiatedFromSceneChooser = false;

    private enum SwapType { Weapon, Consumable, Pet }
    private Queue<(object item, SwapType type)> pendingSwapQueue = new();
    private int selectedReplaceIndex = -1;

    private bool isLastFightAWin;
    private string[] lastFightNames;

    private string lastFightLand;
    private int lastFightStage;
    private bool levelUP;

    private List<Item> rewardItems = new List<Item>();
    private List<GameObject> rewardPets = new List<GameObject>();
    int rewardCoin;


    void Start()
    {
        if (ChooseLandsManager.Instance != null)
        {
            GetLastFight();
            levelUP = false;
            if (isLastFightAWin)
            {
                CharacterData.Instance.needUpdate = true;
                Item newWeapon = itemDataBase.GetRandomDropForStage(lastFightLand + lastFightStage);
                if (newWeapon != null)
                { rewardItems.Add(newWeapon); }
                Item newConsumable = itemDataBase.GetRandomConsumableForStage(lastFightLand + lastFightStage);
                if (newConsumable != null) { rewardItems.Add(newConsumable); }

                foreach (SkillInstance skillInstance in Inventory.Instance.GetSkills())
                {
                    if (skillInstance.skillName == "BeastMaster")
                    {
                        GameObject newPet = petDataBase.GetRandomPetForStage(lastFightLand + lastFightStage);
                        if (newPet != null) { rewardPets.Add(newPet); }
                    }
                }

                rewardCoin = GetCoinReward();
                CharacterData.Instance.coins += rewardCoin;
                int rewardXP = GetXpReward();
                AddXpToCharacter(rewardXP);
                UpdateXpDisplay(rewardXP);
                StartCoroutine(UpdateMonsterHuntStage(
                CharacterData.Instance.Id, lastFightLand, lastFightStage + 1));
            }

            UpdateRewardsSlots();
        }
        else
        {

        }
    }

    private void GetLastFight()
    {
        lastFightNames = FightData.Instance.GetLastFightResultNames();
        isLastFightAWin = FightData.Instance.GetLastFightResultWinOrLoss();
        lastFightLand = FightData.Instance.GetLastFightResultLand();
        lastFightStage = FightData.Instance.GetLastFightResultStage();
        Debug.Log("lastfight land and stage: " + lastFightLand + lastFightStage);

    }

    public Item GetWeaponReward()
    {
        // Get the total number of items in the item database
        int weaponsCount = itemDataBase.GetWeaponsCount();

        // Get a random index from the item database
        int randomIndex = UnityEngine.Random.Range(0, weaponsCount);

        // Return the item at the random index
        Item item = itemDataBase.GetWeapon(randomIndex);
        return item;
    }

    public Item GetConsumableReward()
    {
        // Get the total number of items in the item database
        int consumablesCount = itemDataBase.GetConsumablesCount();

        // Get a random index from the item database
        int randomIndex = UnityEngine.Random.Range(0, consumablesCount);
        // Return the item at the random index
        Item item = itemDataBase.GetConsumable(randomIndex);
        return item;
    }

    public GameObject GetPetReward()
    {
        // Get the total number of items in the item database
        int petsCount = petDataBase.GetPetsCount();

        // Get a random index from the item database
        int randomIndex = UnityEngine.Random.Range(0, petsCount);
        // Return the item at the random index
        GameObject pet = petDataBase.GetPet(randomIndex);
        return pet;
    }

    public int GetXpReward()
    {
        int reward = 0;
        foreach (string monsterName in lastFightNames)
        {
            GameObject monster = monsterDataBase.GetMonsterByName(monsterName);
            MonsterStats stats = monster.GetComponent<MonsterStats>();
            reward += stats.XpReward;
        }
        return reward;
    }
    public int GetCoinReward()
    {
        int reward = 0;
        foreach (string monsterName in lastFightNames)
        {
            GameObject monster = monsterDataBase.GetMonsterByName(monsterName);
            MonsterStats stats = monster.GetComponent<MonsterStats>();
            reward += stats.coinReward;
        }
        return reward;
    }


    public void AddXpToCharacter(int rewardXp)
    {
        int charLvl = CharacterData.Instance.Level;

        // Calculate XP required to level up
        int xpRequiredToLevelUp = CalculateXpRequiredForNextLevel(charLvl);

        // Add XP to the current XP
        CharacterData.Instance.Xp += rewardXp;

        // Check if the character has enough XP to level up
        while (CharacterData.Instance.Xp >= xpRequiredToLevelUp)
        {
            // Subtract the XP required for the current level, carry over the remaining XP
            CharacterData.Instance.Xp -= xpRequiredToLevelUp;

            // Level up the character
            LevelUp();

            // Recalculate XP required for the next level
            charLvl = CharacterData.Instance.Level;  // Update the level
            xpRequiredToLevelUp = CalculateXpRequiredForNextLevel(charLvl);
        }
    }

    private int CalculateXpRequiredForNextLevel(int charLvl)
    {
        int baseXpRequiredToLevelUp = 100;  // Base XP for level 1
        float xpMultiplier = 2f;  // Multiplier for exponential scaling

        // Exponential growth for levels
        return Mathf.FloorToInt(baseXpRequiredToLevelUp * Mathf.Pow(xpMultiplier, charLvl - 1));
    }

    public void LevelUp()
    {
        CharacterData.Instance.Level++;

        levelUP = true;
    }

    public void AddRewardToInventory()
    {
        foreach (Item item in rewardItems)
        {
            if (item.itemType == ItemType.Weapon)
            {
                if (Inventory.Instance.GetWeapons().Count < Inventory.Instance.maxWeapons)
                {
                    Inventory.Instance.AddWeaponToInventory(item);
                    Inventory.Instance.shortcutWeaponIndexes.Add(-1);
                }
                else
                {
                    pendingSwapQueue.Enqueue((item, SwapType.Weapon));
                }
            }
            else if (item.itemType == ItemType.Consumable)
            {
                if (Inventory.Instance.GetConsumables().Count < Inventory.Instance.maxConsumables)
                {
                    Inventory.Instance.AddConsumableToInventory(item);
                }
                else
                {
                    pendingSwapQueue.Enqueue((item, SwapType.Consumable));
                }
            }
        }

        foreach (GameObject pet in rewardPets)
        {
            if (Inventory.Instance.GetPets().Count < Inventory.Instance.GetMaxAllowedPets())
            {
                Inventory.Instance.AddPetToInventory(pet);
            }
            else
            {
                pendingSwapQueue.Enqueue((pet, SwapType.Pet));
            }
        }

        // 👉 Kör bara popup om något behöver swappas
        if (pendingSwapQueue.Count > 0 && !initiatedFromSceneChooser)
        {
            HandleNextPendingItem();
        }
    }

    private void HandleNextPendingItem()
    {

        if (pendingSwapQueue.Count == 0)
        {

            CloseSwapPopup();
            return;
        }

        var (obj, type) = pendingSwapQueue.Dequeue();

        if (type == SwapType.Pet)
        {
            ShowPetSwapPopup((GameObject)obj);
        }
        else
        {
            ShowSwapItemPopup((Item)obj, (ItemType)type);
        }
    }
    public void HighlightSelectedSlot(SwapItemUI selectedSlot)
    {
        if (currentlyHighlightedSlot != null)
        {
            currentlyHighlightedSlot.SetHighlight(false);
        }

        currentlyHighlightedSlot = selectedSlot;
        currentlyHighlightedSlot.SetHighlight(true);
    }
    private void ShowSwapItemPopup(Item item, ItemType type)
    {
        pendingPetReward = null;
        pendingSwapType = default;
        pendingItemReward = item;
        pendingItemType = type;

        swapPopup.SetActive(true);
        newItemSlot.sprite = item.itemIcon;
        newItemSlot.color = Color.white;

        weaponSwapPanel.SetActive(type == ItemType.Weapon);
        consumableSwapPanel.SetActive(type == ItemType.Consumable);

        if (type == ItemType.Weapon)
        {
            UpdateItemSlots(Inventory.Instance.GetWeapons(), weaponSlots);
            swapInfoTitle.text = "Weapon inventory is full";
            swapInfoText.text = "Select the weapon you want to replace.";
        }
        else if (type == ItemType.Consumable)
        {
            UpdateItemSlots(Inventory.Instance.GetConsumables(), consumableSlots);
            swapInfoTitle.text = "Consumable inventory is full";
            swapInfoText.text = "Select the consumable you want to replace.";
        }
    }
    private void ShowPetSwapPopup(GameObject pet)
    {
        pendingItemType = default;
        pendingSwapType = SwapType.Pet;
        swapPopup.SetActive(true);
        pendingPetReward = pet;

        MonsterStats stats = pet.GetComponent<MonsterStats>();
        newItemSlot.sprite = stats.icon;
        newItemSlot.color = Color.white;

        weaponSwapPanel.SetActive(false);
        consumableSwapPanel.SetActive(false);
        petSwapPanel.SetActive(true); // Du behöver lägga till denna som ny panel
        UpdatePetSlots(Inventory.Instance.GetPets(), petSlots);
        swapInfoTitle.text = "Pet inventory is full";
        swapInfoText.text = "Select the pet you want to replace.";
    }
    public void SelectItemToReplace(int index)
    {
        selectedReplaceIndex = index;
    }
    public void ConfirmReplace()
    {
        if (selectedReplaceIndex >= 0)
        {
            if (pendingSwapType == SwapType.Pet)
            {
                Inventory.Instance.GetPets()[selectedReplaceIndex] = pendingPetReward;
            }
            else if (pendingItemType == ItemType.Weapon)
            {
                Inventory.Instance.ReplaceWeaponAt(selectedReplaceIndex, pendingItemReward);
                Inventory.Instance.shortcutWeaponIndexes[selectedReplaceIndex] = -1;
            }
            else if (pendingItemType == ItemType.Consumable)
            {
                Inventory.Instance.ReplaceConsumableAt(selectedReplaceIndex, pendingItemReward);
            }
        }

        selectedReplaceIndex = -1;
        pendingItemReward = null;
        pendingPetReward = null;
        pendingSwapType = default;

        if (pendingSwapQueue.Count > 0)
        {
            HandleNextPendingItem(); // Visa nästa
        }
        else
        {
            LoadAppropriateScene();
        }
    }
    public void SkipReplace()
    {
        selectedReplaceIndex = -1;
        pendingItemReward = null;
        pendingPetReward = null;
        pendingSwapType = default;

        if (pendingSwapQueue.Count > 0)
        {
            HandleNextPendingItem(); // Visa nästa
        }
        else
        {
            LoadAppropriateScene();
        }
    }
    private void CloseSwapPopup()
    {
        swapPopup.SetActive(false);
        selectedReplaceIndex = -1;
        pendingItemReward = null;

        // Rensa preview-bilden i mitten (om du vill nollställa den)
        if (newItemSlot != null)
        {
            newItemSlot.sprite = null;
            newItemSlot.color = new Color(1, 1, 1, 0); // gör den genomskinlig om ingen sprite
        }
    }

    private void UpdateItemSlots(List<Item> items, Image[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Transform slot = slots[i].transform;

            // 🧹 Rensa tidigare objekt i slotten
            foreach (Transform child in slot)
            {
                Destroy(child.gameObject);
            }

            if (i < items.Count)
            {
                Item item = items[i];

                GameObject itemObj = Instantiate(swapItemPrefab, slot);
                itemObj.transform.localPosition = Vector3.zero;

                Image img = itemObj.GetComponent<Image>();
                img.sprite = item.itemIcon;

                SwapItemUI swapUI = itemObj.GetComponent<SwapItemUI>();
                if (swapUI != null)
                {
                    swapUI.Setup(i, this);  // 👈 krävs för att kunna klicka
                }
            }
        }
    }
    private void UpdatePetSlots(List<GameObject> pets, Image[] slots)
    {
        int allowedPets = Inventory.Instance.GetMaxAllowedPets();

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].gameObject.SetActive(i < allowedPets); // 👈 detta döljer överskottet

            foreach (Transform child in slots[i].transform)
            {
                Destroy(child.gameObject);
            }

            if (i < pets.Count)
            {
                GameObject pet = pets[i];
                GameObject obj = Instantiate(swapItemPrefab, slots[i].transform);
                obj.transform.localPosition = Vector3.zero;

                Image img = obj.GetComponent<Image>();
                img.sprite = pet.GetComponent<MonsterStats>().icon;

                SwapItemUI ui = obj.GetComponent<SwapItemUI>();
                ui.Setup(i, this);
            }
        }
    }

    public void UpdateRewardsSlots()
    {
        // Clear any existing slots if necessary
        foreach (Transform child in rewardSlotParent)
        {
            Destroy(child.gameObject);
        }
        GameObject coinSlot = Instantiate(rewardSlotPrefab, rewardSlotParent);
        Image coinSlotImage = coinSlot.GetComponent<Image>();
        coinSlotImage.sprite = coinImage;
        TMP_Text label = coinSlot.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = rewardCoin.ToString();
        }

        // Iterate over each item in the combat inventory
        foreach (Item item in rewardItems)
        {
            // Instantiate a new inventory slot from the prefab
            GameObject newSlot = Instantiate(rewardSlotPrefab, rewardSlotParent);

            // Get the Image component from the slot
            Image slotImage = newSlot.GetComponent<Image>();

            // Set the sprite of the image to the item's sprite
            slotImage.sprite = item.itemSprite;
        }
        foreach (GameObject pet in rewardPets)
        {
            GameObject newSlot = Instantiate(rewardSlotPrefab, rewardSlotParent);
            Image slotImage = newSlot.GetComponent<Image>();
            MonsterStats petStats = pet.GetComponent<MonsterStats>();
            slotImage.sprite = petStats.icon;
        }
    }
    public void UpdateXpDisplay(int xpReward)
    {
        xpRewardText.text = "Experience: +" + xpReward.ToString();
    }
    public void SceneChooser()
    {
        initiatedFromSceneChooser = true;
        AddRewardToInventory();

        if (pendingSwapQueue.Count == 0)
        {
            LoadAppropriateScene(); // 🟢 går direkt om inget att swappa
        }
        else
        {
            // 🟡 Kör bara HandleNextPendingItem här om det fanns swaps
            HandleNextPendingItem(); // ➕ Visa första popupen
        }
    }
    private void LoadAppropriateScene()
    {
        if (levelUP)
            SceneController.instance.LoadScene("LevelUp");
        else
            SceneController.instance.LoadScene("Base");
    }


    public IEnumerator UpdateMonsterHuntStage(int characterId, string map, int newStage)
    {
        var dto = new UpdateMonsterHuntStageDTO
        {
            characterId = characterId,
            map = map,
            newStage = newStage
        };

        string json = JsonUtility.ToJson(dto);
        Debug.Log("📦 JSON som skickas: " + json); // Kontrollera att den innehåller alla fält

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest("http://localhost:5000/api/monsterhunt", "PUT");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        string token = PlayerPrefs.GetString("jwt");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Stage uppdaterades!");
        }
        else
        {
            Debug.LogError("❌ Misslyckades uppdatera stage: " + request.error);
            Debug.Log("Svar från server: " + request.downloadHandler.text);
        }
    }
}
