using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System;
using System.Collections.Generic;
public class ReplayEnemyGladData : MonoBehaviour
{
    public static ReplayEnemyGladData Instance { get; private set; }
    [SerializeField] private int id;
    // Private fields
    [SerializeField] private string charName;
    [SerializeField] private int level;
    [SerializeField] private int xp;
    [SerializeField] private int energy;
    [SerializeField] private int health;
    [SerializeField] private int hitRate;
    [SerializeField] private int lifeSteal;
    [SerializeField] private int dodgeRate;
    [SerializeField] private int critRate;
    [SerializeField] private int stunRate;
    [SerializeField] private int fortune;
    [SerializeField] private int intellect;
    [SerializeField] public int precision;
    [SerializeField] public int initiative;


    //Just for Show
    [SerializeField] private int strength;
    [SerializeField] private int agility;
    [SerializeField] private int defense;


    [SerializeField] private string[] bodyPartLabels;

    private bool createdNow = false;

    public bool CreatedNow
    {
        get { return createdNow; }
        set { createdNow = value; }
    }
    public int Id
    {
        get { return id; }
        set { id = value; }
    }

    // Public properties
    public string CharName
    {
        get { return charName; }
        set { charName = value; }
    }

    public int Level
    {
        get { return level; }
        set { level = value; }
    }

    public int Xp
    {
        get { return xp; }
        set { xp = value; }
    }

    public int Energy
    {
        get { return energy; }
        set { energy = value; }
    }

    public int Health
    {
        get { return health; }
        set { health = value; }
    }
    public int Defense
    {
        get { return defense; }
        set { defense = value; }
    }
    public int HitRate
    {
        get { return hitRate; }
        set { hitRate = value; }
    }

    public int LifeSteal
    {
        get { return lifeSteal; }
        set { lifeSteal = value; }
    }

    public int DodgeRate
    {
        get { return dodgeRate; }
        set { dodgeRate = value; }
    }

    public int CritRate
    {
        get { return critRate; }
        set { critRate = value; }
    }

    public int StunRate
    {
        get { return stunRate; }
        set { stunRate = value; }
    }

    public int Fortune
    {
        get { return fortune; }
        set { fortune = value; }
    }

    public int Strength
    {
        get { return strength; }
        set { strength = value; }
    }

    public int Agility
    {
        get { return agility; }
        set { agility = value; }
    }

    public int Intellect
    {
        get { return intellect; }
        set { intellect = value; }
    }

    public string[] BodyPartLabels
    {
        get { return bodyPartLabels; }
        set { bodyPartLabels = value; }
    }



    // AddStrAgiInt method
    public void AddStrAgiInt(int str, int agi, int inte, int health, int hit, int defense, int fortu, int stun, int lifeSt, int ini)
    {
        Health += health * 5;

        Strength += str;
        if (hitRate - str / 2 >= 0)
        {
            hitRate -= str / 2;
        }

        DodgeRate += agi;
        critRate += agi;

        if (hitRate - agi / 2 >= 0)
        {
            hitRate -= agi / 2;
        }
        Agility += agi;

        intellect += inte;

        if (hitRate + hit >= 0)
        {
            HitRate += hit;
        }

        Fortune += fortu;

        StunRate += stun;

        LifeSteal += lifeSt;

        DodgeRate += defense;
        Defense += defense;
        precision += hit;
        initiative += ini;


    }

    public void LoadStatsFromReplayDTO()
    {
        var dto = ReplayManager.Instance.selectedReplay.enemy.character;

        charName = dto.charName;
        level = dto.level;
        xp = dto.xp;
        health = dto.health;
        lifeSteal = dto.lifeSteal;
        dodgeRate = dto.dodgeRate;
        critRate = dto.critRate;
        stunRate = dto.stunRate;
        fortune = dto.fortune;
        hitRate = dto.hitRate;
        defense = dto.defense;
        strength = dto.strength;
        agility = dto.agility;
        intellect = dto.intellect;
        precision = dto.precision;
        initiative = dto.initiative;

        Debug.Log("✅ ReplayCharacterData: Stats loaded from replay DTO.");
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist this instance across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
        BaseStats();
    }

    public void AddEquipStats(int str, int agi, int inte, int health, int hit, int defense, int fortu, int stun, int lifeSt, int ini)
    {
        AddStrAgiInt(str, agi, inte, health, hit, defense, fortu, stun, lifeSt, ini);
    }

    public void RemoveEquipStats(int str, int agi, int inte, int health, int hit, int defense, int fortu, int stun, int lifeSt, int ini)
    {
        // Reverse the stats added by the AddStrAgiInt method
        Health -= health * 5;
        Strength -= str;
        hitRate += str / 2;
        DodgeRate -= agi;
        critRate -= agi;
        hitRate += agi / 2;
        intellect -= inte;
        HitRate -= hit;
        DodgeRate -= defense;
        Defense -= defense;
        Fortune -= fortu;
        StunRate -= stun;
        LifeSteal -= lifeSt;
        precision -= hit;
        initiative -= ini;

    }

    private void BaseStats()
    {
        Level = 1;
        Xp = 0;
        Health = 50;
        Energy = 10;
        Strength = 1;
        dodgeRate = 0;
        critRate = 0;
        stunRate = 0;
        intellect = 0;
        HitRate = 0;
        Fortune = 0;
        StunRate = 0;
        Defense = 0;
        precision = 0;
        initiative = 0;
    }

}