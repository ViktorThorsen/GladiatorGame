using System.Collections;
using System.Collections.Generic;
using Completed;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class CreateCharacter : MonoBehaviour
{
    public CharacterData characterData;
    public GameObject characterPrefab;   // The character prefab assigned in the Inspector
    private int strength;
    private int agility;
    private int intellect;
    private int health;
    private int hitChance;
    private int defense;
    private int fortune;

    [SerializeField] private TMP_Text strText;
    [SerializeField] private TMP_Text agiText;
    [SerializeField] private TMP_Text intText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text hitText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text fortuneText;
    [SerializeField] private TMP_Text pointsToSpendText;
    [SerializeField] private TMP_InputField nameInput;
    public SwitchPart switchPart;        // The SwitchPart component assigned in the Inspector
    int pointsToSpend;

    void Start()
    {
        // Initialize the stats
        strength = 0;
        strText.text = strength.ToString();
        agility = 0;
        agiText.text = agility.ToString();
        intellect = 0;
        intText.text = intellect.ToString();
        health = 0;
        int healthString = health + 50;
        healthText.text = healthString.ToString();
        hitChance = 0;
        hitText.text = hitChance.ToString();
        defense = 0;
        defenseText.text = defense.ToString();
        fortune = 0;
        pointsToSpend = 10;
        pointsToSpendText.text = pointsToSpend.ToString();

        // Assign switchPart from the characterPrefab
        if (characterPrefab != null)
        {
            switchPart = characterPrefab.GetComponent<SwitchPart>();

            if (switchPart == null)
            {
                Debug.LogError("SwitchPart component not found on the character prefab.");
            }
            else
            {
                // Set all body parts to "blue" initially
                SetAllBodyPartsToBlue();
            }
        }
        else
        {
            Debug.LogError("CharacterPrefab is not assigned in the Inspector.");
        }
    }

    public void IncreaseStrength()
    {
        if (pointsToSpend > 0)
        {
            pointsToSpend--;
            pointsToSpendText.text = pointsToSpend.ToString();
            strength++;
            strText.text = strength.ToString();
        }

    }

    public void IncreaseAgility()
    {
        if (pointsToSpend > 0)
        {
            pointsToSpend--;
            pointsToSpendText.text = pointsToSpend.ToString();
            agility++;
            agiText.text = agility.ToString();
        }
    }

    public void IncreaseIntellect()
    {
        if (pointsToSpend > 0)
        {
            pointsToSpend--;
            pointsToSpendText.text = pointsToSpend.ToString();
            intellect++;
            intText.text = intellect.ToString();
        }
    }
    public void IncreaseHealth()
    {
        if (pointsToSpend > 0)
        {
            pointsToSpend--;
            pointsToSpendText.text = pointsToSpend.ToString();
            health++;
            int healthString = (health * 5) + 50;
            healthText.text = healthString.ToString();
        }
    }
    public void IncreaseHit()
    {
        if (pointsToSpend > 0)
        {
            pointsToSpend--;
            pointsToSpendText.text = pointsToSpend.ToString();
            hitChance++;
            hitText.text = hitChance.ToString();
        }
    }
    public void IncreaseDefense()
    {
        if (pointsToSpend > 0)
        {
            pointsToSpend--;
            pointsToSpendText.text = pointsToSpend.ToString();
            defense++;
            defenseText.text = defense.ToString();
        }
    }

    public void DecreaseStrength()
    {
        if (strength != 0)
        {
            pointsToSpend++;
            pointsToSpendText.text = pointsToSpend.ToString();
            strength--;
            strText.text = strength.ToString();
        }
    }

    public void DecreaseAgility()
    {
        if (agility != 0)
        {
            pointsToSpend++;
            pointsToSpendText.text = pointsToSpend.ToString();
            agility--;
            agiText.text = agility.ToString();
        }
    }

    public void DecreaseIntellect()
    {
        if (intellect != 0)
        {
            pointsToSpend++;
            pointsToSpendText.text = pointsToSpend.ToString();
            intellect--;
            intText.text = intellect.ToString();
        }
    }
    public void DecreaseHealth()
    {
        if (health != 0)
        {
            pointsToSpend++;
            pointsToSpendText.text = pointsToSpend.ToString();
            health--;
            int healthString = (health * 5) + 50;
            healthText.text = healthString.ToString();
        }
    }
    public void DecreaseHit()
    {
        if (hitChance != 0)
        {
            pointsToSpend++;
            pointsToSpendText.text = pointsToSpend.ToString();
            hitChance--;
            hitText.text = hitChance.ToString();
        }
    }
    public void DecreaseDefense()
    {
        if (defense != 0)
        {
            pointsToSpend++;
            pointsToSpendText.text = pointsToSpend.ToString();
            defense--;
            defenseText.text = defense.ToString();
        }

    }
    public void Create()
    {
        // Additional creation logic if necessary
    }

    public void SaveCharacter()
    {
        if (switchPart == null)
        {
            Debug.LogError("SwitchPart has not been initialized.");
            return;
        }
        characterData.CharName = nameInput.text;
        characterData.AddStrAgiInt(strength, agility, intellect, health, hitChance, defense, fortune, 0, 0, 0, 0);

        // Retrieve the current labels from SwitchPart and save them to the ScriptableObject
        List<string> currentBodyPartLabels = new List<string>();
        foreach (var bodyPart in switchPart.bodyParts)
        {
            if (bodyPart != null && bodyPart.CurrentLabel != null)
            {
                currentBodyPartLabels.Add(bodyPart.CurrentLabel);
            }
        }

        characterData.BodyPartLabels = currentBodyPartLabels.ToArray();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(characterData);
#endif
        Debug.Log("Character data saved successfully.");


    }

    // Method to set all body parts to the "blue" label
    private void SetAllBodyPartsToBlue()
    {
        string blueLabel = "blue";  // The label you want to set (ensure it matches exactly with your labels)

        foreach (var bodyPart in switchPart.bodyParts)
        {
            if (bodyPart != null)
            {
                bodyPart.SwitchParts(new string[] { blueLabel });
            }
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(characterData);
#endif
    }
}
