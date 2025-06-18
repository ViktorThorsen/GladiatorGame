using UnityEngine;

[System.Serializable]
public class WeaponDropRates
{
    public Item weapon;
    [Range(0f, 100f)]
    public float dropRate; // procent

}