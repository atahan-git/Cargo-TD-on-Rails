using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemyHealth : MonoBehaviour, IHealth {

	public float baseHealth = 200f;
	
	[ReadOnly]
	public float maxHealth = 20f;
	public float currentHealth = 20f;

	public GameObject deathPrefab;
	public Transform aliveObject;

	public static int enemySpawned;
	public static int enemyKilled;

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


	private void Start() {
		SetUp();
	}

	public void DealDamage(float damage) {
		
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

		if (currentHealth <= 0 && isAlive) {
			Die();
		}
		
		SetBuildingShaderHealth(currentHealth / maxHealth);
	}

	public void Repair(float heal) {
		currentHealth += heal;

		if (currentHealth > maxHealth) {
			currentHealth = maxHealth;
		}

		SetBuildingShaderHealth(currentHealth / maxHealth);
	}

	void SetBuildingShaderHealth(float value) {
		var _renderers = GetComponentsInChildren<MeshRenderer>();
		for (int j = 0; j < _renderers.Length; j++) {
			var rend = _renderers[j];
			rend.material.SetFloat("_Health", value);
		}
	}
	
	void SetBuildingShaderBurn(float value) {
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

		if (activeBurnEffects.Count > targetBurnEffectCount) {
			
			while (activeBurnEffects.Count > targetBurnEffectCount) {
				var decommissioned = activeBurnEffects[0];
				activeBurnEffects.RemoveAt(0);
				decommissioned.GetComponent<SmartDestroy>().Engage();
			}
			
		}else if (activeBurnEffects.Count < targetBurnEffectCount) {

			var n = 5;
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
			Instantiate(LevelReferences.s.damageNumbersPrefab, LevelReferences.s.uiDisplayParent)
				.GetComponent<MiniGUI_DamageNumber>()
				.SetUp(uiTransform, burnDistance, false, false, true);
			DealDamage(burnDistance);

			currentBurn -= burnDistance;
		}

		if (burnSpeed > 0.05f) {
			currentBurn += burnSpeed * Time.deltaTime;
		}

		burnSpeed = Mathf.Lerp(burnSpeed,0,burnReduction*Time.deltaTime);

		if (Mathf.Abs(lastBurn - burnSpeed) > 1 || (lastBurn > 0 && burnSpeed <= 0)) {
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

	void SetUp() {
		maxHealth = baseHealth;
		maxHealth *= 1 + WorldDifficultyController.s.currentHealthIncrease;
		maxShields *= 1 + WorldDifficultyController.s.currentHealthIncrease;
		currentHealth = maxHealth;
		currentShields = maxShields;
		
		enemyUIBar = Instantiate(LevelReferences.s.enemyHealthPrefab, LevelReferences.s.uiDisplayParent).GetComponent<MiniGUI_EnemyUIBar>();
		enemyUIBar.SetUp(this);
		enemySpawned += 1;

		if (!isComponentEnemy) {
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
		enemyKilled += 1;
		isAlive = false;

		var extraRewards = GetComponentsInChildren<EnemyReward>();
		//var otherRewards = GetComponentInChildren<EnemyCartReward>();
		
		
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

			if (rewardMoney > 0) {
				Instantiate(LevelReferences.s.coinDrop, LevelReferences.s.uiDisplayParent).GetComponent<CoinDrop>().SetUp(uiTransform.position, rewardMoney);
			}

			var carryAward = GetComponent<CarrierEnemy>();
			if (carryAward) {
				carryAward.AwardTheCarriedThingOnDeath();
			}
		}

		var pos = aliveObject.position;
		var rot = aliveObject.rotation;
		
		Destroy(aliveObject.gameObject);
		Destroy(enemyUIBar.gameObject);
		
		if(deathPrefab != null)
			Instantiate(deathPrefab, pos, rot);
		
		if(!isComponentEnemy)
			GetComponentInParent<EnemySwarmMaker>().EnemyDeath(this);

		Destroy(gameObject);
	}
	
	private void OnDestroy() {
		if(enemyUIBar != null)
			if(enemyUIBar.gameObject != null)
				Destroy(enemyUIBar.gameObject);
	}

	public bool IsPlayer() {
		return false;
	}

	public bool IsAlive() {
		return isAlive;
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

	public float GetHealthPercent() {
		return currentHealth /  Mathf.Max(maxHealth,1);
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
}


public interface IHealth {
	public bool IsAlive();
	public void DealDamage(float damage);
	public void Repair(float heal);
	public void BurnDamage(float damage);
	public bool IsPlayer();
	public GameObject GetGameObject();
	public Collider GetMainCollider();
	public bool HasArmor();
	public float GetHealthPercent();
	public float GetShieldPercent();
	public bool IsShieldActive();
	public string GetHealthRatioString();

	public Transform GetUITransform();
}
