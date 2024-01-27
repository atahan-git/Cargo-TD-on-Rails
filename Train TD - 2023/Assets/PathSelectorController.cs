using FMODUnity;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PathSelectorController : MonoBehaviour {
	public static PathSelectorController s;

	private void Awake() {
		s = this;
	}

	public Transform trackParent;
	public GameObject trackPrefab;
	public GameObject switchPrefab;

	[Space] public GameObject castleCityPrefab;
	
	private List<MiniGUI_TrackPath> _tracks = new List<MiniGUI_TrackPath>();
	private List<MiniGUI_TrackLever> _levers = new List<MiniGUI_TrackLever>();

	// public AudioSource trainCrossingAudioSource;
	// public AudioClip trackSwitchSound;
	// Merxon: replaced by FMOD
	[FoldoutGroup("FMOD train crossing sound")]
	public FMODAudioSource trainCrossingSpeaker;

	public ManualHorizontalLayoutGroup layoutGroup;

	public int currentSegment = 0;

	public InputActionReference trackSwitchAction;

	public MiniGUI_TrackLever nextLever;
	

	private ConstructedLevel activeLevel => PlayStateMaster.s.currentLevel;

	private void OnEnable() {
		trackSwitchAction.action.Enable();
		trackSwitchAction.action.performed += TrackSwitch;
	}

    private void Start()
    {
		trainCrossingSpeaker = GetComponent<FMODAudioSource>();
    }

    private void TrackSwitch(InputAction.CallbackContext obj) {
		if (nextLever != null && !nextLever.isLocked) {
			nextLever.LeverClicked();
		}
	}

	private void OnDisable() {
		trackSwitchAction.action.Disable();
		trackSwitchAction.action.performed -= TrackSwitch;
	}
	
	private const int upcomingTracksCount = 2;
	public void SetUpPath() {
		if (activeLevel == null) {
			return;
		}

		EnemyWavesController.s.SetUpLevel();

		_tracks.Clear();
		_levers.Clear();
		
		trackParent.DeleteAllChildren();
		DistanceAndEnemyRadarController.s.ClearRadar();

		Instantiate(castleCityPrefab, trackParent);
		
		EnemyWavesController.s.SpawnEnemiesOnSegment(0,  firstLength);

		currentSegment = 0;
		nextSegmentChangeDistance = firstLength;

		nextLever = _levers[0];
		nextLever.SetButtonPromptState(true);
		
		
		Instantiate(castleCityPrefab, trackParent);
		
		ReCalculateMissionLength();
		layoutGroup.isDirty = true;
	}

	public GameObject leftCastleCity;
	public MiniGUI_TrackPath mainTrack;
	public GameObject mainSwitch;
	public MiniGUI_TrackPath topTrack;
	public MiniGUI_TrackPath bottomTrack;
	public GameObject rightCastleCity;
	void ReAdjustTracks() {
		mainTrack.SetUpTrack(PathAndTerrainGenerator.s.currentPathTree.myPath.length);
		topTrack.SetUpTrack(PathAndTerrainGenerator.s.currentPathTree.leftPath.myPath.length);
		bottomTrack.SetUpTrack(PathAndTerrainGenerator.s.currentPathTree.rightPath.myPath.length);
	}

	public void ActivateLever(int id) {
		var stateToSet = !_levers[id].currentState;
		_levers[id].SetTrackState(stateToSet);
		_tracks[id].doubleLever.SetTrackState(stateToSet);
		_tracks[id].SetTrackState(stateToSet);

		ReCalculateMissionLength();

		// trainCrossingAudioSource.PlayOneShot(trackSwitchSound);
		AudioManager.PlayOneShot(SfxTypes.OnTrackSwitch);
	}

	private float nextSegmentChangeDistance = -1;

	void ReCalculateMissionLength() {
		var currentLength = activeLevel.mySegmentsA[0].segmentLength;
		
		PathAndTerrainGenerator.s.activePath.Clear();
		PathAndTerrainGenerator.s.activePath.Add(PathAndTerrainGenerator.s.myPaths[0]);
		var currentPlaceInTree = PathAndTerrainGenerator.s.currentPathTree;
		
		PathAndTerrainGenerator.s.activePath.Add(currentPlaceInTree.myPath);

		for (int i = 0; i < _levers.Count; i++) {
			if (_levers[i].currentState) {
				currentLength += activeLevel.mySegmentsA[i + 1].segmentLength;

				currentPlaceInTree = currentPlaceInTree.leftPath;
				PathAndTerrainGenerator.s.activePath.Add(currentPlaceInTree.myPath);
			}else {
				currentLength += activeLevel.mySegmentsB[i + 1].segmentLength;
				
				currentPlaceInTree = currentPlaceInTree.rightPath;
				PathAndTerrainGenerator.s.activePath.Add(currentPlaceInTree.myPath);
			}
		}

		currentPlaceInTree = currentPlaceInTree.rightPath; // end station
		PathAndTerrainGenerator.s.activePath.Add(currentPlaceInTree.myPath);
		
		SpeedController.s.SetMissionEndDistance(currentLength);
	}


	public float trackSwitchWarningDistance = 50;
	public bool isPlayingTrackSwitchWarning = false;

	private void Update() {
		if (PlayStateMaster.s.isCombatInProgress()) {
			if (SpeedController.s.currentDistance + trackSwitchWarningDistance > nextSegmentChangeDistance && !isPlayingTrackSwitchWarning) {
				//trainCrossingAudioSource.Play();
				trainCrossingSpeaker.Play();

				isPlayingTrackSwitchWarning = true;
				_levers[currentSegment].SetTrackSwitchWarningState(true);
				_tracks[currentSegment].doubleLever.SetTrackSwitchWarningState(true);
			}


			if (nextSegmentChangeDistance > 0 && SpeedController.s.currentDistance > nextSegmentChangeDistance) {
				//trainCrossingAudioSource.Stop();
				trainCrossingSpeaker.Stop();

				isPlayingTrackSwitchWarning = false;

				_tracks[currentSegment].LockTrackState();
				_levers[currentSegment].LockTrackState();


				LevelSegment upcomingSegment;
				if (_levers[currentSegment].currentState) {
					upcomingSegment = activeLevel.mySegmentsA[currentSegment + 1];
				} else {
					upcomingSegment = activeLevel.mySegmentsB[currentSegment + 1];
				}

				_levers[currentSegment].SetTrackSwitchWarningState(false);
				_levers[currentSegment].SetVisibility(false);
				_levers[currentSegment].SetButtonPromptState(false);
				_tracks[currentSegment].doubleLever.SetTrackSwitchWarningState(false);
				_tracks[currentSegment].doubleLever.SetButtonPromptState(false);

				EnemyWavesController.s.PhaseOutExistingEnemies();
				if (upcomingSegment.isEncounter) {
					EncounterController.s.EngageEncounter(upcomingSegment.levelName);
				} else {
					EnemyWavesController.s.SpawnEnemiesOnSegment(nextSegmentChangeDistance, upcomingSegment);
				}

				if (currentSegment < _tracks.Count - 1) {
					nextSegmentChangeDistance += upcomingSegment.segmentLength;
				} else {
					nextSegmentChangeDistance += 10000000;
				}

				currentSegment += 1;


				if (currentSegment < _levers.Count) {
					nextLever = _levers[currentSegment];
					nextLever.SetButtonPromptState(true);
					_tracks[currentSegment].doubleLever.SetButtonPromptState(true);
				}

				SpeedController.s.PlayEngineStartEffects();
			}
		}
	}

}
