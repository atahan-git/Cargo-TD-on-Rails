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

    
    [Space] 
    public Transform gunSlotsParent;
    public GameObject gunSlotPrefab;
    
    public void SetUp(EnemyHealth enemy) {
        
        print("showing enemy info");
        var posFollower = GetComponent<MiniGUI_InfoCardFollowPositionLogic>();

        var swarm = enemy.GetComponent<EnemyInSwarm>();
        icon.sprite = swarm.enemyIcon;
        var info = enemy.GetComponent<ClickableEntityInfo>();
        moduleName.text = info.info;

        moduleDescription.text = info.tooltip.text;
        
        
        gunSlotsParent.DeleteAllChildren();

        var guns = enemy.GetComponentsInChildren<IEnemyEquipment>();

        for (int i = 0; i < guns.Length; i++) {
            Instantiate(gunSlotPrefab, gunSlotsParent).GetComponent<MiniGUI_EnemyEquipmentInfoSlot>().SetUp(guns[i]);
        }
        
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

public interface IEnemyEquipment {
    public Sprite GetSprite();
    public string GetName();
    public string GetDescription();
}
