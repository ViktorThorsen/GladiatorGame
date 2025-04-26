using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterHuntManager : MonoBehaviour
{
    public static MonsterHuntManager Instance { get; private set; }

    // Use a List instead of an array to store multiple monster names
    private List<string> selectedMonsterNames = new List<string>();

    // Property to get the selected monster names
    public List<string> SelectedMonsterNames
    {
        get { return selectedMonsterNames; }
        set { selectedMonsterNames = value; }
    }
    public string sceneState;

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

    // Method to select a monster and add its name to the list
    public void SelectMonster(string name)
    {

        selectedMonsterNames.Clear();
        // Add the selected monster's name to the list (allow duplicates)
        selectedMonsterNames.Add(name);
    }

    public void SetState(string state)
    {
        sceneState = state;
    }

    // Method to clear all selected monsters
    public void ClearSelectedMonsters()
    {
        selectedMonsterNames.Clear();
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
