using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemyHealth : MonoBehaviour, IPlayerHoldable {

	public float baseHealth = 200f;
	
	[ReadOnly]
	public float maxHealth = 20f;
	public float currentHealth = 20f;

	public GameObject deathPrefab;
	public Transform aliveObject;

	public bool isAlive = true;

	public Transform uiTransform;
	
	[ReadOnly]
	public MiniGUI_EnemyUIBar enemyUIBar;
	
	[Space]
	public bool isComponentEnemy = false;
	public float cartEnemyLength = 0.55f;
	public GameObject deadObject;
	public bool isEngine = false;
	[Space]
	
	public float maxShields = 0;
	public float currentShields = 0;
	private float shieldRegenRate = 50;
	private float shieldRegenDelay = 1;
	public float curShieldDelay = 0;

	public EnemyInSwarm mySwarm;

	private void Start() {
		SetUp();
	}
	
	public void SetHealthBarState(bool isVisible) {
		if(enemyUIBar != null)
			enemyUIBar.SetVisible(isVisible);
	}

	[Button]
	public void DealDamage(float damage, Vector3? damageHitPoint, Quaternion? hitRotation) {
		
		curShieldDelay = shieldRegenDelay;
		
		var shieldsWasMoreThan100 = currentShields > 100;
		if (currentShields > 0) {
			currentShields -= damage;
			damage = 0;
			if (currentShields <= 0) {
				if (!shieldsWasMoreThan100) {
					damage = -currentShields;
				}

				isShieldActive = false;
				currentShields = 0;
			}
		}
		
		currentHealth -= damage;

		if ((currentHealth <= 0 || currentHealth < maxHealth*0.08f) && isAlive) {
			Die();
		}

		if (currentHealth >= 0 && !isComponentEnemy) {
			mySwarm.TookDamage(damage / maxHealth);
		}

		if (damageHitPoint.HasValue) {
			SetDamageEffects(damageHitPoint.Value, hitRotation.Value);
		}
		
		SetBuildingShaderHealth(currentHealth / maxHealth);
	}

	public void RepairChunk() {
		currentHealth += ModuleHealth.repairChunkSize;

		if (currentHealth > maxHealth) {
			currentHealth = maxHealth;
		}

		SetBuildingShaderHealth(currentHealth / maxHealth);
	}

	void SetBuildingShaderHealth(float value) {
		SetDamageEffects();
		/*return;
		var _renderers = GetComponentsInChildren<MeshRenderer>();
		for (int j = 0; j < _renderers.Length; j++) {
			var rend = _renderers[j];
			rend.material.SetFloat("_Health", value);
		}*/
	}
	
	void SetBuildingShaderBurn(float value) {
		SetBurnEffects();
		/*return;
		var _renderers = GetComponentsInChildren<MeshRenderer>();
		value = value.Remap(0, 10, 0, 0.5f);
		value = Mathf.Clamp(value, 0, 2f);
		for (int j = 0; j < _renderers.Length; j++) {
			var rend = _renderers[j];
			rend.material.SetFloat("_Burn", value);
		}
		SetBurnEffects();*/
	}
	
	public List<GameObject> activeDamageEffects = new List<GameObject>();

	GameObject SetDamageEffects(Vector3 sourcePoint, Quaternion sourceRotation) {
		var missingHealthPercent = 1-GetHealthPercent();
		var targetBurnEffectCount = Mathf.FloorToInt(missingHealthPercent*10);
		targetBurnEffectCount = Mathf.Clamp(targetBurnEffectCount, 0, 10);
		if (activeDamageEffects.Count < targetBurnEffectCount) {
			//sourcePoint=GetClosestPointOnCollider(sourcePoint);
            
			var overlapWithAnother = false;
			for (int i = 0; i < activeDamageEffects.Count; i++) {
				if (activeDamageEffects[i]!= null && Vector3.Distance(activeDamageEffects[i].transform.position, sourcePoint) < 0.045f) {
					overlapWithAnother = true;
					break;
				}
			}
            
			if (!overlapWithAnother) {
				var burnEffect = Instantiate(LevelReferences.s.enemyDamageEffect, sourcePoint, sourceRotation);
				burnEffect.transform.SetParent(transform.GetChild(0));
				activeDamageEffects.Add(burnEffect);
				return burnEffect;
			}
		}
		return null;
	}
	
	void SetDamageEffects() {
		var missingHealthPercent = 1-GetHealthPercent();
		var targetBurnEffectCount = Mathf.FloorToInt(missingHealthPercent*10);
		targetBurnEffectCount = Mathf.Clamp(targetBurnEffectCount, 0, 10);
		
		if (activeDamageEffects.Count > targetBurnEffectCount) {
			while (activeDamageEffects.Count > targetBurnEffectCount) {
				var decommissioned = activeDamageEffects[0];
				activeDamageEffects.RemoveAt(0);
				decommissioned.GetComponent<SmartDestroy>().Engage();
			}
			
		}else if (activeDamageEffects.Count < targetBurnEffectCount) {

			var n = 10;
			while (activeDamageEffects.Count < targetBurnEffectCount && n > 0) {
				var randomOnCircle = Random.insideUnitCircle * totalSize;
				var rayOrigin = transform.position + Vector3.up * 2 + new Vector3(randomOnCircle.x, 0, randomOnCircle.y);
				var ray = new Ray(rayOrigin, Vector3.down);
				RaycastHit hit;
				if (Physics.Raycast(ray, out hit, 5, LevelReferences.s.enemyLayer)) {
					var hitEnemy = hit.collider.GetComponentInParent<EnemyHealth>();
					if (hitEnemy == this) {
						var burnEffect = Instantiate(LevelReferences.s.enemyDamageEffect, hit.point, Quaternion.identity);
						burnEffect.transform.SetParent(transform.GetChild(0));
						activeDamageEffects.Add(burnEffect);
					}
				}
				n -= 1;
			}
		}

		if (activeDamageEffects.Count < targetBurnEffectCount) {
			Invoke(nameof(SetDamageEffects),0.01f);
		}
	}

	public List<GameObject> activeBurnEffects = new List<GameObject>();
	void SetBurnEffects() {
		var targetBurnEffectCount = GetBurnTier();

		targetBurnEffectCount = Mathf.Clamp(targetBurnEffectCount, 0,40);

		if (activeBurnEffects.Count > targetBurnEffectCount) {
			
			while (activeBurnEffects.Count > targetBurnEffectCount) {
				var decommissioned = activeBurnEffects[0];
				activeBurnEffects.RemoveAt(0);
				decommissioned.GetComponent<SmartDestroy>().Engage();
			}
			
		}else if (activeBurnEffects.Count < targetBurnEffectCount) {

			var n = 10;
			while (activeBurnEffects.Count < targetBurnEffectCount && n > 0) {
				var randomOnCircle = Random.insideUnitCircle * totalSize;
				var rayOrigin = transform.position + Vector3.up * 2 + new Vector3(randomOnCircle.x, 0, randomOnCircle.y);
				var ray = new Ray(rayOrigin, Vector3.down);
				RaycastHit hit;
				if (Physics.Raycast(ray, out hit, 5, LevelReferences.s.enemyLayer)) {
					var hitEnemy = hit.collider.GetComponentInParent<EnemyHealth>();
					if (hitEnemy == this) {
						var burnEffect = Instantiate(LevelReferences.s.burningEffect, hit.point, Quaternion.identity);
						burnEffect.transform.SetParent(transform.GetChild(0));
						activeBurnEffects.Add(burnEffect);
					}
				}
				n -= 1;
			}
		}

		if (activeBurnEffects.Count < targetBurnEffectCount) {
			Invoke(nameof(SetBurnEffects),0.01f);
		}
	}

	public float currentBurn = 0;
	public float burnTimer = 0;
	public int currentBurnTier;
	private int lastBurnTier;
	public float appliedBurnDamage = 0;
	public int maxBurnTier = 2;
	public const int minBurnTier = 2;
	public float burnDecayTimer = 0;
	public void BurnDamage(float damage, float normalizedBurnDamage, int extraBurnTier) {
		currentBurn += damage;
		maxBurnTier = Mathf.Max(maxBurnTier, minBurnTier+extraBurnTier);
		var actualBurnTier = maxBurnTier + BiomeEffectsController.s.GetCurrentEffects().maxBurnTierChange;
		if (Mathf.CeilToInt(currentBurn / burnTierAmount)-1 > GetBurnTier()) {
			currentBurnTier += 1;
		}
		
		currentBurn = Mathf.Clamp(currentBurn, 0, burnTierAmount * actualBurnTier);
		appliedBurnDamage = Mathf.Max(normalizedBurnDamage, appliedBurnDamage);
		burnDecayTimer = 3f;
	}

	private bool isShieldActive = true;
	private void Update() {
		if (GetFillingBurnTier()-1 > GetBurnTier()) { // increase burn tier if fill tier ever increases
			currentBurnTier += 1;
		}

		// 2 - 2 no drop
		// 2 - 1 drop to 1
		if (GetBurnTier()-2 > GetFillingBurnTier()) { // decrease burn tier if fill tier drops 1 tier below
			currentBurnTier -= 1;
		}

		if (currentBurn <= 0) {
			currentBurnTier = 0;
			appliedBurnDamage = 0;
			maxBurnTier = minBurnTier;
		}
		
		if (GetBurnTier() > 0) {
			burnTimer -= Time.deltaTime;

			if (burnTimer <= 0) {
				const float burnDelay = 0.1f;
				var burnDamage = GetBurnTier()*appliedBurnDamage*burnDelay;
				
				var damageNumbers = VisualEffectsController.s.SmartInstantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent,
					VisualEffectsController.EffectPriority.damageNumbers);
				if (damageNumbers != null) {
					damageNumbers.GetComponent<MiniGUI_DamageNumber>()
						.SetUp(uiTransform, burnDamage, false, false, true);
				}

				DealDamage(burnDamage, null, null);

				if (BiomeEffectsController.s.currentEffects.burnDecayMultiplier < 1) {
					SpawnEffectsOnCurrentFires(CommonEffectsProvider.CommonEffectType.fireSlowDecay);
				}else if (BiomeEffectsController.s.currentEffects.burnDecayMultiplier > 1) {
					SpawnEffectsOnCurrentFires(CommonEffectsProvider.CommonEffectType.fireFastDecay);
				}

				burnTimer += burnDelay;
			}
			
		}
		
		burnDecayTimer -= Time.deltaTime;

		if (burnDecayTimer <= 0) {
			currentBurn -= Mathf.Pow(GetFillingBurnTier(),1.6f) * 3f * Time.deltaTime * BiomeEffectsController.s.GetCurrentEffects().burnDecayMultiplier;
		}

		if (currentBurn < 0) {
			currentBurn = 0;
		}

		if (lastBurnTier != currentBurnTier) {
			SetBuildingShaderBurn(currentBurn);
		}
		lastBurnTier = currentBurnTier;
		
		if (curShieldDelay <= 0) {
			isShieldActive = true;
			currentShields += shieldRegenRate * Time.deltaTime + (maxShields*0.1f*Time.deltaTime);
		} else {
			curShieldDelay -= Time.deltaTime;
		}
		currentShields = Mathf.Clamp(currentShields, 0, maxShields);
	}

	void SpawnEffectsOnCurrentFires(CommonEffectsProvider.CommonEffectType type) {
		for (int i = 0; i < activeBurnEffects.Count; i++) {
			var effect = activeBurnEffects[i];
			if (effect != null) {
				CommonEffectsProvider.s.SpawnEffect(type, effect.transform.position, effect.transform.rotation, effect.transform, VisualEffectsController.EffectPriority.High);
			}
		}
	}

	private bool didRegisterAsEnemy = false;
	void SetUp() {
		maxHealth = baseHealth;
		maxHealth *= WorldDifficultyController.s.currentHealthMultiplier;
		maxShields *= WorldDifficultyController.s.currentHealthMultiplier;
		currentHealth = maxHealth;
		currentShields = maxShields;
		
		enemyUIBar = Instantiate(LevelReferences.s.enemyHealthPrefab, LevelReferences.s.uiDisplayParent).GetComponent<MiniGUI_EnemyUIBar>();
		enemyUIBar.SetUp(this);
		SetHealthBarState(false);

		if (!isComponentEnemy) {
			didRegisterAsEnemy = true;
			mySwarm = GetComponent<EnemyInSwarm>();
			GetComponentInParent<EnemySwarmMaker>().EnemySpawn(this);
		}
	}

	private Bounds myBounds;
	private float totalSize;
	private void OnEnable() {
		myBounds = transform.GetCombinedBoundingBoxOfChildren();
		totalSize = myBounds.size.magnitude;
	}

	public float rewardJuice = 20;
	void Die(bool giveRewards = true) {
		isAlive = false;

		if (giveRewards) {
			var rewardMoney = 0;
			if (maxHealth >= 80) {
				var rewardMoneyMax = Mathf.Log(maxHealth / 40, 2); // ie 80 = 1, 160 = 2, 800~= 4
				rewardMoney = Mathf.RoundToInt(Random.Range(0, rewardMoneyMax));
			} else {
				if (Random.value < maxHealth / 160) {
					rewardMoney = 1;
				}
			}

			rewardMoney *= 5;

			if (rewardMoney > 0) {
				//Instantiate(LevelReferences.s.crystalDrop, LevelReferences.s.uiDisplayParent).GetComponent<CrystalDrop>().SetUp(uiTransform.position, rewardMoney);
			}

			/*if (rewardJuice > 0) {
				Instantiate(LevelReferences.s.repairJuiceDrop, transform.position, transform.rotation).GetComponent<CrystalDrop>().SetUp(uiTransform.position, rewardMoney);
			}*/
			
			var juiceTracker = Train.s.GetComponent<RepairJuiceTracker>();
			if (juiceTracker != null) {
				juiceTracker.FillJuice(rewardJuice);
			}
		}

		var pos = aliveObject.position;
		var rot = aliveObject.rotation;
		
		Destroy(aliveObject.gameObject);
		Destroy(enemyUIBar.gameObject);
		
		if(deathPrefab != null)
			VisualEffectsController.s.SmartInstantiate(deathPrefab, pos, rot);

		if (isComponentEnemy && deadObject != null) {
			deadObject.SetActive(true);
			Destroy(GetComponent<PossibleTarget>());
		} else {
			Destroy(gameObject);
		}
	}
	
	private void OnDestroy() {
		if(enemyUIBar != null)
			if(enemyUIBar.gameObject != null)
				Destroy(enemyUIBar.gameObject);
		
		if(didRegisterAsEnemy)
			mySwarm.mySwarm.EnemyDeath(this);
	}

	public Collider GetMainCollider() {
		return GetComponentInChildren<BoxCollider>();
	}

	public float GetHealthPercent() {
		return currentHealth /  Mathf.Max(maxHealth,1);
	}

	public float GetHealth() {
		return currentHealth;
	}

	private const float burnTierAmount = 20f;
	public float GetBurnPercent() {
		return (currentBurn % burnTierAmount)/burnTierAmount;
	}

	public int GetBurnTier() {
		return currentBurnTier;
	}

	public int GetFillingBurnTier() {
		return Mathf.CeilToInt( currentBurn / burnTierAmount);
	}

	public float GetShieldPercent() {
		return currentShields / Mathf.Max(maxShields,1);
	}

	public bool IsShieldActive() {
		return isShieldActive;
	}

	public string GetHealthRatioString() {
		return $"{currentHealth}/{maxHealth}";
	}

	public Transform GetUITransform() {
		return uiTransform;
	}
	

	[ReadOnly]
	public List<HighlightEffect> _outlines = new List<HighlightEffect>();

	void SetUpOutlines() {
		if (_outlines.Count == 0) {
			var renderers = GetComponentsInChildren<MeshRenderer>(true);

			foreach (var rend in renderers) {
				if (rend.GetComponent<HighlightEffect>() == null) {
					var outline = rend.gameObject.AddComponent<HighlightEffect>();
					outline.highlighted = false;
					_outlines.Add(outline);
				}
			}
		}
	}

	public void SetHighlightState(bool isHighlighted) {
		if (_outlines.Count == 0) {
			SetUpOutlines();
		}
        
		foreach (var outline in _outlines) {
			if (outline != null) {
				outline.highlighted = isHighlighted;
			}
		}
	}

	public Transform GetUITargetTransform() {
		return uiTransform;
	}

	public void SetHoldingState(bool state) {
		// do nothing. Enemy health is not holdable
	}

	public bool CanDrag() {
		return false;
	}
	
	private DroneRepairController _holder;
	public DroneRepairController GetHoldingDrone() {
		return _holder;
	}

	public void SetHoldingDrone(DroneRepairController holder) {
		_holder = holder;
	}
}