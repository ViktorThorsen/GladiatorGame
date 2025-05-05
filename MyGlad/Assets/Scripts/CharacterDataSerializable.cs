using System.Collections.Generic;

[System.Serializable]
public class CharacterDataSerializable
{
    public string charName;
    public int level;
    public int xp;

    public int health;
    public int attackDamage;
    public int lifeSteal;
    public int dodgeRate;
    public int critRate;
    public int stunRate;
    public int initiative;

    public int strength;
    public int agility;
    public int intellect;
    // Already an array, no changes needed
}
public class BodyPartsDataSerializable
{
    public string hair;
    public string eyes;
    public string chest;
    public string legs;
}

public class SkillDataSerializable
{
    public List<string> skillNames = new List<string>();
}
public class PetDataSerializable
{
    public List<string> petNames = new List<string>();
}

public class WeaponDataSerializable
{
    public List<string> weaponNames = new List<string>();
}

public class ConsumableDataSerializable
{
    public List<string> consumableNames = new List<string>();
}

public class CharacterWrapper
{
    public CharacterDataSerializable character;
    public BodyPartsDataSerializable bodyPartLabels;
    public SkillDataSerializable skills;
    public PetDataSerializable pets;
    public WeaponDataSerializable weapons;
    public ConsumableDataSerializable consumables;
    public ShortcutDataSerializable shortcuts;

}
public class CharacterIdDTO
{
    public int characterId;
}

public class LinkCharacterRequest
{
    public int userId;
    public int characterId;
}

public class EnergyResponse
{
    public int id;
    public int energy;
}

public class UpdateMonsterHuntStageDTO
{
    public int characterId;
    public string map;
    public int newStage;
}

public class ShortcutDataSerializable
{
    public List<ShortcutEntrySerializable> shortcuts;
}

public class ShortcutEntrySerializable
{
    public int slotIndex;
    public string weaponName;
}