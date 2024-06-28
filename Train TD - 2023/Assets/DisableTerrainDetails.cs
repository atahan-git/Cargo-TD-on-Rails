using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class DisableTerrainDetails : MonoBehaviour
{
    
    [Button]
    public void SetTerrainDetailsState(bool isEnabled) {
        foreach (var terrain in GetComponent<ObjectPool>().GetAllObjs()) {
            terrain.GetComponent<Terrain>().drawTreesAndFoliage = isEnabled;
        }
    }
}
