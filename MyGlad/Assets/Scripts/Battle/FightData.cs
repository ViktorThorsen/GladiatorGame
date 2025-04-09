using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightData : MonoBehaviour
{
    public static FightData Instance { get; private set; }

    // Lista som håller alla strider
    private List<bool> fightHistoryWinOrLoss = new List<bool>();
    private List<string[]> fightHistoryNames = new List<string[]>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Gör så att objektet inte förstörs mellan scener
        }
        else
        {
            Destroy(gameObject); // Säkerställ att endast en instans existerar
        }
    }
    public void AddFightResultWinOrLoss(bool win)
    {
        fightHistoryWinOrLoss.Add(win);
    }
    public void AddFightResultNames(string[] names)
    {
        fightHistoryNames.Add(names);
    }

    // Metod för att hämta alla stridsresultat
    public bool GetFightResultWinOrLoss(int index)
    {
        return fightHistoryWinOrLoss[index];
    }
    public string[] GetFightResultNames(int index)
    {
        return fightHistoryNames[index];
    }
    public string[] GetLastFightResultNames()
    {
        if (fightHistoryNames.Count > 0)
        {
            return fightHistoryNames[fightHistoryNames.Count - 1]; // Hämta den senaste posten
        }
        else
        {
            return null; // Om det inte finns några strider, returnera null
        }
    }

    // Hämta vinst/förlust för den senaste striden
    public bool GetLastFightResultWinOrLoss()
    {
        if (fightHistoryWinOrLoss.Count > 0)
        {
            return fightHistoryWinOrLoss[fightHistoryWinOrLoss.Count - 1]; // Hämta senaste vinst/förlust
        }
        else
        {
            return false; // Returnera false om det inte finns några strider (default)
        }
    }

}
