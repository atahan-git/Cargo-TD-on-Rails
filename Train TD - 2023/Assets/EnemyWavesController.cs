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
	public List<EnemyWave> dynamicWaves = new List<EnemyWave>();

	public bool enemiesInitialized = false;

	[NonSerialized] public bool debugNoRegularSpawns = false;
	public bool spawnDynamicEnemies = true;
	public bool act2DemoEndNoSpawn = false;

	public bool encounterMode = false;

	private EnemyFlockingController _flockingController;
	private void Start() {
		Cleanup();
		_flockingController = GetComponent<EnemyFlockingController>();
		//AddExistingSwarmsForTesting();
	}
	
	public void SetUpLevel() {
		Cleanup();

		curMiniWaveCount = 0;
		enemiesInitialized = true;
		curMiniWaveTime = 90;
		spawnDynamicEnemies = true;
	}

	public void SpawnEnemiesOnSegment(float segmentStartDistance, float segmentLength, UpgradesController.PathEnemyType enemyType) {
		if (debugNoRegularSpawns || act2DemoEndNoSpawn)
			return;
		
		var distance = Random.Range(segmentLength / 10f, segmentLength/3f);

		if (enemyType.myType == UpgradesController.PathEnemyType.PathType.boss) {
			BossController.s.NewPathEnteredWithBoss(segmentStartDistance, segmentLength);
			return ;
		}

		if (enemyType.myType == UpgradesController.PathEnemyType.PathType.pitStop) {
			PitStopController.s.MakePitStop(segmentStartDistance, segmentLength);
			return;
		}

		if (enemyType.myType == UpgradesController.PathEnemyType.PathType.empty) {
			return;
		}

		MakeSegmentEnemy(segmentStartDistance+distance, enemyType);
		/*GameObject enemyPrefab = GetRandomEnemyWithReward(PlayStateMaster.s.currentLevel, enemyType.rewardUniqueName, enemyType.isElite);

		if (enemyPrefab != null) {
			var enemy = SpawnEnemy(enemyPrefab, segmentStartDistance + distance, false, Random.value < 0.5f);
			enemy.GetComponentInChildren<CarrierEnemy>()?.SetWhatIsBeingCarried(enemyType.rewardUniqueName);
			enemy.GetComponentInChildren<GemCarrierEnemy>()?.SetWhatIsBeingCarried(enemyType.rewardUniqueName);
		} else {
			Debug.LogError($"Cannot find enemy with {enemyType.rewardUniqueName} and elite={enemyType.isElite}");
		}*/
	}


	public void MakeSegmentEnemy(float distance, UpgradesController.PathEnemyType enemyType, bool forceDirection = false, bool forcedLeft = false, bool spawnMoving = false, bool forceNoSpecialGear = false) {
		var enemyBudget = 1;
		var uniqueGearBudget = 0;
		var eliteGearBudget = 0;
		var megaEnemyBudget = 0;
		var depth = PathAndTerrainGenerator.s.GetDepthForEnemyDifficultyPurposes();
		switch (enemyType.myType) {
			case UpgradesController.PathEnemyType.PathType.easy:
				var difficulty = Mathf.Clamp(depth+2, 3, 5);
				enemyBudget = difficulty;
				uniqueGearBudget = 0;
				megaEnemyBudget = 0;
				break;
			case UpgradesController.PathEnemyType.PathType.regular:
				if (depth >= MapController.s.currentMap.pitStopDepth) {
					enemyBudget = Random.Range(4, 6);
					uniqueGearBudget = Random.Range(1,2);
				} else {
					enemyBudget = Random.Range(3, 5);
					uniqueGearBudget = 1;
				}
				megaEnemyBudget = 0;
				break;
			case UpgradesController.PathEnemyType.PathType.elite:
				enemyBudget = Random.Range(3, 5);
				eliteGearBudget = 1;
				uniqueGearBudget = 1;
				megaEnemyBudget = 1;
				break;
		}

		enemyBudget += DataSaver.s.GetCurrentSave().currentRun.currentAct - 1;
		uniqueGearBudget += DataSaver.s.GetCurrentSave().currentRun.currentAct - 1;

		enemyBudget = Mathf.RoundToInt(enemyBudget * TweakablesMaster.s.GetEnemyBudgetMultiplier());
		enemyBudget = Mathf.Clamp(enemyBudget, 1, 12);
		uniqueGearBudget += TweakablesMaster.s.GetExtraUniqueGearBudget();
		uniqueGearBudget = Mathf.Clamp(uniqueGearBudget, 0, 1000);

		if (forceNoSpecialGear) {
			uniqueGearBudget = 0;
			eliteGearBudget = 0;
		}

		if (enemyBudget >= 6) {
			var isLeft = Random.value < 0.5f;
			MakeEnemyBattalion(distance, megaEnemyBudget, eliteGearBudget, uniqueGearBudget, 5, spawnMoving,isLeft);
			MakeEnemyBattalion(distance, 0, 0, 0, enemyBudget-5, spawnMoving, !isLeft);
		} else {
			var direction = Random.value < 0.5f;

			if (forceDirection) {
				direction = forcedLeft;
			}
			
			MakeEnemyBattalion(distance, megaEnemyBudget, eliteGearBudget, uniqueGearBudget, enemyBudget, spawnMoving,direction);
		}
		
	}

	private void MakeEnemyBattalion(float distance, int megaEnemyBudget, int eliteGearBudget, int uniqueGearBudget, int enemyBudget, bool spawnMoving, bool isLeft) {
		var curLevel = PlayStateMaster.s.currentLevel;
		
		var playerDistance = SpeedController.s.currentDistance;
		var wave = Instantiate(curLevel.formations[Random.Range(0, curLevel.formations.Length)], Vector3.forward * (distance - playerDistance), Quaternion.identity).GetComponent<EnemyWave>();
		wave.transform.SetParent(transform);

		var slots = wave.GetComponentsInChildren<EnemySwarmMaker>();
		
		print($"Unique gear count of battalion {uniqueGearBudget}");
		// make mega enemy
		for (int i = 0; i < megaEnemyBudget; i++) {
			var slot = GetSlot(slots);
			slot.currentFill += 2;
			var megaEnemy = Instantiate(curLevel.megaEnemies[Random.Range(0, curLevel.megaEnemies.Length)], GetSlot(slots).transform);

			var gunSlots = megaEnemy.GetComponentsInChildren<EnemyGunSlot>();

			for (int j = 0; j < gunSlots.Length; j++) {
				if (eliteGearBudget > 0) {
					Instantiate(curLevel.eliteEquipment[Random.Range(0, curLevel.eliteEquipment.Length)], gunSlots[j].transform);
					eliteGearBudget -= 1;
				} else if (uniqueGearBudget > 0) {
					Instantiate(curLevel.uniqueEquipment[Random.Range(0, curLevel.uniqueEquipment.Length)], gunSlots[j].transform);
					uniqueGearBudget -= 1;
				}else {
					Instantiate(curLevel.enemyGuns[Random.Range(0, curLevel.enemyGuns.Length)], gunSlots[j].transform);
				}
			}

			//megaEnemy.GetComponent<EnemyInSwarm>().isElite = true;
		}

		// budget enemies
		var enemyDistribution = RandomIntsThatAddUpToInput(enemyBudget, 3);
		// make all the other enemies
		for (int i = 0; i < enemyBudget; i++) {
			int spawnCount;
			GameObject enemyPrefab;
			var isSmall = false;
			if (i < enemyDistribution[0]) {
				spawnCount = 1;
				enemyPrefab = curLevel.bigEnemies[Random.Range(0, curLevel.bigEnemies.Length)];
			} else if (i < enemyDistribution[0] + enemyDistribution[1]) {
				spawnCount = Random.Range(1, 3);
				enemyPrefab = curLevel.mediumEnemies[Random.Range(0, curLevel.mediumEnemies.Length)];
			} else {
				spawnCount = Random.Range(2, 6);
				enemyPrefab = curLevel.smallEnemies[Random.Range(0, curLevel.smallEnemies.Length)];
				isSmall = true;
			}

			GameObject enemyGun = curLevel.enemyGuns[Random.Range(0, curLevel.enemyGuns.Length)];
			//curLevel.uniqueEquipment[Random.Range(0, curLevel.uniqueEquipment.Length)];

			var slot = GetSlot(slots);
			slot.currentFill += 1;

			for (int j = 0; j < spawnCount; j++) {
				var enemy = Instantiate(enemyPrefab, slot.transform);
				var gunSlots = enemy.GetComponentsInChildren<EnemyGunSlot>();
				for (int k = 0; k < gunSlots.Length; k++) {
					if (uniqueGearBudget > 0) {
						if (forceSpawnWithEquipment) {
							Instantiate(forcedEquipment, gunSlots[k].transform);
						} else {
							Instantiate(curLevel.uniqueEquipment[Random.Range(0, curLevel.uniqueEquipment.Length)], gunSlots[k].transform);
						}

						uniqueGearBudget -= 1;
						//enemy.GetComponent<EnemyInSwarm>().isElite = true;
					} else {
						Instantiate(enemyGun, gunSlots[k].transform);
					}

					if (isSmall) {
						var guns = gunSlots[k].GetComponentsInChildren<EnemyGunModule>();
						for (int l = 0; l < guns.Length; l++) {
							guns[l].fireDelay *= 2;
						}
					}
				}
			}
		}


		wave.SetUp(distance, spawnMoving, isLeft);
		waves.Add(wave);
	}

	public void SpawnCustomBattalion(GameObject battalionPrefab, float distance, bool spawnMoving, bool isLeft) {
		var playerDistance = SpeedController.s.currentDistance;
		var wave = Instantiate(battalionPrefab, Vector3.forward * (distance - playerDistance), Quaternion.identity).GetComponent<EnemyWave>();
		wave.transform.SetParent(transform);
		
		wave.SetUp(distance, spawnMoving, isLeft);
		waves.Add(wave);
	}

	int[] RandomIntsThatAddUpToInput(int total, int count) {
		float[] randomFloats = new float[count];
		var sum = 0f;
		for (int i = 0; i < randomFloats.Length; i++) {
			randomFloats[i] = Random.value;
			sum += randomFloats[i];
		}
		//make sum approximately total
		int[] result = new int[count];
		var resultSum = 0;
		for (int i = 0; i < result.Length; i++) {
			result[i] = Mathf.FloorToInt(randomFloats[i] * total / sum);
			resultSum += result[i];
		}
		//randomly add missing numbers
		for (int i = 0; i < total-resultSum; i++) {
			result[Random.Range(0, result.Length)] += 1;
		}

		return result;
	}

	EnemySwarmMaker GetSlot(EnemySwarmMaker[] slots) {
		return slots[0];
		var legalSlots = new List<EnemySwarmMaker>();
		for (int i = 0; i < slots.Length; i++) {
			if (slots[i].currentFill < slots[i].enemyCapacity) {
				legalSlots.Add(slots[i]);
			}
		}

		if (legalSlots.Count > 0) {
			return legalSlots[Random.Range(0, legalSlots.Count)];
		} else {
			return slots[Random.Range(0, slots.Length)];
		}
	}


	void MakeDynamicWave() {
		var distance = SpeedController.s.currentDistance-30;
		var enemyBudget = Mathf.CeilToInt(Random.Range(Mathf.Log(curMiniWaveCount+1)+1,(curMiniWaveCount/4f)+2));
		Debug.Log($"Rolled {enemyBudget} dynamic enemies, round {curMiniWaveCount}, range was {Mathf.Log(curMiniWaveCount+1)+1} - {curMiniWaveCount/2f+2}");
		var uniqueGearBudget = 0;
		if (curMiniWaveCount > 10) {
			uniqueGearBudget = 1;
		}

		enemyBudget = Mathf.CeilToInt(enemyBudget/2f);

		if (enemyBudget < 1) {
			enemyBudget = 1;
		}

		MakeEnemyBattalion(distance, 0, 0, uniqueGearBudget, enemyBudget, true, Random.value < 0.5f);

		curMiniWaveCount += 1;
	}

	public int maxConcurrentWaves = 6;

	public EnemyWave SpawnEnemy(GameObject enemyPrefab, float distance, bool startMoving, bool isLeft, bool isDynamic = false) {
		return SpawnEnemy(enemyPrefab, null, distance, startMoving, isLeft, isDynamic);
	}
	
	public EnemyWave SpawnEnemy(GameObject enemyPrefab, GameObject gear, float distance, bool startMoving, bool isLeft, bool isDynamic = false) {
		var playerDistance = SpeedController.s.currentDistance;
		var wave = Instantiate(enemyPrefab, Vector3.forward * (distance - playerDistance), Quaternion.identity).GetComponent<EnemyWave>();
		wave.transform.SetParent(transform);
		wave.SetUp(distance, startMoving, isLeft);
		waves.Add(wave);


		if (gear != null) {
			var gunSlots = wave.GetComponentsInChildren<EnemyGunSlot>();
			for (int k = 0; k < gunSlots.Length; k++) {
				Instantiate(gear, gunSlots[k].transform);
				break;
			}
		}

		if(isDynamic)
			dynamicWaves.Add(wave);

		return wave;
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


	public float miniWaveTime = 60;
	public float curMiniWaveTime;
	public int curMiniWaveCount = 0;

	public void SetWarpingMode(bool isWarping) {
		
	}
	
	void Update() {
		if (PlayStateMaster.s.isCombatInProgress() && enemiesInitialized) {
			var playerDistance = SpeedController.s.currentDistance;

			for (int i = 0; i < waves.Count; i++) {
				waves[i].UpdateBasedOnDistance(playerDistance);
			}


			if (!DynamicSpawnsActive())
				return;

			if (waves.Count < maxConcurrentWaves) {
				curMiniWaveTime -= Time.deltaTime;
				
				if (curMiniWaveTime <= 0) {
					curMiniWaveTime = miniWaveTime;

					MakeDynamicWave();
				}
				
			}
		} else {
			var playerDistance = SpeedController.s.currentDistance;

			for (int i = 0; i < waves.Count; i++) {
				waves[i].UpdateBasedOnDistance(playerDistance);
			}
		}

		/*if (PlayStateMaster.s.isCombatInProgress()) {
			if (SpeedController.s.currentDistance < 30) {
				curMiniWaveTime = 30;
			}
		}*/
		
		_flockingController.UpdateEnemyFlocks();
	}

	public bool DynamicSpawnsActive() {

		return false;
		//var firstRunAfterTutorial = DataSaver.s.GetCurrentSave().tutorialProgress.runsMadeAfterTutorial <= 0;
		var difficultyEasy = DataSaver.s.GetCurrentSave().currentRun.difficulty == 0;
		var isDynamicSpawnSuppressed = debugNoRegularSpawns || act2DemoEndNoSpawn || !spawnDynamicEnemies || encounterMode || difficultyEasy;
		return !isDynamicSpawnSuppressed && PlayStateMaster.s.isCombatInProgress();
	}

	public void RemoveWave(EnemyWave toRemove) {
		waves.Remove(toRemove);

		if (dynamicWaves.Contains(toRemove)) {
			dynamicWaves.Remove(toRemove);
		}
	}

	public void Cleanup() {
		transform.DeleteAllChildren();
		enemySwarmMakers.Clear();
		enemyInSwarms.Clear();
		waves.Clear();
		dynamicWaves.Clear();
		enemiesInitialized = false;
	}
	
	
	public List<EnemySwarmMaker> enemySwarmMakers = new List<EnemySwarmMaker>();
	public List<EnemyInSwarm> enemyInSwarms = new List<EnemyInSwarm>();

	public int GetActiveEnemyCount() {
		return enemyInSwarms.Count;
	}

	public void StopSpawningNewDynamicEnemies() {
		spawnDynamicEnemies = false;
	}
	
	[Header("Debug")]
	public bool forceSpawnWithEquipment;
	public GameObject forcedEquipment;
}
