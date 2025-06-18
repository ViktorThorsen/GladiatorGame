using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }
    public List<Item> inventoryWeapons;
    private List<Item> inventoryConsumables;
    private List<SkillInstance> inventorySkills;

    private List<GameObject> inventoryPets;

    public List<int> shortcutWeaponIndexes;

    public int maxWeapons = 20;
    public int maxConsumables = 3;
    public int maxPets = 3;

    private bool isDirty = false;
    public void MarkAsDirty() => isDirty = true;
    public bool HasChanges() => isDirty;
    public void ClearDirtyFlag() => isDirty = false;

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
            inventorySkills = new List<SkillInstance>();
            shortcutWeaponIndexes = new List<int>();
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

    public int GetMaxAllowedPets()
    {
        var beastmaster = GetSkillInstance("BeastMaster");
        return beastmaster != null ? beastmaster.level : 0;
    }

    public void ReplaceWeaponAt(int index, Item item)
    {
        if (index >= 0 && index < inventoryWeapons.Count)
        {
            inventoryWeapons[index] = item;
        }
    }

    public void ReplaceConsumableAt(int index, Item item)
    {
        if (index >= 0 && index < inventoryConsumables.Count)
        {
            inventoryConsumables[index] = item;
        }
    }

    public void RemoveWeapon(Item item)
    {
        int index = inventoryWeapons.IndexOf(item);
        if (index != -1)
        {
            inventoryWeapons.RemoveAt(index);
            shortcutWeaponIndexes.RemoveAt(index);
            MarkAsDirty();
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

    public List<SkillInstance> GetSkills()
    {
        return inventorySkills;
    }
    public bool HasSkill(string skillName)
    {
        // Loop through the player's skills and check if the given skill is present
        foreach (SkillInstance skill in GetSkills())
        {
            if (skill.skillName == skillName)
            {
                return true;  // Return true if the skill is found
            }
        }
        return false;  // Return false if the skill is not found
    }
    public SkillInstance GetSkillInstance(string skillName)
    {
        return inventorySkills.FirstOrDefault(s => s.skillName == skillName);
    }

    public void AddSkillToInventory(Skill skill)
    {
        if (skill == null) return;

        var existingSkill = inventorySkills.FirstOrDefault(s => s.skillName == skill.skillName);

        if (existingSkill != null)
        {
            if (skill.isLevelable)
            {
                existingSkill.level += 1;
                Debug.Log($"⬆️ Upgraded {skill.skillName} to level {existingSkill.level}");
            }
            else
            {
                Debug.Log($"⚠️ Skill {skill.skillName} already exists and is not levelable.");
            }
        }
        else
        {
            inventorySkills.Add(new SkillInstance(skill.skillName, 1));
            Debug.Log($"🆕 Added skill: {skill.skillName} (Level 1)");
        }
    }

    public void AddSkillInstanceToInventory(SkillInstance skillInstance)
    {
        var existingSkill = inventorySkills.FirstOrDefault(s => s.skillName == skillInstance.skillName);
        if (existingSkill != null)
        {
            existingSkill.level = skillInstance.level;
        }
        else
        {
            inventorySkills.Add(skillInstance);
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

    public void ClearShortcutItem(int index)
    {
        if (index >= 0 && index < shortcutWeaponIndexes.Count)
        {
            shortcutWeaponIndexes[index] = -1;
        }
    }
    public void AssignWeaponToShortcut(int weaponIndex, int shortcutSlot)
    {
        if (!IsValidIndex(weaponIndex, inventoryWeapons)) return;

        // Se till att listan är lika lång som vapenantalet
        while (shortcutWeaponIndexes.Count < inventoryWeapons.Count)
        {
            shortcutWeaponIndexes.Add(-1);
        }

        // Rensa tidigare slot som använde denna plats
        for (int i = 0; i < shortcutWeaponIndexes.Count; i++)
        {
            if (shortcutWeaponIndexes[i] == shortcutSlot)
            {
                shortcutWeaponIndexes[i] = -1;
            }
        }

        // Tilldela slotten till rätt vapen
        shortcutWeaponIndexes[weaponIndex] = shortcutSlot;

        Debug.Log($"🗂️ Weapon index {weaponIndex} assigned to shortcut slot {shortcutSlot}.");
    }

    public void SwapWeapons(int indexA, int indexB)
    {
        if (IsValidIndex(indexA, inventoryWeapons) && IsValidIndex(indexB, inventoryWeapons))
        {
            // Byt vapnen
            (inventoryWeapons[indexA], inventoryWeapons[indexB]) = (inventoryWeapons[indexB], inventoryWeapons[indexA]);

            // Justera shortcutIndex-listan så att shortcutslotarna följer med rätt vapen
            int tmpSlot = shortcutWeaponIndexes[indexA];
            shortcutWeaponIndexes[indexA] = shortcutWeaponIndexes[indexB];
            shortcutWeaponIndexes[indexB] = tmpSlot;
            MarkAsDirty();
            Debug.Log($"🔁 Swapped weapons at {indexA} and {indexB}, including shortcut mapping.");
        }
    }

    public void SwapConsumables(int indexA, int indexB)
    {
        if (IsValidIndex(indexA, inventoryConsumables) && IsValidIndex(indexB, inventoryConsumables))
        {
            (inventoryConsumables[indexA], inventoryConsumables[indexB]) = (inventoryConsumables[indexB], inventoryConsumables[indexA]);
            MarkAsDirty();
            Debug.Log($"🔁 Swapped weapons at {indexA} and {indexB}, including shortcut mapping.");
        }
    }

    public void SwapPets(int indexA, int indexB)
    {
        if (IsValidIndex(indexA, inventoryPets) && IsValidIndex(indexB, inventoryPets))
        {
            (inventoryPets[indexA], inventoryPets[indexB]) = (inventoryPets[indexB], inventoryPets[indexA]);
            MarkAsDirty();
        }
    }

    private bool IsValidIndex<T>(int index, List<T> list)
    {
        return index >= 0 && index < list.Count;
    }
}
