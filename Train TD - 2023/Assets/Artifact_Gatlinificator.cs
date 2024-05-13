using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_Gatlinificator : MonoBehaviour, IChangeStateToEntireTrain
{

    public void ChangeStateToEntireTrain(List<Cart> carts) {
        for (int i = 0; i < carts.Count; i++) {
            ApplyBoost(carts[i]);
        }
    }

    void ApplyBoost(Cart target) {
        if(target == null)
            return;
        
        
        
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            if (gunModule.isGigaGatling) {
                gunModule.currentAffectors.fireRateMultiplier += 1.5f;
            } else {
                gunModule.currentAffectors.gatlinificator = true;
                gunModule.currentAffectors.fireRateDivider += 1f;
            }

            gunModule.ammoPerBarrageMultiplier = 0.5f;
        }

        /*foreach (var roboRepair in target.GetComponentsInChildren<RoboRepairModule>()) {
            roboRepair.amountDivider += 1;
            roboRepair.firerateMultiplier += 1;
        }
        
        /*foreach (var trainGemBridge in target.GetComponentsInChildren<TrainGemBridge>()) {
        }#1#
        
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            var hp = target.GetHealthModule();
            
            /*hp.maxShields /= 2;
            hp.shieldRegenDelayMultiplier += 1;
            hp.shieldRegenRateMultiplier += 1;#1#
        }*/
    }

}