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

	public InputActionReference shootAction => DirectControlMaster.s.shootAction;
	public InputActionReference moveAction => DirectControlMaster.s.moveAction;
	public InputActionReference flyUpAction => DirectControlMaster.s.flyUpAction;
	public InputActionReference flyDownAction => DirectControlMaster.s.flyDownAction;
	
	
	public Image validRepairImage => DirectControlMaster.s.validRepairImage;
	public Slider repairingSlider => DirectControlMaster.s.repairingSlider;


	public bool enterDirectControlShootLock => DirectControlMaster.s.enterDirectControlShootLock;

	private bool doShake = true;

	public Vector3 currentFlightVelocity;

	public float repairTime = 1f;
	public float curRepairTime;
	
	public void ActivateDirectControl() {
		CameraController.s.ActivateDirectControl(myRepairController.GetComponentInChildren<DirectControlCameraPosition>().transform, true);
		repairingSlider.value = 0;
		validRepairImage.gameObject.SetActive(false);

		DirectControlMaster.s.repairControlUI.SetActive(true);

		//CameraShakeController.s.rotationalShake = true;

		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.shoot);
		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.repairDroneMove);
		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.repairDroneUp);
		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.repairDroneDown);
		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.exitDirectControl);

		curRepairTime = 0;
		currentFlightVelocity = Vector3.zero;

		DepthOfFieldController.s.SetDepthOfField(false);


		SetRepairControllerStatus(myRepairController, true);
	}

	public void DisableDirectControl() {
		CameraController.s.DisableDirectControl();

		//CameraShakeController.s.rotationalShake = false;
		
		DirectControlMaster.s.repairControlUI.SetActive(false);
		
		GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.shoot);
		GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.repairDroneMove);
		GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.repairDroneUp);
		GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.repairDroneDown);
		GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.exitDirectControl);

		SetRepairControllerStatus(myRepairController, false);

		DepthOfFieldController.s.SetDepthOfField(true);
	}
	
	void SetRepairControllerStatus(DroneRepairController repairController, bool isDirectControl) {
		if (isDirectControl) {
			repairController.DisableAutoDrone();
			repairController.beingDirectControlled = true;
			
			repairController.StopHoldingThing();
		} else {
			repairController.beingDirectControlled = false;
			repairController.ActivateAutoDrone();
		}
	}

	public LayerMask repairLookMask => DirectControlMaster.s.repairLookMask;

	public void UpdateDirectControl() {
		if (myHealth == null || myHealth.isDead || myHealth.myCart.isDestroyed || myRepairController == null || myRepairController.carryDraggableMode) {
			// in case our module gets destroyed
			DirectControlMaster.s.DisableDirectControl();
			return;
		}

		var camTrans = MainCameraReference.s.cam.transform;
		Ray ray = new Ray(camTrans.position, camTrans.forward);
		RaycastHit hit;
		RepairableBurnEffect repairTarget = null;
		var validTarget = false;

		if (Physics.SphereCast(ray, 0.1f, out hit, 0.5f, repairLookMask)) {
			repairTarget = hit.collider.GetComponentInParent<RepairableBurnEffect>();
			
			//print(hit.collider.gameObject.name);

			if (repairTarget != null) {
				validTarget = true;
			}
		} 
		
		
		validRepairImage.gameObject.SetActive(validTarget);


		var drone = myRepairController.drone;
		
		drone.transform.rotation = Quaternion.Lerp(drone.transform.rotation, camTrans.transform.rotation, 20*Time.unscaledDeltaTime);

		currentFlightVelocity = CalculateFlightVelocity(currentFlightVelocity, moveAction.action.ReadValue<Vector2>(), camTrans, flyUpAction.action.IsPressed(),
			flyDownAction.action.IsPressed());


		drone.transform.position += currentFlightVelocity * Time.unscaledDeltaTime;

		var doRepair = shootAction.action.IsPressed() && !enterDirectControlShootLock;
		
		drone.GetComponent<RepairDrone>().SetCurrentlyRepairingState(doRepair && validTarget);

		repairTime = myRepairController.currentAffectors.directControlRepairTime * TweakablesMaster.s.myTweakables.directRepairTimeMultiplier;
		if (repairTime < 0) {
			repairTime = 0.1f;
		}

		if (validTarget) {
			if (doRepair) {
				curRepairTime += Time.deltaTime;

				
				if (curRepairTime > repairTime) {
					myRepairController.DoRepair(repairTarget.GetComponentInParent<ModuleHealth>(), repairTarget);
					
					repairingSlider.value = 1;
					
					curRepairTime = 0 ;
					return;
				}
			} else {
				curRepairTime -= Time.deltaTime;
			}
		} else {
			curRepairTime -= Time.deltaTime;
		}

		curRepairTime = Mathf.Clamp(curRepairTime, 0, repairTime);

		//print($"{validTarget} - {doRepair} - {curRepairTime}");
		repairingSlider.value = Mathf.Clamp01(curRepairTime / repairTime);
	}
	
	public Vector3 CalculateFlightVelocity(Vector3 previousVelocity, Vector3 wasdInput, Transform cameraTransform, bool upInput, bool downInput)
	{
		// Get forward and right vectors based on camera's orientation
		Vector3 forward = cameraTransform.forward;
		Vector3 right = cameraTransform.right;

		forward.y = 0f; // Ignore vertical component
		right.y = 0f;

		forward.Normalize();
		right.Normalize();

		// Calculate the movement direction based on input
		Vector3 moveDirection = (forward * wasdInput.y + right * wasdInput.x).normalized;

		// Calculate the horizontal velocity change
		Vector3 horizontalVelocityChange = moveDirection  * 10 * myRepairController.currentAffectors.droneAccelerationIncreaser/myRepairController.currentAffectors.droneAccelerationReducer;

		// Calculate the vertical velocity change
		float verticalVelocityChange = ((upInput ? 1:0) - (downInput ? 1:0)) * 10;

		var verticalSpeed = previousVelocity.y;
		previousVelocity.y = 0;

		// Apply acceleration changes
		previousVelocity += horizontalVelocityChange*Time.unscaledDeltaTime;
		verticalSpeed += verticalVelocityChange*Time.unscaledDeltaTime;

		// Apply friction
		previousVelocity = Vector3.Lerp(previousVelocity, Vector3.zero, 4f * Time.deltaTime);
		verticalSpeed = Mathf.Lerp(verticalSpeed, 0, 8 * Time.deltaTime);

		// Clamp velocity to maximum flight speed
		previousVelocity = Vector3.ClampMagnitude(previousVelocity, 1.5f);
		verticalSpeed = Mathf.Clamp(verticalSpeed, -1, 1);

		previousVelocity.y = verticalSpeed;

		return previousVelocity;
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
		return GamepadControlsHelper.PossibleActions.repairControl;
	}
}