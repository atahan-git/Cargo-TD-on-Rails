using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_ExplosiveGem: MonoBehaviour, IChangeCartState,IArtifactDescription
{
    public string currentDescription;
    public void ChangeState(Cart target) {
        var didApply = false;
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.currentAffectors.regularToRangeConversionMultiplier += 0.01f;
            didApply = true;
            if (gunModule.projectileDamage > 0) {
                currentDescription = "Adds an explosion";
            } else {
                currentDescription = "Cannot affect this gun";
            }
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
