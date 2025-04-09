using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class RewardSystem : MonoBehaviour
{
    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private MonsterDataBase monsterDataBase;
    [SerializeField] private PetDataBase petDataBase;
    [SerializeField] private Image[] weaponSlots = new Image[12];
    [SerializeField] private Image[] consumableSlots = new Image[3];
    [SerializeField] private Image[] petSlots = new Image[3];

    [SerializeField] private TMP_Text xpRewardText;

    [SerializeField] private Transform rewardSlotParent;
    [SerializeField] private GameObject rewardSlotPrefab;
    private bool isLastFightAWin;
    private string[] lastFightNames;
    private bool levelUP;

    private List<Item> rewardItems = new List<Item>();
    private List<GameObject> rewardPets = new List<GameObject>();


    void Start()
    {
        GetLastFight();
        levelUP = false;
        UpdateInventorySlots();
        if (isLastFightAWin)
        {
            Item newWeapon = GetWeaponReward();
            rewardItems.Add(newWeapon);
            Item newConsumable = GetConsumableReward();
            rewardItems.Add(newConsumable);
            foreach (Skill skill in Inventory.Instance.GetSkills())
            {
                if (skill.skillName == "BeastMaster")
                {
                    // If the BeastMaster skill is found, add a pet
                    GameObject newPet = GetPetReward();
                    rewardPets.Add(newPet);
                }
            }

            AddRewardToInventory();
            int rewardXP = GetXpReward();
            AddXpToCharacter(rewardXP);
            UpdateXpDisplay(rewardXP);
        }
        UpdateRewardsSlots();

    }

    private void GetLastFight()
    {
        lastFightNames = FightData.Instance.GetLastFightResultNames();
        isLastFightAWin = FightData.Instance.GetLastFightResultWinOrLoss();
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
            if (item.itemType == ItemType.OneHandWeapon || item.itemType == ItemType.TwoHandWeapon)
                if (Inventory.Instance.GetWeapons().Count < 12)
                {
                    Inventory.Instance.AddWeaponToInventory(item);
                }
                else { }
            else if (item.itemType == ItemType.Consumable)
            {
                if (Inventory.Instance.GetConsumables().Count < 3)
                {
                    Inventory.Instance.AddConsumableToInventory(item);
                }
                else { }
            }
        }
        foreach (GameObject pet in rewardPets)
        {
            if (Inventory.Instance.GetPets().Count < 3)
            {
                Inventory.Instance.AddPetToInventory(pet);
            }
            else { }
        }
    }
    public void UpdateRewardsSlots()
    {
        // Clear any existing slots if necessary
        foreach (Transform child in rewardSlotParent)
        {
            Destroy(child.gameObject);
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
            SpriteRenderer petSprite = pet.GetComponent<SpriteRenderer>();
            slotImage.sprite = petSprite.sprite;
        }
    }

    private void UpdateInventorySlots()
    {
        // Make sure the inventory is not null
        if (Inventory.Instance != null)
        {
            // Get the inventory items
            List<Item> weaponItems = Inventory.Instance.GetWeapons();
            List<Item> consumableItems = Inventory.Instance.GetConsumables();
            List<GameObject> pets = Inventory.Instance.GetPets();



            // Loop through the inventory items and assign to slots
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                // If there's an item at this index, assign its sprite to the slot
                if (i < weaponItems.Count)
                {
                    Item item = weaponItems[i];
                    weaponSlots[i].sprite = item.itemIcon;
                    weaponSlots[i].color = new Color(1, 1, 1, 1); // Make the slot fully visible
                }
                else
                {
                    // Clear the slot if there's no corresponding item
                    weaponSlots[i].sprite = null;
                    weaponSlots[i].color = new Color(60f / 255f, 47f / 255f, 47f / 255f, 1f); // Make the slot fully transparent
                }
            }
            for (int i = 0; i < consumableSlots.Length; i++)
            {
                // If there's an item at this index, assign its sprite to the slot
                if (i < consumableItems.Count)
                {
                    Item item = consumableItems[i];
                    consumableSlots[i].sprite = item.itemIcon;
                    consumableSlots[i].color = new Color(1, 1, 1, 1); // Make the slot fully visible
                }
                else
                {
                    // Clear the slot if there's no corresponding item
                    consumableSlots[i].sprite = null;
                    consumableSlots[i].color = new Color(60f / 255f, 47f / 255f, 47f / 255f, 1f); // Make the slot fully transparent
                }
            }
            for (int i = 0; i < petSlots.Length; i++)
            {
                if (i < pets.Count)
                {
                    GameObject pet = pets[i];
                    if (pet != null)
                    {
                        SpriteRenderer petsprite = pet.GetComponent<SpriteRenderer>();
                        if (petsprite != null)
                        {
                            petSlots[i].sprite = petsprite.sprite;
                            petSlots[i].color = new Color(1, 1, 1, 1);
                        }
                        else
                        {
                            Debug.LogWarning("SpriteRenderer missing on pet: " + pet.name);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Pet is null at index " + i);
                    }
                }
                else
                {
                    petSlots[i].sprite = null;
                    petSlots[i].color = new Color(60f / 255f, 47f / 255f, 47f / 255f, 1f);
                }
            }
        }
    }
    public void UpdateXpDisplay(int xpReward)
    {
        xpRewardText.text = "Experience: +" + xpReward.ToString();
    }
    public void SceneChooser()
    {
        if (levelUP == false)
        {
            SceneController.instance.LoadScene("Base");
        }
        else if (levelUP == true)
        {

            SceneController.instance.LoadScene("LevelUp");
        }
    }
}
