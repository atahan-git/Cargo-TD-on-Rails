using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_HomingBullets : ActivateWhenOnArtifactRow
{

    protected override void _Arm() {
        for (int i = 0; i <Train.s.carts.Count; i++) {
            ApplyBoost(Train.s.carts[i]);
        }
    }

    void ApplyBoost(Cart target) {
        if(target == null)
            return;
        
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
            gunModule.isHoming = true;
        }
    }

    protected override void _Disarm() {
        // do nothing
    }
}


/*
: ActivateWhenOnArtifactRow
{

    protected override void _Arm() {
        for (int i = 0; i <Train.s.carts.Count; i++) {
            ApplyBoost(Train.s.carts[i]);
        }
    }

    void ApplyBoost(Cart target) {
        if(target == null)
            return;
        
        
        
        foreach (var gunModule in target.GetComponentsInChildren<GunModule>()) {
        }

        foreach (var moduleAmmo in target.GetComponentsInChildren<ModuleAmmo>()) {
        }
        
        foreach (var directControllable in target.GetComponentsInChildren<DirectControllable>()) {
        }

        foreach (var roboRepair in target.GetComponentsInChildren<RoboRepairModule>()) {
        }
        
        foreach (var trainGemBridge in target.GetComponentsInChildren<TrainGemBridge>()) {
        }
        
        foreach (var shieldGenerator in target.GetComponentsInChildren<ShieldGeneratorModule>()) {
        }
    }

    protected override void _Disarm() {
        // do nothing
    }
}
*/