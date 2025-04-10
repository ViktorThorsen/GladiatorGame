using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting; // Use the correct namespace for UI Image components

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
    [SerializeField] private TMP_Text ADText;
    [SerializeField] private TMP_Text DRText;
    [SerializeField] private TMP_Text CRText;
    [SerializeField] private TMP_Text SRText;
    [SerializeField] private Slider xpBar;

    [SerializeField] private TMP_Text energyText;

    // Array to hold references to the UI item slots (assign in the Inspector)
    [SerializeField] private Image[] weaponSlots = new Image[12];
    [SerializeField] private Image[] consumableSlots = new Image[3];
    [SerializeField] private Image[] petSlots = new Image[3];

    [SerializeField] private GameObject skillImagePrefab;
    [SerializeField] private Transform skillPanel;




    public GameObject characterPrefab;   // Reference to the character prefab
    public Transform parentObj;          // Reference to the Canvas (assign in Inspector)
    public Transform charPos;            // Reference to the charPos Transform (assign in Inspector)

    void Start()
    {
        // Instantiate and set up the character using the CharacterManager
        GameObject characterObject = CharacterManager.InstantiateCharacter(
            CharacterData.Instance,
            characterPrefab,
            parentObj,
            charPos,
            new Vector3(0.7f, 0.7f, 1f) // Set the desired scale
        );

        if (characterObject != null)
        {
            // Apply the saved stats to the UI elements
            lvlText.text = CharacterData.Instance.Level.ToString();
            healthText.text += CharacterData.Instance.Health.ToString();

            strText.text += CharacterData.Instance.Strength.ToString();
            agiText.text += CharacterData.Instance.Agility.ToString();
            intText.text += CharacterData.Instance.Intellect.ToString();

            ADText.text += CharacterData.Instance.AttackDamage.ToString();
            DRText.text += CharacterData.Instance.DodgeRate.ToString();
            CRText.text += CharacterData.Instance.CritRate.ToString();
            SRText.text += CharacterData.Instance.StunRate.ToString();
            energyText.text = CharacterData.Instance.Energy.ToString();


        }

        UpdateXpBar();
        // Update the UI item slots based on the inventory
        UpdateInventorySlots();
        if (Inventory.Instance.HasSkill("BeastMaster"))
        {
            UpdatePetSlots();
        }
    }

    public void OnSaveButtonClick()
    {
        if (CharacterData.Instance != null)
        {
            CharacterData.Instance.SaveCharacterToBackend(); // Call the save method on the singleton instance
        }
        else
        {
            Debug.LogWarning("CharacterData instance not found!");
        }
    }

    public void OnLoadButtonClick()
    {
        if (CharacterData.Instance != null)
        {
            CharacterData.Instance.LoadCharacterFromBackend(itemDataBase, petDataBase, skillDataBase); // Load the data
        }
    }

    // Method to update the inventory slots in the UI
    private void UpdateInventorySlots()
    {
        // Make sure the inventory is not null
        if (Inventory.Instance != null)
        {
            // Get the inventory items
            List<Item> weaponItems = Inventory.Instance.GetWeapons();
            List<Item> consumableItems = Inventory.Instance.GetConsumables();
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
                    weaponSlots[i].color = new Color(60f / 255f, 47f / 255f, 47f / 255f, 1f);
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
                    consumableSlots[i].color = new Color(60f / 255f, 47f / 255f, 47f / 255f, 1f);
                }
            }
            // Clear any existing slots if necessary
            foreach (Transform child in skillPanel)
            {
                Destroy(child.gameObject);
            }
            // Iterate over each item in the combat inventory
            foreach (Skill skill in Inventory.Instance.GetSkills())
            {
                // Instantiate a new inventory slot from the prefab
                GameObject newSlot = Instantiate(skillImagePrefab, skillPanel);

                // Get the Image component from the slot
                Image slotImage = newSlot.GetComponent<Image>();

                // Set the sprite of the image to the item's sprite
                slotImage.sprite = skill.skillIcon;
            }
        }
    }
    public void UpdatePetSlots()
    {
        // If the player has BeastMaster skill, update the pet slots normally
        List<GameObject> pets = Inventory.Instance.GetPets();
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
                        petSlots[i].color = new Color(1, 1, 1, 1);  // Set full opacity (active)
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }
            else
            {
                petSlots[i].sprite = null;
                petSlots[i].color = new Color(1, 1, 1, 1);
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
}