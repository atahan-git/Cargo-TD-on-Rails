using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class StopAndPick3RewardUIController : MonoBehaviour {
	public static StopAndPick3RewardUIController s;


	public GameObject showRewardScreen;
	public Transform rewardsParent;
	public GameObject gemRewardPrefab;
	public GameObject cartRewardPrefab;

	public Vector3 GetRewardPos() {
		return Train.s.trainFront.position + Vector3.up/2f;
	}

	public Quaternion GetRewardRotation() {
		return Random.rotation;
	}

	public Vector3 GetUpForce() {
		return SmitheryController.GetRandomYeetForce();
	}
	
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
		if (PlayStateMaster.s.isCombatInProgress()) {
			yield return new WaitForSecondsRealtime(1f);
		}

		showRewardScreen.SetActive(true);
	}

	void HideScreen() {
		SetTimeSlowdownState(false);
		showRewardScreen.SetActive(false);
		PlayerWorldInteractionController.s.canSelect = true;
	}

	public void TryShowGemReward() {
		if (PathAndTerrainGenerator.s.currentPathTree.myDepth < MapController.s.currentMap.bossDepth) {
			SpawnGemRewardNextIntersection();
		}
	}

	public void ShowGemReward(bool isBigGem) {
		ClearAndShowScreen();
		for (int i = 0; i < 3; i++) {
			Instantiate(gemRewardPrefab, rewardsParent).GetComponent<MiniGUI_Pick3GemReward>().SetUp(UpgradesController.s.GetGemReward(isBigGem), isBigGem);
		}
	}

	public GameObject gemRewardOnRoadPrefab;
	public GameObject bigGemRewardOnRoadPrefab;
	public GameObject cartRewardOnRoadPrefab;

	void SpawnGemRewardNextIntersection() {
		switch (MapController.s.GetSwitchRewardAtDepth(PathAndTerrainGenerator.s.currentPathTree.myDepth+1)) {
			case MapController.SwitchReward.empty:
				// do nothing
				break;
			case MapController.SwitchReward.miniGem:
				Instantiate(gemRewardOnRoadPrefab);
				break;
			case MapController.SwitchReward.bigGem:
				Instantiate(bigGemRewardOnRoadPrefab);
				break;
		}
	}

	public void SpawnMiniGemRewardAtDistance(float position) {
		Instantiate(gemRewardOnRoadPrefab).GetComponent<GemRewardOnRoad>().SetUp(position);
	}
	public void SpawnBigGemRewardAtDistance(float position) {
		Instantiate(bigGemRewardOnRoadPrefab).GetComponent<GemRewardOnRoad>().SetUp(position);
	}
	public void SpawnCartRewardAtDistance(float position) {
		Instantiate(cartRewardOnRoadPrefab).GetComponent<CartRewardOnRoad>().SetUp(position);
	}
	
	public void SpawnMiniGemRewardAtDistance(float position, DataSaver.TrainState.ArtifactState state) {
		Instantiate(gemRewardOnRoadPrefab).GetComponent<GemRewardOnRoad>().SetUp(position, state);
	}
	public void SpawnBigGemRewardAtDistance(float position, DataSaver.TrainState.ArtifactState state) {
		Instantiate(bigGemRewardOnRoadPrefab).GetComponent<GemRewardOnRoad>().SetUp(position, state);
	}
	public void SpawnCartRewardAtDistance(float position, DataSaver.TrainState.CartState state) {
		Instantiate(cartRewardOnRoadPrefab).GetComponent<CartRewardOnRoad>().SetUp(position, state);
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


	public void ShowComponentReward() {
		ClearAndShowScreen();
		for (int i = 0; i < 3; i++) {
			Instantiate(gemRewardPrefab, rewardsParent).GetComponent<MiniGUI_Pick3GemReward>().SetUp(UpgradesController.s.GetComponentReward(), false);
		}
	}
	
	void SetTimeSlowdownState(bool state) {
		TimeController.s.SetSlowDownAndPauseState(state);
	}
}
