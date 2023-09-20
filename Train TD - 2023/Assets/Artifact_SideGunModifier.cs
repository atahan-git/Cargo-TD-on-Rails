using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_SideGunModifier : ActivateWhenOnArtifactRow {

    public float damageMultiplier = 1;
    public float ammoUseMultiplier = 1;



    protected override void _Arm() {
        //GetComponent<Artifact>().range = 100;
        var carts = GetAllCarts();
        for (int i = 0; i < carts.Count; i++) {
            var cart = carts[i];

            var directControl = cart.GetComponentInChildren<DirectControllable>();


            if (!directControl) {
                foreach (var gunModule in cart.GetComponentsInChildren<GunModule>()) {
                    gunModule.damageMultiplier += damageMultiplier - 1;
                }
                
                if (cart.GetComponentInChildren<ModuleAmmo>() != null) {
                    cart.GetComponentInChildren<ModuleAmmo>().ammoPerBarrageMultiplier += ammoUseMultiplier-1;
                }
            }
        }
    }

    protected override void _Disarm() {
        // do nothing
    }
}
