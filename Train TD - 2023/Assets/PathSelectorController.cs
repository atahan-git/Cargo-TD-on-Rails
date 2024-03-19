using FMODUnity;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PathSelectorController : MonoBehaviour {
	public static PathSelectorController s;

	private void Awake() {
		s = this;
	}

	public Transform trackParent;

	// public AudioSource trainCrossingAudioSource;
	// public AudioClip trackSwitchSound;
	// Merxon: replaced by FMOD
	[FoldoutGroup("FMOD train crossing sound")]
	public FMODAudioSource trainCrossingSpeaker;

	public ManualHorizontalLayoutGroup layoutGroup;

	public InputActionReference trackSwitchAction;
	
	private ConstructedLevel activeLevel => PlayStateMaster.s.currentLevel;


	public GameObject trainStationStart;
	public GameObject trainStationEnd;

	private void OnEnable() {
		trackSwitchAction.action.Enable();
		trackSwitchAction.action.performed += TrackSwitch;
	}

    private void Start()
    {
		trainCrossingSpeaker = GetComponent<FMODAudioSource>();
    }

    private void TrackSwitch(InputAction.CallbackContext obj) {
		mainLever.LeverClicked();
	}

	private void OnDisable() {
		trackSwitchAction.action.Disable();
		trackSwitchAction.action.performed -= TrackSwitch;
	}

	
	public void SetUpPath() {
		if (activeLevel == null) {
			return;
		}

		EnemyWavesController.s.SetUpLevel();

		DistanceAndEnemyRadarController.s.ClearRadar();

		//EnemyWavesController.s.SpawnEnemiesOnSegment(0,  PathAndTerrainGenerator.s.currentPathTree.myPath.length);

		nextSegmentChangeDistance = PathAndTerrainGenerator.s.currentPathTree.myPath.length + PathAndTerrainGenerator.s.currentPathTreeOffset;

		mainLever.SetButtonPromptState(true);
		mainLever.SetTrackSwitchWarningState(false);
		
		
		topTrack.gameObject.SetActive(true);
		bottomTrack.gameObject.SetActive(true);
		mainTrack.gameObject.SetActive(true);
		rightCastleCity.SetActive(false);
		
		mainLever.SetTrackState(Random.value < 0.5f);

		ReAdjustTracks();
		layoutGroup.isDirty = true;
	}

	public GameObject leftCastleCity;
	public GameObject rightCastleCity;
	public MiniGUI_TrackPath mainTrack;
	public MiniGUI_TrackLever mainLever;
	public MiniGUI_TrackPath topTrack;
	public MiniGUI_TrackPath bottomTrack;
	public GameObject topCastleCity;
	public GameObject bottomCastleCity;
	public Image topTrackType;
	public Image bottomTrackType;

	public Color[] pathTypeColors;
	
	void ReAdjustTracks() {
		var currentPathTree = PathAndTerrainGenerator.s.currentPathTree;
		mainTrack.SetUpTrack(currentPathTree.myPath.length);
		
		if (currentPathTree.endPath) {
			var stationDistance= nextSegmentChangeDistance-PathGenerator.stationStraightDistance/2f;
			SpeedController.s.SetMissionEndDistance(stationDistance-9f);
			topTrack.gameObject.SetActive(false);
			bottomTrack.gameObject.SetActive(false);
			mainLever.gameObject.SetActive(false);
			rightCastleCity.SetActive(true);
			topCastleCity.SetActive(false);
			bottomCastleCity.SetActive(false);

			trainStationEnd.GetComponent<TrainStation>().stationDistance = stationDistance;
			trainStationEnd.GetComponent<TrainStation>().Update();
			trainStationEnd.SetActive(true);
			
		} else {
			topTrack.SetUpTrack(currentPathTree.leftPathTree.myPath.length);
			SetImageBasedOnPathType(topTrackType, currentPathTree.leftPathTree.myPath.pathRewardUniqueName);
			bottomTrack.SetUpTrack(currentPathTree.rightPathTree.myPath.length);
			SetImageBasedOnPathType(bottomTrackType, currentPathTree.rightPathTree.myPath.pathRewardUniqueName);
			topTrack.SetActiveState(mainLever.topSelected);
			bottomTrack.SetActiveState(!mainLever.topSelected);

			leftCastleCity.SetActive(currentPathTree.startPath);
			topCastleCity.SetActive(currentPathTree.leftPathTree.endPath);
			bottomCastleCity.SetActive(currentPathTree.rightPathTree.endPath);
			
			//trainStationStart.SetActive(currentPathTree.startPath);
			
			
			trainStationEnd.SetActive(false);
		}
	}

	void SetImageBasedOnPathType(Image target, string uniqueName) {
		if (uniqueName.Length == 0) {
			target.enabled = false;
		} else if(DataHolder.s.GetTier1Gun(uniqueName) is var gunModule && gunModule != null){
			target.sprite = gunModule.gunSprite;
			target.color = pathTypeColors[0];
		}else if (DataHolder.s.GetCart(uniqueName, true) is var utilityCart && utilityCart != null) {
			target.sprite = utilityCart.Icon;

			if (utilityCart.GetComponentInChildren<CrystalStorageModule>()) {
				target.color = pathTypeColors[3];
			} else {
				target.color = pathTypeColors[1];
			}
			
		}else if(DataHolder.s.GetArtifact(uniqueName) is var artifact && artifact != null) {
			target.sprite = artifact.mySprite;
			target.color = pathTypeColors[2];
		} else {
			target.enabled = false;
		}
	}

	public void ActivateLever() {
		var stateToSet = !mainLever.topSelected;
		
		mainLever.SetTrackState(stateToSet);

		ReAdjustTracks();

		// trainCrossingAudioSource.PlayOneShot(trackSwitchSound);
		AudioManager.PlayOneShot(SfxTypes.OnTrackSwitch);
	}

	public float nextSegmentChangeDistance = -1;


	public float trackSwitchWarningDistance = 50;
	public bool isPlayingTrackSwitchWarning = false;

	private void Update() {
		if (PlayStateMaster.s.isCombatInProgress()) {
			if (SpeedController.s.currentDistance + trackSwitchWarningDistance > nextSegmentChangeDistance && !isPlayingTrackSwitchWarning) {
				//trainCrossingAudioSource.Play();
				trainCrossingSpeaker.Play();

				isPlayingTrackSwitchWarning = true;
				mainLever.SetTrackSwitchWarningState(true);
			}


			if (nextSegmentChangeDistance > 0 && SpeedController.s.currentDistance > nextSegmentChangeDistance) {
				//trainCrossingAudioSource.Stop();
				trainCrossingSpeaker.Stop();

				isPlayingTrackSwitchWarning = false;

				var upcomingPath = mainLever.topSelected ? PathAndTerrainGenerator.s.currentPathTree.leftPathTree : PathAndTerrainGenerator.s.currentPathTree.rightPathTree;
				
				//mainLever.LockTrackState();
				mainLever.SetTrackSwitchWarningState(false);
				mainLever.SetTrackState(Random.value < 0.5f);
				
				
				//EnemyWavesController.s.PhaseOutExistingEnemies();
				/*if (upcomingSegment.isEncounter) {
					EncounterController.s.EngageEncounter(upcomingSegment.levelName);
				} else {*/
				//}
				EnemyWavesController.s.SpawnEnemiesOnSegment(nextSegmentChangeDistance, upcomingPath.myPath.length, upcomingPath.myPath.pathRewardUniqueName);

				nextSegmentChangeDistance += upcomingPath.myPath.length;
				/*if (!upcomingPath.endPath) {
					nextSegmentChangeDistance += upcomingPath.myPath.length;
				} else {
					nextSegmentChangeDistance += 10000000;
				}*/

				PathAndTerrainGenerator.s.currentPathTreeOffset += PathAndTerrainGenerator.s.currentPathTree.myPath.length;
				PathAndTerrainGenerator.s.currentPathTree = upcomingPath;
				PathAndTerrainGenerator.s.PruneAndExtendPaths();
				ReAdjustTracks();
				
				SpeedController.s.PlayEngineStartEffects();
			}
		}
	}

}
