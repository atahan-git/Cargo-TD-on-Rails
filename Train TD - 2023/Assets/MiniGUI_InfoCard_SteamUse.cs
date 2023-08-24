using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class MiniGUI_InfoCard_SteamUse : MonoBehaviour, IBuildingInfoCard {

    [ReadOnly] public EngineModule engineModule;
    [ReadOnly] public GunModule gunModule;
    [ReadOnly] public RoboRepairModule roboRepairModule;

    public void SetUp(Cart building) {
        engineModule = building.GetComponentInChildren<EngineModule>();
        gunModule = building.GetComponentInChildren<GunModule>();
        roboRepairModule = building.GetComponentInChildren<RoboRepairModule>();

        if (!(engineModule != null || gunModule != null || roboRepairModule != null)) {
            gameObject.SetActive(false);
            return;
        }else{
            gameObject.SetActive(true);
        }
    }
    
    public void SetUp(EnemyHealth enemy) {
        gameObject.SetActive(false);
    }

    public void SetUp(Artifact artifact) {
        gameObject.SetActive(false);
    }
}
