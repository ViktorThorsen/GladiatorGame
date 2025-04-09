using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    [SerializeField] ItemDataBase itemDataBase;
    [SerializeField] PetDataBase petDataBase;
    [SerializeField] SkillDataBase skillDataBase;
    [SerializeField] Transform enemyCharPos;
    [SerializeField] GameObject characterPrefab;
    [SerializeField] Transform parentObj;

    private List<string> selectedGladiatorNames = new List<string>();

    // Property to get the selected monster names
    public List<string> SelectedGladiatorNames
    {
        get { return selectedGladiatorNames; }
        set { selectedGladiatorNames = value; }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (EnemyGladiatorData.Instance != null)
        {
            EnemyGladiatorData.Instance.LoadEnemyGladiatorAndInventory(itemDataBase, petDataBase, skillDataBase, "testChar1.json"); // Load the data
        }
        GameObject characterObject = CharacterManager.InstantiateEnemyGladiator(
                    EnemyGladiatorData.Instance,
                    characterPrefab,
                    parentObj,
                    enemyCharPos,
                    new Vector3(0.7f, 0.7f, 1f) // Set the desired scale
                );
    }

    // Method to select a Gladiator and add its name to the list
    public void SelectGladiator(string name)
    {

        selectedGladiatorNames.Clear();
        // Add the selected Gladiator's name to the list (allow duplicates)
        selectedGladiatorNames.Add(name);
    }

    public void OnLoadButtonClick()
    {
        if (EnemyGladiatorData.Instance != null)
        {
            EnemyGladiatorData.Instance.LoadEnemyGladiatorAndInventory(itemDataBase, petDataBase, skillDataBase, "testChar1.json"); // Load the data
        }
    }

    // Method to clear all selected Gladiators
    public void ClearSelectedGladiators()
    {
        selectedGladiatorNames.Clear();
    }

    public void Cleanup()
    {
        if (Instance == this)
        {
            Instance = null;
            Destroy(gameObject);
        }
    }
}
