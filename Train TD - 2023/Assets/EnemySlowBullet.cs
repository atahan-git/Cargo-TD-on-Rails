using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemySlowBullet : MonoBehaviour,IEnemyProjectile

{
    public float slowAmount = 1f;
    public GameObject toSpawnOnDeath;
    
    [Header("Mostly Constant vars")]
    public float lifetime = 20f;
    
    public float speed = 7.5f;
    public float seekStrength = 20f;
    

    [FoldoutGroup("Internal Variables")]
    public GameObject myOriginObject;
    [FoldoutGroup("Internal Variables")]
    public Transform target;
    [FoldoutGroup("Internal Variables")]
    public bool isDead = false;

    private void OnEnable() {
        isDead = false;
        if (instantDestroy)
            instantDestroy.SetActive(true);
        
        Invoke("DestroySelf", lifetime);
    }

    public void SetUp(GameObject originObject, Vector3 _initialVelocity) {
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
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLook, seekStrength * Time.fixedDeltaTime);
            }


            if (target != null) {
                if (Vector3.Distance(transform.position, target.position) < (speed + 0.1f)  * Time.fixedDeltaTime) {
                    DestroyFlying();
                }
            }

            GetComponent<Rigidbody>().velocity = (transform.forward * speed ) + (initialVelocity);
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
        var health = other.collider.gameObject.GetComponentInParent<ModuleHealth>();
        
        DealDamage(health);
        
        /*GameObject hitPrefab;
        
        var contact = other.GetContact(0);
        var pos = contact.point;
        var rotation = Quaternion.LookRotation(contact.normal);

        if (health == null) { // we didnt hit the player
            hitPrefab = LevelReferences.s.dirtBulletHitEffectPrefab;
        }else{
            hitPrefab = LevelReferences.s.metalBulletHitEffectPrefab;
        }

        VisualEffectsController.s.SmartInstantiate(hitPrefab, pos, rotation);*/
    }



    void DealDamage(ModuleHealth target) {
        if (target != null) {
            SpeedController.s.AddSlow(slowAmount);
        }
    }
}
