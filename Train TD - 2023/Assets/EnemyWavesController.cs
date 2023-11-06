using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

public class EnemyWavesController : MonoBehaviour {
	public static EnemyWavesController s;

	private void Awake() {
		s = this;
	}

	public GameObject enemyWavePrefab;

	public List<EnemyWave> waves = new List<EnemyWave>();

	public bool enemiesInitialized = false;

	[NonSerialized] public bool debugNoRegularSpawns = false;

	public MiniGUI_PursuerTimer pursuerTimerObject;
	
	public UnityEvent<EnemyIdentifier> OnEnemyWaveSpawn = new UnityEvent<EnemyIdentifier>();
	public UnityEvent<EnemyIdentifier> OnEnemyWaveCleared = new UnityEvent<EnemyIdentifier>();

	public bool encounterMode = false;

	private void Start() {
		Cleanup();
	}


	public void SetUpLevel() {
		Cleanup();
		
		
		pursuerTimerObject.gameObject.SetActive(false);
		
		if (PlayStateMaster.s.currentLevel.dynamicSpawnData != null) {
			pursuerTimerObject.SetUp(PlayStateMaster.s.currentLevel.dynamicSpawnData);
			
			var curDynamicSpawn = PlayStateMaster.s.currentLevel.dynamicSpawnData;
			curDynamicSpawn.curTime = curDynamicSpawn.firstSpawnTime;
		}
	}

	public void SpawnEnemiesOnSegment(float segmentStartDistance, LevelSegment segment) {
		if (debugNoRegularSpawns)
			return;


		enemiesInitialized = !segment.isEncounter;
		if (enemiesInitialized) {
			var enemiesOnPath = segment.enemiesOnPath;
			for (int i = 0; i < enemiesOnPath.Length; i++) {
				Artifact artifact = null;
				if (enemiesOnPath[i].hasReward) {
					artifact = DataHolder.s.GetArtifact(segment.artifactRewardUniqueName);
				}

				SpawnEnemy(enemiesOnPath[i].enemyIdentifier,
					segmentStartDistance + enemiesOnPath[i].distanceOnPath,
					false, enemiesOnPath[i].isLeft,
					artifact);
			}
		}
	}



	public void DebugEnemySpawn(EnemyIdentifier debugEnemy, int distance) {
		SpawnEnemy(debugEnemy, SpeedController.s.currentDistance + distance, false, false);
	}

	public void SpawnEnemy(EnemyOnPathData data) {
		SpawnEnemy(data.enemyIdentifier, data.distanceOnPath, false, data.isLeft);
	}

	public int maxConcurrentWaves = 6;

	void SpawnEnemy(EnemyIdentifier enemyIdentifier, float distance, bool startMoving, bool isLeft, Artifact artifact = null) {
		var playerDistance = SpeedController.s.currentDistance;
		var wave = Instantiate(enemyWavePrefab, Vector3.forward * (distance - playerDistance), Quaternion.identity).GetComponent<EnemyWave>();
		wave.transform.SetParent(transform);
		wave.SetUp(enemyIdentifier, distance, startMoving, isLeft, artifact);
		waves.Add(wave);
		//UpdateEnemyTargetables();
		OnEnemyWaveSpawn.Invoke(enemyIdentifier);
	}

	public void SpawnAmbush(LevelSegment ambush) {
		var segment = ambush;
		var enemiesOnPath = segment.enemiesOnPath;
		for (int i = 0; i < enemiesOnPath.Length; i++) {
			Artifact artifact = null;
			if (enemiesOnPath[i].hasReward) {
				artifact = DataHolder.s.GetArtifact(segment.artifactRewardUniqueName);
			}

			SpawnEnemy(enemiesOnPath[i].enemyIdentifier,
				SpeedController.s.currentDistance + enemiesOnPath[i].distanceOnPath,
				true, enemiesOnPath[i].isLeft,
				artifact);
		}
	}

	public void PhaseOutExistingEnemies() {
		for (int i = 0; i < waves.Count; i++) {
			waves[i].Leave(false);
		}
	}

	void Update() {
		if (PlayStateMaster.s.isCombatInProgress() && enemiesInitialized) {
			pursuerTimerObject.gameObject.SetActive(true);

			var playerDistance = SpeedController.s.currentDistance;

			for (int i = 0; i < waves.Count; i++) {
				waves[i].UpdateBasedOnDistance(playerDistance);
			}


			if (debugNoRegularSpawns)
				return;

			if (waves.Count < maxConcurrentWaves) {
				if (encounterMode) {
					pursuerTimerObject.gameObject.SetActive(false);
					return;
				}
				
				//for (int i = 0; i < SceneLoader.s.currentLevel.dynamicSpawnEnemies.Length; i++) {
				if (PlayStateMaster.s.currentLevel.dynamicSpawnData != null) {
					var dynamicSpawnEnemy = PlayStateMaster.s.currentLevel.dynamicSpawnData;

					if (dynamicSpawnEnemy.curTime <= 0) {
						SpawnEnemy(dynamicSpawnEnemy.enemyIdentifier, playerDistance - EnemyDynamicSpawnData.distanceFromTrain, true, Random.value > 0.5f);
						dynamicSpawnEnemy.curTime = dynamicSpawnEnemy.spawnInterval;

						dynamicSpawnEnemy.curIncreaseInNumberCount += 1;
						if (dynamicSpawnEnemy.increaseInNumberInterval >= 0 && dynamicSpawnEnemy.curIncreaseInNumberCount >= dynamicSpawnEnemy.increaseInNumberInterval) {
							dynamicSpawnEnemy.enemyIdentifier.enemyCount += 1;
							dynamicSpawnEnemy.curIncreaseInNumberCount = 0;
						}
					}

					dynamicSpawnEnemy.curTime -= Time.deltaTime;
				}
				//}
			}
		} else {
			var playerDistance = SpeedController.s.currentDistance;

			for (int i = 0; i < waves.Count; i++) {
				waves[i].UpdateBasedOnDistance(playerDistance);
			}
		}
	}

	public void RemoveWave(EnemyWave toRemove) {
		waves.Remove(toRemove);
		OnEnemyWaveCleared.Invoke(toRemove.myEnemy);
	}

	public void Cleanup() {
		pursuerTimerObject.gameObject.SetActive(false);
		transform.DeleteAllChildren();
		enemiesInitialized = false;
	}
}
