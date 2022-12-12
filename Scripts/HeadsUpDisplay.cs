using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeadsUpDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nanoGridCountText;
    [SerializeField] private TextMeshProUGUI solarInstallationsCountText;
    [SerializeField] private TextMeshProUGUI batteryInstallationsCountText;
    [SerializeField] private TextMeshProUGUI instantLoadText;
    [SerializeField] private TextMeshProUGUI instantSolarPowerText;
    [SerializeField] private TextMeshProUGUI instantBatteryChargeText;
    private List<NanoGrid> nanoGrids = new List<NanoGrid>();
    private float time = 0f;
    private Dictionary<EnergyNetwork, Transform> energyNetworkTransformDictionary;
    Transform ioenTemplate;

    private void Awake()
    {
        energyNetworkTransformDictionary = new Dictionary<EnergyNetwork, Transform>();
        ioenTemplate = transform.Find("networkTemplate");
        ioenTemplate.gameObject.SetActive(false);
    }

    void Start()
    {
        Transform[] allTransforms = FindObjectsOfType<Transform>();
        Transform nanoGridsRoot = Array.Find(allTransforms, ele => ele.name == "NanoGrids");
        Transform[] nanoGridTransforms = Array.FindAll(allTransforms, ele => ele.name.StartsWith("NanoGrid -"));
        foreach (Transform nanoGridTransform in nanoGridTransforms)
        {
            NanoGrid nanoGrid = nanoGridTransform.GetComponent<NanoGrid>();
            if (nanoGrid != null)
            {
                nanoGrids.Add(nanoGrid);
            }
        }
        if (nanoGridsRoot != null)
        {
            nanoGridCountText.SetText("Nano Grids: " + nanoGridsRoot.childCount);
        } else
        {
            nanoGridCountText.SetText("No NanoGrids found please run IOEN Integration");
        }
        int index = 0;
        int horizontalOffset = 42;

        foreach (EnergyNetwork energyNetwork in IoenManager.Instance.GetEnergyNetworks().list)
        {
            Transform ioenTransform = Instantiate(ioenTemplate, transform);
            ioenTransform.gameObject.SetActive(true);
            float offsetAmount = 80f;
            float verticalOffset = -1 * offsetAmount * index - 44;
            ioenTransform.GetComponent<RectTransform>().anchoredPosition = new Vector2(horizontalOffset, verticalOffset);
            ioenTransform.Find("Button").Find("image").GetComponent<Image>().sprite = energyNetwork.sprite;
            ioenTransform.Find("nameText").GetComponent<TextMeshProUGUI>().SetText(energyNetwork.name);
            energyNetworkTransformDictionary[energyNetwork] = ioenTransform;
            index++;
        }
        SetValues();
    }

    private void Update()
    {
        time += Time.deltaTime * GameManager.Instance.GetTimeSpeedFactor();
        if (time > GameManager.Instance.GetTimeInterval())
        {
            SetValues();
            time = 0;
        }
    }

    private void SetValues()
    {
        if (!IoenManager.Instance.IsReady()) return;
        float instantSolar = 0f;
        float instantBattery = 0f;
        float instantLoad = 0f;
        int solarInstallationsCount = 0;
        Dictionary<string, float> energyNetworkLoad = new Dictionary<string, float>();
        Dictionary<string, float> energyNetworkAnnualLoad = new Dictionary<string, float>();
        Dictionary<string, float> energyNetworkGeneration = new Dictionary<string, float>();
        Dictionary<string, float> energyNetworkGenerationCapacity = new Dictionary<string, float>();
        Dictionary<string, float> energyNetworkScheduledCapacity = new Dictionary<string, float>();
        foreach (EnergyNetwork energyNetwork in IoenManager.Instance.GetEnergyNetworks().list)
        {
            energyNetworkLoad[energyNetwork.name] = 0;
            energyNetworkAnnualLoad[energyNetwork.name] = 0;
            energyNetworkGeneration[energyNetwork.name] = 0;
            energyNetworkGenerationCapacity[energyNetwork.name] = 0;
            energyNetworkScheduledCapacity[energyNetwork.name] = 0;
        }

        int batteryInstallations = 0;

        foreach (NanoGrid nanoGrid in nanoGrids)
        {
            if (nanoGrid.systemStorageCapacity > 0)
            {
                batteryInstallations++;
            }
            if (nanoGrid.source == "solar")
            {
                float nanoGridGeneration = IoenManager.Instance.GetEnergyNetworkAverageSolarSize(nanoGrid.networkName) * IoenManager.Instance.GetSolarGenerationProfile(GameManager.Instance.GetMonth(), GameManager.Instance.GetHour());
                float nanoGridGenerationCapacity = IoenManager.Instance.GetEnergyNetworkAverageSolarSize(nanoGrid.networkName) * IoenManager.Instance.GetAnnualGenerationFactor();
                instantSolar += nanoGridGeneration;
                if (nanoGrid.networkName != "" && nanoGrid.GetGenesis())
                {
                    energyNetworkGeneration[nanoGrid.networkName] += nanoGridGeneration;
                    energyNetworkGenerationCapacity[nanoGrid.networkName] += nanoGridGenerationCapacity / 1000;
                }
                solarInstallationsCount += 1;
            }
            if (nanoGrid.networkName != "" && nanoGrid.installScheduled)
            {
                energyNetworkScheduledCapacity[nanoGrid.networkName] += IoenManager.Instance.GetEnergyNetworkAverageSolarSize(nanoGrid.networkName) * IoenManager.Instance.GetAnnualGenerationFactor() / 1000;
            }
                //instantBattery += nanoGrid.GetInstantBatteryPower();
            if (nanoGrid.networkName != "" && nanoGrid.GetGenesis())
            {
                energyNetworkLoad[nanoGrid.networkName] += nanoGrid.GetIntervalLoad();
                energyNetworkAnnualLoad[nanoGrid.networkName] += nanoGrid.annualLoad / 1000;
            }
            instantLoad += nanoGrid.GetIntervalLoad();
        }
        solarInstallationsCountText.SetText("Solar Installs: " + solarInstallationsCount.ToString());
        batteryInstallationsCountText.SetText("Battery Installs: " + batteryInstallations);
        instantLoadText.SetText("Load: " + instantLoad.ToString("F1") + " kWh");
        instantSolarPowerText.SetText("Generation: " + instantSolar.ToString("F1") + " kWh");
        instantBatteryChargeText.SetText("Storage: " + instantBattery.ToString("F1") + " kWh");
        foreach (EnergyNetwork energyNetwork in IoenManager.Instance.GetEnergyNetworks().list)
        {
            List<NanoGrid> generatorsList = energyNetwork.nanoGrids.FindAll(n => n.source == "solar");
            int generators = 0;
            if (generatorsList.Count > 0) generators = generatorsList.Count;
            Transform ioenTransform = energyNetworkTransformDictionary[energyNetwork];
            ioenTransform.Find("numberOfNanoGridsText").GetComponent<TextMeshProUGUI>().SetText("Members: " + energyNetwork.nanoGrids.Count.ToString() + " Generators: " + generators);
            ioenTransform.Find("generatingText").GetComponent<TextMeshProUGUI>().SetText("Current Cap: " + energyNetworkGeneration[energyNetwork.name].ToString("F1") + " kWh");
            ioenTransform.Find("generationCapacityText").GetComponent<TextMeshProUGUI>().SetText("Annual Cap: " + energyNetworkGenerationCapacity[energyNetwork.name].ToString("F1") + " MWh");
            ioenTransform.Find("loadText").GetComponent<TextMeshProUGUI>().SetText("Annual Load: " + energyNetworkAnnualLoad[energyNetwork.name].ToString("F1") + " MWh");
            ioenTransform.Find("currentLoadText").GetComponent<TextMeshProUGUI>().SetText("Current Load: " + energyNetworkLoad[energyNetwork.name].ToString("F1") + " kWh");
            ioenTransform.Find("scheduledCapacityText").GetComponent<TextMeshProUGUI>().SetText("Sched Cap: " + energyNetworkScheduledCapacity[energyNetwork.name].ToString("F1") + " MWh");
        }
    }
}

