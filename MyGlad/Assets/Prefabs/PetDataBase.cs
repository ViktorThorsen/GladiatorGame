using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PetDatabse", menuName = "Inventory/PetDatabase", order = 2)]
public class PetDataBase : ScriptableObject
{
    [SerializeField] private GameObject[] pets;

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
}