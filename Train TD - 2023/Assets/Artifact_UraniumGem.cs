using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Artifact_UraniumGem : MonoBehaviour, IChangeCartState, IActiveDuringCombat,IArtifactDescription, IDisabledState
{
    
    public string currentDescription;

    public Cart myCart;
    public GunModule[] attachedGuns;
    public void ChangeState(Cart target) {
        var didApply = false;
        myCart = target;
        
        foreach (var gunModule in attachedGuns) {
            gunModule.onBulletFiredEvent.RemoveListener(BulletFired);
            didApply = true;
        }

        attachedGuns = target.GetComponentsInChildren<GunModule>();
        foreach (var gunModule in attachedGuns) {
            gunModule.onBulletFiredEvent.AddListener(BulletFired);
            if (gunModule.projectileDamage > 0) {
                gunModule.currentAffectors.speed *= 3;
                gunModule.currentAffectors.uranium += 1;
                currentDescription = "Shoot faster, but take base damage every shot";
                didApply = true;
            }
        }

        foreach (var droneRepair in target.GetComponentsInChildren<DroneRepairController>()) {
            if (droneRepair.currentAffectors.uranium >= 1) {
                currentDescription = "Second Uranium Gem has no effect";
                didApply = true;
            } else {
                droneRepair.currentAffectors.uranium += 1;
                currentDescription = "Repair super fast but use ammo while doing so";
                didApply = true;
            }
        }

        foreach (var ammoDirect in target.GetComponentsInChildren<AmmoDirectController>()) {
            //ammoDirect.currentAffectors.speed *= 2;
            ammoDirect.currentAffectors.uranium += 1;
            currentDescription = "Every reload is perfect. But take damage when reloading";
            didApply = true;
        }
        
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            shieldGenerator.currentAffectors.speed *= 2;
            shieldGenerator.currentAffectors.uranium += 1;
            currentDescription = "Shield regens faster, but take damage over time";
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

    private Artifact myArtifact;
    private void Start() {
        myArtifact = GetComponent<Artifact>();
    }

    public void ApplyDamage(Cart target, float amount) {
        if(target == null)
            return;
        
        target.GetHealthModule().DealDamage(amount);
        VisualEffectsController.s.SmartInstantiate(LevelReferences.s.radiationDamagePrefab, target.uiTargetTransform);
    }


    public void ActivateForCombat() {
        this.enabled = true;
    }

    public void Disable() {
        this.enabled = false;
    }

    private void Update() {
        /*if (PlayStateMaster.s.isCombatInProgress()) {
            if (myArtifact != null) {
                if (myArtifact.isAttached && enabled) {
                    if (doRepair) {
                        
                    }
                }
            }
        }*/
    }


    void BulletFired() {
        if (myArtifact != null) {
            if (myArtifact.isAttached && enabled) {
                if (attachedGuns[0].projectileDamage > 0) {
                    ApplyDamage(myCart, attachedGuns[0].projectileDamage /1.5f);
                }
            }
        }
    }

    public void CartDisabled() {
        this.enabled = false;
    }

    public void CartEnabled() {
        this.enabled = true;
    }
}