using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyAmmoHitter : MonoBehaviour, IEnemyProjectile
{
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

    private void Start() {
        Invoke("DestroySelf", lifetime);
    }
    
    public void SetUp(GameObject originObject, Transform _target, Vector3 _initialVelocity) {
        myOriginObject = originObject;
        target = _target;
        initialVelocity = _initialVelocity;
    }

    public Vector3 GetInitialVelocity() {
        return initialVelocity;
    }

    void DestroySelf() {
        Destroy(gameObject);
    }

    public Vector3 initialVelocity;
    void FixedUpdate() {
        if (!isDead) {
            if (target != null) {
                var forwardSpeed = Train.s.GetTrainForward() * LevelReferences.s.speed * Time.deltaTime;
                var targetLook = Quaternion.LookRotation((target.position+forwardSpeed) - transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLook, seekStrength * Time.fixedDeltaTime);
            }


            if (target != null) {
                if (Vector3.Distance(transform.position, target.position) < speed * Time.fixedDeltaTime + 0.05f) {
                    DestroyFlying();
                }
            }

            GetComponent<Rigidbody>().velocity = (transform.forward * speed ) + (initialVelocity);
        }
    }

    void SmartDestroySelf() {
        if (!isDead) {
            isDead = true;

            var particles = GetComponentsInChildren<ParticleSystem>();

            foreach (var particle in particles) {
                particle.transform.SetParent(VisualEffectsController.s.transform);
                particle.transform.localScale = Vector3.one;
                particle.Stop();
                Destroy(particle.gameObject, 1f);
            }

            var trail = GetComponentInChildren<SmartTrail>();
            if (trail != null) {
                trail.StopTrailing();
                trail.transform.SetParent(VisualEffectsController.s.transform);
                Destroy(trail.gameObject, 1f);
            }

            if(toSpawnOnDeath != null)
                Instantiate(toSpawnOnDeath, transform.position, transform.rotation);
            
            
            Destroy(gameObject);
        }
    }

    private void DestroyFlying() {
        if (!isDead) {
            if (target == null) {
                SmartDestroySelf();
                return;
            }
            
            KnockAmmoOff(target);

            SmartDestroySelf();
        }
    }


    private void OnCollisionEnter(Collision other) {
        if (!isDead) {
            KnockAmmoOff(other.transform);

            SmartDestroySelf();
        }
    }

    void KnockAmmoOff(Transform ammo) {
        if (ammo != null) {
            if (ammo.name.Contains("ammo")) {
                var ammoBar = ammo.GetComponentInParent<PhysicalAmmoBar>();
                if ( ammoBar != null) {
                    ammoBar.RemoveChunk(ammo.gameObject);
                    
                    ammo.transform.SetParent(transform);
                    gameObject.AddComponent<RubbleFollowFloor>();
                    CancelInvoke();
                    Invoke(nameof(DestroySelf), 15);
                    isDead = true;
                    target = null;
                }
            }
        }
    }

    
}
