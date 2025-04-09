using System.Collections.Generic;
using UnityEngine;

public class EnemyInventory : MonoBehaviour
{
    public static EnemyInventory Instance { get; private set; }
    private List<Item> inventoryWeapons;
    private List<Item> inventoryConsumables;
    private List<Skill> inventorySkills;

    private List<GameObject> inventoryPets;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist this instance across scenes

            // Initialize the inventory list here
            inventoryWeapons = new List<Item>();
            inventoryConsumables = new List<Item>();
            inventoryPets = new List<GameObject>();
            inventorySkills = new List<Skill>();
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    public List<Item> GetWeapons()
    {
        return inventoryWeapons;
    }

    public Item GetRandomItem()
    {
        // Ensure that the inventoryWeapons is not empty
        if (inventoryWeapons.Count == 0)
        {

            return null;
        }

        // Get a random index based on the inventoryWeapons count
        int randomIndex = Random.Range(0, inventoryWeapons.Count);

        // Return the item at the random index
        return inventoryWeapons[randomIndex];
    }

    public void AddWeaponToInventory(Item item)
    {
        if (item != null)
        {
            inventoryWeapons.Add(item);

        }
        else
        {

        }
    }
    public List<Item> GetConsumables()
    {
        return inventoryConsumables;
    }
    public Item GetRandomConsumable()
    {
        if (inventoryConsumables.Count == 0) { return null; }
        int randomIndex = Random.Range(0, inventoryConsumables.Count);
        return inventoryConsumables[randomIndex];
    }

    public void AddConsumableToInventory(Item item)
    {
        if (item != null)
        {
            inventoryConsumables.Add(item);
        }
    }

    public List<GameObject> GetPets()
    {
        return inventoryPets;
    }
    public GameObject GetRandomPet()
    {
        if (inventoryPets.Count == 0) { return null; }
        int randomIndex = Random.Range(0, inventoryPets.Count);
        return inventoryPets[randomIndex];
    }

    public void AddPetToInventory(GameObject pet)
    {
        if (pet != null)
        {
            inventoryPets.Add(pet);
        }
    }

    public List<Skill> GetSkills()
    {
        return inventorySkills;
    }
    public bool HasSkill(string skillName)
    {
        // Loop through the player's skills and check if the given skill is present
        foreach (Skill skill in GetSkills())
        {
            if (skill.skillName == skillName)
            {
                return true;  // Return true if the skill is found
            }
        }
        return false;  // Return false if the skill is not found
    }

    public void AddSkillToInventory(Skill skill)
    {
        if (skill != null)
        {
            inventorySkills.Add(skill);
        }
    }

    public void ClearInventory()
    {
        inventoryWeapons.Clear();
        inventoryConsumables.Clear();
        inventorySkills.Clear();
        inventoryPets.Clear();

        Debug.Log("Inventory cleared.");
    }
}
