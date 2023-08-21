using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldGeneratorModule : ActivateWhenAttachedToTrain, IResetState, IExtraInfo, IBooster {

	public int baseIncreaseMaxShieldsAmount = 500;
	public int increasePerLevel = 500;
	public int increaseMaxShieldsAmount = 500;

	private ModuleHealth myHealth;
	protected override void _AttachedToTrain() {
		myHealth = GetComponentInParent<ModuleHealth>();
		Activation(myHealth.IsShieldActive());
	}
	
	
	public void ProtectFromDamage(float damage) {
		GetComponentInParent<ModuleHealth>().DealDamage(damage);
	}
    
	public void ProtectFromBurn(float damage) {
		GetComponentInParent<ModuleHealth>().BurnDamage(damage);
	}

	protected override bool CanApply(Cart target) {
		var health = target.GetComponentInChildren<ModuleHealth>();
		var shield = target.GetComponentInChildren<ShieldGeneratorModule>();
		return health != null && shield == null;
	}

	protected override void _ApplyBoost(Cart target, bool doApply) {
		var health = target.GetComponentInChildren<ModuleHealth>();

		if (doApply) {
			health.damageDefenders.Add(ProtectFromDamage);
			health.burnDefenders.Add(ProtectFromBurn);
		} else {
			health.damageDefenders.Remove(ProtectFromDamage);
			health.burnDefenders.Remove(ProtectFromBurn);
		}
		
		/*if (doApply) {
			target.GetHealthModule().maxShields += increaseMaxShieldsAmount;

			if (PlayStateMaster.s.isShopOrEndGame()) {
				target.GetHealthModule().currentShields = target.GetHealthModule().maxShields;
			}
			
			target.GetHealthModule().curShieldDelay = 1f;
		}*/
	}

	protected override void _DetachedFromTrain() {
		myHealth = null;
	}

	
	public string GetInfoText() {
		return $"Protects {GetRange()} nearby carts from damage as long as has shield";
	}

	public int baseRange = 1;
	public int rangeBoost = 0;
	public float boostMultiplier = 1;

	public void ResetState(int level) {
		increaseMaxShieldsAmount = baseIncreaseMaxShieldsAmount + (level * increasePerLevel);
		rangeBoost = level;
		boostMultiplier = 1;
		
		GetComponentInParent<Cart>().GetComponentInChildren<PhysicalShieldBar>().SetSize(GetRange());
	}

	public void ModifyStats(int range, float value) {
		rangeBoost += range;
		boostMultiplier += value;
	}
	
	public int GetRange() {
		return Mathf.Min(Train.s.carts.Count, baseRange + rangeBoost);
	}

	[ColorUsageAttribute(true, true)] public Color boostRangeColor = Color.yellow;
	public Color GetColor() {
		return boostRangeColor;
	}


	private void Update() {
		if (myHealth != null) {
			if (isActive && !myHealth.IsShieldActive()) {
				Activation(false);
			}else if (!isActive && myHealth.IsShieldActive()) {
				Activation(true);
			}
		}
	}


	private bool isActive;
	void Activation(bool doActivate) {
		isActive = doActivate;
		for (int i = 1; i < GetRange()+1; i++) {
			ApplyBoost(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()), doActivate);
			ApplyBoost(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()), doActivate);
		}
	}
}
