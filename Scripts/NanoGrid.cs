using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NanoGrid : MonoBehaviour
{
    [SerializeField] public string buildingName = "";
    [SerializeField] public string networkName = "";
    [SerializeField] public int index;
    [SerializeField] public float size;
    [SerializeField] public string houseNumber = "";
    [SerializeField] public string street = "";
    [SerializeField] public string suburb = "";
    [SerializeField] public string postCode = "";
    [SerializeField] public string state = "";
    [SerializeField] public string power = "";
    [SerializeField] public string source = "";
    [SerializeField] public float systemGenerationCapacity = 0;
    [SerializeField] public float systemStorageCapacity = 0;
    [SerializeField] public float minStorageCapacity = 0.5f;
    [SerializeField] public float intervalStorageCapacity = 0.5f;
    [SerializeField] public float maxChargeRate = 5;

    private float time = 0f;
    private Color networkColour = Color.blue;
    private new Renderer renderer;
    private bool genesis = false;
    private float delay = 0;
    private float genesisDelayTime = 0f;
    private float[,] load;
    public float annualLoad;
    public float annualSolarCapacity = 0;
    public bool installScheduled = false;
    private float intervalExcessGeneration = 0;
    public DateTime dateStartedGenerating;
    public List<SupplyAgreement> supplyAgreements;


    private void Start()
    {
        renderer = transform.GetComponent<Renderer>();
        renderer.material.color = Color.white;
        if (networkName != "")
        {
            if(supplyAgreements.Count > 0)
            {
                delay = 0;
            } else
            {
                delay = index * 3600 / IoenManager.Instance.GetEnergyNetworkTakeUpRate(networkName);
            }
            networkColour = IoenManager.Instance.GetEnergyNetworkColour(networkName);
        }
        StartCoroutine(Setup());
    }

    IEnumerator Setup()
    {
        yield return new WaitUntil(() => IoenManager.Instance.IsReady());
        load = new float[366, 48];
        int index = UnityEngine.Random.Range(0, IoenManager.Instance.GetNumberOfIntervalFiles() -1);
        for (int dayOfYear = 1; dayOfYear < 366; dayOfYear++)
        {
            for (int interval = 0; interval < 48; interval++)
            {
                load[dayOfYear, interval] = IoenManager.Instance.load[index, dayOfYear, interval];
                annualLoad += IoenManager.Instance.load[index, dayOfYear, interval];
            }
        }
        if (source == "solar")
        {
            if (systemGenerationCapacity == 0) systemGenerationCapacity = IoenManager.Instance.GetEnergyNetworkAverageSolarSize(networkName);
            if (systemStorageCapacity == 0) systemGenerationCapacity = IoenManager.Instance.GetEnergyNetworkAverageStorageSize(networkName);
            if (annualSolarCapacity == 0) annualSolarCapacity = IoenManager.Instance.GetAverageSolarSystemSize() * IoenManager.Instance.GetAnnualGenerationFactor();
        }
    }
        
    private void Update()
    {
        if (!IoenManager.Instance.IsReady()) return;

        if (networkName != "")  
        {
            genesisDelayTime += Time.deltaTime * GameManager.Instance.GetTimeSpeedFactor();
            if (genesisDelayTime > delay && genesis == false)
            {
                genesis = true;
                renderer.material.color = Color.Lerp(Color.white, networkColour, 1f);
                genesisDelayTime = 0;
                IoenManager.Instance.AddNanoGridToEnergyNetwork(networkName, this);
            }
        }
        time += Time.deltaTime * GameManager.Instance.GetTimeSpeedFactor();
        if (time > GameManager.Instance.GetTimeInterval())
        {
            float load = GetIntervalLoad();
            float generation = GetIntervalGeneration();
            if (generation > 0) IoenManager.Instance.GeneratePower(generation);
            if (generation - load > 0)
            {
                intervalExcessGeneration = ChargeBattery(generation - load);
                IoenManager.Instance.StorePower(generation - load - intervalExcessGeneration);
            }
            else
            {
                load = DischargeBattery(load - generation);
                if (load > 0 && genesis)
                {
                    // Execute supply agreements

                    if (load > 0)
                    {
                        // Buy from Retailer
                        IoenManager.Instance.BuyRetailPower(load);
                    }
                } else if (load > 0)
                {
                    // Buy from Retailer
                    IoenManager.Instance.BuyRetailPower(load);
                }
            }
            time = 0;
        }
    }

    public float GetIntervalLoad()
    {
        return load[GameManager.Instance.GetDayOfYear(), GameManager.Instance.GetIntervalOfday()];
    }

    public float GetIntervalGeneration()
    {
        return systemGenerationCapacity * IoenManager.Instance.GetSolarGenerationProfile(GameManager.Instance.GetMonth(), GameManager.Instance.GetHour());
    }

    public float GetIntervalCapacity()
    {
        return intervalExcessGeneration + intervalStorageCapacity;
    }

    private float ChargeBattery(float amount)
    {
        if (intervalStorageCapacity + amount <= systemStorageCapacity)
        {
            intervalStorageCapacity += amount;
            return 0;
        } else
        {
            float excessAmount = amount - (systemStorageCapacity - intervalStorageCapacity);
            intervalStorageCapacity = systemStorageCapacity;
            return excessAmount;
        }
    }

    private float DischargeBattery(float amount)
    {
        if (intervalStorageCapacity + amount <= systemStorageCapacity)
        {
            intervalStorageCapacity += amount;
            return 0;
        }
        else
        {
            float excessAmount = amount - (systemStorageCapacity - intervalStorageCapacity);
            intervalStorageCapacity = systemStorageCapacity;
            return excessAmount;
        }
    }

    public bool GetGenesis()
    {
        return genesis;
    }

    public void InstallSolar()
    {
        Debug.Log("Install Capacity on " + name + " in Energy Network " + networkName);
        installScheduled = false;
        dateStartedGenerating = GameManager.Instance.GetGameTimeNow();
        source = "solar";
        power = "generator";
        annualSolarCapacity = IoenManager.Instance.GetEnergyNetworkAverageSolarSize(networkName) * IoenManager.Instance.GetAnnualGenerationFactor();
        systemGenerationCapacity = IoenManager.Instance.GetEnergyNetworkAverageSolarSize(networkName);
        systemStorageCapacity = IoenManager.Instance.GetEnergyNetworkAverageStorageSize(networkName);
    }
}
