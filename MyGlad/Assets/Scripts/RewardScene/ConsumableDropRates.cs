using UnityEngine;

[System.Serializable]
public class ConsumableDropRates
{
    public Item consumable;
    [Range(0f, 100f)]
    public float dropRate;
}