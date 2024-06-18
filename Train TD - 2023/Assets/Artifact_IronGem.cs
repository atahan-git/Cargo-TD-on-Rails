using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_IronGem : MonoBehaviour, IChangeCartState, IArtifactDescription {

    public string currentDescription;
    public void ChangeState(Cart target) {
        var didApply = false;
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.currentAffectors.flatDamageAdd += 2;
            gunModule.currentAffectors.power += 1;
            gunModule.currentAffectors.flatAmmoCostAdd += 0.2f;
            gunModule.currentAffectors.ammoMultiplier *= 1.5f;
            currentDescription = "Increases damage and ammo use greatly";
            didApply = true;
        }

        foreach (var droneRepair in target.GetComponentsInChildren<DroneRepairController>()) {
            if (droneRepair.currentAffectors.iron == 0) {
                currentDescription = "Makes the drone a mega drone, repairing the entire cart at once";
            } else {
                currentDescription = "The mega drone now repairs nearby carts too.";
            }
            droneRepair.currentAffectors.iron += 1;

            droneRepair.currentAffectors.speed *= 0.2f;
            droneRepair.currentAffectors.power *= 0.2f;
            //droneRepair.currentAffectors.efficiency *= 0.5f;

            /*if (droneRepair.currentAffectors.power > 1) {
                currentDescription += "Power has no effect on Mega Drone";
            }*/
            
            droneRepair.UpdateDroneSize();
            didApply = true;
        }
        
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            shieldGenerator.currentAffectors.power *= 3f;
            shieldGenerator.currentAffectors.speed /= 2f;
            shieldGenerator.currentAffectors.efficiency /= 2f;

            shieldGenerator.currentAffectors.iron += 1;
            
            shieldGenerator.SetShieldSize();
            
            currentDescription = "Increases shield size but makes it slower";
            didApply = true;
        }
        
        foreach (var moduleAmmo in target.GetComponentsInChildren<ModuleAmmo>()) {
            moduleAmmo.currentAffectors.maxAmmoMultiplier *= 2;
            currentDescription = "Doubles ammo capacity";
            didApply = true;
        }
        
        /*foreach (var ammoDirect in target.GetComponentsInChildren<AmmoDirectController>()) {
            ammoDirect.currentAffectors.ammoCapacityMultiplier *= 2;
            currentDescription = "Doubles ammo capacity";
            didApply = true;
        }*/

        if (!didApply) {
            currentDescription = "Cannot affect this cart yet";
        }
        GetComponent<Artifact>().cantAffectOverlay.SetActive(!didApply);
    }

    public string GetDescription() {
        return currentDescription;
    }
}
