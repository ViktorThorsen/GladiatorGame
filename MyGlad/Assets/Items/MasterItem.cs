using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item", order = 1)]
public class Item : ScriptableObject
{
    public string itemName; // Name of the item
    public Sprite itemIcon; // Icon for the item (for the UI)
    public Sprite itemSprite;

    public Vector3 equippedPositionOffset;
    public Vector3 equippedRotation;
    public Vector3 equippedScale;

    public string description; // Description of the item
    public ItemType itemType; // If the item is consumable
    public abilityType abilityType;
    public int durability;
    public int healthRestorationAmount; // Example of an item property (for consumables)

    //Stats
    public int strength;
    public int agility;
    public int intellect;
    public int health;
    public int attackDamage;
    public int dodgeRate;
    public int critRate;
    public int stunRate;




    // Method to use the item
    public virtual void Use()
    {

    }
}