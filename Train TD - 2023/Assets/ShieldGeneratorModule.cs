using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldGeneratorModule : MonoBehaviour, IResetState, IExtraInfo {

	public int baseShieldAmount = 1000;
	public float currentShieldAmount = 1000;
	public float currentMaxShieldAmount = 1000;
	
	
	public List<GameObject> prefabToSpawnWhenShieldIsHit = new List<GameObject>();
	public List<GameObject> prefabToSpawnWhenShieldIsDestroyed = new List<GameObject>();

	private ModuleHealth myHealth;
	
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
	
	public void ProtectFromDamage(float damage) {
		GetComponentInParent<ModuleHealth>().DealDamage(damage);
	}
    
	public void ProtectFromBurn(float damage) {
		GetComponentInParent<ModuleHealth>().BurnDamage(damage);
	}

	public string GetInfoText() {
		return $"Protects {GetRange()} nearby carts from damage as long as has shield";
	}

	public int baseRange = 1;
	public int rangeBoost = 0;
	public float boostMultiplier = 1;

	public void ResetState() {
		currentMaxShieldAmount = baseShieldAmount;
		
		if (PlayStateMaster.s.isCombatInProgress()) {
			currentShieldAmount = Mathf.Clamp(currentShieldAmount, 0, currentMaxShieldAmount);
		} else {
			currentShieldAmount = currentMaxShieldAmount;
		}
		
		rangeBoost = 0;
		boostMultiplier = 1;
		
		prefabToSpawnWhenShieldIsHit.Clear();
		prefabToSpawnWhenShieldIsDestroyed.Clear();

		//GetComponentInParent<Cart>().GetComponentInChildren<PhysicalShieldBar>().SetSize(GetRange());
	}

	public void ModifyStats(int range, float value) {
		rangeBoost += range;
		boostMultiplier += value;
	}
	
	public int GetRange() {
		return Mathf.Min(Train.s.carts.Count, baseRange + rangeBoost);
	}
}
