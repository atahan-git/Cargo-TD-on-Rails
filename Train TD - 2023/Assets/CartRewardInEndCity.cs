using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;

public class CartRewardInEndCity : MonoBehaviour, IClickableWorldItem, IResetShopBuilding {
    public bool fullyDisable = false;
    public void CheckIfShouldEnableSelf() {
        if (fullyDisable) {
            gameObject.SetActive(false);
            return;
        }
        if (DataSaver.s.GetCurrentSave().currentRun.currentAct == 1) {
            gameObject.SetActive(false);
        } else {
            gameObject.SetActive(true);
        }
    }
    
    public bool CanClick() {
        return !PlayStateMaster.s.isCombatInProgress();
    }

    public void _OnMouseEnter() {
        GetComponent<HighlightEffect>().enabled = true;
    }

    public void _OnMouseExit() {
        GetComponent<HighlightEffect>().enabled = false;
    }

    public void _OnMouseUpAsButton() {
        StopAndPick3RewardUIController.s.ShowCartReward();
        Destroy(gameObject);
    }
}
