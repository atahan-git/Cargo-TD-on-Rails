using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGUI_InfoCard_CriticalComponent : MonoBehaviour, IBuildingInfoCard {


    public void SetUp(Cart building) {
        if (!building.loseGameIfYouLoseThis) {
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
