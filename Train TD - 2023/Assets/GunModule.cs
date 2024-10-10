using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using static VisualEffectsController;

public class GunModule : MonoBehaviour, IComponentWithTarget, IActiveDuringCombat, IResetState, IDisabledState {

    [Tooltip("only used for serializing player carts")]
    public string gunUniqueName = "unset unique name";
    public Sprite gunSprite;
    [System.Serializable]
    public class TransformWithActivation {
        public Transform transform;
    }
    
    [System.Serializable]
    public class SingleAxisRotation {
        public Transform anchor;
        public Transform xAxis;
        public Transform yAxis;
        public Transform centerBarrelEnd;
    }

    [Header("Gatling info")]
    public bool isGigaGatling = false;
    public bool isLazer = false;
    public float lazerMinDamageMultiplier = 0.1f;
    public float gatlingDecayMultiplier = 1f;
    
    public float maxFireRateReduction = 0.9f;
    public float gatlingAmount;
    public int maxGatlingAmount = 4;

    public int GetMaxGatlingAmount() {
        return (isGigaGatling && currentAffectors.gatlinificator) ? (maxGatlingAmount * 2) : maxGatlingAmount;
    }

    public float GetGatlingIncrease() {
        return 1 * BiomeEffectsController.s.GetCurrentEffects().gatlinificationIncreaseRate;
    }

    public float GetCurrentGatlingPercent() {
        if (isGigaGatling || currentAffectors.gatlinificator) {
            return Mathf.Clamp01(gatlingAmount / GetMaxGatlingAmount());
        } else {
            return 0;
        }
    }
    
    [Header("Bullet Info")]
    public ProjectileProvider.ProjectileTypes myType;

    public float GetFireDelay() {
        if (!isLazer && (isGigaGatling || currentAffectors.gatlinificator)) {
            return GetFireDelayAtGatlingPercent(gatlingAmount / GetMaxGatlingAmount());
        } else {
            return GetFireDelayAtGatlingPercent(0);
        }
    }
    
    public float GetFireDelayAtGatlingPercent(float percent) {
        var curDelay = 0.08f;

        if (percent > 0) {
            var reduction = Mathf.Pow(percent, 1 / 3f) * maxFireRateReduction;
            reduction = (1 - reduction);
            reduction *= BiomeEffectsController.s.GetCurrentEffects().gatlinificationGunsMaxEffectMultiplier;
            curDelay = (fireDelay * reduction) * GetAttackSpeedMultiplier();
        } else {
            curDelay = fireDelay * GetAttackSpeedMultiplier();
        }

        return Mathf.Max(curDelay, 0.08f);
    }
    
    [Header("Damage and stuff")]
    public float fireDelay = 2f; // dont use this in code
    public int fireBarrageCount = 5;
    public float fireBarrageDelay = 0.1f;// dont use this in code
    public float GetFireBarrageDelay() { return fireBarrageDelay * GetAttackSpeedMultiplier();}
    [Tooltip("beware that if damage is less than 1 then damage numbers won't show up")]
    public float projectileDamage = 2f; // dont use this
    [Tooltip("beware that if burn damage is less than 1 then damage numbers won't show up")]
    public float burnDamage = 0; // dont use this in code
    public bool dontGetAffectByMultipliers = false;

    [InfoBox("@(projectileDamage * fireBarrageCount) / (fireDelay * (1-maxFireRateReduction))", InfoMessageType.Warning)] 
    [InfoBox("@(projectileDamage * fireBarrageCount) / (fireDelay)")]


    public Affectors currentAffectors = new Affectors();
    [Serializable]
    public class Affectors {
        public float power = 1;
        public float speed = 1;
        public float efficiency = 1;

        public float flatDamageAdd = 0;
        public float flatAmmoCostAdd = 0;
        public float ammoMultiplier = 1;

        public float uranium = 0;
        public float fire = 0;
        public int fireExtraBurnTier = 0;
        public float explosionRangeAdd = 0;

        public bool vampiric = false;
        public bool gatlinificator = false;
        public bool isHoming = false;
        
        
        public bool ancientDisabled = false;
        public bool lizardOverride = false;
        public bool IsActive() {
            return lizardOverride || !ancientDisabled;
        }
    }
    
    
    [Header("Gun Type Vars")]
    public bool isLockOn = false;
    public float rotateSpeed = 10f;
    public float directControlShakeMultiplier = 1f;
    public bool mortarRotation = false;
    public bool isHitScan = false;

    [Header("Ammo use vars")]
    private AmmoTracker _ammoTracker;
    public float ammoPerBarrage = 1;

    public float explosionRange = 0;
    public float baseInaccuracy = 1f;
    public float inaccuracyAddFireDelayMultiplier = 1f;
    
    [InfoBox("@baseInaccuracy + (inaccuracyAddFireDelayMultiplier * 0.25f * (1/(fireDelay * (1-maxFireRateReduction))))", InfoMessageType.Warning)] 
    [InfoBox("@baseInaccuracy + (inaccuracyAddFireDelayMultiplier * 0.25f * (1/fireDelay))")]

    [Header("Shake info")]
    public bool gunShakeOnShoot = true;
    private float gunShakeMagnitude = 0.04f;
    public float gunShakeMagnitudeMultiplier = 1f;
    public float gunRotateUpOnShoot = 2f;



    [Header("Model info")]
    public TransformWithActivation[] rotateTransforms;
    public SingleAxisRotation rotateTransform;
    public TransformWithActivation[] barrelEndTransforms;
    public float projectileSpawnOffset = 0.2f;
    public Transform rangeOrigin;

    // events
    [HideInInspector]
    public UnityEvent startShootingEvent = new UnityEvent();
    [HideInInspector]
    public UnityEvent stopShootingEvent = new UnityEvent();
    [HideInInspector]
    public UnityEvent onBulletFiredEvent = new UnityEvent();
    
    
    [FoldoutGroup("Internal Variables")]
    public float waitTimer;
    [FoldoutGroup("Internal Variables")]
    private bool isShooting = false;
    [FoldoutGroup("Internal Variables")]
    private float stopShootingTimer = 0f;
    [FoldoutGroup("Internal Variables")]
    public bool isActuallyShooting = false;
    [FoldoutGroup("Internal Variables")]
    public Transform target;
    [FoldoutGroup("Internal Variables")]
    public float targetLastTrackTime;
    [FoldoutGroup("Internal Variables")]
    public bool beingDirectControlled = false;
    [FoldoutGroup("Internal Variables")]
    public bool gunActive = true;
    [FoldoutGroup("Internal Variables")]
    public bool IsBarrelPointingCorrectly = false;
    [FoldoutGroup("Internal Variables")]
    private Quaternion realRotation = Quaternion.identity;
    [FoldoutGroup("Internal Variables")]
    public float directControlDamageMultiplier = 1f;
    [FoldoutGroup("Internal Variables")]
    public float directControlFirerateMultiplier = 1f;
    [FoldoutGroup("Internal Variables")]
    public float directControlAmmoUseMultiplier = 1f;

    public static bool debugMaxDamage = false;
    public float GetDamage() {
        if (debugMaxDamage) {
            return 1000;
        }
        if (dontGetAffectByMultipliers) {
            return projectileDamage;
        } else {
            if (projectileDamage > 0) {
                return (projectileDamage + currentAffectors.flatDamageAdd) * GetDamageMultiplier();
            } else {
                return 0;
            }
        }
    }

    public bool SearchingForTargets() {
        return !beingDirectControlled && gunActive && HasAmmo();
    }

    public float GetBurnDamage() {
        var burnBulletAddonDamage = 0f;

        if (projectileDamage > 0) {
            burnBulletAddonDamage = projectileDamage * currentAffectors.fire * 0.5f;
        } else {
            burnBulletAddonDamage = burnDamage * currentAffectors.fire;
            /*burnBulletAddonDamage += burnDamage * currentAffectors.regularToRangeConversionMultiplier;*/
        }

        if (dontGetAffectByMultipliers) {
            return burnDamage;
        } else {
            return (burnDamage + burnBulletAddonDamage) * GetBurnDamageMultiplier();
        }
    }



    public bool HasAmmo() {
        if (ammoPerBarrage <= 0)
            return true;

        if (_ammoTracker == null) {
            _ammoTracker = GetComponentInParent<AmmoTracker>();
        }

        if (_ammoTracker == null) {
            return true;
        }

        var ammoUse = AmmoUseWithMultipliers();
        for (int i = 0; i < _ammoTracker.ammoProviders.Count; i++) {
            if (_ammoTracker.ammoProviders[i].AvailableAmmo() >= ammoUse) {
                return true;
            }
        }

        return false;
    }
    
    public void ResetState() {
        currentAffectors = new Affectors();
    }

    [FoldoutGroup("Internal Variables")]
    public Vector3 myVelocity;
    [FoldoutGroup("Internal Variables")]
    public Vector3 lastPos;
    [FoldoutGroup("Internal Variables")]
    public Quaternion lastRot;
    [FoldoutGroup("Internal Variables")]
    public bool hadTargetLastFrame;
    private void LateUpdate() {
        if (gunActive) {
            Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRot);
            realRotation = deltaRotation * realRotation;
            lastRot = transform.rotation;

            if (target != null) {
                hadTargetLastFrame = true;
            }

            if (hadTargetLastFrame && target == null) {
                hadTargetLastFrame = false;
                targetLastTrackTime = 2f;
            }
            
            if (target != null) {
                // Look at target
                if (rotateTransform.anchor == null) {
                    IsBarrelPointingCorrectly = true;
                } else {
                    LookAtLocation(target.position);
                }


                if (!beingDirectControlled) {
                    gatlingAmount += Time.deltaTime * GetGatlingIncrease();
                    gatlingAmount = Mathf.Clamp(gatlingAmount, 0, GetMaxGatlingAmount());
                }

            } else {
                // look at center of targeting area
                if (rotateTransform.anchor != null) {
                    if (SearchingForTargets()) {
                        if (targetLastTrackTime > 0) {
                            targetLastTrackTime -= Time.deltaTime;
                        } else {
                            SetRotation(Quaternion.LookRotation(GetRangeOrigin().forward, Vector3.up));
                        }
                    } else {
                        SetRotation(Quaternion.LookRotation(GetRangeOrigin().forward - Vector3.up/2f, Vector3.up));
                    }
                }

                IsBarrelPointingCorrectly = false;

                if (!beingDirectControlled) {
                    gatlingAmount -= Time.deltaTime*GetGatlingDecayMultiplier();
                    gatlingAmount = Mathf.Clamp(gatlingAmount, 0, GetMaxGatlingAmount());
                }
            }
        } else {
            if (!beingDirectControlled) {
                gatlingAmount -= Time.deltaTime * GetGatlingDecayMultiplier();
                gatlingAmount = Mathf.Clamp(gatlingAmount, 0, GetMaxGatlingAmount());
            }
            
            if(!beingDirectControlled)
                SetRotation(Quaternion.LookRotation(GetRangeOrigin().forward - Vector3.up/2f, Vector3.up));
        }

        stopShootingTimer -= Time.deltaTime;
        if (stopShootingTimer <= 0) {
            StopShootingFindingHelperThingy();
        }


        if (Time.deltaTime > 0) {
            myVelocity = Vector3.Lerp(myVelocity,(transform.position - lastPos) /Time.deltaTime, 5*Time.deltaTime);
        }
        lastPos = transform.position;


        if (gunActive && isShooting) {
            if (!IsBarrelPointingCorrectly || !HasAmmo() || waitTimer > 0) {
                waitTimer -= Time.deltaTime;
                //print(IsBarrelPointingCorrectly);
            } else {
                StartCoroutine(_ShootBarrage());
                waitTimer = GetFireDelay();
            }
        }
    }

    public float GetGatlingDecayMultiplier() {
        return gatlingDecayMultiplier*3;
    }

    public void LookAtLocation(Vector3 location) {
        if (rotateTransform.anchor != null) {
            var lookAxis = location - rotateTransform.centerBarrelEnd.position;
            if (mortarRotation)
                lookAxis.y = 0;
            var lookRotation = Quaternion.LookRotation(lookAxis, Vector3.up);

            //Debug.DrawLine(rotateTransform.centerBarrelEnd.position, rotateTransform.centerBarrelEnd.position + lookAxis * 3);

            SetRotation(lookRotation);
        }
    }


    public void SetRotation(Quaternion rotation) {
        var searchingForTargets = SearchingForTargets();
        var _rotateSpeed = (searchingForTargets ? rotateSpeed : 2f);
        _rotateSpeed *= 40 * Time.deltaTime;
        if (beingDirectControlled) {
            _rotateSpeed *= 2;
        }
        realRotation = Quaternion.RotateTowards(realRotation, rotation, _rotateSpeed);

        if (Quaternion.Angle(realRotation, rotation) < 5) {
            IsBarrelPointingCorrectly = true;
        }else {
            IsBarrelPointingCorrectly = false;
        }

        rotateTransform.yAxis.rotation = Quaternion.Euler(0, realRotation.eulerAngles.y, 0);
        rotateTransform.xAxis.rotation = Quaternion.Euler(realRotation.eulerAngles.x, realRotation.eulerAngles.y, 0);
        if(!mortarRotation)
            rotateTransform.centerBarrelEnd.rotation = realRotation;
    }


    private void Start() {
        /*for (int i = 0; i < rotateTransforms.Length; i++) {
            var curRotate = rotateTransforms[i].transform;
            var anchor = new GameObject("Turret Rotate Anchor");
            anchor.transform.SetParent(curRotate.parent);
            anchor.transform.position = curRotate.position;
            curRotate.SetParent(anchor.transform);


        }*/

        if ((rotateTransform == null || rotateTransform.anchor == null) && rotateTransforms != null && rotateTransforms.Length > 0) {
            rotateTransform.anchor = rotateTransforms[0].transform;
            rotateTransform.xAxis = rotateTransforms[0].transform;
            rotateTransform.yAxis = rotateTransforms[0].transform;
            rotateTransform.centerBarrelEnd = barrelEndTransforms[0].transform;
        }

        if (rotateTransform.anchor != null) {
            var preAnchor = rotateTransform.anchor;
            var realAnchor = new GameObject("Turret RotateAnchor");
            realAnchor.transform.SetParent(preAnchor.parent);
            realAnchor.transform.position = preAnchor.position;
            realAnchor.transform.rotation = preAnchor.rotation;
            preAnchor.SetParent(realAnchor.transform);
            rangeOrigin = realAnchor.transform;
        }

        realRotation = rotateTransform.centerBarrelEnd.rotation;
    }

    float GetAttackSpeedMultiplier() {
        var boost = 1f / currentAffectors.speed;
        
        boost /= TweakablesMaster.s.GetPlayerFirerateBoost();
        
        if (beingDirectControlled) {
            boost /= directControlFirerateMultiplier;
        }

        return boost;
    }

    float GetDamageMultiplier() {
        var dmgMul = TweakablesMaster.s.GetPlayerDamageMultiplier();
        
        if (beingDirectControlled) {
            dmgMul *= directControlDamageMultiplier;
        }

        if (isLazer) {
            var reduction = Mathf.Pow(gatlingAmount / GetMaxGatlingAmount(), 3);
            reduction = Mathf.Clamp(reduction, lazerMinDamageMultiplier, 1f);
            dmgMul *= reduction;
            //print(reduction);
        }

        dmgMul *= currentAffectors.power;

        return dmgMul;
    }
    
    float GetBurnDamageMultiplier() {
        var dmgMul = TweakablesMaster.s.GetPlayerDamageMultiplier();
            
        if (beingDirectControlled) {
            dmgMul *= directControlDamageMultiplier;
        }
        
        dmgMul *= currentAffectors.power;

        return dmgMul;
    }
    
    float AmmoUseWithMultipliers() {
        var ammoUse = ammoPerBarrage + currentAffectors.flatAmmoCostAdd;

        if (beingDirectControlled)
            ammoUse *= directControlAmmoUseMultiplier;

        ammoUse *= currentAffectors.ammoMultiplier;

        return ammoUse;
    }
    

    void StopShootingFindingHelperThingy() {
        stopShootingEvent?.Invoke();
        isActuallyShooting = false;
    }


    public float GetExplosionRange() {
        return explosionRange + currentAffectors.explosionRangeAdd;
    }

    public float vampiricHealthStorage;
    void OnHit() {
        if (currentAffectors.vampiric) {
            vampiricHealthStorage += GetDamage() + GetBurnDamage();

            if (vampiricHealthStorage > ModuleHealth.repairChunkSize) {
                GetComponentInParent<ModuleHealth>().RepairChunk();
                vampiricHealthStorage -= ModuleHealth.repairChunkSize;
            }
        }
    }
    
    IEnumerator _ShootBarrage(bool isFree = false, GenericCallback shotCallback = null, GenericCallback onHitCallback = null, GenericCallback onMissCallback = null) {
        stopShootingTimer = GetFireDelay()+0.05f;
        if (!isActuallyShooting) {
            isActuallyShooting = true;
            startShootingEvent?.Invoke();
        }


        var neverUseAmmo = currentAffectors.efficiency >= 2;// at >2 efficiency chance to not use ammo is 100%
        if (!isFree && !neverUseAmmo) {
            if (_ammoTracker != null) {
                for (int i = 0; i < _ammoTracker.ammoProviders.Count; i++) {
                    if (_ammoTracker.ammoProviders[i].AvailableAmmo() >= AmmoUseWithMultipliers()) {
                        if (currentAffectors.efficiency > 1) {
                            var noUseChance = (currentAffectors.efficiency - 1f);
                            if (Random.value > noUseChance) {
                                _ammoTracker.ammoProviders[i].UseAmmo(AmmoUseWithMultipliers());
                            }
                        }else {
                            
                            var ammoUse = ((1f / currentAffectors.efficiency)); // at 1 efficiency ammoUse=1,  at 0.5 efficiency we get ammoUse = 2 meaning double ammo. in between we get a chance to use more 
                            while (ammoUse >= 1) {
                                ammoUse -= 1;
                                _ammoTracker.ammoProviders[i].UseAmmo(AmmoUseWithMultipliers());
                            }

                            if (ammoUse > 0) {
                                if (Random.value < ammoUse) {
                                    _ammoTracker.ammoProviders[i].UseAmmo(AmmoUseWithMultipliers());
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        for (int i = 0; i < fireBarrageCount; i++) {
            
            var barrelEnd = GetShootTransform().transform;
            var position = barrelEnd.position;
            var rotation = barrelEnd.rotation;

            var maxInaccuracy = baseInaccuracy;
            maxInaccuracy += (1 / GetFireDelay())*0.25f * inaccuracyAddFireDelayMultiplier; // max possible base add is 6.25

            var randomDirection = Random.onUnitSphere;
            var actualInaccuracy = Random.Range(0, maxInaccuracy);
            
            var bullet = ProjectileProvider.s.GetProjectile(myType, currentAffectors.fire, position + barrelEnd.forward*projectileSpawnOffset, rotation);

            var bulletForward = bullet.transform.forward;
            
            bullet.transform.forward = Vector3.RotateTowards(bulletForward, randomDirection,actualInaccuracy*Mathf.Deg2Rad, 0);
            //print($"{bulletForward} - {randomDirection} - {bullet.transform.forward}");

            bullet.transform.localScale = Vector3.one*(currentAffectors.power*1.5f);
            if (i == 0 || GetFireBarrageDelay() > 0) {
                var muzzleFlash = ProjectileProvider.s.GetMuzzleFlash(myType, position, rotation, transform, beingDirectControlled ? EffectPriority.Always : EffectPriority.High);
                if (GetCurrentGatlingPercent() >= 1f && BiomeEffectsController.s.currentEffects.gatlinificationGunsMaxEffectMultiplier > 1) {
                    CommonEffectsProvider.s.SpawnEffect(CommonEffectsProvider.CommonEffectType.gatlingTooHot, position, rotation, transform, beingDirectControlled ? EffectPriority.Always : EffectPriority.High);
                }

                if (GetCurrentGatlingPercent() > 0.1f &&  BiomeEffectsController.s.currentEffects.gatlinificationGunsMaxEffectMultiplier < 1) {
                    CommonEffectsProvider.s.SpawnEffect(CommonEffectsProvider.CommonEffectType.gatlingChilled, position, rotation, transform, beingDirectControlled ? EffectPriority.Always : EffectPriority.High);
                }
                
                if (currentAffectors.uranium > 0) {
                    CommonEffectsProvider.s.SpawnEffect(CommonEffectsProvider.CommonEffectType.radGunSelfDamageMuzzleFlash, position, rotation, transform, beingDirectControlled ? EffectPriority.Always : EffectPriority.High);
                }
            }

            var projectile = bullet.GetComponent<Projectile>();
            projectile.defaultData.CopyTo(projectile.currentData);
            var data = projectile.currentData;
            data.isTargetingPlayer = false;
            data.originObject = GetComponentInParent<Rigidbody>().gameObject;
            data.projectileDamage = GetDamage();
            data.burnDamage = GetBurnDamage();
            data.normalizedBurnDamage = GetBurnDamage() * fireBarrageCount / GetFireDelay();
            data.extraBurnTier = currentAffectors.fireExtraBurnTier;
            if (target != null) {
                data.targetEnemyHealth = target.GetComponent<EnemyHealth>();
                if (data.targetEnemyHealth != null) {
                    data.target = data.targetEnemyHealth.uiTransform;
                } else {
                    data.target = target;
                }
            }

            data.explosionRange = GetExplosionRange();
            if (beingDirectControlled && !data.isPhaseThrough) {
                data.speed *= 3;
                data.acceleration *= 3;
                data.seekAcceleration *= 3;
            } else {
                data.isHoming = currentAffectors.isHoming;
            }
            
            data.initialVelocity = myVelocity;

            data.onHitCallback += onHitCallback;
            data.onHitCallback += OnHit;
            data.onMissCallback = onMissCallback;
            
            projectile.SetUp(data);

            LogShotData(GetDamage());


            if (i == 0 || GetFireBarrageDelay() > 0) {
                shotCallback?.Invoke();
                onBulletFiredEvent?.Invoke();
                if (gunShakeOnShoot)
                    StartCoroutine(ShakeGun());
            }

            var waitTimer = 0f;

            while (waitTimer < GetFireBarrageDelay()) {
                waitTimer += Time.deltaTime;
                yield return null;
            }
        }
    }
    
    public IEnumerator ShakeGun() {
        yield return null;
        
        var range = Mathf.Clamp01(GetDamage() / 10f) + Mathf.Clamp01(GetDamage() / 10f);
        range /= 4f;
        range *= gunShakeMagnitudeMultiplier;

        var realMagnitude = gunShakeMagnitude;
        if (beingDirectControlled) {
            realMagnitude *= CameraShakeController.s.overallShakeAmount;
        }
        
        rotateTransform.anchor.localPosition = Random.insideUnitSphere * realMagnitude * range + (-rotateTransform.centerBarrelEnd.forward * realMagnitude * range * 2);
        rotateTransform.xAxis.Rotate(-gunRotateUpOnShoot,0,0);

        yield return null;

        rotateTransform.anchor.localPosition = Vector3.zero;
    }

    [Button]
    public void ShootBarrageDebug() {
        StartCoroutine(_ShootBarrage(true));
    }

    public void ShootBarrage(bool isFree, GenericCallback shotCallback, GenericCallback onHitCallback, GenericCallback onMissCallback) {
        StartCoroutine(_ShootBarrage(isFree, shotCallback, onHitCallback, onMissCallback));
    }


    void LogShotData(float damage) {
        /*var currentLevelStats = PlayerBuildingController.s.currentLevelStats;
        var buildingName = GetComponent<TrainBuilding>().uniqueName;
        if (currentLevelStats.TryGetValue(buildingName, out PlayerBuildingController.BuildingData data)) {
            data.damageData += damage;
        } else {
            var toAdd = new PlayerBuildingController.BuildingData();
            toAdd.uniqueName = buildingName;
            toAdd.damageData += damage;
            currentLevelStats.Add(buildingName, toAdd);
        }*/
    }

    private int lastIndex = -1;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    public TransformWithActivation GetShootTransform() {
        List<TransformWithActivation> activeTransforms = new List<TransformWithActivation>();

        for (int i = 0; i < barrelEndTransforms.Length; i++) {
            if (barrelEndTransforms[i].transform.gameObject.activeInHierarchy) {
                activeTransforms.Add(barrelEndTransforms[i]);
            }
        }

        if (activeTransforms.Count == 0) {
            activeTransforms.Add(barrelEndTransforms[0]);
        }

        lastIndex++;
        lastIndex = lastIndex % activeTransforms.Count;
        return activeTransforms[lastIndex];
    }
    

    public void SetTarget(Transform target) {
        this.target = target;
        StartShooting();
    }

    public bool gunCannotActivateOverride = false;
    public void DeactivateGun() {
        gunActive = false;
        StopShooting();
    }

    public void ActivateGun() {
        var myCart = GetComponentInParent<Cart>();
        if(gunCannotActivateOverride || !currentAffectors.IsActive() || beingDirectControlled || myCart == null || myCart.isDestroyed || myCart.isBeingDisabled)
            return;
        
        gunActive = true;
        StartShooting();
    }

    void StopShooting() {
        if (isShooting) {
            isShooting = false;
            stopShootingEvent?.Invoke();
        }
    }

    void StartShooting() {
        isShooting = true;
    }


    public void UnsetTarget() {
        if (target != null) {
            targetLastTrackTime = 2f;
        }
        this.target = null;
        StopShooting();
    }

    public Transform GetRangeOrigin() {
        if (rangeOrigin != null) {
            return rangeOrigin;
        } else if (rotateTransform.anchor != null) {
            return rotateTransform.anchor;
        } else{
            return transform;
        }
    }

    public Transform GetActiveTarget() {
        return target;
    }

    public void ActivateForCombat() {
        //this.enabled = true;
    }

    public void Disable() {
        //this.enabled = false;
    }
    
    public void CartDisabled() {
        //this.enabled = false;
        DeactivateGun();
    }

    public void CartEnabled() {
        //this.enabled = true;
        ActivateGun();
    }
}
