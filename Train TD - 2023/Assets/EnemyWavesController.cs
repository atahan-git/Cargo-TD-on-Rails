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

	public bool encounterMode = false;

	private void Start() {
		Cleanup();
		//AddExistingSwarmsForTesting();
	}
	
	public void SetUpLevel() {
		Cleanup();

		enemiesInitialized = true;
		curMiniWaveTime = 20;
	}

	public void SpawnEnemiesOnSegment(float segmentStartDistance, float segmentLength, string rewardUniqueName, bool mergeReward) {
		if (debugNoRegularSpawns)
			return;
		
		var distance = Random.Range(segmentLength / 10f, segmentLength);

		GameObject enemyPrefab = GetRandomEnemyWithReward(PlayStateMaster.s.currentLevel.rewardBattalions, rewardUniqueName, mergeReward);

		if (enemyPrefab != null) {
			var enemy = SpawnEnemy(enemyPrefab, segmentStartDistance + distance, false, Random.value > 0.5f);
			enemy.GetComponentInChildren<CarrierEnemy>()?.SetWhatIsBeingCarried(rewardUniqueName);
			enemy.GetComponentInChildren<GemCarrierEnemy>()?.SetWhatIsBeingCarried(rewardUniqueName);
		} else {
			Debug.LogError($"Cannot find enemy with {rewardUniqueName} and merge={mergeReward}");
		}
	}


	GameObject GetRandomEnemyWithReward(GameObject[] possiblePrefabs, string uniqueName, bool mergeReward) {
		var legalPrefabs = new List<GameObject>();
		
		for (int i = 0; i < possiblePrefabs.Length; i++) {
			var mergeCarrier = possiblePrefabs[i].GetComponentInChildren<MergeCarrierEnemy>();
			if (mergeReward) {
				if (mergeCarrier == null) {
					continue;
				}
			} else {
				if (mergeCarrier != null) {
					continue;
				}
			}
			
			
			var carrier = possiblePrefabs[i].GetComponentInChildren<CarrierEnemy>();
			if (carrier != null) {
				for (int j = 0; j < carrier.carryAwards.Length; j++) {
					if (carrier.carryAwards[j].uniqueName == uniqueName) {
						legalPrefabs.Add(possiblePrefabs[i]);
						break;
					}
				}
				continue;
			}
			
			var gemCarrier = possiblePrefabs[i].GetComponentInChildren<GemCarrierEnemy>();
			if (gemCarrier != null) {
				for (int j = 0; j < gemCarrier.carryAwards.Length; j++) {
					if (gemCarrier.carryAwards[j].uniqueName == uniqueName) {
						legalPrefabs.Add(possiblePrefabs[i]);
						break;
					}
				}
				continue;
			}
		}

		if (legalPrefabs.Count <= 0) {
			return null;
		}

		return legalPrefabs[Random.Range(0, legalPrefabs.Count)];
	}


	public int maxConcurrentWaves = 6;

	public EnemyWave SpawnEnemy(GameObject enemyPrefab, float distance, bool startMoving, bool isLeft, bool isDynamic = false) {
		var playerDistance = SpeedController.s.currentDistance;
		var wave = Instantiate(enemyPrefab, Vector3.forward * (distance - playerDistance), Quaternion.identity).GetComponent<EnemyWave>();
		wave.transform.SetParent(transform);
		wave.SetUp(distance, startMoving, isLeft);
		waves.Add(wave);
		
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


	public Vector2 newMiniWaveRandomTime = new Vector2(5,10);
	public float curMiniWaveTime;
	public float miniWaveChance = 0.2f;
	public Vector2Int miniWaveSize = new Vector2Int(1, 5);

	public float miniWaveChanceMultiplier = 1;
	public float miniWaveCountdownSpeed = 1;
	public float maxWaveCountMultiplier = 1;

	public void SetWarpingMode(bool isWarping) {
		if (isWarping) {
			miniWaveChanceMultiplier = 3;
			miniWaveCountdownSpeed = 2;
			maxWaveCountMultiplier = 1.5f;
		} else {
			miniWaveChanceMultiplier = 1;
			miniWaveCountdownSpeed = 1;
			maxWaveCountMultiplier = 1;
		}
	}
	
	void Update() {
		if (PlayStateMaster.s.isCombatInProgress()) {
			if (SpeedController.s.currentDistance < 30) {
				curMiniWaveTime = 5;
			}
		}
		
		if (PlayStateMaster.s.isCombatInProgress() && enemiesInitialized) {
			var playerDistance = SpeedController.s.currentDistance;

			for (int i = 0; i < waves.Count; i++) {
				waves[i].UpdateBasedOnDistance(playerDistance);
			}


			if (debugNoRegularSpawns)
				return;

			if (waves.Count < maxConcurrentWaves*maxWaveCountMultiplier) {
				if (encounterMode) {
					return;
				}

				curMiniWaveTime -= Time.deltaTime*miniWaveCountdownSpeed;
				if (dynamicWaves.Count == 0) {
					curMiniWaveTime -= Time.deltaTime*5f;
				}
				
				if (curMiniWaveTime <= 0) {
					curMiniWaveTime = Random.Range(newMiniWaveRandomTime.x, newMiniWaveRandomTime.y);

					if (Random.value < (miniWaveChance*miniWaveChanceMultiplier) || dynamicWaves.Count == 0) {
						var enemyPrefab = PlayStateMaster.s.currentLevel.dynamicBattalions[Random.Range(0, PlayStateMaster.s.currentLevel.dynamicBattalions.Length)];
						var dynamicWave = SpawnEnemy(enemyPrefab, SpeedController.s.currentDistance - 30, true,Random.value < 0.5f, true);
						
						dynamicWave.GetComponentInChildren<DynamicSpawnEnemies>().SpawnEnemies(Random.Range(miniWaveSize.x, miniWaveSize.y+1));
					}
				}
				
			}
		} else {
			var playerDistance = SpeedController.s.currentDistance;

			for (int i = 0; i < waves.Count; i++) {
				waves[i].UpdateBasedOnDistance(playerDistance);
			}
		}

		UpdateEnemyFlocks();
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
		waves.Clear();
		dynamicWaves.Clear();
		enemiesInitialized = false;
	}
	
	
	// Flocking behavior controller
	
	// inspired by https://github.com/beneater/boids/blob/master/boids.js

	public List<EnemySwarmMaker> enemySwarmMakers = new List<EnemySwarmMaker>();
	public void AddExistingSwarmsForTesting() {
		var _enemySwarmMakers = GetComponentsInChildren<EnemySwarmMaker>();

		for (int i = 0; i < _enemySwarmMakers.Length; i++) {
			AddEnemySwarmMaker(_enemySwarmMakers[i]);
		}
	}

	public void AddEnemySwarmMaker(EnemySwarmMaker swarm) {
		enemySwarmMakers.Add(swarm);
		if (!swarm.unsquished) {
			StartCoroutine(UnsquishFlock(swarm));
		}
	}

	public void RemoveEnemySwarmMaker(EnemySwarmMaker swarm) {
		enemySwarmMakers.Remove(swarm);
	}


	IEnumerator UnsquishFlock(EnemySwarmMaker flock) {
		yield return null; // give time for the Start() function to run on the flock
		var enemies = flock.activeEnemies;

		for (int i = 0; i < enemies.Count; i++) {
			// unsquish
			if (enemies.Count > 1) {
				var random = Random.onUnitSphere;
				random.y = 0;
				random = random.normalized * Random.Range(0.5f, 1.5f);
				//Debug.DrawLine(enemies[i].transform.position, enemies[i].transform.position + random, Color.yellow, 1f);
				enemies[i].transform.position += random;
			}
		}

		flock.unsquished = true;
	}

	public void UpdateEnemyFlocks() {
		var allEnemies = new List<EnemyInSwarm>();
		
		for (int i = 0; i < enemySwarmMakers.Count; i++) {
			var swarm = enemySwarmMakers[i];
			
			CalculateSwarmCenterAndVelocity(swarm);
			
			allEnemies.AddRange(swarm.activeEnemies);
		}
		
		for (int j = 0; j < allEnemies.Count; j++) {
			var enemy = allEnemies[j];
				
			FlyTowardsCenter(enemy);
			MatchVelocity(enemy);
			AvoidOthers(enemy, allEnemies);
			NormalizeSpeed(enemy);
			KeepWithinBounds(enemy);
			MoveWithVelocity(enemy);
		}
	}

	void CalculateSwarmCenterAndVelocity(EnemySwarmMaker swarm) {
		var position = Vector3.zero;
		var velocity = Vector3.zero;
		
		for (int j = 0; j < swarm.activeEnemies.Count; j++) {
			position += swarm.activeEnemies[j].transform.position;
			velocity += swarm.activeEnemies[j].boilRealDelta;
		}

		var count = swarm.activeEnemies.Count;
		var averagePosition = position / count;
		var averageVelocity = velocity / count;

		swarm.swarmCenter = averagePosition;

		swarm.swarmAverageVelocity = Vector3.RotateTowards(swarm.swarmAverageVelocity, averageVelocity, 1f * Time.deltaTime, 0.05f*Time.deltaTime);

		//Debug.DrawLine(swarm.swarmCenter, swarm.swarmCenter + swarm.swarmAverageVelocity, Color.red);
		//Debug.DrawLine(swarm.swarmCenter, swarm.swarmCenter + averageVelocity, Color.magenta);
	}

	void FlyTowardsCenter(EnemyInSwarm boid) {
		float centeringFactor = 3f * Time.deltaTime;

		var centerDirection =  boid.mySwarm.swarmCenter - boid.transform.position;

		boid.boidTargetDelta += centerDirection * centeringFactor;
	}
	
	void AvoidOthers(EnemyInSwarm boid, List<EnemyInSwarm> allEnemies) {
		float minDistance = 0.6f; // we willd add boid radius to this.
		float avoidFactor = 60f * Time.deltaTime;

		Vector3 pushForce = Vector3.zero;

		for (int i = 0; i < allEnemies.Count; i++) {
			var otherBoid = allEnemies[i];
			if(boid == otherBoid)
				continue;
			
			var closestPointOnMe = boid.mainCollider.ClosestPoint(otherBoid.transform.position);
			var closestPointOnOtherBoid = otherBoid.mainCollider.ClosestPoint((boid.transform.position));
			var otherBoidToOurBoidVector =  closestPointOnMe - closestPointOnOtherBoid;
			otherBoidToOurBoidVector.y = 0;
			var distance = otherBoidToOurBoidVector.magnitude;
			
			otherBoidToOurBoidVector =  boid.transform.position - otherBoid.transform.position ;

			if (distance < minDistance) {
				// make shorter distance more important
				var multiplier = (minDistance - distance);
				multiplier += 0.7f;
				multiplier = multiplier * multiplier;
				multiplier -= 0.7f;

				if (distance <= 0f) {
					otherBoidToOurBoidVector = Random.onUnitSphere;
				}

				if (multiplier < 0) {
					multiplier = 0;
				}

				/*if (multiplier > 0) {
					Debug.DrawLine(closestPointOnOtherBoid, closestPointOnMe, new Color(multiplier, 0, 0));
				} else {
					Debug.DrawLine(closestPointOnOtherBoid, closestPointOnMe, new Color(0, -multiplier, 0));
				}*/

				pushForce += otherBoidToOurBoidVector.normalized * multiplier ;
			}
		}

		boid.boidTargetDelta += pushForce * avoidFactor;
	}
	
	void MatchVelocity(EnemyInSwarm boid) {
		float matchingFactor = 2f * Time.deltaTime;
		
		var velocityDifference =  boid.mySwarm.swarmAverageVelocity.normalized - boid.boidTargetDelta;

		boid.boidTargetDelta += velocityDifference * matchingFactor;
	}
	
	void NormalizeSpeed(EnemyInSwarm boid) {
		float speedLimit = 1f;

		boid.boidTargetDelta.y = 0;
		var speed = boid.boidTargetDelta.magnitude;

		if (speed > speedLimit) {
			boid.boidTargetDelta = boid.boidTargetDelta.normalized * speedLimit;
		}

		if (speed < speedLimit) {
			speed = Mathf.MoveTowards(speed, speedLimit, 0.2f * Time.deltaTime);
			boid.boidTargetDelta = boid.boidTargetDelta.normalized * speed;
		}
	}
	
	void KeepWithinBounds(EnemyInSwarm boid) {
		float turnFactor = 4f * Time.deltaTime;
		float speedLimit = 2f;
		
		var currentPosition = boid.transform.position;
		var pointInBounds = boid.mySwarm.GetComponent<Collider>().ClosestPoint(currentPosition);

		var delta = pointInBounds - currentPosition;

		delta.y = 0;

		var magnitude = delta.magnitude;
		magnitude = Mathf.Clamp(magnitude, 0f, 1f);

		if (magnitude > 0.01f) {
			//boid.boidMoveDelta = Vector3.MoveTowards(boid.boidMoveDelta, delta.normalized*speedLimit, turnFactor);
			boid.boidTargetDelta += delta.normalized*magnitude;
		}
	}

	void MoveWithVelocity(EnemyInSwarm boid) {
		boid.boilRealDelta = Vector3.MoveTowards(boid.boilRealDelta,boid.boidTargetDelta, 1f*Time.deltaTime);
		boid.transform.position += boid.boilRealDelta * Time.deltaTime * 0.2f;

		//Debug.DrawLine(boid.transform.position, boid.transform.position + boid.boilRealDelta, Color.green);
		//Debug.DrawLine(boid.transform.position, boid.transform.position + boid.boidTargetDelta, Color.blue);
	}
}
