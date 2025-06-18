using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
[CreateAssetMenu(fileName = "ItemDataBase", menuName = "Inventory/ItemDataBase", order = 2)]
public class ItemDataBase : ScriptableObject
{
    [SerializeField] private List<Item> weapons;
    [SerializeField] private Item[] consumables;

    [SerializeField] private List<StageWeaponDrops> stageWeaponDrops;

    [SerializeField] private List<StageConsumableDrops> stageConsumableDrops;



    public int GetWeaponsCount()
    {
        return weapons.Count; // Use Length instead of Count for arrays
    }

    public Item GetWeapon(int index)
    {
        return weapons[index];
    }

    public Item GetRandomDropForStage(string stageName)
    {
        StageWeaponDrops found = stageWeaponDrops.Find(s => s.stageName == stageName);
        if (found == null || found.drops.Count == 0)
            return null;

        // Hämta vapennamn från inventoryt
        var ownedWeaponNames = Inventory.Instance.GetWeapons()
                                    .Select(w => w.itemName)
                                    .ToHashSet(); // snabbare sök

        List<(Item weapon, float dropRate)> successfulDrops = new List<(Item, float)>();

        foreach (var entry in found.drops)
        {
            // Hoppa över om vapnet redan ägs
            if (ownedWeaponNames.Contains(entry.weapon.itemName))
                continue;

            float roll = Random.Range(0f, 1f);

            if (roll <= entry.dropRate)
            {
                successfulDrops.Add((entry.weapon, entry.dropRate));
            }
        }

        if (successfulDrops.Count > 0)
        {
            // Returnera vapnet med lägst dropRate
            var leastCommon = successfulDrops.OrderBy(d => d.dropRate).First();
            return leastCommon.weapon;
        }

        // Inget lyckades – ingen drop
        return null;
    }
    public Item GetRandomConsumableForStage(string stageName)
    {
        StageConsumableDrops found = stageConsumableDrops.Find(s => s.stageName == stageName);
        if (found == null || found.drops.Count == 0)
            return null;

        List<Item> successfulDrops = new List<Item>();

        foreach (var entry in found.drops)
        {
            float roll = Random.Range(0f, 1f);
            if (roll <= entry.dropRate)
            {
                successfulDrops.Add(entry.consumable);
            }
        }

        if (successfulDrops.Count > 0)
        {
            return successfulDrops[Random.Range(0, successfulDrops.Count)];
        }

        return null;
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
