using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyFlockingController : MonoBehaviour {
	public static EnemyFlockingController s;

	private void Awake() {
		s = this;
	}
	
	// Flocking behavior controller
    // inspired by https://github.com/beneater/boids/blob/master/boids.js

	public List<EnemySwarmMaker> enemySwarmMakers => EnemyWavesController.s.enemySwarmMakers;
	
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

	private List<EnemyInSwarm> allEnemies = new List<EnemyInSwarm>(); 
	public void UpdateEnemyFlocks() {
		if (LevelReferences.s.speed < 0.2f) {
			return;
		}
		
		allEnemies.Clear();
		
		for (int i = 0; i < enemySwarmMakers.Count; i++) {
			var swarm = enemySwarmMakers[i];
			
			CalculateSwarmCenterAndVelocity(swarm);
			
			allEnemies.AddRange(swarm.activeEnemies);
		}

		for (int i = 0; i < allEnemies.Count; i++) {
			allEnemies[i].boidPosition = allEnemies[i].transform.position;
		}
		
		for (int j = 0; j < allEnemies.Count; j++) {
			var enemy = allEnemies[j];
				
			FlyTowardsCenter(enemy);
			MatchVelocity(enemy);
			AvoidOthers(enemy);
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
	
	void AvoidOthers(EnemyInSwarm boid) {
		float minDistance = 0.6f; // we willd add boid radius to this.
		float avoidFactor = 60f * Time.deltaTime;

		Vector3 pushForce = Vector3.zero;

		for (int i = 0; i < allEnemies.Count; i++) {
			var otherBoid = allEnemies[i];
			if(boid == otherBoid)
				continue;
			
			var closestPointOnMe = boid.mainCollider.ClosestPoint(otherBoid.boidPosition);
			var closestPointOnOtherBoid = otherBoid.mainCollider.ClosestPoint(boid.boidPosition);
			//var closestPointOnMe = boid.boidPosition;
			//var closestPointOnOtherBoid = otherBoid.boidPosition;
			var otherBoidToOurBoidVector =  closestPointOnMe - closestPointOnOtherBoid;
			otherBoidToOurBoidVector.y = 0;
			var distance = otherBoidToOurBoidVector.magnitude;
			
			otherBoidToOurBoidVector =  boid.boidPosition - otherBoid.boidPosition ;

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
