using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private CharacterData characterData;
    [SerializeField] private Canvas settingCanvas;


    void Start()
    {
        settingCanvas.enabled = false;
    }

    public void DeleteCharacter()
    {
        characterData.CharName = "";
        characterData.Health = 0;

        characterData.LifeSteal = 0;
        characterData.DodgeRate = 0;
        characterData.CritRate = 0;

        characterData.Strength = 0;
        characterData.Agility = 0;
        characterData.Intellect = 0;
        SceneController.instance.LoadScene("MainMenu");
    }
    public void OpenSettings()
    {
        settingCanvas.enabled = true;
    }
}
