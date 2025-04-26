using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.Networking;
using System.Collections;
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
    public void SaveCharacterToBackend()
    {
        StartCoroutine(SendCharacterDataCoroutine());
    }

    private IEnumerator SendCharacterDataCoroutine()
    {
        CharacterDataSerializable data = new CharacterDataSerializable
        {
            // Fyll på med dina fält som innan
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
        };
        BodyPartsDataSerializable bodyPartsData = new BodyPartsDataSerializable
        {
            hair = bodyPartLabels[0],
            eyes = bodyPartLabels[1],
            chest = bodyPartLabels[2],
            legs = bodyPartLabels[3],
        };

        SkillDataSerializable skillData = new SkillDataSerializable
        {
            skillNames = Inventory.Instance.GetSkills().Select(skill => skill.skillName).ToList()
        };

        PetDataSerializable petData = new PetDataSerializable
        {
            petNames = Inventory.Instance.GetPets().Select(pet => pet.GetComponent<MonsterStats>().MonsterName).ToList()
        };

        WeaponDataSerializable weaponData = new WeaponDataSerializable
        {
            weaponNames = Inventory.Instance.GetWeapons().Select(weapon => weapon.itemName).ToList()
        };
        ConsumableDataSerializable consumableData = new ConsumableDataSerializable
        {
            consumableNames = Inventory.Instance.GetConsumables().Select(consumable => consumable.itemName).ToList()
        };


        CharacterWrapper wrapper = new CharacterWrapper
        {
            character = data,
            bodyPartLabels = bodyPartsData,
            skills = skillData,
            pets = petData,
            weapons = weaponData,
            consumables = consumableData
        };

        string json = JsonConvert.SerializeObject(wrapper);
        Debug.Log("Serialized JSON: " + json); // Log the serialized JSON for debugging
        Debug.Log(json.ToString()); // Log the serialized JSON for debugging])
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        UnityWebRequest request = new UnityWebRequest("http://localhost:5000/api/characters", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Character saved to backend!");
        }
        else
        {
            Debug.LogError("Error saving character: " + request.error);
        }
    }

    public IEnumerator LoadCharacterFromBackend(ItemDataBase itemDataBase, PetDataBase petDataBase, SkillDataBase skillDataBase, int id)
    {
        yield return StartCoroutine(LoadCharacterCoroutine(itemDataBase, petDataBase, skillDataBase, id));
    }

    private IEnumerator LoadCharacterCoroutine(ItemDataBase itemDataBase, PetDataBase petDataBase, SkillDataBase skillDataBase, int id)
    {
        int characterId = id;
        UnityWebRequest request = UnityWebRequest.Get($"http://localhost:5000/api/characters/{characterId}");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            Debug.Log("Received character JSON: " + json);

            CharacterWrapper data = JsonConvert.DeserializeObject<CharacterWrapper>(json);

            // === Karaktärsdata ===
            charName = data.character.charName;
            level = data.character.level;
            xp = data.character.xp;
            health = data.character.health;
            attackDamage = data.character.attackDamage;
            lifeSteal = data.character.lifeSteal;
            dodgeRate = data.character.dodgeRate;
            critRate = data.character.critRate;
            stunRate = data.character.stunRate;
            initiative = data.character.initiative;
            strength = data.character.strength;
            agility = data.character.agility;
            intellect = data.character.intellect;

            // === Kroppsdelar ===
            bodyPartLabels = new string[]
            {
            data.bodyPartLabels.hair,
            data.bodyPartLabels.eyes,
            data.bodyPartLabels.chest,
            data.bodyPartLabels.legs
            };

            // === Inventering ===
            EnemyInventory.Instance.ClearInventory();

            foreach (string skillName in data.skills.skillNames)
            {
                var skill = skillDataBase.GetSkillByName(skillName);
                if (skill != null) EnemyInventory.Instance.AddSkillToInventory(skill);
            }

            foreach (string petName in data.pets.petNames)
            {
                var pet = petDataBase.GetPetByName(petName);
                if (pet != null) EnemyInventory.Instance.AddPetToInventory(pet);
            }

            foreach (string weaponName in data.weapons.weaponNames)
            {
                var weapon = itemDataBase.GetWeaponByName(weaponName);
                if (weapon != null) EnemyInventory.Instance.AddWeaponToInventory(weapon);
            }

            foreach (string consumableName in data.consumables.consumableNames)
            {
                var item = itemDataBase.GetConsumableByName(consumableName);
                if (item != null) EnemyInventory.Instance.AddConsumableToInventory(item);
            }

            Debug.Log("Character loaded from backend.");
        }
        else
        {
            Debug.LogError("Failed to load character: " + request.error);
        }
    }
}
