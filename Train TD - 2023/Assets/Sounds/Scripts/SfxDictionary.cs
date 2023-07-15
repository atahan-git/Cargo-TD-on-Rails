using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
public enum SfxTypes
{
    ButtonClick1, ButtonClick2,
    OnCargoHover, OnCargoPickUp, OnCargoDrop
}


[CreateAssetMenu(fileName = "SfxDictionary", menuName = "FMOD/SfxDictionary")]
public class SfxDictionary : SerializedScriptableObject
{
    public Dictionary<SfxTypes, EventReference> sfxDict;
}
