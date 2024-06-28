using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyRocket : MonoBehaviour,IEnemyProjectile{
    public float projectileDamage = 20f;
    public float burnDamage = 0;

    public GameObject toSpawnOnDeath;
    
    [Header("Mostly Constant vars")]
    public float lifetime = 20f;
    
    public float topSpeed = 5f;
    public float acceleration = 2.5f;
    public float topSeekStrength = 440f;
    public float seekAcceleration = 200f;
    

    [FoldoutGroup("Internal Variables")]
    public GameObject myOriginObject;
    [FoldoutGroup("Internal Variables")]
    public Transform target;
    [FoldoutGroup("Internal Variables")]
    public bool isDead = false;
    [FoldoutGroup("Internal Variables")] 
    public float curSpeed;
    [FoldoutGroup("Internal Variables")] 
    public float curSeekStrength;
    
    
    private void Start() {
        projectileDamage *= TweakablesMaster.s.myTweakables.enemyDamageMultiplier;
        burnDamage *= TweakablesMaster.s.myTweakables.enemyDamageMultiplier;
    }

    private void OnEnable() {
        isDead = false;
        curSpeed = 0;
        curSeekStrength = 0;
        if (instantDestroy)
            instantDestroy.SetActive(true);
        
        Invoke("DestroySelf", lifetime);
    }
    
    public void SetUp(GameObject originObject,  Vector3 _initialVelocity) {
        myOriginObject = originObject;
        initialVelocity = _initialVelocity;
    }

    public Vector3 GetInitialVelocity() {
        return initialVelocity;
    }


    public Vector3 initialVelocity;
    void FixedUpdate() {
        if (!isDead) {
            if (target != null) {
                var targetLook = Quaternion.LookRotation(target.position - transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLook, curSeekStrength * Time.fixedDeltaTime);
            }

            curSpeed = Mathf.MoveTowards(curSpeed, topSpeed, acceleration * Time.fixedDeltaTime);
            curSeekStrength = Mathf.MoveTowards(curSeekStrength, topSeekStrength, seekAcceleration * Time.fixedDeltaTime);

            if (target != null) {
                if (Vector3.Distance(transform.position, target.position) < (curSpeed + 0.1f) * Time.fixedDeltaTime) {
                    DestroyFlying();
                }
            }

            GetComponent<Rigidbody>().velocity =  (transform.forward * curSpeed) + (initialVelocity);
        }
    }
    
    public GameObject instantDestroy;
    void DestroySelf() {
        GetComponent<PooledObject>().DestroyPooledObject();
    }
    void SmartDestroySelf() {
        if (!isDead) {
            isDead = true;

            var particles = GetComponentsInChildren<ParticleSystem>();

            foreach (var particle in particles) {
                if (particle.gameObject != instantDestroy) {
                    particle.Stop();
                }
            }
            
            var trail = GetComponentInChildren<SmartTrail>();
            if (trail != null) {
                trail.StopTrailing();
            }
            
            if(instantDestroy != null)
                instantDestroy.SetActive(false);

            GetComponent<PooledObject>().lifeTime = ProjectileProvider.bulletAfterDeathLifetime;

            GetComponent<Rigidbody>().detectCollisions = false;
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

                var enemy = other.transform.root.GetComponent<EnemyWavesController>();
                
                if (enemy != null) {
                    // make enemy projectiles not hit other enemy projectiles
                    return;
                }
                
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

        VisualEffectsController.s.SmartInstantiate(hitPrefab, pos, rotation, VisualEffectsController.EffectPriority.Medium);
    }



    void DealDamage(ModuleHealth target) {
        if (target != null) {
            var dmg = projectileDamage;
            
            if (dmg > 0) {
                target.DealDamage(dmg, transform.position, Quaternion.AngleAxis(180, transform.up) * transform.rotation);
                if (dmg > 1) {
                    var damageNumbers = VisualEffectsController.s.SmartInstantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent,
                        VisualEffectsController.EffectPriority.damageNumbers);
                    if (damageNumbers != null) {
                        damageNumbers.GetComponent<MiniGUI_DamageNumber>()
                            .SetUp(target.GetUITransform(), (int)dmg, false, false, false);
                    }
                }
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
