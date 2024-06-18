using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Act2DemoEndCrystalCollision : MonoBehaviour, IShowOnDistanceRadar {
	private float myDistance = 0;
	public void SetUp(float distance) {
		myDistance = distance;
		Update();
		DistanceAndEnemyRadarController.s.RegisterUnit(this);
	}


	private void Update() {
		transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(myDistance-SpeedController.s.currentDistance);
		transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(myDistance-SpeedController.s.currentDistance);
		
	}

	public void OnCollisionEnter(Collision collision) {
		if (!MissionLoseFinisher.s.isMissionLost && collision.collider.GetComponentInParent<Train>()) {
			Act2DemoEndController.s.CollidedWithCrystal(collision.GetContact(0).point);
			GetComponentInChildren<MeshCollider>().enabled = false;
		}
	}

	public Sprite radarIcon;

	public bool IsTrain() {
		return false;
	}

	public float GetDistance() {
		return myDistance;
	}

	public Sprite GetIcon() {
		return radarIcon;
	}

	public bool isLeftUnit() {
		return true;
	}
}
