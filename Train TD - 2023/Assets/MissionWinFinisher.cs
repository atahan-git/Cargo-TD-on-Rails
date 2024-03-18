using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MissionWinFinisher : MonoBehaviour {
	public static MissionWinFinisher s;

	private void Awake() {
		s = this;
	}

	public MonoBehaviour[] scriptsToDisable;
	public GameObject[] gameObjectsToDisable;

	public GameObject winUI;
	public GameObject winInitialUI;
	public GameObject winCheckoutUI;

	public CameraSwitcher cameraSwitcher;
	
	public GateScript gateScript;

	public Tooltip deliverYourCargoFirstTooltip;
	public Tooltip getAllTheCarts;
	public Tooltip allGoodToGoTooltip;
	private void Start() {
		winUI.SetActive(false);
		gateScript.OnCanLeaveAndPressLeave.AddListener(ContinueToNextCity);
		//gateScript.SetCanGoStatus(false, deliverYourCargoFirstTooltip);
	}

	public bool isWon = false;
	public void MissionWon(bool isShowingPrevRewards = false) {
		
		SpeedController.s.TravelToMissionEndDistance(isShowingPrevRewards);
		isWon = true;
		PlayStateMaster.s.FinishCombat(!isShowingPrevRewards);
		EnemyWavesController.s.Cleanup();
		PlayerWorldInteractionController.s.canSelect = false;
		//EnemyHealth.winSelfDestruct?.Invoke(false);



		for (int i = 0; i < scriptsToDisable.Length; i++) {
			scriptsToDisable[i].enabled = false;
		}
		
		for (int i = 0; i < gameObjectsToDisable.Length; i++) {
			gameObjectsToDisable[i].SetActive(false);
		}

		ChangeRangeShowState(false);
		
		var mySave = DataSaver.s.GetCurrentSave();

		// mission rewards
		if (!isShowingPrevRewards) {
			UpgradesController.s.ClearCurrentShop();
			GenerateMissionRewards();
		}
		
		
		// save our resources
		Train.s.SaveTrainState(true);
		mySave.isInEndRunArea = true;
		
		DataSaver.s.SaveActiveGame();
		
		cameraSwitcher.Engage();
		winUI.SetActive(true);
		winInitialUI.SetActive(true);
		winCheckoutUI.SetActive(false);


		if (PlayStateMaster.s.currentLevel != null)  { // if level is null that means we are getting unclaimed rewards. hence no need to send data again.
			
			//send analytics
			AnalyticsResult analyticsResult = Analytics.CustomEvent(
				"LevelWon",
				new Dictionary<string, object> {
					{ "Level", PlayStateMaster.s.currentLevel.levelName },
					{ "enemiesLeftAlive", EnemyHealth.enemySpawned - EnemyHealth.enemyKilled },
					{ "winTime", WorldDifficultyController.s.GetMissionTime()},
				}
			);
			
			Debug.Log("Mission Won Analytics: " + analyticsResult);
		}
		

		if(!isShowingPrevRewards)
			SoundscapeController.s.PlayMissionWonSound();

		// MusicPlayer.s.SwapMusicTracksAndPlay(false);
		FMODMusicPlayer.s.SwapMusicTracksAndPlay(false);

		DirectControlMaster.s.DisableDirectControl();
	}

	void ChangeRangeShowState(bool state) {
		var ranges = Train.s.GetComponentsInChildren<RangeVisualizer>();

		for (int i = 0; i < ranges.Length; i++) {
			ranges[i].ChangeVisualizerEdgeShowState(state);
		}
	}

	public void ShowUnclaimedRewards() {
		//PathAndTerrainGenerator.s.MakeFakePathForMissionRewards();
		MissionWon(true);
		//Invoke(nameof(DelayedShowRewards), 0.05f);
	}


	void GenerateMissionRewards() {
		var mySave = DataSaver.s.GetCurrentSave();

		Train.s.SaveTrainState();
		mySave.castlesTraveled += 1;
	}

	
	public void ContinueToClearOutOfCombat() {
		for (int i = 0; i < gameObjectsToDisable.Length; i++) {
			gameObjectsToDisable[i].SetActive(false);
		}
		
		cameraSwitcher.Disengage();
		winInitialUI.SetActive(false);
		winCheckoutUI.SetActive(true);
		
		for (int i = 0; i < scriptsToDisable.Length; i++) {
			scriptsToDisable[i].enabled = true;
		}

		PlayStateMaster.s.EnterMissionRewardArea();

		Invoke(nameof(SplitSecondLater), 0.05f);
	}

	void SplitSecondLater() {
		PlayerWorldInteractionController.s.canSelect = true;
	}

	public void ContinueToNextCity() {
		if (isWon) { // call this only once

			/*if (targetStar.isBoss) {
				ActFinishController.s.OpenActWinUI();
				OnActCleared(DataSaver.s.GetCurrentSave().currentRun.currentAct);
			} else {*/
				PlayStateMaster.s.LeaveMissionRewardAreaAndEnterShopState();
			//}
		}

		isWon = false;
		winUI.SetActive(false);
	}

	public void CleanupWhenLeavingMissionRewardArea() {
		//PathSelectorController.s.trainStationEnd.SetActive(false);
		DataSaver.s.GetCurrentSave().shopInitialized = false;
		DataSaver.s.GetCurrentSave().isInEndRunArea = false;
		Train.s.SaveTrainState(true);
		DataSaver.s.SaveActiveGame();
	}
	
	
	public void SetCanGo() {
		gateScript.SetCanGoStatus(true, allGoodToGoTooltip);
	}

	public void SetCannotGo(bool becauseOfDelivery) {
		if (becauseOfDelivery) {
			gateScript.SetCanGoStatus(false, deliverYourCargoFirstTooltip);
		} else {
			gateScript.SetCanGoStatus(false, getAllTheCarts);
		}
	}
}
