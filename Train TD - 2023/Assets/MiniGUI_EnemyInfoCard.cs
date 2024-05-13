using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_EnemyInfoCard : MonoBehaviour
{
    public Image icon;
    public TMP_Text moduleName;
    public TMP_Text moduleDescription;

    public void SetUp(EnemyHealth enemy) {
        var posFollower = GetComponent<MiniGUI_InfoCardFollowPositionLogic>();

        var swarm = GetComponent<EnemyInSwarm>();
        icon.sprite = swarm.enemyIcon;
        var info = GetComponent<ClickableEntityInfo>();
        moduleName.text = info.info;

        moduleDescription.text = info.tooltip.text;
        
        posFollower.SetUp(enemy.GetUITargetTransform());
        Show();
    }

    public void Show() {
        GetComponent<MiniGUI_InfoCardFollowPositionLogic>().Show();
    }

    public void Hide() {
        GetComponent<MiniGUI_InfoCardFollowPositionLogic>().Hide();
    }
}
