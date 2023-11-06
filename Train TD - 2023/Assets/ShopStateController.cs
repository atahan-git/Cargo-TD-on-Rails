using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopStateController : MonoBehaviour {
	public static ShopStateController s;
	private void Awake() { s = this; }

	public List<LevelData> allLevels {
		get {
			return LevelDataLoader.s.allLevels;
		}
	}

	public GameObject starterUI;
	public GameObject gameUI;

	public int selectedLevelIndex = -1;

	public enum CanStartLevelStatus {
		needToPutThingInFleaMarket, needToPickUpFreeCarts, needToSelectDestination, allGoodToGo
	}

	public Button mapOpenButton;

	public TMP_Text backToProfileOrAbandonText;

	private void Start() {
		gateScript.OnCanLeaveAndPressLeave.AddListener(StartLevel);
	}

	void UpdateBackToProfileOrAbandonButton() {
		/*if (PlayStateMaster.s.isCombatInProgress()) {
			backToProfileOrAbandonText.text = "Abandon Run";
		} else {*/
			backToProfileOrAbandonText.text = "Back to Main Menu";
		//}
	}
	public void BackToMainMenuOrAbandon() {
		/*if (PlayStateMaster.s.isCombatInProgress()) {
			Pauser.s.AbandonMission();
		} else {*/
			Pauser.s.Unpause();
			BackToMainMenu();
		//}
	}
	
	public void BackToMainMenu() {
		starterUI.SetActive(false);
		PlayStateMaster.s.OpenMainMenu();

		// MusicPlayer.s.SwapMusicTracksAndPlay(false);
		FMODMusicPlayer.s.SwapMusicTracksAndPlay(false);
	}

	public void OpenShopUI() {
		if(PlayStateMaster.s.isCombatInProgress())
			return;
		
		starterUI.SetActive(true);
		RangeVisualizer.SetAllRangeVisualiserState(false);

		mapOpenButton.interactable = true;
		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.openMap);
		//mapDisabledDuringBattleOverlay.SetActive(false);
		
		CameraController.s.ResetCameraPos();
		
		if (DataSaver.s.GetCurrentSave().currentRun.isInEndRunArea) {
			starterUI.SetActive(false);
			MissionWinFinisher.s.ShowUnclaimedRewards();
			HexGrid.s.CreateEndAreaChunk();

		} else {
			UpgradesController.s.DrawShopOptions();
			UpdateBackToProfileOrAbandonButton();
		}
	}

	public void SetStarterUIStatus(bool status) {
		starterUI.SetActive(status);
	}

	public GateScript gateScript;
	public Tooltip selectDestinationTooltip;
	public Tooltip pickUpWorldCartTooltip;
	public Tooltip fillFleaMarketTooltip;
	public Tooltip allGoodToGoTooltip;
	public void SetGoingLeft() {
		gateScript.SetCanGoStatus(true, allGoodToGoTooltip);
		
		var playerStar = DataSaver.s.GetCurrentSave().currentRun.map.GetPlayerStar();
		var targetStar = DataSaver.s.GetCurrentSave().currentRun.map.GetStarWithName(playerStar.outgoingConnections[0]);
		SelectLevel(targetStar);
	}

	public void SetGoingRight() {
		gateScript.SetCanGoStatus(true, allGoodToGoTooltip);

		var playerStar = DataSaver.s.GetCurrentSave().currentRun.map.GetPlayerStar();
		var targetStar = DataSaver.s.GetCurrentSave().currentRun.map.GetStarWithName(playerStar.outgoingConnections[1]);
		SelectLevel(targetStar);
	}

	public void SetCannotGo(CanStartLevelStatus status) {
		Tooltip tooltip;

		switch (status) {
			case CanStartLevelStatus.needToSelectDestination:
				tooltip = selectDestinationTooltip;
				break;
			case CanStartLevelStatus.needToPickUpFreeCarts:
				tooltip = pickUpWorldCartTooltip;
				break;
			case CanStartLevelStatus.needToPutThingInFleaMarket:
				tooltip = fillFleaMarketTooltip;
				break;
			default:
				tooltip = null;
				break;
		}
		
		gateScript.SetCanGoStatus(false, tooltip);
	}

	public void StartLevel() {
		StartLevel(true);
	}

	public void StartLevel(bool legitStart) {
		if (PlayStateMaster.s.IsLevelSelected()) {
			var currentLevel = PlayStateMaster.s.currentLevel;
			starterUI.SetActive(false);

			mapOpenButton.interactable = false;
			GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.openMap);
			//mapDisabledDuringBattleOverlay.SetActive(true);

			ClearStaticTrackers();

			gameUI.SetActive(true);
			
			UpdateBackToProfileOrAbandonButton();

			if (legitStart) {
				PlayStateMaster.s.StarCombat();
				
				RangeVisualizer.SetAllRangeVisualiserState(true);

				SoundscapeController.s.PlayMissionStartSound();
				
				if(currentLevel.isBossLevel)
					MiniGUI_BossNameUI.s.ShowBossName(currentLevel.levelNiceName);
			} 
			/*} else {
				SceneLoader.s.FinishLevel();
				EncounterController.s.EngageEncounter(currentLevel);
			}*/ // stuf to do during encounters
		}
	}

	/*public void DebugEngageEncounter(string encounterName) {
		var playerStar = DataSaver.s.GetCurrentSave().currentRun.map.GetPlayerStar();
		starterUI.SetActive(false);

		mapOpenButton.interactable = false;
		//mapDisabledDuringBattleOverlay.SetActive(true);

		PlayStateMaster.s.FinishCombat();
		EncounterController.s.EngageEncounter(encounterName);
	}*/

	void ClearStaticTrackers() {
		EnemyHealth.enemySpawned = 0;
		EnemyHealth.enemyKilled = 0;
		//PlayerBuildingController.s.currentLevelStats = new Dictionary<string, PlayerBuildingController.BuildingData>();
	}

	public void SelectLevelAndStart(StarState targetStar) {
		SelectLevel(targetStar);
		StartLevel();
	}

	public void SelectLevel(StarState targetStar) {
		DataSaver.s.GetCurrentSave().currentRun.targetStar = targetStar.starName;
		var playerStar = DataSaver.s.GetCurrentSave().currentRun.map.GetPlayerStar();

		for (int i = 0; i < playerStar.outgoingConnections.Count; i++) {
			if (playerStar.outgoingConnections[i] == targetStar.starName) {
				selectedLevelIndex = i;
				break;
			}
		}

		if (selectedLevelIndex == -1) {
			Debug.LogError($"Illegal star target: {targetStar.starName}");
			return;
		}

		var level = playerStar.outgoingConnectionLevels[selectedLevelIndex];
		PlayStateMaster.s.SetCurrentLevel(level);
	}

	public void SelectLevelAndStart_StarterUIStartOnly(ConstructedLevel data) {
		PlayStateMaster.s.SetCurrentLevel(data);

		starterUI.SetActive(false);

		ClearStaticTrackers();

		gameUI.SetActive(true);
		PlayStateMaster.s.StarCombat();

		UpdateBackToProfileOrAbandonButton();
		
		RangeVisualizer.SetAllRangeVisualiserState(true);

		SoundscapeController.s.PlayMissionStartSound();

		// MusicPlayer.s.SwapMusicTracksAndPlay(true);
		FMODMusicPlayer.s.SwapMusicTracksAndPlay(true);
		
		StartLevel();
	}

	public void FinishTravellingToStar() {
		var currentRun = DataSaver.s.GetCurrentSave().currentRun;
		MapController.s.TravelToStar(currentRun.map.GetStarWithName(currentRun.targetStar));
	}

	public void QuickStart() {
		if (DataSaver.s.GetCurrentSave().isInARun) {
			var playerStar = DataSaver.s.GetCurrentSave().currentRun.map.GetPlayerStar();
			var targetStar = DataSaver.s.GetCurrentSave().currentRun.map.GetStarWithName(playerStar.outgoingConnections[0]);
			SelectLevelAndStart(targetStar);
		}
	}
}
