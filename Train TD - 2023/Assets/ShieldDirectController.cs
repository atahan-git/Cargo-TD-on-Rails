using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShieldDirectController : MonoBehaviour, IDirectControllable
{
	private void Start() {
		myHealth = GetComponentInParent<ModuleHealth>();
	}

	[ReadOnly]
	public ModuleHealth myHealth;
	public InputActionReference moveAction => DirectControlMaster.s.moveAction;
	public bool enterDirectControlShootLock => DirectControlMaster.s.enterDirectControlShootLock;

	public void ActivateDirectControl() {
		CameraController.s.ActivateDirectControl(GetComponentInChildren<DirectControlCameraPosition>().transform, true);

		DirectControlMaster.s.repairControlUI.SetActive(true);

		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.repairDroneMove);
		GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.exitDirectControl);

		DepthOfFieldController.s.SetDepthOfField(false);
	}

	public void DisableDirectControl() {
		CameraController.s.DisableDirectControl();

		//CameraShakeController.s.rotationalShake = false;
		
		DirectControlMaster.s.repairControlUI.SetActive(false);
		
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

		var move = moveAction.action.ReadValue<Vector2>();

		var rightAmount = move.x + move.y;
		rightAmount = Mathf.Clamp(rightAmount, -1, 1);
		
		physicalShield.transform.position += Vector3.forward*rightAmount*Time.deltaTime;
	}
	
	public Color GetHighlightColor() {
		return PlayerWorldInteractionController.s.shieldColor;
	}

	public GamepadControlsHelper.PossibleActions GetActionKey() {
		return GamepadControlsHelper.PossibleActions.gunControl;
	}
}