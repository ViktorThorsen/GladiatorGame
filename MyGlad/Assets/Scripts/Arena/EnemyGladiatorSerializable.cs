[System.Serializable]
public class EnemyGladiatorDataSerializable
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

    public string[] bodyPartLabels;   // Already an array, no changes needed

    // Inventory-related fields (change to arrays)
    public string[] weapons;      // Store weapon names
    public string[] consumables;  // Store consumable names
    public string[] skills;       // Store skill names
    public string[] pets;         // Store pet names
}

public class EnemyCharacterResponse
{
    public int id;
    public string name;
}
