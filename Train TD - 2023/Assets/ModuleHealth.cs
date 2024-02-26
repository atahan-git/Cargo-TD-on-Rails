using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ModuleHealth : MonoBehaviour, IHealth, IActiveDuringCombat, IActiveDuringShopping {

    public static bool isImmune = false;

    public bool canHaveShields = true;
    public float maxShields = 0;
    public float currentShields = 0;
    private float shieldRegenRate = 50;
    private float shieldRegenDelay = 5;
    public float shieldRegenDelayMultiplier = 1f;
    public float shieldRegenDelayDivider = 1f;
    public float shieldRegenRateMultiplier = 1f;
    public float shieldRegenRateDivider = 1f;
    public float curShieldDelay = 0;


    public float baseShields = 0;
    public float baseHealth = 50;
    [ReadOnly]
    public float maxHealth = 50;
    public float currentHealth = 50;

    public float damageReductionMultiplier = 1f;

    public GameObject explodePrefab;
    public bool isDead = false;

    public bool damageNearCartsOnDeath = false;
    public bool selfDamage = false;
    [ShowIf("selfDamage")] 
    private float selfDamageTimer;
    public int[] selfDamageAmounts = new[] { 20, 10 };

    [NonSerialized]
    public Cart myCart;

    private GameObject activeHPCriticalIndicatorEffects;
    private enum HPLowStates {
        full, low, critical, destroyed
    }

    private HPLowStates hpState;

    public bool unDying = false;
    public bool invincible = false;

    [Header("These are the values artifacts change. They reset on their own")]
    
    public float repairEfficiency = 1;
    public float shieldUpEfficiency = 1;

    public float burnReductionMultiplier = 1;
    
    public bool glassCart = false;
    public bool reflectiveShields = false;

    public void ResetState() {
        repairEfficiency = 1;
        shieldUpEfficiency = 1;
        burnReductionMultiplier = 1;
        
        reflectiveShields = false;
        
        maxHealth = baseHealth * (0.6f + (DataSaver.s.GetCurrentSave().armorUpgradesBought* 0.2f));
        
        maxShields = baseShields;
        canHaveShields = true;
        isShieldActive = maxShields > 0;

        shieldRegenDelayMultiplier = 1;
        shieldRegenDelayDivider = 1;
        shieldRegenRateMultiplier = 1;
        shieldRegenRateDivider = 1;
        
        if (PlayStateMaster.s.isCombatInProgress()) {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        } else {
            currentHealth = maxHealth;
            currentShields = maxShields;
        }
        
        glassCart = false;

        UpdateHpState();
    }

    private float invincibilityTime = 0;
    [Button]
    public void DealDamage(float damage) {
        Assert.IsTrue(damage > 0);
        if(isImmune || invincible)
            return;

        /*if (luckyCart && Random.value < DataSaver.s.GetCurrentSave().currentRun.luck) {
            Instantiate(LevelReferences.s.luckyNegate, transform.position, Quaternion.identity);
            return;
        }*/

        if (invincibilityTime > 0) {
            return;
        }

        myCart = GetComponent<Cart>();
        if (!isDead && (myCart == null || !myCart.isDestroyed)) {
            damage *= damageReductionMultiplier;
            var shieldsWasMoreThan100 = currentShields > 100;
            if (isShieldActive && currentShields > 0) {
                curShieldDelay = shieldRegenDelay* (1f/shieldRegenDelayMultiplier) * shieldRegenDelayDivider;
                currentShields -= damage;
                damage = 0;
                if (currentShields <= 0) {
                    if (!shieldsWasMoreThan100) {
                        damage = -currentShields;
                    }

                    currentShields = 0;
                    isShieldActive = false;
                }

                if (currentShields > 0 && reflectiveShields) {
                    currentShields -= damage;
                    if (currentShields < 0)
                        currentShields = 0;
                }

                var shieldGen = GetComponentInChildren<ShieldGeneratorModule>();
                if (shieldGen != null) {
                    shieldGen.SpawnGemEffect(this);
                }
            }
            
            var prevHpPercent = currentHealth / maxHealth;

            /*if (myCart.isFragile) { // lose less hp the less hp you have if this is a fragile cart
                damage *= Mathf.Clamp(prevHpPercent*2,0.5f,1f);
            }*/
            
            
            currentHealth -= damage;

            /*if (myCart.isFragile) { // never be able to get one shot lose a fragile cart
                if (prevHpPercent > 0.1f) {
                    if (currentHealth <= 0) {
                        currentHealth = Mathf.Min(5, Mathf.CeilToInt(maxHealth*0.05f));
                    }

                    invincibilityTime = 0.5f;
                }
            }*/

            if(currentHealth <= 0) {
                //if (!myCart.isFragile) {
                    GetDestroyed();
                /*} else {
                    if(!unDying)
                        Die();
                    else 
                        currentHealth = 1;
                }*/
            }

            UpdateHpState();

            if (currentHealth < 0) {
                currentHealth = 0;
            }
            
        }
    }

    public void UpdateHpState() {
        Train.s.HealthModified();
        UpdateHPCriticalIndicators();
        SetBuildingShaderHealth();
    }
    
    void UpdateHPCriticalIndicators() {
        var hpPercent = currentHealth / maxHealth;
        if(isDead)
            return;
        myCart = GetComponent<Cart>();
        if (myCart == null) { // carts have module health but not trainbuilding
            return;
        }

        if (myCart.isDestroyed) {
            if (hpState != HPLowStates.destroyed) {
                if(activeHPCriticalIndicatorEffects != null)
                    activeHPCriticalIndicatorEffects.GetComponent<SmartDestroy>().Engage();
                activeHPCriticalIndicatorEffects = Instantiate(LevelReferences.s.buildingDestroyedParticles, transform);
                hpState = HPLowStates.destroyed;
            }
        } else {
            if (hpPercent < 0.25f) {
                if (hpState != HPLowStates.critical) {
                    if(activeHPCriticalIndicatorEffects != null)
                        activeHPCriticalIndicatorEffects.GetComponent<SmartDestroy>().Engage();
                    activeHPCriticalIndicatorEffects = Instantiate(LevelReferences.s.buildingHPCriticalParticles, transform);
                    hpState = HPLowStates.critical;
                }
            }else if (hpPercent < 0.5f) {
                if (hpState != HPLowStates.low) {
                    if(activeHPCriticalIndicatorEffects != null)
                        activeHPCriticalIndicatorEffects.GetComponent<SmartDestroy>().Engage();
                    activeHPCriticalIndicatorEffects = Instantiate(LevelReferences.s.buildingHPLowParticles, transform);
                    hpState = HPLowStates.low;
                }
            } else {
                if (hpState != HPLowStates.full) {
                    if(activeHPCriticalIndicatorEffects != null)
                        activeHPCriticalIndicatorEffects.GetComponent<SmartDestroy>().Engage();
                    activeHPCriticalIndicatorEffects = null;
                    hpState = HPLowStates.full;
                }
            }
        }
    }

    public void Repair(float heal) {
        Assert.IsTrue(heal >= 0);
        if(heal <= 0)
            return;
        
        heal *= repairEfficiency;

        if (currentHealth < maxHealth) {
            currentHealth += heal;

            Instantiate(LevelReferences.s.repairEffectPrefab, GetUITransform());

            if (myCart.isDestroyed && currentHealth > maxHealth / 2) {
                GetUnDestroyed();
            }

            if (currentHealth > maxHealth) {
                currentHealth = maxHealth;
            }


            UpdateHpState();
        }
    }
    
    
    public void ShieldUp(float shieldUp) {
        Assert.IsTrue(shieldUp > 0);
        shieldUp *= shieldUpEfficiency;

        if (currentShields < maxShields) {
            currentShields += shieldUp;

            Instantiate(LevelReferences.s.shieldUpEffectPrefab, GetUITransform());
            

            if (currentShields > maxShields) {
                currentShields = maxShields;
            }
        }

        curShieldDelay = shieldRegenDelay* (1f/shieldRegenDelayMultiplier) *shieldRegenDelayDivider;
    }

    public void SetHealth(float health) {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        
        if(currentHealth <= 0) {
            if (true) {
                GetDestroyed();
            } else {
                Die();
            }
        }
        
        myCart = GetComponent<Cart>();
        if (myCart.isDestroyed && currentHealth > maxHealth / 2) {
            GetUnDestroyed();
        }
        
        
        UpdateHpState();
    }

    [ShowIf("damageNearCartsOnDeath")]
    public int[] explosionDamages = new[] { 100, 50, 25 };
    void DamageNearCartsOnDeath() {
        return;
        /*var myModule = GetComponent<Cart>();

        var forwardWave = myModule;
        for (int i = 0; i < 3; i++) {
            forwardWave = Train.s.GetNextBuilding(true, forwardWave);
            if(forwardWave == null)
                break;
            
            GameObject prefab;
            switch (i) {
                case 0:
                    prefab = LevelReferences.s.bigDamagePrefab;
                    break;
                case 1:
                    prefab = LevelReferences.s.mediumDamagePrefab;
                    break;
                case 2:
                    prefab = LevelReferences.s.smallDamagePrefab;
                    break;
                default:
                    prefab = LevelReferences.s.smallDamagePrefab;
                    break;
            }
            DealDamageToBuilding(forwardWave, prefab, explosionDamages[i]);
        }
        
        var backwardsWave = myModule;
        for (int i = 0; i < 3; i++) {
            backwardsWave = Train.s.GetNextBuilding(false, myModule);
            if(backwardsWave == null)
                break;
            
            GameObject prefab;
            switch (i) {
                case 0:
                    prefab = LevelReferences.s.bigDamagePrefab;
                    break;
                case 1:
                    prefab = LevelReferences.s.mediumDamagePrefab;
                    break;
                case 2:
                    prefab = LevelReferences.s.smallDamagePrefab;
                    break;
                default:
                    prefab = LevelReferences.s.smallDamagePrefab;
                    break;
            }
            DealDamageToBuilding(backwardsWave, prefab, explosionDamages[i]);
        }


        var range = 1.8f;
        var allEnemies = EnemyWavesController.s.GetComponentsInChildren<EnemyHealth>();

        for (int i = 0; i < allEnemies.Length; i++) {
            var enemy = allEnemies[i];
            var distance = Vector3.Distance(enemy.transform.position, transform.position);
            if (distance < range) {
                distance = Mathf.Clamp(distance, range/3, range);
                var damage = distance.Remap(range/3, range, 100, 25);

                GameObject prefab = LevelReferences.s.smallDamagePrefab;
                if (damage > 80) {
                    prefab = LevelReferences.s.bigDamagePrefab;
                }else if (damage > 50) {
                    prefab = LevelReferences.s.mediumDamagePrefab;
                }

                var point = enemy.GetMainCollider().ClosestPoint(transform.position);

                Instantiate(prefab, point, Quaternion.identity);
                enemy.DealDamage(damage);

            }
        }*/
    }


    private void DealDamageToBuilding(Cart building, GameObject prefab, float damage) {
        if (building == null)
            return;

        var hp = building.GetComponent<ModuleHealth>();
        if (hp != null) {
            hp.DealDamage(damage);
            Instantiate(prefab, hp.transform.position, Quaternion.identity);
        }
    }

    public bool burnResistant = false;
    float burnReduction = 0.6f;
    public float currentBurn = 0;
    public float burnSpeed = 0;
    private float lastBurn;
    
    
    public void BurnDamage(float damage) {
        if (burnResistant)
            damage /= 2;
        
        burnSpeed += damage;
    }

    public bool isShieldActive;
    private void Update() {
        var burnDistance = Mathf.Max(burnSpeed / 2f, 1f);
        if (currentBurn >= burnDistance) {
            Instantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent)
                .GetComponent<MiniGUI_DamageNumber>()
                .SetUp(GetGameObject().transform, burnDistance, true, false, true);
            DealDamage(burnDistance);

            currentBurn -= burnDistance;
        }

        if (burnSpeed > 0.05f) {
            currentBurn += burnSpeed * Time.deltaTime;
        }
        
        burnSpeed = Mathf.Lerp(burnSpeed,0,burnReductionMultiplier*burnReduction*Time.deltaTime);


        if (PlayStateMaster.s.isCombatInProgress()) {
            if (selfDamage) {
                selfDamageTimer -= Time.deltaTime;
                if (selfDamageTimer <= 0) {
                    selfDamageTimer = 10;

                    SelfDamage();
                }
            }
        }
        
        if (Mathf.Abs(lastBurn - burnSpeed) > 1 || (lastBurn > 0 && burnSpeed <= 0)) {
            SetBuildingShaderBurn(burnSpeed);
            lastBurn = burnSpeed;
        }


        if (maxShields > 0) {
            if (curShieldDelay <= 0) {
                if (!isShieldActive) {
                    isShieldActive = currentShields >= (maxShields / 2f);
                }

                currentShields += shieldRegenRate * shieldRegenRateMultiplier * Time.deltaTime * (1/shieldRegenRateDivider);
            } else {
                curShieldDelay -= Time.deltaTime;
            }
        }

        currentShields = Mathf.Clamp(currentShields, 0, maxShields);

        if (invincibilityTime > 0) {
            invincibilityTime -= Time.deltaTime;
        }
    }

    void SelfDamage() {
        var myModule = GetComponent<Cart>();

        var multiplier = 1;
        DealDamage(selfDamageAmounts[0] * multiplier);
        var prefab = LevelReferences.s.smallDamagePrefab;
        Instantiate(prefab, transform.position, Quaternion.identity);
        
        DealDamageToBuilding(Train.s.GetNextBuilding(1, myModule), prefab, selfDamageAmounts[1] * multiplier);
        DealDamageToBuilding(Train.s.GetNextBuilding(-1, myModule), prefab, selfDamageAmounts[1] * multiplier);
    }
    

    [NonSerialized]
    public UnityEvent dieEvent = new UnityEvent();

    private static readonly int Health = Shader.PropertyToID("_Health");
    private static readonly int Burn = Shader.PropertyToID("_Burn");
    private static readonly int Alive = Shader.PropertyToID("_Alive");

    [Button]
    public void Die() {
        /*if (damageNearCartsOnDeath) {
            DamageNearCartsOnDeath();
        }*/

        isDead = true;
        Instantiate(explodePrefab, transform.position, transform.rotation);
        SoundscapeController.s.PlayModuleExplode();

        /*// in case of death give some of the cost back
        var trainBuilding = GetComponent<Cart>();
        if(trainBuilding)
            LevelReferences.s.SpawnResourceAtLocation(ResourceTypes.scraps, trainBuilding.cost * 0.25f, transform.position);*/
        
        
        dieEvent?.Invoke();
        
        var emptyCart = Instantiate(LevelReferences.s.scrapCart).GetComponent<Cart>();
        
        Train.s.CartDestroyed(myCart);
        Train.s.AddCartAtIndex(myCart.trainIndex, emptyCart);
        
        Destroy(gameObject);
    }

    [Button]
    public void GetDestroyed() {
        myCart.isDestroyed = true;
        myCart.SetDisabledState();

        SetBuildingShaderAlive(false);

        Instantiate(explodePrefab, transform.position, transform.rotation);
        SoundscapeController.s.PlayModuleExplode();
        
        if (damageNearCartsOnDeath) {
            DamageNearCartsOnDeath();
        }

        burnSpeed = 0;
    }

    [Button]
    public void GetUnDestroyed() {
        myCart.isDestroyed = false;
        myCart.SetDisabledState();

        SetBuildingShaderAlive(true);
    }
    
    void SetBuildingShaderHealth() {
        var hpPercent = currentHealth / maxHealth;
        var _renderers = myCart._meshes;
        for (int j = 0; j < _renderers.Length; j++) {
            var rend = _renderers[j];
            if (rend != null && rend.sharedMaterials.Length > 1) {
                rend.sharedMaterials[1].SetFloat(Health, hpPercent);
            }
        }
    }
    
    void SetBuildingShaderBurn(float value) {
        var _renderers = myCart._meshes;
        value = value.Remap(0, 10, 0, 0.5f);
        value = Mathf.Clamp(value, 0, 2f);
        for (int j = 0; j < _renderers.Length; j++) {
            var rend = _renderers[j];
            if (rend != null && rend.sharedMaterials.Length > 1) {
                rend.sharedMaterials[1].SetFloat(Burn, value);
            }
        }
    }

    void SetBuildingShaderAlive(bool isAlive) {
        var _renderers = myCart._meshes;
        var value = isAlive ? 1f : 0.611f;
        for (int j = 0; j < _renderers.Length; j++) {
            var rend = _renderers[j];
            if (rend != null && rend.sharedMaterials.Length > 1) {
                rend.sharedMaterials[1].SetFloat(Alive, value);
            }
        }
    }

    public bool IsPlayer() {
        return true;
    }
    
    public bool IsAlive() {
        return !isDead;
    }

    public GameObject GetGameObject() {
        return gameObject;
    }
    
    public Collider GetMainCollider() {
        return GetComponentInChildren<BoxCollider>();
    }

    public bool HasArmor() {
        return false;
    }
    public float GetShieldPercent() {
        return currentShields / Mathf.Max(maxShields,1);
    }
    
    public bool IsShieldActive() {
        return isShieldActive;
    }
    public float GetHealthPercent() {
        return currentHealth /  Mathf.Max(maxHealth,1);
    }
    public string GetHealthRatioString() {
        return $"{currentHealth}/{maxHealth}";
    }

    public Transform GetUITransform() {
        myCart = GetComponent<Cart>();
        if (myCart != null) {
            return myCart.GetUITargetTransform();
        } else {
            return transform;
        }
    }

    public void ActivateForCombat() {
        this.enabled = true;
    }

    public void ActivateForShopping() {
        this.enabled = true;
    }

    public void Disable() {
        this.enabled = false;
    }
}
