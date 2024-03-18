using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Artifact_UraniumGem : ActivateWhenOnArtifactRow, IResetStateArtifact, IActiveDuringCombat,IApplyToEnemyWithGem
{
    
    //[Space]
    public float boostFirerateBase = 0.15f;
    public float boostFireratePerLevel = 0.05f;
    public float activeFireRateBoost = 0;

    public GameObject currentRadiation;

    public float radiationDelayReduction = 1f;
    public float radiationDelayReductionPerLevel = 0.5f;
    public float curRadiationDelayReduction = 0;
    
    private Artifact myArtifact;
    private void Start() {
        myArtifact = GetComponent<Artifact>();
    }
    
    protected override void _Arm() {
        var range = GetComponent<Artifact>().range;
        ApplyBoost(Train.s.GetNextBuilding(0, GetComponentInParent<Cart>()));
        for (int i = 1; i < range+1; i++) {
            ApplyBoost(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()));
            ApplyBoost(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()));
        }
    }

    void ApplyBoost(Cart target) {
        if(target == null)
            return;
        
        bool didApply = false;
        
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.fireRateMultiplier += activeFireRateBoost;
            didApply = true;
        }

        foreach (var roboRepair in target.GetComponentsInChildren<RoboRepairModule>()) {
            roboRepair.firerateMultiplier += activeFireRateBoost;
            didApply = true;
        }
        
        /*foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
            var hp = target.GetHealthModule();
            hp.shieldRegenDelayMultiplier += activeFireRateBoost;
            hp.shieldRegenRateMultiplier += activeFireRateBoost;
            didApply = true;
        }*/
        
        foreach (var trainGemBridge in target.GetComponentsInChildren<TrainGemBridge>()) {
            trainGemBridge.uranium = currentRadiation;
            trainGemBridge.uraniumDelayReduction += curRadiationDelayReduction;
            didApply = true;
        }
        
        
        if (didApply) {
            GetComponent<Artifact>()._ApplyToTarget(target);
        }
    }

    void ApplyDamage(Cart target) {
        if(target == null)
            return;
        
        target.GetHealthModule().DealDamage(Random.Range(radiationDamageAmount.x, radiationDamageAmount.y), null);
        VisualEffectsController.s.SmartInstantiate(LevelReferences.s.radiationDamagePrefab, target.uiTargetTransform);
    }

    protected override void _Disarm() {
        // do nothing
    }

    public void ResetState(int level) {
        activeFireRateBoost = boostFirerateBase + (boostFireratePerLevel * level);
        curRadiationDelayReduction = radiationDelayReduction + (radiationDelayReductionPerLevel * level);
    }

    public Vector2 radiationDamageDelay = new Vector2(1,3);
    public Vector2 radiationDamageAmount = new Vector2(50,300);

    private float radiationDelay;
    public void ActivateForCombat() {
        this.enabled = true;
        radiationDelay = LevelReferences.s.bigEffectFirstActivateTimeAfterCombatStarts + Random.Range(radiationDamageDelay.x, radiationDamageDelay.y);
    }

    
    private void Update() {
        if ((myArtifact == null || myArtifact.isAttached) && PlayStateMaster.s.isCombatInProgress()) {
            radiationDelay -= Time.deltaTime;
            if (radiationDelay <= 0) {
                radiationDelay = Random.Range(radiationDamageDelay.x, radiationDamageDelay.y);

                if (enemyToApplyTo) {
                    ApplyToEnemy();
                } else {
                    var range = GetComponent<Artifact>().range;
                    ApplyDamage(Train.s.GetNextBuilding(0, GetComponentInParent<Cart>()));
                    for (int i = 1; i < range + 1; i++) {
                        ApplyDamage(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()));
                        ApplyDamage(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()));
                    }
                }
            }
        }
    }

    public void Disable() {
        this.enabled = false;
    }

    void ApplyToEnemy() {
        enemyToApplyTo.GetComponent<EnemyHealth>().DealDamage(Random.Range(radiationDamageAmount.x, radiationDamageAmount.y), null);
        VisualEffectsController.s.SmartInstantiate(LevelReferences.s.radiationDamagePrefab, enemyToApplyTo.transform);
    }

    private EnemyInSwarm enemyToApplyTo;
    public void ApplyToEnemyWithGem(EnemyInSwarm enemy) {
        enemyToApplyTo =enemy;
        foreach (var gunModule in enemy.GetComponentsInChildren<GunModule>()) {
            gunModule.fireRateMultiplier += activeFireRateBoost;
        }
    }
}