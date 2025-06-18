using UnityEngine;
using System.Collections.Generic;

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

    public abilityType abilityType1;
    public int durability;
    public int healthRestorationAmount; // Example of an item property (for consumables)

    //Stats
    public int strength;
    public int agility;
    public int intellect;
    public int health;
    public int hit;
    public int defense;
    public int stunRate;
    public int lifesteal;

    public int initiative;
    public int combo;


    // Method to use the item
    public virtual void Use()
    {

    }
}