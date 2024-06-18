using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_AncientBrain : MonoBehaviour, IChangeStateToEntireTrain
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
            gunModule.currentAffectors.ancientDisabled = true;
            gunModule.DeactivateGun();
            gunModule.currentAffectors.power *= 2;
            gunModule.currentAffectors.speed *= 2;
        }

        foreach (var repairModule in target.GetComponentsInChildren<DroneRepairController>()) {
            repairModule.currentAffectors.ancientDisabled = true;
            repairModule.currentAffectors.power *= 2;
            repairModule.currentAffectors.efficiency *= 2;
            repairModule.currentAffectors.speed *= 2;
        }
        
        foreach (var ammoModule in target.GetComponentsInChildren<AmmoDirectController>()) {
            ammoModule.currentAffectors.power *= 2;
        }
        
        
        /*foreach (var engineModule in target.GetComponentsInChildren<EngineModule>()) {
            
        }*/
    }
}
