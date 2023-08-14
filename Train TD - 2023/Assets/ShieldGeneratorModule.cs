using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldGeneratorModule : ActivateWhenAttachedToTrain, IResetState {

	public int baseIncreaseMaxShieldsAmount = 500;
	public int increasePerLevel = 500;
	public int increaseMaxShieldsAmount = 500;
	protected override void _AttachedToTrain() {
		for (int i = 0; i < Train.s.carts.Count; i++) {
			ApplyBoost(Train.s.carts[i], true);
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

	public void ResetState(int level) {
		increaseMaxShieldsAmount = baseIncreaseMaxShieldsAmount + (level * increasePerLevel);
	}
}
