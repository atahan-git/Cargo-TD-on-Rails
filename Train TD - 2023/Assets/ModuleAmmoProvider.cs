using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Now obsolete
/// </summary>
public class ModuleAmmoProvider : ActivateWhenAttachedToTrain,IBooster, IResetState {

    public int boostReloadCountPerLevel = 1;
    
    [Space]
    public float boostFireDamageBase = 0;
    public float boostFireDamagePercentPerLevel = 0;
    public float activeFireDamageBoost = 0;
    
    [Space]
    public float boostExplosionRangeBase = 0;
    public float boostExplosionRangePercentPerLevel = 0;
    public float activeExplosionRangeBoost = 0;
    public void ResetState(int level) {
        PlayerWorldInteractionController.s.reloadAmountPerClickBoost += (boostReloadCountPerLevel*level);
        activeFireDamageBoost = boostFireDamageBase + (boostFireDamagePercentPerLevel * level);
        activeExplosionRangeBoost = boostExplosionRangeBase + (boostExplosionRangePercentPerLevel * level);
        
        rangeBoost = level;
    }

    protected override void _AttachedToTrain() {
        for (int i = 1; i < GetRange()+1; i++) {
            ApplyBoost(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()), true);
            ApplyBoost(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()), true);
        }
    }

    protected override bool CanApply(Cart target) {
        return target.GetComponentInChildren<GunModule>() != null;
    }

    protected override void _ApplyBoost(Cart target, bool doApply) {
        if (doApply) {
            foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
                gunModule.regularToBurnDamageConversionMultiplier += activeFireDamageBoost;
                gunModule.regularToRangeConversionMultiplier += activeExplosionRangeBoost;
            }

        }
    }

    public int baseRange = 1;
    public int rangeBoost = 0;
    protected override void _DetachedFromTrain() {
        // do nothing
    }

    public void ModifyStats(int range, float value) {
        rangeBoost += range;
    }

    public int GetRange() {
        return Mathf.Min(Train.s.carts.Count, baseRange + rangeBoost );
    }

    [ColorUsageAttribute(true, true)] public Color ammoColor = Color.red;
    public Color GetColor() {
        return ammoColor;
    }
}
