using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ShopStateController : MonoBehaviour {
	public static ShopStateController s;
	private void Awake() { s = this; }


	public GameObject starterUI;
	public GameObject gameUI;

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
		PlayerWorldInteractionController.s.canSelect = true;
		if (DataSaver.s.GetCurrentSave().showWakeUp) {
			WakeUpAnimation.s.Engage();
			DataSaver.s.GetCurrentSave().showWakeUp = false;
		}
		
		
		if(PlayStateMaster.s.isCombatInProgress())
			return;
		
		starterUI.SetActive(true);
		RangeVisualizer.SetAllRangeVisualiserState(false);

		mapOpenButton.interactable = true;
		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.openMap);
		//mapDisabledDuringBattleOverlay.SetActive(false);
		
		CameraController.s.ResetCameraPos();
		
		if (DataSaver.s.GetCurrentSave().isInEndRunArea) {
			starterUI.SetActive(false);
			MissionWinFinisher.s.ShowUnclaimedRewards();
			//HexGrid.s.CreateEndAreaChunk();

		} else {
			PathSelectorController.s.trainStationStart.SetActive(true);
			UpgradesController.s.DrawShopOptions();
			UpdateBackToProfileOrAbandonButton();
		}
	}

	public void SetStarterUIStatus(bool status) {
		starterUI.SetActive(status);
	}

	public GateScript gateScript;

	public void StartLevel() {
		StartLevel(true);
	}

	public void StartLevel(bool legitStart) {
		PlayStateMaster.s.SetCurrentLevel(DataHolder.s.levelArchetypeScriptables[Random.Range(0,DataHolder.s.levelArchetypeScriptables.Length)].GenerateLevel());
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
				
				/*if(currentLevel.isBossLevel)
					MiniGUI_BossNameUI.s.ShowBossName(currentLevel.levelNiceName);*/
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
	
	public void QuickStart() {
		StartLevel();
	}
}
