using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyNetwork
{
    public string id;
    public string name;
    public Color colour;
    public List<NanoGrid> nanoGrids;
    public float annualSolarCapacity;
    public float annualLoad;
    public float capacityToInstall;
}