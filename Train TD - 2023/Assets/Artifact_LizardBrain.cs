using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_LizardBrain : MonoBehaviour, IChangeStateToEntireTrain {

    public GameObject lizardHead;

    public Vector3 defaultLocation;
    
    public Cart currentTarget;
    void Start() {
        defaultLocation = lizardHead.transform.localPosition;
        DirectControlMaster.s.OnDirectControlStateChange.AddListener(OnDirectControlStateChange);
    }

    private void OnDestroy() {
        if (DirectControlMaster.s != null) {
            DirectControlMaster.s.OnDirectControlStateChange.RemoveListener(OnDirectControlStateChange);
        }
    }

    // Update is called once per frame
    void Update() {
        var targetPos = defaultLocation;
        if (currentTarget != null) {
            targetPos = currentTarget.transform.position + Vector3.up * 1.5f;
            targetPos = lizardHead.transform.parent.InverseTransformPoint(targetPos);
        } 
        
        lizardHead.transform.localPosition = Vector3.Lerp(lizardHead.transform.localPosition, targetPos, 3 * Time.deltaTime);
    }

    void OnDirectControlStateChange(bool state) {
        var curDirControllable = (DirectControlMaster.s.currentDirectControllable as MonoBehaviour);
        if (state) {
            if (curDirControllable.GetComponentInParent<Cart>() == currentTarget) {
                if (currentTarget != null) {
                    ChangeCartEffects(currentTarget, false);
                    currentTarget = null;
                }
            }
        } else {
            if (currentTarget != null) {
                ChangeCartEffects(currentTarget, false);
                currentTarget = null;
            }
            
            currentTarget = curDirControllable.GetComponentInParent<Cart>();

            ChangeCartEffects(currentTarget, true);
        }
    }

    void ChangeCartEffects(Cart target, bool isApplying) {
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            if (isApplying) {
                gunModule.currentAffectors.power *= DirectControlMaster.s.directControlDamageMultiplier;
                gunModule.currentAffectors.speed *= DirectControlMaster.s.directControlFirerateMultiplier;
                gunModule.currentAffectors.lizardOverride = true;
                gunModule.ActivateGun();
            } else {
                gunModule.currentAffectors.power /= DirectControlMaster.s.directControlDamageMultiplier;
                gunModule.currentAffectors.speed /= DirectControlMaster.s.directControlFirerateMultiplier;
                gunModule.currentAffectors.lizardOverride = false;
                if (!gunModule.currentAffectors.IsActive()) {
                    gunModule.DeactivateGun();
                }
            }
        }

        foreach (var repairModule in target.GetComponentsInChildren<DroneRepairController>()) {
            if (isApplying) {
                repairModule.currentAffectors.power *= 3;
                repairModule.currentAffectors.lizardOverride = true;
            } else {
                repairModule.currentAffectors.power /= 3;
                repairModule.currentAffectors.lizardOverride = false;
            }
        }
        
        foreach (var ammoModule in target.GetComponentsInChildren<ModuleAmmo>()) {
            if (isApplying) {
                ammoModule.currentAffectors.reloadOverTime += 1;
            } else {
                ammoModule.currentAffectors.reloadOverTime -= 1;
            }
        }
        
        
        foreach (var engineModule in target.GetComponentsInChildren<EngineModule>()) {
            if (isApplying) {
                engineModule.lizardControlled = false;
            } else {
                engineModule.lizardControlled = true;
            }
        }
        
    }

    public void ChangeStateToEntireTrain(List<Cart> carts) {
        if (currentTarget != null) {
            ChangeCartEffects(currentTarget, true);
        }
    }
}
