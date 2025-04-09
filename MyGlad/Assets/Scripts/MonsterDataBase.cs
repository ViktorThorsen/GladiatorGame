using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "MonsterDataBase", menuName = "ScriptableObjects/MonsterDataBase", order = 2)]
public class MonsterDataBase : ScriptableObject
{
    [SerializeField] private GameObject[] monsters;

    public GameObject GetMonster(int index)
    {
        return monsters[index];
    }

    public GameObject GetMonsterByName(string monsterName)
    {
        foreach (var monster in monsters)
        {
            MonsterStats monsterStats = monster.GetComponent<MonsterStats>();
            if (monsterStats.MonsterName == monsterName)
                return monster;
        }

        Debug.LogWarning($"Monster with name '{monsterName}' not found.");
        return null; // Return null if not found
    }
}
