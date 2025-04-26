using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "BackgroundImageDataBase", menuName = "Background/BackgroundImageDataBase", order = 1)]
public class BackgroundImageDataBase : ScriptableObject
{
    [SerializeField] private BattleBackground[] BattleBackgrounds;



    public int GetBattleBackgroundsCount()
    {
        return BattleBackgrounds.Length; // Use Length instead of Count for arrays
    }

    public BattleBackground GetBattleBackground(int index)
    {
        return BattleBackgrounds[index];
    }
    public BattleBackground GetBattleBackgroundByName(string BattleBackgroundName)
    {
        foreach (var BattleBackground in BattleBackgrounds)
        {
            if (BattleBackground.backgroundName == BattleBackgroundName)
            {
                return BattleBackground;
            }
        }
        Debug.LogWarning("Item not found: " + BattleBackgroundName);
        return null;
    }

}

