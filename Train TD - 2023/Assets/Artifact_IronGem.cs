using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_IronGem : ActivateWhenOnArtifactRow, IResetStateArtifact
{
    
    //[Space]
    public float baseDamageMultiplier = 1.5f;
    public float damageMultiplierIncreasePerLevel = 1f;
    public float curDamageMultiplier = 1.5f;
    public float baseFirerateReduction = -0.5f;
    
    public float baseEngineHpIncrease = 500f;
    public float engineHpIncreasePerLevel = 500f;
    public float curEngineHpIncrease = 500f;
    public float trainSlowAmount = -0.1f;
    protected override void _Arm() {
        var range = GetComponent<Artifact>().range;
        ApplyBoost(Train.s.GetNextBuilding(0, GetComponentInParent<Cart>()));
        for (int i = 1; i < range+1; i++) {
            ApplyBoost(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()));
            ApplyBoost(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()));
        }
    }

    void ApplyBoost(Cart target) {
        if(target == null)
            return;
        
        bool didApply = false;
        
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.damageMultiplier += curDamageMultiplier;
            gunModule.fireRateDivider += baseFirerateReduction;
            didApply = true;
        }

        foreach (var roboRepair in target.GetComponentsInChildren<RoboRepairModule>()) {
            roboRepair.amountMultiplier += curDamageMultiplier;
            roboRepair.firerateDivider += baseFirerateReduction;
            didApply = true;
        }
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            var hp = target.GetHealthModule();
            hp.maxShields += curEngineHpIncrease;
            hp.shieldRegenDelayDivider += baseFirerateReduction;
            hp.shieldRegenRateDivider += baseFirerateReduction;
            didApply = true;
        }
        
        foreach (var engine in target.GetComponentsInChildren<EngineModule>()) {
            engine.extraEnginePower += 1;
            engine.extraSpeedAdd += trainSlowAmount;
            var engineHP = target.GetHealthModule();
            if (!engineHP.glassCart) {
                engineHP.maxHealth += curEngineHpIncrease;
                engineHP.currentHealth += curEngineHpIncrease;
            }

            didApply = true;
        }

        if (target.isCargo) {
            if (!target.GetHealthModule().glassCart) {
                target.GetHealthModule().maxHealth += curEngineHpIncrease;
                target.GetHealthModule().currentHealth += curEngineHpIncrease;
            }

            didApply = true;
        }
        
        if (didApply) {
            GetComponent<Artifact>()._ApplyToTarget(target);
        }
    }

    protected override void _Disarm() {
        // do nothing
    }

    public void ResetState(int level) {
        curDamageMultiplier = baseDamageMultiplier + (damageMultiplierIncreasePerLevel * level);
        curEngineHpIncrease = baseEngineHpIncrease + (engineHpIncreasePerLevel * level);
    }

    /*public string GetInfoText() {
        return "When leveled up slows down enemies more and shots more ice shards";
    }*/
}
