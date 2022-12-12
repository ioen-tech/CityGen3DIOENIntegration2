using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUI : MonoBehaviour
{
    private PowerTypeList powerTypeList;
    private Dictionary<PowerType, Transform> powerTypeTransformDictionary;
    private void Awake()
    {
        powerTypeList = Resources.Load<PowerTypeList>(typeof(PowerTypeList).Name);
        powerTypeTransformDictionary = new Dictionary<PowerType, Transform>();
        Transform powerTemplate = transform.Find("powerTemplate");
        powerTemplate.gameObject.SetActive(false);
        int index = 0;
        foreach (PowerType powerType in powerTypeList.list)
        {
            Transform powerTransform = Instantiate(powerTemplate, transform);
            powerTransform.gameObject.SetActive(true);
            float offsetAmount = -80f;
            powerTransform.GetComponent<RectTransform>().anchoredPosition = new Vector2(-60, offsetAmount * index - 400);
            powerTransform.Find("image").GetComponent<Image>().sprite = powerType.sprite;
            powerTypeTransformDictionary[powerType] = powerTransform;
            index++;
        }
    }

    private void Start()
    {
        IoenManager.Instance.OnPowerAmountChanged += PowerManager_OnPowerAmountChanged;
        UpdatePowerAmount();
    }

    private void PowerManager_OnPowerAmountChanged(object sender, System.EventArgs e)
    {
        UpdatePowerAmount();
    }

    private void UpdatePowerAmount()
    {
        foreach (PowerType powerType in powerTypeList.list)
        {
            Transform powerTransform = powerTypeTransformDictionary[powerType];
            float intervalAmount = IoenManager.Instance.GetIntervalAmount(powerType) /1000;
            float totalAmount = IoenManager.Instance.GetTotalAmount(powerType) / 1000;
            float co2Amount = totalAmount * powerType.co2 / 1000;
            powerTransform.Find("intervalText").GetComponent<TextMeshProUGUI>().SetText(intervalAmount.ToString("F1") + " MWh");
            powerTransform.Find("totalText").GetComponent<TextMeshProUGUI>().SetText(totalAmount.ToString("F1") + " MWh");
            powerTransform.Find("co2Text").GetComponent<TextMeshProUGUI>().SetText(co2Amount.ToString("F1") + " t CO2");
        }
    }
}
