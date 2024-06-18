using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Artifact_FireGem : MonoBehaviour, IChangeCartState, IArtifactDescription {

    public string currentDescription;
    public void ChangeState(Cart target) {
        var didApply = false;
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            if (gunModule.burnDamage > 0 || gunModule.currentAffectors.fire > 0) {
                currentDescription = "Increases burn damage";
            } else {
                currentDescription = "Converts regular damage to burn damage";
            }
            
            gunModule.currentAffectors.fire += 1f;
            didApply = true;
        }
        
        foreach (var droneRepair in target.GetComponentsInChildren<DroneRepairController>()) {
            droneRepair.currentAffectors.power += 1;
            currentDescription = "Repair an extra burn";
            didApply = true;
        }
        
        foreach (var moduleAmmo in target.GetComponentsInChildren<ModuleAmmo>()) {
            moduleAmmo.currentAffectors.reloadOverTime += 0.5f;
            currentDescription = "Slowly gain ammo";
            didApply = true;
        }

        if (!didApply) {
            currentDescription = "Cannot affect this cart yet";
        }
        GetComponent<Artifact>().cantAffectOverlay.SetActive(!didApply);
    }

    public string GetDescription() {
        return currentDescription;
    }
}
