using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopAndPick3RewardUIController : MonoBehaviour {
	public static StopAndPick3RewardUIController s;


	public GameObject showRewardScreen;
	public Transform rewardsParent;
	public GameObject gemRewardPrefab;
	public GameObject cartRewardPrefab;

	public Transform instantiatePos;
	private void Awake() {
		s = this;
		showRewardScreen.SetActive(false);
	}
	
	void ClearAndShowScreen() {
		SetTimeSlowdownState(true);
		rewardsParent.DeleteAllChildren();
		DirectControlMaster.s.DisableDirectControl();
		PlayerWorldInteractionController.s.canSelect = false;
		StartCoroutine(ShowScreenWithDelay());
	}

	IEnumerator ShowScreenWithDelay() {
		yield return new WaitForSecondsRealtime(1f);
		showRewardScreen.SetActive(true);
	}

	void HideScreen() {
		SetTimeSlowdownState(false);
		showRewardScreen.SetActive(false);
		PlayerWorldInteractionController.s.canSelect = true;
	}

	public void TryShowGemReward() {
		if (PathAndTerrainGenerator.s.currentPathTree.myDepth % 2 == 1) {
			//ShowGemReward();
			// the gem will spawn the reward on its own now
		} else {
			if (PathAndTerrainGenerator.s.currentPathTree.myDepth < MapController.s.currentMap.bossDepth) {
				SpawnGemRewardNextIntersection();
			}
		}
	}

	public void ShowGemReward() {
		ClearAndShowScreen();
		for (int i = 0; i < 3; i++) {
			Instantiate(gemRewardPrefab, rewardsParent).GetComponent<MiniGUI_Pick3GemReward>().SetUp(UpgradesController.s.GetGemReward());
		}
	}

	public GameObject gemRewardOnRoadPrefab;
	void SpawnGemRewardNextIntersection() {
		Instantiate(gemRewardOnRoadPrefab);
	}
	

	public void ShowCartReward() {
		ClearAndShowScreen();
		for (int i = 0; i < 3; i++) {
			Instantiate(cartRewardPrefab, rewardsParent).GetComponent<MiniGUI_Pick3CartReward>().SetUp(UpgradesController.s.GetCartReward());
		}
	}


	public void RewardWasPicked() {
		HideScreen();
	}

	public void SkipRewards() {
		HideScreen();
	}

	void SetTimeSlowdownState(bool state) {
		TimeController.s.SetSlowDownAndPauseState(state);
	}
}
