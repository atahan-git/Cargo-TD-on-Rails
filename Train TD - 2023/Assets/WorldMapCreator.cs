using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class WorldMapCreator : MonoBehaviour {
	public static WorldMapCreator s;

	private void Awake() {
		s = this;
		ResetWorldMapGenerationProgress();
	}

	public InputActionReference openMap;

	private void OnEnable() {
		openMap.action.Enable();
		openMap.action.performed += ToggleWorldMap;
	}

	private void OnDisable() {
		openMap.action.performed -= ToggleWorldMap;
		openMap.action.Disable();
	}

	private void ToggleWorldMap(InputAction.CallbackContext obj) {
		ToggleWorldMap();
	}

	public TMP_Text mapText;

	public bool worldMapOpen = false;

	public WorldMapHexGrid hexGrid;
	public Transform objectsParent;

	public LayerMask castleClickLayerMask;

	public Image mapIcon;
	public Sprite openMapIcon;
	public Sprite backToTrainIcon;
	public void ToggleWorldMap() {
		if (PlayStateMaster.s.isShopOrEndGame()) {
			if (!worldMapOpen) {
				OpenWorldMap();
			} else {
				ReturnToRegularMap();
			}
		}
	}


	private CastleWorldScript highlightedCastle;
	public bool ignoreNextHide = false;

	public bool canSelectCastles = true;

	public RectTransform[] ignoredRects;
	private void Update() {
		if (canSelectCastles && worldMapOpen && Mouse.current.leftButton.wasPressedThisFrame) {
			var mousePos = Mouse.current.position.ReadValue();
			var ray = LevelReferences.s.mainCam.ScreenPointToRay(mousePos);

			var cam = OverlayCamsReference.s.uiCam;
			for (int i = 0; i < ignoredRects.Length; i++) {
				if (ignoredRects[i].gameObject.activeInHierarchy) {
					var insideRect = RectTransformUtility.RectangleContainsScreenPoint(ignoredRects[i], mousePos, cam);
					if (insideRect) {
						return; // we dont process the click if its inside one of the rects
					}
				}
			}

			if (Physics.Raycast(ray, out RaycastHit hit, 100, castleClickLayerMask)) {
				var castle = hit.rigidbody.GetComponent<CastleWorldScript>();
				if (castle != null) {
					MapController.s.ShowStarInfo(castle.myInfo, InfoScreenHidden);

					if (highlightedCastle != null) {
						highlightedCastle.SetHighlightState(false);
					}

					castle.SetHighlightState(true);
					highlightedCastle = castle;

					ignoreNextHide = true;
					Invoke(nameof(StopIgnoreNextHide),0.01f);
				}
			}
		}
	}

	void StopIgnoreNextHide() {
		ignoreNextHide = false;
	}

	bool InfoScreenHidden() {
		if (!ignoreNextHide) {
			if (highlightedCastle != null) {
				highlightedCastle.SetHighlightState(false);
				highlightedCastle = null;
			}

			return true;
		} else {
			return false;
		}
	}


	public GameObject targetStarInfoScreenBudget;

	public UIElementFollowWorldTarget playerHereUIMarker;
	public UIElementFollowWorldTarget yourObjectiveUIMarker;
	public UIElementFollowWorldTarget meteorHereUIMarker;

	public void OpenWorldMap() {
		if (!worldMapOpen) {
			hexGrid.gameObject.SetActive(true);
			for (int i = 0; i < castles.Count; i++) {
				castles[i].Refresh();
				if (castles[i].myInfo.isPlayerHere) {
					playerTrainTargetTransform = castles[i].playerIndicator.transform;
				}
			}
			
			for (int i = 0; i < rails.Count; i++) {
				rails[i].Refresh();
			}
			
			mapText.text = "Train";
			ShopStateController.s.SetStarterUIStatus(false);

			Train.s.transform.position = playerTrainTargetTransform.transform.position;
			Train.s.transform.localScale = playerTrainTargetTransform.transform.localScale;
			
			playerHereUIMarker.gameObject.SetActive(true);
			playerHereUIMarker.SetUp(playerTrainTargetTransform);
			
			yourObjectiveUIMarker.gameObject.SetActive(true);
			
			meteorHereUIMarker.gameObject.SetActive(true);

			//MiniGUI_EnemyUIBar.showHealthBars = false;
			//PlayerModuleSelector.s.DisableModuleSelecting();
			worldMapOpen = true;

			mapIcon.sprite = backToTrainIcon;

			PlayerWorldInteractionController.s.canSelect = false;
			CameraController.s.cannotSelectButCanMoveOverride = true;
			
			CameraController.s.EnterMapMode();

			//SFX
			AudioManager.PlayOneShot(SfxTypes.OpenMap);
		}
	}

	public void ReturnToRegularMap() {
		if (worldMapOpen) {
			hexGrid.gameObject.SetActive(false);
			mapText.text = "Map";
			ShopStateController.s.SetStarterUIStatus(true);

			Train.s.ResetTrainPosition();

			//MiniGUI_EnemyUIBar.showHealthBars = true;
			//PlayerModuleSelector.s.EnableModuleSelecting();
			worldMapOpen = false;
			targetStarInfoScreenBudget.SetActive(false);
			
			
			playerHereUIMarker.gameObject.SetActive(false);
			yourObjectiveUIMarker.gameObject.SetActive(false);
			meteorHereUIMarker.gameObject.SetActive(false);

			mapIcon.sprite = openMapIcon;

			PlayerWorldInteractionController.s.canSelect = true;
			CameraController.s.cannotSelectButCanMoveOverride = true;
			
			CameraController.s.ExitMapMode();

			//SFX
			AudioManager.PlayOneShot(SfxTypes.CloseMap);
		}
	}

	public void ResetMapPos() {
		CameraController.s.ResetMapPos();
	}

	private List<CastleWorldScript> castles;

	[Button]
	public void DebugGenerateWorldMap() {
		GenerateWorldMap();
	}

	public float worldMapGenerationProgress = 0;

	public void ResetWorldMapGenerationProgress() {
		if (generateWorldMap) {
			worldMapGenerationProgress = 0;
		} else {
			worldMapGenerationProgress = 1;
		}
	}

	public void QuickStartNoWorldMap() {
		generateWorldMap = false;
		worldMapGenerationProgress = 1;
	}

	private bool generateWorldMap = true;
	public void GenerateWorldMap() {
		if(!generateWorldMap)
			return;
		
		worldMapGenerationProgress = 0;
		ReturnToRegularMap();

		ResetMapPos();
		objectsParent.DeleteAllChildren();
		hexGrid.ClearGrids();
		castles = CreateCastles();
		CreateRandomWeights(castles);
		CreateRails();

		hexGrid.CreateGridsOverAFewFrames(AfterGridWasMade);
	}

	void AfterGridWasMade() {
		worldMapGenerationProgress = 1 / 3f;
		hexGrid.ApplyHeights(OnceApplyHeightsIsDone);
	}

	void OnceApplyHeightsIsDone() {
		hexGrid.ApplyRails();
		RePositionCastles();

		for (int i = 0; i < castles.Count; i++) {
			castles[i].SetHighlightState(false);
		}
		
		hexGrid.MeshCombine();
		
		worldMapGenerationProgress = 1;
		hexGrid.gameObject.SetActive(false);
	}

	public LayerMask castleSnapGroundLayerMask;

	void RePositionCastles() {
		for (int i = 0; i < castles.Count; i++) {
			var ray = new Ray(castles[i].transform.position + Vector3.up * 100, Vector3.down);

			if (Physics.Raycast(ray, out RaycastHit hit, 200, castleSnapGroundLayerMask)) {
				castles[i].transform.position = hit.point;
			}
		}
	}


	public Vector3 castleRandomPosOffsets = new Vector3(2,2,1);
	public float rightDistancePerChunk = 4;
	public float upDownDistancePerCastle = 3;

	public GameObject castlePrefab;

	public Transform playerTrainTargetTransform;

	List<CastleWorldScript> CreateCastles() {
		var map = DataSaver.s.GetCurrentSave().currentRun.map;

		var curDist = rightDistancePerChunk;

		var castles = new List<CastleWorldScript>();
		for (int i = 0; i < map.chunks.Count; i++) {

			var totalZRange = (map.chunks[i].myStars.Count-1) * upDownDistancePerCastle;
			var curZOffset = -totalZRange / 2;
			
			for (int j = 0; j < map.chunks[i].myStars.Count; j++) {
				var myStar = map.chunks[i].myStars[j];

				var castle = Instantiate(castlePrefab, objectsParent);
				castle.transform.localPosition = new Vector3(curDist, 0, curZOffset) +
				                                 new Vector3(
					                                 Random.Range(-castleRandomPosOffsets.x, castleRandomPosOffsets.x),
					                                 Random.Range(-castleRandomPosOffsets.y, castleRandomPosOffsets.y),
					                                 Random.Range(-castleRandomPosOffsets.z, castleRandomPosOffsets.z)
				                                 );

				var script = castle.GetComponent<CastleWorldScript>();
				script.Initialize(myStar);
				if (myStar.isPlayerHere) {
					playerTrainTargetTransform = script.playerIndicator.transform;
				}

				if (myStar.isBoss) {
					yourObjectiveUIMarker.SetUp(castle.transform);
					var meteorOffset = new GameObject("Meteor Offset");
					meteorOffset.transform.SetParent(castle.transform);
					meteorOffset.transform.position = castle.transform.position + Vector3.forward * 4;
					meteorHereUIMarker.SetUp(meteorOffset.transform);
				}
				
				castles.Add(script);
				curZOffset += upDownDistancePerCastle;
			}

			curDist += rightDistancePerChunk;
		}

		return castles;
	}
	
	public Vector3 randomWeightPosOffsets = new Vector3(1,1,1);
	public Vector3 randomWeightPosBasePos = new Vector3(0,1,0);
	public GameObject randomWeightPrefab;
	public Vector2 randomWeightStrengthRange = new Vector2(0.5f, 1f);

	public Transform topRight;
	public Transform bottomLeft;

	public Vector2Int randomWeightCounts = new Vector2Int(20, 60);

	public float castleDistance = 1f;

	void CreateRandomWeights(List<CastleWorldScript> castles) {
		var xOffset = (topRight.position.x-bottomLeft.position.x)/randomWeightCounts.x;
		var zOffset = (topRight.position.z - bottomLeft.position.z)/randomWeightCounts.y;

		for (int x = 0; x < randomWeightCounts.x; x++) {
			for (int z = 0; z < randomWeightCounts.y; z++) {
				var basePos = bottomLeft.position + new Vector3(xOffset * x, 0, zOffset * z);

				var tooClose = false;
				for (int i = 0; i < castles.Count; i++) {

					if (Vector3.Distance(basePos, castles[i].transform.position) < castleDistance) {
						tooClose = true;
						break;
					}
				}
				if(tooClose)
					continue;
				

				var randomWeight = Instantiate(randomWeightPrefab, objectsParent);
				
				
				randomWeight.transform.position =  basePos+ randomWeightPosBasePos +
				                                   new Vector3(
					                                   Random.Range(-randomWeightPosOffsets.x, randomWeightPosOffsets.x),
					                                   Random.Range(-randomWeightPosOffsets.y, randomWeightPosOffsets.y),
					                                   Random.Range(-randomWeightPosOffsets.z, randomWeightPosOffsets.z)
				                                   );

				randomWeight.GetComponent<HexHeightAffector>().pinWeight = Random.Range(randomWeightStrengthRange.x, randomWeightStrengthRange.y);
			}
		}
	}

	public GameObject railAffectorPrefab;

	public List<RailWorldScript> rails = new List<RailWorldScript>();
	void CreateRails() {
		var map = DataSaver.s.GetCurrentSave().currentRun.map;

		rails.Clear();
		for (int i = 0; i < castles.Count; i++) {
			var a = castles[i];

			for (int j = 0; j < a.myInfo.outgoingConnections.Count; j++) {
				var b = GetCastleWithName(castles, a.myInfo.outgoingConnections[j]);

				var rail = Instantiate(railAffectorPrefab, objectsParent).GetComponent<RailWorldScript>();
				rail.Initialize(a,b, j);
				rails.Add(rail);
			}
		}
	}

	CastleWorldScript GetCastleWithName(List<CastleWorldScript> castles, string starName) {

		for (int i = 0; i < castles.Count; i++) {
			if (castles[i].myInfo.starName == starName)
				return castles[i];
		}

		return null;
	}

}
