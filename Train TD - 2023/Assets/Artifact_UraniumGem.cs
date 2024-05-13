using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Artifact_UraniumGem : MonoBehaviour, IChangeCartState, IActiveDuringCombat,IArtifactDescription, IDisabledState
{
    
    public string currentDescription;
    public void ChangeState(Cart target) {
        var didApply = false;
        
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.currentAffectors.fireRateMultiplier += 0.50f;
            currentDescription = "Shoot faster, but take damage over time";
            didApply = true;
        }

        foreach (var droneRepair in target.GetComponentsInChildren<DroneRepairController>()) {
            droneRepair.currentAffectors.repairRateIncreaseMultiplier += 1f;
            droneRepair.currentAffectors.droneAccelerationIncreaser += 0.3f;
            droneRepair.currentAffectors.directControlRepairTime -= 0.5f;
            currentDescription = "Repair faster, but take damage over time";
            didApply = true;
        }
        
        foreach (var ammoDirect in target.GetComponentsInChildren<AmmoDirectController>()) {
            ammoDirect.currentAffectors.moveSpeedMultiplier += 1f;
            currentDescription = "Reload faster, but take damage over time";
            didApply = true;
        }
        
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            shieldGenerator.currentAffectors.regenMultiplier += 0.50f;
            shieldGenerator.currentAffectors.regenTimerReductionMultiplier += 0.15f;
            shieldGenerator.currentAffectors.shieldMoveSpeedIncreaser += 0.30f;
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

    void ApplyDamage(Cart target) {
        if(target == null)
            return;
        
        target.GetHealthModule().DealDamage(Random.Range(radiationDamageAmount.x, radiationDamageAmount.y), null);
        VisualEffectsController.s.SmartInstantiate(LevelReferences.s.radiationDamagePrefab, target.uiTargetTransform);
    }

    public Vector2 radiationDamageDelay = new Vector2(1,3);
    public Vector2 radiationDamageAmount = new Vector2(50,300);

    private float radiationDelay;
    public void ActivateForCombat() {
        this.enabled = true;
        radiationDelay = LevelReferences.s.bigEffectFirstActivateTimeAfterCombatStarts + Random.Range(radiationDamageDelay.x, radiationDamageDelay.y);
    }

    public void Disable() {
        this.enabled = false;
    }

    private void Update() {
        if (PlayStateMaster.s.isCombatInProgress()) {
            if (myArtifact != null) {
                if (myArtifact.isAttached) {
                    radiationDelay -= Time.deltaTime;
                    if (radiationDelay <= 0) {
                        radiationDelay = Random.Range(radiationDamageDelay.x, radiationDamageDelay.y);
                        
                        ApplyDamage(GetComponentInParent<Cart>());
                    }
                }
            }
        }
    }

    public void CartDisabled() {
        this.enabled = false;
    }

    public void CartEnabled() {
        this.enabled = true;
        radiationDelay = Random.Range(radiationDamageDelay.x, radiationDamageDelay.y);
    }
}