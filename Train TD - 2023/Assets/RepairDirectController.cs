using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;
public class RepairDirectController : MonoBehaviour , IDirectControllable
{
	private void Start() {
		myHealth = GetComponentInParent<ModuleHealth>();
		myRepairController = GetComponent<DroneRepairController>();
	}

	[ReadOnly]
	public ModuleHealth myHealth;
	private DroneRepairController myRepairController;

	public InputActionReference directControlShootAction => DirectControlMaster.s.directControlShootAction;
	
	
	public Image validRepairImage => DirectControlMaster.s.validRepairImage;
	public Slider repairingSlider => DirectControlMaster.s.repairingSlider;


	public bool enterDirectControlShootLock => DirectControlMaster.s.enterDirectControlShootLock;

	private bool doShake = true;
	
	public void ActivateDirectControl() {
		CameraController.s.ActivateDirectControl(myRepairController.GetComponentInChildren<DirectControlCameraPosition>().transform, true);
		repairingSlider.value = 0;
		validRepairImage.enabled = false;

		DirectControlMaster.s.repairControlUI.SetActive(true);

		//CameraShakeController.s.rotationalShake = true;

		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.shoot);
		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.exitDirectControl);

		DepthOfFieldController.s.SetDepthOfField(false);
	}

	public void DisableDirectControl() {
		CameraController.s.DisableDirectControl();

		//CameraShakeController.s.rotationalShake = false;
		
		DirectControlMaster.s.repairControlUI.SetActive(false);
		
		GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.shoot);
		GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.exitDirectControl);

		SetRepairControllerStatus(myRepairController, false);

		DepthOfFieldController.s.SetDepthOfField(true);
	}
	
	void SetRepairControllerStatus(DroneRepairController repairController, bool isDirectControl) {
		if (isDirectControl) {
			repairController.DisableAutoDrone();
			repairController.beingDirectControlled = true;
		} else {
			repairController.beingDirectControlled = false;
			repairController.ActivateAutoDrone();
		}
	}

	public LayerMask repairLookMask => DirectControlMaster.s.repairLookMask;

	public void UpdateDirectControl() {
		if (myHealth == null || myHealth.isDead || myRepairController == null) {
			// in case our module gets destroyed
			DirectControlMaster.s.DisableDirectControl();
			return;
		}

		var camTrans = MainCameraReference.s.cam.transform;
		Ray ray = new Ray(camTrans.position, camTrans.forward);
		RaycastHit hit;
		bool didHit = false;

		if (Physics.Raycast(ray, out hit, 30, repairLookMask)) {
			//myRepairController.LookAtLocation(hit.point);
			didHit = true;
			//Debug.DrawLine(ray.origin, hit.point);
		} else {
			//myRepairController.LookAtLocation(ray.GetPoint(10));
			//Debug.DrawLine(ray.origin, ray.GetPoint(10));
		}

		/*if (curCooldown >= myRepairController.GetFireDelay() && directControlShootAction.action.IsPressed() && !enterDirectControlShootLock) {
			ApplyBulletTypes();
			myRepairController.ShootBarrage(false, OnShoot, OnHit, OnMiss);
			curCooldown = 0;
		}*/
		
		
		/*curOnHitDecayTime -= Time.deltaTime;
		if (curOnHitDecayTime <= 0) {
			if (currentOnHit > 0)
				currentOnHit -= 1;

			curOnHitDecayTime = onHitSoundDecayTime;
		}*/
	}
	

	/*void OnShoot() {
		//if (doShake) {
		var range = Mathf.Clamp01(myRepairController.projectileDamage / 10f) ;
		range /= 8f;
		range *= myRepairController.directControlShakeMultiplier;
		
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
				range /= myRepairController.directControlShakeMultiplier;
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
			);#1#
		}
		//}
	}*/

	/*float hitPitch = 0.8f;
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
			myRepairController.sniperDamageMultiplier = 1+sniperMultiplier;
			sniperAmount.text = $"Smart Bullets:\n+{sniperMultiplier*100:F0}%";
		}
	}

	void OnMiss() {
		if (isSniper) {
			sniperMultiplier *= sniperMultiplierLoss;
			sniperMultiplier = Mathf.Clamp(sniperMultiplier, 0, sniperMaxMultiplier);
			myRepairController.sniperDamageMultiplier = 1+sniperMultiplier;
			sniperAmount.text = $"Smart Bullets:\n+{sniperMultiplier*100:F0}%";
		}
	}*/
	
	public Color GetHighlightColor() {
		return PlayerWorldInteractionController.s.repairColor;
	}

	public GamepadControlsHelper.PossibleActions GetActionKey() {
		return GamepadControlsHelper.PossibleActions.gunControl;
	}
}