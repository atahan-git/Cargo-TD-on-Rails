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
		
		backToProfileOrAbandonText.text = "Back to Main Menu";
	}
	public void BackToMainMenuOrAbandon() {
		Pauser.s.Unpause();
		BackToMainMenu();
	}
	
	public void BackToMainMenu() {
		starterUI.SetActive(false);
		DataSaver.s.GetCurrentSave().instantRestart = false;
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
		
		CameraController.s.ResetCameraPos();

		PathSelectorController.s.trainStationStart.SetActive(true);
		DrawShopOptions();
		UpdateBackToProfileOrAbandonButton();
	}

	
	public GateScript gateScript;

	public void StartLevel() {
		StartLevel(true);
	}

	public void StartLevel(bool legitStart) {
		PlayStateMaster.s.SetCurrentLevel(DataHolder.s.levelArchetypeScriptables[DataSaver.s.GetCurrentSave().currentRun.currentAct-1].GenerateLevel());
		MapController.s.MakeNewMap();
		if (PlayStateMaster.s.IsLevelSelected()) {
			var currentLevel = PlayStateMaster.s.currentLevel;
			starterUI.SetActive(false);

			mapOpenButton.interactable = false;
			GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.openMap);
			//mapDisabledDuringBattleOverlay.SetActive(true);

			gameUI.SetActive(true);
			
			UpdateBackToProfileOrAbandonButton();

			if (legitStart) {
				PlayStateMaster.s.StarCombat();
				
				RangeVisualizer.SetAllRangeVisualiserState(true);

				SoundscapeController.s.PlayMissionStartSound();
				
				/*if(currentLevel.isBossLevel)
					MiniGUI_BossNameUI.s.ShowBossName(currentLevel.levelNiceName);*/
			} 
			
		}
	}

	public void QuickStart() {
		StartLevel();
	}

	public List<Artifact> shopArtifacts;
	public List<Cart> shopCarts;
	
	public MiniGUI_DepartureChecklist shopChecklist;
	public MiniGUI_DepartureChecklist endGameAreaChecklist;

	public void RemoveCartFromShop(Cart cart) {
		if(PlayStateMaster.s.isCombatInProgress())
			return;
		shopCarts.Remove(cart);
		SaveShopState();
	}

	public void AddCartToShop(Cart cart, bool doSave = true) {
		if(PlayStateMaster.s.isCombatInProgress())
			return;
		
		if (!shopCarts.Contains(cart)) {
			shopCarts.Add(cart);
			if(doSave)
				SaveShopState();
		}
	}

	public void RemoveArtifactFromShop(Artifact artifact, bool doSave = true) {
		if(PlayStateMaster.s.isCombatInProgress())
			return;
		if (shopArtifacts.Contains(artifact)) {
			shopArtifacts.Remove(artifact);
			if(doSave)
				SaveShopState();
		}
	}

	public void AddArtifactToShop(Artifact artifact, bool doSave = true) {
		if(PlayStateMaster.s.isCombatInProgress())
			return;
		if (!shopArtifacts.Contains(artifact)) {
			shopArtifacts.Add(artifact);
			if(doSave)
				SaveShopState();
		}
	}

	public void SaveCartStateWithDelay() {
		SaveShopState();
		CancelInvoke(nameof(SaveShopState));
		Invoke(nameof(SaveShopState), 2f);
	}

	public void SaveShopState() {
		var shopState = new ShopState();
		for (int i = 0; i < shopCarts.Count; i++) {
			var cart = shopCarts[i];
			shopState.cartStates.Add(new WorldCartState() {
				isSnapped =  cart.GetComponentInParent<SnapLocation>(),
				pos = cart.transform.position,
				rot = cart.transform.rotation,
				state = Train.GetStateFromCart(cart)
			});
		}

		for (int i = 0; i < shopArtifacts.Count; i++) {
			var myArtifact = shopArtifacts[i];

			if (myArtifact != null) {
				shopState.artifactStates.Add(new WorldArtifactState() {
					isSnapped =  myArtifact.GetComponentInParent<SnapLocation>(),
					pos = myArtifact.transform.position,
					rot = myArtifact.transform.rotation,
					state = Train.GetStateFromArtifact(myArtifact),
				});
			}
		}

		DataSaver.s.GetCurrentSave().currentRun.shopState = shopState;
		DataSaver.s.SaveActiveGame();
	}
	

	[System.Serializable]
	public class ShopState {
		public List<WorldArtifactState> artifactStates = new List<WorldArtifactState>();
		public List<WorldCartState> cartStates = new List<WorldCartState>();
	}
	
	[Serializable]
	public class WorldArtifactState {
		public bool isSnapped;
		public Vector3 pos;
		public Quaternion rot;
		public DataSaver.TrainState.ArtifactState state = new DataSaver.TrainState.ArtifactState();
	}
	
	[Serializable]
	public class WorldCartState {
		public bool isSnapped;
		public Vector3 pos;
		public Quaternion rot;
		public DataSaver.TrainState.CartState state = new DataSaver.TrainState.CartState();
	}

	void InitializeShop(DataSaver.SaveFile state) {
		state.currentRun.shopState = new ShopState();
		
		state.currentRun.shopInitialized = true;
		DataSaver.s.SaveActiveGame();
	}

	public void DrawShopOptions() {
		if (!DataSaver.s.GetCurrentSave().currentRun.shopInitialized) {
			InitializeShop(DataSaver.s.GetCurrentSave());
		}
		
		SpawnShopItems();
	}

	public void ClearCurrentShop() {
		transform.DeleteAllChildren();
		for (int i = shopCarts.Count-1; i >= 0; i--) {
			if(shopCarts[i] != null && shopCarts[i].gameObject != null)
				Destroy(shopCarts[i].gameObject);
		}
		shopCarts.Clear();
		
		
		for (int i = shopArtifacts.Count-1; i >= 0; i--) {
			if(shopArtifacts[i] != null && shopArtifacts[i].gameObject != null)
				Destroy(shopArtifacts[i].gameObject);
		}
		shopArtifacts.Clear();
		
		DataSaver.s.GetCurrentSave().currentRun.shopState = new ShopState();
		DataSaver.s.SaveActiveGame();
	}

	
	void SpawnShopItems() {
		transform.DeleteAllChildren();
		for (int i = shopCarts.Count-1; i >= 0; i--) {
			if(shopCarts[i] != null && shopCarts[i].gameObject != null)
				Destroy(shopCarts[i].gameObject);
		}
		shopCarts.Clear();
		
		for (int i = shopArtifacts.Count-1; i >= 0; i--) {
			if(shopArtifacts[i] != null && shopArtifacts[i].gameObject != null)
				Destroy(shopArtifacts[i].gameObject);
		}
		shopArtifacts.Clear();


		if (DataSaver.s.GetCurrentSave().currentRun.shopInitialized) {
			var prevShopState = DataSaver.s.GetCurrentSave().currentRun.shopState;

			for (int i = 0; i <prevShopState.cartStates.Count; i++) {
				var curState = prevShopState.cartStates[i];
				var cart = Instantiate(DataHolder.s.GetCart(curState.state.uniqueName), curState.pos, curState.rot);
				Train.ApplyStateToCart(cart, curState.state);
				AddCartToShop(cart, false);
				if (curState.isSnapped) {
					var snapLoc =  PlayerWorldInteractionController.s.GetSnapLoc(curState.pos, cart.GetComponent<Cart>());
					if (snapLoc != null) {
						snapLoc.SnapObject(cart.gameObject);
					}
				}
			}
			
			for (int i = 0; i <prevShopState.artifactStates.Count; i++) {
				var curState = prevShopState.artifactStates[i];
				var artifact = Instantiate(DataHolder.s.GetArtifact(curState.state.uniqueName), curState.pos, curState.rot);
				Train.ApplyStateToArtifact(artifact, curState.state);
				AddArtifactToShop(artifact, false);
				if (curState.isSnapped) {
					var snapLoc =  PlayerWorldInteractionController.s.GetSnapLoc(curState.pos, artifact.GetComponent<Artifact>());
					if (snapLoc != null) {
						snapLoc.SnapObject(artifact.gameObject);
					}
				}
			}
		}

		SaveShopState();
	}
	
	public void OnCombatStart() {
		for (int i = 0; i < shopCarts.Count; i++) {
			if (shopCarts[i].GetComponentInParent<SnapLocation>() == null) {
				shopCarts[i].gameObject.AddComponent<RubbleFollowFloor>().InstantAttachToFloor();
			}
		}

		for (int i = 0; i < shopArtifacts.Count; i++) {
			if (shopArtifacts[i].GetComponentInParent<SnapLocation>() == null) {
				shopArtifacts[i].gameObject.AddComponent<RubbleFollowFloor>().InstantAttachToFloor();
			}
		}
	}
}
