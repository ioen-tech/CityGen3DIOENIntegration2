using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NanoGrid : MonoBehaviour
{
    [SerializeField] public string buildingName = "";
    [SerializeField] public string networkId = "";
    [SerializeField] public string networkName = "";
    [SerializeField] public int networkIndex;
    [SerializeField] public int index;
    [SerializeField] public float size;
    [SerializeField] public string housenumber = "";
    [SerializeField] public string street = "";
    [SerializeField] public string suburb = "";
    [SerializeField] public string postcode = "";
    [SerializeField] public string state = "";
    [SerializeField] public string power = "";
    [SerializeField] public string source = "";

    private Color networkColour = Color.blue;
    private Renderer renderer;
    private bool genesis = false;
    private float delay = 0;
    private float genesisDelayTime = 0f;

    private void Start()
    {
        renderer = transform.GetComponent<Renderer>();
        renderer.material.color = Color.white;

        if (networkId != "")
        {
            delay = networkIndex * 60000;
        }
    }

    private void Update()
    {
        if (networkId != "")
        {
            genesisDelayTime += Time.deltaTime * GameManager.Instance.GetTimeSpeedFactor();
            if (genesisDelayTime > delay && genesis == false)
            {
                genesis = true;
                renderer.material.color = Color.Lerp(Color.white, networkColour, 1f);
                genesisDelayTime = 0;
            }
        }
    }
}
