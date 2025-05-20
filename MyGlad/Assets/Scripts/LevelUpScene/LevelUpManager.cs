using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private TMP_Text fortText;

    [SerializeField] private TMP_Text levelText;
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
        fortText.text = CharacterData.Instance.Fortune.ToString();
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
        }
    }

    public Skill GetSkillReward()
    {
        // Get the total number of skills in the skill database
        int skillsCount = skillDataBase.GetSkillsCount();

        // Get all the player's current skills
        List<Skill> playerSkills = Inventory.Instance.GetSkills();

        // Check if the player already has all the skills
        if (playerSkills.Count >= skillsCount)
        {
            Debug.Log("Got All Skills!");  // The player already has all skills, no new skills can be awarded
            return null;  // Optionally return null or handle this case accordingly
        }

        Skill skill;
        bool skillIsAlreadyInInventory;

        // Keep rerolling until we find a skill the player doesn't have
        do
        {
            // Get a random index from the skill database
            int randomIndex = UnityEngine.Random.Range(0, skillsCount);

            // Retrieve the skill from the database
            skill = skillDataBase.GetSkill(randomIndex);

            // Check if the player already has this skill
            skillIsAlreadyInInventory = playerSkills.Any(s => s.skillName == skill.skillName);

        } while (skillIsAlreadyInInventory);  // Continue looping until a new skill is found

        // Return the new skill
        return skill;
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
            healthToAdd += 5;
            availablePoints--;
            healthText.text = (healthToAdd + CharacterData.Instance.Health).ToString();
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
    public void IncreaseFortune()
    {
        if (availablePoints > 0)
        {
            fortToAdd++;
            availablePoints--;
            fortText.text = (fortToAdd + CharacterData.Instance.Fortune).ToString();
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
            healthToAdd -= 5;
            availablePoints++;
            healthText.text = (healthToAdd + CharacterData.Instance.Health).ToString();
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
    public void DecreaseFortune()
    {
        if (fortToAdd > 0)
        {
            fortToAdd--;
            availablePoints++;
            fortText.text = (fortToAdd + CharacterData.Instance.Fortune).ToString();
            availablePointsText.text = availablePoints.ToString();
        }
    }
    public void AddStatsAndSwitchScene()
    {
        CharacterData.Instance.AddStrAgiInt(strToAdd, agiToAdd, intToAdd, healthToAdd, precToAdd, defToAdd, fortToAdd, 0, 0);

        Inventory.Instance.AddSkillToInventory(newSkill);
        SceneController.instance.LoadScene("Base");
    }
}
