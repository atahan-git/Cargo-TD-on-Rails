using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MiniGUI_BuildingInfoCard : MonoBehaviour
{
    public Image icon;
    public Image armorPenetrationIcon;
    public Image fireDamageIcon;
    public Image explosiveDamageIcon;
    public TMP_Text moduleName;
    [Space] 
    public TMP_Text moduleDescription;
    
    [Space] 
    public Transform gemSlotsParent;
    public GameObject gemSlotPrefab;

    public void SetUp(Cart building) {
        var posFollower = GetComponent<MiniGUI_InfoCardFollowPositionLogic>();

        var gunModule = building.GetComponentInChildren<GunModule>();
        if (gunModule != null) {
            armorPenetrationIcon.gameObject.SetActive(gunModule.canPenetrateArmor);
            fireDamageIcon.gameObject.SetActive(gunModule.GetBurnDamage() > 0);
            explosiveDamageIcon.gameObject.SetActive(gunModule.GetExplosionRange() > 0);
            
            icon.sprite = gunModule.gunSprite;
        } else {
            armorPenetrationIcon.gameObject.SetActive(false);
            fireDamageIcon.gameObject.SetActive(false);
            explosiveDamageIcon.gameObject.SetActive(false);
            
            icon.sprite = building.Icon;
        }
        
        moduleName.text = building.displayName;

        moduleDescription.text = building.GetComponentInChildren<ClickableEntityInfo>().GetTooltip().text;


        gemSlotsParent.DeleteAllChildren();

        for (int i = 0; i < building.myArtifactLocations.Count; i++) {
            Instantiate(gemSlotPrefab, gemSlotsParent).GetComponent<MiniGUI_GemInfoSlot>().SetUp(building.myArtifactLocations[i]);
        }

        posFollower.SetUp(building.GetUITargetTransform());
        Show();
    }

    public void Show() {
        GetComponent<MiniGUI_InfoCardFollowPositionLogic>().Show();
    }

    public void Hide() {
        GetComponent<MiniGUI_InfoCardFollowPositionLogic>().Hide();
    }
    
}