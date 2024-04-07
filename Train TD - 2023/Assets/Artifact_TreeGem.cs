using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_TreeGem : ActivateWhenOnArtifactRow, IResetStateArtifact, IActiveDuringCombat, IApplyToEnemyWithGem
{
    
    //[Space]
    public float reloadDelayBase = 2f;
    public float reloadBoostPerLevel = -0.5f;
    public float currentReloadDelay = 0;

    private Artifact myArtifact;
    private void Start() {
        myArtifact = GetComponent<Artifact>();
    }

    protected override void _Arm() {
        var range = GetComponent<Artifact>().range;
        ApplyBoost(GetComponentInParent<Cart>());
        for (int i = 1; i < range+1; i++) {
            ApplyBoost(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()));
            ApplyBoost(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()));
        }
    }

    void ApplyBoost(Cart target) {
        if(target == null)
            return;

        bool didApply = true;
        
        if (didApply) {
            GetComponent<Artifact>()._ApplyToTarget(target);
        }
    }

    void ApplyReloadOrHealth(Cart target) {
        if(target == null)
            return;

        var hasAmmo = false;
        foreach (var moduleAmmo in target.GetComponentsInChildren<ModuleAmmo>()) {
            moduleAmmo.Reload(1);
            hasAmmo = true;
        }

        if (!hasAmmo) {
            target.GetHealthModule().RepairChunk();
        }
        VisualEffectsController.s.SmartInstantiate(LevelReferences.s.growthEffectPrefab, target.uiTargetTransform, VisualEffectsController.EffectPriority.High);
    }

    protected override void _Disarm() {
        // do nothing
    }

    public void ResetState(int level) {
        currentReloadDelay = reloadDelayBase + (level * reloadBoostPerLevel);
    }
    
    public void ActivateForCombat() {
        this.enabled = true;
        activeDelay = LevelReferences.s.smallEffectFirstActivateTimeAfterCombatStarts;
    }

    private float activeDelay;
    private void Update() {
        if (PlayStateMaster.s.isCombatInProgress()) {
            if (myArtifact != null) {
                if (myArtifact.isAttached) {
                    activeDelay -= Time.deltaTime;
                    if (activeDelay <= 0) {
                        activeDelay = currentReloadDelay;

                        if (!enemyToApplyTo) {
                            var range = GetComponent<Artifact>().range;
                            ApplyReloadOrHealth(GetComponentInParent<Cart>());
                            for (int i = 1; i < range + 1; i++) {
                                ApplyReloadOrHealth(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()));
                                ApplyReloadOrHealth(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()));
                            }
                        }
                    }
                }
            } else {
                activeDelay -= Time.deltaTime;
                if (activeDelay <= 0) {
                    activeDelay = currentReloadDelay;

                    if (enemyToApplyTo) {
                        ApplyToEnemy();
                    } 
                }
            }
        }
    }

   
    public void Disable() {
        this.enabled = false;
    }
    
    void ApplyToEnemy() {
        enemyToApplyTo.GetComponent<EnemyHealth>().RepairChunk();
        VisualEffectsController.s.SmartInstantiate(LevelReferences.s.growthEffectPrefab, enemyToApplyTo.transform, VisualEffectsController.EffectPriority.High);
    }

    private EnemyInSwarm enemyToApplyTo;
    public void ApplyToEnemyWithGem(EnemyInSwarm enemy) {
        enemyToApplyTo = enemy;
        //throw new NotImplementedException();
    }
}
