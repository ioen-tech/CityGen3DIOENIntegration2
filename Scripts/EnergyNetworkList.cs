using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Energy Network List")]
public class EnergyNetworkList : ScriptableObject
{
    public List<EnergyNetwork> list;
}