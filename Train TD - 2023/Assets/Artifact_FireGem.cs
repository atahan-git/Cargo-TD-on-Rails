using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Artifact_FireGem : ActivateWhenOnArtifactRow, IResetStateArtifact
{
    
    //[Space]
    public float boostFireDamageBase = 1;
    public float boostFireDamagePercentPerLevel = 0.5f;
    public float activeFireDamageBoost = 0;

    public GameObject[] fireBursts;

    public GameObject currentFireBurst;
    
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
            gunModule.regularToBurnDamageConversionMultiplier += activeFireDamageBoost;
            didApply = true;
        }

        foreach (var roboRepair in target.GetComponentsInChildren<RoboRepairModule>()) {
            roboRepair.extraPrefabToSpawnOnAffected.Add(currentFireBurst);
            didApply = true;
        }
        
        foreach (var trainGemBridge in target.GetComponentsInChildren<TrainGemBridge>()) {
            trainGemBridge.extraPrefabToSpawnOnAffected.Add(currentFireBurst);
            didApply = true;
        }
        
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            shieldGenerator.prefabToSpawnWhenShieldIsHit.Add(currentFireBurst);
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
        activeFireDamageBoost = boostFireDamageBase + (boostFireDamagePercentPerLevel * level);
        currentFireBurst = fireBursts[level];
    }

    /*public string GetInfoText() {
        return "When leveled up deals more fire damage and causes bigger bursts";
    }*/
}
