using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_TinyTrain : MonoBehaviour, IChangeStateToEntireTrain
{

    public void ChangeStateToEntireTrain(List<Cart> carts) {
        for (int i = 0; i < carts.Count; i++) {
            ApplyBoost(carts[i]);
        }
    }
    
    void ApplyBoost(Cart target) {
        if(target == null)
            return;

        /*foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.damageMultiplier += 0.1f;
        }

        foreach (var moduleAmmo in target.GetComponentsInChildren<ModuleAmmo>()) {
            moduleAmmo.maxAmmoMultiplier += 0.2f;
        }
        
        foreach (var directControllable in target.GetComponentsInChildren<IDirectControllable>()) {
        }

        foreach (var roboRepair in target.GetComponentsInChildren<RoboRepairModule>()) {
            roboRepair.amountMultiplier += 0.1f;
        }

        if (!target.GetHealthModule().glassCart) {
            target.GetHealthModule().maxHealth *= 1.3f;
            target.GetHealthModule().currentHealth *= 1.3f;
        }*/
    }


   
}