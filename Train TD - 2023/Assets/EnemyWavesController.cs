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
	
	// the new general idea is that you constantly get smaller enemies, then on each segment you get a big enemy. Enemies no longer leave you alone at intersections

	private void Awake() {
		s = this;
	}

	public List<EnemyWave> waves = new List<EnemyWave>();

	public bool enemiesInitialized = false;

	[NonSerialized] public bool debugNoRegularSpawns = false;

	public bool encounterMode = false;

	private void Start() {
		Cleanup();
	}


	public void SetUpLevel() {
		Cleanup();
	}

	public void SpawnEnemiesOnSegment(float segmentStartDistance, float segmentLength) {
		if (debugNoRegularSpawns)
			return;


		enemiesInitialized = true; // ie for empty segments
		if (enemiesInitialized) {
			var distance = Random.Range(segmentLength/10f, segmentLength);

			var enemyPrefab =PlayStateMaster.s.currentLevel.battalions[Random.Range(0, PlayStateMaster.s.currentLevel.battalions.Length)];

			SpawnEnemy(enemyPrefab, segmentStartDistance + distance, false, Random.value > 0.5f);
		}
	}


	public int maxConcurrentWaves = 6;

	void SpawnEnemy(GameObject enemyPrefab, float distance, bool startMoving, bool isLeft) {
		return;
		var playerDistance = SpeedController.s.currentDistance;
		var wave = Instantiate(enemyPrefab, Vector3.forward * (distance - playerDistance), Quaternion.identity).GetComponent<EnemyWave>();
		wave.transform.SetParent(transform);
		wave.SetUp( distance, startMoving, isLeft);
		waves.Add(wave);
		//UpdateEnemyTargetables();
	}

	/*public void SpawnAmbush(LevelSegment ambush) {
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
	}*/

	public void PhaseOutExistingEnemies() {
		for (int i = 0; i < waves.Count; i++) {
			waves[i].Leave(false);
		}
	}

	void Update() {
		if (PlayStateMaster.s.isCombatInProgress() && enemiesInitialized) {

			var playerDistance = SpeedController.s.currentDistance;

			for (int i = 0; i < waves.Count; i++) {
				waves[i].UpdateBasedOnDistance(playerDistance);
			}


			if (debugNoRegularSpawns)
				return;

			if (waves.Count < maxConcurrentWaves) {
				if (encounterMode) {
					return;
				}
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
	}

	public void Cleanup() {
		transform.DeleteAllChildren();
		enemiesInitialized = false;
	}
}
