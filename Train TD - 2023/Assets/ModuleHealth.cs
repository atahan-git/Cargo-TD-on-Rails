using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ModuleHealth : MonoBehaviour, IHealth, IActiveDuringCombat, IActiveDuringShopping, IDisabledState {

    public static bool isImmune = false;
    
    public float baseHealth = 50;
    [ReadOnly]
    public float maxHealth = 50;
    public float maxHealthReduction = 0;
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
    
    public bool invincible = false;

    private float repairEffectSpawnDistance = 2;

    public Collider mainCollider;

    public MiniGUI_CartUIBar cartUIBar;
    private void Start() {
        repairEffectSpawnDistance = GetMainCollider().bounds.size.magnitude + 1;
        cartUIBar = Instantiate(LevelReferences.s.cartHealthPrefab, LevelReferences.s.uiDisplayParent).GetComponent<MiniGUI_CartUIBar>();
        cartUIBar.SetUp(this);
        SetHealthBarState(false);
    }

    public void SetHealthBarState(bool isVisible) {
        cartUIBar.SetVisible(isVisible);
    }
    
    private void OnDestroy() {
        if(cartUIBar != null)
            if(cartUIBar.gameObject != null)
                Destroy(cartUIBar.gameObject);
    }

    [NonSerialized] public bool glassCart;
    public void ResetState() {
        maxHealth = baseHealth;

        if (PlayStateMaster.s.isCombatInProgress()) {
            currentHealth = Mathf.Clamp(currentHealth, 0, GetMaxHealth());
        } else {
            currentHealth = GetMaxHealth();
        }

        glassCart = false;

        UpdateHpState();
    }

    public ShieldGeneratorModule myProtector;

    private float invincibilityTime = 0;
    [Button]
    public void DealDamage(float damage, Vector3? hitPoint) {
        Assert.IsTrue(damage > 0);
        if(isImmune || invincible)
            return;
        
        if (invincibilityTime > 0) {
            return;
        }


        myCart = GetComponent<Cart>();
        if (myProtector != null) {
            myProtector.ProtectFromDamage(damage);
            return;
        }
        
        damage *= damageReductionMultiplier;
        
        currentHealth -= damage;

        if(currentHealth <= 0) {
            currentHealth = 0;
            GetDestroyed();
        }

        UpdateHpState(hitPoint);
    }

    public void UpdateHpState(Vector3? hitPoint = null) {
        Train.s.HealthModified();
        UpdateHPCriticalIndicators();
        SetBuildingShaderHealth();
        SetBuildingRepairableBurnChunks(hitPoint);
        SetBuildingMaxHealthReductionChunks();
    }
    
    void UpdateHPCriticalIndicators() {
        var hpPercent = currentHealth / GetMaxHealth(false);
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

    void Repair(float heal, bool showEffect = true) {
        Assert.IsTrue(heal >= 0);
        if(heal <= 0)
            return;

        if (currentHealth < GetMaxHealth()) {
            currentHealth += heal;

            if (heal > 5 && showEffect) {
                Instantiate(LevelReferences.s.repairEffectPrefab, GetUITransform());
            }

            if (myCart.isDestroyed && currentHealth > GetMaxHealth(false) / 2) {
                GetUnDestroyed();
            }

            if (currentHealth > GetMaxHealth()) {
                currentHealth = GetMaxHealth();
            }

            UpdateHpState();
        }
    }

    public float GetMaxHealth(bool withReduction = true) {
        if (withReduction) {
            return maxHealth - maxHealthReduction;
        } else {
            return maxHealth;
        }
    }

    public void SetHealth(float health, float _maxHealthReduction) {
        if (_maxHealthReduction > 0) {
            maxHealthReduction = _maxHealthReduction;
        } else {
            maxHealthReduction = 0;
        }

        currentHealth = Mathf.Clamp(health, 0, GetMaxHealth());
        
        if(currentHealth <= 0) {
            if (true) {
                GetDestroyed();
            } else {
                Die();
            }
        }
        
        myCart = GetComponent<Cart>();
        if (myCart.isDestroyed && currentHealth > GetMaxHealth(false) / 2) {
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
            hp.DealDamage(damage, null);
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

    private void Update() {
        var burnDistance = Mathf.Max(burnSpeed / 2f, 1f);
        if (currentBurn >= burnDistance) {
            Instantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent)
                .GetComponent<MiniGUI_DamageNumber>()
                .SetUp(GetGameObject().transform, burnDistance, true, false, true);
            DealDamage(burnDistance, null);

            currentBurn -= burnDistance;
        }

        if (burnSpeed > 0.05f) {
            currentBurn += burnSpeed * Time.deltaTime;
        }
        
        burnSpeed = Mathf.Lerp(burnSpeed,0,burnReduction*Time.deltaTime);


        if (PlayStateMaster.s.isCombatInProgress()) {
            if (selfDamage) {
                selfDamageTimer -= Time.deltaTime;
                if (selfDamageTimer <= 0) {
                    selfDamageTimer = 10;

                    SelfDamage(selfDamageAmounts[0], true, selfDamageAmounts[1]);
                }
            }
        }
        
        if (Mathf.Abs(lastBurn - burnSpeed) > 1 || (lastBurn > 0 && burnSpeed <= 0)) {
            SetBuildingShaderBurn(burnSpeed);
            lastBurn = burnSpeed;
        }

        if (invincibilityTime > 0) {
            invincibilityTime -= Time.deltaTime;
        }
    }

    public void SelfDamage(float amount, bool damageNear = false, float damageNearAmount = 0f) {
        var myModule = GetComponent<Cart>();


        DealDamage(amount, null);
        var prefab = LevelReferences.s.smallDamagePrefab;
        Instantiate(prefab, transform.position, Quaternion.identity);

        if (damageNear) {
            DealDamageToBuilding(Train.s.GetNextBuilding(1, myModule), prefab,  damageNearAmount);
            DealDamageToBuilding(Train.s.GetNextBuilding(-1, myModule), prefab, damageNearAmount);
        }
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
        if (myCart.isDestroyed) {
            return;
        }
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
        var hpPercent = currentHealth / GetMaxHealth(false);
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
    
    public List<RepairableBurnEffect> activeBurnEffects = new List<RepairableBurnEffect>();
    public List<GameObject> activeMaxHealthReductionPlates = new List<GameObject>();

    void SetBuildingRepairableBurnChunks() {
        SetBuildingRepairableBurnChunks(null);
    }
    void SetBuildingRepairableBurnChunks(Vector3? sourcePoint) {
        var missingHealth = GetMaxHealth() - currentHealth;
        var targetBurnEffectCount = Mathf.CeilToInt(missingHealth / repairChunkSize);
        targetBurnEffectCount = Mathf.Clamp(targetBurnEffectCount, 0, Mathf.CeilToInt(GetMaxHealth(false) / repairChunkSize + 1));

        if (activeBurnEffects.Count > targetBurnEffectCount) {
			
            while (activeBurnEffects.Count > targetBurnEffectCount) {
                var decommissioned = activeBurnEffects[0];
                var index = 0;
                if (decommissioned.isTaken) {
                    if (activeBurnEffects.Count >= 2) {
                        decommissioned = activeBurnEffects[1];
                        index = 1;
                    }
                }
                activeBurnEffects.RemoveAt(index);
                SetBuildingMaxHealthReductionChunks(decommissioned.transform.position, decommissioned.transform.rotation);
                decommissioned.GetComponent<RepairableBurnEffect>().Repair();
            }
			
        }else if (activeBurnEffects.Count < targetBurnEffectCount) {

            var n = 10;
            while (activeBurnEffects.Count < targetBurnEffectCount && n > 0) {
                Vector3 raySourceDirection;
                if (sourcePoint != null) {
                    raySourceDirection = (Vector3)sourcePoint - GetUITransform().position;
                    raySourceDirection += Random.insideUnitSphere * (10-n) * 0.1f;
                } else {
                    raySourceDirection = Random.insideUnitSphere;
                }

                if (raySourceDirection.y < 0) {
                    raySourceDirection.y = 0;
                }
                raySourceDirection.Normalize();
                
                var rayOrigin = GetUITransform().position + raySourceDirection * repairEffectSpawnDistance;
                
                var rayDirection =  transform.position - rayOrigin;
                var ray = new Ray(rayOrigin, rayDirection);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 5, LevelReferences.s.buildingLayer)) {
                    if (hit.collider.GetComponentInParent<RepairableBurnEffect>()
                        || activeMaxHealthReductionPlates.Contains(hit.collider.gameObject)) { // dont spawn over another effect
                        continue;
                    }
                    var hitBuilding = hit.collider.GetComponentInParent<ModuleHealth>();
                    if (hitBuilding == this) {
                        var burnEffect = Instantiate(LevelReferences.s.cartRepairableDamageEffect, hit.point, Quaternion.LookRotation(hit.normal));
                        burnEffect.transform.SetParent(myCart.genericParticlesParent);
                        activeBurnEffects.Add(burnEffect.GetComponent<RepairableBurnEffect>());
                    }
                }
                n -= 1;
            }
        }

        if (activeBurnEffects.Count < targetBurnEffectCount) {
            Invoke(nameof(SetBuildingRepairableBurnChunks),0.01f);
        }
    }
    
    
    void SetBuildingMaxHealthReductionChunks() {
        return;
        var targetMissingHealthPlatesCount = Mathf.CeilToInt(maxHealthReduction / maxHealthReductionChunkSize);

        if (activeMaxHealthReductionPlates.Count > targetMissingHealthPlatesCount) {
            while (activeMaxHealthReductionPlates.Count > targetMissingHealthPlatesCount) {
                var decommissioned = activeMaxHealthReductionPlates[0];
                activeMaxHealthReductionPlates.RemoveAt(0);
                Destroy(decommissioned.gameObject);
            }
			
        }else if (activeMaxHealthReductionPlates.Count < targetMissingHealthPlatesCount) {

            var n = 10;
            while (activeMaxHealthReductionPlates.Count < targetMissingHealthPlatesCount && n > 0) {
                var raySourceDirection = Random.insideUnitSphere;

                if (raySourceDirection.y < 0) {
                    raySourceDirection.y = 0;
                }
                raySourceDirection.Normalize();
                
                var rayOrigin = GetUITransform().position + raySourceDirection * repairEffectSpawnDistance;
                
                var rayDirection =  transform.position - rayOrigin;
                var ray = new Ray(rayOrigin, rayDirection);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 5, LevelReferences.s.buildingLayer)) {
                    if (hit.collider.GetComponentInParent<RepairableBurnEffect>()
                        || activeMaxHealthReductionPlates.Contains(hit.collider.gameObject)) { // dont spawn over another effect
                        continue;
                    }
                    var hitBuilding = hit.collider.GetComponentInParent<ModuleHealth>();
                    if (hitBuilding == this) {
                        SetBuildingMaxHealthReductionChunks(hit.point, Quaternion.LookRotation(hit.normal));
                    }
                }
                n -= 1;
            }
        }

        if (activeMaxHealthReductionPlates.Count < targetMissingHealthPlatesCount) {
            Invoke(nameof(SetBuildingMaxHealthReductionChunks),0.01f);
        }
    }
    void SetBuildingMaxHealthReductionChunks(Vector3 targetPoint, Quaternion targetRotation) {
        var plate = Instantiate(LevelReferences.s.maxHealthReductionPlatePrefab, targetPoint, targetRotation);
        plate.transform.SetParent(myCart.genericParticlesParent);
        activeMaxHealthReductionPlates.Add(plate);
    }

    public const int repairChunkSize = 50;
    public const int maxHealthReductionChunkSize = 5;
    public void RepairChunk(RepairableBurnEffect toRepair) {
        if (invincible)
            return;
        
        //maxHealthReduction += maxHealthReductionChunkSize;
        
        if (toRepair.canRepair) {
            var effect = Instantiate(LevelReferences.s.repairDoneEffect, toRepair.transform.position, toRepair.transform.rotation);
            effect.transform.SetParent(myCart.genericParticlesParent);
            toRepair.Repair();
            activeBurnEffects.Remove(toRepair);
            
            SetBuildingMaxHealthReductionChunks(toRepair.transform.position, toRepair.transform.rotation);
        }
        
        Repair(repairChunkSize, false);
    }

    [Button]
    public void RepairChunk() {
        if (invincible)
            return;

        if (activeBurnEffects.Count == 0) {
            return;
        }

        //maxHealthReduction += maxHealthReductionChunkSize;
        Repair(repairChunkSize, false);
    }

    public void RepairChunk(int count) {
        for (int i = 0; i < activeBurnEffects.Count; i++) {
            if (!activeBurnEffects[i].isTaken) {
                RepairChunk(activeBurnEffects[i]);
                count -= 1;
            }

            if (count <= 0) {
                return;
            }
        }
        
        for (int i = 0; i < count; i++) {
            RepairChunk();
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
        if (mainCollider != null) {
            return mainCollider;
        }
        
        var boxCol = GetComponentInChildren<BoxCollider>();

        if (boxCol != null) {
            return boxCol;
        }

        var meshCol = GetComponentInChildren<MeshCollider>();

        return meshCol;
    }

    public bool HasArmor() {
        return false;
    }

    public float GetHealth() {
        return currentHealth;
    }

    public float GetShieldPercent() {
        //return currentShields / Mathf.Max(maxShields,1);
        return 0;
    }
    
    public bool IsShieldActive() {
        return false;
    }
    public float GetHealthPercent() {
        return currentHealth /  Mathf.Max(GetMaxHealth(),1);
    }
    public string GetHealthRatioString() {
        return $"{currentHealth}/{GetMaxHealth()}";
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
    
    public void CartDisabled() {
        this.enabled = false;
    }

    public void CartEnabled() {
        this.enabled = true;
    }
}
