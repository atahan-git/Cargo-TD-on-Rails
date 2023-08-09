using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DirectControlMaster : MonoBehaviour {
	public static DirectControlMaster s;

	private void Awake() {
		s = this;
	}

	private void Start() {
		DirectControlGameObject.SetActive(false);
		directControlInProgress = false;
		hitImageColor = hitImage.color;
		hitAud = GetComponent<AudioSource>();
	}

	public InputActionReference cancelDirectControlAction;
	public InputActionReference directControlShootAction;

	[Space]
	public ModuleHealth directControlTrainBuilding;
	public DirectControllable directControllable;
	private GunModule myGun;
	public ModuleAmmo myAmmo;
	public GameObject exitToReload;
	public float curCooldown;


	private AudioSource hitAud;
	public Image hitImage;
	private Color hitImageColor;
	public Slider cooldownSlider;
	public Slider ammoSlider;
	public GameObject DirectControlGameObject;

	public bool directControlInProgress = false;

	private bool doShake = true;
	public bool hasAmmo = true;
	private void OnEnable() {
		cancelDirectControlAction.action.Enable();
		directControlShootAction.action.Enable();
		cancelDirectControlAction.action.performed += DisableDirectControl;
	}

	private void OnDisable() {
		cancelDirectControlAction.action.Disable();
		directControlShootAction.action.Disable();
		cancelDirectControlAction.action.performed -= DisableDirectControl;
	}

	public bool isSniper = false;
	public float sniperMultiplier = 0;
	public float sniperMultiplierGain = 0.2f;
	public float sniperMultiplierLoss = 0.5f;
	public float sniperMaxMultiplier = 5;

	public float directControlLock = 0;
	public bool enterDirectControlShootLock = false;

	public GameObject isFire;
	public GameObject isExplosive;
	public GameObject isSticky;
	public void AssumeDirectControl(DirectControllable source) {
		if (!directControlInProgress && directControlLock <= 0) {
			PlayerWorldInteractionController.s.canSelect = false;
			
			CameraController.s.ActivateDirectControl(source.GetDirectControlTransform());
			directControllable = source;

			directControlTrainBuilding = directControllable.GetComponentInParent<ModuleHealth>();
			myGun = directControllable.GetComponentInParent<GunModule>();
			//myAmmo = myGun.GetComponent<ModuleAmmo>();
			//ammoSlider.gameObject.SetActive(myAmmo != null);
			ammoSlider.gameObject.SetActive(myGun.isGigaGatling || myGun.gatlinificator);
			/*if (myAmmo != null) {
				hasAmmo = myAmmo.curAmmo > 0;
			} else {*/
				exitToReload.SetActive(false);
				ammoSlider.value = myGun.gatlingAmount;
				hasAmmo = true;
			//}

			myGun.DeactivateGun();
			doShake = myGun.gunShakeOnShoot;
			myGun.beingDirectControlled = true;

			curCooldown = myGun.GetFireDelay();
			enterDirectControlShootLock = true;
			Invoke(nameof(DisableDirectControlEnterShootLock), 0.5f);

			DirectControlGameObject.SetActive(true);
			directControlInProgress = true;

			//PlayerModuleSelector.s.DeselectObject();
			//PlayerModuleSelector.s.DisableModuleSelecting();

			onHitAlpha = 0;

			CameraShakeController.s.rotationalShake = true;

			currentMode = source.myMode;

			switch (currentMode) {
				case DirectControllable.DirectControlMode.Gun:
					gunCrosshair.gameObject.SetActive(true);
					rocketCrosshairEverything.gameObject.SetActive(false);
					CameraController.s.velocityAdjustment = true;
					break;
				case DirectControllable.DirectControlMode.LockOn:
					gunCrosshair.gameObject.SetActive(false);
					rocketCrosshairEverything.gameObject.SetActive(true);
					CameraController.s.velocityAdjustment = false;
					break;
			}

			curRocketLockInTime = rocketLockOnTime;
			
			GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.shoot);
			GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.exitDirectControl);


			//sniperMultiplier = 0;
			isSniper = myGun.isHoming;
			if (isSniper) {
				sniperAmount.text = $"Smart Bullets:\n+{sniperMultiplier*100:F0}%";
				sniperAmount.gameObject.SetActive(true);
				myGun.sniperDamageMultiplier = sniperMultiplier;
			} else {
				myGun.sniperDamageMultiplier = 1;
				sniperAmount.gameObject.SetActive(false);
				
			}

			
			ApplyBulletTypes();
		}
	}

	void DisableDirectControlEnterShootLock() {
		enterDirectControlShootLock = false;
	}

	void ApplyBulletTypes() {
		isFire.SetActive(myGun.isFire);
		isExplosive.SetActive(myGun.isExplosive);
		isSticky.SetActive(myGun.isSticky);
	}

	private void DisableDirectControl(InputAction.CallbackContext obj) {
		if (directControlInProgress) {
			PlayerWorldInteractionController.s.canSelect = true;
			CameraController.s.DisableDirectControl();

			if (myGun != null) {
				myGun.beingDirectControlled = false;
				myGun.gatlingAmount = 0;
			}

			DirectControlGameObject.SetActive(false);
			directControlInProgress = false;

			//PlayerModuleSelector.s.EnableModuleSelecting();
			
			CameraShakeController.s.rotationalShake = false;
				
				
			GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.shoot);
			GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.exitDirectControl);

			directControlLock = 0.2f;

			isSniper = false;
			myGun.sniperDamageMultiplier = 1;
			
			CancelInvoke(nameof(DisableDirectControlEnterShootLock));
		}
	}

	public void DisableDirectControl() {
		DisableDirectControl(new InputAction.CallbackContext());
	}

	public LayerMask lookMask;

	public Image gunCrosshair;
	public GameObject rocketCrosshairEverything;
	public Image rocketCrosshairMain;
	public Image rocketCrosshairLock;
	public TMP_Text rocketLockStatus;
	public bool hasTarget = false;
	
	public float curRocketLockInTime;
	public float rocketLockOnTime = 1f;

	private float onHitAlpha = 0f;
	public float onHitAlphaDecay = 2f;

	public GameObject noAmmoInStorage;
	public Transform noAmmoParent;

	public DirectControllable.DirectControlMode currentMode;

	private bool reticleIsGreen = false;

	public TMP_Text sniperAmount;
	private void Update() {
		if (directControlInProgress && !Pauser.s.isPaused) {
			if (directControlTrainBuilding == null || directControlTrainBuilding.isDead || myGun == null) {
				// in case our module gets destroyed
				DisableDirectControl();
				return;
			}

			var camTrans = MainCameraReference.s.cam.transform;
			Ray ray = new Ray(camTrans.position, camTrans.forward);
			RaycastHit hit;
			bool didHit = false;
			
			if (Physics.Raycast(ray, out hit, 1000, lookMask)) {
				myGun.LookAtLocation(hit.point);
				didHit = true;
				//Debug.DrawLine(ray.origin, hit.point);
			} else {
				myGun.LookAtLocation(ray.GetPoint(10));
				//Debug.DrawLine(ray.origin, ray.GetPoint(10));
			}

			float reTargetingTime = myGun.fireDelay - Mathf.Min(1, myGun.fireDelay / 2f);

			if (currentMode == DirectControllable.DirectControlMode.LockOn) {
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
							PlayerWorldInteractionController.s.SelectEnemy(enemy, true, false);
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
						
						if(reticleIsGreen)
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
					
					if(reticleIsGreen)
						rocketCrosshairMain.color = Color.white;
					
					reticleFlashed = false;
					
					PlayerWorldInteractionController.s.Deselect();
				}
			}


			var lockInPercent = curRocketLockInTime / rocketLockOnTime;
			rocketCrosshairLock.transform.rotation = Quaternion.Euler(0, 0,lockInPercent * 90);
			rocketCrosshairLock.color = new Color(1, 1, 1, lockInPercent);
			rocketCrosshairLock.transform.localScale = Vector3.one * Mathf.Lerp(1,0.5f,lockInPercent);


			if (directControlShootAction.action.WasReleasedThisFrame()) {
				enterDirectControlShootLock = false;
			}

			//if (hasAmmo) {
			switch (currentMode) {
					case DirectControllable.DirectControlMode.Gun:
						if (curCooldown >= myGun.GetFireDelay() && directControlShootAction.action.IsPressed()&& !enterDirectControlShootLock) {
							ApplyBulletTypes();
							myGun.ShootBarrage(false, OnShoot, OnHit, OnMiss);
							curCooldown = 0;

							/*if (currentMode == DirectControlAction.DirectControlMode.LockOn) {
								StopCoroutine(flashCoroutine);
								reticleIsGreen = false;
								StartCoroutine(FlashReticleOnShoot());
							}*/
						}
						
						if (directControlShootAction.action.IsPressed()&& !enterDirectControlShootLock) {
							myGun.gatlingAmount += Time.deltaTime;
							myGun.gatlingAmount = Mathf.Clamp(myGun.gatlingAmount, 0, myGun.maxGatlingAmount);
						}else{
							myGun.gatlingAmount -= Time.deltaTime;
							myGun.gatlingAmount = Mathf.Clamp(myGun.gatlingAmount, 0, myGun.maxGatlingAmount);
						}
						
						break;
					case DirectControllable.DirectControlMode.LockOn:
						if ((curRocketLockInTime >= rocketLockOnTime && hasTarget)) {
							if (curCooldown >= myGun.GetFireDelay() && directControlShootAction.action.IsPressed()&& !enterDirectControlShootLock) {
								ApplyBulletTypes();
								myGun.ShootBarrage(false, OnShoot, OnHit, OnMiss);
								curCooldown = 0;

								/*if (currentMode == DirectControlAction.DirectControlMode.LockOn) {
									StopCoroutine(flashCoroutine);
									reticleIsGreen = false;
									StartCoroutine(FlashReticleOnShoot());
								}*/
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
					
				ammoSlider.value = Mathf.Clamp01(myGun.gatlingAmount/myGun.maxGatlingAmount);
			/*} else {
				if (directControlShootAction.action.triggered) {
					/*var managedToReload = myAmmo.GetComponent<ReloadAction>().EngageAction();
					if (!managedToReload) {
						Instantiate(noAmmoInStorage, noAmmoParent);
					}#1#

					curCooldown = myGun.GetFireDelay()*2;
				}
			}*/

			rocketLockOnTime = Mathf.Min(1, myGun.GetFireDelay() * (1 / 2f));
			curCooldown += Time.deltaTime;
			cooldownSlider.value = Mathf.Clamp01(1-(curCooldown / myGun.GetFireDelay()));

			/*if (myAmmo != null) {
				ammoSlider.value = myAmmo.curAmmo / myAmmo.maxAmmo;
				hasAmmo = myAmmo.curAmmo > 0;
				exitToReload.SetActive(!hasAmmo);
			}*/

			hitImageColor.a = onHitAlpha;
			hitImage.color = hitImageColor;
			onHitAlpha -= onHitAlphaDecay * Time.deltaTime;
			onHitAlpha = Mathf.Clamp01(onHitAlpha);
		}

		curOnHitDecayTime -= Time.deltaTime;
		if (curOnHitDecayTime <= 0) {
			if (currentOnHit > 0)
				currentOnHit -= 1;

			curOnHitDecayTime = onHitSoundDecayTime;
		}

		if (directControlLock > 0) {
			directControlLock -= Time.deltaTime;
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
			if(!SettingsController.GamepadMode())
				CameraController.s.ProcessDirectControl(new Vector2(Random.Range(-range*2, range*2), range*5));
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
			hitAud.pitch = hitPitch + Random.Range(-hitPitchRandomness, hitPitchRandomness);
			hitAud.PlayOneShot(hitAud.clip);
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
}
