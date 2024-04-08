using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_IronGem : ActivateWhenOnArtifactRow, IResetStateArtifact, IApplyToEnemyWithGem
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

        foreach (var droneRepair in target.GetComponentsInChildren<DroneRepairController>()) {
            droneRepair.repairRateIncreaseReducer += 0.25f;
            droneRepair.additionalRepairs += 1;
            didApply = true;
        }
        
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            shieldGenerator.regenTimerReductionDivider += 0.25f;
            shieldGenerator.currentMaxShieldAmount *= 1.5f;
            didApply = true;
        }
        
        foreach (var ammoDirect in target.GetComponentsInChildren<AmmoDirectController>()) {
            ammoDirect.reloadAmountMultiplier += 1f;
            ammoDirect.moveSpeedReducer += 0.5f;
            didApply = true;
        }

        foreach (var engine in target.GetComponentsInChildren<EngineModule>()) {
            engine.extraEnginePower += 1;
            engine.extraSpeedAdd += trainSlowAmount;

            didApply = true;
        }

        
        if (!target.GetHealthModule().glassCart) {
            target.GetHealthModule().maxHealth += curEngineHpIncrease;
            target.GetHealthModule().currentHealth += curEngineHpIncrease;
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
    public void ApplyToEnemyWithGem(EnemyInSwarm enemy) {
        foreach (var gunModule in enemy.GetComponentsInChildren<GunModule>()) {
            gunModule.damageMultiplier += curDamageMultiplier;
            gunModule.fireRateDivider += baseFirerateReduction;
        }
    }
}
