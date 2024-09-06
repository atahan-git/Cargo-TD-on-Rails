using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;

public class ComponentRewardInEndCity : MonoBehaviour, IClickableWorldItem, IResetShopBuilding
{
	public void CheckIfShouldEnableSelf() {
		if (DataSaver.s.GetCurrentSave().currentRun.currentAct == 1) {
			gameObject.SetActive(false);
		}else {
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
		StopAndPick3RewardUIController.s.ShowComponentReward();
		Destroy(gameObject);
	}
}
