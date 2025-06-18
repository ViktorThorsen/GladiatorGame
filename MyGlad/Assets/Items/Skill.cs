using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Inventory/Skill", order = 1)]
public class Skill : ScriptableObject
{
    public bool isLevelable;
    public string skillName; // Name of the item
    public Sprite skillIcon; // Icon for the item (for the UI)
    public Sprite skillprite;

    public Vector3 equippedPositionOffset;
    public Vector3 equippedRotation;
    public Vector3 equippedScale;

    public string description; // Description of the item

    public int effectPercentIncreaseLevel1;
    public int effectPercentIncreaseLevel2;
    public int effectPercentIncreaseLevel3;

    //Stats
    public int strength;
    public int agility;
    public int intellect;
    public int health;
    public int dodgeRate;
    public int critRate;
    public int stunRate;

    public int hit;
    public int defense;

    public int lifesteal;

    public int initiative;
    public int combo;




    // Method to use the item
    public virtual void Use()
    {

    }
}
