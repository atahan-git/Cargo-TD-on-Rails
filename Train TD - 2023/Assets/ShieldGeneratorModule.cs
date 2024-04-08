using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldGeneratorModule : MonoBehaviour, IResetState, IExtraInfo {

	public int baseShieldAmount = 500;
	public float currentShieldAmount = 500;
	public float currentMaxShieldAmount = 500;

	public float curRegenTimer = 0.5f;
	public float regenSpeed = 50f;

	public float regenMultiplier = 1f;
	public float regenTimerReductionMultiplier = 1f;
	public float regenTimerReductionDivider = 1f;

	public List<GameObject> prefabToSpawnWhenShieldIsHit = new List<GameObject>();
	public List<GameObject> prefabToSpawnWhenShieldIsDestroyed = new List<GameObject>();

	private ModuleHealth myHealth;
	public PhysicalShieldBar myPhysicalShieldBar;

	private void Start() {
		myHealth = GetComponentInParent<ModuleHealth>();
		myHealth.myProtector = this;
	}

	public void SpawnGemEffect(ModuleHealth target) {
		StartCoroutine(_SpawnGemEffect(target));
	}

	IEnumerator _SpawnGemEffect(ModuleHealth target) {
		foreach (var prefab in prefabToSpawnWhenShieldIsHit) {
			if (target == null) {
				yield break;
			}
            
			Instantiate(prefab, target.GetUITransform());

			yield return new WaitForSeconds(0.1f);
		}
	}

	private void Update() {
		if (currentShieldAmount > 0) {
			if (curRegenTimer > 0) {
				curRegenTimer -= Time.deltaTime * regenTimerReductionMultiplier * (1/regenTimerReductionDivider);
			} else {
				currentShieldAmount += regenSpeed * Time.deltaTime * regenMultiplier;
			}
		} else {
			if (curRegenTimer > 0) {
				curRegenTimer -= Time.deltaTime * regenTimerReductionMultiplier * (1/regenTimerReductionDivider);
			} else {
				myHealth.myProtector = this;
				currentShieldAmount = currentMaxShieldAmount;
			}
		}
		
		myPhysicalShieldBar.UpdateShieldPercent(currentShieldAmount/currentMaxShieldAmount);
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

		SpawnGemEffect(myHealth);
	}

	public string GetInfoText() {
		return $"Protects nearby carts from damage as long as has shield";
	}

	public void ResetState() {
		currentMaxShieldAmount = baseShieldAmount;
		
		if (PlayStateMaster.s.isCombatInProgress()) {
			currentShieldAmount = Mathf.Clamp(currentShieldAmount, 0, currentMaxShieldAmount);
		} else {
			currentShieldAmount = currentMaxShieldAmount;
		}

		prefabToSpawnWhenShieldIsHit.Clear();
		prefabToSpawnWhenShieldIsDestroyed.Clear();


		regenMultiplier = 1;
		regenTimerReductionMultiplier = 1;
		regenTimerReductionDivider = 1;
		//GetComponentInParent<Cart>().GetComponentInChildren<PhysicalShieldBar>().SetSize(GetRange());
	}
}
