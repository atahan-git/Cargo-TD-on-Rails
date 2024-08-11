using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_ExplosiveGem: MonoBehaviour, IChangeCartState,IArtifactDescription
{
    public string currentDescription;
    public void ChangeState(Cart target) {
        var didApply = false;
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.currentAffectors.explosionRangeAdd += 0.5f;
            didApply = true;
            if (gunModule.projectileDamage > 0) {
                currentDescription = "Adds an explosion";
            } else {
                currentDescription = "Cannot affect this gun";
            }
        }
        
        
        foreach (var repairModule in target.GetComponentsInChildren<DroneRepairController>()) {
            if (repairModule.currentAffectors.explosionRange <= 0) {
                currentDescription = "Adds a repair explosion";
                repairModule.currentAffectors.explosionRange += 0.35f;
            } else {
                currentDescription = "Increases repair explosion range";
                repairModule.currentAffectors.explosionRange += 0.15f;
            }
            
            didApply = true;
            
        }
        
        foreach (var ammoModule in target.GetComponentsInChildren<ModuleAmmo>()) {
            if (ammoModule.currentAffectors.explosionResistance == 0) {
                currentDescription = "Ammo doesn't disappear if cart explodes";
            } else {
                currentDescription = "No extra effect";
            }
            
            ammoModule.currentAffectors.explosionResistance += 1;
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
