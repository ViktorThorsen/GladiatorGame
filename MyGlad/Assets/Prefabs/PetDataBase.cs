using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
[CreateAssetMenu(fileName = "PetDatabse", menuName = "Inventory/PetDatabase", order = 2)]
public class PetDataBase : ScriptableObject
{
    [SerializeField] private GameObject[] pets;
    [SerializeField] private List<StagePetDrops> stagePetDrops;

    public int GetPetsCount()
    {
        return pets.Length;
    }

    public GameObject GetPet(int index)
    {
        return pets[index];
    }
    public GameObject GetPetByName(string petName)
    {
        foreach (var pet in pets)
        {
            string name = pet.GetComponent<MonsterStats>().MonsterName;
            if (name == petName)
            {
                return pet;
            }
        }
        Debug.LogWarning("pet not found:");
        return null;
    }
    public GameObject GetRandomPetForStage(string stageName)
    {
        StagePetDrops found = stagePetDrops.Find(s => s.stageName == stageName);
        if (found == null || found.drops.Count == 0)
            return null;

        List<(GameObject pet, float dropRate)> successfulDrops = new();

        foreach (var entry in found.drops)
        {
            float roll = Random.Range(0f, 1f);
            Debug.Log($"🎲 Roll: {roll} vs DropRate: {entry.dropRate} för pet: {entry.pet.name}");
            if (roll <= entry.dropRate)
            {
                successfulDrops.Add((entry.pet, entry.dropRate));
            }
        }

        if (successfulDrops.Count > 0)
        {
            // Return the rarest one (lowest dropRate)
            var rarest = successfulDrops.OrderBy(d => d.dropRate).First();
            return rarest.pet;
        }

        return null;
    }
}