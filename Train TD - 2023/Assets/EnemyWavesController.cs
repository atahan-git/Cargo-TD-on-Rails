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
		//AddExistingSwarmsForTesting();
	}
	
	public void SetUpLevel() {
		Cleanup();
	}

	public void SpawnEnemiesOnSegment(float segmentStartDistance, float segmentLength, PathGenerator.PathType type) {
		if (debugNoRegularSpawns)
			return;


		enemiesInitialized = true; // ie for empty segments
		if (enemiesInitialized) {
			var distance = Random.Range(segmentLength / 10f, segmentLength);

			GameObject enemyPrefab;

			switch (type) {
				case PathGenerator.PathType.empty:
					enemyPrefab = null;
					break;
				case PathGenerator.PathType.gunCart:
					enemyPrefab = PlayStateMaster.s.currentLevel.gunCartBattalions[Random.Range(0, PlayStateMaster.s.currentLevel.gunCartBattalions.Length)];
					break;
				case PathGenerator.PathType.utilityCart:
					enemyPrefab = PlayStateMaster.s.currentLevel.utilityCartBattalions[Random.Range(0, PlayStateMaster.s.currentLevel.utilityCartBattalions.Length)];
					break;
				case PathGenerator.PathType.gem:
					enemyPrefab = PlayStateMaster.s.currentLevel.gemBattalions[Random.Range(0, PlayStateMaster.s.currentLevel.gemBattalions.Length)];
					break;
				case PathGenerator.PathType.cargo:
					enemyPrefab = PlayStateMaster.s.currentLevel.cargoBattalions[Random.Range(0, PlayStateMaster.s.currentLevel.cargoBattalions.Length)];
					break;
				default:
					enemyPrefab = null;
					break;
			}

			if (enemyPrefab != null) {
				SpawnEnemy(enemyPrefab, segmentStartDistance + distance, false, Random.value > 0.5f);
			}
		}
	}


	public int maxConcurrentWaves = 6;

	public EnemyWave SpawnEnemy(GameObject enemyPrefab, float distance, bool startMoving, bool isLeft) {
		var playerDistance = SpeedController.s.currentDistance;
		var wave = Instantiate(enemyPrefab, Vector3.forward * (distance - playerDistance), Quaternion.identity).GetComponent<EnemyWave>();
		wave.transform.SetParent(transform);
		wave.SetUp(distance, startMoving, isLeft);
		waves.Add(wave);

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

				curMiniWaveTime -= Time.deltaTime;
				if (curMiniWaveTime <= 0) {
					curMiniWaveTime = Random.Range(newMiniWaveRandomTime.x, newMiniWaveRandomTime.y);

					if (Random.value < miniWaveChance) {
						var enemyPrefab = PlayStateMaster.s.currentLevel.dynamicBattalions[Random.Range(0, PlayStateMaster.s.currentLevel.dynamicBattalions.Length)];
						var dynamicWave = SpawnEnemy(enemyPrefab, SpeedController.s.currentDistance - 50, true,Random.value < 0.5f);
						
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
	}

	public void Cleanup() {
		transform.DeleteAllChildren();
		enemySwarmMakers.Clear();
		waves.Clear();
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
