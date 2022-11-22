using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IoenManager : MonoBehaviour
{
    public static IoenManager Instance { get; private set; }
    [Range(1, 32)]
    [SerializeField] private float averageSolarSystemSize = 6.6f;
    [Range(10, 100)]
    [SerializeField] private float size = 60f;
    [Range(10, 100)]
    [SerializeField] private int takeUpRate = 70;
    [Range(1, 90)]
    [SerializeField] private int growthTimeDays = 7;
    [Range(1, 100)]
    [SerializeField] private int solarInstallDays = 5;
    [Range(1, 100)]
    [SerializeField] private int solarInstallCapacity = 1; // Number of systems install in parallel
    [Range(0, 100)]
    [SerializeField] private int greenEnergyMix = 36;
    private List<EnergyNetwork> energyNetworks;

    private void Awake()
    {
        Instance = this;
        energyNetworks = new List<EnergyNetwork>();
    }

    public float GetDensity()
    {
        return takeUpRate / 100f;
    }

    public float GetGrowthTime()
    {
        return growthTimeDays * 24;
    }

    public void AddEnergyNetwork(EnergyNetwork energyNetwork)
    {
        energyNetworks.Add(energyNetwork);
    }

    public List<EnergyNetwork> GetEnergyNetworks()
    {
        return energyNetworks;
    }
}
