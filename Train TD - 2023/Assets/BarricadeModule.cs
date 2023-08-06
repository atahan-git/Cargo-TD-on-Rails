using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarricadeModule : ActivateWhenAttachedToTrain, IExtraInfo,IBooster
{
    public void ProtectFromDamage(float damage) {
        GetComponentInParent<ModuleHealth>().DealDamage(damage);
    }
    
    public void ProtectFromBurn(float damage) {
        GetComponentInParent<ModuleHealth>().BurnDamage(damage);
    }
    
    
    protected override void _AttachedToTrain() {
        for (int i = 1; i < GetRange()+1; i++) {
            ApplyBoost(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()), true);
            ApplyBoost(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()), true);
        }
    }
    
    protected override bool CanApply(Cart target) {
        var health = target.GetComponentInChildren<ModuleHealth>();
        var barricade = target.GetComponentInChildren<BarricadeModule>();
        return health != null && barricade == null;
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
    }

    protected override void _DetachedFromTrain() {
        //do nothing
    }

    public string GetInfoText() {
        return "Redirects damage from attached carts to itself";
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

    [ColorUsageAttribute(true, true)] public Color boostRangeColor = Color.blue;
    public Color GetColor() {
        return boostRangeColor;
    }
}
