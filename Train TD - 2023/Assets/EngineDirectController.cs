using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class EngineDirectController : MonoBehaviour, IDirectControllable, IResetState {

    
    private void Start() {
        myHealth = GetComponentInParent<ModuleHealth>();
        myEngineModule = GetComponentInChildren<EngineModule>();
    }
    
    public ModuleHealth myHealth;
    public EngineModule myEngineModule;
    public Transform[] directControlCamPositions;

    public InputActionReference activateAction => DirectControlMaster.s.activateAction;
    public bool enterDirectControlShootLock => DirectControlMaster.s.enterDirectControlShootLock;
    
    
    public SpeedometerScript pressureGauge =>DirectControlMaster.s.pressureGauge;
    public TMP_Text pressureInfo =>DirectControlMaster.s.pressureInfo;
    
    public Affectors currentAffectors;

    [Serializable]
    public class Affectors {
        public bool vampiric = false;
    }
    
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
        
        
        DirectControlMaster.s.trainEngineControlUI.SetActive(true);
        
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.directControlActivate);
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.engineControlSwitch);
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.exitDirectControl);
    }

    public int crystalStored = 0;
    public void UpdateDirectControl() {
        if (myHealth == null || myHealth.isDead || myHealth.myCart.isDestroyed || myHealth.myCart.isBeingDisabled  || myEngineModule == null) {
            // in case our module gets destroyed
            DirectControlMaster.s.DisableDirectControl();
            return;
        }

        var click = activateAction.action.WasPerformedThisFrame() && !enterDirectControlShootLock;

        if (click) {
            if (crystalStored <= 0) {
                //var crystalIn = CrystalsAndWarpController.s.TryUseCrystals(1);
                var crystalIn = 1;

                if (crystalIn > 0) {
                    crystalStored += crystalIn/**5*/;
                }
            }

            if (crystalStored > 0) {
                myEngineModule.currentPressure +=  0.05f;
                crystalStored -= 1;
            }

            DoAdd();
        }
        
        pressureGauge.SetSpeed(myEngineModule.currentPressure);
        pressureInfo.text = $"Pull Strength: {myEngineModule.GetEffectivePressure():0.0}\n" +
                            $"Pressure Use: {myEngineModule.GetPressureUse()}/s\n" +
                            $"Self Damage: {myEngineModule.GetSelfDamageMultiplier()}";


    }

    public float vampiricHealthStorage;
    void DoAdd() {
        if (currentAffectors.vampiric) {
            vampiricHealthStorage += 15;

            if (vampiricHealthStorage > ModuleHealth.repairChunkSize) {
                GetComponentInParent<ModuleHealth>().RepairChunk();
                vampiricHealthStorage -= ModuleHealth.repairChunkSize;
            }
        }
    }


    public void DisableDirectControl() {
        CameraController.s.DisableDirectControl();
        DirectControlMaster.s.trainEngineControlUI.SetActive(false);

        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.directControlActivate);
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.engineControlSwitch);
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.exitDirectControl);
    }

    public Color GetHighlightColor() {
        return PlayerWorldInteractionController.s.engineBoostColor;
    }

    public GamepadControlsHelper.PossibleActions GetActionKey() {
        return GamepadControlsHelper.PossibleActions.engineControl;
    }

    public void ResetState() {
        currentAffectors = new Affectors();
    }
}