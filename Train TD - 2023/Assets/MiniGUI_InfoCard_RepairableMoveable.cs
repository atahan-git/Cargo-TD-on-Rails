using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class MiniGUI_InfoCard_RepairableMoveable : MonoBehaviour, IBuildingInfoCard {

    public Toggle repairable;
    public Toggle fragile;
    public Toggle moveable;
    
    public void SetUp(Cart building) {
        //repairable.isOn = building.isRepairable;
        repairable.isOn = true;
        fragile.isOn = !building.isRepairable;
        moveable.isOn = !building.isMainEngine;
    }
    
    public void SetUp(EnemyHealth enemy) {
        gameObject.SetActive(false);
    }

    public void SetUp(Artifact artifact) {
        gameObject.SetActive(false);
    }
}
