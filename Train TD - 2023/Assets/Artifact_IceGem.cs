using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_IceGem : ActivateWhenOnArtifactRow, IResetStateArtifact
{
    
    //[Space]
    public float boostIceDamageBase = 1;
    public float boostIceDamageIncreasePerLevel = 0.5f;
    public float activeIceDamageBoost = 0;

    public GameObject[] iceShards;

    public GameObject currentIceShard;
    
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
            //gunModule.regularToBurnDamageConversionMultiplier += activeFireDamageBoost;
            didApply = true;
        }

        foreach (var roboRepair in target.GetComponentsInChildren<RoboRepairModule>()) {
            roboRepair.extraPrefabToSpawnOnAffected.Add(currentIceShard);
            didApply = true;
        }
        
        foreach (var trainGemBridge in target.GetComponentsInChildren<TrainGemBridge>()) {
            trainGemBridge.extraPrefabToSpawnOnAffected.Add(currentIceShard);
            didApply = true;
        }
        
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            shieldGenerator.prefabToSpawnWhenShieldIsHit.Add(currentIceShard);
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
        activeIceDamageBoost = boostIceDamageBase + (boostIceDamageIncreasePerLevel * level);
        currentIceShard = iceShards[level];
    }

    /*public string GetInfoText() {
        return "When leveled up slows down enemies more and shots more ice shards";
    }*/
}
