using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuggyBullet : MonoBehaviour {
	public GameObject spawnBattalion;


	private void Start() {
		Invoke(nameof(EnableCollisions), 0.2f);
	}

	void EnableCollisions() {
		GetComponent<Rigidbody>().detectCollisions = true;
	}

	bool isSpawned = false;
	private void OnCollisionEnter(Collision collision) {
		if (isSpawned) {
			return;
		}
	    var ground = collision.gameObject.GetComponent<TrainTerrainData>();
	    if (ground != null) {
		    var enemy = EnemyWavesController.s.SpawnCustomBattalion(spawnBattalion, SpeedController.s.currentDistance, true, true);

		    var car = enemy.GetComponentInChildren<CarLikeMovementOffsetsController>();
		    car.transform.position = transform.position;
		    car.transform.rotation = transform.rotation;

		    var health = enemy.GetComponentInChildren<EnemyHealth>();
		    var myHealth = GetComponent<EnemyHealth>();

		    health.currentHealth = health.currentHealth;
		    health.currentBurn = myHealth.currentBurn;
		    health.maxBurnTier = myHealth.maxBurnTier;
		    
		    Destroy(gameObject);
		    isSpawned = true;
	    }
    }
}
