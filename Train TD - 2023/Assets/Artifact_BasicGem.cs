using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_BasicGem : MonoBehaviour, IChangeCartState, IArtifactDescription {

    public string currentDescription;

    [Header("Use values like 1.5 or -1.5 for 50%boost or 50%reduction")]
    public float powerMultiplier = 0;
    public float speedMultiplier = 0;
    public float efficiencyMultiplier = 0;
    public float healthMultiplier = 0;
    public float armorMultiplier = 0;
    public float flatArmor = 0;
    
    public void ChangeState(Cart target) {
        currentDescription = "";
        
        var didApply = false;
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            if (powerMultiplier > 1) {
                gunModule.currentAffectors.power *= powerMultiplier;
                currentDescription = $"Boosts damage by {GetPercentString(powerMultiplier)}";
            }else if (powerMultiplier < -1) {
                gunModule.currentAffectors.power /= -powerMultiplier;
                currentDescription = $"Lowers damage by {GetPercentString(powerMultiplier)}";
            }
            if (speedMultiplier > 1) {
                gunModule.currentAffectors.speed *= speedMultiplier;
                currentDescription = $"Boosts firerate by {GetPercentString(speedMultiplier)}";
            }else if (speedMultiplier < -1) {
                gunModule.currentAffectors.speed /= -speedMultiplier;
                currentDescription = $"Lowers firerate by {GetPercentString(speedMultiplier)}";
            }
            if (efficiencyMultiplier > 1) {
                gunModule.currentAffectors.efficiency *= efficiencyMultiplier;
                currentDescription = $"Increases chance to not use ammo by {GetPercentString(efficiencyMultiplier)}";
            }else if (efficiencyMultiplier < -1) {
                gunModule.currentAffectors.efficiency /= -efficiencyMultiplier;
                if (gunModule.currentAffectors.efficiency >= 1) {
                    currentDescription = $"Decreases chance to not use ammo by {GetPercentString(efficiencyMultiplier)}";
                } else {
                    currentDescription = $"Increases change to use more ammo by {GetPercentString(efficiencyMultiplier)}";
                }
            }
            
            
            didApply = true;
        }
        
        foreach (var repairModule in target.GetComponentsInChildren<DroneRepairController>()) {
            var repairMore = efficiencyMultiplier;
            var repairTime = powerMultiplier;
            if (repairMore > 1) {
                repairModule.currentAffectors.power *= repairMore;
                currentDescription += $"Increases chance to repair multiple by {GetPercentString(repairMore)}";
            }else if (repairMore < -1) {
                repairModule.currentAffectors.power /= -repairMore;
                if (repairModule.currentAffectors.efficiency >= 1) {
                    currentDescription += $"Decreases chance to not repair multiple by {GetPercentString(efficiencyMultiplier)}";
                } else {
                    currentDescription += $"Increases change to not repair at all by {GetPercentString(efficiencyMultiplier)}";
                }
            }
            if (speedMultiplier > 1) {
                repairModule.currentAffectors.speed *= speedMultiplier;
                currentDescription += $"Boosts move speed by {GetPercentString(speedMultiplier)}";
            }else if (speedMultiplier < -1) {
                repairModule.currentAffectors.speed /= -speedMultiplier;
                currentDescription += $"Lowers move speed by {GetPercentString(speedMultiplier)}";
            }
            if (repairTime > 1) {
                repairModule.currentAffectors.efficiency *= repairTime;
                currentDescription += $"Reduces repair time by {GetPercentString(repairTime)}";
            }else if (repairTime < -1) {
                repairModule.currentAffectors.efficiency /= -repairTime;
                currentDescription += $"Increases repair time by {GetPercentString(repairTime)}";
            }
            
            
            didApply = true;
        }
        
        foreach (var ammoModule in target.GetComponentsInChildren<AmmoDirectController>()) {
            if (powerMultiplier > 1) {
                ammoModule.currentAffectors.power *= powerMultiplier;
                currentDescription += $"Increases reload count by {GetPercentString(powerMultiplier)}";
            }else if (powerMultiplier < -1) {
                ammoModule.currentAffectors.power /= -powerMultiplier;
                currentDescription += $"Decreases reload count by {GetPercentString(powerMultiplier)}";
            }
            if (speedMultiplier > 1) {
                ammoModule.currentAffectors.speed *= speedMultiplier;
                currentDescription += $"Increases move speed by {GetPercentString(speedMultiplier)}";
            }else if (speedMultiplier < -1) {
                ammoModule.currentAffectors.speed /= -speedMultiplier;
                currentDescription += $"Decreases move speed by {GetPercentString(speedMultiplier)}";
            }
            if (efficiencyMultiplier > 1) {
                ammoModule.currentAffectors.efficiency *= efficiencyMultiplier;
                currentDescription += $"Makes it easier to get perfect reload by {GetPercentString(efficiencyMultiplier)}";
            }else if (efficiencyMultiplier < -1) {
                ammoModule.currentAffectors.efficiency /= -efficiencyMultiplier;
                currentDescription += $"Makes it harder to get perfect reload by {GetPercentString(efficiencyMultiplier)}";
            }
            
            
            didApply = true;
        }
        
        foreach (var shieldModule in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            if (powerMultiplier > 1) {
                shieldModule.currentAffectors.power *= powerMultiplier;
                currentDescription += $"Increases max shields by {GetPercentString(powerMultiplier)}";
            }else if (powerMultiplier < -1) {
                shieldModule.currentAffectors.power /= -powerMultiplier;
                currentDescription += $"Decreases max shields by {GetPercentString(powerMultiplier)}";
            }
            if (speedMultiplier > 1) {
                shieldModule.currentAffectors.speed *= speedMultiplier;
                currentDescription += $"Increases regeneration rate by {GetPercentString(speedMultiplier)}";
            }else if (speedMultiplier < -1) {
                shieldModule.currentAffectors.speed /= -speedMultiplier;
                currentDescription += $"Decreases regeneration rate by {GetPercentString(speedMultiplier)}";
            }
            if (efficiencyMultiplier > 1) {
                shieldModule.currentAffectors.efficiency *= efficiencyMultiplier;
                currentDescription += $"Lowers regeneration delay by {GetPercentString(efficiencyMultiplier)}";
            }else if (efficiencyMultiplier < -1) {
                shieldModule.currentAffectors.efficiency /= -efficiencyMultiplier;
                currentDescription += $"Increases regeneration delay by {GetPercentString(efficiencyMultiplier)}";
            }
            
            didApply = true;
        }


        if (healthMultiplier > 1) {
            target.GetHealthModule().MultiplyMaxHealthByAmount(healthMultiplier);
            currentDescription += $"Increases max health by {GetPercentString(healthMultiplier)}";
            didApply = true;
        }else if (healthMultiplier < -1) {
            target.GetHealthModule().MultiplyMaxHealthByAmount(1/-healthMultiplier);
            currentDescription += $"Decreases max health by {GetPercentString(healthMultiplier)}";
            didApply = true;
        }
        
        if (armorMultiplier > 1) {
            target.GetHealthModule().currentAffectors.armor *= armorMultiplier;
            currentDescription += $"Increases armor by {GetPercentString(armorMultiplier)}";
            didApply = true;
        }else if (armorMultiplier < -1) {
            target.GetHealthModule().currentAffectors.armor /= -armorMultiplier;
            currentDescription += $"Decreases armor by {GetPercentString(armorMultiplier)}";
            didApply = true;
        }
        
        if (flatArmor > 0) {
            target.GetHealthModule().currentAffectors.flatArmor += flatArmor;
            currentDescription += $"Reduces incoming damage by {flatArmor:F0}";
            didApply = true;
        }else if (flatArmor < -0) {
            target.GetHealthModule().currentAffectors.flatArmor += flatArmor;
            currentDescription += $"Increases incoming damage by {flatArmor:F0}";
            didApply = true;
        }

        if (!didApply) {
            currentDescription = "Cannot affect this cart yet";
        }
        GetComponent<Artifact>().cantAffectOverlay.SetActive(!didApply);
    }

    string GetPercentString(float value) {
        value = Mathf.Abs(value);
        value -= 1;
        value *= 100;

        return $"{value:F0}%";
    }

    public string GetDescription() {
        return currentDescription;
    }
}
