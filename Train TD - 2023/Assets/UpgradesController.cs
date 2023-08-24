using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UpgradesController : MonoBehaviour {
	public static UpgradesController s;
	private void Awake() {
		s = this;
	}
	
	
	public enum CartRarity {
		common, rare, epic, boss, special
	}

	[ValueDropdown("GetAllModuleNames")]
	public List<string> tier1Buildings = new List<string>();
	[ValueDropdown("GetAllModuleNames")]
	public List<string> firstShopCarts = new List<string>();

	public List<string> regularArtifacts = new List<string>();
	public List<string> bossArtifacts = new List<string>();

	public SnapCartLocation[] fleaMarketLocations;
	public Transform shopableComponentsParent;
	public GameObject buildingCargo;

	public SnapCartLocation leftCargoParent;
	public SnapCartLocation rightCargoParent;

	public Transform leftRewardPos;
	public Transform rightRewardPos;

	public List<Artifact> shopArtifacts;
	public List<Cart> shopCarts;
	public Cart leftCargo;
	public Cart rightCargo;

	public bool shopArea_destinationSelected = false;
	public bool shopArea_fleaMarketFull = false;
	//public bool shopArea_noFreeCarts = false;

	public bool endGame_noCargoOnTrain;
	public bool endGame_noFreeCarts;
	
	public MiniGUI_DepartureChecklist shopChecklist;
	public MiniGUI_DepartureChecklist endGameAreaChecklist;

	private bool hasArtifactsGeneratedOnce = false;
	public void OnShopOpened() {
		bossArtifacts.Clear();
		regularArtifacts.Clear();
		
		var allArtifacts = DataHolder.s.artifacts;
		var myArtifacts = new List<Artifact>(ArtifactsController.s.myArtifacts);
		for (int i = 0; i < allArtifacts.Length; i++) {
			if (allArtifacts[i].myRarity == CartRarity.boss) {
				if (myArtifacts.FindIndex(a => a.uniqueName == allArtifacts[i].uniqueName) == -1) { // only add the artifact if we dont already have it
					bossArtifacts.Add(allArtifacts[i].uniqueName);
				}
			} else if (allArtifacts[i].myRarity != CartRarity.special) {
				if (allArtifacts[i].isGenericArtifact || myArtifacts.FindIndex(a => a.uniqueName == allArtifacts[i].uniqueName) == -1) {
					regularArtifacts.Add(allArtifacts[i].uniqueName);
				}
			}
		}
		
		recentArtifacts.Clear();
		recentBuildings.Clear();

		hasArtifactsGeneratedOnce = true;
	}
	
	//public GameObject cargoLyingAroundParticles;
	public float luck => DataSaver.s.GetCurrentSave().currentRun.luck;

	public void RemoveCartFromShop(Cart cart) {
		shopCarts.Remove(cart);
		SaveShopState();
	}

	public void AddCartToShop(Cart cart, CartLocation location, bool doSave = true) {
		cart.myLocation = location;
		shopCarts.Add(cart);
		if(doSave)
			SaveShopState();
	}

	public void ChangeCartLocation(Cart cart, CartLocation location) {
		cart.myLocation = location;
		SaveShopState();
	}
	
	
	public void RemoveArtifactFromShop(Artifact artifact, bool doSave = true) {
		if (shopArtifacts.Contains(artifact)) {
			shopArtifacts.Remove(artifact);
			if(doSave)
				SaveShopState();
		}
	}

	public void AddArtifactToShop(Artifact artifact, CartLocation location, bool doSave = true) {
		if (!shopArtifacts.Contains(artifact)) {
			shopArtifacts.Add(artifact);
			if(doSave)
				SaveShopState();
		}
	}
	
	public void ChangeArtifactLocation(Artifact artifact, CartLocation location) {
		if (artifact.myLocation != location) {
			artifact.myLocation = location;
			SaveShopState();
		}
	}

	public void SaveCartStateWithDelay() {
		CancelInvoke(nameof(SaveShopState));
		Invoke(nameof(SaveShopState), 2f);
	}

	public SnapCartLocation[] cargoSwapLocations;
	
	public void UpdateCargoHighlights() {
		var cargoCount = CheckIfCanGo();

		if (cargoCount > 0) {
			for (int i = 0; i < cargoSwapLocations.Length; i++) {
				cargoSwapLocations[i].SetEmptyStatus(cargoSwapLocations[i].IsEmpty());
			}
		} else {
			for (int i = 0; i < cargoSwapLocations.Length; i++) {
				cargoSwapLocations[i].SetEmptyStatus(false);
			}
		}
	}

	public void UpdateCartShopHighlights() {
		for (int i = 0; i < fleaMarketLocations.Length; i++) {
			fleaMarketLocations[i].SetEmptyStatus(fleaMarketLocations[i].IsEmpty());
			//_cargoLocations[i].SetEmptyStatus(isLocationEmpty);
		}

		CheckIfCanGo();
	}
	
	public void SnapDestinationCargos(Cart lastCart) {
		var directionSet = false;
		if (leftCargo.myLocation == CartLocation.train && rightCargo.myLocation == CartLocation.train) {
			if (leftCargo == lastCart) {
				PutCargoBackInDestinationSelect(rightCargo, false);
			}

			if (rightCargo == lastCart) {
				PutCargoBackInDestinationSelect(leftCargo, true);
			}
		}

		if (leftCargo.myLocation == CartLocation.train && rightCargo.myLocation != CartLocation.train) {
			directionSet = true;
			ShopStateController.s.SetGoingLeft();

			PutCargoBackInDestinationSelect(rightCargo, false);
		}
		
		if (rightCargo.myLocation == CartLocation.train && leftCargo.myLocation != CartLocation.train) {
			directionSet = true;
			ShopStateController.s.SetGoingRight();

			PutCargoBackInDestinationSelect(leftCargo, true);
		}

		if (!directionSet) {
			PutCargoBackInDestinationSelect(leftCargo, true);
			PutCargoBackInDestinationSelect(rightCargo, false);
		}
		
		CheckIfCanGo();
	}

	void PutCargoBackInDestinationSelect(Cart cart, bool isLeft) {
		var targetParent = isLeft ? leftCargoParent.snapTransform : rightCargoParent.snapTransform;

		if (cart.myLocation == CartLocation.train) {
			Train.s.RemoveCart(cart);
			shopCarts.Add(cart);
		}
		
		cart.myLocation = CartLocation.destinationSelect;
		cart.transform.SetParent(targetParent);

		var shopState = DataSaver.s.GetCurrentSave().currentRun.shopState;
		var cargo = cart.GetComponentInChildren<CargoModule>();
		var cargoState =  DataSaver.TrainState.CartState.CargoState.GetStateFromModule(cargo);
		
		if (isLeft) {
			shopState.leftCargo = cargoState;
		} else {
			shopState.rightCargo = cargoState;
		}

		cart.GetComponent<Rigidbody>().isKinematic = true;
		cart.GetComponent<Rigidbody>().useGravity = false;

		SaveShopState();
		Train.s.SaveTrainState();
	}


	public float destinationCargoLerpSpeed = 5f;
	public float destinationCargoSlerpSpeed = 20f;
	private void Update() {
		if (rightCargo != null) {
			if (rightCargo.myLocation == CartLocation.destinationSelect) {
				rightCargo.transform.localPosition = Vector3.Lerp(rightCargo.transform.localPosition, Vector3.zero, destinationCargoLerpSpeed * Time.deltaTime);
				rightCargo.transform.localRotation = Quaternion.Slerp(rightCargo.transform.localRotation, Quaternion.identity, destinationCargoSlerpSpeed * Time.deltaTime);
			}
		}

		if (leftCargo != null) {
			if (leftCargo.myLocation == CartLocation.destinationSelect) {
				leftCargo.transform.localPosition = Vector3.Lerp(leftCargo.transform.localPosition, Vector3.zero, destinationCargoLerpSpeed * Time.deltaTime);
				leftCargo.transform.localRotation = Quaternion.Slerp(leftCargo.transform.localRotation, Quaternion.identity, destinationCargoSlerpSpeed * Time.deltaTime);
			}
		}
	}

	
	public void SaveShopState() {
		var shopState = new ShopState();
		for (int i = 0; i < shopCarts.Count; i++) {
			var cart = shopCarts[i];
			if (cart.isCargo && !PlayStateMaster.s.isEndGame()) { // in the end game area cargos are also regular carts
				var cargo = cart.GetComponentInChildren<CargoModule>();
				if (cargo.GetState().isLeftCargo) { 
					shopState.leftCargo =  DataSaver.TrainState.CartState.CargoState.GetStateFromModule(cargo);
				} else { 
					shopState.rightCargo =  DataSaver.TrainState.CartState.CargoState.GetStateFromModule(cargo);
				}
			} else {
				shopState.cartStates.Add(new WorldCartState() {
					location = cart.myLocation,
					pos = cart.transform.position,
					rot = cart.transform.rotation,
					state = Train.GetStateFromCart(cart)
				});
			}
		}

		for (int i = 0; i < shopArtifacts.Count; i++) {
			var myArtifact = shopArtifacts[i];

			if (myArtifact != null) {
				shopState.artifactStates.Add(new WorldArtifactState() {
					location =  myArtifact.myLocation,
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
		public DataSaver.TrainState.CartState.CargoState leftCargo;
		public DataSaver.TrainState.CartState.CargoState rightCargo;
	}
	
	[Serializable]
	public class WorldArtifactState {
		public CartLocation location;
		public Vector3 pos;
		public Quaternion rot;
		public DataSaver.TrainState.ArtifactState state = new DataSaver.TrainState.ArtifactState();
	}
	
	[Serializable]
	public class WorldCartState {
		public CartLocation location;
		public Vector3 pos;
		public Quaternion rot;
		public DataSaver.TrainState.CartState state = new DataSaver.TrainState.CartState();
	}

	[Serializable]
	public enum CartLocation {
		train = 0, market = 1, world = 2, forge = 3, destinationSelect = 4, cargoDelivery = 5, rewardDisplay = 6
	}

	public Cart secondCityForcedRepairDrop;
	void InitializeShop(DataSaver.RunState state) {
		state.shopState = new ShopState();
		var buildingCargoCount = fleaMarketLocationCount;
		var artifactCount = 0;


		if (DataSaver.s.GetCurrentSave().xpProgress.xp > 2) {
			var artifactRoll = Random.value;
			if (artifactRoll < 0.2f) {
				// 2 artifacts
				buildingCargoCount -= 2;
				artifactCount += 2;
			} else if (artifactRoll < 0.5f) {
				// 1 artifacts
				buildingCargoCount -= 1;
				artifactCount += 1;
			}
		}

		var didRollEpic = false;
		var rollName = "";
		var rollLevel = 0;
		var artifactRollName = "";
		var artifactRollLevel = 0;

		for (int i = 0; i < buildingCargoCount; i++) {
			didRollEpic = false;
			rollName = GetRandomBuildingCargoForFleaMarket(ref didRollEpic);
			rollLevel = GetFleaMarketLevel(didRollEpic); 
			
			state.shopState.cartStates.Add(new WorldCartState() {
				location =  CartLocation.market,
				state = new DataSaver.TrainState.CartState() {
					uniqueName = rollName,
					level = rollLevel 
				}
			});
		}

		for (int i = 0; i < artifactCount; i++) {
			didRollEpic = false;
			artifactRollName = GetRandomFleaMarketArtifact(ref didRollEpic);
			artifactRollLevel = GetRandomFleaMarketArtifactLevel(didRollEpic); 
			
			state.shopState.artifactStates.Add(new WorldArtifactState() {
				location =  CartLocation.market,
				state =  new DataSaver.TrainState.ArtifactState() {
					uniqueName =  artifactRollName,
					level = artifactRollLevel
				}
			});
		}

		if (state.map.GetPlayerStar().starChunk == 1) {
			state.shopState.cartStates[0].state.uniqueName = secondCityForcedRepairDrop.uniqueName;
		}
		
		
		rollName = GetRandomBuildingCargoForDestinationReward(ref didRollEpic);
		rollLevel = GetDestinationCargoLevel(didRollEpic);
		artifactRollName = GetRandomDestinationArtifact(ref didRollEpic);
		artifactRollLevel = GetRandomFleaMarketArtifactLevel(didRollEpic);

		state.shopState.leftCargo = new DataSaver.TrainState.CartState.CargoState(
			rollName,
			artifactRollName,
			true,
			rollLevel,
			artifactRollLevel
		);
		
		rollName = GetRandomBuildingCargoForDestinationReward(ref didRollEpic);
		rollLevel = GetDestinationCargoLevel(didRollEpic); 
		artifactRollName = GetRandomDestinationArtifact(ref didRollEpic);
		artifactRollLevel = GetRandomFleaMarketArtifactLevel(didRollEpic);
		
		state.shopState.rightCargo = new DataSaver.TrainState.CartState.CargoState(
			rollName,
			artifactRollName,
			false,
			rollLevel,
			artifactRollLevel
		);

		state.shopInitialized = true;
		DataSaver.s.SaveActiveGame();
	}

	public void DrawShopOptions() {
		var currentRun = DataSaver.s.GetCurrentSave().currentRun;
		if (!currentRun.shopInitialized) {
			InitializeShop(currentRun);
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
		
		var currentRun = DataSaver.s.GetCurrentSave().currentRun;
		currentRun.shopState = new ShopState();
		DataSaver.s.SaveActiveGame();
	}

	public int fleaMarketLocationCount = 3;
	public bool rewardDestinationArtifact = true;
	public bool rewardDestinationCart = true;

	public void ResetFleaMarketAndDestCargoValues() {
		fleaMarketLocationCount = 3;
		rewardDestinationArtifact = true;
		rewardDestinationCart = true;
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
		
		
		var currentRun = DataSaver.s.GetCurrentSave().currentRun;
		
		for (int i = 0; i < fleaMarketLocations.Length; i++) {
			var buildings = fleaMarketLocations[i].GetComponentsInChildren<Cart>();
			for (int j = 0; j < buildings.Length; j++) {
				Destroy(buildings[j].gameObject);
			}
			
			fleaMarketLocations[i].transform.rotation = Quaternion.Euler(0,Random.Range(0,360),0);

			if (i >= fleaMarketLocationCount) {
				fleaMarketLocations[i].gameObject.SetActive(false);
			}
		}


		var fleaMarketLocationIndex = 0;
		if (currentRun.isInEndRunArea) {
			for (int i = 0; i < currentRun.shopState.cartStates.Count; i++) { // we don't properly load from carts in cargo delivery thingy
				var cart = currentRun.shopState.cartStates[i];

				var thingy = Instantiate(DataHolder.s.GetCart(cart.state.uniqueName).gameObject, shopableComponentsParent);
				Train.ApplyStateToCart(thingy.GetComponent<Cart>(), cart.state);
				AddCartToShop(thingy.GetComponent<Cart>(), CartLocation.world, false);
				thingy.transform.position = cart.pos;
				thingy.transform.rotation = cart.rot;
				thingy.GetComponent<Rigidbody>().isKinematic = false;
				thingy.GetComponent<Rigidbody>().useGravity = true;
			}
			
		} else {
			for (int i = 0; i < currentRun.shopState.cartStates.Count; i++) {
				var cart = currentRun.shopState.cartStates[i];
				
				var myCartInShop = Instantiate(DataHolder.s.GetCart(cart.state.uniqueName).gameObject, shopableComponentsParent);
				Train.ApplyStateToCart(myCartInShop.GetComponent<Cart>(), cart.state);
				if (cart.location == CartLocation.market) { // If a cart is anywhere but in market it might as well be in the world, eg the smithery
					var location = fleaMarketLocations[fleaMarketLocationIndex]; // we don't care about the previous flea market order
					fleaMarketLocationIndex++;
					
					location.SnapToLocation(myCartInShop);
					AddCartToShop(myCartInShop.GetComponent<Cart>(), CartLocation.market, false);
				} else {
					AddCartToShop(myCartInShop.GetComponent<Cart>(), CartLocation.world, false);
					myCartInShop.transform.position = cart.pos;
					myCartInShop.transform.rotation = cart.rot;
					myCartInShop.GetComponent<Rigidbody>().isKinematic = false;
					myCartInShop.GetComponent<Rigidbody>().useGravity = true;
				}
				
			}


			if (currentRun.shopState.leftCargo != null && currentRun.shopState.leftCargo.cargoReward != null && currentRun.shopState.leftCargo.cargoReward.Length > 0) {
				leftCargo = SpawnCargo(currentRun.shopState.leftCargo, leftCargoParent);
			} else {
				var carts = Train.s.carts;
				for (int i = 0; i < carts.Count; i++) {
					if (carts[i].isCargo) {
						var cargo = carts[i].GetComponentInChildren<CargoModule>();
						if (cargo.GetState().isLeftCargo) {
							leftCargo = carts[i];
							ShopStateController.s.SetGoingLeft();
							break;
						}
					}
				}
			}

			if (currentRun.shopState.rightCargo != null && currentRun.shopState.rightCargo.cargoReward != null && currentRun.shopState.rightCargo.cargoReward.Length > 0) {
				rightCargo = SpawnCargo(currentRun.shopState.rightCargo, rightCargoParent);
			} else {
				var carts = Train.s.carts;
				for (int i = 0; i < carts.Count; i++) {
					if (carts[i].isCargo) {
						var cargo = carts[i].GetComponentInChildren<CargoModule>();
						if (!cargo.GetState().isLeftCargo) {
							rightCargo = carts[i];
							ShopStateController.s.SetGoingRight();
							break;
						}
					}
				}
			}

			
			SpawnRewardAtPos(leftRewardPos, leftCargo.GetComponentInChildren<CargoModule>());
			SpawnRewardAtPos(rightRewardPos, rightCargo.GetComponentInChildren<CargoModule>());
		}


		for (int i = 0; i < currentRun.shopState.artifactStates.Count; i++) {
			var myArtifactState = currentRun.shopState.artifactStates[i];
			var artifact = Instantiate(DataHolder.s.GetArtifact(myArtifactState.state.uniqueName).gameObject, myArtifactState.pos, myArtifactState.rot).GetComponent<Artifact>();
			Train.ApplyStateToArtifact(artifact, myArtifactState.state);
			if (myArtifactState.location == CartLocation.market) {
				var location = fleaMarketLocations[fleaMarketLocationIndex]; // we don't care about the previous flea market order
				fleaMarketLocationIndex++;
				artifact.AttachToSnapLoc(location,false);
			} else {
				artifact.DetachFromCart(false);
			}
		}

		CheckIfCanGo();
		SaveShopState();
	}

	[ColorUsage(true, true)] 
	public Color rewardOverlayColor = Color.cyan;

	void SpawnRewardAtPos(Transform pos, CargoModule module) {
		pos.DeleteAllChildren();

		var rewardCart = Instantiate(DataHolder.s.GetCart(module.GetState().cargoReward).gameObject, pos).GetComponent<Cart>();

		rewardCart.SetUpOverlays();
		rewardCart.canPlayerDrag = false;
		rewardCart.myLocation = CartLocation.rewardDisplay;
		rewardCart.level = module.GetState().cargoLevel;
		rewardCart.ResetState();

		var renderers = rewardCart.GetComponentsInChildren<MeshRenderer>();

		for (int i = 0; i < renderers.Length; i++) {
			renderers[i].sharedMaterials[1].SetColor("OverlayColor", rewardOverlayColor);
		}

	}

	int CheckIfCanGo() {
		if (DataSaver.s.GetCurrentSave().currentRun.isInEndRunArea) {
			var cargoCount = 0;
			var onlyRegularCargoCount = 0;
			var looseCartCount = shopCarts.Count;
			for (int i = 0; i < shopCarts.Count; i++) {
				var cart = shopCarts[i];
				if (cart.isCargo) {
					onlyRegularCargoCount += 1;
				}
				
				if (cart.isCargo || (MissionWinFinisher.s.needToDeliverMysteriousCargo && cart.isMysteriousCart)) {
					cargoCount += 1;
					cart.GetComponentInChildren<CargoModule>()?.HighlightForDelivery();
					cart.GetComponentInChildren<MysteriousCargoModule>()?.HighlightForDelivery();
				}
			}

			for (int i = 0; i < Train.s.carts.Count; i++) {
				var cart = Train.s.carts[i];
				if (cart.isCargo) {
					onlyRegularCargoCount += 1;
				}
				
				if (cart.isCargo || (MissionWinFinisher.s.needToDeliverMysteriousCargo && cart.isMysteriousCart)) {
					cargoCount += 1;
					cart.GetComponentInChildren<CargoModule>()?.HighlightForDelivery();
					cart.GetComponentInChildren<MysteriousCargoModule>()?.HighlightForDelivery();
				}
			}

			if (looseCartCount > 0 || cargoCount > 0) {
				MissionWinFinisher.s.SetCannotGo(cargoCount > 0);
			} else {
				MissionWinFinisher.s.SetCanGo();
			}
			
			
			endGame_noFreeCarts = looseCartCount == 0;
			endGame_noCargoOnTrain = cargoCount == 0;
			endGameAreaChecklist.UpdateStatus(new []{endGame_noCargoOnTrain, endGame_noFreeCarts});
			
			ShopStateController.s.SetCannotGo(ShopStateController.CanStartLevelStatus.needToSelectDestination);

			return onlyRegularCargoCount;

		} else {
			var fleaMarketCount = 0;
			for (int i = 0; i < fleaMarketLocations.Length; i++) {
				if (fleaMarketLocations[i].snapTransform.childCount > 0) {
					fleaMarketCount += 1;
				}
			}
			shopArea_fleaMarketFull = fleaMarketCount >= fleaMarketLocationCount;
			
			if (leftCargo.myLocation == CartLocation.train) {
				shopArea_destinationSelected = true;
				ShopStateController.s.SetGoingLeft();
			}

			if (rightCargo.myLocation == CartLocation.train) {
				shopArea_destinationSelected = true;
				ShopStateController.s.SetGoingRight();
			}


			if (!shopArea_fleaMarketFull) {
				ShopStateController.s.SetCannotGo(ShopStateController.CanStartLevelStatus.needToPutThingInFleaMarket);
			}

			/*shopArea_noFreeCarts = true;
			for (int i = 0; i < shopCarts.Count; i++) {
				if (shopCarts[i].myLocation == CartLocation.world || shopCarts[i].myLocation == CartLocation.forge) {
					//Instantiate(cargoLyingAroundParticles, shopCarts[i].genericParticlesParent);
					shopArea_noFreeCarts = false;
					ShopStateController.s.SetCannotGo(ShopStateController.CanStartLevelStatus.needToPickUpFreeCarts);
					break;
				}
			}*/
			
			if (leftCargo.myLocation != CartLocation.train && rightCargo.myLocation != CartLocation.train) {
				shopArea_destinationSelected = false;
				ShopStateController.s.SetCannotGo(ShopStateController.CanStartLevelStatus.needToSelectDestination);
			}

			
			shopChecklist.UpdateStatus(new []{shopArea_destinationSelected, shopArea_fleaMarketFull/*, shopArea_noFreeCarts*/});
			MissionWinFinisher.s.SetCannotGo(true);
			return fleaMarketCount;
		}
	}

	Cart SpawnCargo(DataSaver.TrainState.CartState.CargoState cargoState, SnapCartLocation location) {
		var thingy = Instantiate(buildingCargo, shopableComponentsParent);
		thingy.transform.position = location.snapTransform.position;
		thingy.transform.rotation = location.snapTransform.rotation;
		thingy.transform.SetParent(location.snapTransform);
			
		thingy.GetComponentInChildren<CargoModule>().SetCargo(cargoState);
		AddCartToShop(thingy.GetComponent<Cart>(), CartLocation.destinationSelect, false);
		return thingy.GetComponent<Cart>();
	}

	public string GetRandomPowerup() {
		return DataHolder.s.powerUps[Random.Range(0, DataHolder.s.powerUps.Length)].name;
	}

	class RarityPickChance {
		public float epicChance; 
		public float rareChance;
		public float startingValue;
		public float increaseValue;
	}

	RarityPickChance fleaMarketRarity = new RarityPickChance() {
		epicChance = 0.03f,
		rareChance = 0.37f,
		startingValue = -0.05f,
		increaseValue = 0.01f
	};
	RarityPickChance destinationRarity = new RarityPickChance() {
		epicChance = 0.10f,
		rareChance = 0.40f,
		startingValue = -0.12f,
		increaseValue = 0.02f
	};
	
	RarityPickChance fleaMarketArtifactRarity = new RarityPickChance() {
		epicChance = 0.03f,
		rareChance = 0.37f,
		startingValue = -0.05f,
		increaseValue = 0.01f
	};
	
	RarityPickChance destinationArtifactRarity = new RarityPickChance() {
		epicChance = 0.10f,
		rareChance = 0.40f,
		startingValue = -0.12f,
		increaseValue = 0.02f
	};
	
	RarityPickChance eliteArtifactRarity = new RarityPickChance() {
		epicChance = 0.10f,
		rareChance = 0.40f,
		startingValue = -0.12f,
		increaseValue = 0.02f
	};
	
	
	class UpgradeChange {
		public ActUpgradeChance[] chances;
	}

	class ActUpgradeChance {
		public float level1;
		public float level2;
	}
	
	private UpgradeChange fleaMarketArtifactUpgradeChances = new UpgradeChange() {
		chances = new[] {
			new ActUpgradeChance() {
				level1 = 0f,
				level2 = 0f,
			},
			new ActUpgradeChance() {
				level1 = 0.25f,
				level2 = 0.05f,
			},
			new ActUpgradeChance() {
				level1 = 0.5f,
				level2 = 0.15f,
			},
		}
	};
	
	private UpgradeChange destinationArtifactUpgradeChances = new UpgradeChange() {
		chances = new[] {
			new ActUpgradeChance() {
				level1 = 0.25f,
				level2 = 0f,
			},
			new ActUpgradeChance() {
				level1 = 0.9f,
				level2 = 0.1f,
			},
			new ActUpgradeChance() {
				level1 = 0.75f,
				level2 = 0.25f,
			},
		}
	};

	private UpgradeChange fleaMarketUpgradeChances = new UpgradeChange() {
		chances = new[] {
			new ActUpgradeChance() {
				level1 = 0f,
				level2 = 0f,
			},
			new ActUpgradeChance() {
				level1 = 0.25f,
				level2 = 0.05f,
			},
			new ActUpgradeChance() {
				level1 = 0.5f,
				level2 = 0.15f,
			},
		}
	};
	
	private UpgradeChange destinationCargoUpgradeChances = new UpgradeChange() {
		chances = new[] {
			new ActUpgradeChance() {
				level1 = 0.25f,
				level2 = 0f,
			},
			new ActUpgradeChance() {
				level1 = 0.9f,
				level2 = 0.1f,
			},
			new ActUpgradeChance() {
				level1 = 0.75f,
				level2 = 0.25f,
			},
		}
	};


	public void SetUpNewCharacterRarityBoosts() {
		DataSaver.s.GetCurrentSave().currentRun.fleaMarketRarityBoost = fleaMarketRarity.startingValue;
		DataSaver.s.GetCurrentSave().currentRun.destinationRarityBoost = destinationRarity.startingValue;
		DataSaver.s.GetCurrentSave().currentRun.fleaMarketArtifactRarityBoost = fleaMarketArtifactRarity.startingValue;
		DataSaver.s.GetCurrentSave().currentRun.destinationArtifactRarityBoost = destinationArtifactRarity.startingValue;
		DataSaver.s.GetCurrentSave().currentRun.eliteArtifactRarityBoost = eliteArtifactRarity.startingValue;
	}

	public string GetRandomBuildingCargoForDestinationReward(ref bool didRollEpic) {
		return _GetRandomBuildingCargo(tier1Buildings, ref DataSaver.s.GetCurrentSave().currentRun.destinationRarityBoost, destinationRarity, ref didRollEpic);
	}

	public string GetRandomBuildingCargoForFleaMarket(ref bool didRollEpic) {
		List<string> rollSource = tier1Buildings;

		if (DataSaver.s.GetCurrentSave().currentRun.map.GetPlayerStar().starChunk == 0) {
			rollSource = firstShopCarts;
		}
		
		return _GetRandomBuildingCargo(rollSource, ref DataSaver.s.GetCurrentSave().currentRun.fleaMarketRarityBoost, fleaMarketRarity, ref didRollEpic);
	}

	public int GetFleaMarketLevel(bool didRollEpic) { // epic carts will be 1 upgrade level lower than their normal counterparts
		var roll = Random.value;
		var act = DataSaver.s.GetCurrentSave().currentRun.currentAct - 1;
		
		if (roll < fleaMarketUpgradeChances.chances[act].level2) {
			// rolled an epic cart
			return 2 + (didRollEpic? -1 : 0);
		}else if ( roll < fleaMarketUpgradeChances.chances[act].level2 + fleaMarketUpgradeChances.chances[act].level1) {
			//rolled a rare cart

			return 1 + (didRollEpic? -1 : 0);
		} else {
			return 0;
		}
	}

	public int GetDestinationCargoLevel(bool didRollEpic) { // epic carts will be 1 upgrade level lower than their normal counterparts
		var roll = Random.value;
		var act = DataSaver.s.GetCurrentSave().currentRun.currentAct - 1;
		
		if (roll < destinationCargoUpgradeChances.chances[act].level2) {
			// rolled an epic cart

			return 2 + (didRollEpic? -1 : 0);
		}else if ( roll < destinationCargoUpgradeChances.chances[act].level2 + destinationCargoUpgradeChances.chances[act].level1) {
			//rolled a rare cart

			return 1 + (didRollEpic? -1 : 0);
		} else {
			return 0;
		}
	}


	private List<string> recentBuildings = new List<string>();

	string _GetRandomBuildingCargo(List<string> rollSource, ref float rarityBoost, RarityPickChance rarityPickChance, ref bool didRollEpic) {
		var building = _GetRandomBuildingCargo(rollSource, ref rarityBoost, rarityPickChance, true);

		for (int i = 0; i < 3; i++) {
			if (building.Length > 0) {
				break;
			} else {
				building = _GetRandomBuildingCargo(rollSource, ref rarityBoost, rarityPickChance, true);
			}
		}

		if (building.Length <= 0) {
			building = _GetRandomBuildingCargo(rollSource, ref rarityBoost, rarityPickChance, false);
		}

		didRollEpic = false;
		if (DataHolder.s.GetCart(building).myRarity == CartRarity.epic) {
			rarityBoost = rarityPickChance.startingValue;
			didRollEpic = true;
		} else {
			rarityBoost += rarityPickChance.increaseValue;
		}

		recentBuildings.Add(building);
		if (recentBuildings.Count > 7) {
			recentBuildings.RemoveAt(0);
		}

		return building;
	}

	string _GetRandomBuildingCargo(List<string> rollSource, ref float rarityBoost, RarityPickChance rarityPickChance, bool careRecentlySpawned) {
		List<string> possibleCarts = new List<string>();

		var cartRarityRoll = Random.value - rarityBoost - luck;

		if (cartRarityRoll < rarityPickChance.epicChance) {
			// rolled an epic cart
			for (int i = 0; i < rollSource.Count; i++) {
				if (DataHolder.s.GetCart(rollSource[i]).myRarity == CartRarity.epic) {
					if (!recentBuildings.Contains(rollSource[i]) || !careRecentlySpawned) {
						possibleCarts.Add(rollSource[i]);
					}
				}
			}
			
		}
		// if there are no carts of the higher tier available, roll a lower tier
		if (possibleCarts.Count == 0 && cartRarityRoll < rarityPickChance.rareChance + rarityPickChance.epicChance) {
			//rolled a rare cart
			
			for (int i = 0; i < rollSource.Count; i++) {
				if (DataHolder.s.GetCart(rollSource[i]).myRarity == CartRarity.rare) {
					if (!recentBuildings.Contains(rollSource[i]) || !careRecentlySpawned) {
						possibleCarts.Add(rollSource[i]);
					}
				}
			}
			
		}
		// if there are no carts of the higher tier available, roll a lower tier
		if (possibleCarts.Count == 0) {
			//rolled a common cart
			
			for (int i = 0; i < rollSource.Count; i++) {
				if (DataHolder.s.GetCart(rollSource[i]).myRarity == CartRarity.common) {
					if (!recentBuildings.Contains(rollSource[i]) || !careRecentlySpawned) {
						possibleCarts.Add(rollSource[i]);
					}
				}
			}
		}


		var result = "";

		if (possibleCarts.Count > 0) {
			result = possibleCarts[Random.Range(0, possibleCarts.Count)];
		}
		
		return result;
	}
	
	
	private static IEnumerable GetAllModuleNames() {
		var buildings = GameObject.FindObjectOfType<DataHolder>().buildings;
		var buildingNames = new List<string>();
		buildingNames.Add("");
		for (int i = 0; i < buildings.Length; i++) {
			buildingNames.Add(buildings[i].uniqueName);
		}
		return buildingNames;
	}

	public int WorldCartCount() {
		var count = 0;
		for (int i = 0; i < shopCarts.Count; i++) {
			if (shopCarts[i].myLocation == CartLocation.world)
				count += 1;
		}

		return count;
	}
	
	public string GetRandomDestinationArtifact(ref bool didRollEpic) {
		if(!hasArtifactsGeneratedOnce)
			OnShopOpened();
		
		return _GetRandomArtifact(regularArtifacts, ref DataSaver.s.GetCurrentSave().currentRun.destinationArtifactRarityBoost, destinationArtifactRarity, ref didRollEpic);
	}
	
	public string GetRandomFleaMarketArtifact(ref bool didRollEpic) {
		if(!hasArtifactsGeneratedOnce)
			OnShopOpened();
		
		return _GetRandomArtifact(regularArtifacts, ref DataSaver.s.GetCurrentSave().currentRun.fleaMarketArtifactRarityBoost, fleaMarketArtifactRarity, ref didRollEpic);
	}
	
	public string GetRandomEliteRewardArtifact() {
		if(!hasArtifactsGeneratedOnce)
			OnShopOpened();

		bool didRollEpic = false;
		return _GetRandomArtifact(regularArtifacts, ref DataSaver.s.GetCurrentSave().currentRun.eliteArtifactRarityBoost, eliteArtifactRarity, ref didRollEpic);
	}
	
	public int GetRandomFleaMarketArtifactLevel(bool didRollEpic) { // epic carts will be 1 upgrade level lower than their normal counterparts
		var roll = Random.value;
		var act = DataSaver.s.GetCurrentSave().currentRun.currentAct - 1;
		
		if (roll < fleaMarketArtifactUpgradeChances.chances[act].level2) {
			// rolled an epic cart
			return 2 + (didRollEpic? -1 : 0);
		}else if ( roll < fleaMarketArtifactUpgradeChances.chances[act].level2 + fleaMarketArtifactUpgradeChances.chances[act].level1) {
			//rolled a rare cart

			return 1 + (didRollEpic? -1 : 0);
		} else {
			return 0;
		}
	}
	
	public int GetRandomDestinationArtifactLevel(bool didRollEpic) { // epic carts will be 1 upgrade level lower than their normal counterparts
		var roll = Random.value;
		var act = DataSaver.s.GetCurrentSave().currentRun.currentAct - 1;
		
		if (roll < destinationArtifactUpgradeChances.chances[act].level2) {
			// rolled an epic cart
			return 2 + (didRollEpic? -1 : 0);
		}else if ( roll < destinationArtifactUpgradeChances.chances[act].level2 + destinationArtifactUpgradeChances.chances[act].level1) {
			//rolled a rare cart

			return 1 + (didRollEpic? -1 : 0);
		} else {
			return 0;
		}
	}
	
	public string[] GetRandomBossArtifacts(int count) {
		if(!hasArtifactsGeneratedOnce)
			OnShopOpened();

		var indexes = new List<int>();
		for (int i = 0; i < bossArtifacts.Count; i++) {
			indexes.Add(i);
		}
		
		indexes.Shuffle();

		var results = new string[count];

		for (int i = 0; i < results.Length; i++) {
			results[i] = bossArtifacts[indexes[i]];
		}
		
		return results;
	}
	
	private List<string> recentArtifacts = new List<string>();

	string _GetRandomArtifact(List<string> rollSource, ref float rarityBoost, RarityPickChance rarityPickChance ,ref bool didRollEpic) {
		var building = _GetRandomArtifact(rollSource, ref rarityBoost, rarityPickChance, true);

		for (int i = 0; i < 3; i++) {
			if (building.Length > 0) {
				break;
			} else {
				building = _GetRandomArtifact(rollSource, ref rarityBoost, rarityPickChance, true);
			}
		}

		if (building.Length <= 0) {
			building = _GetRandomArtifact(rollSource, ref rarityBoost, rarityPickChance, false);
		}
		
		didRollEpic = false;
		if (DataHolder.s.GetArtifact(building).myRarity == CartRarity.epic) {
			rarityBoost = rarityPickChance.startingValue;
			didRollEpic = true;
		} else {
			rarityBoost += rarityPickChance.increaseValue;
		}

		recentArtifacts.Add(building);
		if (recentArtifacts.Count > 7) {
			recentArtifacts.RemoveAt(0);
		}

		
		return building;
	}

	string _GetRandomArtifact(List<string> rollSource, ref float rarityBoost, RarityPickChance rarityPickChance, bool careRecentlySpawned) {
		List<string> possibleCarts = new List<string>();

		var cartRarityRoll = Random.value - rarityBoost - luck;

		if (cartRarityRoll < rarityPickChance.epicChance) {
			// rolled an epic cart
			for (int i = 0; i < rollSource.Count; i++) {
				if (DataHolder.s.GetArtifact(rollSource[i]).myRarity == CartRarity.epic) {
					if (!recentArtifacts.Contains(rollSource[i]) || !careRecentlySpawned) {
						possibleCarts.Add(rollSource[i]);
					}
				}
			}
			
		}
		// if there are no carts of the higher tier available, roll a lower tier
		if (possibleCarts.Count == 0 && cartRarityRoll < rarityPickChance.rareChance + rarityPickChance.epicChance) {
			//rolled a rare cart
			
			for (int i = 0; i < rollSource.Count; i++) {
				if (DataHolder.s.GetArtifact(rollSource[i]).myRarity == CartRarity.rare) {
					if (!recentArtifacts.Contains(rollSource[i]) || !careRecentlySpawned) {
						possibleCarts.Add(rollSource[i]);
					}
				}
			}
			
		}
		// if there are no carts of the higher tier available, roll a lower tier
		if (possibleCarts.Count == 0) {
			//rolled a common cart
			
			for (int i = 0; i < rollSource.Count; i++) {
				if (DataHolder.s.GetArtifact(rollSource[i]).myRarity == CartRarity.common) {
					if (!recentArtifacts.Contains(rollSource[i]) || !careRecentlySpawned) {
						possibleCarts.Add(rollSource[i]);
					}
				}
			}
		}


		var result = "";

		if (possibleCarts.Count > 0) {
			result = possibleCarts[Random.Range(0, possibleCarts.Count)];
		}
		
		
		
		
		return result;
	}

	public void OnCombatStart() {
		for (int i = 0; i < shopCarts.Count; i++) {
			if (shopCarts[i].myLocation == CartLocation.world) {
				shopCarts[i].gameObject.AddComponent<RubbleFollowFloor>().InstantAttachToFloor();
			}
		}

		for (int i = 0; i < shopArtifacts.Count; i++) {
			if (shopArtifacts[i].myLocation == CartLocation.world) {
				shopArtifacts[i].gameObject.AddComponent<RubbleFollowFloor>().InstantAttachToFloor();
			}
		}
	}
}
