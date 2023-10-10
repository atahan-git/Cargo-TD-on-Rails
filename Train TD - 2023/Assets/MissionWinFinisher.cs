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
		gateScript.SetCanGoStatus(false, deliverYourCargoFirstTooltip);
	}

	public GameObject mysteriousCargoDeliveryArea;
	public bool needToDeliverMysteriousCargo = false;
	
	public GameObject winContinueButton;

	public bool isWon = false;
	public void MissionWon(bool isShowingPrevRewards = false) {
		var targetStar = DataSaver.s.GetCurrentSave().currentRun.map.GetStarWithName(DataSaver.s.GetCurrentSave().currentRun.targetStar);

		if (targetStar.isBoss) {
			needToDeliverMysteriousCargo = true;
			mysteriousCargoDeliveryArea.SetActive(true);
		} else {
			mysteriousCargoDeliveryArea.SetActive(false);
			needToDeliverMysteriousCargo = false;
		}
		
		SpeedController.s.TravelToMissionEndDistance();
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

		MapController.s.FinishTravelingToStar();

		// mission rewards
		if (!isShowingPrevRewards) {
			UpgradesController.s.ClearCurrentShop();
			GenerateMissionRewards();
		}
		
		
		// save our resources
		mySave.currentRun.myResources.scraps = Mathf.FloorToInt(MoneyController.s.scraps);
		Train.s.SaveTrainState(true);
		mySave.currentRun.isInEndRunArea = true;
		
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

					{ "character", DataSaver.s.GetCurrentSave().currentRun.character.uniqueName },

					{ "enemiesLeftAlive", EnemyHealth.enemySpawned - EnemyHealth.enemyKilled },
					{ "winTime", SpeedController.s.currentTime },
				}
			);
			
			Debug.Log("Mission Won Analytics: " + analyticsResult);
		}
		

		if(!isShowingPrevRewards)
			SoundscapeController.s.PlayMissionWonSound();

		// MusicPlayer.s.SwapMusicTracksAndPlay(false);
		FMODMusicPlayer.s.SwapMusicTracksAndPlay(false);

		DirectControlMaster.s.DisableDirectControl();
		
		if(SettingsController.GamepadMode())
			EventSystem.current.SetSelectedGameObject(winContinueButton);
	}

	void OnActCleared(int current_act) {//eg if you finish act 1 this number will be equal to 1
		var allArtifacts = ArtifactsController.s.myArtifacts;

		var eligibleComponents = new List<Artifact>();
		var eligibleGems = new List<Artifact>();
		for (int i = 0; i < allArtifacts.Count; i++) {
			switch (allArtifacts[i].myRarity) {
				case UpgradesController.CartRarity.common:
				case UpgradesController.CartRarity.rare:
				case UpgradesController.CartRarity.epic:
					eligibleGems.Add(allArtifacts[i]);
					break;
				case UpgradesController.CartRarity.boss:
					eligibleComponents.Add(allArtifacts[i]);
					break;
				case UpgradesController.CartRarity.special:
					// do nothing. We never recover special artifacts
					break;
			}
		}

		var eligibleCarts = new List<Cart>();

		for (int i = 0; i < Train.s.carts.Count; i++) {
			var cart = Train.s.carts[i];
			if (!cart.isCargo && !cart.isMainEngine && !cart.isMysteriousCart && cart.myRarity != UpgradesController.CartRarity.special) {
				eligibleCarts.Add(cart);
			}
		}

		if (eligibleComponents.Count > 0) {
			DataSaver.s.GetCurrentSave().metaProgress.bonusComponent = eligibleComponents[Random.Range(0, eligibleComponents.Count)].uniqueName;
		}

		if (eligibleGems.Count > 0) {
			DataSaver.s.GetCurrentSave().metaProgress.bonusGem = eligibleGems[Random.Range(0, eligibleGems.Count)].uniqueName;
		}

		if (eligibleCarts.Count > 0) {
			DataSaver.s.GetCurrentSave().metaProgress.bonusCart = eligibleCarts[Random.Range(0, eligibleCarts.Count)].uniqueName;
		}
		
		/*if (current_act >= 2) {
			for (int i = 0; i < eligibleBossArtifacts.Count; i++) {
				if (!DataSaver.s.GetCurrentSave().metaProgress.unlockedStarterArtifacts.Contains(eligibleBossArtifacts[i].uniqueName)) {
					DataSaver.s.GetCurrentSave().metaProgress.unlockedStarterArtifacts.Add(eligibleBossArtifacts[i].uniqueName);
					break;
				}
			}
		}*/
	}

	void ChangeRangeShowState(bool state) {
		var ranges = Train.s.GetComponentsInChildren<RangeVisualizer>();

		for (int i = 0; i < ranges.Length; i++) {
			ranges[i].ChangeVisualizerEdgeShowState(state);
		}
	}

	public void ShowUnclaimedRewards() {
		MissionWon(true);
		//Invoke(nameof(DelayedShowRewards), 0.05f);
	}


	void GenerateMissionRewards() {
		var mySave = DataSaver.s.GetCurrentSave();
		var playerStar = mySave.currentRun.map.GetPlayerStar();

		Train.s.SaveTrainState();
		mySave.metaProgress.castlesTraveled += 1;
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

		
		UpgradesController.s.UpdateCargoHighlights();
		
		PlayStateMaster.s.EnterMissionRewardArea();

		Invoke(nameof(SplitSecondLater), 0.05f);
	}

	void SplitSecondLater() {
		PlayerWorldInteractionController.s.canSelect = true;
	}

	public void ContinueToNextCity() {
		if (isWon) { // call this only once
			var targetStar = DataSaver.s.GetCurrentSave().currentRun.map.GetStarWithName(DataSaver.s.GetCurrentSave().currentRun.targetStar);
			
			if (targetStar.isBoss) {
				ActFinishController.s.OpenActWinUI();
				OnActCleared(DataSaver.s.GetCurrentSave().currentRun.currentAct);
			} else {
				PlayStateMaster.s.LeaveMissionRewardArea();
			}
		}

		isWon = false;
		winUI.SetActive(false);
	}

	public void CleanupWhenLeavingMissionRewardArea() {
		DataSaver.s.GetCurrentSave().currentRun.shopInitialized = false;
		DataSaver.s.GetCurrentSave().currentRun.isInEndRunArea = false;
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
