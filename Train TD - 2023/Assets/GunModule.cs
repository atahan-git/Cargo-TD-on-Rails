using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class GunModule : MonoBehaviour, IComponentWithTarget, IActiveDuringCombat, IResetState {

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
    public float fireRateMultiplier = 1f; // higher means GOOD
    public float fireRateDivider = 1f; // higher means BAD
    public float GetFireBarrageDelay() { return fireBarrageDelay * GetAttackSpeedMultiplier();}
    [Tooltip("beware that if damage is less than 1 then damage numbers won't show up")]
    public float projectileDamage = 2f; // dont use this
    [Tooltip("beware that if burn damage is less than 1 then damage numbers won't show up")]
    public float burnDamage = 0; // dont use this
    public float bonusBurnDamage = 0;
    public float damageMultiplier = 1f;
    public float sniperDamageMultiplier = 1f;
    public float directControlDamageMultiplier = 1f;
    public float directControlFirerateMultiplier = 1f;
    public float directControlAmmoUseMultiplier = 1f;
    public float burnDamageMultiplier = 1f;
    public float regularToBurnDamageConversionMultiplier = 0;
    //public float regularToIceDamageConversionMultiplier = 0;
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

    public bool SearchingForTargets() {
        return !beingDirectControlled && gunActive && HasAmmo();
    }

    public float GetBurnDamage() {
        var burnBulletAddonDamage = 0f;

        if (projectileDamage > 0) {
            burnBulletAddonDamage = projectileDamage * regularToBurnDamageConversionMultiplier;
        } else {
            burnBulletAddonDamage = burnDamage * regularToBurnDamageConversionMultiplier;
            burnBulletAddonDamage += burnDamage * regularToRangeConversionMultiplier;
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

    private AmmoTracker _ammoTracker;

    public float ammoPerBarrage = 1;
    public float ammoPerBarrageMultiplier = 1;

    public bool isPlayer = false;

    public Transform rangeOrigin;

    public bool canPenetrateArmor = false;

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

    public bool isLockOn = false;
    
    public bool isHoming = false;
    public void ResetState() {
        damageMultiplier = 1;
        sniperDamageMultiplier = 1;
        fireRateMultiplier = 1;
        fireRateDivider = 1;
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
        var boost = 1f/fireRateMultiplier;
        boost *= fireRateDivider;

        if (isPlayer) {
            boost /= TweakablesMaster.s.myTweakables.playerFirerateBoost;
            if (beingDirectControlled) {
                boost /= directControlFirerateMultiplier;
            }
        } else {
            boost /= TweakablesMaster.s.myTweakables.enemyFirerateBoost;
        }

        return boost;
    }

    float GetDamageMultiplier() {
        var dmgMul = damageMultiplier ;
        
        if (isPlayer) {
            dmgMul *= TweakablesMaster.s.myTweakables.playerDamageMultiplier;
            dmgMul *= 0.6f + (DataSaver.s.GetCurrentSave().cityUpgradesProgress.damageUpgradesBought * 0.2f);
            dmgMul *= sniperDamageMultiplier;

            if (beingDirectControlled) {
                dmgMul *= directControlDamageMultiplier;
            }
            
        } else {
            dmgMul *= TweakablesMaster.s.myTweakables.enemyDamageMultiplier * WorldDifficultyController.s.currentDamageMultiplier;
        }

        return dmgMul;
    }
    
    float GetBurnDamageMultiplier() {
        var dmgMul = burnDamageMultiplier;
        
        if (isPlayer) {
            dmgMul *= TweakablesMaster.s.myTweakables.playerDamageMultiplier;
            dmgMul *= 0.6f + (DataSaver.s.GetCurrentSave().cityUpgradesProgress.damageUpgradesBought * 0.2f);
            dmgMul *= sniperDamageMultiplier;
            
            if (beingDirectControlled) {
                dmgMul *= directControlDamageMultiplier;
            }
            
        } else {
            dmgMul *= TweakablesMaster.s.myTweakables.enemyDamageMultiplier * WorldDifficultyController.s.currentDamageMultiplier;
        }
        

        return dmgMul;
    }
    
    float AmmoUseWithMultipliers() {
        var ammoUse = ammoPerBarrage * ammoPerBarrageMultiplier;

        if (beingDirectControlled)
            ammoUse *= directControlAmmoUseMultiplier;

        if (isPlayer) {
            ammoUse *= TweakablesMaster.s.myTweakables.playerAmmoUseMultiplier;
        }

        return ammoUse;
    }


    private IEnumerator ActiveShootCycle;
    private bool isShooting = false;
    public float waitTimer;
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

    private float stopShootingTimer = 0f;
    void StopShootingFindingHelperThingy() {
        stopShootingEvent?.Invoke();
        isWarmedUp = false;
    }

    
    public float explosionRange = 0;
    public float explosionRangeBoost = 0;

    public float inaccuracyMultiplier = 1f;

    public float GetExplosionRange() {
        var damageToRangeConversion = 0f;

        if (regularToRangeConversionMultiplier > 0) {
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
            //if (!isPlayer || AreThereEnoughMaterialsToShoot() || isFree) {
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
            
            
            bullet.transform.localScale = Vector3.one*(damageMultiplier*1.5f);
            SetColors(bullet);
            var muzzleFlash = VisualEffectsController.s.SmartInstantiate(muzzleFlashPrefab, position, rotation);
            var projectile = bullet.GetComponent<Projectile>();
            projectile.myOriginObject = this.GetComponentInParent<Rigidbody>().gameObject;
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
                projectile.seekAcceleration *= 3;
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
            /*if (isPlayer && !isFree) {
                SpeedController.s.UseSteam(steamUsePerShot * TweakablesMaster.s.myTweakables.gunSteamUseMultiplier);
            }*/

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
            var enemyHp = GetComponentInParent<EnemyHealth>();
            if (enemyHp != null) {
                bool isElite = GetComponentInParent<CarrierEnemy>() != null;
                if (!isElite) {
                    needShootCredits = true;
                    EnemyTargetAssigner.s.shootRequesters.Enqueue(this);
                    //gotShootCredits = true;
                }
            } 
        }
    }

    private void OnDisable() {
        if (!isPlayer) {
            var enemyHp = GetComponentInParent<EnemyHealth>();
            if (enemyHp != null) {
                bool isElite = GetComponentInParent<CarrierEnemy>() != null;
                if (!isElite) {
                    needShootCredits = false;
                }
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

    void SetColors(GameObject bullet) {
        if (regularToBurnDamageConversionMultiplier <= 0) {
            return;
        }

        var mainColor = HeatToColor(regularToBurnDamageConversionMultiplier);
        var emissiveColor = HeatToColor(regularToBurnDamageConversionMultiplier, true);

        foreach (var meshRenderer in bullet.GetComponentsInChildren<MeshRenderer>()) {
            meshRenderer.material.color = mainColor;
            meshRenderer.material.SetColor(EmissionColor, emissiveColor);
        }

        foreach (var particleSystem in bullet.GetComponentsInChildren<ParticleSystem>()) {
            var main = particleSystem.main;
            main.startColor = mainColor;
        }
    }

    Color HeatToColor(float temperature, bool isEmissive = false) {

        var initialTemperature = temperature;
        
        // conversion to temperature
        // 0.5 -> 1000
        // 1 -> 2000
        // 2 -> 10000
        // brought to you by desmos graphing approximation
        temperature /= 2f;
        temperature = Mathf.Pow(4.84f, (temperature-1.043f))*1000;
        
        // algorithm
        // https://tannerhelland.com/2012/09/18/convert-temperature-rgb-algorithm-code.html
        temperature = temperature / 100f;


        var red = 0f;
        var green = 0f;
        var blue = 0f;
        
        // red
        if (temperature <= 66) {
            red = 255f;
        } else {
            red = temperature - 60;
            red = 329.698727446f * Mathf.Pow(red,-0.1332047592f);
        }
        
        // green
        if (temperature <= 66) {
            green = temperature;
            green = 99.4708025861f * Mathf.Log(green) - 161.1195681661f;
        } else {
            green = temperature - 60;
            green = 288.1221695283f * Mathf.Pow(green, -0.0755148492f);
        }
        
        // blue
        if (temperature >= 66) {
            blue = 255f;
        } else {
            if (temperature <= 19) {
                blue = 0f;
            } else {
                blue = temperature - 10;
                blue = 138.5177312231f * Mathf.Log(blue) - 305.0447927307f;
            }
        }

        // convert to unity color
        red = red/255f;
        red = Mathf.Clamp01(red);
        green = green/255f;
        green = Mathf.Clamp01(green);
        blue = blue/255f;
        blue = Mathf.Clamp01(blue);

        if (isEmissive) {
            var _intensity = (red +green + blue) / 3f;
            var intensity = 0.25f;
            float factor = intensity / _intensity;
            red *= factor;
            green *= factor;
            blue *= factor;
        }

        var color = new Color(red, green, blue);
        Color.RGBToHSV(color, out float H, out float S, out float V);
        if(initialTemperature > 4)
            S = S * initialTemperature;

        return Color.HSVToRGB(H,S,V);
    }
}
