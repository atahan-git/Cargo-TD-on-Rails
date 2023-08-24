using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MiniGUI_InfoCard_Rarity : MonoBehaviour, IBuildingInfoCard {


    public GameObject common;
    public GameObject rare;
    public GameObject epic;
    
    public void SetUp(Cart building) {
        common.SetActive(false);
        rare.SetActive(false);
        epic.SetActive(false);
        
        switch (building.myRarity) {
        case UpgradesController.CartRarity.common:
            common.SetActive(true);
            
            break;
        case UpgradesController.CartRarity.rare:
            rare.SetActive(true);
            
            break;
        case UpgradesController.CartRarity.epic:
            epic.SetActive(true);
            
            break;
        
        default:
            gameObject.SetActive(false);
            break;
        }
    }

    public void SetUp(EnemyHealth enemy) {
        gameObject.SetActive(false);
    }

    public void SetUp(Artifact artifact) {
        common.SetActive(false);
        rare.SetActive(false);
        epic.SetActive(false);
        
        switch (artifact.myRarity) {
            case UpgradesController.CartRarity.common:
                common.SetActive(true);
            
                break;
            case UpgradesController.CartRarity.rare:
                rare.SetActive(true);
            
                break;
            case UpgradesController.CartRarity.epic:
                epic.SetActive(true);
            
                break;
        
            default:
                gameObject.SetActive(false);
                break;
        }
    }
}
