using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyBullet : MonoBehaviour, IEnemyProjectile
{
    public float projectileDamage = 20f;
    public float burnDamage = 0;
    public float slowDamage = 0;

    public GameObject toSpawnOnDeath;
    
    [Header("Mostly Constant vars")]
    public float lifetime = 20f;
    
    public float speed = 7.5f;
    public float seekStrength = 20f;
    

    public bool isArrow = false;
    public float arrowLeaveChance = 1f;
    public GameObject arrowObject;

    [FoldoutGroup("Internal Variables")]
    public GameObject myOriginObject;
    [FoldoutGroup("Internal Variables")]
    public Cart target;
    [FoldoutGroup("Internal Variables")]
    public bool isDead = false;

    private void Start() {
        projectileDamage *= TweakablesMaster.s.myTweakables.enemyDamageMultiplier;
        burnDamage *= TweakablesMaster.s.myTweakables.enemyDamageMultiplier;
    }

    public Vector3 targetPoint;

    private void OnEnable() {
        isDead = false;
        if (instantDestroy)
            instantDestroy.SetActive(true);

        Invoke("DestroySelf", lifetime);
    }

    public void SetUp(GameObject originObject, Vector3 _initialVelocity) {
        myOriginObject = originObject;
        initialVelocity = _initialVelocity;
        if (initialVelocity.magnitude > LevelReferences.s.speed * 10) {
            initialVelocity = Vector3.zero;
        }
        target = null;
        
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 15, LevelReferences.s.buildingLayer)) {
            targetPoint = hit.point;
            target = hit.collider.GetComponentInParent<Cart>();
            targetPoint = target.transform.InverseTransformPoint(targetPoint);
            Debug.DrawLine(transform.position, targetPoint, Color.green, 1f);
        } else {
            targetPoint = transform.position + transform.forward * 15f;
            Debug.DrawLine(transform.position, targetPoint, Color.red,1f);
        }
    }

    public Vector3 GetInitialVelocity() {
        return initialVelocity;
    }


    public Vector3 initialVelocity;
    void LateUpdate() {
        if (!isDead) {
            if (target != null) {
                var targetLook = Quaternion.LookRotation(target.transform.position - transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLook, seekStrength * Time.fixedDeltaTime);
            }


            /*if (target != null) {
                if (Vector3.Distance(transform.position, target.position) < (speed + 0.1f)  * Time.fixedDeltaTime) {
                    DestroyFlying();
                }
            }*/

            var actualTarget = targetPoint;
            if (target != null) {
                actualTarget = target.transform.TransformPoint(targetPoint);
            } else {
                targetPoint += initialVelocity * Time.deltaTime;
                actualTarget = targetPoint;
            }
            transform.position += initialVelocity * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, actualTarget, speed*Time.deltaTime);
            if (Vector3.Distance(transform.position, actualTarget) < 0.005f) {
                ContactDamage(target);
                
                SmartDestroySelf();
            }
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
        }
    }

    private void DestroyFlying() {
        if (!isDead) {
            Debug.Log("flying death");
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

    private void ContactDamage(Cart _target) {
        if (projectileDamage == 0 && burnDamage == 0) {
            return;
        }
        
        var health = _target?.GetHealthModule();
        
        DealDamage(health);
        
        GameObject hitPrefab;
        
        var pos = transform.position;
        var rotation = Quaternion.AngleAxis(180,transform.up) * transform.rotation;

        if (health == null) { // we didnt hit the player
            hitPrefab = LevelReferences.s.dirtBulletHitEffectPrefab;
        }else{
            hitPrefab = LevelReferences.s.metalBulletHitEffectPrefab;
        }

        VisualEffectsController.s.SmartInstantiate(hitPrefab, pos, rotation, VisualEffectsController.EffectPriority.Low);
    }



    void DealDamage(ModuleHealth target) {
        if (target != null) {
            var dmg = projectileDamage;

            if (dmg > 0) {
                var burnChunk = target.DealDamage(dmg, transform.position, Quaternion.AngleAxis(180, transform.up) * transform.rotation);
                DealWithBurnChunk(burnChunk);
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

            if (slowDamage > 0) {
                SpeedController.s.AddSlow(slowDamage);
            }
        }
    }

    void DealWithBurnChunk(GameObject burnChunk) {
        if (isArrow && burnChunk != null) {
            var arrowMadeIt = Random.value <= arrowLeaveChance;

            if (arrowMadeIt) {
                var targetTransform = burnChunk.transform;
                var repairable = burnChunk.GetComponent<RepairableBurnEffect>();
                var rotatedRotation = Quaternion.AngleAxis(180, targetTransform.up) * targetTransform.rotation;
                repairable.arrow =  Instantiate(arrowObject, targetTransform.position, rotatedRotation, targetTransform);
                repairable.hasArrow = true;
                //Debug.Log(burnChunk.GetComponentInParent<Cart>().gameObject.name);
                //Debug.Break();
            } else {
                var arrow = VisualEffectsController.s.SmartInstantiate(arrowObject, arrowObject.transform.position, arrowObject.transform.rotation, VisualEffectsController.EffectPriority.Medium);
                if (arrow != null) {
                    arrow.AddComponent<Rigidbody>();
                    arrow.AddComponent<RubbleFollowFloor>();
                    arrow.GetComponent<Rigidbody>().AddForce(SmitheryController.GetRandomYeetForce() / 10);
                }
            }
        }
    }

}
