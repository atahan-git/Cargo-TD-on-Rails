using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class NotImplementedDirectController : MonoBehaviour, IDirectControllable, IResetState {
    private void Start() {
        myHealth = GetComponentInParent<ModuleHealth>();
    }

    public ModuleHealth myHealth;
    public Transform[] directControlCamPositions;
    
    public bool enterDirectControlShootLock => DirectControlMaster.s.enterDirectControlShootLock;
    
    public void ActivateDirectControl() {
        var currentCameraForward = MainCameraReference.s.cam.transform.forward;

        var bigestDot = float.MinValue;
        var targetTransform = directControlCamPositions[0];
        for (int i = 0; i < directControlCamPositions.Length; i++) {
            var dot = Vector3.Dot( directControlCamPositions[i].transform.forward,currentCameraForward);

            if (dot > bigestDot) {
                targetTransform = directControlCamPositions[i];
                bigestDot = dot;
            }
        }
        
        CameraController.s.ActivateDirectControl(targetTransform, false);
        
        DirectControlMaster.s.notImplementedUI.SetActive(true);
        
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.exitDirectControl);
    }
    public void UpdateDirectControl() {
        if (myHealth == null || myHealth.isDead || myHealth.myCart.isDestroyed) {
            // in case our module gets destroyed
            DirectControlMaster.s.DisableDirectControl();
            return;
        }
    }
    
    public void DisableDirectControl() {
        CameraController.s.DisableDirectControl();
        DirectControlMaster.s.notImplementedUI.SetActive(false);
        
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.exitDirectControl);
    }

    public Color GetHighlightColor() {
        return PlayerWorldInteractionController.s.directControlColor;
    }

    public GamepadControlsHelper.PossibleActions GetActionKey() {
        return GamepadControlsHelper.PossibleActions.reloadControl;
    }

    public void ResetState() {
    }
}
