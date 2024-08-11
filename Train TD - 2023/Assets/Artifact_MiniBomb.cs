using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_MiniBomb : MonoBehaviour
{
    public bool isArmed = false;
    public bool isExploded = false;
    void Update()
    {
        if (PlayerWorldInteractionController.s.currentSelectedThingMonoBehaviour == GetComponent<Artifact>()) {
            isArmed = true;
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (isArmed && !isExploded && PlayStateMaster.s.isCombatInProgress() && !GetComponent<Artifact>().isAttached) {
            var playerIsHoldingMe = PlayerWorldInteractionController.s.currentSelectedThingMonoBehaviour == GetComponent<Artifact>();
            //var myHolderDrone = GetComponent<Artifact>().GetHoldingDrone();
            var droneIsHoldingMe = false;
        
            if (!playerIsHoldingMe && !droneIsHoldingMe) {
                isExploded = true;
                Explode();
            }
        }
    }

    public void Explode() {
        CommonEffectsProvider.s.SpawnEffect(CommonEffectsProvider.CommonEffectType.megaDamage, transform.position, transform.rotation, VisualEffectsController.EffectPriority.Always);

        var targets = Physics.OverlapSphere(transform.position, 1.25f, LevelReferences.s.enemyLayer);


        var healthsInRange = new List<EnemyHealth>();
        for (int i = 0; i < targets.Length; i++) {
            var target = targets[i];
            
            var health = target.gameObject.GetComponentInParent<EnemyHealth>();
            if (health != null ) {
                if (!healthsInRange.Contains(health)) {
                    healthsInRange.Add(health);
                }
            }
        }

        foreach (var health in healthsInRange) {
            health.DealDamage(750, null);
            ApplyHitForceToObject(health);
            var closestPoint = health.GetMainCollider().ClosestPoint(transform.position);
            CommonEffectsProvider.s.SpawnEffect(CommonEffectsProvider.CommonEffectType.mortarMiniHit, closestPoint, Quaternion.identity, VisualEffectsController.EffectPriority.Low);
        }
        
        if (LevelReferences.s.combatHoldableThings.Contains(GetComponent<Artifact>())) {
            LevelReferences.s.combatHoldableThings.Remove(GetComponent<Artifact>());
        }

        MiniGUI_Pick3GemReward.MakeGem(UpgradesController.s.potatoGemName, transform.position, transform.rotation);

        Destroy(gameObject);
    }
    
    void ApplyHitForceToObject(EnemyHealth health) {
        var collider = health.GetMainCollider();
        var closestPoint = collider.ClosestPoint(transform.position);
        var rigidbody = collider.GetComponent<Rigidbody>();
        if (rigidbody == null) {
            rigidbody = collider.GetComponentInParent<Rigidbody>();
        }
        
        if(rigidbody == null)
            return;

        //var force = collider.transform.position - transform.position;
        var force = transform.forward;
        //var force = GetComponent<Rigidbody>().velocity;
        force = 500*force.normalized;
        
        rigidbody.AddForceAtPosition(force, closestPoint);
    }
}
