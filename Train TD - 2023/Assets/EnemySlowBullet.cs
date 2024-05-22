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
    
    public GameObject instantDestroy;

    [FoldoutGroup("Internal Variables")]
    public GameObject myOriginObject;
    [FoldoutGroup("Internal Variables")]
    public Transform target;
    [FoldoutGroup("Internal Variables")]
    public bool isDead = false;

    private void Start() {
        Invoke("DestroySelf", lifetime);
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
