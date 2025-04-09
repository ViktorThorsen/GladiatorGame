using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterStats : MonoBehaviour
{
    [SerializeField] private string monsterName;
    [SerializeField] private int health;
    [SerializeField] private int attackDamage;
    [SerializeField] private int lifeSteal;
    [Tooltip("Dodge Rate as a percentage (e.g., 10 = 10%)")]
    [SerializeField] private int dodgeRate;
    [SerializeField] private int critRate;
    [SerializeField] private int stunRate;

    [SerializeField] private int initiative;
    [SerializeField] private Vector3 scale;

    [SerializeField] private int xpReward;
    [SerializeField] private int itemreward;



    // Public properties
    public string MonsterName
    {
        get { return monsterName; }
        set { monsterName = value; }
    }
    public int XpReward
    {
        get { return xpReward; }
        set { xpReward = value; }
    }
    public int Health
    {
        get { return health; }
        set { health = value; }
    }
    public int AttackDamage
    {
        get { return attackDamage; }
        set { attackDamage = value; }
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
    public int Initiative
    {
        get { return initiative; }
        set { initiative = value; }
    }
    public Vector3 Scale
    {
        get { return scale; }
        set { scale = value; }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
