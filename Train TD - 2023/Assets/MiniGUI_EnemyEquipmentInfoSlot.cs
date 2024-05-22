using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_EnemyEquipmentInfoSlot : MonoBehaviour
{
    public Image icon;
    public TMP_Text title;
    public TMP_Text description;

    public void SetUp(IEnemyEquipment myEnemyEquipment) {
        if (myEnemyEquipment != null) {
            icon.sprite = myEnemyEquipment.GetSprite();
            title.text = myEnemyEquipment.GetName();
            description.text = myEnemyEquipment.GetDescription();
        } 
    }
}
