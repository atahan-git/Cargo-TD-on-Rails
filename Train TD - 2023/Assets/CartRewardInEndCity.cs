using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CartRewardInEndCity : MonoBehaviour, IGenericClickable, IResetShopBuilding
{
    public void CheckIfShouldEnableSelf() {
        if (DataSaver.s.GetCurrentSave().currentRun.currentAct == 1) {
            gameObject.SetActive(false);
        } else {
            gameObject.SetActive(true);
        }
    }

    public Transform GetUITargetTransform() {
        return transform;
    }

    public void SetHoldingState(bool state) {
        // do nothing
    }


    public void Click() {
        StopAndPick3RewardUIController.s.ShowCartReward();
        Destroy(gameObject);
    }
}
