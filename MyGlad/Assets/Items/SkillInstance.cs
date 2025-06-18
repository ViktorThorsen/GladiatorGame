[System.Serializable]
public class SkillInstance
{
    public string skillName;
    public int level;

    public SkillInstance(string name, int level = 1)
    {
        skillName = name;
        this.level = level;
    }
    public Skill GetSkillData()
    {
        return SkillDataBase.Instance.GetSkillByName(skillName);
    }
}