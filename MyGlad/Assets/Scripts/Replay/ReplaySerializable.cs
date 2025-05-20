using System;
using System.Collections.Generic;

[System.Serializable]
public class MatchEventDTO
{
    public int Turn;
    public CharacterType Actor;
    public string Action; // "attack", "heal", "venom", "crit", "dodge", "weaponBreak"
    public CharacterType Target;
    public int Value; // kan vara 0 vid t.ex. dodge

}
public class ReplayPayload
{
    public CharacterWrapper player;
    public CharacterWrapper enemy;
    public List<MatchEventDTO> actions;
    public string mapName;
    public string winner;
    public string timestamp;
}

public enum CharacterType
{
    Player = 1,
    EnemyGlad = 2,
    EnemyPet1 = 3,
    EnemyPet2 = 4,
    EnemyPet3 = 5,
    Pet1 = 6,
    Pet2 = 7,
    Pet3 = 8,
    None
}