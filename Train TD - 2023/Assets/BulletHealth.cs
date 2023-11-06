using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class BulletHealth : MonoBehaviour, IHealth {

	public float baseHealth = 200f;
	
	[ReadOnly]
	public float maxHealth = 20f;
	public float currentHealth = 20f;
	
	public GameObject deathPrefab;

	public bool isAlive = true;

	[ReadOnly]
	public MiniGUI_BulletUIBar enemyUIBar;

	private void Start() {
		SetUp();
	}

	public void DealDamage(float damage) {

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
					var hitEnemy = hit.collider.GetComponentInParent<BulletHealth>();
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
				.SetUp(transform, burnDistance, false, false, true);
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
	}

	public void SetUp() {
		maxHealth = baseHealth;
		maxHealth *= 1 + WorldDifficultyController.s.currentHealthIncrease;
		currentHealth = maxHealth;
		
		enemyUIBar = Instantiate(LevelReferences.s.bulletHealthPrefab, LevelReferences.s.uiDisplayParent).GetComponent<MiniGUI_BulletUIBar>();
		enemyUIBar.SetUp(this);
	}

	private Bounds myBounds;
	private float totalSize;
	private void OnEnable() {
		myBounds = transform.GetCombinedBoundingBoxOfChildren();
		totalSize = myBounds.size.magnitude;
	}

	
	void Die() {
		isAlive = false;

		Destroy(enemyUIBar.gameObject);
		
		if(deathPrefab != null)
			Instantiate(deathPrefab, transform.position, transform.rotation);

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
		return GetComponentInChildren<SphereCollider>();
	}

	public bool HasArmor() {
		return false;
	}

	public float GetHealthPercent() {
		return currentHealth /  Mathf.Max(maxHealth,1);
	}
	
	public float GetShieldPercent() {
		return 0;
	}

	public bool IsShieldActive() {
		return isShieldActive;
	}

	public string GetHealthRatioString() {
		return $"{currentHealth}/{maxHealth}";
	}

	public Transform GetUITransform() {
		return transform;
	}
}