using UnityEngine;

[System.Serializable]
public class PetDropRates
{
    public GameObject pet;
    [Range(0f, 100f)]
    public float dropRate;
}