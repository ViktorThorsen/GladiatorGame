using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseLandsManager : MonoBehaviour
{
    public static ChooseLandsManager Instance { get; private set; }
    public string ChoosedLand;

    void Awake()
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
    public void Forest()
    {
        ChoosedLand = "Forest";
        SceneController.instance.LoadScene("MonsterHuntForest");
    }
    public void Desert()
    {
        ChoosedLand = "Desert";
        SceneController.instance.LoadScene("MonsterHuntDesert");
    }
    public void Swamp()
    {
        ChoosedLand = "Swamp";
        SceneController.instance.LoadScene("MonsterHuntSwamp");
    }
    public void Jungle()
    {
        ChoosedLand = "Jungle";
        SceneController.instance.LoadScene("MonsterHuntJungle");
    }
    public void Farm()
    {
        ChoosedLand = "Farm";
        SceneController.instance.LoadScene("MonsterHuntFarm");
    }
    public void Frostlands()
    {
        ChoosedLand = "Frostlands";
        SceneController.instance.LoadScene("MonsterHuntFrostlands");
    }
    public void Savannah()
    {
        ChoosedLand = "Savannah";
        SceneController.instance.LoadScene("MonsterHuntSavannah");
    }
}
