using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoBoostModule : ActivateWhenAttachedToTrain, IExtraInfo, IBooster, IResetState {
	public float ammoBoost = 1;
	
	protected override void _AttachedToTrain() {
		for (int i = 1; i < GetRange()+1; i++) {
			ApplyBoost(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()), true);
			ApplyBoost(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()), true);
		}
	}

	protected override bool CanApply(Cart target) {
		var ammo = target.GetComponentInChildren<ModuleAmmo>();
		return ammo != null;
	}

	protected override void _ApplyBoost(Cart target, bool doApply) {
		var ammo = target.GetComponentInChildren<ModuleAmmo>();
		if (doApply) {
			ammo.ChangeMaxAmmo(ammoBoost*boostMultiplier);
		} 
	}

	protected override void _DetachedFromTrain() {
		//do nothing
	}

	public string GetInfoText() {
		return "Doubles the max ammo of connected carts";
	}

	public int baseRange = 1;
	public int rangeBoost = 0;
	public float boostMultiplier = 1;

	public void ResetState(int level) {
		rangeBoost = level;
		boostMultiplier = 1;
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
}
