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
    [SerializeField] private int energy;
    [SerializeField] private int health;
    [SerializeField] private int hitRate;
    [SerializeField] private int lifeSteal;
    [SerializeField] private int dodgeRate;
    [SerializeField] private int critRate;
    [SerializeField] private int stunRate;
    [SerializeField] private int fortune;
    [SerializeField] private int intellect;
    [SerializeField] public int precision;
    [SerializeField] public int initiative;
    [SerializeField] public int combo;

    //Just for Show
    [SerializeField] private int strength;
    [SerializeField] private int agility;
    [SerializeField] private int defense;


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

    public int Energy
    {
        get { return energy; }
        set { energy = value; }
    }

    public int Health
    {
        get { return health; }
        set { health = value; }
    }
    public int Defense
    {
        get { return defense; }
        set { defense = value; }
    }
    public int HitRate
    {
        get { return hitRate; }
        set { hitRate = value; }
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

    public int Fortune
    {
        get { return fortune; }
        set { fortune = value; }
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
    public void AddStrAgiInt(int str, int agi, int inte, int health, int hit, int defense, int fortu, int stun, int lifeSt, int ini, int comb)
    {
        Health += health * 5;

        Strength += str;
        if (hitRate - str / 2 >= 0)
        {
            hitRate -= str / 2;
        }

        DodgeRate += agi;
        critRate += agi;

        if (hitRate - agi / 2 >= 0)
        {
            hitRate -= agi / 2;
        }
        Agility += agi;

        intellect += inte;

        if (hitRate + hit >= 0)
        {
            HitRate += hit;
        }

        Fortune += fortu;

        StunRate += stun;

        LifeSteal += lifeSt;

        Defense += defense;
        precision += hit;
        initiative += ini;
        combo += combo;
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

    public void AddEquipStats(int str, int agi, int inte, int health, int hit, int defense, int fortu, int stun, int lifeSt, int ini, int comb)
    {
        AddStrAgiInt(str, agi, inte, health, hit, defense, fortu, stun, lifeSt, ini, comb);
    }

    public void RemoveEquipStats(int str, int agi, int inte, int health, int hit, int defense, int fortu, int stun, int lifeSt, int ini, int comb)
    {
        // Reverse the stats added by the AddStrAgiInt method
        Health -= health * 5;
        Strength -= str;
        hitRate += str / 2;
        DodgeRate -= agi;
        critRate -= agi;
        hitRate += agi / 2;
        intellect -= inte;
        HitRate -= hit;
        Defense -= defense;
        Fortune -= fortu;
        StunRate -= stun;
        LifeSteal -= lifeSt;
        precision -= hit;
        initiative -= initiative;
        combo -= comb;

    }

    private void BaseStats()
    {
        Level = 1;
        Xp = 0;
        Health = 50;
        Energy = 10;
        Strength = 1;
        dodgeRate = 0;
        critRate = 0;
        stunRate = 0;
        intellect = 0;
        HitRate = 100;
        Fortune = 0;
        StunRate = 0;
        Defense = 0;
        precision = 0;
        initiative = 0;
        combo = 0;
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
            lifeSteal = this.lifeSteal,
            dodgeRate = this.dodgeRate,
            critRate = this.critRate,
            stunRate = this.stunRate,
            hitRate = this.hitRate,
            fortune = this.fortune,
            strength = this.strength,
            agility = this.agility,
            intellect = this.intellect,
            defense = this.Defense,
            precision = this.precision,
            initiative = this.initiative,
            combo = this.combo
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
            skills = Inventory.Instance.GetSkills()
        .Select(skill => new SkillEntrySerializable
        {
            skillName = skill.skillName,
            level = skill.level
        }).ToList()
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
            lifeSteal = data.character.lifeSteal;
            dodgeRate = data.character.dodgeRate;
            critRate = data.character.critRate;
            stunRate = data.character.stunRate;
            fortune = data.character.fortune;
            hitRate = data.character.hitRate;
            defense = data.character.defense;
            strength = data.character.strength;
            agility = data.character.agility;
            intellect = data.character.intellect;
            precision = data.character.precision;
            initiative = data.character.initiative;
            combo = data.character.combo;

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

            foreach (var skillEntry in data.skills.skills)
            {
                var skill = skillDataBase.GetSkillByName(skillEntry.skillName);
                if (skill != null)
                {
                    EnemyInventory.Instance.AddSkillInstanceToInventory(new SkillInstance(skillEntry.skillName, skillEntry.level));
                }
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
            if (data.shortcuts?.shortcuts != null)
            {
                EnemyInventory.Instance.shortcutWeaponIndexes = Enumerable.Repeat(-1, EnemyInventory.Instance.GetWeapons().Count).ToList();

                // Bygg om shortcut-listan med namn-matchning
                foreach (var shortcut in data.shortcuts.shortcuts)
                {
                    string weaponName = shortcut.weaponName;
                    int shortcutSlot = shortcut.slotIndex;

                    int weaponIndex = EnemyInventory.Instance.GetWeapons()
                        .FindIndex(w => w.itemName == weaponName);

                    if (weaponIndex != -1)
                    {
                        EnemyInventory.Instance.shortcutWeaponIndexes[weaponIndex] = shortcutSlot;
                        Debug.Log($"🔁 Loaded shortcut: weapon '{weaponName}' → slot {shortcutSlot}");
                    }
                }
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
