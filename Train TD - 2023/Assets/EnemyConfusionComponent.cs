using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyConfusionComponent : MonoBehaviour,IComponentWithTarget,IEnemyEquipment {

    public Sprite gunSprite;
    
    public Transform target;

    public bool gunActive = true;
    
    public float chargeTime = 45f;
    public float activeTime = 15f;
    public float curTime;


    public InputActionReference primaryClick;

    public InputActionReference secondaryClick;

    public InputActionReference directControlLeftClick;
    public InputActionReference directControlActivate;
    
    public InputActionReference directControlExit;

    public GameObject rotatePart;
    public GameObject chargingParticles;
    public GameObject chargedParticles;

    public GameObject activateExplosion;

    public enum ConfusionMode {
        controlFlip, screenFlip
    }

    public ConfusionMode myMode;

    private void Start() {
        chargingParticles.SetActive(true);
        chargedParticles.SetActive(true);
        
        foreach (var particleSystem in chargingParticles.GetComponentsInChildren<ParticleSystem>()) {
            particleSystem.Stop();
        }
        
        foreach (var particleSystem in chargedParticles.GetComponentsInChildren<ParticleSystem>()) {
            particleSystem.Stop();
        }
    }

    // Update is called once per frame
    void Update() {
        if(!gunActive)
            return;
        
        if (target != null) {
            curTime += Time.deltaTime;
        } else {
            curTime -= Time.deltaTime;
        }

        if (curTime < chargeTime) {
            // do nothing
            if (!isCharging) {
                ChangeChargingState(true);
            }

            var chargePercent = curTime / chargeTime;
            var rotateSpeed = chargePercent * 480 + 30;
            
            rotatePart.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        } else if (curTime < chargeTime + activeTime) {
            // shuffle!
            ApplyConfusion();
            
            if (isCharging) {
                ChangeChargingState(false);
            }
            
            rotatePart.transform.Rotate(Vector3.up, 720 * Time.deltaTime);
            
        } else {
            UndoConfusion();
            curTime = 0;
        }
        
        
        curTime = Mathf.Clamp(curTime, 0, chargeTime + activeTime);

        var curActiveTime = curTime - chargeTime;
        curActiveTime = Mathf.Clamp(curActiveTime, 0, chargeTime);
        curActiveTime = chargeTime - curActiveTime;
        MiniGUI_ConfusedOverlay.s.SetConfusedTime(curActiveTime);
    }

    public bool isCharging = false;

    void ChangeChargingState(bool _isCharging) {
        isCharging = _isCharging;
        foreach (var particleSystem in chargingParticles.GetComponentsInChildren<ParticleSystem>()) {
            if (isCharging) {
                particleSystem.Play();
            } else {
                particleSystem.Stop();
            }
        }
        
        foreach (var particleSystem in chargedParticles.GetComponentsInChildren<ParticleSystem>()) {
            if (!isCharging) {
                particleSystem.Play();
            } else {
                particleSystem.Stop();
            }
        }
    }


    public bool isConfusionApplied = false;

     void ApplyConfusion() {
         if (!isConfusionApplied) {
             isConfusionApplied = true;
             switch (myMode) {
                 case ConfusionMode.controlFlip:
                     ShuffleInputs();
                     MiniGUI_ConfusedOverlay.s.SetConfused(true, "inputs are flipped");
                     break;
                 case ConfusionMode.screenFlip:
                     FlipScreen();
                     MiniGUI_ConfusedOverlay.s.SetConfused(true, "the screen is flipped");
                     break;
             }
             
         }
     }

     void UndoConfusion() {
         if (isConfusionApplied) {
             isConfusionApplied = false;
             switch (myMode) {
                 case ConfusionMode.controlFlip:
                     RestoreInputs();
                     break;
                 case ConfusionMode.screenFlip:
                     RestoreScreen();
                     break;
             }
        
             MiniGUI_ConfusedOverlay.s.SetConfused(false, "");
         }
    }

     void FlipScreen() {
         ExtensionMethods.SetRenderFeatureState<FlipScreenRenderFeature>(true);
         PlayerWorldInteractionController.s.screenIsFlipped = true;
     }

     void RestoreScreen() {
         ExtensionMethods.SetRenderFeatureState<FlipScreenRenderFeature>(false);
         PlayerWorldInteractionController.s.screenIsFlipped = false;
     }

    void ShuffleInputs() {
        SwapActionBindings(primaryClick.action, secondaryClick.action);
        SwapActionBindings(directControlLeftClick.action, directControlExit.action);
        //SwapActionBindings(directControlActivate.action, directControlExit.action);

        var exp = Instantiate(activateExplosion, transform);
        exp.transform.position = activateExplosion.transform.position;
        exp.transform.rotation = activateExplosion.transform.rotation;
        exp.SetActive(true);
        
    }

    void RestoreInputs() {
        primaryClick.action.RemoveAllBindingOverrides();
        secondaryClick.action.RemoveAllBindingOverrides();
        directControlLeftClick.action.RemoveAllBindingOverrides();
        //directControlActivate.action.RemoveAllBindingOverrides();
        directControlExit.action.RemoveAllBindingOverrides();
    }
    
    private void SwapActionBindings(InputAction action1, InputAction action2)
    {
        // Get the current bindings
        var action1Bindings = action1.bindings;
        var action2Bindings = action2.bindings;

        // Clear existing overrides
        action1.RemoveAllBindingOverrides();
        action2.RemoveAllBindingOverrides();

        // Swap bindings
        for (int i = 0; i < action1Bindings.Count; i++)
        {
            var binding1 = action1Bindings[i];
            var binding2 = action2Bindings[i];
            
            action1.ApplyBindingOverride(i, binding2.effectivePath);
            action2.ApplyBindingOverride(i, binding1.effectivePath);
        }
    }

    private void OnDestroy() {
        UndoConfusion();
    }

    public void SetTarget(Transform _target) {
        target = _target;
    }

    public void UnsetTarget() {
        target = null;
    }

    public Transform GetRangeOrigin() {
        return transform;
    }

    public Transform GetActiveTarget() {
        return target;
    }

    public bool SearchingForTargets() {
        return gunActive;
    }


    public Sprite GetSprite() {
        return gunSprite;
    }

    public string GetName() {
        return GetComponent<ClickableEntityInfo>().info;
    }

    public string GetDescription() {
        return GetComponent<ClickableEntityInfo>().tooltip.text;
    }
}
