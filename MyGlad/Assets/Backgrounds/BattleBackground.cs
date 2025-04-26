using UnityEngine;

[CreateAssetMenu(fileName = "BattleBackground", menuName = "Backgrounds/BattleBackground", order = 1)]
public class BattleBackground : ScriptableObject
{
    public string backgroundName; // Name of the item
    public Sprite backgroundImage; // Icon for the item (for the UI)
}
