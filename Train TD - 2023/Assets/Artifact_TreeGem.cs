using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_TreeGem : MonoBehaviour, IChangeCartState, IActiveDuringCombat,IArtifactDescription, IDisabledState
{
    public void ChangeState(Cart target) {
        // do nothing

        currentDescription = "Repairs the cart slowly";
        var hasAmmo = false;
        foreach (var moduleAmmo in target.GetComponentsInChildren<ModuleAmmo>()) {
            hasAmmo = true;
        }

        if (hasAmmo) {
            currentDescription = "Reloads then repairs the cart";
        }
        
    }
    //[Space]
    public float repairDelay = 5;
    public int repairChunks = 1;
    public int reloadBullets = 1;

    private Artifact myArtifact;
    
    
    public string currentDescription;
    private void Start() {
        myArtifact = GetComponent<Artifact>();
    }

    void ApplyReloadOrHealth(Cart target) {
        if(target == null)
            return;

        var hasAmmo = false;
        foreach (var moduleAmmo in target.GetComponentsInChildren<ModuleAmmo>()) {
            moduleAmmo.Reload(reloadBullets);
            hasAmmo = true;
        }

        if (!hasAmmo) {
            target.GetHealthModule().RepairChunk(repairChunks);
        }
        
        VisualEffectsController.s.SmartInstantiate(LevelReferences.s.growthEffectPrefab, target.uiTargetTransform, VisualEffectsController.EffectPriority.High);
    }

    private float activeDelay;
    private void Update() {
        if (PlayStateMaster.s.isCombatInProgress()) {
            if (myArtifact != null) {
                if (myArtifact.isAttached) {
                    activeDelay -= Time.deltaTime;
                    if (activeDelay <= 0) {
                        activeDelay = repairDelay;

                        ApplyReloadOrHealth(GetComponentInParent<Cart>());
                        /*for (int i = 1; i < range + 1; i++) {
                            ApplyReloadOrHealth(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()));
                            ApplyReloadOrHealth(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()));
                        }*/
                    }
                }
            }
        }
    }


    public void ActivateForCombat() {
        this.enabled = true;
    }

    public void Disable() {
        this.enabled = false;
    }

    public string GetDescription() {
        return currentDescription;
    }

    public void CartDisabled() {
        this.enabled = false;
    }

    public void CartEnabled() {
        this.enabled = true;
    }
}
