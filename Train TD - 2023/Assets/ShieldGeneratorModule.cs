using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldGeneratorModule : MonoBehaviour, IResetState {

	public int baseShieldAmount = 500;
	public float currentShieldAmount = 500;

	public float curRegenTimer = 0.5f;
	public float regenSpeed = 50f;
	
	public Affectors currentAffectors;
	[Serializable]
	public class Affectors {
		public float power = 1f;
		public float speed = 1;
		public float efficiency = 1;

		public float uranium = 0;
		public int iron = 0;
	}

	private ModuleHealth myHealth;
	public PhysicalShieldBar myPhysicalShieldBar;

	private void Start() {
		myHealth = GetComponentInParent<ModuleHealth>();
		myHealth.myProtector = this;
	}

	private void Update() {
		if (currentShieldAmount <= 0) {
			if (curRegenTimer <= 0) {
				myHealth.myProtector = this; // this will be true for one frame only
			} 
		} 
		
		if (curRegenTimer > 0) {
			curRegenTimer -= Time.deltaTime * (1f/currentAffectors.efficiency);
		} else {
			currentShieldAmount += regenSpeed * currentAffectors.speed;
		}

		currentShieldAmount = Mathf.Clamp(currentShieldAmount, 0, GetMaxShields());
		
		myPhysicalShieldBar.UpdateShieldPercent(currentShieldAmount/GetMaxShields());
	}

	public void ProtectFromDamage(float damage) {
		if (currentShieldAmount > 0) {
			currentShieldAmount -= damage;
		}

		curRegenTimer = 0.5f;

		if (currentShieldAmount <= 0) {
			myHealth.myProtector = null;
			currentShieldAmount = 0;
			curRegenTimer = 10;
		}
	}

	public void ResetState() {
		currentAffectors = new Affectors();
		
		if (PlayStateMaster.s.isCombatInProgress()) {
			//currentShieldAmount = Mathf.Clamp(currentShieldAmount, 0, GetMaxShields());
		} else {
			currentShieldAmount = GetMaxShields();
		}

		SetShieldSize();
	}

	public float GetMaxShields() {
		return baseShieldAmount * currentAffectors.power;
	}

	public void SetShieldSize() {
		myPhysicalShieldBar.SetSize(1+currentAffectors.iron);
	}
}
