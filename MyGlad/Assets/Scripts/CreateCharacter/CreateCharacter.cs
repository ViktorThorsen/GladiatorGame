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
    [SerializeField] private TMP_Text strText;
    [SerializeField] private TMP_Text agiText;
    [SerializeField] private TMP_Text intText;
    [SerializeField] private TMP_InputField nameInput;
    public SwitchPart switchPart;        // The SwitchPart component assigned in the Inspector

    void Start()
    {
        // Initialize the stats
        strength = 1;
        agility = 1;
        intellect = 1;

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
        strength++;
        strText.text = strength.ToString();
    }

    public void IncreaseAgility()
    {
        agility++;
        agiText.text = agility.ToString();
    }

    public void IncreaseIntellect()
    {
        intellect++;
        intText.text = intellect.ToString();
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
        characterData.AddStrAgiInt(strength, agility, intellect);

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
