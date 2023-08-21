using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class MiniGUI_InfoCard_ExtraEffect : MonoBehaviour, IBuildingInfoCard {

    public TMP_Text myText;

    [ReadOnly] public IExtraInfo extraInfo;
    [ReadOnly] public ClickableEntityInfo tooltip;
    public void SetUp(Cart building) {
        extraInfo = building.GetComponentInChildren<IExtraInfo>();
        
        if (extraInfo == null) {
            gameObject.SetActive(false);
            return;
        }else{
            gameObject.SetActive(true);
        }

        myText.text = extraInfo.GetInfoText();
    }

    public void SetUp(EnemyHealth enemy) {
        tooltip = enemy.GetComponent<ClickableEntityInfo>();

        if (tooltip == null) {
            gameObject.SetActive(false);
            return;
        } else {
            gameObject.SetActive(true);
        }

        myText.text = tooltip.GetTooltip().text;
    }

    public void SetUp(Artifact artifact) {
        extraInfo = artifact.GetComponentInChildren<IExtraInfo>();
        
        if (extraInfo == null) {
            gameObject.SetActive(false);
            return;
        }else{
            gameObject.SetActive(true);
        }

        myText.text = extraInfo.GetInfoText();
    }
}


public interface IExtraInfo {
    public string GetInfoText();
}