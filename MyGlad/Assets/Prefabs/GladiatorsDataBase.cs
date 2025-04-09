using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "GladiatorDataBase", menuName = "Inventory/GladiatorDataBase", order = 2)]
public class GladiatorDataBase : ScriptableObject
{
    [SerializeField] private GameObject[] gladiators;

    public int GetGladiatorsCount()
    {
        return gladiators.Length; // Use Length instead of Count for arrays
    }

    public GameObject GetGladiator(int index)
    {
        return gladiators[index];
    }

}
