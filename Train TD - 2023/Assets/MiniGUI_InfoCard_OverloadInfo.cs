using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class MiniGUI_InfoCard_OverloadInfo : MonoBehaviour, IBuildingInfoCard {

    [ReadOnly] public EngineModule engineModule;

    public void SetUp(Cart building) {
        engineModule = building.GetComponentInChildren<EngineModule>();

        if (engineModule == null) {
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
