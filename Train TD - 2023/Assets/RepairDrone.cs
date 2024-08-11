using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairDrone : MonoBehaviour {

	public float currentChargePercent = 1f;
	public bool needToFullyCharge = false;

	public GameObject repairParticles;

	private ParticleSystem[] particles;

	public bool prevState;

	private MiniGUI_ShowRepairDroneChargePercent _chargePercent;
	private void Start() {
		particles = GetComponentsInChildren<ParticleSystem>();
		prevState = true;
		SetCurrentlyRepairingState(false);
		_chargePercent = GetComponentInChildren<MiniGUI_ShowRepairDroneChargePercent>(true);
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


	private void Update() {
		_chargePercent.SetPercent(currentChargePercent);
	}
}
