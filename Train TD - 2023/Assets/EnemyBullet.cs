using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyBullet : MonoBehaviour, IEnemyProjectile
{
    public float projectileDamage = 20f;
    public float burnDamage = 0;


    public GameObject toSpawnOnDeath;
    
    [Header("Mostly Constant vars")]
    public float lifetime = 20f;
    
    public float speed = 7.5f;
    public float seekStrength = 20f;
    
    public GameObject instantDestroy;

    [FoldoutGroup("Internal Variables")]
    public GameObject myOriginObject;
    [FoldoutGroup("Internal Variables")]
    public Transform target;
    [FoldoutGroup("Internal Variables")]
    public bool isDead = false;

    private void Start() {
        Invoke("DestroySelf", lifetime);

        projectileDamage *= TweakablesMaster.s.myTweakables.enemyDamageMultiplier;
        burnDamage *= TweakablesMaster.s.myTweakables.enemyDamageMultiplier;
    }
    
    public void SetUp(GameObject originObject, Transform _target) {
        myOriginObject = originObject;
        target = _target;
    }

    void DestroySelf() {
        Destroy(gameObject);
    }

    void FixedUpdate() {
        if (!isDead) {
            if (target != null) {
                var targetLook = Quaternion.LookRotation(target.position - transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLook, seekStrength * Time.fixedDeltaTime);
            }


            if (target != null) {
                if (Vector3.Distance(transform.position, target.position) < (speed + 0.1f)  * Time.fixedDeltaTime) {
                    DestroyFlying();
                }
            }

            GetComponent<Rigidbody>().MovePosition(transform.position + transform.forward * speed * Time.fixedDeltaTime);
        }
    }

    void SmartDestroySelf() {
        if (!isDead) {
            isDead = true;

            var particles = GetComponentsInChildren<ParticleSystem>();

            foreach (var particle in particles) {
                if (particle.gameObject != instantDestroy) {
                    particle.transform.SetParent(VisualEffectsController.s.transform);
                    particle.transform.localScale = Vector3.one;
                    particle.Stop();
                    Destroy(particle.gameObject, 1f);
                }
            }

            if(toSpawnOnDeath != null)
                Instantiate(toSpawnOnDeath, transform.position, transform.rotation);
            
            Destroy(instantDestroy);
            Destroy(gameObject);
        }
    }

    private void DestroyFlying() {
        if (!isDead) {
            if (target == null) {
                SmartDestroySelf();
                return;
            }
            
            var health = target.GetComponentInParent<ModuleHealth>();

            if (health != null) {
                DealDamage(health);
            }

            SmartDestroySelf();
        }
    }


    private void OnCollisionEnter(Collision other) {
        if (!isDead) {
            ContactDamage(other);

            SmartDestroySelf();
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!isDead) {
            if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject != myOriginObject) {
                PhaseDamage(other);
            }
        }
    }

    void PhaseDamage(Collider other) {
        var health = other.gameObject.GetComponentInParent<ModuleHealth>();

        if (health != null) {
            DealDamage(health);
        }
    }

    private void ContactDamage(Collision other) {
        if (projectileDamage == 0 && burnDamage == 0) {
            return;
        }
        
        var health = other.collider.gameObject.GetComponentInParent<ModuleHealth>();
        
        DealDamage(health);
        
        GameObject hitPrefab;
        
        var contact = other.GetContact(0);
        var pos = contact.point;
        var rotation = Quaternion.LookRotation(contact.normal);

        if (health == null) { // we didnt hit the player
            hitPrefab = LevelReferences.s.dirtBulletHitEffectPrefab;
        }else{
            hitPrefab = LevelReferences.s.metalBulletHitEffectPrefab;
        }

        VisualEffectsController.s.SmartInstantiate(hitPrefab, pos, rotation);
    }



    void DealDamage(ModuleHealth target) {
        if (target != null) {
            var dmg = projectileDamage;
            
            if (dmg > 0) {
                target.DealDamage(dmg, transform.position);
                if(dmg > 1)
                    VisualEffectsController.s.SmartInstantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent)
                        .GetComponent<MiniGUI_DamageNumber>()
                        .SetUp(target.GetUITransform(), (int)dmg, false, false, false);
            }

            if (burnDamage > 0) {
                target.BurnDamage(burnDamage);
                /*if(burnDamage > 1)
                    VisualEffectsController.s.SmartInstantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent)
                        .GetComponent<MiniGUI_DamageNumber>()
                        .SetUp(target.GetUITransform(), (int)burnDamage, isPlayerBullet, armorProtected, true);*/
            }
        }
    }

    
}
