using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/PowerType")]
public class PowerType : ScriptableObject
{
    public string nameString;
    public string nameShort;
    public Sprite sprite;
    public int amount;
    public float co2;
}
