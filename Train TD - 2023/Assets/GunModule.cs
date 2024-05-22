using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

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

    public float maxFireRateReduction = 0.9f;
    public float gatlingAmount;
    public int maxGatlingAmount = 4;
    
    [Header("Bullet Info")]
    public bool useProviderBullet = false;
    [ShowIf("useProviderBullet")]
    public ProjectileProvider.ProjectileTypes myType;

    [HideIf("useProviderBullet")]
    public GameObject bulletPrefab;
    [HideIf("useProviderBullet")]
    public GameObject muzzleFlashPrefab;

    public float GetFireDelay() {
        if (isGigaGatling || currentAffectors.gatlinificator) { 
            var reduction = Mathf.Pow(gatlingAmount / maxGatlingAmount, 1 / 3f) * maxFireRateReduction;
            return (fireDelay * (1-reduction)) * GetAttackSpeedMultiplier();
        } else {
            return fireDelay * GetAttackSpeedMultiplier();
        }
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
    [InfoBox("@projectileDamage / fireDelay * maxFireRateReduction")]


    public Affectors currentAffectors = new Affectors();
    [Serializable]
    public class Affectors {
        public float damageMultiplier = 1f;
        public float sniperDamageMultiplier = 1f;
        
        public float fireRateMultiplier = 1f; // higher means GOOD
        public float fireRateDivider = 1f; // higher means BAD
        
        public float burnDamageMultiplier = 1f;
        public float regularToBurnDamageConversionMultiplier = 0;
        public float bonusBurnDamage = 0;

        public float regularToRangeConversionMultiplier = 0;
        
        public bool gatlinificator = false;
        public bool isHoming = false;
        public float explosionRangeBoost = 0;
    }
    
    
    [Header("Gun Type Vars")]
    public bool isLockOn = false;
    public float rotateSpeed = 10f;
    public bool isHeal = false;
    public float directControlShakeMultiplier = 1f;
    public bool mortarRotation = false;

    [Header("Ammo use vars")]
    private AmmoTracker _ammoTracker;
    public float ammoPerBarrage = 1;
    public float ammoPerBarrageMultiplier = 1;

    public float explosionRange = 0;
    public float inaccuracyMultiplier = 1f;

    [Header("Shake info")]
    public bool gunShakeOnShoot = true;
    private float gunShakeMagnitude = 0.04f;
    public float gunShakeMagnitudeMultiplier = 1f;



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
    private IEnumerator ActiveShootCycle;
    [FoldoutGroup("Internal Variables")]
    private bool isShooting = false;
    [FoldoutGroup("Internal Variables")]
    private float stopShootingTimer = 0f;
    [FoldoutGroup("Internal Variables")]
    public bool isActuallyShooting = false;
    [FoldoutGroup("Internal Variables")]
    public Transform target;
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
            return projectileDamage * GetDamageMultiplier();
        }
    }

    public bool SearchingForTargets() {
        return !beingDirectControlled && gunActive && HasAmmo();
    }

    public float GetBurnDamage() {
        var burnBulletAddonDamage = 0f;

        if (projectileDamage > 0) {
            burnBulletAddonDamage = projectileDamage * currentAffectors.regularToBurnDamageConversionMultiplier;
        } else {
            burnBulletAddonDamage = burnDamage * currentAffectors.regularToBurnDamageConversionMultiplier;
            /*burnBulletAddonDamage += burnDamage * currentAffectors.regularToRangeConversionMultiplier;*/
        }

        if (dontGetAffectByMultipliers) {
            return burnDamage + burnBulletAddonDamage;
        } else {
            return (burnDamage + currentAffectors.bonusBurnDamage + burnBulletAddonDamage) * GetBurnDamageMultiplier();
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

    private void Update() {
        if (gunActive) {
            if (target != null) {
                // Look at target
                if (rotateTransform.anchor == null) {
                    IsBarrelPointingCorrectly = true;
                } else {
                    LookAtLocation(target.position);
                }


                if (!beingDirectControlled) {
                    gatlingAmount += Time.deltaTime;
                    gatlingAmount = Mathf.Clamp(gatlingAmount, 0, maxGatlingAmount);
                }

            } else {
                // look at center of targeting area
                if (rotateTransform.anchor != null) {
                    if (SearchingForTargets()) {
                        SetRotation(Quaternion.LookRotation(GetRangeOrigin().forward, Vector3.up));
                    } else {
                        SetRotation(Quaternion.LookRotation(GetRangeOrigin().forward - Vector3.up/2f, Vector3.up));
                    }
                }

                IsBarrelPointingCorrectly = false;

                if (!beingDirectControlled) {
                    gatlingAmount -= Time.deltaTime*2;
                    gatlingAmount = Mathf.Clamp(gatlingAmount, 0, maxGatlingAmount);
                }
            }
        } else {
            if (!beingDirectControlled) {
                gatlingAmount -= Time.deltaTime * 2;
                gatlingAmount = Mathf.Clamp(gatlingAmount, 0, maxGatlingAmount);
            }
        }

        stopShootingTimer -= Time.deltaTime;
        if (stopShootingTimer <= 0) {
            StopShootingFindingHelperThingy();
        }
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
        var boost = 1f/currentAffectors.fireRateMultiplier;
        boost *= currentAffectors.fireRateDivider;
        
        boost /= TweakablesMaster.s.myTweakables.playerFirerateBoost;
        if (beingDirectControlled) {
            boost /= directControlFirerateMultiplier;
        }

        return boost;
    }

    float GetDamageMultiplier() {
        var dmgMul = currentAffectors.damageMultiplier ;
        
        dmgMul *= TweakablesMaster.s.myTweakables.playerDamageMultiplier;
        dmgMul *= currentAffectors.sniperDamageMultiplier;

        if (beingDirectControlled) {
            dmgMul *= directControlDamageMultiplier;
        }

        return dmgMul;
    }
    
    float GetBurnDamageMultiplier() {
        var dmgMul = currentAffectors.burnDamageMultiplier;
        
        dmgMul *= TweakablesMaster.s.myTweakables.playerDamageMultiplier;
        dmgMul *= currentAffectors.sniperDamageMultiplier;
            
        if (beingDirectControlled) {
            dmgMul *= directControlDamageMultiplier;
        }

        return dmgMul;
    }
    
    float AmmoUseWithMultipliers() {
        var ammoUse = ammoPerBarrage * ammoPerBarrageMultiplier;

        if (beingDirectControlled)
            ammoUse *= directControlAmmoUseMultiplier;

        ammoUse *= TweakablesMaster.s.myTweakables.playerAmmoUseMultiplier;

        return ammoUse;
    }


    IEnumerator ShootCycle() {
        while (true) {
            while (!IsBarrelPointingCorrectly || !HasAmmo() || waitTimer > 0) {
                waitTimer -= Time.deltaTime;
                //print(IsBarrelPointingCorrectly);
                yield return null;
            }
            
            if (isShooting) {
                StartCoroutine(_ShootBarrage());
            } else {
                break;
            }

            waitTimer = GetFireDelay();
        }
    }

    void StopShootingFindingHelperThingy() {
        stopShootingEvent?.Invoke();
        isActuallyShooting = false;
    }


    public float GetExplosionRange() {
        var damageToRangeConversion = 0f;

        if (currentAffectors.regularToRangeConversionMultiplier > 0) {
            damageToRangeConversion = 0.25f;
            damageToRangeConversion += projectileDamage * currentAffectors.regularToRangeConversionMultiplier;
        }

        return explosionRange + currentAffectors.explosionRangeBoost +damageToRangeConversion;
    }
    
    IEnumerator _ShootBarrage(bool isFree = false, GenericCallback shotCallback = null, GenericCallback onHitCallback = null, GenericCallback onMissCallback = null) {
        stopShootingTimer = GetFireDelay()+0.05f;
        if (!isActuallyShooting) {
            isActuallyShooting = true;
            startShootingEvent?.Invoke();
        }


        if (!isFree) {
            if (_ammoTracker != null) {
                for (int i = 0; i < _ammoTracker.ammoProviders.Count; i++) {
                    if (_ammoTracker.ammoProviders[i].AvailableAmmo() >= AmmoUseWithMultipliers()) {
                        _ammoTracker.ammoProviders[i].UseAmmo(AmmoUseWithMultipliers());
                        break;
                    }
                }
            }
        }

        for (int i = 0; i < fireBarrageCount; i++) {
            if (useProviderBullet) {
                bulletPrefab = ProjectileProvider.s.GetProjectile(myType);
                muzzleFlashPrefab = ProjectileProvider.s.GetMuzzleFlash(myType, isGigaGatling);
            }

            var barrelEnd = GetShootTransform().transform;
            var position = barrelEnd.position;
            var rotation = barrelEnd.rotation;

            var addAngleInaccuracy = 1f;
            addAngleInaccuracy += gatlingAmount / Mathf.Max(maxGatlingAmount,1) * 3;

            var randomDirection = Random.onUnitSphere;
            var actualInaccuracy = Random.Range(0, addAngleInaccuracy);
            actualInaccuracy *= inaccuracyMultiplier;
            
            var bullet = VisualEffectsController.s.SmartInstantiate(bulletPrefab, position + barrelEnd.forward * projectileSpawnOffset, rotation);

            var bulletForward = bullet.transform.forward;
            
            bullet.transform.forward = Vector3.RotateTowards(bulletForward, randomDirection,actualInaccuracy*Mathf.Deg2Rad, 0);
            //print($"{bulletForward} - {randomDirection} - {bullet.transform.forward}");
            
            
            bullet.transform.localScale = Vector3.one*(currentAffectors.damageMultiplier*1.5f);
            var muzzleFlash = VisualEffectsController.s.SmartInstantiate(muzzleFlashPrefab, position, rotation);
            var projectile = bullet.GetComponent<Projectile>();
            projectile.myOriginObject = this.GetComponentInParent<Rigidbody>().gameObject;
            projectile.projectileDamage = GetDamage();
            projectile.burnDamage = GetBurnDamage();
            projectile.target = target;
            projectile.isHeal = isHeal;
            projectile.explosionRange = GetExplosionRange();
            if (beingDirectControlled) {
                projectile.speed *= 3;
                projectile.acceleration *= 3;
                projectile.seekAcceleration *= 3;
            } else {
                projectile.isHoming = currentAffectors.isHoming;
            }

            projectile.SetIsPlayer(true);
            projectile.source = this;

            projectile.onHitCallback = onHitCallback;
            projectile.onMissCallback = onMissCallback;

            LogShotData(GetDamage());

            shotCallback?.Invoke();
            onBulletFiredEvent?.Invoke();
            if (gunShakeOnShoot)
                StartCoroutine(ShakeGun());

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
        rotateTransform.xAxis.Rotate(-2,0,0);

        yield return null;

        rotateTransform.anchor.localPosition = Vector3.zero;
    }

    [Button]
    public void ShootBarrageDebug() {
        StartCoroutine(_ShootBarrage(true));
    }

    [Button]
    public void ShootBarrageContinuousDebug() {
        if (Application.isPlaying) {
            gunShakeOnShoot = false;
            StartCoroutine(DebugShootingCycle());
        }
    }

    IEnumerator DebugShootingCycle() {
        while (true) {
            while (waitTimer > 0) {
                waitTimer -= Time.deltaTime;
                yield return null;
            }
            
            StartCoroutine(_ShootBarrage(true));

            waitTimer = GetFireDelay();
        }
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

    public void DeactivateGun() {
        gunActive = false;
        StopShooting();
    }

    public void ActivateGun() {
        gunActive = true;
        StartShooting();
    }

    void StopShooting() {
        if (isShooting) {
            if(ActiveShootCycle != null)
                StopCoroutine(ActiveShootCycle);
            ActiveShootCycle = null;
            isShooting = false;
            stopShootingEvent?.Invoke();
        }
    }

    void StartShooting() {
        if (gunActive && target != null) {
            if (!isShooting) {
                StopAllCoroutines();

                ActiveShootCycle = ShootCycle();
                isShooting = true;
                StartCoroutine(ActiveShootCycle);
            }
        }
    }


    public void UnsetTarget() {
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
        this.enabled = true;
    }

    public void Disable() {
        this.enabled = false;
    }
    
    public void CartDisabled() {
        this.enabled = false;
    }

    public void CartEnabled() {
        this.enabled = true;
    }
}
