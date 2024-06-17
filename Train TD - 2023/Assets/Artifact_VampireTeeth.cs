using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_VampireTeeth : MonoBehaviour, IChangeStateToEntireTrain {

    public float healthMultiplier = 0.5f;
    public void ChangeStateToEntireTrain(List<Cart> carts) {
        for (int i = 0; i < carts.Count; i++) {
            ApplyBoost(carts[i]);
        }
    }

    void ApplyBoost(Cart target) {
        if(target == null)
            return;

        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.currentAffectors.vampiric = true;
        }

        foreach (var roboRepair in target.GetComponentsInChildren<DroneRepairController>()) {
            roboRepair.currentAffectors.vampiric = true;
        }
        
        foreach (var ammoDirect in target.GetComponentsInChildren<AmmoDirectController>()) {
            ammoDirect.currentAffectors.vampiric = true;
        }
        
        foreach (var engineDirect in target.GetComponentsInChildren<EngineDirectController>()) {
            engineDirect.currentAffectors.vampiric = true;
        }


        target.GetHealthModule().MultiplyMaxHealthByAmount(healthMultiplier);
    }

}