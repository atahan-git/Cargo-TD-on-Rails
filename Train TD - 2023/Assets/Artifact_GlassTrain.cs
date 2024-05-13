using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_GlassTrain: MonoBehaviour, IChangeStateToEntireTrain
{

    public void ChangeStateToEntireTrain(List<Cart> carts) {
        for (int i = 0; i < carts.Count; i++) {
            ApplyBoost(carts[i]);
        }
    }

    void ApplyBoost(Cart target) {
        /*if(target == null)
            return;
        
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.damageMultiplier += 1;
        }

        foreach (var roboRepair in target.GetComponentsInChildren<RoboRepairModule>()) {
            roboRepair.amountMultiplier += 1;
        }

        /*foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            target.GetHealthModule().maxShields *= 2;
        }#1#

        foreach (var engineModule in target.GetComponentsInChildren<EngineModule>()) {
            engineModule.extraSpeedAdd += 0.1f;
        }

        
        
        target.GetHealthModule().maxHealth = 100;
        target.GetHealthModule().currentHealth = 100;
        target.GetHealthModule().glassCart = true;*/
    }
}
