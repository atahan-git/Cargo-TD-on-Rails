using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_BasicComponent : MonoBehaviour, IChangeStateToEntireTrain
{
    [Header("Use values like 1.5 or -1.5 for 50%boost or 50%reduction")]
    public float powerMultiplier = 0;
    public float speedMultiplier = 0;
    public float efficiencyMultiplier = 0;
    public float healthMultiplier = 0;
    public float armorMultiplier = 0;
    public float flatArmor = 0;
    
    public void ChangeStateToEntireTrain(List<Cart> carts) {
        for (int i = 0; i < carts.Count; i++) {
            ChangeState(carts[i]);
        }
    }
    
    public void ChangeState(Cart target) {
        
        var didApply = false;
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            if (powerMultiplier > 1) {
                gunModule.currentAffectors.power *= powerMultiplier;
            }else if (powerMultiplier < -1) {
                gunModule.currentAffectors.power /= -powerMultiplier;
            }
            if (speedMultiplier > 1) {
                gunModule.currentAffectors.speed *= speedMultiplier;
            }else if (speedMultiplier < -1) {
                gunModule.currentAffectors.speed /= -speedMultiplier;
            }
            if (efficiencyMultiplier > 1) {
                gunModule.currentAffectors.efficiency *= efficiencyMultiplier;
            }else if (efficiencyMultiplier < -1) {
                gunModule.currentAffectors.efficiency /= -efficiencyMultiplier;
            }
            
            
            didApply = true;
        }
        
        foreach (var repairModule in target.GetComponentsInChildren<DroneRepairController>()) {
            var repairMore = efficiencyMultiplier;
            var repairTime = powerMultiplier;
            if (repairMore > 1) {
                repairModule.currentAffectors.power *= repairMore;
            }else if (repairMore < -1) {
                repairModule.currentAffectors.power /= -repairMore;
            }
            if (speedMultiplier > 1) {
                repairModule.currentAffectors.speed *= speedMultiplier;
            }else if (speedMultiplier < -1) {
                repairModule.currentAffectors.speed /= -speedMultiplier;
            }
            if (repairTime > 1) {
                repairModule.currentAffectors.efficiency *= repairTime;
            }else if (repairTime < -1) {
                repairModule.currentAffectors.efficiency /= -repairTime;
            }
            
            
            didApply = true;
        }
        
        foreach (var ammoModule in target.GetComponentsInChildren<AmmoDirectController>()) {
            if (powerMultiplier > 1) {
                ammoModule.currentAffectors.power *= powerMultiplier;
            }else if (powerMultiplier < -1) {
                ammoModule.currentAffectors.power /= -powerMultiplier;
            }
            if (speedMultiplier > 1) {
                ammoModule.currentAffectors.speed *= speedMultiplier;
            }else if (speedMultiplier < -1) {
                ammoModule.currentAffectors.speed /= -speedMultiplier;
            }
            if (efficiencyMultiplier > 1) {
                ammoModule.currentAffectors.efficiency *= efficiencyMultiplier;
            }else if (efficiencyMultiplier < -1) {
                ammoModule.currentAffectors.efficiency /= -efficiencyMultiplier;
            }

            didApply = true;
        }
        
        foreach (var shieldModule in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            if (powerMultiplier > 1) {
                shieldModule.currentAffectors.power *= powerMultiplier;
            }else if (powerMultiplier < -1) {
                shieldModule.currentAffectors.power /= -powerMultiplier;
            }
            if (speedMultiplier > 1) {
                shieldModule.currentAffectors.speed *= speedMultiplier;
            }else if (speedMultiplier < -1) {
                shieldModule.currentAffectors.speed /= -speedMultiplier;
            }
            if (efficiencyMultiplier > 1) {
                shieldModule.currentAffectors.efficiency *= efficiencyMultiplier;
            }else if (efficiencyMultiplier < -1) {
                shieldModule.currentAffectors.efficiency /= -efficiencyMultiplier;
            }
            
            didApply = true;
        }


        if (healthMultiplier > 1) {
            target.GetHealthModule().currentAffectors.maxHealth *= healthMultiplier;
            didApply = true;
        }else if (healthMultiplier < -1) {
            target.GetHealthModule().currentAffectors.maxHealth /= -healthMultiplier;
            didApply = true;
        }
        
        if (armorMultiplier > 1) {
            target.GetHealthModule().currentAffectors.armor *= armorMultiplier;
            didApply = true;
        }else if (armorMultiplier < -1) {
            target.GetHealthModule().currentAffectors.armor /= -armorMultiplier;
            didApply = true;
        }
        
        if (flatArmor > 0) {
            target.GetHealthModule().currentAffectors.flatArmor += flatArmor;
            didApply = true;
        }else if (flatArmor < -0) {
            target.GetHealthModule().currentAffectors.flatArmor += flatArmor;
            didApply = true;
        }
    }
}
