using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour {
    public ActiveData currentData;
    public ActiveData defaultData;

    public float hitForceMultiplier = 20f;

    public float lifetime = 20f;

    public enum HitType {
        Bullet=0, Rocket=1, Mortar=2, Laser=3, Railgun=4
    }

    public HitType myHitType = HitType.Bullet;
    
    [Serializable]
    public class ActiveData {
        public bool isTargetingPlayer = false;
        public bool didHitTarget = false;

        [Space]
        public float acceleration = 2.5f;
        public float seekAcceleration = 100f;
        public float speed = 7.5f;
        public float explosionRange = 0;
        
        [Space]
        public bool isPhaseThrough = false;
        [ShowIf("isPhaseThrough")]
        public Vector3 phaseDamageHalfExtends;
        public bool isHoming = false;
        public bool isAmmoHitter = false;
        
        [Space]
        public bool isSlowDamage = false;
        [ShowIf("isSlowDamage")]
        public float slowDamage = 2.5f;
        
        [Space]
        public bool isHeal = false;
        [ShowIf("isHeal")]
        public int healChunkCount = 2;
        
        [Space]
        public bool isTargetSeeking = true;
        [ShowIf("isTargetSeeking")]
        public float seekStrength = 20f;
        [ShowIf("isTargetSeeking")]
        public float stopSeekTime = 0.5f;
        
        [Space]
        public float projectileDamage = 20f;
        public float burnDamage = 0;
        public float normalizedBurnDamage = 0;
        public int extraBurnTier = 0;
        [Space]
        public bool leaveArrow = false;
        [ShowIf("leaveArrow")]
        public float leaveArrowChance = 0.5f;
        
        
        [Space]
        public bool scaleUpOverTime = false;
        [ShowIf("scaleUpOverTime")]
        public float scaleUpSpeed = 2f;
        [ShowIf("scaleUpOverTime")]
        public Vector3 baseScale = Vector3.one;

        [FoldoutGroup("Internal Variables")]
        public float curSpeed = 0;
        [FoldoutGroup("Internal Variables")]
        public float curSeekStrength = 0;
        
        [Space]
        [FoldoutGroup("Internal Variables")]
        public GameObject originObject;
        [FoldoutGroup("Internal Variables")]
        public Transform target;
        [FoldoutGroup("Internal Variables")]
        public EnemyHealth targetEnemyHealth;
        [FoldoutGroup("Internal Variables")]
        public ModuleHealth targetPlayerHealth;
        
        [Space]
        [FoldoutGroup("Internal Variables")]
        public Vector3 targetPoint;

        [FoldoutGroup("Internal Variables")]
        public Vector3 initialVelocity;
        
        [FoldoutGroup("Internal Variables")]
        public float aliveTime;

        [Space]
        [FoldoutGroup("Internal Variables")]
        public GenericCallback onHitCallback;
        [FoldoutGroup("Internal Variables")]
        public GenericCallback onMissCallback;

        [Space] 
        [FoldoutGroup("Internal Variables")]
        public Vector3 hitPoint;
        [FoldoutGroup("Internal Variables")] 
        public Vector3 hitNormal;
        public void CopyTo (ActiveData copy) {
            copy.isTargetingPlayer = isTargetingPlayer;
            
            copy.curSpeed = curSpeed;
            copy.curSeekStrength = curSeekStrength;
            copy.acceleration = acceleration;
            copy.seekAcceleration = seekAcceleration;
            copy.speed = speed;
            copy.projectileDamage = projectileDamage;
            copy.burnDamage = burnDamage;
            copy.explosionRange = explosionRange;
            
            
            copy.isPhaseThrough = isPhaseThrough;
            copy.phaseDamageHalfExtends = phaseDamageHalfExtends;
            copy.isHoming = isHoming;
            copy.isAmmoHitter = isAmmoHitter;
            
            copy.isSlowDamage = isSlowDamage;
            copy.slowDamage = slowDamage;
            
            copy.isHeal = isHeal;
            copy.healChunkCount = healChunkCount;
            
            copy.isTargetSeeking = isTargetSeeking;
            copy.seekStrength = seekStrength;
            
            
            copy.scaleUpOverTime = scaleUpOverTime;
            copy.scaleUpSpeed = scaleUpSpeed;
            copy.baseScale = baseScale;
            
            
            copy.leaveArrow = leaveArrow;
            copy.leaveArrowChance = leaveArrowChance;
        }
    }
    
    public void SetUp(ActiveData data) {
        currentData = data;
        currentData.didHitTarget = false;
        
        GetComponent<PooledObject>().lifeTime = lifetime;

        isDead = false;

        if (instantDestroy)
            instantDestroy.SetActive(true);

        if (currentData.isHoming) {
            currentData.seekStrength *= 5;
            currentData.isTargetSeeking = true;
        }
        
        if (currentData.scaleUpOverTime) {
            transform.localScale = currentData.baseScale;
        }
        
        switch (myHitType) {
            case HitType.Bullet:
                currentData.curSpeed = currentData.speed;
                currentData.curSeekStrength =  currentData.seekStrength;
                break;
            
            case HitType.Rocket:
                //curSpeed = 0;
                //curSeekStrength = 0;
                break;
            
            case HitType.Laser:
                HitScan();
                break;
            
            case HitType.Railgun:
                if (raycastResultsArray == null || raycastResultsArray.Length <= 0) {
                    raycastResultsArray = new RaycastHit[64];
                }
                RailgunScan();
                break;
        }

        /*if (currentData.isPhaseThrough) {
            /*if (raycastResultsArray == null || raycastResultsArray.Length <= 0) {
                raycastResultsArray = new RaycastHit[64];
            }#1#
            if (explosiveOverlapArray == null || explosiveOverlapArray.Length <= 0) {
                explosiveOverlapArray = new Collider[64];
            }
        }*/

        if (currentData.explosionRange > 0) {
            if (explosiveOverlapArray == null || explosiveOverlapArray.Length <= 0) {
                explosiveOverlapArray = new Collider[64];
            }
        }
        
        prevPhaseDamaged.Clear();
    }

    private int hitScanRange = 50;

    void HitScan() {
        var hitType = CommonEffectsProvider.CommonEffectType.lazerHit;
        var hitScanLayerMask = currentData.isTargetingPlayer ? LevelReferences.s.buildingLayer : LevelReferences.s.enemyLayer;
        
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, hitScanRange, hitScanLayerMask)) {
            if (hit.rigidbody && hit.rigidbody.gameObject != currentData.originObject) {
                var health = hit.transform.GetComponentInParent<EnemyHealth>();

                currentData.didHitTarget = true;
                DealDamageToEnemy(health);
            
                var pos = hit.point;
                var rotation = Quaternion.LookRotation(hit.normal);

                CommonEffectsProvider.s.SpawnEffect(hitType, pos, rotation);
            }
        }
        
        DestroySelf();
    }

    private RaycastHit[] raycastResultsArray;
    private Collider[] explosiveOverlapArray;
    void RailgunScan() {
        var hitType = CommonEffectsProvider.CommonEffectType.mortarMiniHit;
        var hitScanLayerMask = currentData.isTargetingPlayer ? LevelReferences.s.buildingLayer : LevelReferences.s.enemyLayer;
        var count = Physics.RaycastNonAlloc(transform.position, transform.forward, raycastResultsArray, hitScanRange, hitScanLayerMask);
        for(int i = 0; i < count; i ++) {
            var hit = raycastResultsArray[i];
            if (!hit.rigidbody)
                return;
            
            if (hit.rigidbody.gameObject != currentData.originObject) {
                var health = hit.transform.GetComponentInParent<EnemyHealth>();
    
                currentData.didHitTarget = true;
                DealDamageToEnemy(health);
            
                var pos = hit.point;
                var rotation = Quaternion.LookRotation(hit.normal);

                CommonEffectsProvider.s.SpawnEffect(hitType, pos, rotation);
            }
        }
        

        var lineEndPos = transform.position + transform.forward * hitScanRange;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit2, hitScanRange, LevelReferences.s.groundLayer)) {
            lineEndPos = hit2.point + transform.forward*5;
        }

        var line = GetComponentInChildren<SmartTrail>();
        if (line) {
            line.RailgunOntoPoint(transform.position, lineEndPos);
        }

        SmartDestroySelf();
    }

    void FixedUpdate() {
        if (!isDead) {
            currentData.aliveTime += Time.deltaTime;
            if (currentData.isTargetSeeking && currentData.stopSeekTime >= 0 && currentData.aliveTime > currentData.stopSeekTime) {
                currentData.isTargetSeeking = false;
            }
            
            if ( currentData.isTargetSeeking) {
                if (currentData.target != null) {
                    var targetLook = Quaternion.LookRotation(currentData.target.position - transform.position);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLook, currentData.curSeekStrength * Time.fixedDeltaTime);
                } else {
                    currentData.isTargetSeeking = false;
                }
            }

            if (myHitType == HitType.Rocket) {
                currentData.curSpeed = Mathf.MoveTowards(currentData.curSpeed, currentData.speed, currentData.acceleration * Time.fixedDeltaTime);
                currentData.curSeekStrength = Mathf.MoveTowards(currentData.curSeekStrength,  currentData.seekStrength, currentData.seekAcceleration * Time.fixedDeltaTime);
            }
            
            
            switch (myHitType) {
                case HitType.Bullet:
                case HitType.Rocket:
                    var doMove = true;
                    if (currentData.isPhaseThrough) {
                        //PhaseThroughWhileDamaging();
                        // this is now done through a child trigger as god intended
                    }else if (RaycastCollisionCheck()) {
                        transform.position = currentData.targetPoint;

                        if (currentData.explosionRange <= 0) {
                            ContactDamage();
                        }else {
                            ExplosiveDamage();
                        }

                        doMove = false;
                        SmartDestroySelf();
                    }
                
                    if(doMove){
                        transform.position += transform.forward * currentData.speed * Time.deltaTime;
                    }
                    
                    break;
            }

            if (currentData.scaleUpOverTime) {
                transform.localScale = transform.localScale + Vector3.one*currentData.scaleUpSpeed*Time.deltaTime;
            }
        }
    }

    private List<EnemyHealth> prevPhaseDamaged = new List<EnemyHealth>();
    public void ChildTriggerEnter(Collider collider) {
        if (collider != null) {
            var enemyHealth = collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null) {
                if (!prevPhaseDamaged.Contains(enemyHealth)) {
                    prevPhaseDamaged.Add(enemyHealth);
                    DealDamageToEnemy(enemyHealth, true);
                }
            }
        }
    }

    bool RaycastCollisionCheck() {
        float minDistance = float.MaxValue;
        bool foundCollision = false;
        var ray = new Ray(transform.position, transform.forward);
        var hitInfo = new RaycastHit();

        if (currentData.target != null) {
            if (currentData.isTargetingPlayer) {
                if (currentData.targetPlayerHealth) {
                    for (int i = 0; i < currentData.targetPlayerHealth.surfaceColliders.Length; i++) {
                        var collider = currentData.targetPlayerHealth.surfaceColliders[i];
                        if (collider.Raycast(ray, out hitInfo, currentData.speed * Time.deltaTime * 1.2f)) {
                            var dist = Vector3.Distance(transform.position, hitInfo.point);
                            if (dist < minDistance) {
                                currentData.targetPoint = hitInfo.point;
                                minDistance = dist;
                                currentData.hitPoint = hitInfo.point;
                                currentData.hitNormal = hitInfo.normal;
                            }

                            currentData.didHitTarget = true;
                            foundCollision = true;

                            //Debug.DrawLine(transform.position, currentData.targetPoint, Color.green, 1f);
                            //Debug.DrawLine(currentData.targetPoint, currentData.targetPoint + Vector3.up, Color.green, 1f);
                        }
                    }
                }


            } 
        }

        if (!currentData.isTargetingPlayer) {
            if (Physics.Raycast(ray, out hitInfo, currentData.speed * Time.deltaTime * 1.2f, LevelReferences.s.enemyLayer)) {
                var targetHealth = hitInfo.collider.GetComponentInParent<EnemyHealth>();
                currentData.targetEnemyHealth = targetHealth;
                
                currentData.targetPoint = hitInfo.point;
                currentData.hitPoint = hitInfo.point;
                currentData.hitNormal = hitInfo.normal;
                
                currentData.didHitTarget = true;
                foundCollision = true;

                //Debug.DrawLine(transform.position, currentData.targetPoint, Color.magenta, 1f);
                //Debug.DrawLine(currentData.targetPoint, currentData.targetPoint + Vector3.up, Color.magenta, 1f);
            }
        }


        if (foundCollision) {
            currentData.didHitTarget = true;
        } else {
            // check collision with terrain if we didnt collide with our target

            if (currentData.isTargetingPlayer) {
                return false; // enemies never miss
            }


            if (Physics.Raycast(ray, out hitInfo, currentData.speed * Time.deltaTime * 1.2f, LevelReferences.s.groundLayer)) {
                currentData.targetPoint = hitInfo.point;
                currentData.hitPoint = hitInfo.point;
                currentData.hitNormal = hitInfo.normal;
                
                currentData.didHitTarget = false;
                foundCollision = true;
                
                
                //Debug.DrawLine(transform.position, currentData.targetPoint, Color.yellow, 1f);
                //Debug.DrawLine(currentData.targetPoint, currentData.targetPoint + Vector3.up, Color.yellow, 1f);
            }
        }

        return foundCollision;
    }

    public bool isDead = false;

    public GameObject instantDestroy;


    void DestroySelf() {
        if (!isDead) {
            isDead = true;
            GetComponent<PooledObject>().DestroyPooledObject();
            if (currentData.didHitTarget) {
                currentData.onHitCallback?.Invoke();
            } else {
                currentData.onMissCallback?.Invoke();
            }
        }
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

            if (instantDestroy != null)
                instantDestroy.SetActive(false);

            GetComponent<PooledObject>().lifeTime = ProjectileProvider.bulletAfterDeathLifetime;

            if (currentData.didHitTarget) {
                currentData.onHitCallback?.Invoke();
            } else {
                currentData.onMissCallback?.Invoke();
            }
        }
    }
    
    private void ExplosiveDamage() {
        var effectiveRange = Mathf.Sqrt(currentData.explosionRange);

        var layerMask = currentData.isTargetingPlayer ? LevelReferences.s.buildingLayer : LevelReferences.s.enemyLayer;
        
        var count = Physics.OverlapSphereNonAlloc(transform.position, effectiveRange, explosiveOverlapArray, layerMask);

        if (currentData.isTargetingPlayer) {
            var healthsInRange = new List<ModuleHealth>();
            for (int i = 0; i < count; i++) {
                var health = explosiveOverlapArray[i].gameObject.GetComponentInParent<ModuleHealth>();
                if (health != null) {
                    if (!healthsInRange.Contains(health)) {
                        healthsInRange.Add(health);
                    }
                }
            }
            
            foreach (var health in healthsInRange) {
                DealDamageToPlayer(health, false, false, Vector3.zero, Quaternion.identity);
            }

        } else {
            var healthsInRange = new List<EnemyHealth>();
            for (int i = 0; i < count; i++) {
                var health = explosiveOverlapArray[i].gameObject.GetComponentInParent<EnemyHealth>();
                if (health != null) {
                    if (!healthsInRange.Contains(health)) {
                        healthsInRange.Add(health);
                    }
                }
            }
            
            foreach (var health in healthsInRange) {
                ApplyHitForceToObject(health);
                DealDamageToEnemy(health);
            }
        }
        
        CommonEffectsProvider.s.SpawnEffect(CommonEffectsProvider.CommonEffectType.rocketExplosion, transform.position,Quaternion.identity,VisualEffectsController.EffectPriority.High);
    }

    private void ContactDamage() {
        if (currentData.isAmmoHitter) {
            KnockAmmoOff(currentData.target);
        }

        if (currentData.projectileDamage == 0 && currentData.burnDamage == 0 && !currentData.isSlowDamage) {
            return;
        }

        var pos = currentData.hitPoint;
        var rotation = Quaternion.LookRotation(currentData.hitNormal);
        
        if (currentData.didHitTarget) {
            if (currentData.isTargetingPlayer) {
                DealDamageToPlayer(currentData.targetPlayerHealth, true,true,pos, rotation);
                CommonEffectsProvider.s.SpawnEffect(CommonEffectsProvider.CommonEffectType.trainHit, pos, rotation, VisualEffectsController.EffectPriority.Low);
            } else {
                ApplyHitForceToObject(currentData.targetEnemyHealth);
                DealDamageToEnemy(currentData.targetEnemyHealth);
                CommonEffectsProvider.s.SpawnEffect(CommonEffectsProvider.CommonEffectType.enemyHit, pos, rotation, VisualEffectsController.EffectPriority.Low);
            }
            
        } else {
            CommonEffectsProvider.s.SpawnEffect(CommonEffectsProvider.CommonEffectType.dirtHit, pos, rotation, VisualEffectsController.EffectPriority.Low);
        }
    }


    void ApplyHitForceToObject(EnemyHealth health) {
        var collider = health.GetMainCollider();
        var rigidbody = collider.GetComponent<Rigidbody>();
        if (rigidbody == null) {
            rigidbody = collider.GetComponentInParent<Rigidbody>();
        }
        
        if(rigidbody == null)
            return;
        
        var closestPoint = collider.ClosestPoint(transform.position);
        

        //var force = collider.transform.position - transform.position;
        var force = transform.forward;
        //var force = GetComponent<Rigidbody>().velocity;
        force = (currentData.projectileDamage * hitForceMultiplier/2f)*force.normalized;
        
        rigidbody.AddForceAtPosition(force, closestPoint);
    }


    void DealDamageToEnemy(EnemyHealth target, bool phaseDamage = false) {
        if (target != null) {
            var dmg = currentData.projectileDamage;
            var burnDmg = currentData.burnDamage;

            /*if (phaseDamage) {
                dmg *= Time.deltaTime;
                burnDmg *= Time.deltaTime;
            }*/

            if (currentData.isHeal) {
                for (int i = 0; i < currentData.healChunkCount; i++) {
                    target.RepairChunk();
                }
            }else if (currentData.isSlowDamage) {
                target.gameObject.GetComponentInParent<EnemyWave>().AddSlow(currentData.slowDamage);
            } else  {
                if (dmg > 0) {
                    if (phaseDamage) {
                        target.DealDamage(dmg, null, null);
                    } else {
                        target.DealDamage(dmg, transform.position, Quaternion.AngleAxis(180, transform.up) * transform.rotation);
                    }
                    if (dmg > 1) {
                        var damageNumbers = VisualEffectsController.s.SmartInstantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent,
                            VisualEffectsController.EffectPriority.damageNumbers);
                        if (damageNumbers != null) {
                            damageNumbers.GetComponent<MiniGUI_DamageNumber>()
                                .SetUp(target.GetUITransform(), (int)dmg, true, false, false);
                        }
                    }
                }

                if (burnDmg > 0) {
                    target.BurnDamage(burnDmg, currentData.normalizedBurnDamage, currentData.extraBurnTier);
                    //if(burnDamage > 1)
                        /*VisualEffectsController.s.SmartInstantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent)
                            .GetComponent<MiniGUI_DamageNumber>()
                            .SetUp(target.GetUITransform(), (int)burnDmg, true, false, true);*/
                }
            }
        }
    }

    void DealDamageToPlayer(ModuleHealth target, bool canCrit, bool canMiss, Vector3 pos, Quaternion rot) {
        if (target != null) {
            var dmg = currentData.projectileDamage * TweakablesMaster.s.GetEnemyDamageMultiplier();
            var burnDmg = currentData.burnDamage * TweakablesMaster.s.GetEnemyDamageMultiplier();

            var missChance = PlayerSpeedToEnemyDamageModifiersController.s.GetMissChance();
            if (canMiss && Random.value < missChance) {
                // missed
                var missEffect = VisualEffectsController.s.SmartInstantiate(LevelReferences.s.missPrefab, target.GetUITransform(), pos, rot,
                    VisualEffectsController.EffectPriority.Medium);

                dmg = 0;
                // burn dmg does not crit, or miss
            }

            var critChance = PlayerSpeedToEnemyDamageModifiersController.s.GetCriticalChance();
            var crit = false;
            if (canCrit && Random.value < critChance) {
                dmg *= 2;
                // burn dmg does not crit, or miss
                crit = true;
            }

            if (dmg > 0) {
                var burnChunk = target.DealDamage(dmg, transform.position, Quaternion.AngleAxis(180, transform.up) * transform.rotation);
                DealWithArrowness(burnChunk);
                if (dmg > 1) {
                    var damageNumbers = VisualEffectsController.s.SmartInstantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent,
                        VisualEffectsController.EffectPriority.damageNumbers);
                    if (damageNumbers != null) {
                        damageNumbers.GetComponent<MiniGUI_DamageNumber>()
                            .SetUp(target.GetUITransform(), (int)dmg, false, false, false);
                    }
                }
                
                if (crit) {
                    var critEffect = VisualEffectsController.s.SmartInstantiate(LevelReferences.s.criticalDamagePrefab, target.GetUITransform(), pos, rot,
                        VisualEffectsController.EffectPriority.Medium);
                }
            }

            if (burnDmg > 0) {
                target.BurnDamage(burnDmg);
                /*if(burnDamage > 1)
                    VisualEffectsController.s.SmartInstantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent)
                        .GetComponent<MiniGUI_DamageNumber>()
                        .SetUp(target.GetUITransform(), (int)burnDamage, isPlayerBullet, armorProtected, true);*/
            }

            if (currentData.isSlowDamage) {
                SpeedController.s.AddSlow(currentData.slowDamage);
            }
        }
    }

    void DealWithArrowness(GameObject burnChunk) {
        if (currentData.leaveArrow && burnChunk != null) {
            var arrowMadeIt = Random.value <= currentData.leaveArrowChance;

            if (arrowMadeIt) {
                var targetTransform = burnChunk.transform;
                var repairable = burnChunk.GetComponent<RepairableBurnEffect>();
                var rotatedRotation = Quaternion.AngleAxis(180, targetTransform.up) * targetTransform.rotation;
                repairable.arrow =  Instantiate(instantDestroy, targetTransform.position, rotatedRotation, targetTransform);
                repairable.hasArrow = true;
                //Debug.Log(burnChunk.GetComponentInParent<Cart>().gameObject.name);
                //Debug.Break();
            } else {
                var arrowObj = VisualEffectsController.s.SmartInstantiate(instantDestroy, instantDestroy.transform.position, instantDestroy.transform.rotation, VisualEffectsController.EffectPriority.Medium);
                if (arrowObj != null) {
                    arrowObj.AddComponent<Rigidbody>();
                    arrowObj.AddComponent<RubbleFollowFloor>();
                    arrowObj.GetComponent<Rigidbody>().AddForce(SmitheryController.GetRandomYeetForce() / 10);
                }
            }
        }
    }
    
    void KnockAmmoOff(Transform ammo) {
        if (ammo != null) {
            if (ammo.name.Contains("ammo")) {
                var ammoBar = ammo.GetComponentInParent<PhysicalAmmoBar>();
                if ( ammoBar != null) {
                    ammoBar.RemoveChunk(ammo.gameObject);
                    ammo.transform.SetParent(VisualEffectsController.s.transform);
                    Instantiate(instantDestroy, instantDestroy.transform.position, instantDestroy.transform.rotation,ammo.transform);

                    ammo.gameObject.AddComponent<RubbleFollowFloor>();
                    
                    var ammoRg = ammo.gameObject.AddComponent<Rigidbody>();
                    ammoRg.mass = 20;
                    
                    isDead = true;
                    ammoRg.velocity = (transform.forward * currentData.speed ) + (currentData.initialVelocity);
                }
            }
        }
    }
}
