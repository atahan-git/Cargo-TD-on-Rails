using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_Gatlinificator : ActivateWhenOnArtifactRow
{

    protected override void _Arm() {
        for (int i = 0; i <Train.s.carts.Count; i++) {
            ApplyBoost(Train.s.carts[i]);
        }
    }

    void ApplyBoost(Cart target) {
        if(target == null)
            return;
        
        
        
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            if (gunModule.isGigaGatling) {
                gunModule.fireRateMultiplier += 1.5f;
            } else {
                gunModule.gatlinificator = true;
                gunModule.fireRateDivider += 1f;
            }

            gunModule.ammoPerBarrageMultiplier = 0.5f;
        }

        foreach (var roboRepair in target.GetComponentsInChildren<RoboRepairModule>()) {
            roboRepair.amountDivider += 1;
            roboRepair.firerateMultiplier += 1;
        }
        
        /*foreach (var trainGemBridge in target.GetComponentsInChildren<TrainGemBridge>()) {
        }*/
        
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            var hp = target.GetHealthModule();
            
            hp.maxShields /= 2;
            hp.shieldRegenDelayMultiplier += 1;
            hp.shieldRegenRateMultiplier += 1;
        }
    }

    protected override void _Disarm() {
        // do nothing
    }
}