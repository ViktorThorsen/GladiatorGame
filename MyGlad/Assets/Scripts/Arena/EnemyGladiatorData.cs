using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
public class EnemyGladiatorData : MonoBehaviour
{
    public static EnemyGladiatorData Instance { get; private set; }

    // Private fields
    [SerializeField] private string charName;
    [SerializeField] private int level;
    [SerializeField] private int xp;


    [SerializeField] private int health;
    [SerializeField] private int attackDamage;
    [SerializeField] private int lifeSteal;
    [SerializeField] private int dodgeRate;
    [SerializeField] private int critRate;
    [SerializeField] private int stunRate;

    [SerializeField] private int initiative;

    [SerializeField] private int strength;
    [SerializeField] private int agility;
    [SerializeField] private int intellect;

    [SerializeField] private string[] bodyPartLabels;



    // Public properties
    public string CharName
    {
        get { return charName; }
        set { charName = value; }
    }

    public int Level
    {
        get { return level; }
        set { level = value; }
    }

    public int Xp
    {
        get { return xp; }
        set { xp = value; }
    }

    public int Health
    {
        get { return health; }
        set { health = value; }
    }

    public int AttackDamage
    {
        get { return attackDamage; }
        set { attackDamage = value; }
    }

    public int LifeSteal
    {
        get { return lifeSteal; }
        set { lifeSteal = value; }
    }

    public int DodgeRate
    {
        get { return dodgeRate; }
        set { dodgeRate = value; }
    }

    public int CritRate
    {
        get { return critRate; }
        set { critRate = value; }
    }

    public int StunRate
    {
        get { return stunRate; }
        set { stunRate = value; }
    }

    public int Initiative
    {
        get { return initiative; }
        set { initiative = value; }
    }

    public int Strength
    {
        get { return strength; }
        set { strength = value; }
    }

    public int Agility
    {
        get { return agility; }
        set { agility = value; }
    }

    public int Intellect
    {
        get { return intellect; }
        set { intellect = value; }
    }

    public string[] BodyPartLabels
    {
        get { return bodyPartLabels; }
        set { bodyPartLabels = value; }
    }



    // AddStrAgiInt method
    public void AddStrAgiInt(int str, int agi, int inte)
    {
        Health += str * 5;
        AttackDamage += str * 5;
        DodgeRate += agi * 5;
        CritRate += agi * 5;
        Initiative += inte * 5;
        Strength += str;
        Agility += agi;
        Intellect += inte;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist this instance across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
        BaseStats();
    }

    public void AddEquipStats(int str, int agi, int inte, int health, int attack, int dodge, int crit, int stun)
    {
        AddStrAgiInt(str, agi, inte);
        Health += health;
        AttackDamage += attack;
        DodgeRate += dodge;
        CritRate += crit;
        StunRate += stun;
    }

    public void RemoveEquipStats(int str, int agi, int inte, int health, int attack, int dodge, int crit, int stun)
    {
        // Reverse the stats added by the AddStrAgiInt method
        Health -= str * 5;
        AttackDamage -= str * 5;
        DodgeRate -= agi * 5;
        CritRate -= agi * 5;
        Initiative -= inte * 5;
        Strength -= str;
        Agility -= agi;
        Intellect -= inte;

        // Reverse the additional stats added by the AddEquipStats method
        Health -= health;
        AttackDamage -= attack;
        DodgeRate -= dodge;
        CritRate -= crit;
        StunRate -= stun;

    }

    private void BaseStats()
    {
        Level = 1;
        Xp = 0;
        Health = 100;
        attackDamage = 20;
        dodgeRate = 1;
        critRate = 1;
        initiative = 0;
        stunRate = 1;
    }

    // Method to save the EnemyGladiator data
    public void SaveEnemyGladiatorAndInventory(string fileName)
    {
        EnemyGladiatorDataSerializable data = new EnemyGladiatorDataSerializable
        {
            // EnemyGladiator data
            charName = this.charName,
            level = this.level,
            xp = this.xp,
            health = this.health,
            attackDamage = this.attackDamage,
            lifeSteal = this.lifeSteal,
            dodgeRate = this.dodgeRate,
            critRate = this.critRate,
            stunRate = this.stunRate,
            initiative = this.initiative,
            strength = this.strength,
            agility = this.agility,
            intellect = this.intellect,
            bodyPartLabels = this.bodyPartLabels,

            // Convert lists to arrays
            weapons = EnemyInventory.Instance.GetWeapons().Select(w => w.itemName).ToArray(),
            consumables = EnemyInventory.Instance.GetConsumables().Select(c => c.itemName).ToArray(),
            skills = EnemyInventory.Instance.GetSkills().Select(s => s.skillName).ToArray(),
            pets = EnemyInventory.Instance.GetPets().Select(p => p.name).ToArray()  // Store pet names
        };

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(filePath, json);

        Debug.Log("EnemyGladiator and inventory data saved to " + filePath);
    }
    public void LoadEnemyGladiatorAndInventory(ItemDataBase itemDataBase, PetDataBase petDataBase, SkillDataBase skillDataBase, string fileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            EnemyGladiatorDataSerializable data = JsonConvert.DeserializeObject<EnemyGladiatorDataSerializable>(json);

            // Apply EnemyGladiator data
            this.charName = data.charName;
            this.level = data.level;
            this.xp = data.xp;
            this.health = data.health;
            this.attackDamage = data.attackDamage;
            this.lifeSteal = data.lifeSteal;
            this.dodgeRate = data.dodgeRate;
            this.critRate = data.critRate;
            this.stunRate = data.stunRate;
            this.initiative = data.initiative;
            this.strength = data.strength;
            this.agility = data.agility;
            this.intellect = data.intellect;
            this.bodyPartLabels = data.bodyPartLabels;

            // Clear the current inventory
            EnemyInventory.Instance.ClearInventory();

            // Retrieve pets, skills, and items by name and add them back to the inventory
            foreach (string petName in data.pets)
            {
                GameObject petPrefab = petDataBase.GetPetByName(petName);
                if (petPrefab != null)
                {
                    EnemyInventory.Instance.AddPetToInventory(petPrefab);
                }
                else
                {
                    Debug.LogWarning($"Pet not found: {petName}");
                }
            }

            foreach (string skillName in data.skills)
            {
                Skill skill = skillDataBase.GetSkillByName(skillName);
                if (skill != null)
                {
                    EnemyInventory.Instance.AddSkillToInventory(skill);
                }
                else
                {
                    Debug.LogWarning($"Skill not found: {skillName}");
                }
            }

            foreach (string itemName in data.weapons)
            {
                Item item = itemDataBase.GetWeaponByName(itemName);
                if (item != null)
                {
                    EnemyInventory.Instance.AddWeaponToInventory(item);
                }
                else
                {
                    Debug.LogWarning($"Item not found: {itemName}");
                }
            }

            foreach (string consumableName in data.consumables)
            {
                Item consumable = itemDataBase.GetConsumableByName(consumableName); // Assuming consumables are items
                if (consumable != null)
                {
                    EnemyInventory.Instance.AddConsumableToInventory(consumable);
                }
                else
                {
                    Debug.LogWarning($"Consumable not found: {consumableName}");
                }
            }

            Debug.Log("EnemyGladiator and inventory data loaded successfully.");
        }
        else
        {
            Debug.LogWarning("Save file not found at " + filePath);
        }
    }
}
