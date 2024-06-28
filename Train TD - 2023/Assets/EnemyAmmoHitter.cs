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

    private void OnEnable() {
        Invoke("DestroySelf", lifetime);

        isDead = false;
        
        if (actuallyDestroy != null) {
            Destroy(actuallyDestroy);
        }
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

    private GameObject actuallyDestroy;
    void DestroySelf() {
        GetComponent<PooledObject>().DestroyPooledObject();
    }
    void SmartDestroySelf() {
        if (!isDead) {
            isDead = true;
            
            var trail = GetComponentInChildren<SmartTrail>();
            if (trail != null) {
                trail.StopTrailing();
            }

            GetComponent<PooledObject>().lifeTime = ProjectileProvider.bulletAfterDeathLifetime;
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
                    actuallyDestroy = ammo.gameObject;
                    CancelInvoke();
                    Invoke(nameof(DestroySelf), 15);
                    isDead = true;
                    target = null;
                    GetComponent<Rigidbody>().velocity = (transform.forward * speed ) + (initialVelocity);
                }
            }
        }
    }

    
}
