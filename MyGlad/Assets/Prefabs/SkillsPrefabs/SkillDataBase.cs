using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "SkillDataBase", menuName = "Inventory/SkillDataBase", order = 2)]
public class SkillDataBase : ScriptableObject
{
    public static SkillDataBase Instance { get; private set; }

    public void InitializeInstance()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("⚠️ SkillDataBase.Instance is already set!");
        }
    }

    [SerializeField] private Skill[] skills;

    public int GetSkillsCount()
    {
        return skills.Length; // Use Length instead of Count for arrays
    }

    public Skill[] GetAllSkills()
    {
        return skills;
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