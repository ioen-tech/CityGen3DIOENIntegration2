using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class IoenManager : MonoBehaviour
{
    public static IoenManager Instance { get; private set; }
    [Range(5, 30)]
    [SerializeField] private int intervalFiles = 5;
    [Range(1, 32)]
    [SerializeField] private float averageSolarSystemSize = 6.6f;
    [Range(1, 32)]
    [SerializeField] private float averageStorageSystemSize = 6.6f;
    [Range(1, 90)]
    [SerializeField] private int growthTimeDays = 7;
    [Range(1, 100)]
    [SerializeField] private int solarInstallDays = 5;
    [Range(1, 100)]
    [SerializeField] private int solarInstallCapacity = 1; // Number of systems install in parallel
    [Range(0, 100)]
    [SerializeField] private int greenEnergyMix = 36;
    private float[,] solargenerationProfile;
    private float annualGenerationFactor;
    private EnergyNetworkList energyNetworks;
    public float[,,] load;
    [SerializeField] private string rootPath = "/Users/philipbeadle/IOEN/AEMO/IntervalData";
    private bool ready = false;
    private Queue<NanoGrid> nanogridsForSolarInstall = new Queue<NanoGrid>();
    private float timeToNextInstall = 0;
    private Transform[] allTransforms;
    private NanoGrid[] nanoGrids;

    private Dictionary<PowerType, float> intervalAmountDictionary = new Dictionary<PowerType, float>();
    private Dictionary<PowerType, float> totalAmountDictionary = new Dictionary<PowerType, float>();
    private PowerTypeList powerTypeList;
    private PowerType ioen;
    private PowerType fossilFuel;
    private PowerType greenPower;
    private PowerType rooftopPV;
    private PowerType battery;
    public event EventHandler OnPowerAmountChanged;

    private async void Awake()
    {
        Instance = this;
        SetupGenerationProfileData();
        energyNetworks = Resources.Load<EnergyNetworkList>(typeof(EnergyNetworkList).Name);
        foreach(EnergyNetwork energyNetwork in energyNetworks.list)
        {
            energyNetwork.nanoGrids = new List<NanoGrid>();
        }

        powerTypeList = Resources.Load<PowerTypeList>(typeof(PowerTypeList).Name);
        foreach (PowerType powerType in powerTypeList.list)
        {
            intervalAmountDictionary[powerType] = 0;
            totalAmountDictionary[powerType] = 0;
        }
        ioen = powerTypeList.list.Find(powerType => powerType.nameShort == "IOEN");
        fossilFuel = powerTypeList.list.Find(powerType => powerType.nameShort == "Fossil");
        greenPower = powerTypeList.list.Find(powerType => powerType.nameShort == "GreenPower");
        rooftopPV = powerTypeList.list.Find(powerType => powerType.nameShort == "RooftopPV");
        battery = powerTypeList.list.Find(powerType => powerType.nameShort == "Battery");
        DirectoryInfo dir = new DirectoryInfo(Path.Combine(rootPath));
        FileInfo[] fis = dir.GetFiles();
        load = new float[10, 366, 48];
        int i = 0;
        foreach (FileInfo fi in fis)
        {
            if (fi.Extension.Contains("csv"))
            {
                if (i >= intervalFiles) break;
                Debug.Log("Processing " + (int)(i + 1) + " of " + intervalFiles + " interval files");
                using (var sr = new StreamReader(Path.Combine(rootPath, fi.Name)))
                {
                    await sr.ReadLineAsync(); // skip first line
                    int interval = 0;
                    while (true)
                    {
                        if (interval == 48) interval = 0;
                        var line = await sr.ReadLineAsync();
                        if (line == null) break;
                        var values = line.Split(",");
                        var dayOfYear = int.Parse(values[0]);
                        var generalSupplyKwh = float.Parse(values[2]);
                        load[i, dayOfYear, interval] = generalSupplyKwh;
                        interval++;
                    }
                }
                i++;
            }
        }
        ready = true;
        for (int month = 1; month < 13; month++)
        {
            for (int hourOfDay = 0; hourOfDay < 24; hourOfDay++)
            {
                annualGenerationFactor += solargenerationProfile[month, hourOfDay] * DateTime.DaysInMonth(DateTime.Now.Year, month) * 0.8f;
            }
        }
        Debug.Log("IOEN Manager Awake " + IsReady());
    }

    private void Start()
    {
        allTransforms = FindObjectsOfType<Transform>();
        nanoGrids = FindObjectsOfType<NanoGrid>();
    }

    private void Update()
    {
        timeToNextInstall += Time.deltaTime * GameManager.Instance.GetTimeSpeedFactor();
        if (timeToNextInstall > solarInstallDays * 24 * 3600)
        {
            timeToNextInstall = 0;
            for (int i = 0; i < solarInstallCapacity ; i++)
            {
                if (nanogridsForSolarInstall.Count > 0)
                {
                    NanoGrid nanogridToInstallSolar = nanogridsForSolarInstall.Dequeue();
                    nanogridToInstallSolar.InstallSolar();
                    Debug.Log("Install " + nanogridToInstallSolar.buildingName);
                }
            }
        }
    }

    public bool IsReady()
    {
        return ready;
    }

    public void BuyRetailPower(float amount)
    {
        intervalAmountDictionary[fossilFuel] += amount * (1 - GetGreenEnergyMix() / 100);
        totalAmountDictionary[fossilFuel] += amount * (1 - GetGreenEnergyMix() / 100);
        intervalAmountDictionary[greenPower] += amount * GetGreenEnergyMix() / 100;
        totalAmountDictionary[greenPower] += amount * GetGreenEnergyMix() / 100;
        OnPowerAmountChanged?.Invoke(this, EventArgs.Empty);
    }

    public void GeneratePower(float amount)
    {
        intervalAmountDictionary[rooftopPV] += amount;
        totalAmountDictionary[rooftopPV] += amount;
        OnPowerAmountChanged?.Invoke(this, EventArgs.Empty);
    }

    public void StorePower(float amount)
    {
        intervalAmountDictionary[battery] += amount;
        totalAmountDictionary[battery] += amount;
        OnPowerAmountChanged?.Invoke(this, EventArgs.Empty);
    }

    public float GetIntervalAmount(PowerType powerType)
    {
        return intervalAmountDictionary[powerType];
    }

    public float GetTotalAmount(PowerType powerType)
    {
        return totalAmountDictionary[powerType];
    }

    public int GetNumberOfIntervalFiles()
    {
        return intervalFiles;
    }

    public EnergyNetworkList GetEnergyNetworks()
    {
        return energyNetworks;
    }

    public EnergyNetwork GetEnergyNetwork(string networkName)
    {
        EnergyNetwork energyNetwork = energyNetworks.list.Find(energyNetwork => energyNetwork.name == networkName);
        return energyNetwork;
    }

    public void AddNanoGridToEnergyNetwork(string networkName, NanoGrid nanoGrid)
    {
        EnergyNetwork energyNetwork = energyNetworks.list.Find(energyNetwork => energyNetwork.name == networkName);
        if (energyNetwork == null)
        {
            Debug.Log(networkName + " " + nanoGrid.buildingName);
        }
        float annualLoad = 0;
        float scheduledSolarCapacity = 0;
        float annualSolarCapacity = 0;
        foreach (NanoGrid ng in energyNetwork.nanoGrids)
        {
            annualLoad += ng.annualLoad;
            if (ng.source == "solar")
            {
                annualSolarCapacity += energyNetwork.averageSolarSize * annualGenerationFactor;
            }
            if (ng.installScheduled)
            {
                scheduledSolarCapacity += energyNetwork.averageSolarSize * annualGenerationFactor;
            }
        }
        if (annualSolarCapacity + scheduledSolarCapacity < annualLoad + nanoGrid.annualLoad)
        {
            Debug.Log("Schedule: " + nanoGrid.buildingName);
            nanoGrid.installScheduled = true;
            nanogridsForSolarInstall.Enqueue(nanoGrid);
        }
        if (nanoGrid.supplyAgreements.Count == 0)
        {
            Debug.Log("Supply Agreements: " + nanoGrid.buildingName);

            List<NanoGrid> suppliers = energyNetwork.nanoGrids.FindAll(ng => ng.source == "solar" || ng.installScheduled == true);
            suppliers.Sort((x, y) => DateTime.Compare(x.dateStartedGenerating, y.dateStartedGenerating));
            int supplyAgreementIndex = 0;
            foreach(NanoGrid supplier in suppliers)
            {
                if (supplyAgreementIndex >= energyNetwork.numberOfSupplyAgreements) break;
                Debug.Log(supplier);
                string buildingAddress = "";
                if (nanoGrid.houseNumber != "") buildingAddress = nanoGrid.houseNumber + " ";
                if (nanoGrid.street != "") buildingAddress += nanoGrid.street + " ";
                if (nanoGrid.state != "") buildingAddress += nanoGrid.state + " ";
                if (nanoGrid.postCode != "") buildingAddress += nanoGrid.postCode; ;
                string id = nanoGrid.buildingName;
                if (buildingAddress != "") id = buildingAddress;
                Transform nanoGridTransform = Array.Find(allTransforms, ele => ele.name == "NanoGrid - " + id);
                GameObject supplyAgreementObject = Instantiate(Resources.Load("Prefabs/pfSupplyAgreement", typeof(GameObject))) as GameObject;
                supplyAgreementObject.transform.parent = nanoGridTransform.transform;
                supplyAgreementObject.name = "SupplyAgreement - " + supplier.buildingName;
                supplyAgreementObject.SetActive(true);
                SupplyAgreement supplyAgreement = supplyAgreementObject.GetComponent<SupplyAgreement>();
                supplyAgreement.SetConsumerNanoGrid(nanoGrid);
                supplyAgreement.SetSupplierNanoGrid(supplier);
                supplyAgreement.SetTariffIoenFuel(energyNetwork.tariffIoenFuel);
                supplyAgreement.SetCreditLimit(energyNetwork.creditLimit);
                supplyAgreement.SetTransactionEnergyLimit(energyNetwork.transactionEnergyLimit);
                nanoGrid.supplyAgreements.Add(supplyAgreement);
                supplyAgreementIndex++;
            }
        }
        energyNetwork.nanoGrids.Add(nanoGrid);
    }

    public float GetEnergyNetworkTakeUpRate(string networkName)
    {
        EnergyNetwork energyNetwork = energyNetworks.list.Find(energyNetwork => energyNetwork.name == networkName);
        if (energyNetwork) return energyNetwork.takeUpRate / 100;
        return 0.9f;
    }

    public float GetGrowthTime()
    {
        return growthTimeDays * 24;
    }

    public int GetGreenEnergyMix()
    {
        return greenEnergyMix;
    }

    public Color GetEnergyNetworkColour(string networkName)
    {
        EnergyNetwork energyNetwork = energyNetworks.list.Find(energyNetwork => energyNetwork.name == networkName);
        if (energyNetwork) return energyNetwork.colour;
        return Color.black;
    }

    public float GetEnergyNetworkAverageSolarSize(string networkName)
    {
        EnergyNetwork energyNetwork = energyNetworks.list.Find(energyNetwork => energyNetwork.name == networkName);
        if (energyNetwork) return energyNetwork.averageSolarSize;
        return averageSolarSystemSize;
    }

    public float GetEnergyNetworkAverageStorageSize(string networkName)
    {
        EnergyNetwork energyNetwork = energyNetworks.list.Find(energyNetwork => energyNetwork.name == networkName);
        if (energyNetwork) return energyNetwork.averageStorageSize;
        return averageStorageSystemSize;
    }

    public float GetAnnualGenerationFactor()
    {
        return annualGenerationFactor;
    }

    public float GetAverageSolarSystemSize()
    {
        return averageSolarSystemSize;
    }

    public float GetSolarGenerationProfile(int month, int hour)
    {
        return solargenerationProfile[month, hour];
    }

    private void SetupGenerationProfileData()
    {
        solargenerationProfile = new float[13, 24];
        solargenerationProfile[1, 0] = 0f;
        solargenerationProfile[1, 1] = 0f;
        solargenerationProfile[1, 2] = 0f;
        solargenerationProfile[1, 3] = 0f;
        solargenerationProfile[1, 4] = 0f;
        solargenerationProfile[1, 5] = 0f;
        solargenerationProfile[1, 6] = 0.093f;
        solargenerationProfile[1, 7] = 0.200f;
        solargenerationProfile[1, 8] = 0.360f;
        solargenerationProfile[1, 9] = 0.547f;
        solargenerationProfile[1, 10] = 0.732f;
        solargenerationProfile[1, 11] = 0.875f;
        solargenerationProfile[1, 12] = 0.951f;
        solargenerationProfile[1, 13] = 0.970f;
        solargenerationProfile[1, 14] = 0.933f;
        solargenerationProfile[1, 15] = 0.819f;
        solargenerationProfile[1, 16] = 0.655f;
        solargenerationProfile[1, 17] = 0.455f;
        solargenerationProfile[1, 18] = 0.260f;
        solargenerationProfile[1, 19] = 0.120f;
        solargenerationProfile[1, 20] = 0f;
        solargenerationProfile[1, 21] = 0f;
        solargenerationProfile[1, 22] = 0f;
        solargenerationProfile[1, 23] = 0f;

        solargenerationProfile[2, 0] = 0f;
        solargenerationProfile[2, 1] = 0f;
        solargenerationProfile[2, 2] = 0f;
        solargenerationProfile[2, 3] = 0f;
        solargenerationProfile[2, 4] = 0f;
        solargenerationProfile[2, 5] = 0f;
        solargenerationProfile[2, 6] = 0.06f;
        solargenerationProfile[2, 7] = 0.159f;
        solargenerationProfile[2, 8] = 0.322f;
        solargenerationProfile[2, 9] = 0.519f;
        solargenerationProfile[2, 10] = 0.706f;
        solargenerationProfile[2, 11] = 0.849f;
        solargenerationProfile[2, 12] = 0.935f;
        solargenerationProfile[2, 13] = 0.966f;
        solargenerationProfile[2, 14] = 0.923f;
        solargenerationProfile[2, 15] = 0.806f;
        solargenerationProfile[2, 16] = 0.637f;
        solargenerationProfile[2, 17] = 0.437f;
        solargenerationProfile[2, 18] = 0.234f;
        solargenerationProfile[2, 19] = 0.096f;
        solargenerationProfile[2, 20] = 0f;
        solargenerationProfile[2, 21] = 0f;
        solargenerationProfile[2, 22] = 0f;
        solargenerationProfile[2, 23] = 0f;

        solargenerationProfile[3, 0] = 0f;
        solargenerationProfile[3, 1] = 0f;
        solargenerationProfile[3, 2] = 0f;
        solargenerationProfile[3, 3] = 0f;
        solargenerationProfile[3, 4] = 0f;
        solargenerationProfile[3, 5] = 0f;
        solargenerationProfile[3, 6] = 0.04f;
        solargenerationProfile[3, 7] = 0.112f;
        solargenerationProfile[3, 8] = 0.267f;
        solargenerationProfile[3, 9] = 0.463f;
        solargenerationProfile[3, 10] = 0.63f;
        solargenerationProfile[3, 11] = 0.773f;
        solargenerationProfile[3, 12] = 0.84f;
        solargenerationProfile[3, 13] = 0.851f;
        solargenerationProfile[3, 14] = 0.794f;
        solargenerationProfile[3, 15] = 0.685f;
        solargenerationProfile[3, 16] = 0.513f;
        solargenerationProfile[3, 17] = 0.329f;
        solargenerationProfile[3, 18] = 0.151f;
        solargenerationProfile[3, 19] = 0.053f;
        solargenerationProfile[3, 20] = 0f;
        solargenerationProfile[3, 21] = 0f;
        solargenerationProfile[3, 22] = 0f;
        solargenerationProfile[3, 23] = 0f;

        solargenerationProfile[4, 0] = 0f;
        solargenerationProfile[4, 1] = 0f;
        solargenerationProfile[4, 2] = 0f;
        solargenerationProfile[4, 3] = 0f;
        solargenerationProfile[4, 4] = 0f;
        solargenerationProfile[4, 5] = 0f;
        solargenerationProfile[4, 6] = 0f;
        solargenerationProfile[4, 7] = 0.113f;
        solargenerationProfile[4, 8] = 0.278f;
        solargenerationProfile[4, 9] = 0.453f;
        solargenerationProfile[4, 10] = 0.595f;
        solargenerationProfile[4, 11] = 0.677f;
        solargenerationProfile[4, 12] = 0.679f;
        solargenerationProfile[4, 13] = 0.646f;
        solargenerationProfile[4, 14] = 0.559f;
        solargenerationProfile[4, 15] = 0.417f;
        solargenerationProfile[4, 16] = 0.253f;
        solargenerationProfile[4, 17] = 0.102f;
        solargenerationProfile[4, 18] = 0.037f;
        solargenerationProfile[4, 19] = 0f;
        solargenerationProfile[4, 20] = 0f;
        solargenerationProfile[4, 21] = 0f;
        solargenerationProfile[4, 22] = 0f;
        solargenerationProfile[4, 23] = 0f;

        solargenerationProfile[5, 0] = 0f;
        solargenerationProfile[5, 1] = 0f;
        solargenerationProfile[5, 2] = 0f;
        solargenerationProfile[5, 3] = 0f;
        solargenerationProfile[5, 4] = 0f;
        solargenerationProfile[5, 5] = 0f;
        solargenerationProfile[5, 6] = 0f;
        solargenerationProfile[5, 7] = 0.068f;
        solargenerationProfile[5, 8] = 0.214f;
        solargenerationProfile[5, 9] = 0.377f;
        solargenerationProfile[5, 10] = 0.518f;
        solargenerationProfile[5, 11] = 0.596f;
        solargenerationProfile[5, 12] = 0.602f;
        solargenerationProfile[5, 13] = 0.54f;
        solargenerationProfile[5, 14] = 0.441f;
        solargenerationProfile[5, 15] = 0.305f;
        solargenerationProfile[5, 16] = 0.152f;
        solargenerationProfile[5, 17] = 0.042f;
        solargenerationProfile[5, 18] = 0f;
        solargenerationProfile[5, 19] = 0f;
        solargenerationProfile[5, 20] = 0f;
        solargenerationProfile[5, 21] = 0f;
        solargenerationProfile[5, 22] = 0f;
        solargenerationProfile[5, 23] = 0f;

        solargenerationProfile[6, 0] = 0f;
        solargenerationProfile[6, 1] = 0f;
        solargenerationProfile[6, 2] = 0f;
        solargenerationProfile[6, 3] = 0f;
        solargenerationProfile[6, 4] = 0f;
        solargenerationProfile[6, 5] = 0f;
        solargenerationProfile[6, 6] = 0f;
        solargenerationProfile[6, 7] = 0.039f;
        solargenerationProfile[6, 8] = 0.151f;
        solargenerationProfile[6, 9] = 0.284f;
        solargenerationProfile[6, 10] = 0.415f;
        solargenerationProfile[6, 11] = 0.505f;
        solargenerationProfile[6, 12] = 0.506f;
        solargenerationProfile[6, 13] = 0.455f;
        solargenerationProfile[6, 14] = 0.37f;
        solargenerationProfile[6, 15] = 0.249f;
        solargenerationProfile[6, 16] = 0.116f;
        solargenerationProfile[6, 17] = 0.035f;
        solargenerationProfile[6, 18] = 0f;
        solargenerationProfile[6, 19] = 0f;
        solargenerationProfile[6, 20] = 0f;
        solargenerationProfile[6, 21] = 0f;
        solargenerationProfile[6, 22] = 0f;
        solargenerationProfile[6, 23] = 0f;

        solargenerationProfile[7, 0] = 0f;
        solargenerationProfile[7, 1] = 0f;
        solargenerationProfile[7, 2] = 0f;
        solargenerationProfile[7, 3] = 0f;
        solargenerationProfile[7, 4] = 0f;
        solargenerationProfile[7, 5] = 0f;
        solargenerationProfile[7, 6] = 0f;
        solargenerationProfile[7, 7] = 0.043f;
        solargenerationProfile[7, 8] = 0.168f;
        solargenerationProfile[7, 9] = 0.331f;
        solargenerationProfile[7, 10] = 0.475f;
        solargenerationProfile[7, 11] = 0.559f;
        solargenerationProfile[7, 12] = 0.57f;
        solargenerationProfile[7, 13] = 0.518f;
        solargenerationProfile[7, 14] = 0.426f;
        solargenerationProfile[7, 15] = 0.289f;
        solargenerationProfile[7, 16] = 0.148f;
        solargenerationProfile[7, 17] = 0.044f;
        solargenerationProfile[7, 18] = 0f;
        solargenerationProfile[7, 19] = 0f;
        solargenerationProfile[7, 20] = 0f;
        solargenerationProfile[7, 21] = 0f;
        solargenerationProfile[7, 22] = 0f;
        solargenerationProfile[7, 23] = 0f;

        solargenerationProfile[8, 0] = 0f;
        solargenerationProfile[8, 1] = 0f;
        solargenerationProfile[8, 2] = 0f;
        solargenerationProfile[8, 3] = 0f;
        solargenerationProfile[8, 4] = 0f;
        solargenerationProfile[8, 5] = 0f;
        solargenerationProfile[8, 6] = 0.029f;
        solargenerationProfile[8, 7] = 0.085f;
        solargenerationProfile[8, 8] = 0.242f;
        solargenerationProfile[8, 9] = 0.421f;
        solargenerationProfile[8, 10] = 0.58f;
        solargenerationProfile[8, 11] = 0.654f;
        solargenerationProfile[8, 12] = 0.645f;
        solargenerationProfile[8, 13] = 0.601f;
        solargenerationProfile[8, 14] = 0.513f;
        solargenerationProfile[8, 15] = 0.371f;
        solargenerationProfile[8, 16] = 0.213f;
        solargenerationProfile[8, 17] = 0.075f;
        solargenerationProfile[8, 18] = 0f;
        solargenerationProfile[8, 19] = 0f;
        solargenerationProfile[8, 20] = 0f;
        solargenerationProfile[8, 21] = 0f;
        solargenerationProfile[8, 22] = 0f;
        solargenerationProfile[8, 23] = 0f;

        solargenerationProfile[9, 0] = 0f;
        solargenerationProfile[9, 1] = 0f;
        solargenerationProfile[9, 2] = 0f;
        solargenerationProfile[9, 3] = 0f;
        solargenerationProfile[9, 4] = 0f;
        solargenerationProfile[9, 5] = 0f;
        solargenerationProfile[9, 6] = 0.06f;
        solargenerationProfile[9, 7] = 0.191f;
        solargenerationProfile[9, 8] = 0.383f;
        solargenerationProfile[9, 9] = 0.567f;
        solargenerationProfile[9, 10] = 0.709f;
        solargenerationProfile[9, 11] = 0.776f;
        solargenerationProfile[9, 12] = 0.781f;
        solargenerationProfile[9, 13] = 0.73f;
        solargenerationProfile[9, 14] = 0.619f;
        solargenerationProfile[9, 15] = 0.46f;
        solargenerationProfile[9, 16] = 0.282f;
        solargenerationProfile[9, 17] = 0.126f;
        solargenerationProfile[9, 18] = 0.037f;
        solargenerationProfile[9, 19] = 0f;
        solargenerationProfile[9, 20] = 0f;
        solargenerationProfile[9, 21] = 0f;
        solargenerationProfile[9, 22] = 0f;
        solargenerationProfile[9, 23] = 0f;

        solargenerationProfile[10, 0] = 0f;
        solargenerationProfile[10, 1] = 0f;
        solargenerationProfile[10, 2] = 0f;
        solargenerationProfile[10, 3] = 0f;
        solargenerationProfile[10, 4] = 0f;
        solargenerationProfile[10, 5] = 0f;
        solargenerationProfile[10, 6] = 0.079f;
        solargenerationProfile[10, 7] = 0.204f;
        solargenerationProfile[10, 8] = 0.371f;
        solargenerationProfile[10, 9] = 0.551f;
        solargenerationProfile[10, 10] = 0.701f;
        solargenerationProfile[10, 11] = 0.818f;
        solargenerationProfile[10, 12] = 0.887f;
        solargenerationProfile[10, 13] = 0.899f;
        solargenerationProfile[10, 14] = 0.812f;
        solargenerationProfile[10, 15] = 0.675f;
        solargenerationProfile[10, 16] = 0.498f;
        solargenerationProfile[10, 17] = 0.296f;
        solargenerationProfile[10, 18] = 0.134f;
        solargenerationProfile[10, 19] = 0.05f;
        solargenerationProfile[10, 20] = 0f;
        solargenerationProfile[10, 21] = 0f;
        solargenerationProfile[10, 22] = 0f;
        solargenerationProfile[10, 23] = 0f;

        solargenerationProfile[11, 0] = 0f;
        solargenerationProfile[11, 1] = 0f;
        solargenerationProfile[11, 2] = 0f;
        solargenerationProfile[11, 3] = 0f;
        solargenerationProfile[11, 4] = 0f;
        solargenerationProfile[11, 5] = 0.045f;
        solargenerationProfile[11, 6] = 0.117f;
        solargenerationProfile[11, 7] = 0.239f;
        solargenerationProfile[11, 8] = 0.413f;
        solargenerationProfile[11, 9] = 0.584f;
        solargenerationProfile[11, 10] = 0.738f;
        solargenerationProfile[11, 11] = 0.846f;
        solargenerationProfile[11, 12] = 0.901f;
        solargenerationProfile[11, 13] = 0.891f;
        solargenerationProfile[11, 14] = 0.831f;
        solargenerationProfile[11, 15] = 0.714f;
        solargenerationProfile[11, 16] = 0.546f;
        solargenerationProfile[11, 17] = 0.343f;
        solargenerationProfile[11, 18] = 0.179f;
        solargenerationProfile[11, 19] = 0.075f;
        solargenerationProfile[11, 20] = 0.031f;
        solargenerationProfile[11, 21] = 0f;
        solargenerationProfile[11, 22] = 0f;
        solargenerationProfile[11, 23] = 0f;

        solargenerationProfile[12, 0] = 0f;
        solargenerationProfile[12, 1] = 0f;
        solargenerationProfile[12, 2] = 0f;
        solargenerationProfile[12, 3] = 0f;
        solargenerationProfile[12, 4] = 0f;
        solargenerationProfile[12, 5] = 0.049f;
        solargenerationProfile[12, 6] = 0.125f;
        solargenerationProfile[12, 7] = 0.248f;
        solargenerationProfile[12, 8] = 0.422f;
        solargenerationProfile[12, 9] = 0.618f;
        solargenerationProfile[12, 10] = 0.804f;
        solargenerationProfile[12, 11] = 0.929f;
        solargenerationProfile[12, 12] = 0.998f;
        solargenerationProfile[12, 13] = 1f;
        solargenerationProfile[12, 14] = 0.936f;
        solargenerationProfile[12, 15] = 0.825f;
        solargenerationProfile[12, 16] = 0.645f;
        solargenerationProfile[12, 17] = 0.438f;
        solargenerationProfile[12, 18] = 0.245f;
        solargenerationProfile[12, 19] = 0.113f;
        solargenerationProfile[12, 20] = 0.046f;
        solargenerationProfile[12, 21] = 0f;
        solargenerationProfile[12, 22] = 0f;
        solargenerationProfile[12, 23] = 0f;
    }
}
