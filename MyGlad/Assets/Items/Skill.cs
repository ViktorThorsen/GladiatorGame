using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Inventory/Skill", order = 1)]
public class Skill : ScriptableObject
{
    public int skillLevel;
    public string skillName; // Name of the item
    public Sprite skillIcon; // Icon for the item (for the UI)
    public Sprite skillprite;

    public Vector3 equippedPositionOffset;
    public Vector3 equippedRotation;
    public Vector3 equippedScale;

    public string description; // Description of the item

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
