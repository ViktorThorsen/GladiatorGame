using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ItemDataBase", menuName = "Inventory/ItemDataBase", order = 2)]
public class ItemDataBase : ScriptableObject
{
    [SerializeField] private Item[] weapons;
    [SerializeField] private Item[] consumables;



    public int GetWeaponsCount()
    {
        return weapons.Length; // Use Length instead of Count for arrays
    }

    public Item GetWeapon(int index)
    {
        return weapons[index];
    }

    public int GetConsumablesCount()
    {
        return consumables.Length; // Use Length instead of Count for arrays
    }

    public Item GetConsumable(int index)
    {
        return consumables[index];
    }
    public Item GetWeaponByName(string weaponName)
    {
        foreach (var weapon in weapons)
        {
            if (weapon.itemName == weaponName)
            {
                return weapon;
            }
        }
        Debug.LogWarning("Item not found: " + weaponName);
        return null;
    }
    public Item GetConsumableByName(string consumableName)
    {
        foreach (var consumable in consumables)
        {
            if (consumable.itemName == consumableName)
            {
                return consumable;
            }
        }
        Debug.LogWarning("consumable not found: " + consumableName);
        return null;
    }

}
