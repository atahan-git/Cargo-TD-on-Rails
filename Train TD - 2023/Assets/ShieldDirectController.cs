using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShieldDirectController : MonoBehaviour, IDirectControllable
{
	private void Start() {
		myHealth = GetComponentInParent<ModuleHealth>();
		myShields = GetComponent<ShieldGeneratorModule>();
	}

	[ReadOnly]
	public ModuleHealth myHealth;

	[ReadOnly] public ShieldGeneratorModule myShields;
	public InputActionReference moveAction => DirectControlMaster.s.moveAction;
	public bool enterDirectControlShootLock => DirectControlMaster.s.enterDirectControlShootLock;

	public void ActivateDirectControl() {
		//CameraController.s.ActivateDirectControl(GetComponentInChildren<DirectControlCameraPosition>().transform, true);

		DirectControlMaster.s.shieldMoveUI.SetActive(true);

		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.repairDroneMove);
		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.exitDirectControl);

		DepthOfFieldController.s.SetDepthOfField(false);
	}

	public void DisableDirectControl() {
		//CameraController.s.DisableDirectControl();

		//CameraShakeController.s.rotationalShake = false;
		
		DirectControlMaster.s.shieldMoveUI.SetActive(false);
		
		GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.repairDroneMove);
		GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.exitDirectControl);

		DepthOfFieldController.s.SetDepthOfField(true);
	}

	public Transform physicalShield;
	

	public void UpdateDirectControl() {
		if (myHealth == null || myHealth.isDead || myHealth.myCart.isDestroyed) {
			// in case our module gets destroyed
			DirectControlMaster.s.DisableDirectControl();
			return;
		}

		var mainCam = MainCameraReference.s.cam.transform;

		var move = moveAction.action.ReadValue<Vector2>();

		var actualMove = -mainCam.forward * move.x + mainCam.right * move.y;
		actualMove.y = 0;

		var rightAmount = actualMove.x + actualMove.z;
		if (rightAmount > 0) {
			rightAmount = 1;
		}else if (rightAmount < 0) {
			rightAmount = -1;
		}
		
		physicalShield.transform.localPosition += Vector3.forward*rightAmount*Time.deltaTime * (myShields.currentAffectors.speed);
	}
	
	public Color GetHighlightColor() {
		return PlayerWorldInteractionController.s.shieldColor;
	}

	public GamepadControlsHelper.PossibleActions GetActionKey() {
		return GamepadControlsHelper.PossibleActions.gunControl;
	}
}