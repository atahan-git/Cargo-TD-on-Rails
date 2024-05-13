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
		public float regenMultiplier = 1f;
		public float regenTimerReductionMultiplier = 1f;
		public float regenTimerReductionDivider = 1f;
		public float shieldMoveSpeedReducer = 1;
		public float shieldMoveSpeedIncreaser= 1;
		public float currentMaxShieldAmount = 500;
		public int shieldCoverage = 2; // covering this+2 carts
	}

	private ModuleHealth myHealth;
	public PhysicalShieldBar myPhysicalShieldBar;

	private void Start() {
		myHealth = GetComponentInParent<ModuleHealth>();
		myHealth.myProtector = this;
	}

	private void Update() {
		if (currentShieldAmount > 0) {
			if (curRegenTimer > 0) {
				curRegenTimer -= Time.deltaTime * currentAffectors.regenTimerReductionMultiplier * (1/currentAffectors.regenTimerReductionDivider);
			} else {
				currentShieldAmount += regenSpeed * Time.deltaTime * currentAffectors.regenMultiplier;
			}
		} else {
			if (curRegenTimer > 0) {
				curRegenTimer -= Time.deltaTime * currentAffectors.regenTimerReductionMultiplier * (1/currentAffectors.regenTimerReductionDivider);
			} else {
				myHealth.myProtector = this;
				currentShieldAmount = currentAffectors.currentMaxShieldAmount;
			}
		}
		
		myPhysicalShieldBar.UpdateShieldPercent(currentShieldAmount/currentAffectors.currentMaxShieldAmount);
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
		currentAffectors.currentMaxShieldAmount = baseShieldAmount;
		
		if (PlayStateMaster.s.isCombatInProgress()) {
			currentShieldAmount = Mathf.Clamp(currentShieldAmount, 0, currentAffectors.currentMaxShieldAmount);
		} else {
			currentShieldAmount = currentAffectors.currentMaxShieldAmount;
		}


		currentAffectors = new Affectors();
		
		SetShieldSize();
	}

	public void SetShieldSize() {
		myPhysicalShieldBar.SetSize(currentAffectors.shieldCoverage);
	}
}
