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
            if (!areInputsShuffled) {
                ShuffleInputs();
            }
            if (isCharging) {
                ChangeChargingState(false);
            }
            
            rotatePart.transform.Rotate(Vector3.up, 720 * Time.deltaTime);
            
        } else {
            RestoreInputs();
            curTime = 0;
        }
        
        
        curTime = Mathf.Clamp(curTime, 0, chargeTime + activeTime);
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


    public bool areInputsShuffled = false;
    void ShuffleInputs() {
        areInputsShuffled = true;
        SwapActionBindings(primaryClick.action, secondaryClick.action);
        SwapActionBindings(directControlLeftClick.action, directControlExit.action);
        //SwapActionBindings(directControlActivate.action, directControlExit.action);

        var exp = Instantiate(activateExplosion, transform);
        exp.transform.position = activateExplosion.transform.position;
        exp.transform.rotation = activateExplosion.transform.rotation;
        exp.SetActive(true);
        
        MiniGUI_ConfusedOverlay.s.SetConfused(true);
    }

    void RestoreInputs() {
        areInputsShuffled = false;
        primaryClick.action.RemoveAllBindingOverrides();
        secondaryClick.action.RemoveAllBindingOverrides();
        directControlLeftClick.action.RemoveAllBindingOverrides();
        //directControlActivate.action.RemoveAllBindingOverrides();
        directControlExit.action.RemoveAllBindingOverrides();
        
        MiniGUI_ConfusedOverlay.s.SetConfused(false);
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
        RestoreInputs();
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
