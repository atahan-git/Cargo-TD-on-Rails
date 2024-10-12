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
	public Image arrowRepairImage => DirectControlMaster.s.arrowRepairImage;
	public Slider repairingSlider => DirectControlMaster.s.repairingSlider;
	public GameObject droppedSomething => DirectControlMaster.s.droppedSomething;
	public GameObject highWindsActive => DirectControlMaster.s.highWindsActive_repair;
	private bool droppedSomethingCheck = false;

	public bool enterDirectControlShootLock => DirectControlMaster.s.enterDirectControlShootLock;

	private bool doShake = true;

	public Vector3 currentFlightVelocity;

	public float curRepairTime;
	
	public void ActivateDirectControl() {
		CameraController.s.ActivateDirectControl(myRepairController.GetComponentInChildren<DirectControlCameraPosition>().transform, true, true);
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
		highWindsActive.SetActive(false);
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
		highWindsActive.SetActive(false);
	}
	
	void SetRepairControllerStatus(DroneRepairController repairController, bool isDirectControl) {
		if (isDirectControl) {
			if (repairController.carryDraggableMode) {
				droppedSomething.SetActive(true);
				droppedSomethingCheck = true;
			} else {
				droppedSomething.SetActive(false);
				droppedSomethingCheck = false;
			}
			repairController.StopHoldingThing();
			
			repairController.DisableAutoDrone();
			repairController.beingDirectControlled = true;
			
		} else {
			repairController.beingDirectControlled = false;
			repairController.ActivateAutoDrone();
		}
	}

	public LayerMask repairLookMask => DirectControlMaster.s.repairLookMask;


	private bool needToUnClick = false;
	public bool arrowRepairing = false;
	private RepairableBurnEffect repairTarget;
	public float keepClickTimer = 0;
	public void UpdateDirectControl() {
		if (myHealth == null || myHealth.isDead || myHealth.myCart.isDestroyed|| myHealth.myCart.isBeingDisabled  || myRepairController == null) {
			// in case our module gets destroyed
			DirectControlMaster.s.DisableDirectControl();
			return;
		}

		if (droppedSomethingCheck) {
			if (LevelReferences.s.combatHoldableThings.Count <= 0) {
				droppedSomethingCheck = false;
				droppedSomething.SetActive(false);
			}
		}
		

		var camTrans = MainCameraReference.s.cam.transform;
		var camPos = camTrans.position;
		Ray ray = new Ray(camPos, camTrans.forward);
		RaycastHit hit;
		var validTarget = false;

		if (arrowRepairing) {
			if (repairTarget == null) {
				arrowRepairing = false;
			} else {
				validTarget = true;
			}
		} else {
			repairTarget = null;
		}

		if (!arrowRepairing && !needToUnClick) {
			var allCast = Physics.SphereCastAll(ray, 0.1f, 0.5f, repairLookMask);
			RepairableBurnEffect closestArrow = null;
			float arrowMinDist = float.MaxValue;
			RepairableBurnEffect closestRegular = null;
			float regularMinDist = float.MaxValue;

			for (int i = 0; i < allCast.Length; i++) {
				var target = allCast[i].collider.GetComponentInParent<RepairableBurnEffect>();

				if (target != null) {
					if(!CanSeeTarget(target, camPos))
						continue;
					
					var dist = Vector3.Distance(camPos, target.transform.position);
					if (target.hasArrow) {
						if (dist < arrowMinDist) {
							closestArrow = target;
							arrowMinDist = dist;
						}
					} else {
						if (dist < regularMinDist) {
							closestRegular = target;
							regularMinDist = dist;
						}
					}
				}
			}

			if (closestArrow != null && closestRegular != null) {

				if (arrowMinDist < regularMinDist + 0.2f) {
					repairTarget = closestArrow;
				} else {
					repairTarget = closestRegular;
				}
				
			}else if (closestArrow != null) {
				repairTarget = closestArrow;
			}else if (closestRegular != null) {
				repairTarget = closestRegular;
			}
			
			
			if (repairTarget != null) {
				initialGrabLocalOffset = Vector3.Distance(camTrans.position, repairTarget.transform.position);
				validTarget = true;
			}
		}

		validRepairImage.gameObject.SetActive(validTarget && !repairTarget.hasArrow);

		var drone = myRepairController.drone;

		if (!HighWindsController.s.currentlyHighWinds) {
			drone.transform.rotation = Quaternion.Lerp(drone.transform.rotation, camTrans.transform.rotation, 20 * Time.unscaledDeltaTime);

			currentFlightVelocity = CalculateFlightVelocity(currentFlightVelocity, moveAction.action.ReadValue<Vector2>(), camTrans, flyUpAction.action.IsPressed(),
				flyDownAction.action.IsPressed());


			drone.transform.position += currentFlightVelocity * Time.unscaledDeltaTime;
		}

		var doRepair = shootAction.action.IsPressed() && !enterDirectControlShootLock;

		if (HighWindsController.s.currentlyHighWinds) {
			doRepair = false;
		}

		highWindsActive.SetActive(HighWindsController.s.currentlyHighWinds);
		
		
		/*var repairClick = shootAction.action.WasPerformedThisFrame() && !enterDirectControlShootLock;

		if (keepClickTimer > 0) {
			keepClickTimer -= Time.deltaTime;
			doRepair = true;
		}*/

		if (arrowRepairing && !doRepair) {
			arrowRepairing = false;
			arrowAnimating = true;
		}

		if (needToUnClick) {
			if (!doRepair) {
				needToUnClick = false;
			}
		}
		
		UpdateArrowRepairImageState(validTarget, doRepair);

		var isRepairing = doRepair && validTarget;
		drone.GetComponent<RepairDrone>().SetCurrentlyRepairingState(isRepairing);

		var directControlRepairSpeedMultiplier = 5f;

		if (validTarget) {
			if (doRepair) {
				if (repairTarget.hasArrow) {
					arrowRepairing = true;
					needToUnClick = true;
					arrowAnimating = false;
					MakeArrowPullOutMagic();
				} else {
					curRepairTime = myRepairController.TryDoRepair(repairTarget, out bool removedArrow, out bool repairSuccess, Time.deltaTime,directControlRepairSpeedMultiplier);

					/*if (repairClick) {
						curRepairTime = myRepairController.TryDoRepair(repairTarget, out removedArrow, out repairSuccess, 0.1f, 5f);
						keepClickTimer = 0.3f;
					}*/
					
					if (repairSuccess) {
						repairTarget = null;
					}
				}
			} else {
				curRepairTime -= Time.deltaTime/2f;
			}
		} else {
			curRepairTime -= Time.deltaTime;
		}

		repairingSlider.value = curRepairTime;
	}

	private void LateUpdate() {
		var camTransform = MainCameraReference.s.cam.transform;
		// matchTargetPos
		if (repairTarget != null) {
			if (!arrowAnimating) {
				if (!arrowRepairing) {
					arrowRepairImage.GetComponent<UIElementFollowWorldTarget>().OneTimeSetPosition(repairTarget.transform.position);
				} else {
					var grabPos = (camTransform.position + camTransform.forward * initialGrabLocalOffset);
					arrowRepairImage.GetComponent<UIElementFollowWorldTarget>().OneTimeSetPosition(grabPos);
				}
			}

			validRepairImage.GetComponent<UIElementFollowWorldTarget>().OneTimeSetPosition(repairTarget.transform.position);
			repairingSlider.GetComponent<UIElementFollowWorldTarget>().OneTimeSetPosition(repairTarget.transform.position);
		} else {
			repairingSlider.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
			//repairingSlider.GetComponent<UIElementFollowWorldTarget>().OneTimeSetPosition(repairTarget.transform.position);
		}
	}

	bool CanSeeTarget(RepairableBurnEffect effect, Vector3 camPos) {
		if (Physics.Raycast(camPos, effect.transform.position - camPos, out RaycastHit hit, 10, LevelReferences.s.buildingLayer)) {
			var depth = Vector3.Distance(hit.point, effect.transform.position);

			if (depth > 0.1f) {
				return false;
			} else {
				return true;
			}
			
		} else {
			return true;
		}
	}

	void UpdateArrowRepairImageState(bool validTarget, bool doRepair) {
		if (validTarget && repairTarget.hasArrow && !arrowAnimating) {
			arrowRepairImage.color = Color.green;
			if (doRepair) {
				arrowRepairImage.transform.localScale = Vector3.Lerp(arrowRepairImage.transform.localScale, Vector3.one, 20*Time.deltaTime);
			} else {
				arrowRepairImage.transform.localScale = Vector3.Lerp(arrowRepairImage.transform.localScale, Vector3.one*1.5f, 20*Time.deltaTime);
			}
		}

		if (arrowAnimating) {
			if (arrowRepairSuccess) {
				arrowRepairImage.transform.localScale = Vector3.MoveTowards(arrowRepairImage.transform.localScale, Vector3.one*10f, 25*Time.deltaTime);
				var col = arrowRepairImage.color;
				col.a = Mathf.MoveTowards(col.a, 0, 10 * Time.deltaTime);
				arrowRepairImage.color = col;

				if (col.a <= 0) {
					arrowAnimating = false;
				}
			} else {
				arrowRepairImage.transform.localScale = Vector3.MoveTowards(arrowRepairImage.transform.localScale, Vector3.one*10f, 10*Time.deltaTime);
				var col = arrowRepairImage.color;
				col.r = 1;
				col.g = 0;
				col.a = Mathf.MoveTowards(col.a, 0, 5 * Time.deltaTime);
				arrowRepairImage.color = col;

				if (col.a <= 0) {
					arrowAnimating = false;
				}
			}
		}
		
		
		arrowRepairImage.gameObject.SetActive(arrowAnimating || (validTarget && repairTarget.hasArrow));
	}

	private float initialGrabLocalOffset;
	private bool arrowRepairSuccess = false;
	private bool arrowAnimating = false;
	void MakeArrowPullOutMagic() {
		curRepairTime = 0;
		arrowRepairSuccess = false;
		
		var camTransform = MainCameraReference.s.cam.transform;
		var pullOutAmount = repairTarget.requiredPullOutAmount;
		
		var pointAPos = repairTarget.transform.position;
		var pointBPos = repairTarget.transform.position + (repairTarget.transform.forward*pullOutAmount);

		// position based movePercent
		var grabPos = (camTransform.position + camTransform.forward * initialGrabLocalOffset);
		var pullVector =  grabPos- pointAPos;
		var vectorFromAtoB = pointBPos - pointAPos;
		var pullDot = Vector3.Dot(pullVector, vectorFromAtoB);
		var distancePercent = pullDot / (0.06f * (pullOutAmount/0.25f));
		
		distancePercent = Mathf.Clamp01(distancePercent);

		//repairTarget.removeArrowState = Mathf.MoveTowards(repairTarget.removeArrowState, distancePercent, 2 * Time.deltaTime);
		repairTarget.removeArrowState = distancePercent;
		repairTarget.SetRemoveArrowState(repairTarget.removeArrowState);

		if (repairTarget.removeArrowState >= 1) {
			repairTarget.RemoveArrow();
			arrowRepairing = false;
			arrowRepairSuccess = true;
			arrowAnimating = true;
		}

		/*var distToA = Vector3.Distance(grabPos, pointAPos);
		var distToB = Vector3.Distance(grabPos, pointBPos);

		var minDist = Mathf.Min(distToA, distToB);

		if (minDist > 0.5f) {
			arrowRepairing = false;
		}*/
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
		Vector3 horizontalVelocityChange = moveDirection  * 10;

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
		previousVelocity = Vector3.ClampMagnitude(previousVelocity, 1.5f * myRepairController.GetSpeed());
		verticalSpeed = Mathf.Clamp(verticalSpeed, -1, 1);

		previousVelocity.y = verticalSpeed;

		var repairDronePos = myRepairController.drone.transform.position;
		var minDistance = float.MaxValue;
		var minDistanceMoveVector = Vector3.zero;
		for (int i = 0; i < Train.s.carts.Count; i++) {
			var pos = Train.s.carts[i].uiTargetTransform.position;
			var towardsVector = pos - repairDronePos;
			if (towardsVector.magnitude < minDistance) {
				minDistance = towardsVector.magnitude;
				minDistanceMoveVector = towardsVector;
			}
		}
		
		
		if (minDistance > 5) {
			previousVelocity = minDistanceMoveVector.normalized * 1.5f;
			if (minDistance > 10) {
				previousVelocity *= minDistance;
			}
		}

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