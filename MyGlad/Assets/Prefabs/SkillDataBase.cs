using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "SkillDataBase", menuName = "Inventory/SkillDataBase", order = 2)]
public class SkillDataBase : ScriptableObject
{
    [SerializeField] private Skill[] skills;

    public int GetSkillsCount()
    {
        return skills.Length; // Use Length instead of Count for arrays
    }

    public Skill GetSkill(int index)
    {
        return skills[index];
    }
    public Skill GetSkillByName(string skillName)
    {
        foreach (var skill in skills)
        {
            if (skill.skillName == skillName)
            {
                return skill;
            }
        }
        Debug.LogWarning("Skill not found: " + skillName);
        return null;
    }
}