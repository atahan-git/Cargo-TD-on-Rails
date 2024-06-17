using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetShopBuildings : MonoBehaviour {
	public static ResetShopBuildings s;

	private void Awake() {
		s = this;
	}

	private void Start() {
		OnShopEntered();
	}

	public void OnShopEntered() {
		var myBuildings = GetComponentsInChildren<IResetShopBuilding>(true);
		var forceDisable = PlayStateMaster.s.myGameState == PlayStateMaster.GameState.mainMenu;
		for (int i = 0; i < myBuildings.Length; i++) {
			if (forceDisable) {
				(myBuildings[i] as MonoBehaviour).gameObject.SetActive(false);
			} else {
				myBuildings[i].CheckIfShouldEnableSelf();
			}
		}
	}
}


public interface IResetShopBuilding {
	public void CheckIfShouldEnableSelf();
}