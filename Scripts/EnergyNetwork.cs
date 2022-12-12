using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/EnergyNetwork")]
public class EnergyNetwork : ScriptableObject
    {
    public string id;
    public new string name;
    public Color colour;
    public Sprite sprite;
    public float takeUpRate;
    public float averageSolarSize;
    public float averageStorageSize;
    public int numberOfSupplyAgreements;
    public float tariffIoenFuel;
    public float transactionEnergyLimit;
    public float creditLimit;
    public List<NanoGrid> nanoGrids;
}