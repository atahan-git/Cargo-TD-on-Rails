using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGUI_InfoCard_FragileSturdy : MonoBehaviour, IBuildingInfoCard {

    public GameObject fragile;
    public GameObject sturdy;

    [Multiline]
    public string fragileText;
    [Multiline]
    public string sturdyText;
    
    public void SetUp(Cart building) {
        fragile.SetActive(false);
        sturdy.SetActive(false);
        if (building.isFragile) {
            fragile.SetActive(true);
            GetComponent<UITooltipDisplayer>().myTooltip.text = fragileText;
        }

        if (building.isSturdy) {
            sturdy.SetActive(true);
            GetComponent<UITooltipDisplayer>().myTooltip.text = sturdyText;
        }

    }

    public void SetUp(EnemyHealth enemy) {
        gameObject.SetActive(false);
    }

    public void SetUp(Artifact artifact) {
        gameObject.SetActive(false);
    }
}
