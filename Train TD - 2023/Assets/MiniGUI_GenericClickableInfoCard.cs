using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_GenericClickableInfoCard : MonoBehaviour
{

    public Image icon;
    public TMP_Text moduleName;
    public TMP_Text moduleDescription;

    [Space] 
    public Transform gunSlotsParent;
    public GameObject gunSlotPrefab;
    
    public void SetUp(GenericClickable genericClickable) {
        
        print("showing enemy info");
        var posFollower = GetComponent<MiniGUI_InfoCardFollowPositionLogic>();

        icon.sprite = genericClickable.myIcon;
        moduleName.text = genericClickable.myDetailsTitle;

        moduleDescription.text = genericClickable.myDetails;
        
        gunSlotsParent.DeleteAllChildren();

        var genericInfo = genericClickable.GetComponents<IGenericInfoProvider>();

        for (int i = 0; i < genericInfo.Length; i++) {
            Instantiate(gunSlotPrefab, gunSlotsParent).GetComponent<MiniGUI_GenericInfoSlot>().SetUp(genericInfo[i]);
        }
        
        posFollower.SetUp(genericClickable.GetUITargetTransform());
        Show();
    }

    public void Show() {
        GetComponent<MiniGUI_InfoCardFollowPositionLogic>().Show();
    }

    public void Hide() {
        GetComponent<MiniGUI_InfoCardFollowPositionLogic>().Hide();
    }
}

public interface IGenericInfoProvider {
    public Sprite GetSprite();
    public string GetDescription();
}
