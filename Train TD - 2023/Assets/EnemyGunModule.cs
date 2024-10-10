using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class EnemyGunModule : MonoBehaviour, IComponentWithTarget,IEnemyEquipment {

    [Tooltip("only used for serializing player carts")]
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

    [ShowIf("isGigaGatling")]
    public float maxFireRateReduction = 0.9f;
    [ShowIf("isGigaGatling")]
    public float gatlingAmount;
    [ShowIf("isGigaGatling")]
    public int maxGatlingAmount = 4;
    
    [Header("Bullet Info")]
    public ProjectileProvider.EnemyProjectileTypes myType;

    public float GetGatlingIncrease() {
        return 1 * BiomeEffectsController.s.GetCurrentEffects().gatlinificationIncreaseRate;
    }
    public float GetFireDelay() {
        if (isGigaGatling ) { 
            var reduction = Mathf.Pow(gatlingAmount / maxGatlingAmount, 1 / 3f) * maxFireRateReduction;
            reduction = (1 - reduction);
            reduction *= BiomeEffectsController.s.GetCurrentEffects().gatlinificationGunsMaxEffectMultiplier;
            return (fireDelay * reduction) * GetAttackSpeedMultiplier();
        } else {
            return fireDelay * GetAttackSpeedMultiplier();
        }
    }
    
    [Header("Damage and stuff")]
    public float fireDelay = 2f; // dont use this in code
    public int fireBarrageCount = 1;
    [ShowIf("@fireBarrageCount>1")]
    public float fireBarrageDelay = 0.1f;// dont use this in code
    public float GetFireBarrageDelay() { return fireBarrageDelay * GetAttackSpeedMultiplier();}

    [Header("Gun Type Vars")]
    public float rotateSpeed = 10f;
    public bool mortarRotation = false;
    
    public float inaccuracyMultiplier = 1f;

    [Header("Shake info")]
    public bool gunShakeOnShoot = true;
    public float gunShakeMagnitude = 0.04f;
    
    
    [Header("Shoot Credits")]
    public bool isUniqueGearNoNeedForShootCredit = false;
    public float shootCreditsUse = 1f;

    public bool NeedShootCredit() {
        return !(isUniqueGearNoNeedForShootCredit || isEliteNoNeedForShootCredit);
    }

    [Header("Model info")]
    public TransformWithActivation[] rotateTransforms;
    public SingleAxisRotation rotateTransform;
    public TransformWithActivation[] barrelEndTransforms;
    public float projectileSpawnOffset = 0.2f;
    public Transform rangeOrigin;

    [FoldoutGroup("Internal Variables")]
    public float waitTimer;
    [FoldoutGroup("Internal Variables")]
    public bool isShooting = false;
    [FoldoutGroup("Internal Variables")]
    public Transform target;
    [FoldoutGroup("Internal Variables")]
    public bool gunActive = true;
    [FoldoutGroup("Internal Variables")]
    public bool IsBarrelPointingCorrectly = false;
    [FoldoutGroup("Internal Variables")]
    private Quaternion realRotation = Quaternion.identity;
    [FoldoutGroup("Internal Variables")]
    public bool gotShootCredits = false;
    [FoldoutGroup("Internal Variables")]
    public bool isEliteNoNeedForShootCredit = false;

    public bool SearchingForTargets() {
        return gunActive;
    }

    [FoldoutGroup("Internal Variables")]
    public Vector3 myVelocity;
    [FoldoutGroup("Internal Variables")]
    public Vector3 lastPos;
    private void Update() {
        if (gunActive) {
            if (target != null) {
                // Look at target
                if (rotateTransform.anchor == null) {
                    IsBarrelPointingCorrectly = true;
                } else {
                    LookAtLocation(target.position);
                }

                gatlingAmount += Time.deltaTime * GetGatlingIncrease();
                gatlingAmount = Mathf.Clamp(gatlingAmount, 0, maxGatlingAmount);
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

                gatlingAmount -= Time.deltaTime*2;
                gatlingAmount = Mathf.Clamp(gatlingAmount, 0, maxGatlingAmount);
            }
        } else {
            gatlingAmount -= Time.deltaTime * 2;
            gatlingAmount = Mathf.Clamp(gatlingAmount, 0, maxGatlingAmount);
        }
        
        myVelocity = (transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;


        if (gunActive && isShooting) {
            if (!IsBarrelPointingCorrectly || waitTimer > 0) {
                waitTimer -= Time.deltaTime;
                //print(IsBarrelPointingCorrectly);
                
            } else {
                StartCoroutine(_ShootBarrage());
                waitTimer = GetFireDelay();
            }
        }
    }

    public void LookAtLocation(Vector3 location) {
        if (rotateTransform.anchor != null) {
            var lookAxis = location - rotateTransform.centerBarrelEnd.position;
            if (mortarRotation)
                lookAxis.y = 0;
            var lookRotation = Quaternion.LookRotation(lookAxis, Vector3.up);

            SetRotation(lookRotation);
        }
    }


    public void SetRotation(Quaternion rotation) {
        var searchingForTargets = SearchingForTargets();
        var _rotateSpeed = (searchingForTargets ? rotateSpeed : 2f);
        _rotateSpeed *= 40 * Time.deltaTime;
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
        var enemy = GetComponentInParent<EnemyInSwarm>();
        if (enemy != null) {
            bool isElite = enemy.isElite;
            isEliteNoNeedForShootCredit = isElite;
            if (!isElite) {
                gotShootCredits = false;
            }
        } 

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
        return 1f/TweakablesMaster.s.GetEnemyFirerateBoost();
    }
    

    IEnumerator _ShootBarrage() {
        if (target == null) {
            yield break;
        }
       
        if (NeedShootCredit()) {
            EnemyTargetAssigner.s.TryToGetShootCredits(this);
            if (!gotShootCredits) {
                yield return new WaitForSeconds(Random.Range(GetFireDelay()*0.2f, GetFireDelay()*0.7f));

                if (!gotShootCredits) {
                    yield break;
                } else {
                    waitTimer = GetFireDelay();
                }
            } else {
                gotShootCredits = false;
            }
        }

        for (int i = 0; i < fireBarrageCount; i++) {
            if (target == null) {
                yield break;
            }
            
            var barrelEnd = GetShootTransform().transform;
            var position = barrelEnd.position;
            var rotation = barrelEnd.rotation;

            var addAngleInaccuracy = 1f;
            addAngleInaccuracy += gatlingAmount / Mathf.Max(maxGatlingAmount,1) * 3;

            var randomDirection = Random.onUnitSphere;
            var actualInaccuracy = Random.Range(0, addAngleInaccuracy);
            actualInaccuracy *= inaccuracyMultiplier;
            
            var bullet = ProjectileProvider.s.GetEnemyProjectile(myType, position + barrelEnd.forward * projectileSpawnOffset, rotation);

            var bulletForward = bullet.transform.forward;
            
            bullet.transform.forward = Vector3.RotateTowards(bulletForward, randomDirection,actualInaccuracy*Mathf.Deg2Rad, 0);
            
            var muzzleFlash = ProjectileProvider.s.GetEnemyMuzzleFlash(myType, position, rotation, transform);
            var rg = GetComponentInParent<Rigidbody>();
            
            var projectile = bullet.GetComponent<Projectile>();
            if (!projectile) {
                yield break;
            }
            
            projectile.defaultData.CopyTo(projectile.currentData);
            var data = projectile.currentData;
            data.isTargetingPlayer = myType != ProjectileProvider.EnemyProjectileTypes.heal;
            GameObject originObject = gameObject;
            if (rg) {
                originObject = rg.gameObject;
            }
            data.originObject = originObject;
            if (target == null) {
                Debug.Break();
            }

            if (data.isTargetingPlayer) {
                data.targetPlayerHealth = target.GetComponentInParent<ModuleHealth>();
                if (data.targetPlayerHealth == null) {
                    print($"{target.gameObject.name}");
                }

                data.target = data.targetPlayerHealth.GetUITransform();
            } else {
                data.targetEnemyHealth = target.GetComponentInParent<EnemyHealth>();
                
                if (data.targetEnemyHealth == null) {
                    print($"{target.gameObject.name}");
                }

                data.target = data.targetEnemyHealth.GetUITransform();
            }

            data.initialVelocity = myVelocity;
            
            projectile.SetUp(data);
            
            
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

        var realMagnitude = gunShakeMagnitude;

        rotateTransform.anchor.localPosition =Random.insideUnitSphere * realMagnitude + (-rotateTransform.centerBarrelEnd.forward * realMagnitude  * 2);
        rotateTransform.xAxis.Rotate(-2,0,0);

        yield return null;

        rotateTransform.anchor.localPosition = Vector3.zero;
    }

    [Button]
    public void ShootBarrageDebug() {
        gotShootCredits = true;
        StartCoroutine(_ShootBarrage());
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
            
            StartCoroutine(_ShootBarrage());

            waitTimer = GetFireDelay();
        }
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

    [Button]
    public void StopShooting() {
        isShooting = false;
    }

    [Button]
    public void StartShooting() {
        isShooting = true;
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

    public Sprite GetSprite() {
        return gunSprite;
    }

    public string GetName() {
        return GetComponent<ClickableEntityInfo>().info;
    }

    public string GetDescription() {
        return GetComponent<ClickableEntityInfo>().tooltip.text;
    }
}