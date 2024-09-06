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

	public GameObject radarAndTrackPicker;

	private void OnEnable() {
		trackSwitchAction.action.Enable();
		trackSwitchAction.action.performed += TrackSwitch;
		//radarAndTrackPicker.SetActive(true);
	}

    private void Start()
    {
		trainCrossingSpeaker = GetComponent<FMODAudioSource>();
    }

    private void TrackSwitch(InputAction.CallbackContext obj) {
	    if(mainLever != null && PlayStateMaster.s.isCombatInProgress())
			mainLever.LeverClicked();
	}

	private void OnDisable() {
		trackSwitchAction.action.Disable();
		trackSwitchAction.action.performed -= TrackSwitch;
		trainCrossingSpeaker.Stop();
		if(radarAndTrackPicker!= null)
			radarAndTrackPicker.SetActive(false);
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
	public Image topMergeStar;
	public Image topBoss;
	public Image topGoodThing;
	public Image bottomMergeStar;
	public Image bottomBoss;
	public Image bottomGoodThing;

	public Color[] pathTypeColors;
	public Sprite[] pathTypeImages;
	
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
			SetImageBasedOnPathType(topTrackType, currentPathTree.leftPathTree.enemyType.myType);
			topMergeStar.enabled = currentPathTree.leftPathTree.enemyType.myType == UpgradesController.PathEnemyType.PathType.elite;
			topBoss.enabled = currentPathTree.leftPathTree.enemyType.myType == UpgradesController.PathEnemyType.PathType.boss;
			topGoodThing.enabled = currentPathTree.leftPathTree.enemyType.myType == UpgradesController.PathEnemyType.PathType.pitStop;
			topTrack.SetActiveState(mainLever.topSelected);
			topCastleCity.SetActive(currentPathTree.leftPathTree.endPath);
			if (currentPathTree.rightPathTree != null) {
				bottomTrack.SetUpTrack(currentPathTree.rightPathTree.myPath.length);
				SetImageBasedOnPathType(bottomTrackType, currentPathTree.rightPathTree.enemyType.myType);
				bottomMergeStar.enabled = currentPathTree.rightPathTree.enemyType.myType == UpgradesController.PathEnemyType.PathType.elite;
				bottomBoss.enabled = currentPathTree.rightPathTree.enemyType.myType == UpgradesController.PathEnemyType.PathType.boss;
				bottomGoodThing.enabled = currentPathTree.rightPathTree.enemyType.myType == UpgradesController.PathEnemyType.PathType.pitStop;
				bottomTrack.SetActiveState(!mainLever.topSelected);
				bottomCastleCity.SetActive(currentPathTree.rightPathTree.endPath);
			}
			
			
			leftCastleCity.SetActive(currentPathTree.startPath);
			//trainStationStart.SetActive(currentPathTree.startPath);
			
			
			trainStationEnd.SetActive(false);
		}
	}

	void SetImageBasedOnPathType(Image target, UpgradesController.PathEnemyType.PathType enemyType) {
		switch (enemyType) {
			case UpgradesController.PathEnemyType.PathType.pitStop:
			case UpgradesController.PathEnemyType.PathType.empty:
				target.enabled = false;
				break;
			case UpgradesController.PathEnemyType.PathType.easy:
				target.enabled = true;
				target.color = pathTypeColors[0];
				target.sprite = pathTypeImages[0];
				break;
			case UpgradesController.PathEnemyType.PathType.regular:
				target.enabled = true;
				target.color = pathTypeColors[0];
				target.sprite = pathTypeImages[1];
				break;
			case UpgradesController.PathEnemyType.PathType.elite:
				target.enabled = true;
				target.color = pathTypeColors[0];
				target.sprite = pathTypeImages[2];
				break;
			case UpgradesController.PathEnemyType.PathType.boss:
				target.enabled = true;
				target.color = pathTypeColors[0];
				target.sprite = pathTypeImages[3];
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(enemyType), enemyType, null);
		}
		/*if (uniqueName.Length == 0) {
			target.enabled = false;
		} else if(DataHolder.s.GetTier1Gun(uniqueName) is var gunModule && gunModule != null){
			target.sprite = gunModule.gunSprite;
			target.color = pathTypeColors[0];
			
			target.enabled = true;
		}else if (DataHolder.s.GetCart(uniqueName, true) is var utilityCart && utilityCart != null) {
			target.sprite = utilityCart.Icon;

			if (utilityCart.GetComponentInChildren<CrystalStorageModule>()) {
				target.color = pathTypeColors[3];
			} else {
				target.color = pathTypeColors[1];
			}
			
			target.enabled = true;
		}else if(DataHolder.s.GetArtifact(uniqueName, true) is var artifact && artifact != null) {
			target.sprite = artifact.mySprite;
			target.color = pathTypeColors[2];
			
			target.enabled = true;
		} else {
			target.enabled = false;
		}*/
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
				//trainCrossingSpeaker.Play();

				isPlayingTrackSwitchWarning = true;
				mainLever.SetTrackSwitchWarningState(true);
			}


			if (nextSegmentChangeDistance > 0 && SpeedController.s.currentDistance > nextSegmentChangeDistance) {
				trainCrossingSpeaker.Stop();

				isPlayingTrackSwitchWarning = false;

				//var upcomingPath = mainLever.topSelected ? PathAndTerrainGenerator.s.currentPathTree.leftPathTree : PathAndTerrainGenerator.s.currentPathTree.rightPathTree;
				var upcomingPath = PathAndTerrainGenerator.s.currentPathTree.leftPathTree;
				
				mainLever.SetTrackSwitchWarningState(false);
				mainLever.SetTrackState(Random.value < 0.5f);
				
				
				//EnemyWavesController.s.SpawnEnemiesOnSegment(nextSegmentChangeDistance, upcomingPath.myPath.length, upcomingPath.enemyType);
				MapController.s.UpdateTrainPosition();
				
				//StopAndPick3RewardUIController.s.TryShowGemReward();

				nextSegmentChangeDistance += upcomingPath.myPath.length;

				PathAndTerrainGenerator.s.currentPathTreeOffset += PathAndTerrainGenerator.s.currentPathTree.myPath.length;
				PathAndTerrainGenerator.s.currentPathTree = upcomingPath;
				PathAndTerrainGenerator.s.PruneAndExtendPaths();
				ReAdjustTracks();
				
				//SpeedController.s.PlayEngineStartEffects();
			}
		}
	}

}
