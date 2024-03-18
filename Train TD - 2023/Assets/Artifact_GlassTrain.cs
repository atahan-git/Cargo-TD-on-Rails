using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_GlassTrain: ActivateWhenOnArtifactRow
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
            gunModule.damageMultiplier += 1;
        }

        foreach (var roboRepair in target.GetComponentsInChildren<RoboRepairModule>()) {
            roboRepair.amountMultiplier += 1;
        }

        /*foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            target.GetHealthModule().maxShields *= 2;
        }*/

        foreach (var engineModule in target.GetComponentsInChildren<EngineModule>()) {
            engineModule.extraSpeedAdd += 0.1f;
        }

        
        
        target.GetHealthModule().maxHealth = 100;
        target.GetHealthModule().currentHealth = 100;
        target.GetHealthModule().glassCart = true;
    }

    protected override void _Disarm() {
        // do nothing
    }
}
