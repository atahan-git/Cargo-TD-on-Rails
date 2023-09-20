using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AOEDamage : MonoBehaviour {

    public bool singleDamage = false;
    
    public float projectileDamage = 20f;
    public float burnDamage = 0;

    public bool isHeal = false;

    public GameObject myOriginObject;

    public bool isPlayerBullet = false;

    public bool canPenetrateArmor = false;
    
    public bool isSlowDamage = false;
    public float damageRate = 0.2f;


    public float curTime = 0;
    private void Update() {
        curTime -= Time.deltaTime;
        if (curTime <= 0) {
            curTime = damageRate;


            for (int i = 0; i < damageList.Count; i++) {
                var target = damageList[i];
                if (target != null && target.IsAlive()) {
                    DealDamage(target);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.transform.root.gameObject != myOriginObject) {
            var otherProjectile = other.transform.root.GetComponent<Projectile>();
            if (otherProjectile != null) {
                if (otherProjectile.isPlayerBullet == isPlayerBullet) {
                    // we don't want projectiles from the same faction collide with each other
                    return;
                }
            }

            var train = other.transform.root.GetComponent<Train>();

            if (train != null && isPlayerBullet) {
                // make player bullets dont hit the player
                return;
            }

            var enemy = other.transform.root.GetComponent<EnemyTypeData>();

            if (enemy != null && !isPlayerBullet) {
                // make enemy projectiles not hit the player projectiles
                return;
            }

            if (singleDamage) {
                var health = other.gameObject.GetComponentInParent<IHealth>();
                DealDamage(health);
            } else {
                AddToDamageList(other);
            }
            //SmartDestroySelf();
        }
    }

    private void OnTriggerExit(Collider other) {
        var health = other.gameObject.GetComponentInParent<IHealth>();
        if (health != null) {
            if (damageList.Contains(health)) {
                damageList.Remove(health);
            }
        }
    }


    public List<IHealth> damageList = new List<IHealth>();

    void AddToDamageList(Collider other) {
        var health = other.gameObject.GetComponentInParent<IHealth>();

        if (health != null) {
            if (!damageList.Contains(health))
                damageList.Add(health);
        }
    }

    void DealDamage(IHealth target) {
        if (target != null) {
            var dmg = projectileDamage;
            var armorProtected = false;
            if (target.HasArmor() && !canPenetrateArmor) {
                dmg = projectileDamage/ 2;
                armorProtected = true;
            }

            if (isSlowDamage) {
                if (target.IsPlayer()) {
                    SpeedController.s.AddSlow(projectileDamage);
                } else {
                    target.GetGameObject().GetComponentInParent<EnemyWave>().AddSlow(projectileDamage);
                }
            } else if (isHeal) {
                target.Repair(dmg);
            } else {
                target.BurnDamage(burnDamage);
                target.DealDamage(dmg);
                Instantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent)
                    .GetComponent<MiniGUI_DamageNumber>()
                    .SetUp(target.GetUITransform(), (int)dmg, isPlayerBullet, armorProtected, false);
                
                Instantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent)
                    .GetComponent<MiniGUI_DamageNumber>()
                    .SetUp(target.GetUITransform(), (int)burnDamage, isPlayerBullet, armorProtected, true);
            }
        }
    }
}
