using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour {
    private float curSpeed = 0;
    private float curSeekStrength = 0;
    public float acceleration = 5f;
    public float seekAcceleration = 200f;
    public float speed = 5f;
    public float damage = 20f;
    public float mortarRange = 2f;

    public float hitForceMultiplier = 20f;

    public float lifetime = 20f;
    
    public GameObject myOriginObject;

    public bool isTargetSeeking = true;
    public float seekStrength = 180f;

    public bool isPlayerBullet = false;

    public bool canPenetrateArmor = false;

    public Transform target {
        get {
            return source.target;
        }
    }
    public GunModule source;

    public GenericCallback onHitCallback;

    
    public enum HitType {
        Bullet, Rocket, Mortar, Laser
    }

    public HitType myHitType = HitType.Bullet;

    public Vector3 mortarGravity = new Vector3(0, -9.81f, 0f);
    private Vector3 mortarVelocity;
    public float mortarAimPredictTime = 1f;
    public float mortarVelocityMultiplier = 0.5f;

    public LineRenderer myLine;

    public bool isPhaseThrough = false;
    public bool isBurnDamage = false;
    private void Start() {
        Invoke("DestroySelf", lifetime);

        switch (myHitType) {
            case HitType.Bullet:
                curSpeed = speed;
                curSeekStrength = seekStrength;
                break;
            
            case HitType.Rocket:
                curSpeed = 0;
                curSeekStrength = 0;
                break;
            
            case HitType.Mortar:
                curSpeed = speed;
                curSeekStrength = seekStrength;
                var targetPos = target.position;
                
                var enemyWave = target.GetComponentInParent<EnemyWave>();

                
                if (enemyWave != null) {
                    targetPos +=  Vector3.forward * enemyWave.currentSpeed * mortarAimPredictTime;
                }

                var randomOffset = Random.insideUnitSphere * mortarRange;
                randomOffset.y = 0;
                targetPos += randomOffset;
                
                float angle = 90- Vector3.Angle(Vector3.up, transform.forward);

                //print(transform.forward);
                var predictedDirection = targetPos - transform.position;
                predictedDirection = new Vector3(predictedDirection.x, 0, predictedDirection.z);
                var rotAxis = Vector3.Cross(predictedDirection, Vector3.up);
                predictedDirection = Quaternion.AngleAxis(angle, rotAxis) * predictedDirection;
                transform.rotation = Quaternion.LookRotation(predictedDirection);
                //print(transform.forward);
                
                float gx = mortarGravity.y * Vector3.Distance(transform.position, targetPos);
                float sinVal = 0.5f* Mathf.Sin(angle);
                float velocity = Mathf.Sqrt(Mathf.Abs(gx / sinVal));


                mortarVelocity = transform.forward.normalized * velocity;
                mortarVelocity *= mortarVelocityMultiplier;
                break;
            
            case HitType.Laser:
                HitScan();
                break;
        }
    }

    public LayerMask hitScanLayerMask;
    public int hitScanRange = 10;

    void HitScan() {
        var hitPrefab = LevelReferences.s.laserHitPrefab;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, hitScanRange, hitScanLayerMask)) {
            if (hit.transform.root.gameObject != myOriginObject) {
                var otherProjectile = hit.transform.root.GetComponent<Projectile>();
                if (otherProjectile != null) {
                    if (otherProjectile.isPlayerBullet == isPlayerBullet) {
                        // we don't want projectiles from the same faction collide with each other
                        return;
                    }
                }

                var train = hit.transform.root.GetComponent<Train>();

                if (train != null && isPlayerBullet) {
                    // make player bullets dont hit the player
                    return;
                }

                var enemy = hit.transform.root.GetComponent<EnemyTypeData>();

                if (enemy != null && !isPlayerBullet) {
                    // make enemy projectiles not hit the player projectiles
                    return;
                }
                
                
                var health = hit.transform.GetComponentInParent<IHealth>();
        
                DealDamage(health);
                
                var pos = hit.point;
                var rotation = Quaternion.LookRotation(hit.normal);

                Instantiate(hitPrefab, pos, rotation);

                SmartDestroySelf();
            }
        }
    }

    void DestroySelf() {
        Destroy(gameObject);
    }

    void FixedUpdate() {
        if (!isDead) {
            if (isTargetSeeking) {
                if (target != null) {
                    var targetLook = Quaternion.LookRotation(target.position - transform.position);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLook, curSeekStrength * Time.fixedDeltaTime);
                } else {
                    isTargetSeeking = false;
                }
            }

            if (myHitType == HitType.Rocket) {
                curSpeed = Mathf.MoveTowards(curSpeed, speed, acceleration * Time.fixedDeltaTime);
                curSeekStrength = Mathf.MoveTowards(curSeekStrength, seekStrength, seekAcceleration * Time.fixedDeltaTime);
            }

            if (target != null) {
                if (Vector3.Distance(transform.position, target.position) < (curSpeed + 0.1f) * Time.fixedDeltaTime) {
                    if (!isPhaseThrough) {
                        DestroyFlying();
                    }
                }
            }

            switch (myHitType) {
                case HitType.Bullet:
                case HitType.Rocket:
                    GetComponent<Rigidbody>().MovePosition(transform.position + transform.forward * curSpeed * Time.fixedDeltaTime);
                    break;
                case HitType.Mortar:
                    GetComponent<Rigidbody>().MovePosition(transform.position + mortarVelocity * Time.fixedDeltaTime);
                    mortarVelocity += mortarGravity * Time.fixedDeltaTime;

                    transform.rotation = Quaternion.LookRotation(mortarVelocity);
                    break;
            }
        }
    }

    private bool isDead = false;

    public GameObject instantDestroy;

    void SmartDestroySelf() {
        if (!isDead) {
            isDead = true;

            var particles = GetComponentsInChildren<ParticleSystem>();

            foreach (var particle in particles) {
                if (particle.gameObject != instantDestroy) {
                    particle.transform.SetParent(null);
                    particle.Stop();
                    Destroy(particle.gameObject, 1f);
                }
            }
            
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
            //print("destroyflying");
            GameObject hitPrefab = null;
            switch (myHitType) {
                case HitType.Bullet:
                    hitPrefab = LevelReferences.s.metalBulletHitEffectPrefab;
                    break;
                case HitType.Rocket:
                    hitPrefab = LevelReferences.s.rocketExplosionEffectPrefab;
                    break;
                case HitType.Mortar:
                    hitPrefab = null;
                    break;
            }

            if (myHitType != HitType.Mortar) {
                var health = target.GetComponentInParent<IHealth>();

                if (health != null) {
                    DealDamage(health);
                }
            } else {
                MortarDamage();
            }

            if (hitPrefab != null) {
                Instantiate(hitPrefab, transform.position, transform.rotation);
            }

            SmartDestroySelf();
        }
    }


    private void OnCollisionEnter(Collision other) {
        if (!isDead) {
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
                
                switch (myHitType) {
                    case HitType.Bullet:
                        ContactDamage(other);
                        break;
                    case HitType.Rocket:
                        ExplosiveDamage(other);
                        break;
                    case HitType.Mortar:
                        MortarDamage();
                        break;
                }

                SmartDestroySelf();
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!isDead) {
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
                
                PhaseDamage(other);

                //SmartDestroySelf();
            }
        }
    }

    void PhaseDamage(Collider other) {
        var health = other.gameObject.GetComponentInParent<IHealth>();

        if (health != null) {
            DealDamage(health);
            
            GameObject miniHitPrefab = LevelReferences.s.mortarMiniHitPrefab;
            var closestPoint = health.GetMainCollider().ClosestPoint(transform.position);
            Instantiate(miniHitPrefab, closestPoint, Quaternion.identity);
        }
    }

    private void MortarDamage() {
        GameObject hitPrefab = LevelReferences.s.mortarExplosionEffectPrefab;
        GameObject miniHitPrefab = LevelReferences.s.mortarMiniHitPrefab;

        var targets = Physics.OverlapSphere(transform.position, mortarRange);

        var healthsInRange = new List<IHealth>();
        var healthsInRangeGms = new List<GameObject>();
        for (int i = 0; i < targets.Length; i++) {
            var target = targets[i];
            
            var health = target.gameObject.GetComponentInParent<IHealth>();
            if (health != null && !health.IsPlayer()) {
                if (!healthsInRange.Contains(health)) {
                    healthsInRange.Add(health);
                }
            }
        }

        foreach (var health in healthsInRange) {
            DealDamage(health);
            ApplyHitForceToObject(health);
            var closestPoint = health.GetMainCollider().ClosestPoint(transform.position);
            Instantiate(miniHitPrefab, closestPoint, Quaternion.identity);
        }

        Instantiate(hitPrefab, transform.position, Quaternion.identity);
    }

    private void ExplosiveDamage(Collision other) {
        var contact = other.GetContact(0);
        var pos = contact.point;
        var rotation = Quaternion.LookRotation(contact.normal);

        GameObject hitPrefab = LevelReferences.s.rocketExplosionEffectPrefab;

        var health = other.gameObject.GetComponentInParent<IHealth>();
        
        if (health != null) {
            DealDamage(health);
            ApplyHitForceToObject(health);
        }

        Instantiate(hitPrefab, pos, rotation);
    }

    private void ContactDamage(Collision other) {
        var health = other.gameObject.GetComponentInParent<IHealth>();
        
        DealDamage(health);
        
        GameObject hitPrefab;
        
        var contact = other.GetContact(0);
        var pos = contact.point;
        var rotation = Quaternion.LookRotation(contact.normal);


        if (health == null) { // we didnt hit the player or the enemies
            hitPrefab = LevelReferences.s.dirtBulletHitEffectPrefab;
            
        }else{
            if (health is ModuleHealth) {
                hitPrefab = LevelReferences.s.metalBulletHitEffectPrefab;

            } else {
                ApplyHitForceToObject(health);


                if (health.HasArmor() && !canPenetrateArmor) {
                    // if enemy has armor and we cannot penetrate it, show it through an effect
                    hitPrefab = LevelReferences.s.enemyCantPenetrateHitEffectPrefab;
                } else {
                    hitPrefab = LevelReferences.s.enemyRegularHitEffectPrefab;
                }
            }
        }


        Instantiate(hitPrefab, pos, rotation);
    }


    void ApplyHitForceToObject(IHealth health) {
        var collider = health.GetMainCollider();
        var closestPoint = collider.ClosestPoint(transform.position);
        var rigidbody = collider.GetComponent<Rigidbody>();
        if (rigidbody == null) {
            rigidbody = collider.GetComponentInParent<Rigidbody>();
        }

        var force = collider.transform.position - transform.position;
        force = force.normalized * damage * hitForceMultiplier;
        
        rigidbody.AddForceAtPosition(force, closestPoint);
    }


    void DealDamage(IHealth target) {
        if (target != null) {
            var dmg = damage;
            var armorProtected = false;
            if (target.HasArmor() && !canPenetrateArmor) {
                dmg = damage/ 2;
                armorProtected = true;
            }

            if (isBurnDamage) {
                target.BurnDamage(dmg);
            } else {
                target.DealDamage(dmg);
                Instantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent)
                    .GetComponent<MiniGUI_DamageNumber>()
                    .SetUp(target.GetGameObject().transform, (int)dmg, isPlayerBullet, armorProtected, isBurnDamage);
            }

            onHitCallback?.Invoke();
        }
    }
}
