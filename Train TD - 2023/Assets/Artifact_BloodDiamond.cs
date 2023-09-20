using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_BloodDiamond : ActivateWhenOnArtifactRow {


    // and with high enough luck you will never get commons
    public float modifyAmount = 0.25f;

    protected override void _Arm() {
        DataSaver.s.GetCurrentSave().currentRun.luck += modifyAmount;

        for (int i = 0; i <Train.s.carts.Count; i++) {
            ApplyBoost(Train.s.carts[i]);
        }
    }
    
    void ApplyBoost(Cart target) {
        if(target == null)
            return;

        bool didApply = true;
        target.GetHealthModule().luckyCart = true;

        if (didApply) {
            GetComponent<Artifact>()._ApplyToTarget(target);
        }
    }

    protected override void _Disarm() { 
        //do nothing
    }
}
