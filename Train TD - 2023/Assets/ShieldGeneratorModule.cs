using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldGeneratorModule : ActivateWhenAttachedToTrain, IResetState, IExtraInfo, IBooster {

	public int baseIncreaseMaxShieldsAmount = 500;
	public int increasePerLevel = 500;
	public int increaseMaxShieldsAmount = 500;
	protected override void _AttachedToTrain() {
		for (int i = 1; i < GetRange()+1; i++) {
			ApplyBoost(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()), true);
			ApplyBoost(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()), true);
		}
	}

	protected override bool CanApply(Cart target) {
		return target.GetHealthModule() != null;
	}

	protected override void _ApplyBoost(Cart target, bool doApply) {
		if (doApply) {
			target.GetHealthModule().maxShields += increaseMaxShieldsAmount;

			if (PlayStateMaster.s.isShopOrEndGame()) {
				target.GetHealthModule().currentShields = target.GetHealthModule().maxShields;
			}
			
			target.GetHealthModule().curShieldDelay = 1f;
		}
	}

	protected override void _DetachedFromTrain() {
		
	}

	
	public string GetInfoText() {
		return $"Adds {increaseMaxShieldsAmount} max shields to {GetRange()} nearby carts";
	}

	public int baseRange = 1;
	public int rangeBoost = 0;
	public float boostMultiplier = 1;

	public void ResetState(int level) {
		increaseMaxShieldsAmount = baseIncreaseMaxShieldsAmount + (level * increasePerLevel);
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
