using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Artifact_FireGem : MonoBehaviour, IChangeCartState, IArtifactDescription {

    public string currentDescription;
    public void ChangeState(Cart target) {
        var didApply = false;
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.currentAffectors.regularToBurnDamageConversionMultiplier += 0.25f;
            didApply = true;
            if (gunModule.burnDamage > 0) {
                currentDescription = "Increases burn damage";
            } else {
                currentDescription = "Adds burn damage";
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
