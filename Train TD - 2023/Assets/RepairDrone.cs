using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairDrone : MonoBehaviour {

	private ParticleSystem[] particles;

	public bool prevState;

	private void Start() {
		particles = GetComponentsInChildren<ParticleSystem>();
		prevState = true;
		SetCurrentlyRepairingState(false);
	}

	public void SetCurrentlyRepairingState(bool isRepairing) {
		if (isRepairing != prevState) {
			prevState = isRepairing;
			foreach (var particle in particles) {
				if (isRepairing) {
					particle.Play();
				} else {
					particle.Stop();
				}
			}
		}
	}
}
