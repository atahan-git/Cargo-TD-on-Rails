using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Tier2GunModuleDirectController : MonoBehaviour, IDirectControllable
{
	private void Start() {
		myHealth = GetComponentInParent<ModuleHealth>();
		myAmmo = Train.s.GetComponent<AmmoTracker>();
		myGun = GetComponentInChildren<GunModule>();
	}

	[ReadOnly]
	public ModuleHealth myHealth;
	public AmmoTracker myAmmo;
	private GunModule myGun;

	public InputActionReference directControlShootAction => DirectControlMaster.s.directControlShootAction;
	public GameObject exitToReload => DirectControlMaster.s.exitToReload;
	public float curCooldown;
	
	public Image hitImage => DirectControlMaster.s.hitImage;
	public Slider cooldownSlider => DirectControlMaster.s.cooldownSlider;
	public Slider ammoSlider=> DirectControlMaster.s.ammoSlider;
	public Slider gatlinificationSlider => DirectControlMaster.s.gatlinificationSlider;


	public bool enterDirectControlShootLock => DirectControlMaster.s.enterDirectControlShootLock;

	private bool doShake = true;
	public bool hasAmmo => myGun.HasAmmo();
	public bool needAmmo = true;

	public bool isSniper = false;
	public float sniperMultiplier = 0;
	public float sniperMultiplierGain = 0.2f;
	public float sniperMultiplierLoss = 0.5f;
	public float sniperMaxMultiplier = 5;

	public GameObject isFire => DirectControlMaster.s.isFire;
	public GameObject isExplosive => DirectControlMaster.s.isExplosive;
	public GameObject isSticky => DirectControlMaster.s.isSticky;
	public void ActivateDirectControl() {
		CameraController.s.ActivateDirectControl(myGun.GetComponentInChildren<DirectControlCameraPosition>().transform, true);
		gatlinificationSlider.gameObject.SetActive(myGun.isGigaGatling || myGun.gatlinificator);
		gatlinificationSlider.value = myGun.gatlingAmount;

		needAmmo = myGun.ammoPerBarrage > 0;
		ammoSlider.gameObject.SetActive(needAmmo);
		
		exitToReload.SetActive(!hasAmmo);
		
		doShake = myGun.gunShakeOnShoot;
		curCooldown = myGun.GetFireDelay();
		
		DirectControlMaster.s.gunCrosshairsUI.SetActive(true);
		
		onHitAlpha = 0;

		CameraShakeController.s.rotationalShake = true;

		currentMode = myGun.isLockOn ? DirectControlMaster.GunMode.LockOn : DirectControlMaster.GunMode.Gun;
		
		switch (currentMode) {
			case DirectControlMaster.GunMode.Gun:
				gunCrosshair.gameObject.SetActive(true);
				rocketCrosshairEverything.gameObject.SetActive(false);
				CameraController.s.velocityAdjustment = true;
				break;
			case DirectControlMaster.GunMode.LockOn:
				gunCrosshair.gameObject.SetActive(false);
				rocketCrosshairEverything.gameObject.SetActive(true);
				CameraController.s.velocityAdjustment = false;
				break;
		}
		
		curRocketLockInTime = rocketLockOnTime;

		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.shoot);
		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.exitDirectControl);
		
		isSniper = myGun.isHoming;
		
		SetGunDirectControlStatus(myGun, true);
		if (isSniper) {
			sniperAmount.text = $"Smart Bullets:\n+{sniperMultiplier*100:F0}%";
			sniperAmount.gameObject.SetActive(true);
		} else {
			sniperAmount.gameObject.SetActive(false);
		}

		ApplyBulletTypes();
			
		DepthOfFieldController.s.SetDepthOfField(false);
	}

	void ApplyBulletTypes() {
		isFire.SetActive(myGun.GetBurnDamage() > 0);
		isExplosive.SetActive(myGun.GetExplosionRange() > 0);
		isSticky.SetActive(false);
	}

	public void DisableDirectControl() {
		CameraController.s.DisableDirectControl();

		CameraShakeController.s.rotationalShake = false;
		
		DirectControlMaster.s.gunCrosshairsUI.SetActive(false);
		
		GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.shoot);
		GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.exitDirectControl);
		

		isSniper = false;
		
		SetGunDirectControlStatus(myGun, false);
		
		
		DepthOfFieldController.s.SetDepthOfField(true);
	}
	
	void SetGunDirectControlStatus(GunModule gunModule, bool isDirectControl) {
		if (isDirectControl) {
			gunModule.DeactivateGun();
			gunModule.beingDirectControlled = true;
			if (isSniper) {
				gunModule.sniperDamageMultiplier = sniperMultiplier;
			}

			gunModule.directControlDamageMultiplier = DirectControlMaster.s.directControlDamageMultiplier;
			gunModule.directControlFirerateMultiplier = DirectControlMaster.s.directControlFirerateMultiplier;
			gunModule.directControlAmmoUseMultiplier = DirectControlMaster.s.directControlAmmoUseMultiplier;
		} else {
			gunModule.beingDirectControlled = false;
			gunModule.gatlingAmount = 0;
			gunModule.sniperDamageMultiplier = 1;
			
			gunModule.directControlDamageMultiplier = 1;
			gunModule.directControlFirerateMultiplier = 1;
			gunModule.directControlAmmoUseMultiplier = 1;
			myGun.ActivateGun();
		}
	}

	public LayerMask gunLookMask => DirectControlMaster.s.gunLookMask;

	public Image gunCrosshair => DirectControlMaster.s.gunCrosshair;
	public GameObject rocketCrosshairEverything => DirectControlMaster.s.rocketCrosshairEverything;
	public Image rocketCrosshairMain => DirectControlMaster.s.rocketCrosshairMain;
	public Image rocketCrosshairLock => DirectControlMaster.s.rocketCrosshairLock;
	public TMP_Text rocketLockStatus => DirectControlMaster.s.rocketLockStatus;
	public bool hasTarget = false;
	
	public float curRocketLockInTime;
	public float rocketLockOnTime = 1f;

	private float onHitAlpha = 0f;
	public float onHitAlphaDecay = 2f;

	public DirectControlMaster.GunMode currentMode;

	private bool reticleIsGreen = false;
	public TMP_Text sniperAmount => DirectControlMaster.s.sniperAmount;

	public void UpdateDirectControl() {
		if (myHealth == null || myHealth.isDead || myGun == null) {
			// in case our module gets destroyed
			DirectControlMaster.s.DisableDirectControl();
			return;
		}

		var camTrans = MainCameraReference.s.cam.transform;
		Ray ray = new Ray(camTrans.position, camTrans.forward);
		RaycastHit hit;
		bool didHit = false;

		if (Physics.Raycast(ray, out hit, 30, gunLookMask)) {
			myGun.LookAtLocation(hit.point);
			didHit = true;
			//Debug.DrawLine(ray.origin, hit.point);
		} else {
			myGun.LookAtLocation(ray.GetPoint(10));
			//Debug.DrawLine(ray.origin, ray.GetPoint(10));
		}

		float reTargetingTime = myGun.fireDelay - Mathf.Min(1, myGun.fireDelay / 2f);

		if (currentMode == DirectControlMaster.GunMode.LockOn) {
			if (didHit && curCooldown > reTargetingTime) {
				var possibleTarget = hit.collider.GetComponentInParent<PossibleTarget>();
				if (possibleTarget == null) {
					possibleTarget = hit.collider.GetComponent<PossibleTarget>();
				}

				if (possibleTarget != null && possibleTarget.myType == PossibleTarget.Type.enemy) {
					curRocketLockInTime += Time.deltaTime;
					curRocketLockInTime = Mathf.Clamp(curRocketLockInTime, 0, 1);
					myGun.SetTarget(possibleTarget.targetTransform);
					hasTarget = true;
					if (curRocketLockInTime < rocketLockOnTime) {
						rocketLockStatus.text = "Locking in";
					} else {
						rocketLockStatus.text = "Target Locked";
						if (!reticleFlashed) {
							flashCoroutine = FlashReticleOnLock();
							StartCoroutine(flashCoroutine);
							reticleFlashed = true;
						}
					}

					var enemy = possibleTarget.GetComponentInParent<EnemyHealth>();
					if (enemy != null) {
						PlayerWorldInteractionController.s.DirectControlSelectEnemy(enemy, true, false);
					}

				} else {
					curRocketLockInTime = 0;
					myGun.UnsetTarget();
					hasTarget = true;
					if (curCooldown > reTargetingTime) {
						rocketLockStatus.text = "No Targets";
					} else {
						rocketLockStatus.text = "Waiting";
					}

					if (reticleIsGreen)
						rocketCrosshairMain.color = Color.white;

					reticleFlashed = false;

					PlayerWorldInteractionController.s.Deselect();
				}
			} else {
				curRocketLockInTime = 0;
				myGun.UnsetTarget();
				hasTarget = true;
				if (curCooldown > reTargetingTime) {
					rocketLockStatus.text = "No Targets";
				} else {
					rocketLockStatus.text = "Waiting";
				}

				if (reticleIsGreen)
					rocketCrosshairMain.color = Color.white;

				reticleFlashed = false;

				PlayerWorldInteractionController.s.Deselect();
			}
		}

		var lockInPercent = curRocketLockInTime / rocketLockOnTime;
		rocketCrosshairLock.transform.rotation = Quaternion.Euler(0, 0, lockInPercent * 90);
		rocketCrosshairLock.color = new Color(1, 1, 1, lockInPercent);
		rocketCrosshairLock.transform.localScale = Vector3.one * Mathf.Lerp(1, 0.5f, lockInPercent);
		

		//if (hasAmmo) {
		switch (currentMode) {
			case DirectControlMaster.GunMode.Gun:
				if (curCooldown >= myGun.GetFireDelay() && directControlShootAction.action.IsPressed() && !enterDirectControlShootLock) {
					ApplyBulletTypes();
					myGun.ShootBarrage(false, OnShoot, OnHit, OnMiss);
					curCooldown = 0;
				}

				if (directControlShootAction.action.IsPressed() && !enterDirectControlShootLock) {
					myGun.gatlingAmount += Time.deltaTime;
					myGun.gatlingAmount = Mathf.Clamp(myGun.gatlingAmount, 0, myGun.maxGatlingAmount);
				} else {
					myGun.gatlingAmount -= Time.deltaTime;
					myGun.gatlingAmount = Mathf.Clamp(myGun.gatlingAmount, 0, myGun.maxGatlingAmount);
				}

				break;
			case DirectControlMaster.GunMode.LockOn:
				if ((curRocketLockInTime >= rocketLockOnTime && hasTarget)) {
					if (curCooldown >= myGun.GetFireDelay() && directControlShootAction.action.IsPressed() && !enterDirectControlShootLock) {
						ApplyBulletTypes();
						myGun.ShootBarrage(false, OnShoot, OnHit, OnMiss);
						curCooldown = 0;
					}
				}

				if (hasTarget) {
					myGun.gatlingAmount += Time.deltaTime;
					myGun.gatlingAmount = Mathf.Clamp(myGun.gatlingAmount, 0, myGun.maxGatlingAmount);
				} else {
					myGun.gatlingAmount -= Time.deltaTime;
					myGun.gatlingAmount = Mathf.Clamp(myGun.gatlingAmount, 0, myGun.maxGatlingAmount);
				}

				break;
		}

		gatlinificationSlider.value = Mathf.Clamp01(myGun.gatlingAmount / myGun.maxGatlingAmount);

		rocketLockOnTime = Mathf.Min(1, myGun.GetFireDelay() * (1 / 2f));
		curCooldown += Time.deltaTime;
		cooldownSlider.value = Mathf.Clamp01(1 - (curCooldown / myGun.GetFireDelay()));

		if (needAmmo) {
			ammoSlider.value = myAmmo.GetAmmoPercent();
			exitToReload.SetActive(!hasAmmo);
		}

		var hitImageColor = hitImage.color;
		hitImageColor.a = onHitAlpha;
		hitImage.color = hitImageColor;
		onHitAlpha -= onHitAlphaDecay * Time.deltaTime;
		onHitAlpha = Mathf.Clamp01(onHitAlpha);
		
		curOnHitDecayTime -= Time.deltaTime;
		if (curOnHitDecayTime <= 0) {
			if (currentOnHit > 0)
				currentOnHit -= 1;

			curOnHitDecayTime = onHitSoundDecayTime;
		}
	}

	private IEnumerator flashCoroutine;
	private bool reticleFlashed = false;
	IEnumerator FlashReticleOnLock() {
		rocketCrosshairMain.color = Color.green;
		yield return new WaitForSeconds(0.1f);
		rocketCrosshairMain.color = Color.white;

		if (!hasTarget)
			yield break;
		
		yield return new WaitForSeconds(0.1f);
		rocketCrosshairMain.color = Color.green;
		yield return new WaitForSeconds(0.1f);
		rocketCrosshairMain.color = Color.white;
		
		if (!hasTarget)
			yield break;
		
		yield return new WaitForSeconds(0.1f);
		rocketCrosshairMain.color = Color.green;
		reticleIsGreen = true;
	}
	
	IEnumerator FlashReticleOnShoot() {
		rocketCrosshairMain.color = Color.red;
		yield return new WaitForSeconds(0.5f);
		rocketCrosshairMain.color = Color.white;
	}

	void OnShoot() {
		//if (doShake) {
		var range = Mathf.Clamp01(myGun.projectileDamage / 10f) ;
		range /= 8f;
		range *= myGun.directControlShakeMultiplier;
		
		//print(range);
		if (doShake) {
			CameraShakeController.s.ShakeCamera(
				Mathf.Lerp(0.1f, 0.7f, range),
				Mathf.Lerp(0.005f, 0.045f, range),
				Mathf.Lerp(2, 10, range),
				Mathf.Lerp(0.1f, 0.5f, range),
				true
			);
			if (!SettingsController.GamepadMode()) {
				range /= myGun.directControlShakeMultiplier;
				range *= 2;
				CameraController.s.ProcessDirectControl(new Vector2(Random.Range(-range * 2, range * 2), range * 5));
			}
		} else {
			/*CameraShakeController.s.ShakeCamera(
				Mathf.Lerp(0.1f, 0.7f, range),
				Mathf.Lerp(0.001f, 0.05f, range),
				Mathf.Lerp(1, 2, range),
				Mathf.Lerp(0.1f, 0.5f, range),
				true
			);*/
		}
		//}
	}

	float hitPitch = 0.8f;
	float hitPitchRandomness = 0.1f;
	float onHitSoundDecayTime = 0.1f;
	private float curOnHitDecayTime = 0;
	int maxOnHitSimultaneously = 10;
	private int currentOnHit = 0;

	void OnHit() {
		onHitAlpha = 1;
		if (currentOnHit < maxOnHitSimultaneously) {
			DirectControlMaster.s.PlayOnHitSound( hitPitch + Random.Range(-hitPitchRandomness, hitPitchRandomness));
			currentOnHit += 1;
		}

		if (isSniper) {
			sniperMultiplier += sniperMultiplierGain;
			sniperMultiplier = Mathf.Clamp(sniperMultiplier, 0, sniperMaxMultiplier);
			myGun.sniperDamageMultiplier = 1+sniperMultiplier;
			sniperAmount.text = $"Smart Bullets:\n+{sniperMultiplier*100:F0}%";
		}
	}

	void OnMiss() {
		if (isSniper) {
			sniperMultiplier *= sniperMultiplierLoss;
			sniperMultiplier = Mathf.Clamp(sniperMultiplier, 0, sniperMaxMultiplier);
			myGun.sniperDamageMultiplier = 1+sniperMultiplier;
			sniperAmount.text = $"Smart Bullets:\n+{sniperMultiplier*100:F0}%";
		}
	}
	
	public Color GetHighlightColor() {
		return PlayerWorldInteractionController.s.directControlColor;
	}

	public GamepadControlsHelper.PossibleActions GetActionKey() {
		return GamepadControlsHelper.PossibleActions.gunControl;
	}
}
