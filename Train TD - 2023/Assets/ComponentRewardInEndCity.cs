using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentRewardInEndCity : MonoBehaviour, IGenericClickable, IResetShopBuilding
{
	public void CheckIfShouldEnableSelf() {
		if (DataSaver.s.GetCurrentSave().currentRun.currentAct == 1) {
			gameObject.SetActive(false);
		}else {
			gameObject.SetActive(true);
		}
	}

	public Transform GetUITargetTransform() {
		return transform;
	}

	public void SetHoldingState(bool state) {
		// do nothing
	}

	public DroneRepairController GetHoldingDrone() {
		return null;
	}

	public void SetHoldingDrone(DroneRepairController holder) {
		// do nothing
	}

	public void Click() {
		StopAndPick3RewardUIController.s.ShowComponentReward();
		Destroy(gameObject);
	}

}
