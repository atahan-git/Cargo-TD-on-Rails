using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_ExplosiveGem: ActivateWhenOnArtifactRow, IResetStateArtifact, IApplyToEnemyWithGem
{
    
    //[Space]
    public float boostExplosionRangeBase = 0.01f;
    public float boostExplosionRangeLevelIncrease = 0.005f;
    public float activeRangeBoost = 0;
    
    public float explosiveRepairBase = 0.25f;
    public float explosiveRepairIncreasePerLevel = 0.25f;
    public float activeExplosiveRepair = 0;

    public GameObject[] shieldMiniExplosions;

    public GameObject currentShieldExplosion;
    
    public GameObject[] trainGigaExplosions;
    public GameObject currentGigaExplosion;
    
    
    public GameObject[] trainMiniExplosions;
    public GameObject currentMiniExplosion;
    
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
            gunModule.regularToRangeConversionMultiplier += activeRangeBoost;
            didApply = true;
        }

        foreach (var roboRepair in target.GetComponentsInChildren<RoboRepairModule>()) {
            roboRepair.explosiveRepair = true;
            roboRepair.explosiveRepairAmount = activeExplosiveRepair;
            didApply = true;
        }
        
        foreach (var trainGemBridge in target.GetComponentsInChildren<TrainGemBridge>()) {
            didApply = true;
        }
        
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            shieldGenerator.prefabToSpawnWhenShieldIsDestroyed.Add(currentShieldExplosion);
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
        activeRangeBoost = boostExplosionRangeBase + (boostExplosionRangeLevelIncrease * level);
        currentShieldExplosion = shieldMiniExplosions[level];
        currentGigaExplosion = trainGigaExplosions[level];
        currentMiniExplosion = trainMiniExplosions[level];
        activeExplosiveRepair = explosiveRepairBase + (explosiveRepairIncreasePerLevel * level);
    }

    /*public string GetInfoText() {
        return "When leveled up deals more fire damage and causes bigger bursts";
    }*/

    public void ApplyToEnemyWithGem(EnemyInSwarm enemy) {
        foreach (var gunModule in enemy.GetComponentsInChildren<GunModule>()) {
            gunModule.regularToRangeConversionMultiplier += activeRangeBoost;
        }
    }
}
