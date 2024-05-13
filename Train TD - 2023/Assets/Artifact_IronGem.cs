using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_IronGem : MonoBehaviour, IChangeCartState, IArtifactDescription {

    public string currentDescription;
    public void ChangeState(Cart target) {
        var didApply = false;
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.currentAffectors.damageMultiplier += 1.5f;
            gunModule.currentAffectors.fireRateDivider += 1;
            currentDescription = "Increases damage but reduces firerate";
            didApply = true;
        }

        foreach (var droneRepair in target.GetComponentsInChildren<DroneRepairController>()) {
            if (droneRepair.currentAffectors.megaRepair == 0) {
                currentDescription = "Repairing is a lot slower, but repairs the entire cart";
                
                droneRepair.currentAffectors.directControlRepairTime += 5f; 
            } else {
                currentDescription = "Repairing is even slower, but repairs nearby carts";
                droneRepair.currentAffectors.directControlRepairTime += 2.5f; 
            }

            droneRepair.currentAffectors.repairRateIncreaseReducer += 3;
            droneRepair.currentAffectors.megaRepair +=1;
            droneRepair.currentAffectors.droneAccelerationReducer += 0.5f;
            droneRepair.currentAffectors.droneSizeMultiplier += 0.5f;
            foreach (var drone in droneRepair.GetComponentsInChildren<RepairDrone>()) {
                drone.transform.localScale = Vector3.one * ((droneRepair.currentAffectors.megaRepair/2f)+1);
            }
            didApply = true;
        }
        
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            shieldGenerator.currentAffectors.regenTimerReductionDivider += 0.25f;
            shieldGenerator.currentAffectors.currentMaxShieldAmount *= 1.5f;
            shieldGenerator.currentAffectors.shieldCoverage += 1;
            shieldGenerator.currentAffectors.shieldMoveSpeedReducer += 0.25f;
            currentDescription = "Increases shields but increases pause before regen";
            didApply = true;
        }
        
        foreach (var ammoDirect in target.GetComponentsInChildren<AmmoDirectController>()) {
            ammoDirect.currentAffectors.reloadAmountMultiplier += 3f;
            ammoDirect.currentAffectors.moveSpeedReducer += 2;
            currentDescription = "Increases reload amount, but reduces move speed";
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
