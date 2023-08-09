using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class GunModule : MonoBehaviour, IComponentWithTarget, IActiveDuringCombat, IResetState {

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


    public bool isGigaGatling = false;
    public bool gatlinificator = false;

    public float maxFireRateReduction = 0.9f;
    public float gatlingAmount;
    public int maxGatlingAmount = 4;
    
    public TransformWithActivation[] rotateTransforms;
    public SingleAxisRotation rotateTransform;
    public TransformWithActivation[] barrelEndTransforms;
    public float projectileSpawnOffset = 0.2f;


    public bool useProviderBullet = false;
    [ShowIf("useProviderBullet")]
    public ProjectileProvider.ProjectileTypes myType;

    [HideIf("useProviderBullet")]
    public GameObject bulletPrefab;
    [HideIf("useProviderBullet")]
    public GameObject muzzleFlashPrefab;


    public Transform target;


    public bool mortarRotation = false;
    public float fireDelay = 2f; // dont use this

    public float GetFireDelay() {
        if (isGigaGatling || gatlinificator) { 
            //print((fireDelay-(Mathf.Pow(((float)Mathf.FloorToInt(gatlingAmount))/(float)maxGatlingAmount, 1/2f)*maxFireRateReduction)) * GetAttackSpeedMultiplier());
            /*if (isPlayer) {
                return (fireDelay - (((float)Mathf.FloorToInt(gatlingAmount)) / (float)maxGatlingAmount) * maxFireRateReduction) * GetAttackSpeedMultiplier();
            } else {*/
            var reduction = Mathf.Pow(gatlingAmount / maxGatlingAmount, 1 / 3f) * maxFireRateReduction;
                return (fireDelay * (1-reduction)) * GetAttackSpeedMultiplier();
            //}
        } else {
            return fireDelay * GetAttackSpeedMultiplier();
        }
    }

    public int fireBarrageCount = 5;
    public float fireBarrageDelay = 0.1f;// dont use this
    public float fireRateMultiplier = 1f;
    public float GetFireBarrageDelay() { return fireBarrageDelay * GetAttackSpeedMultiplier();}
    public float projectileDamage = 2f; // dont use this
    public float burnDamage = 0; // dont use this
    public float bonusBurnDamage = 0;
    public float damageMultiplier = 1f;
    public float sniperDamageMultiplier = 1f;
    public float burnDamageMultiplier = 1f;
    public float regularToBurnDamageConversionMultiplier = 0;
    public float regularToRangeConversionMultiplier = 0;
    public bool dontGetAffectByMultipliers = false;

    public float directControlShakeMultiplier = 1f;

    public bool isHeal = false;

    public float GetDamage() {
        if (dontGetAffectByMultipliers) {
            return projectileDamage;
        } else {
            return projectileDamage * GetDamageMultiplier();
        }
    }
    
    public float GetBurnDamage() {
        var burnBulletAddonDamage = 0f;
        
        if (isFire) {
            if (projectileDamage > 0) {
                burnBulletAddonDamage = projectileDamage * regularToBurnDamageConversionMultiplier;
            } else {
                burnBulletAddonDamage = burnDamage * regularToBurnDamageConversionMultiplier;
            }
        }
        
        if (dontGetAffectByMultipliers) {
            return burnDamage + burnBulletAddonDamage;
        } else {
            return (burnDamage + bonusBurnDamage + burnBulletAddonDamage) * GetBurnDamageMultiplier();
        }
    }


    public float rotateSpeed = 10f;

    public bool gunActive = true;
    public bool IsBarrelPointingCorrectly = false;
    public bool hasAmmo = true;

    public bool isPlayer = false;

    public Transform rangeOrigin;

    public UnityEvent barrageShot;

    public bool canPenetrateArmor = false;

    public float steamUsePerShot = 0;

    public bool needWarmUp = false;
    private bool isWarmedUp = false;

    [HideInInspector]
    public UnityEvent startWarmUpEvent = new UnityEvent();
    [HideInInspector]
    public UnityEvent onBulletFiredEvent = new UnityEvent();
    [HideInInspector]
    public UnityEvent stopShootingEvent = new UnityEvent();
    [HideInInspector]
    public UnityEvent gatlingCountZeroEvent = new UnityEvent();

    public bool gunShakeOnShoot = true;
    private float gunShakeMagnitude = 0.04f;
    public float gunShakeMagnitudeMultiplier = 1f;

    public bool beingDirectControlled = false;

    public bool isHoming = false;

    public float boostDamageOnUpgrade = 0.5f;
    public void ResetState(int level) {
        damageMultiplier = 1 + (boostDamageOnUpgrade*level);
        sniperDamageMultiplier = 1;
        fireRateMultiplier = 1;
        burnDamageMultiplier = 1;
        bonusBurnDamage = 0;
        regularToBurnDamageConversionMultiplier = 0;
        gatlinificator = false;
        isHoming = false;
        explosionRangeBoost = 0;
    }

    private bool triggeredGatlingZero = true;
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
                    SetRotation(Quaternion.LookRotation(GetRangeOrigin().forward, Vector3.up));
                }

                IsBarrelPointingCorrectly = false;

                if (!beingDirectControlled) {
                    gatlingAmount -= Time.deltaTime*2;
                    gatlingAmount = Mathf.Clamp(gatlingAmount, 0, maxGatlingAmount);
                }
            }
        }

        if (isWarmedUp) {
            stopShootingTimer -= Time.deltaTime;
            if (stopShootingTimer <= 0) {
                StopShootingFindingHelperThingy();
            }
        }

        if (gatlingAmount <= 0) {
            if (!triggeredGatlingZero) {
                gatlingCountZeroEvent?.Invoke();
                triggeredGatlingZero = true;
            }
        } else {
            triggeredGatlingZero = false;
        }

    }

    private Quaternion realRotation = Quaternion.identity;
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
        realRotation = Quaternion.Lerp(realRotation, rotation, rotateSpeed * Time.deltaTime);

        if (Quaternion.Angle(realRotation, rotation) < 5) {
            IsBarrelPointingCorrectly = true;
        }else {
            IsBarrelPointingCorrectly = false;
        }

        rotateTransform.yAxis.rotation = Quaternion.Euler(0, realRotation.eulerAngles.y, 0);
        rotateTransform.xAxis.rotation = Quaternion.Euler(realRotation.eulerAngles.x, realRotation.eulerAngles.y, 0);
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
    }

    float GetAttackSpeedMultiplier() {
        var boost = 1f/fireRateMultiplier;

        if (isPlayer) {
            boost /= TweakablesMaster.s.myTweakables.playerFirerateBoost;
        } else {
            boost /= TweakablesMaster.s.myTweakables.enemyFirerateBoost;
        }

        return boost;
    }

    float GetDamageMultiplier() {
        var dmgMul = damageMultiplier ;
        
        if (isPlayer) {
            dmgMul *= TweakablesMaster.s.myTweakables.playerDamageMultiplier;
            dmgMul *= sniperDamageMultiplier;
        } else {
            dmgMul *= TweakablesMaster.s.myTweakables.enemyDamageMutliplier + WorldDifficultyController.s.currentDamageIncrease;
        }

        return dmgMul;
    }
    
    float GetBurnDamageMultiplier() {
        var dmgMul = burnDamageMultiplier;
        
        if (isPlayer) {
            dmgMul *= TweakablesMaster.s.myTweakables.playerDamageMultiplier;
        } else {
            dmgMul *= TweakablesMaster.s.myTweakables.enemyDamageMutliplier + WorldDifficultyController.s.currentDamageIncrease;
        }
        

        return dmgMul;
    }


    private IEnumerator ActiveShootCycle;
    private bool isShooting = false;
    public float waitTimer;
    IEnumerator ShootCycle() {
        while (true) {
            while (!IsBarrelPointingCorrectly || !hasAmmo || waitTimer > 0) {
                waitTimer -= Time.deltaTime;
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

    private float stopShootingTimer = 0f;
    void StopShootingFindingHelperThingy() {
        stopShootingEvent?.Invoke();
        isWarmedUp = false;
    }

    public bool isFire;
    public bool isSticky;
    public bool isExplosive;
    public float explosionRange = 0;
    public float explosionRangeBoost = 0;

    float GetExplosionRange() {
        var damageToRangeConversion = 0f;
        if (isExplosive) {
            damageToRangeConversion = 0.25f;
            damageToRangeConversion += projectileDamage * regularToRangeConversionMultiplier;
        }
        
        return explosionRange + explosionRangeBoost +damageToRangeConversion;
    }
    
    IEnumerator _ShootBarrage(bool isFree = false, GenericCallback shotCallback = null, GenericCallback onHitCallback = null, GenericCallback onMissCallback = null) {
        stopShootingTimer = GetFireDelay()+0.05f;
        if (!isWarmedUp) {
            isWarmedUp = true;
            startWarmUpEvent?.Invoke();
            
            if (needWarmUp) {
                yield break;
            }
        }

        if (!isPlayer) {
            if (needShootCredits) {
                if (!gotShootCredits) {
                    yield break;
                } else {
                    EnemyTargetAssigner.s.shootRequesters.Enqueue(this);
                    gotShootCredits = false;
                }
            }
        }


        if(!isFree)
            barrageShot?.Invoke();

        for (int i = 0; i < fireBarrageCount; i++) {
            //if (!isPlayer || AreThereEnoughMaterialsToShoot() || isFree) {
            if (useProviderBullet) {
                bulletPrefab = ProjectileProvider.s.GetProjectile(myType, isFire || bonusBurnDamage > 0, isSticky);
                muzzleFlashPrefab = ProjectileProvider.s.GetMuzzleFlash(myType, isGigaGatling, isFire || bonusBurnDamage > 0, isSticky);
            }


            var barrelEnd = GetShootTransform().transform;
            var position = barrelEnd.position;
            var rotation = barrelEnd.rotation;
            var bullet = Instantiate(bulletPrefab, position + barrelEnd.forward * projectileSpawnOffset, rotation);
            var muzzleFlash = Instantiate(muzzleFlashPrefab, position, rotation);
            var projectile = bullet.GetComponent<Projectile>();
            projectile.myOriginObject = this.transform.root.gameObject;
            projectile.projectileDamage = GetDamage();
            projectile.burnDamage = GetBurnDamage();
            projectile.target = target;
            projectile.isHeal = isHeal;
            projectile.explosionRange = GetExplosionRange();
            //projectile.isTargetSeeking = true;
            projectile.canPenetrateArmor = canPenetrateArmor;
            if (beingDirectControlled) {
                projectile.speed *= 3;
                projectile.acceleration *= 3;
            } else {
                projectile.isHoming = isHoming;
            }

            projectile.SetIsPlayer(isPlayer);
            projectile.source = this;

            projectile.onHitCallback = onHitCallback;
            projectile.onMissCallback = onMissCallback;

            //if(myCart != null)
            if (isPlayer)
                LogShotData(GetDamage());
            if (isPlayer && !isFree) {
                SpeedController.s.UseSteam(steamUsePerShot * TweakablesMaster.s.myTweakables.gunSteamUseMultiplier);
            }

            shotCallback?.Invoke();
            onBulletFiredEvent?.Invoke();
            if (gunShakeOnShoot)
                StartCoroutine(ShakeGun());

            /*if (isGigaGatling) {
                gatlingAmount += 1;
                gatlingAmount = Mathf.Clamp(gatlingAmount, 0, maxGatlingAmount);
            }*/

            //}

            var waitTimer = 0f;

            while (waitTimer < GetFireBarrageDelay()) {
                //print(GetFireBarrageDelay());
                waitTimer += Time.deltaTime;
                yield return null;
            }
            //yield return new WaitForSeconds(GetFireBarrageDelay());
        }
    }

    
    public bool gotShootCredits = false;
    public bool needShootCredits = false;
    public float shootCreditsUse = 1f;
    private void OnEnable() {
        if (!isPlayer) {
            bool isElite = GetComponentInParent<EnemyHealth>().rewardArtifactOnDeath;
            if (!isElite) {
                needShootCredits = true;
                gotShootCredits = true;
            }
        }
    }

    private void OnDisable() {
        if (!isPlayer) {
            bool isElite = GetComponentInParent<EnemyHealth>().rewardArtifactOnDeath;
            if (!isElite) {
                needShootCredits = false;
            }
        }
    }

    private bool stopUpdateRotation = false;

    public IEnumerator ShakeGun() {
        yield return null;
        
        var range = Mathf.Clamp01(GetDamage() / 10f) + Mathf.Clamp01(GetDamage() / 10f);
        range /= 4f;
        range *= gunShakeMagnitudeMultiplier;

        var defaultPositions = new List<Vector3>();

        var realMagnitude = gunShakeMagnitude;
        if (beingDirectControlled) {
            realMagnitude *= CameraShakeController.s.overallShakeAmount;
        }
        
        
        rotateTransform.anchor.localPosition =Random.insideUnitSphere * realMagnitude * range + (-rotateTransform.centerBarrelEnd.forward * realMagnitude * range * 2);
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
        StartCoroutine(ShootCycle());
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
        if (gunActive) {
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
}
