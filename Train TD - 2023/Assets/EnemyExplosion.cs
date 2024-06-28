using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyExplosion : MonoBehaviour {

    public float explosionDamage = 50;
    public float explosionBurn = 0;
    public float explosionRange = 0.5f;
    private void Start() {
        explosionDamage *= TweakablesMaster.s.myTweakables.enemyDamageMultiplier;
        explosionBurn *= TweakablesMaster.s.myTweakables.enemyDamageMultiplier;
        
        OverlapSphereDamage();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }

    void OverlapSphereDamage() {
        var targets = Physics.OverlapSphere(transform.position, explosionRange, LevelReferences.s.buildingLayer);

        var healths = new List<ModuleHealth>();
        for (int i = 0; i < targets.Length; i++) {
            var health = targets[i].gameObject.GetComponentInParent<ModuleHealth>();

            if (health != null) {
                if (health.IsPlayer()) {
                    if (!healths.Contains(health)) {
                        healths.Add(health);
                    }
                }
            }
        }

        for (int i = 0; i < healths.Count; i++) {
            DealDamage(healths[i]);
        }
    }

    void DealDamage(ModuleHealth target) {
        if (target != null) {

            if (explosionDamage > 0) {
                target.DealDamage(explosionDamage, transform.position, Quaternion.AngleAxis(180, transform.up) * transform.rotation);
                if (explosionDamage > 1) {
                    var damageNumbers = VisualEffectsController.s.SmartInstantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent,
                        VisualEffectsController.EffectPriority.damageNumbers);
                    if (damageNumbers != null) {
                        damageNumbers.GetComponent<MiniGUI_DamageNumber>()
                            .SetUp(target.GetUITransform(), (int)explosionDamage, false, false, false);
                    }
                }
            }

            if (explosionBurn > 0) {
                target.BurnDamage(explosionBurn);
                /*if(burnDamage > 1)
                    VisualEffectsController.s.SmartInstantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent)
                        .GetComponent<MiniGUI_DamageNumber>()
                        .SetUp(target.GetUITransform(), (int)burnDamage, isPlayerBullet, armorProtected, true);*/
            }
            
        }
    }
}
