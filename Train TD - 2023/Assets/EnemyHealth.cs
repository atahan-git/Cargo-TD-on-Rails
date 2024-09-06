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
	
	public bool isComponentEnemy = false;
	
	public float maxShields = 0;
	public float currentShields = 0;
	private float shieldRegenRate = 50;
	private float shieldRegenDelay = 1;
	public float curShieldDelay = 0;

	public EnemyInSwarm mySwarm;

	private void Start() {
		SetUp();
	}

	[Button]
	public void DealDamage(float damage, Vector3? damageHitPoint) {
		
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

		if (currentHealth >= 0) {
			mySwarm.TookDamage(damage / maxHealth);
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
		return;
		var _renderers = GetComponentsInChildren<MeshRenderer>();
		for (int j = 0; j < _renderers.Length; j++) {
			var rend = _renderers[j];
			rend.material.SetFloat("_Health", value);
		}
	}
	
	void SetBuildingShaderBurn(float value) {
		SetBurnEffects();
		return;
		var _renderers = GetComponentsInChildren<MeshRenderer>();
		value = value.Remap(0, 10, 0, 0.5f);
		value = Mathf.Clamp(value, 0, 2f);
		for (int j = 0; j < _renderers.Length; j++) {
			var rend = _renderers[j];
			rend.material.SetFloat("_Burn", value);
		}
		SetBurnEffects();
	}

	public List<GameObject> activeBurnEffects = new List<GameObject>();
	void SetBurnEffects() {
		var targetBurnEffectCount = (int)burnSpeed/2f;

		targetBurnEffectCount = Mathf.Clamp(targetBurnEffectCount, 0,20);

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

[NonSerialized]
	public float burnReduction = 0.5f;
	public float currentBurn = 0;
	public float burnSpeed = 0;
	private float lastBurn;
	public void BurnDamage(float damage) {
		burnSpeed += damage;
	}

	private bool isShieldActive = true;
	private void Update() {
		var burnDistance = Mathf.Max(burnSpeed / 2f, 1f);
		if (currentBurn >= burnDistance) {
			var damageNumbers = VisualEffectsController.s.SmartInstantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent,
				VisualEffectsController.EffectPriority.damageNumbers);
			if (damageNumbers != null) {
				damageNumbers.GetComponent<MiniGUI_DamageNumber>()
					.SetUp(uiTransform, burnDistance, false, false, true);
			}

			DealDamage(burnDistance, null);

			currentBurn -= burnDistance;
		}

		if (burnSpeed > 0.05f) {
			currentBurn += burnSpeed * Time.deltaTime;
		}

		burnSpeed = Mathf.Lerp(burnSpeed,0,burnReduction*Time.deltaTime);

		if (Mathf.Abs(lastBurn - burnSpeed) > 1f || (lastBurn > 0 && burnSpeed <= 0)) {
			SetBuildingShaderBurn(burnSpeed);
			lastBurn = burnSpeed;
		}
		
		if (curShieldDelay <= 0) {
			isShieldActive = true;
			currentShields += shieldRegenRate * Time.deltaTime + (maxShields*0.1f*Time.deltaTime);
		} else {
			curShieldDelay -= Time.deltaTime;
		}
		currentShields = Mathf.Clamp(currentShields, 0, maxShields);
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
		}

		var pos = aliveObject.position;
		var rot = aliveObject.rotation;
		
		Destroy(aliveObject.gameObject);
		Destroy(enemyUIBar.gameObject);
		
		if(deathPrefab != null)
			VisualEffectsController.s.SmartInstantiate(deathPrefab, pos, rot);

		Destroy(gameObject);
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
}