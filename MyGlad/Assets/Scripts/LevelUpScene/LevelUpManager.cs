using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting.Dependencies.NCalc;

public class LevelUpManager : MonoBehaviour
{
    public GameObject characterPrefab;   // Reference to the character prefab
    public Transform parentObj;          // Reference to the Canvas (assign in Inspector)
    public Transform charPos;

    [SerializeField] private SkillDataBase skillDataBase;
    [SerializeField] private Transform newSkillPanel;
    private Skill newSkill;
    [SerializeField] private TMP_Text availablePointsText;
    private int availablePoints;
    [SerializeField] private TMP_Text strText;
    [SerializeField] private TMP_Text agiText;
    [SerializeField] private TMP_Text intText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text defText;
    [SerializeField] private TMP_Text precText;

    [SerializeField] private TMP_Text levelText;
    [SerializeField] private PetDataBase petDataBase;
    private int strToAdd;
    private int agiToAdd;
    private int intToAdd;
    private int healthToAdd;
    private int defToAdd;
    private int precToAdd;
    private int fortToAdd;



    // Start is called before the first frame update
    void Start()
    {
        strToAdd = 0;
        agiToAdd = 0;
        intToAdd = 0;
        defToAdd = 0;
        healthToAdd = 0;
        precToAdd = 0;
        fortToAdd = 0;
        availablePoints = 5;
        strText.text = CharacterData.Instance.Strength.ToString();
        agiText.text = CharacterData.Instance.Agility.ToString();
        intText.text = CharacterData.Instance.Intellect.ToString();
        healthText.text = CharacterData.Instance.Health.ToString();
        defText.text = CharacterData.Instance.Defense.ToString();
        precText.text = CharacterData.Instance.HitRate.ToString();
        levelText.text = "LVL." + CharacterData.Instance.Level;
        GameObject characterObject = CharacterManager.InstantiateCharacter(
            CharacterData.Instance,
            characterPrefab,
            parentObj,
            charPos,
            new Vector3(0.7f, 0.7f, 1f) // Set the desired scale
        );
        newSkill = GetSkillReward();
        if (newSkill != null)
        {

            // Activate the newSkillPanel
            newSkillPanel.gameObject.SetActive(true);

            // Access the Image component inside the panel
            Image newSkillImage = newSkillPanel.GetChild(0).GetComponent<Image>();

            // Set the sprite of the new skill to the Image component
            newSkillImage.sprite = newSkill.skillIcon;
            SkillUI skillUI = newSkillPanel.GetChild(0).GetComponent<SkillUI>();
            if (skillUI != null)
            {
                skillUI.Skill = newSkill;
                skillUI.Level = 1; // Eftersom det är ny skill
            }
            else
            {
                Debug.LogWarning("SkillUI saknas på nya skillpanelen!");
            }
        }
    }

    public Skill GetSkillReward()
    {
        int skillsCount = skillDataBase.GetSkillsCount();
        List<SkillInstance> playerSkills = Inventory.Instance.GetSkills();

        var allAvailableSkills = skillDataBase.GetAllSkills(); // Denna bör returnera List<Skill>
        var possibleSkills = allAvailableSkills
            .Where(s =>
            {
                var owned = playerSkills.FirstOrDefault(p => p.skillName == s.skillName);
                if (owned == null) return true;
                if (s.isLevelable && owned.level < 3) return true; // ← OBS: ändrat från .skillLevel till .level
                return false;
            })
            .ToList();

        if (possibleSkills.Count == 0)
        {
            Debug.Log("🎓 Alla skills uppnådda/maxade.");
            return null;
        }

        Skill chosenSkill = possibleSkills[Random.Range(0, possibleSkills.Count)];
        return chosenSkill;
    }

    public void IncreaseStrength()
    {
        if (availablePoints > 0)
        {
            strToAdd++;
            availablePoints--;
            strText.text = (strToAdd + CharacterData.Instance.Strength).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }
    public void IncreaseAgility()
    {
        if (availablePoints > 0)
        {
            agiToAdd++;
            availablePoints--;
            agiText.text = (agiToAdd + CharacterData.Instance.Agility).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }
    public void IncreaseIntellect()
    {
        if (availablePoints > 0)
        {
            intToAdd++;
            availablePoints--;
            intText.text = (intToAdd + CharacterData.Instance.Intellect).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }
    public void IncreaseHealth()
    {
        if (availablePoints > 0)
        {
            healthToAdd++;
            availablePoints--;
            healthText.text = (healthToAdd * 5 + CharacterData.Instance.Health).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }
    public void IncreaseDef()
    {
        if (availablePoints > 0)
        {
            defToAdd++;
            availablePoints--;
            defText.text = (defToAdd + CharacterData.Instance.Defense).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }
    public void IncreasePrec()
    {
        if (availablePoints > 0)
        {
            precToAdd++;
            availablePoints--;
            precText.text = (precToAdd + CharacterData.Instance.HitRate).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }

    public void DecreaseStrength()
    {
        if (strToAdd > 0)
        {
            strToAdd--;
            availablePoints++;
            strText.text = (strToAdd + CharacterData.Instance.Strength).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }
    public void DecreaseAgility()
    {
        if (agiToAdd > 0)
        {
            agiToAdd--;
            availablePoints++;
            agiText.text = (agiToAdd + CharacterData.Instance.Agility).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }
    public void DecreaseIntellect()
    {
        if (intToAdd > 0)
        {
            intToAdd--;
            availablePoints++;
            intText.text = (intToAdd + CharacterData.Instance.Intellect).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }
    public void DecreaseHealth()
    {
        if (healthToAdd > 0)
        {
            healthToAdd--;
            availablePoints++;
            healthText.text = (healthToAdd * 5 + CharacterData.Instance.Health).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }
    public void DecreaseDefense()
    {
        if (defToAdd > 0)
        {
            defToAdd--;
            availablePoints++;
            defText.text = (defToAdd + CharacterData.Instance.Defense).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }
    public void DecreasePrecision()
    {
        if (precToAdd > 0)
        {
            precToAdd--;
            availablePoints++;
            precText.text = (precToAdd + CharacterData.Instance.HitRate).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }

    public void AddStatsAndSwitchScene()
    {
        if (!Inventory.Instance.HasSkill("BeastMaster") && newSkill.skillName == "BeastMaster")
        {
            Inventory.Instance.AddPetToInventory(petDataBase.GetPetByName("Pig"));
        }
        Inventory.Instance.AddSkillToInventory(newSkill);
        CheckSkillStatsUpdate(newSkill.skillName);

        CharacterData.Instance.AddStrAgiInt(strToAdd, agiToAdd, intToAdd, healthToAdd, precToAdd, defToAdd, fortToAdd, 0, 0, 0, 0);


        SceneController.instance.LoadScene("Base");
    }

    public void CheckSkillStatsUpdate(string skillname)
    {
        if (skillname == "Vampyre")
        {
            CharacterData.Instance.LifeSteal += 5;
        }
        else if (skillname == "HungerToFight")
        {
            CharacterData.Instance.initiative += 10;
        }
        else if (skillname == "Momentum")
        {
            CharacterData.Instance.combo += 10;
        }
    }


}
